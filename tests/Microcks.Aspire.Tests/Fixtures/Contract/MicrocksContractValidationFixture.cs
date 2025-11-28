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
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Testing;
using Xunit;

namespace Microcks.Aspire.Tests.Fixtures.Contract;

/// <summary>
/// Fixture that adds two container resources (bad/good implementations)
/// to the shared distributed application builder before Microcks is configured.
/// </summary>
public sealed class MicrocksContractValidationFixture : IAsyncLifetime, IDisposable
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

    private const string BAD_PASTRY_IMAGE = "quay.io/microcks/contract-testing-demo:01";
    private const string GOOD_PASTRY_IMAGE = "quay.io/microcks/contract-testing-demo:02";

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Create builder without per-test ITestOutputHelper to avoid recreating logging per test
        Builder = TestDistributedApplicationBuilder.Create(o => { });

        // Configure Microcks with the artifacts used by tests so services are available
        var microcksBuilder = Builder.AddMicrocks("microcks")
            .WithPostmanRunner()
            .WithSnapshots(Path.Combine(AppContext.BaseDirectory, "resources", "microcks-repository.json"))
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "apipastries-openapi.yaml"),
                Path.Combine(AppContext.BaseDirectory, "resources", "subdir", "weather-forecast-openapi.yaml")
            )
            .WithSecondaryArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "apipastries-postman-collection.json")
            )
            .WithMainRemoteArtifacts("https://raw.githubusercontent.com/microcks/microcks/master/samples/APIPastry-openapi.yaml");

        // Add bad implementation container
        var badImpl = new ContainerResource("bad-impl");
        Builder.AddResource(badImpl)
            .WithImage(BAD_PASTRY_IMAGE)
            .WaitForConsoleOutput("Example app listening on port 3001")
            .WithHttpEndpoint(targetPort: 3001, name: "http")
            .WithReferenceRelationship(microcksBuilder.Resource);

        // Add good implementation container
        var goodImpl = new ContainerResource("good-impl");
        Builder.AddResource(goodImpl)
            .WithImage(GOOD_PASTRY_IMAGE)
            .WaitForConsoleOutput("Example app listening on port 3002")
            .WithHttpEndpoint(targetPort: 3002, name: "http")
            .WithReferenceRelationship(microcksBuilder.Resource);

        App = Builder.Build();
        await App.StartAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        MicrocksResource = microcksBuilder.Resource;

        // Wait for Microcks to be ready before proceeding with tests
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            MicrocksResource.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            badImpl.Name, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            goodImpl.Name, cancellationToken: TestContext.Current.CancellationToken)
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
                await App.StopAsync(TestContext.Current.CancellationToken)
                    .ConfigureAwait(false);
                App.Dispose();
            }
        }
        catch
        {
            // swallow, we're tearing down tests
        }
    }

    /// <summary>
    /// Disposes of the fixture resources.
    /// </summary>
    public void Dispose()
    {
        _ = DisposeAsync();
    }

}
