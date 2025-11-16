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
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Microcks.Async;
using Aspire.Microcks.Testing;
using Aspire.Confluent.Kafka;
using Xunit;
using Xunit.Internal;

namespace Aspire.Hosting.Microcks.Tests.Fixtures.Async.Kafka;

/// <summary>
/// Fixture that sets up a shared Microcks instance with Async Minion and Kafka
/// for tests requiring Kafka messaging capabilities.
/// </summary>
public sealed class MicrocksKafkaFixture : IAsyncLifetime
{
    public TestDistributedApplicationBuilder Builder { get; private set; } = default!;
    public DistributedApplication App { get; private set; } = default!;
    public MicrocksResource MicrocksResource { get; private set; } = default!;
    public KafkaServerResource KafkaResource { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        // Create builder without per-test ITestOutputHelper to avoid recreating logging per test
        Builder = TestDistributedApplicationBuilder.Create(o => { });

        // Add Kafka server
        var kafkaBuilder = Builder.AddKafka("kafka")
            .WithKafkaUI();

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

        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Kafka to be ready
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            kafkaBuilder.Resource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Async Minion to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            asyncMinionResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        KafkaResource = kafkaBuilder.Resource;
        MicrocksResource = microcksBuilder.Resource;
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
