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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Aspire.Hosting.Microcks.Clients.Model;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aspire.Hosting.Microcks.Clients;

/// <summary>
/// Implementation of IMicrocksClient that encapsulates HttpClient functionality
/// and provides high-level operations for Microcks contract testing.
/// </summary>
internal sealed class MicrocksClient : IMicrocksClient
{
    private readonly IMicrocksEndpoint _client;
    private readonly ILogger<MicrocksClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrocksClient"/> class.
    /// </summary>
    /// <param name="client">The Microcks client to use for HTTP operations.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <exception cref="ArgumentNullException">Thrown if client or logger is null.</exception>
    public MicrocksClient(IMicrocksEndpoint client, ILoggerFactory loggerFactory)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = loggerFactory?.CreateLogger<MicrocksClient>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public async Task<TestResult> TestEndpointAsync(TestRequest testRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(testRequest);

        TestResult testResult = await _client.TestEndpointAsync(testRequest, cancellationToken)
            .ConfigureAwait(false);

        // Handle successful creation
        _logger.LogInformation("Test request for service '{ServiceId}' completed successfully.", testRequest.ServiceId);

        var testResultId = testResult.Id;
        _logger.LogDebug("Test Result ID: {TestResultId}, new polling for progression", testResultId);

        // Polling for test result completion
        try
        {
            await WaitForConditionAsync(
                async () => !(await _client.RefreshTestResultAsync(testResultId, cancellationToken)).InProgress,
                atMost: TimeSpan.FromMilliseconds(1000).Add(testRequest.Timeout),
                delay: TimeSpan.FromMilliseconds(100),
                interval: TimeSpan.FromMilliseconds(200),
                cancellationToken);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning(
                taskCanceledException,
                "Test timeout reached, stopping polling for test {TestEndpoint}", testRequest.TestEndpoint);
        }

        return await _client.RefreshTestResultAsync(testResultId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<RequestResponsePair>> GetMessagesForTestCaseAsync(
        TestResult testResult,
        string operationName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(testResult);
        ArgumentException.ThrowIfNullOrEmpty(operationName);

        var operation = operationName.Replace('/', '!');
        var testCaseId = $"{testResult.Id}-{testResult.TestNumber}-{HttpUtility.UrlEncode(operation)}";

        return await _client.GetMessagesForTestCaseAsync(testResult.Id, testCaseId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ImportArtifactAsync(string artifactPath, bool isMainArtifact, CancellationToken cancellationToken)
    {
        const int retryCount = 5;
        var retryDelay = TimeSpan.FromMilliseconds(100);

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                await UploadArtifactAsync(artifactPath, isMainArtifact, cancellationToken);
                return;
            }
            catch (HttpRequestException ex) when (attempt < retryCount)
            {
                _logger.LogWarning(ex, "Transient error uploading artifact '{FileName}', attempt {Attempt}", Path.GetFileName(artifactPath), attempt);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task ImportSnapshotAsync(string artifactPath, CancellationToken cancellationToken)
    {
        await ImportWithRetryAsync(artifactPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ImportRemoteArtifactAsync(string remoteUrl, CancellationToken cancellationToken)
    {
        var result = await _client.DownloadArtifactAsync(true, remoteUrl, cancellationToken);
        if (result.StatusCode != HttpStatusCode.Created)
        {
            _logger.LogError("Failed to import remote artifact from '{RemoteUrl}' with status code {StatusCode}", remoteUrl, result.StatusCode);
            throw new InvalidOperationException($"Failed to import remote artifact from '{remoteUrl}'");
        }
        _logger.LogInformation("Remote artifact imported successfully from '{RemoteUrl}'", remoteUrl);
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var healthCheckResponse = await _client
                .CheckHealthAsync(cancellationToken)
                .ConfigureAwait(false);

            if (healthCheckResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Microcks service is healthy");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception during health check for Microcks service");
        }

        return false;
    }

    private async Task UploadArtifactAsync(string artifactPath, bool isMainArtifact, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(artifactPath);
        var fileName = Path.GetFileName(artifactPath);
        var content = new StreamPart(stream, fileName, "application/json");

        var result = await _client.UploadArtifactAsync(isMainArtifact, content, cancellationToken)
            .ConfigureAwait(false);

        if (result.StatusCode != HttpStatusCode.Created)
        {
            _logger.LogError("Failed to upload artifact '{FileName}' with status code {StatusCode}", fileName, result.StatusCode);
            throw new InvalidOperationException($"Failed to upload artifact '{fileName}'");
        }

        _logger.LogInformation("Artifact '{FileName}' uploaded successfully", fileName);
    }

    private async Task ImportWithRetryAsync(string artifactPath, CancellationToken cancellationToken)
    {
        const int retryCount = 5;
        var retryDelay = TimeSpan.FromMilliseconds(100);

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                await ImportArtifactAsync(artifactPath, cancellationToken);
                return;
            }
            catch (HttpRequestException ex) when (attempt < retryCount)
            {
                _logger.LogWarning(ex, "Transient error importing artifact '{FileName}', attempt {Attempt}", Path.GetFileName(artifactPath), attempt);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task ImportArtifactAsync(string artifactPath, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(artifactPath);
        var fileName = Path.GetFileName(artifactPath);
        var content = new StreamPart(stream, fileName, "application/json");

        var result = await _client.ImportArtifactAsync(content, cancellationToken)
            .ConfigureAwait(false);

        if (result.StatusCode != HttpStatusCode.Created)
        {
            _logger.LogError("Failed to import artifact '{FileName}' with status code {StatusCode}", fileName, result.StatusCode);
            throw new InvalidOperationException($"Failed to import artifact '{fileName}'");
        }

        _logger.LogInformation("Artifact '{FileName}' imported successfully", fileName);
    }

    private static async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan atMost,
        TimeSpan delay,
        TimeSpan interval,
        CancellationToken cancellationToken = default)
    {
        // Delay before first check
        await Task.Delay(delay, cancellationToken);

        // Cancel after atMost
        using var atMostCancellationToken = new CancellationTokenSource(atMost);
        // Create linked token so we can be cancelled either by caller or by timeout
        using var cancellationTokenSource = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, atMostCancellationToken.Token);

        // Polling
        while (!await condition())
        {
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            await Task.Delay(interval, cancellationTokenSource.Token);
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(string serviceName, string serviceVersion,
        DateOnly? invocationDate = null, CancellationToken cancellationToken = default)
    {
        var dailyInvocationStatistic = await this.GetServiceInvocationsAsync(
            serviceName, serviceVersion, invocationDate: invocationDate, cancellationToken: cancellationToken);
        if (dailyInvocationStatistic == null)
        {
            return false;
        }
        return dailyInvocationStatistic.DailyCount > 0;
    }

    private async Task<DailyInvocationStatistic> GetServiceInvocationsAsync(string serviceName, string serviceVersion, DateOnly? invocationDate = null, CancellationToken cancellationToken = default)
    {
        // Wait to avoid race condition issue when requesting Microcks Metrics REST API.
        await Task.Delay(100, cancellationToken);

        ApiResponse<DailyInvocationStatistic> serviceInvocationsCountResponse = await this._client.GetServiceInvocationsCountAsync(
            serviceName,
            serviceVersion,
            invocationDate,
            cancellationToken: cancellationToken);

        if (serviceInvocationsCountResponse.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogWarning("Failed to get service invocations for '{ServiceName}' with version '{ServiceVersion}'. Status code: {StatusCode}", serviceName, serviceVersion, serviceInvocationsCountResponse.StatusCode);
            return null;
        }

        // Content length > 0 pour éviter une exception
        if (serviceInvocationsCountResponse.Content == null)
        {
            _logger.LogWarning("No content received when getting service invocations for '{ServiceName}' with version '{ServiceVersion}'.", serviceName, serviceVersion);
            return null;
        }

        DailyInvocationStatistic dailyInvocationStatistic = serviceInvocationsCountResponse.Content;

        return dailyInvocationStatistic;
    }

    /// <inheritdoc />
    public async Task<double> GetServiceInvocationsCountAsync(string serviceName, string serviceVersion, DateOnly? invocationDate = null, CancellationToken cancellationToken = default)
    {
        var dailyInvocationStatistic = await this.GetServiceInvocationsAsync(serviceName, serviceVersion, invocationDate, cancellationToken);
        return dailyInvocationStatistic?.DailyCount ?? 0;
    }

    /// <summary>
    /// Retrieve event messages received during a test on an endpoint (for further investigation or checks).
    /// </summary>
    /// <param name="container">Microcks container</param>
    /// <param name="testResult">The test result to retrieve events from</param>
    /// <param name="operationName">The name of the operation to retrieve events to test result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of UnidirectionalEvent</returns>
    /// <exception cref="MicrocksException">If events have not been correctly retrieved</exception>
    /// <inheritdoc />
    public async Task<List<UnidirectionalEvent>> GetEventMessagesForTestCaseAsync(
        TestResult testResult, string operationName, CancellationToken cancellationToken = default)
    {
        var operation = operationName.Replace('/', '!');
        var testCaseId = $"{testResult.Id}-{testResult.TestNumber}-{HttpUtility.UrlEncode(operation)}";

        // Retrieve event messages for the test case
        var events = await this._client.GetEventMessagesForTestCaseAsync(
            testResult.Id,
            testCaseId,
            cancellationToken);

        return events;
    }
}
