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
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using Microcks.Aspire.Async;
using Microcks.Aspire.Clients.Model;
using Microcks.Aspire.Tests.Fixtures.Async;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async;

/// <summary>
/// Tests for the Microcks Async Minion resource builder and runtime behavior.
/// Uses a shared Microcks instance with Async Minion provided by <see cref="MicrocksAsyncFixture"/>.
/// </summary>
public sealed class MicrocksAsyncTests(ITestOutputHelper testOutputHelper, MicrocksAsyncFixture fixture)
    : IClassFixture<MicrocksAsyncFixture>, IAsyncLifetime
{
    private MicrocksAsyncFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Initialize the fixture before any test runs.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous initialization operation.</returns>
    public async ValueTask InitializeAsync()
    {
        await this._fixture.InitializeAsync(_testOutputHelper);
    }

    /// <summary>
    /// When the application is started, then the MicrocksAsyncMinionResource is available.
    /// </summary>
    [Fact]
    public void WhenApplicationIsStarted_ThenMicrocksAsyncResourceIsAvailable()
    {
        Assert.NotNull(_fixture.App);
        Assert.NotNull(_fixture.MicrocksResource);

        var app = _fixture.App;
        var name = _fixture.MicrocksResource.Name;
        string expectedName = $"{name}-async-minion";

        // Retrieve Resource from application (MicrocksAsyncMinion)
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var microcksAsyncMinionResources = appModel.Resources.OfType<MicrocksAsyncMinionResource>();
        MicrocksAsyncMinionResource asyncMinionResource = Assert.Single(microcksAsyncMinionResources);

        Assert.Equal(expectedName, asyncMinionResource.Name);
    }

    /// <summary>
    /// When a WebSocket message is sent to the Microcks Async Minion, then it is received.
    /// </summary>
    [Fact]
    public async Task WhenWebSocketMessageIsSend_ThenItIsReceived()
    {
        Assert.NotNull(_fixture.App);
        Assert.NotNull(_fixture.MicrocksResource);

        const string expectedMessage = "{\"id\":\"4dab240d-7847-4e25-8ef3-1530687650c8\",\"customerId\":\"fe1088b3-9f30-4dc1-a93d-7b74f0a072b9\",\"status\":\"VALIDATED\",\"productQuantities\":[{\"quantity\":2,\"pastryName\":\"Croissant\"},{\"quantity\":1,\"pastryName\":\"Millefeuille\"}]}";
        var app = _fixture.App;
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var microcksAsyncMinionResources = appModel.Resources.OfType<MicrocksAsyncMinionResource>();
        MicrocksAsyncMinionResource asyncMinionResource = Assert.Single(microcksAsyncMinionResources);

        // Get the WebSocket endpoint for the "Pastry orders API" with version "0.1.0" and subscription "SUBSCRIBE pastry/orders".
        var webSocketEndpoint = asyncMinionResource
            .GetWebSocketMockEndpoint("Pastry orders API", "0.1.0", "SUBSCRIBE pastry/orders");

        using var webSocketClient = new ClientWebSocket();
        await webSocketClient.ConnectAsync(webSocketEndpoint, TestContext.Current.CancellationToken);

        var buffer = new byte[1024];

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(7));
        var result = await webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

        await webSocketClient.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Test done",
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedMessage, message);
    }

    /// <summary>
    /// When a bad WebSocket message is sent to the Microcks Async Minion, then the correct contract status is returned.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhenBadWebSocketMessageIsSend_ThenCorrectStatusContractIsReturned()
    {
        Assert.NotNull(_fixture.App);
        Assert.NotNull(_fixture.MicrocksResource);

        // New Test request
        TestRequest testRequest = new()
        {
            ServiceId = "Pastry orders API:0.1.0",
            RunnerType = TestRunnerType.ASYNC_API_SCHEMA,
            Timeout = TimeSpan.FromMilliseconds(7000),
            TestEndpoint = "ws://bad-contract-async:4001/websocket",
        };

        var microcksResource = _fixture.MicrocksResource;
        var microcksClient = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var testResult = await microcksClient.TestEndpointAsync(
            testRequest,
            TestContext.Current.CancellationToken);
        stopwatch.Stop();

        // Assert 
        Assert.False(testResult.InProgress, "Test should not be in progress");
        Assert.True(stopwatch.ElapsedMilliseconds > 7000, "Test should have been stopped by timeout");
        Assert.False(testResult.Success, "Test result should not be successful");
        Assert.Equal(testRequest.TestEndpoint, testResult.TestedEndpoint);

        Assert.NotEmpty(testResult.TestCaseResults.First().TestStepResults);
        var testCaseResult = testResult.TestCaseResults.First();
        var testStepResults = testCaseResult.TestStepResults;

        var testStepResult = testStepResults.First();
        Assert.Contains("required property 'status' not found", testStepResult.Message);
    }

    /// <summary>
    /// When a good WebSocket message is sent to the Microcks Async Minion, then the correct contract status is returned.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhenGoodWebSocketMessageIsSend_ThenCorrectStatusContractIsReturned()
    {
        Assert.NotNull(_fixture.App);
        Assert.NotNull(_fixture.MicrocksResource);

        // Small delay to ensure all imports, connections, etc. are settled
        // await Task.Delay(400, TestContext.Current.CancellationToken)
        //     .ConfigureAwait(true);
        // New Test request
        TestRequest testRequest = new()
        {
            ServiceId = "Pastry orders API:0.1.0",
            RunnerType = TestRunnerType.ASYNC_API_SCHEMA,
            Timeout = TimeSpan.FromMilliseconds(5000),
            TestEndpoint = "ws://good-contract-async:4002/websocket",
        };
        var microcksResource = _fixture.MicrocksResource;
        var microcksClient = _fixture.App.CreateMicrocksClient(microcksResource.Name);

        var testResult = await microcksClient.TestEndpointAsync(
            testRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(testResult.InProgress, "Test should not be in progress");
        Assert.True(testResult.Success, "Test result should be successful");
        Assert.Equal(testRequest.TestEndpoint, testResult.TestedEndpoint);

        Assert.NotEmpty(testResult.TestCaseResults.First().TestStepResults);
        Assert.True(string.IsNullOrEmpty(testResult.TestCaseResults.First().TestStepResults.First().Message));
    }

    /// <summary>
    /// Dispose resources used by the fixture.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await this._fixture.DisposeAsync();
    }
}
