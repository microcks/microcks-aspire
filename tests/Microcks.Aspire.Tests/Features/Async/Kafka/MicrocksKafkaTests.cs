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
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microcks.Aspire.Async;
using Microcks.Aspire.Clients.Model;
using Microcks.Aspire.Tests.Fixtures.Async.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async.Kafka;

/// <summary>
/// Tests for the Microcks Async Minion with Kafka resource builder and runtime behavior.
/// Uses a shared Microcks instance with Async Minion and Kafka provided by <see cref="MicrocksKafkaFixture"/>.
/// </summary>
/// <param name="testOutputHelper">The test output helper for logging.</param>
/// <param name="fixture">The Microcks Kafka fixture.</param>
public sealed class MicrocksKafkaTests(ITestOutputHelper testOutputHelper, MicrocksKafkaFixture fixture)
    : IClassFixture<MicrocksKafkaFixture>, IAsyncLifetime
{
    private readonly MicrocksKafkaFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
    private ILogger<MicrocksKafkaTests> _logger = default!;

    /// <summary>
    /// Initialize the fixture before any test runs.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous initialization operation.</returns>
    public async ValueTask InitializeAsync()
    {
        await this._fixture.InitializeAsync(_testOutputHelper);
        _logger = _fixture.App.Services.GetRequiredService<ILogger<MicrocksKafkaTests>>();
    }

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
            Timeout = TimeSpan.FromSeconds(3)
        };

        var microcksClient = _fixture.App.CreateMicrocksClient(_fixture.MicrocksResource.Name);

        // Get Kafka producer from host
        var producer = host.Services.GetRequiredService<IProducer<string, string>>();

        // Start the test (this will fail initially as there are no messages being sent)
        // but it validates that the test setup and endpoint format are correct
        var taskTestResult = microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);

        // Wait a bit to let the test initialize
        await Task.Delay(750, TestContext.Current.CancellationToken);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>() })
            .Build();

        // Act
        _logger.LogInformation("Starting to send 5 messages...");
        for (var i = 0; i < 5; i++)
        {
            await pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogInformation("Sending message {Current}/{Total}", i + 1, 5);
                var deliveryResult = await producer.ProduceAsync("pastry-orders", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                }, cancellationToken);
                _logger.LogInformation("Message {Index} delivered to {Destination}", i + 1, deliveryResult.TopicPartitionOffset);
            }, TestContext.Current.CancellationToken);

            producer.Flush(TestContext.Current.CancellationToken);
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        // Wait for a test result
        _logger.LogInformation("All messages sent, waiting for test result...");
        var testResult = await taskTestResult;
        _logger.LogInformation("Test result received");

        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Test result payload:{NewLine}{Payload}", Environment.NewLine, json);

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
            Timeout = TimeSpan.FromSeconds(4)
        };

        var microcksClient = _fixture.App.CreateMicrocksClient(_fixture.MicrocksResource.Name);

        // Get Kafka producer from host
        var producer = host.Services.GetRequiredService<IProducer<string, string>>();

        // Start the test (this will fail initially as there are no messages being sent)
        // but it validates that the test setup and endpoint format are correct
        var taskTestResult = microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);

        _logger.LogInformation("Test started, waiting a bit to let it initialize...");
        // Wait a bit to let the test initialize
        await Task.Delay(750, TestContext.Current.CancellationToken);

        _logger.LogInformation("Starting to send 5 bad messages...");

        // Retry policy for producing messages
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>() })
            .Build();

        // Act
        for (var i = 0; i < 5; i++)
        {
            await pipeline.ExecuteAsync(async cancellationToken =>
            {
                _logger.LogInformation("Sending bad message {Current}/{Total}", i + 1, 5);
                var deliveryResult = await producer.ProduceAsync("pastry-orders", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                }, cancellationToken);
                _logger.LogInformation("Bad message {Index} delivered to {Destination}", i + 1, deliveryResult.TopicPartitionOffset);
            }, TestContext.Current.CancellationToken);

            _logger.LogInformation("Waiting 500ms between messages...");
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
        producer.Flush(TestContext.Current.CancellationToken);

        // Wait for a test result
        _logger.LogInformation("All messages sent, waiting for test result...");
        var testResult = await taskTestResult;
        _logger.LogInformation("Test result received");

        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Test result payload:{NewLine}{Payload}", Environment.NewLine, json);

        // Assert
        Assert.False(testResult.InProgress, "Test should have completed");
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
        Assert.True(events.Count >= 4);

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
        var host = hostBuilder.Build();

        _logger.LogInformation("Starting Kafka client host...");
        await host.StartAsync(TestContext.Current.CancellationToken);
        _logger.LogInformation("Kafka client host started");

        _logger.LogInformation("Creating required Kafka topics...");
        await CreateRequiredTopicsAsync(TestContext.Current.CancellationToken);
        _logger.LogInformation("Kafka topics creation completed");

        return host;
    }

    /// <summary>
    /// Create required Kafka topics for tests.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task CreateRequiredTopicsAsync(CancellationToken cancellationToken)
    {
        var kafkaResource = _fixture.KafkaResource;
        var kafkaConnectionStringExpression = kafkaResource.ConnectionStringExpression;
        var kafkaConnectionString = await kafkaConnectionStringExpression.GetValueAsync(cancellationToken);

        var config = new AdminClientConfig
        {
            BootstrapServers = kafkaConnectionString
        };

        using var adminClient = new AdminClientBuilder(config).Build();

        var topics = new List<string> { "pastry-orders" };
        _logger.LogInformation("Attempting to create topics: {Topics}", string.Join(", ", topics));

        try
        {
            await adminClient.CreateTopicsAsync(
                topics.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }));
            _logger.LogInformation("Topics created successfully");
        }
        catch (CreateTopicsException ex)
        {
            // Ignore if topic already exists
            if (ex.Results.Any(r => r.Error.Code != Confluent.Kafka.ErrorCode.TopicAlreadyExists))
            {
                _logger.LogError(ex, "Error creating topics");
            }
            else
            {
                _logger.LogInformation("Topics already exist - skipping creation");
            }
        }
    }

    /// <summary>
    /// Dispose resources used by the fixture.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await this._fixture.DisposeAsync();
    }
}
