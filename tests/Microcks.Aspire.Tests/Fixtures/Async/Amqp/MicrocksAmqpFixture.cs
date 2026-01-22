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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microcks.Aspire.Async;
using Microcks.Aspire.Testing;
using Xunit;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microcks.Aspire.Tests.Fixtures.Async.Amqp;

/// <summary>
/// Fixture that sets up a shared Microcks instance with Async Minion and RabbitMQ
/// for tests requiring AMQP messaging capabilities.
/// </summary>
public sealed class MicrocksAmqpFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the test distributed application builder.
    /// </summary>
    public IDistributedApplicationBuilder Builder { get; private set; } = default!;

    /// <summary>
    /// Gets the distributed application instance.
    /// </summary>
    public DistributedApplication App { get; private set; } = default!;

    /// <summary>
    /// Gets the Microcks resource.
    /// </summary>
    public MicrocksResource MicrocksResource { get; private set; } = default!;

    /// <summary>
    /// Gets the RabbitMQ server resource.
    /// </summary>
    public RabbitMQServerResource RabbitMQResource { get; private set; } = default!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Builder = TestDistributedApplicationBuilder.Create(o =>
        {
            o.EnableResourceLogging = true;
        });

        Builder.Services.AddLogging(logging =>
        {
            //logging.ClearProviders();
            logging.AddSimpleConsole(configure =>
            {
                configure.SingleLine = true;
            });
        });

        var username = Builder.AddParameter("username", () => "test", secret: true);
        var password = Builder.AddParameter("password", () => "test", secret: true);

        // Add RabbitMQ server with explicit credentials
        var rabbitmqBuilder = Builder.AddRabbitMQ("rabbitmq", username, password)
            .WithManagementPlugin();

        // Microcks with AsyncMinion
        var microcksBuilder = Builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature(minion =>
            {
                minion.WithAmqpConnection(rabbitmqBuilder, username, password);
            });

        App = Builder.Build();

        var asyncMinionResource = Builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        RabbitMQResource = rabbitmqBuilder.Resource;
        MicrocksResource = microcksBuilder.Resource;

        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for RabbitMQ to be ready
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            RabbitMQResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Async Minion to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            asyncMinionResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Dispose resources used by the fixture.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (App is not null)
            {
                await App.StopAsync(TestContext.Current.CancellationToken)
                    .ConfigureAwait(false);
                App.Dispose();
            }
        }
        catch
        {
            // swallow, we're tearing down tests
        }
        await App.DisposeAsync();
    }
}
