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
using System.Threading.Tasks;
using Aspire.Hosting.Microcks.Clients.Model;
using Aspire.Microcks.Testing.Features.Mocking.Contract;
using Aspire.Microcks.Testing.Fixtures.Contract;
using Xunit;

namespace Aspire.Hosting.Microcks.Tests.Features.ContractTesting;

/// <summary>
/// Postman contract testing using Microcks in Aspire.
/// </summary>
/// <param name="fixture"></param>
[Collection(MicrocksContractValidationCollection.CollectionName)]
public sealed class PostmanContractTestingTests(MicrocksContractValidationFixture fixture)
{
    private readonly MicrocksContractValidationFixture _fixture = fixture;

    [Fact]
    public async Task WhenCallingTestEndpoint_WithBadImplementation_ShouldReturnValidationFailures()
    {
        Assert.NotNull(_fixture.MicrocksResource);
        Assert.NotNull(_fixture.App);

        var microcksResource = _fixture.MicrocksResource;
        var microcksClient = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        TestRequest badPostmanTestRequest = new()
        {
            ServiceId = "API Pastries:0.0.1",
            RunnerType = TestRunnerType.POSTMAN,
            TestEndpoint = "http://bad-impl:3001",
            Timeout = TimeSpan.FromSeconds(3)
        };

        // Call TestEndpoint from Microcks Resource
        var badTestResult = await microcksClient.TestEndpointAsync(
            badPostmanTestRequest,
            TestContext.Current.CancellationToken);

        Assert.False(badTestResult.Success);
        Assert.Equal("http://bad-impl:3001", badTestResult.TestedEndpoint);
        Assert.Equal(3, badTestResult.TestCaseResults.Count);

        // Postman runner stop at first failure so there's just 1 testStepResult per testCaseResult
        var testCaseResult = badTestResult.TestCaseResults[0];
        Assert.Single(testCaseResult.TestStepResults);

        // Order is not deterministic so it could be a matter of invalid size, invalid name or invalid price.
        var testStepResult = testCaseResult.TestStepResults[0];
        Assert.True(testStepResult.Message.Contains("Valid size in response pastries")
            || testStepResult.Message.Contains("Valid name in response pastry")
            || testStepResult.Message.Contains("Valid price in response pastry"));
    }
}
