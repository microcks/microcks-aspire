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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microcks.Aspire.Tests.Fixtures.Secrets;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Secrets;

/// <summary>
/// Integration tests for verifying secrets are properly created in Microcks API.
/// </summary>
public class MicrocksSecretsIntegrationTests(ITestOutputHelper testOutputHelper, MicrocksSecretsFixture fixture)
    : IClassFixture<MicrocksSecretsFixture>, IAsyncLifetime
{
    private readonly MicrocksSecretsFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Initialize the fixture before any test runs.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync(_testOutputHelper);
    }

    /// <summary>
    /// Cleanup after tests.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Verifies that secrets configured via WithSecrets are available in Microcks API.
    /// </summary>
    [Fact]
    public async Task WithSecrets_ShouldCreateSecretsInMicrocksApi()
    {
        // Arrange
        var microcks = _fixture.MicrocksResource;
        var endpoint = microcks.GetEndpoint();
        var uriBuilder = new UriBuilder(endpoint.Url)
        {
            Path = "api/secrets"
        };
        
        // Act
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(uriBuilder.Uri, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        _testOutputHelper.WriteLine($"Secrets API response: {content}");
        
        var document = JsonDocument.Parse(content);
        var secrets = document.RootElement;
        
        Assert.True(secrets.GetArrayLength() >= 1, $"Expected at least 1 secret, got {secrets.GetArrayLength()}");

        var secret = secrets.EnumerateArray().First();
        var name = secret.GetProperty("name").GetString();

        Assert.Equal("my-secret", name);
        Assert.Equal("abc-123-xyz", secret.GetProperty("token").GetString());
        Assert.Equal("x-microcks", secret.GetProperty("tokenHeader").GetString());
    }
}
