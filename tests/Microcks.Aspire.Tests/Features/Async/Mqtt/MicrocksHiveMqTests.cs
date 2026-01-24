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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Microcks.Aspire.Tests.Fixtures.Async.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Polly;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async.Mqtt;

/// <summary>
/// Tests for the Microcks Async Minion with MQTT broker (HiveMQ) and Azure Event Grid resource builder and runtime behavior.
/// Uses a shared Microcks instance with Async Minion and HiveMQ provided by <see cref="MicrocksHiveMqFixture"/>.
/// </summary>
[Collection(MicrocksHiveMqCollection.CollectionName)]
public sealed class MicrocksHiveMqTests(MicrocksHiveMqFixture fixture)
{
    private readonly MicrocksHiveMqFixture _fixture = fixture;

    /// <summary>
    /// When an MQTT message is sent by Microcks Async Minion, then it is received.
    /// </summary>
    [Fact]
    public async Task WhenMqttMessageIsSent_ThenItIsReceived()
    {
        const string expectedMessage = "{\"id\":\"4dab240d-7847-4e25-8ef3-1530687650c8\",\"customerId\":\"fe1088b3-9f30-4dc1-a93d-7b74f0a072b9\",\"status\":\"VALIDATED\",\"productQuantities\":[{\"quantity\":2,\"pastryName\":\"Croissant\"},{\"quantity\":1,\"pastryName\":\"Millefeuille\"}]}";

        var appModel = _fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var microcksAsyncMinionResource = appModel.GetContainerResources()
            .OfType<MicrocksAsyncMinionResource>()
            .Single();

        // Get the MQTT topic for the pastry/orders subscription
        var mqttTopic = microcksAsyncMinionResource
            .GetMqttMockTopic("Pastry orders API", "0.1.0", "SUBSCRIBE pastry/orders");

        // Get HiveMQ endpoint
        var hivemqEndpoint = _fixture.HiveMqResource.Resource.GetEndpoint("mqtt");

        // Create MQTT client
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        string? receivedMessage = null;
        var messageReceivedTcs = new TaskCompletionSource<bool>();

        // Setup message received handler
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            receivedMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            // Signal that the message has been received
            messageReceivedTcs.TrySetResult(true);
            return Task.CompletedTask;
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(hivemqEndpoint.Host, hivemqEndpoint.Port)
            .WithClientId($"test-client-{Guid.NewGuid()}")
            .Build();

        // Connect with retry logic
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1) })
            .Build();

        // Execute the connection within the resilience pipeline
        await pipeline.ExecuteAsync(async cancellationToken =>
        {
            var mqttConnectResult = await mqttClient.ConnectAsync(options, cancellationToken);

            Console.WriteLine(mqttConnectResult.ResponseInformation);
        }, TestContext.Current.CancellationToken);

        // Check mqttTopic correctness
        Assert.Equal("PastryordersAPI-0.1.0-pastry/orders", mqttTopic);
        // Ensure client is connected
        Assert.True(mqttClient.IsConnected, "MQTT client should be connected");

        // Subscribe to the topic
        var subscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(mqttTopic))
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, TestContext.Current.CancellationToken);

        // Wait for message with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await messageReceivedTcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Message not received within timeout
        }

        // Disconnect
        await mqttClient.DisconnectAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(expectedMessage, receivedMessage);
    }

}
