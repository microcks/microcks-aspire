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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Microcks.Async;
using Aspire.Hosting.Microcks.Clients.Model;
using Aspire.Hosting.Microcks.Tests.Fixtures.Async.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Microcks.Tests.Features.Async.Kafka;

/// <summary>
/// Tests for the Microcks Async Minion with Kafka resource builder and runtime behavior.
/// Uses a shared Microcks instance with Async Minion and Kafka provided by <see cref="MicrocksKafkaFixture"/>.
/// </summary>
[Collection(MicrocksKafkaCollection.CollectionName)]
public sealed class MicrocksKafkaTests
{
    private readonly MicrocksKafkaFixture _fixture;

    public MicrocksKafkaTests(MicrocksKafkaFixture fixture)
    {
        _fixture = fixture;
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

        // Get Kafka producer from host
        var producer = host.Services.GetRequiredService<IProducer<string, string>>();

        // Start the test (this will fail initially as there are no messages being sent)
        // but it validates that the test setup and endpoint format are correct
        var taskTestResult = microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);

        // Wait a bit to let the test initialize
        await Task.Delay(750, TestContext.Current.CancellationToken);

        // Act
        for (var i = 0; i < 5; i++)
        {
            producer.Produce("pastry-orders", new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message
            });
            producer.Flush(TestContext.Current.CancellationToken);
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        // Wait for a test result
        var testResult = await taskTestResult;

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
            Timeout = TimeSpan.FromSeconds(5)
        };

        var microcksClient = _fixture.App.CreateMicrocksClient(_fixture.MicrocksResource.Name);

        // Get Kafka producer from host
        var producer = host.Services.GetRequiredService<IProducer<string, string>>();

        // Start the test (this will fail initially as there are no messages being sent)
        // but it validates that the test setup and endpoint format are correct
        var taskTestResult = microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);

        // Wait a bit to let the test initialize
        await Task.Delay(750, TestContext.Current.CancellationToken);

        // Act
        for (var i = 0; i < 5; i++)
        {
            producer.Produce("pastry-orders", new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message
            });
            producer.Flush(TestContext.Current.CancellationToken);
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        // Wait for a test result
        var testResult = await taskTestResult;

        // Assert
        Assert.False(testResult.InProgress);
        Assert.False(testResult.Success);
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
        hostBuilder.AddKafkaProducer<string, string>("kafka");
        hostBuilder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
        {
            consumerBuilder.Config.GroupId = "aspire-consumer-group";
            consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
        var host = hostBuilder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }
}
