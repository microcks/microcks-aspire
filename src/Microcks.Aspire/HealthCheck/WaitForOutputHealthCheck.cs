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
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microcks.Aspire.HealthCheck;

/// <summary>
/// Health check that waits for a specific log line in the console output of a resource.
/// </summary>
/// <param name="resource">The resource to monitor.</param>
/// <param name="executionContext">The execution context providing services and scope management.</param>
/// <param name="outputSubstring">The substring to look for in the console output.</param>
public class WaitForOutputHealthCheck(IResource resource, DistributedApplicationExecutionContext executionContext, string outputSubstring) : IHealthCheck
{
    private readonly IResource _resource = resource;
    private readonly string _outputSubstring = outputSubstring;
    private readonly DistributedApplicationExecutionContext _executionContext = executionContext;

    /// <summary>
    /// Checks the health of the resource by monitoring its console output for a specific log line.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous health check operation. The task result contains the health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var scope = this._executionContext.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<WaitForOutputHealthCheck>();


        logger.LogDebug("Checking console output for {Resource}", this._resource.Name);

        var resourceLoggerService = scope.ServiceProvider.GetRequiredService<ResourceLoggerService>();

        try
        {
            // Watch the logs of the Microcks resource until we find the line "Microcks server started"
            await foreach (var batch in resourceLoggerService.WatchAsync(this._resource).WithCancellation(cancellationToken))
            {
                // Watch for the "Started MicrocksApplication" log line
                if (batch.Any(line => line.Content.Contains(this._outputSubstring, StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation while listening to logs
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking console output for {Resource}", this._resource.Name);
            return HealthCheckResult.Unhealthy($"Error while checking console output: {ex.Message}", ex);
        }
        logger.LogDebug("Console output contains expected log line for {Resource}", this._resource.Name);
        return HealthCheckResult.Healthy($"Console output contains expected log line for {this._resource.Name}.");
    }
}
