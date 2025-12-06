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
using Microsoft.Extensions.DependencyInjection;

namespace Microcks.Aspire.Tests.Fixtures.Async.Kafka;

/// <summary>
/// Fixture that sets up a shared Microcks instance with Async Minion and Kafka
/// for tests requiring Kafka messaging capabilities.
/// </summary>
public sealed class MicrocksKafkaFixture : IAsyncDisposable
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
    /// Gets the Kafka server resource.
    /// </summary>
    public KafkaServerResource KafkaResource { get; private set; } = default!;

    /// <summary>
    /// Initializes the shared distributed application with Microcks, Async Minion and Kafka.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper for logging.</param>
    /// <returns>ValueTask representing the asynchronous initialization operation.</returns>
    public async ValueTask InitializeAsync(ITestOutputHelper testOutputHelper)
    {
        Builder = TestDistributedApplicationBuilder.Create(o =>
        {
            o.EnableResourceLogging = true;
        })
        .WithTestAndResourceLogging(testOutputHelper);

        // Add Kafka server
        var kafkaBuilder = Builder.AddKafka("kafka");

        // Microcks with AsyncMinion
        var microcksBuilder = Builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature(minion =>
            {
                minion.WithKafkaConnection(kafkaBuilder);
            });

        App = Builder.Build();

        var asyncMinionResource = Builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        KafkaResource = kafkaBuilder.Resource;
        MicrocksResource = microcksBuilder.Resource;

        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Kafka to be ready
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            KafkaResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Async Minion to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            asyncMinionResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Dispose resources used by the fixture.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous dispose operation.</returns>
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
