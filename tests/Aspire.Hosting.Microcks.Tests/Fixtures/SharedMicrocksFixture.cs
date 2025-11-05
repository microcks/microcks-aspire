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
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Microcks;
using Aspire.Hosting.Microcks.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Microcks.Testing.Fixtures;

/// <summary>
/// Shared fixture that starts a single Microcks instance for all tests in the collection.
/// Use this as a collection fixture so tests reuse the same running Microcks.
/// The fixture constructs a <see cref="TestDistributedApplicationBuilder"/>,
/// configures Microcks with the artifacts used by tests and starts the
/// distributed application once for the collection lifetime.
/// </summary>
public abstract class SharedMicrocksFixture : IAsyncLifetime, IDisposable
{
    public TestDistributedApplicationBuilder Builder { get; private set; } = default!;
    public DistributedApplication App { get; private set; } = default!;
    public MicrocksResource MicrocksResource { get; private set; } = default!;

    // Derived fixtures can override this to customize the builder (for example
    // to add additional container resources used by tests).
    protected virtual void ConfigureBuilder(TestDistributedApplicationBuilder builder)
    {
        // Default: no-op. Subclasses may add resources or adjust options.
    }

    /// <summary>
    /// Initializes the shared distributed application and starts Microcks.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        // Create builder without per-test ITestOutputHelper to avoid recreating logging per test
        Builder = TestDistributedApplicationBuilder.Create(o => { });

        // Allow derived fixtures to customize the builder before adding Microcks
        ConfigureBuilder(Builder);

        // TODO: Check to replace AppContext.BaseDirectory by other variable Builder.AppHostDirectory ?

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

        App = Builder.Build();
        await App.StartAsync(CancellationToken.None).ConfigureAwait(false);

        MicrocksResource = microcksBuilder.Resource;
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
                await App.StopAsync(CancellationToken.None).ConfigureAwait(false);
                App.Dispose();
            }
        }
        catch
        {
            // swallow, we're tearing down tests
        }

        Builder?.Dispose();
    }

    public void Dispose()
    {
        _ = DisposeAsync();
    }
}
