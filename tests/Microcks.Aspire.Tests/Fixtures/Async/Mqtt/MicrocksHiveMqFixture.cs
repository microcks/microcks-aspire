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
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Microcks.Aspire.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microcks.Aspire.Tests.Fixtures.Async.Mqtt;

/// <summary>
/// Fixture that sets up a shared Microcks instance with Async Minion and HiveMQ
/// for tests requiring MQTT messaging capabilities.
/// </summary>
public sealed class MicrocksHiveMqFixture : IAsyncLifetime
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
    /// Gets the HiveMQ MQTT broker resource.
    /// </summary>
    public IResourceBuilder<ContainerResource> HiveMqResource { get; private set; } = default!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Builder = TestDistributedApplicationBuilder.Create(o =>
        {
            o.EnableResourceLogging = true;
        });

        Builder.Services.AddLogging(logging =>
        {
            logging.AddSimpleConsole(configure =>
            {
                configure.SingleLine = true;
            });
        });

        // Add HiveMQ MQTT broker (no authentication by default in CE version)
        var hivemqBuilder = Builder.AddContainer("hivemq", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        // Microcks with AsyncMinion configured for HiveMQ without authentication
        var microcksBuilder = Builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(hivemqBuilder, port: 1883);
            });

        App = Builder.Build();

        var asyncMinionResource = Builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        HiveMqResource = hivemqBuilder;
        MicrocksResource = microcksBuilder.Resource;

        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for HiveMQ to be ready
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            HiveMqResource.Resource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Async Minion to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            asyncMinionResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (App != null)
        {
            await App.StopAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
            await App.DisposeAsync()
                .ConfigureAwait(false);
        }
    }
}
