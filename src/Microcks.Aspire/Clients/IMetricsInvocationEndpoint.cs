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
using System.Threading.Tasks;
using Microcks.Aspire.Clients.Model;
using Refit;

namespace Microcks.Aspire.Clients;

/// <summary>
/// Defines the endpoint for retrieving metrics and invocation statistics from Microcks.
/// </summary>
public interface IMetricsInvocationEndpoint
{
    /// <summary>
    /// Gets the service invocations count for a specific service and version on a given day.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="serviceVersion">Version of the service.</param>
    /// <param name="day">The day for which to retrieve the invocation count (format: yyyyMMdd)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Daily invocation statistics.</returns>
    [Get("/api/metrics/invocations/{serviceName}/{serviceVersion}")]
    Task<ApiResponse<DailyInvocationStatistic>> GetServiceInvocationsCountAsync(
        string serviceName,
        string serviceVersion,
        [Query(Format = "yyyyMMdd")] DateOnly? day,
        CancellationToken cancellationToken);
}
