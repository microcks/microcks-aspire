//
// Copyright The Microcks Authors.
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Microcks.Aspire.Clients.Model;
using Microcks.Aspire.Tests.Fixtures.Async.Kafka;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Aspire.Hosting;
using System.Text.Json;

namespace Microcks.Aspire.Tests.Features.Async.Kafka;

/// <summary>
/// Tests for the Microcks Async Minion with Kafka resource builder and runtime behavior.
/// Uses a shared Microcks instance with Async Minion and Kafka provided by <see cref="MicrocksKafkaFixture"/>.
/// </summary>
[Collection(MicrocksKafkaCollection.CollectionName)]
public sealed class MicrocksKafkaTests(MicrocksKafkaFixture fixture)
{
    private readonly MicrocksKafkaFixture _fixture = fixture;

    /// <summary>
    /// When the application is started, then the MicrocksAsyncMinionResource and Kafka are available.
    /// </summary>
    [Fact]
    public void WhenApplicationIsStarted_ThenMicrocksAsyncResourceAndKafkaAreAvailable()
    {
        Assert.NotNull(_fixture.App);
        Assert.NotNull(_fixture.MicrocksResource);

        var app = _fixture.App;
        var name = _fixture.MicrocksResource.Name;
        string expectedAsyncMinionName = $"{name}-async-minion";

        // Retrieve Resources from application (MicrocksAsyncMinion and Kafka)
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Check Microcks Async Minion
        var microcksAsyncMinionResources = appModel.Resources.OfType<MicrocksAsyncMinionResource>();
        MicrocksAsyncMinionResource asyncMinionResource = Assert.Single(microcksAsyncMinionResources);
        Assert.Equal(expectedAsyncMinionName, asyncMinionResource.Name);

        // Check Kafka resource
        var kafkaResources = appModel.Resources.Where(r => r.Name == "kafka");
        var kafkaResource = Assert.Single(kafkaResources);
        Assert.Equal("kafka", kafkaResource.Name);
    }

    /// <summary>
    /// When a Kafka message is sent by Microcks Async Minion, then it is received.
    /// </summary>
    [Fact]
    public async Task WhenKafkaMessageIsSend_ThenItIsReceived()
    {
        using var host = await CreateKafkaClientHostAsync();
        const string expectedMessage = "{\"id\":\"4dab240d-7847-4e25-8ef3-1530687650c8\",\"customerId\":\"fe1088b3-9f30-4dc1-a93d-7b74f0a072b9\",\"status\":\"VALIDATED\",\"productQuantities\":[{\"quantity\":2,\"pastryName\":\"Croissant\"},{\"quantity\":1,\"pastryName\":\"Millefeuille\"}]}";

        var appModel = _fixture.App.Services
            .GetRequiredService<DistributedApplicationModel>();
        // Retrieve MicrocksAsyncMinionResource from application
        var microcksAsyncMinionResource = appModel.GetContainerResources()
            .OfType<MicrocksAsyncMinionResource>()
            .Single();

        // Get Kafka consumer from host
        var consumer = host.Services.GetRequiredService<IConsumer<string, string>>();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ConsumeException>() })
            .Build();

        pipeline.Execute(() =>
        {
            // Subscribe to the Kafka topic used by Microcks Async Minion for the pastry/orders subscription
            var kafkaTopic = microcksAsyncMinionResource
                .GetKafkaMockTopic("Pastry orders API", "0.1.0", "SUBSCRIBE pastry/orders");
            consumer.Subscribe(kafkaTopic);

            string message = null;

            // Consume message from Kafka 5000 milliseconds attempt
            var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(5000));

            if (consumeResult != null)
            {
                message = consumeResult.Message.Value;
            }

            Assert.Equal(expectedMessage, message);
        });
    }

    /// <summary>
    /// When a good kafka message is sent to Kafka Topic, then Microcks Async API Schema test returns correct status.
    /// </summary>
    [Fact]
    public async Task WhenGoodMessageIsSent_ThenReturnsCorrectStatus()
    {
        using var host = await CreateKafkaClientHostAsync();

        const string message =
            "{\"id\":\"abcd\",\"customerId\":\"efgh\",\"status\":\"CREATED\",\"productQuantities\":[{\"quantity\":2,\"pastryName\":\"Croissant\"},{\"quantity\":1,\"pastryName\":\"Millefeuille\"}]}";

        var testRequest = new TestRequest
        {
            ServiceId = "Pastry orders API:0.1.0",
            RunnerType = TestRunnerType.ASYNC_API_SCHEMA,
            TestEndpoint = "kafka://kafka:9093/pastry-orders", // 9093 is the internal Docker network port
            Timeout = TimeSpan.FromSeconds(5)
        };

        var microcksClient = _fixture.App.CreateMicrocksClient(_fixture.MicrocksResource.Name);

        // Get Kafka producer and admin client from host
        var producer = host.Services.GetRequiredService<IProducer<string, string>>();
        var adminClient = host.Services.GetRequiredService<IAdminClient>();

        // Start the test (this will fail initially as there are no messages being sent)
        // but it validates that the test setup and endpoint format are correct
        var taskTestResult = microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);
        
        TestContext.Current.TestOutputHelper
            .WriteLine($"{DateTime.Now.ToLocalTime()} Test started...");

        // Wait for the Microcks Async Minion consumer to connect to the Kafka topic
        await WaitForMinionConsumerAsync(adminClient, "pastry-orders", TestContext.Current.CancellationToken);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>() })
            .Build();

        // Act
        for (var i = 0; i < 5; i++)
        {
            await pipeline.ExecuteAsync(async cancellationToken =>
            {
                var deliveryResult = await producer.ProduceAsync("pastry-orders", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                }, cancellationToken);
            }, TestContext.Current.CancellationToken);

            producer.Flush(TestContext.Current.CancellationToken);
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        // Wait for a test result
        var testResult = await taskTestResult;

        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        TestContext.Current.TestOutputHelper.WriteLine(json);

        // Assert
        Assert.False(testResult.InProgress);
        Assert.True(testResult.Success);
        Assert.Equal(testRequest.TestEndpoint, testResult.TestedEndpoint);

        var testCaseResult = testResult.TestCaseResults.First();
        var testStepResults = testCaseResult.TestStepResults;

        // Minimum 1 message captured
        Assert.NotEmpty(testStepResults);
        // No error message
        Assert.Null(testStepResults.First().Message);
    }

    /// <summary>
    /// When a bad kafka message is sent to Kafka Topic, then Microcks Async API Schema test returns correct status.
    /// Message: "Required property 'status' not found" 
    /// And event messages can be retrieved.
    /// </summary>
    [Fact]
    public async Task WhenBadMessageIsSent_ThenReturnsCorrectStatus()
    {
        using var host = await CreateKafkaClientHostAsync();

        const string message = "{\"id\":\"abcd\",\"customerId\":\"efgh\",\"productQuantities\":[{\"quantity\":2,\"pastryName\":\"Croissant\"},{\"quantity\":1,\"pastryName\":\"Millefeuille\"}]}";

        var testRequest = new TestRequest
        {
            ServiceId = "Pastry orders API:0.1.0",
            RunnerType = TestRunnerType.ASYNC_API_SCHEMA,
            TestEndpoint = "kafka://kafka:9093/pastry-orders", // 9093 is the internal Docker network port
            Timeout = TimeSpan.FromMilliseconds(30000)
        };

        var microcksClient = _fixture.App.CreateMicrocksClient(_fixture.MicrocksResource.Name);

        // Get Kafka producer and admin client from host
        var producer = host.Services.GetRequiredService<IProducer<string, string>>();
        var adminClient = host.Services.GetRequiredService<IAdminClient>();

        // Start the test (this will fail initially as there are no messages being sent)
        // but it validates that the test setup and endpoint format are correct
        var taskTestResult = microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);
        
        TestContext.Current.TestOutputHelper
            .WriteLine($"{DateTime.Now.ToLocalTime()} Test started...");
            
        // Wait for the Microcks Async Minion consumer to connect to the Kafka topic
        await WaitForMinionConsumerAsync(adminClient, "pastry-orders", TestContext.Current.CancellationToken);

        // Retry policy for producing messages
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>() })
            .Build();

        // Act
        for (var i = 0; i < 5; i++)
        {
            TestContext.Current.TestOutputHelper
                .WriteLine($"{DateTime.Now.ToLocalTime()} Sending bad message #{i + 1}: {message}");
            await pipeline.ExecuteAsync(async cancellationToken =>
            {
                var deliveryResult = await producer.ProduceAsync("pastry-orders", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                }, cancellationToken);
            }, TestContext.Current.CancellationToken);

            producer.Flush(TestContext.Current.CancellationToken);
            await Task.Delay(500, TestContext.Current.CancellationToken);

            TestContext.Current.TestOutputHelper
                .WriteLine($"{DateTime.Now.ToLocalTime()} Bad message #{i + 1} sent.");
        }

        // Wait for a test result
        var testResult = await taskTestResult;

        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        TestContext.Current.TestOutputHelper.WriteLine(json);

        // Assert
        //Assert.False(testResult.InProgress, "Test should have completed");
        Assert.False(testResult.Success, "Test should have failed");
        Assert.Equal(testRequest.TestEndpoint, testResult.TestedEndpoint);

        var testCaseResult = testResult.TestCaseResults.First();
        var testStepResults = testCaseResult.TestStepResults;

        // Minimum 1 message captured
        Assert.NotEmpty(testStepResults);

        // Error message status is missing
        Assert.Contains("required property 'status' not found", testStepResults.First().Message);

        // Retrieve event messages for the failing test case.
        List<UnidirectionalEvent> events = await microcksClient.GetEventMessagesForTestCaseAsync(
            testResult, "SUBSCRIBE pastry/orders", TestContext.Current.CancellationToken);

        // We should have at least 4 events.
        Assert.True(events.Count >= 4, "At least 4 events should have been captured.");

        // Check that all events have the correct message.
        Assert.All(events, e =>
        {
            // Check these are the correct message.
            Assert.NotNull(e.EventMessage);
            Assert.Equal(message, e.EventMessage.Content);
        });
    }

    /// <summary>
    /// Creates a host with Kafka producer and consumer configured to connect to the Kafka resource in the fixture.
    /// </summary>
    private async Task<IHost> CreateKafkaClientHostAsync()
    {
        var hostBuilder = Host.CreateApplicationBuilder();

        var kafkaResource = _fixture.KafkaResource;
        var kafkaConnectionStringExpression = kafkaResource.ConnectionStringExpression;
        // Assign connection string to configuration
        hostBuilder.Configuration[$"ConnectionStrings:{kafkaResource.Name}"]
            = await kafkaConnectionStringExpression.GetValueAsync(TestContext.Current.CancellationToken);

        // Add Kafka producer and consumer (kafka is the name of the resource in the fixture)
        hostBuilder.AddKafkaProducer<string, string>("kafka", producerBuilder =>
        {
            producerBuilder.Config.Acks = Acks.All;
        });
        hostBuilder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
        {
            consumerBuilder.Config.GroupId = "aspire-consumer-group";
            consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
        
        // Add AdminClient for checking consumer group status
        hostBuilder.Services.AddSingleton<IAdminClient>(serviceProvider =>
        {
            var connectionString = hostBuilder.Configuration[$"ConnectionStrings:{kafkaResource.Name}"];
            var adminConfig = new AdminClientConfig { BootstrapServers = connectionString };
            return new AdminClientBuilder(adminConfig).Build();
        });
        
        var host = hostBuilder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }
    
    /// <summary>
    /// Waits for a Kafka consumer (the Microcks Async Minion) to connect to a specific topic 
    /// by checking with the Kafka Admin API until a consumer with the minion's client ID appears.
    /// </summary>
    /// <remarks>
    /// Waits up to 5 seconds (10 attempts × 500ms) for the consumer to connect.
    /// The minion creates consumer groups with pattern {testResultId}-{timestamp} and 
    /// uses client ID "microcks-async-minion-test".
    /// </remarks>
    private async Task WaitForMinionConsumerAsync(IAdminClient adminClient, string topic, CancellationToken cancellationToken)
    {
        // Configuration for consumer detection
        const int MaxWaitAttempts = 10;  // Maximum number of polling attempts (10 × 500ms = 5 seconds total)
        const int DelayBetweenAttemptsMs = 500;  // Delay between polling attempts
        const int AdminApiTimeoutSeconds = 3;  // Timeout for Admin API calls
        const string MinionClientId = "microcks-async-minion-test";  // Client ID used by Microcks Async Minion
        
        TestContext.Current.TestOutputHelper
            .WriteLine($"{DateTime.Now.ToLocalTime()} Waiting for Microcks Async Minion consumer to connect to topic '{topic}'...");
        
        for (int attempt = 0; attempt < MaxWaitAttempts; attempt++)
        {
            try
            {
                // List all consumer groups
                var groups = adminClient.ListGroups(TimeSpan.FromSeconds(AdminApiTimeoutSeconds));
                
                // Look for consumer groups with members that have the Microcks minion client ID
                // The minion creates groups with pattern {testResultId}-{timestamp}
                var minionGroups = groups.Where(g => 
                    g.Members != null && g.Members.Any(m => m.ClientId == MinionClientId)).ToList();
                
                if (minionGroups.Any())
                {
                    foreach (var group in minionGroups)
                    {
                        TestContext.Current.TestOutputHelper
                            .WriteLine($"{DateTime.Now.ToLocalTime()} Found Microcks consumer group '{group.Group}' with client ID '{MinionClientId}'.");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                TestContext.Current.TestOutputHelper
                    .WriteLine($"{DateTime.Now.ToLocalTime()} Attempt {attempt + 1}/{MaxWaitAttempts} - Waiting for consumer: {ex.Message}");
            }
            
            await Task.Delay(DelayBetweenAttemptsMs, cancellationToken);
        }
        
        TestContext.Current.TestOutputHelper
            .WriteLine($"{DateTime.Now.ToLocalTime()} Warning: No Microcks consumer with client ID '{MinionClientId}' detected after {MaxWaitAttempts} attempts. Proceeding anyway...");
    }
}
