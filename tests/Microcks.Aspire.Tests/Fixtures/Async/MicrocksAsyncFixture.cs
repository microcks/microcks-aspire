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
using Xunit;

namespace Microcks.Aspire.Tests.Fixtures.Async;

/// <summary>
/// Fixture that sets up a shared Microcks instance with Async Minion
/// for tests requiring asynchronous messaging capabilities.
/// </summary>
public sealed class MicrocksAsyncFixture : IAsyncDisposable
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

    private const string BadPastryAsyncImage = "quay.io/microcks/contract-testing-demo-async:01";
    private const string GoodPastryAsyncImage = "quay.io/microcks/contract-testing-demo-async:02";

    /// <summary>
    /// Initializes the shared distributed application with Microcks and Async Minion.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper for logging.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask InitializeAsync(ITestOutputHelper testOutputHelper)
    {
        Builder = TestDistributedApplicationBuilder.Create(o =>
        {
            o.EnableResourceLogging = true;
        })
        .WithTestAndResourceLogging(testOutputHelper);

        // Good
        var wsGoodImplResource = new ContainerResource("good-contract-async");
        var wsGoodImplBuilder = Builder.AddResource(wsGoodImplResource)
            .WaitForConsoleOutput("Starting WebSocket server on ws://localhost:4002/websocket")
            .WithImage(GoodPastryAsyncImage);

        // Bad
        var wsBadImplResource = new ContainerResource("bad-contract-async");
        var wsBadImplBuilder = Builder.AddResource(wsBadImplResource)
            .WaitForConsoleOutput("Starting WebSocket server on ws://localhost:4001/websocket")
            .WithImage(BadPastryAsyncImage);

        // Microcks with AsyncMinion
        var microcksBuilder = Builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature();

        App = Builder.Build();

        var asyncMinionResource = Builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();

        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Wait for Async Minion to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            asyncMinionResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
        // Wait for implementations to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            "good-contract-async", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            "bad-contract-async", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        MicrocksResource = microcksBuilder.Resource;
    }

    /// <summary>
    /// Dispose resources used by the fixture.
    /// </summary>
    /// <returns></returns>.
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
