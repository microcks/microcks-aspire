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
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Microcks.Aspire.Tests.Fixtures.Async.Amqp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Aspire.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Microcks.Aspire.Tests.Features.Async.Amqp;

/// <summary>
/// Tests for the Microcks Async Minion with RabbitMQ (AMQP) resource builder and runtime behavior.
/// Uses a shared Microcks instance with Async Minion and RabbitMQ provided by <see cref="MicrocksAmqpFixture"/>.
/// </summary>
[Collection(MicrocksAmqpCollection.CollectionName)]
public sealed class MicrocksAmqpTests(MicrocksAmqpFixture fixture)
{
    private readonly MicrocksAmqpFixture _fixture = fixture;

    /// <summary>
    /// When an AMQP message is sent by Microcks Async Minion, then it is received.
    /// </summary>
    [Fact]
    public async Task WhenAmqpMessageIsSend_ThenItIsReceived()
    {
        using var host = await CreateRabbitMQClientHostAsync();
        const string expectedMessage = "{\"id\":\"4dab240d-7847-4e25-8ef3-1530687650c8\",\"customerId\":\"fe1088b3-9f30-4dc1-a93d-7b74f0a072b9\",\"status\":\"VALIDATED\",\"productQuantities\":[{\"quantity\":2,\"pastryName\":\"Croissant\"},{\"quantity\":1,\"pastryName\":\"Millefeuille\"}]}";

        const string amqpDestination = "PastryordersAPI-0.1.0-pastry/orders";
        var appModel = _fixture.App.Services
            .GetRequiredService<DistributedApplicationModel>();

        // Retrieve MicrocksAsyncMinionResource from application
        var microcksAsyncMinionResource = appModel.GetContainerResources()
            .OfType<MicrocksAsyncMinionResource>()
            .Single();

        // Get RabbitMQ connection from Aspire host
        var connection = host.Services.GetRequiredService<IConnection>();
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Declare the exchange (topic type as per AsyncAPI spec)
        await channel.ExchangeDeclareAsync(amqpDestination, "topic", false, cancellationToken: TestContext.Current.CancellationToken);

        // Create a temporary queue and bind it to the exchange
        var queueDeclareResult = await channel.QueueDeclareAsync(cancellationToken: TestContext.Current.CancellationToken);
        var queueName = queueDeclareResult.QueueName;
        await channel.QueueBindAsync(queueName, amqpDestination, "#", cancellationToken: TestContext.Current.CancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        var messageReceived = new ManualResetEventSlim(false);
        string receivedMessage = null;

        consumer.ReceivedAsync += async (model, ea) =>
        {
            receivedMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: TestContext.Current.CancellationToken);
            messageReceived.Set();
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: TestContext.Current.CancellationToken);

        // Wait for the message (timeout 10s to account for Microcks async minion startup)
        var messageReceivedInTime = messageReceived.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(messageReceivedInTime, "Message should have been received within timeout");
        Assert.NotNull(receivedMessage);
        Assert.True(receivedMessage.Length > 1);
        Assert.Equal(expectedMessage, receivedMessage);
    }

    /// <summary>
    /// Creates a host with RabbitMQ client configured to connect to the RabbitMQ resource in the fixture.
    /// </summary>
    private async Task<IHost> CreateRabbitMQClientHostAsync()
    {
        var hostBuilder = Host.CreateApplicationBuilder();

        var rabbitmqResource = _fixture.RabbitMQResource;
        var rabbitmqConnectionString = await rabbitmqResource.ConnectionStringExpression
            .GetValueAsync(TestContext.Current.CancellationToken);

        // Assign connection string to configuration
        hostBuilder.Configuration[$"ConnectionStrings:{rabbitmqResource.Name}"]
            = rabbitmqConnectionString;

        // Add RabbitMQ client (rabbitmq is the name of the resource in the fixture)
        hostBuilder.AddRabbitMQClient(rabbitmqResource.Name,
            configureConnectionFactory:
            static factory => factory.ClientProvidedName = "xunit-test-client");

        var host = hostBuilder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }
}
