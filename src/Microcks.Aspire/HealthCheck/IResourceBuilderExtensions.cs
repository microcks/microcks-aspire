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
using System.Threading;
using Microcks.Aspire.HealthCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting.ApplicationModel;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extension methods for <see cref="IResourceBuilder{T}"/> to add health checks.
/// </summary>
public static class IResourceBuilderExtensions
{
    /// <summary>
    /// Adds a health check that waits for specific console output from the container resource.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The resource builder for the container resource.</param>
    /// <param name="outputSubstring">Output substring to wait for in the container's console output.</param>
    /// <param name="timeout">Timeout duration to wait for the output substring. Defaults to 30 seconds if not specified.</param>
    /// <returns>The same <see cref="IResourceBuilder{T}"/> instance for chaining.</returns>
    public static IResourceBuilder<T> WaitForConsoleOutput<T>(this IResourceBuilder<T> builder, string outputSubstring, TimeSpan? timeout = null)
        where T : ContainerResource
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentException.ThrowIfNullOrEmpty(outputSubstring, nameof(outputSubstring));

        var applicationBuilder = builder.ApplicationBuilder;
        string healthCheckName = $"wait-for-output-health-check-{builder.Resource.Name}";
        //
        var healthCheck = new WaitForOutputHealthCheck(builder.Resource, applicationBuilder.ExecutionContext, outputSubstring);
        applicationBuilder.Services.AddHealthChecks().AddAsyncCheck(healthCheckName, async ct =>
        {
            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cancellationToken.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));

            var healthCheckResult = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken.Token)
                .ConfigureAwait(false);

            return healthCheckResult;
        });

        return builder.WithHealthCheck(healthCheckName);
    }
}
