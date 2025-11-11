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

using Xunit;

using Aspire.Microcks.Testing.Fixtures.Contract;
using Aspire.Hosting.Microcks.Clients.Model;
using Aspire.Microcks.Testing.Features.Mocking.Contract;

using System.Collections.Generic;
using Aspire.Hosting.Testing;

namespace Aspire.Hosting.Microcks.Tests.Features.ContractTesting;

/// <summary>
/// Aspire-based rewrite of the contract-testing demo that previously used Testcontainers.
/// This test starts Microcks and two container resources (bad/good implementations) in a single
/// distributed application and performs basic verification: the Microcks services list is available
/// and the demo implementations expose HTTP endpoints.
/// </summary>
[Collection(MicrocksContractValidationCollection.CollectionName)]
public sealed class MicrocksContractTestingTests(MicrocksContractValidationFixture fixture)
{
    private readonly MicrocksContractValidationFixture _fixture = fixture;

    /// <summary>
    /// Tests calling the TestEndpoint API of Microcks with the bad implementation,
    /// expecting validation failures.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhenCallingTestEndpoint_WithBadImplementation_ShouldReturnValidationFailures()
    {
        Assert.NotNull(_fixture.MicrocksResource);
        Assert.NotNull(_fixture.App);

        var microcksResource = _fixture.MicrocksResource;
        var microcksClient = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        TestRequest badTestRequest = new()
        {
            ServiceId = "API Pastries:0.0.1",
            RunnerType = TestRunnerType.OPEN_API_SCHEMA,
            TestEndpoint = "http://bad-impl:3001",
            Timeout = TimeSpan.FromMilliseconds(2000)
        };
        // Call TestEndpoint from Microcks Resource
        var badTestResult = await microcksClient.TestEndpointAsync(
            badTestRequest,
            TestContext.Current.CancellationToken);

        Assert.False(badTestResult.Success);
        Assert.Equal("http://bad-impl:3001", badTestRequest.TestEndpoint);
        Assert.Equal(3, badTestResult.TestCaseResults.Count);
        Assert.Contains("string found, number expected",
        badTestResult.TestCaseResults[0].TestStepResults[0].Message);

        List<RequestResponsePair> messages = await microcksClient.GetMessagesForTestCaseAsync(
            badTestResult,
            "GET /pastries",
            TestContext.Current.CancellationToken);

        Assert.Equal(3, messages.Count);
        Assert.All(messages, message =>
        {
            Assert.NotNull(message.Request);
            Assert.NotNull(message.Response);
            Assert.NotNull(message.Response.Content);
            // Check these are the correct requests.
            Assert.NotNull(message.Request.QueryParameters);
            Assert.Single(message.Request.QueryParameters);
            Assert.Equal("size", message.Request.QueryParameters[0].Name);
        });
    }


    /// <summary>
    /// Tests calling the TestEndpoint API of Microcks with the good implementation,
    /// expecting validation success.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhenCallingTestEndpoint_WithGoodImplementation_ShouldReturnValidationSuccess()
    {
        Assert.NotNull(_fixture.MicrocksResource);
        Assert.NotNull(_fixture.App);

        var microcksResource = _fixture.MicrocksResource;
        var microcksClient = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        TestRequest goodTestRequest = new()
        {
            ServiceId = "API Pastries:0.0.1",
            RunnerType = TestRunnerType.OPEN_API_SCHEMA,
            TestEndpoint = "http://good-impl:3002",
            Timeout = TimeSpan.FromMilliseconds(2000)
        };
        // Call TestEndpoint from Microcks Resource
        var goodTestResult = await microcksClient.TestEndpointAsync(
            goodTestRequest,
            TestContext.Current.CancellationToken);

        Assert.True(goodTestResult.Success);
        Assert.Equal("http://good-impl:3002", goodTestRequest.TestEndpoint);
        Assert.Equal(3, goodTestResult.TestCaseResults.Count);
        Assert.Empty(goodTestResult.TestCaseResults[0].TestStepResults[0].Message);
    }

    /// <summary>
    /// Tests calling the TestEndpoint API of Microcks with custom headers,
    /// expecting the headers to be reflected in the test result.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhenCallingTestEndpoint_WithHeader_ShouldReturnHeaderInTestResult()
    {
        Assert.NotNull(_fixture.MicrocksResource);
        Assert.NotNull(_fixture.App);

        var microcksResource = _fixture.MicrocksResource;

        var microcksProvider = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        TestRequest headerTestRequest = new()
        {
            ServiceId = "API Pastries:0.0.1",
            RunnerType = TestRunnerType.OPEN_API_SCHEMA,
            TestEndpoint = "http://good-impl:3002",
            Timeout = TimeSpan.FromMilliseconds(2000),
            OperationsHeaders = new Dictionary<string, List<Header>>
            {
                {
                    "GET /pastries",
                    new List<Header> {
                        new() {
                            Name = "X-Custom-Header-1",
                            Values = "value1,value2,value3"
                        }
                    }
                }
            }
        };

        // Call TestEndpoint from Microcks Resource
        var testResult = await microcksProvider.TestEndpointAsync(
            headerTestRequest,
            TestContext.Current.CancellationToken);

        Assert.True(testResult.Success);
        Assert.Equal("http://good-impl:3002", headerTestRequest.TestEndpoint);
        Assert.Equal(3, testResult.TestCaseResults.Count);
        Assert.Empty(testResult.TestCaseResults[0].TestStepResults[0].Message);

        // Verify that the header is present in the test result
        Assert.Single(testResult.OperationsHeaders);
        Assert.True(testResult.OperationsHeaders.ContainsKey("GET /pastries"));
        var headers = testResult.OperationsHeaders["GET /pastries"];
        Assert.Single(headers);
        Assert.Equal("X-Custom-Header-1", headers[0].Name);

        var headerValues = headers[0].Values.Split(',');
        var expectedValues = new List<string> { "value1", "value2", "value3" };
        Assert.Equivalent(expectedValues, headerValues);
    }


    /// <summary>
    /// Tests calling the TestEndpoint API of Microcks with host.docker.internal to resolve the good implementation,
    /// expecting validation success.
    /// 
    /// Prevent DNS resolution issues from containers (podman, docker desktop, etc.)
    /// with a methods available in Aspire Microcks extension WithHostNetworkAccess.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhenCallingTestEndpoint_WithHostNetworkAccess_ShouldReturnValidResult()
    {
        Assert.NotNull(_fixture.MicrocksResource);
        Assert.NotNull(_fixture.App);

        var microcksResource = _fixture.MicrocksResource;

        var microcksProvider = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        var goodImplEndpoint = _fixture.App.GetEndpoint("good-impl");

        TestRequest withEndpointTestRequest = new()
        {
            ServiceId = "API Pastries:0.0.1",
            RunnerType = TestRunnerType.OPEN_API_SCHEMA,
            TestEndpoint = $"http://host.alias.testing:{goodImplEndpoint.Port}",
            Timeout = TimeSpan.FromMilliseconds(2000),
        };

        // Call TestEndpoint from Microcks Resource
        var testResult = await microcksProvider.TestEndpointAsync(
            withEndpointTestRequest,
            TestContext.Current.CancellationToken);

        Assert.True(testResult.Success);
        Assert.Equal($"http://host.alias.testing:{goodImplEndpoint.Port}", withEndpointTestRequest.TestEndpoint);
        Assert.Equal(3, testResult.TestCaseResults.Count);
        Assert.Empty(testResult.TestCaseResults[0].TestStepResults[0].Message);
    }
}

