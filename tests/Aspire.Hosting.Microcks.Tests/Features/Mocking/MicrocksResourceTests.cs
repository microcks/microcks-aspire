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

using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

using Aspire.Hosting.Testing;
using Aspire.Microcks.Testing.Fixtures.Mock;
using Aspire.Hosting.Microcks.Clients;
using Aspire.Microcks.Testing.Fixtures;

namespace Aspire.Hosting.Microcks.Tests.Features.Mocking;

/// <summary>
/// Tests for the Microcks resource builder and runtime behavior.
/// Uses a shared Microcks instance provided by <see cref="MicrocksMockingFixture"/>.
/// </summary>
[Collection(MicrocksMockingCollection.CollectionName)]
public class MicrocksResourceTests(MicrocksMockingFixture fixture)
{
    private readonly MicrocksMockingFixture _fixture = fixture;

    /// <summary>
    /// Builds a test distributed application with Microcks and ensures that
    /// the mock endpoints helpers return the expected URIs for SOAP, REST,
    /// GraphQL and gRPC mocks when the application is started.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AddMicrocks_ShouldConfigureMockEndpoints()
    {
        // Use the shared Microcks instance started by the collection fixture
        var microcks = _fixture.MicrocksResource;

        await Task.CompletedTask; // placeholder to keep async signature; resource already started by fixture

        Assert.NotNull(microcks);

        var endpoint = microcks.GetEndpoint();
        Assert.NotNull(endpoint);

        Uri baseSoapEndpoint = microcks.GetSoapMockEndpoint("Pastries Service", "1.0");
        Assert.Equal($"{endpoint.Url}/soap/Pastries Service/1.0", baseSoapEndpoint.ToString());

        Uri baseRestEndpoint = microcks.GetRestMockEndpoint("Pastries Service", "0.0.1");
        Assert.Equal($"{endpoint.Url}/rest/Pastries Service/0.0.1", baseRestEndpoint.ToString());

        Uri baseGraphQLEndpoint = microcks.GetGraphQLMockEndpoint("Pastries Graph", "1");
        Assert.Equal($"{endpoint.Url}/graphql/Pastries Graph/1", baseGraphQLEndpoint.ToString());

        Uri baseGrpcEndpoint = microcks.GetGrpcMockEndpoint();

        var uriBuilder = new UriBuilder(endpoint.Url)
        {
            Scheme = "grpc"
        };
        Assert.Equal(uriBuilder.Uri, baseGrpcEndpoint);
    }

    /// <summary>
    /// Verifies that when snapshots and artifacts are provided to the Microcks
    /// builder, the running Microcks instance exposes the expected services
    /// list via its <c>/api/services</c> endpoint.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AddMicrocks_WithArtifacts_ShouldAvailableServices()
    {
        // Use the shared Microcks instance started by the collection fixture
        var microcks = _fixture.MicrocksResource;
        var app = _fixture.App;

        Assert.NotNull(microcks);

        var endpoint = microcks.GetEndpoint();
        Assert.NotNull(endpoint);
        var uriBuilder = new UriBuilder(endpoint.Url)
        {
            Path = "api/services"
        };

        // Call uri to get the list of services using the shared app's http client
        using var httpClient = app.CreateHttpClient(microcks.Name);
        var response = await httpClient.GetAsync(uriBuilder.Uri, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var servicesJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(servicesJson);

        Assert.Equal(7, jsonDoc.RootElement.GetArrayLength());

        var expectedNames = new[]
        {
            "Petstore API",
            "HelloService Mock",
            "io.github.microcks.grpc.hello.v1.HelloService",
            "Movie Graph API",
            "API Pastry - 2.0",
            "API Pastries",
            "WeatherForecast API"
        };

        // Extract the names from the JSON
        var actualNames = jsonDoc.RootElement
            .EnumerateArray()
            .Select(e => e.GetProperty("name").GetString())
            .ToList();
        // Assert that all expected names are present in the actual names
        foreach (var expectedName in expectedNames)
        {
            Assert.Contains(actualNames, name => name == expectedName);
        }
    }

    /// <summary>
    /// Verifies that a given REST mock path returns the expected JSON payload.
    /// Uses inline data to test multiple endpoints/paths.
    /// </summary>
    [Theory]
    [InlineData("API Pastries", "0.0.1", "pastries/Millefeuille", "Millefeuille")]
    [InlineData("API Pastries", "0.0.1", "pastries/Eclair Chocolat", "Eclair Chocolat")]
    [InlineData("API Pastry - 2.0", "2.0.0", "pastry/Millefeuille", "Millefeuille")]
    public async Task WhenCallingRestMocks_ShouldReturnExpectedNameInPayload(
        string serviceName, string serviceVersion, string relativePath, string expectedName)
    {
        // Arrange
        MicrocksResource microcksResource = _fixture.MicrocksResource;
        IMicrocksClient microcksClient = _fixture.App.CreateMicrocksClient(microcksResource.Name);
        var app = _fixture.App;

        Assert.NotNull(microcksResource);
        Assert.NotNull(app);

        using var microcksHttpClient = app.CreateHttpClient(microcksResource.Name);

        // Get the service endpoint
        var serviceEndpoint = microcksResource.GetRestMockEndpoint(serviceName, serviceVersion);

        // Act
        // Call verify endpoint before mock invocation
        bool isVerifiedBefore = await microcksClient
                .VerifyAsync(serviceName, serviceVersion, cancellationToken: TestContext.Current.CancellationToken);

        // Get Invocation count before mock invocation
        double invocationCountBefore = await microcksClient
                .GetServiceInvocationsCountAsync(serviceName, serviceVersion, cancellationToken: TestContext.Current.CancellationToken);

        // Assert that the invocation count matches the verification status
        Assert.Equal(isVerifiedBefore, invocationCountBefore > 0);

        // Call the endpoint provided by Microcks
        var response = await microcksHttpClient.GetAsync($"{serviceEndpoint}/{relativePath}",
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = System.Text.Json.JsonDocument.Parse(responseBody);

        // Assert
        Assert.Equal(expectedName, json.RootElement.GetProperty("name").GetString());

        // Verify that the service is now verified
        bool isVerifiedAfter = await microcksClient
                .VerifyAsync(serviceName, serviceVersion, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(isVerifiedAfter, "Service should be verified after mock invocation");

        // Get Invocation count after mock invocation
        double invocationCountAfter = await microcksClient
                .GetServiceInvocationsCountAsync(serviceName, serviceVersion, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(invocationCountBefore + 1, invocationCountAfter);
    }

}
