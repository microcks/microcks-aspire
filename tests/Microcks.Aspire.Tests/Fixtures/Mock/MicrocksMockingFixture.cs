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
using System.Threading.Tasks;
using Aspire.Hosting;
using Microcks.Aspire.Testing;
using Xunit;

namespace Microcks.Aspire.Tests.Fixtures.Mock;

/// <summary>
/// Shared fixture that starts a single Microcks instance for all tests in the collection.
/// Use this as a collection fixture so tests reuse the same running Microcks.
/// The fixture constructs a <see cref="TestDistributedApplicationBuilder"/>,
/// configures Microcks with the artifacts used by tests and starts the
/// distributed application once for the collection lifetime.
/// </summary>
public sealed class MicrocksMockingFixture : IAsyncDisposable
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
    /// Initializes the shared distributed application and starts Microcks.
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

        // Configure Microcks with the artifacts used by tests so services are available
        var microcksBuilder = Builder.AddMicrocks("microcks")
            .WithSnapshots(Path.Combine(AppContext.BaseDirectory, "resources", "microcks-repository.json"))
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "apipastries-openapi.yaml"),
                Path.Combine(AppContext.BaseDirectory, "resources", "subdir", "weather-forecast-openapi.yaml")
            )
            .WithSecondaryArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "apipastries-postman-collection.json")
            )
            .WithMainRemoteArtifacts("https://raw.githubusercontent.com/microcks/microcks/master/samples/APIPastry-openapi.yaml");

        microcksBuilder.WithHostNetworkAccess("localhost");

        App = Builder.Build();
        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        MicrocksResource = microcksBuilder.Resource;

        // Wait for Microcks to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            MicrocksResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the distributed application and disposes the builder.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (App is not null)
            {
                await App.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                App.Dispose();
            }
        }
        catch
        {
            // swallow, we're tearing down tests
        }

    }

}
