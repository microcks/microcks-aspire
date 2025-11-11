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
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Microcks;

/// <summary>
/// Extension methods for IResource.
/// </summary>
public static class IResourceExtensions
{
    /// <summary>
    /// Waits until the container resource emits a startup log line
    /// </summary>
    /// <param name="containerResource">The container resource to monitor.</param>
    /// <param name="cancellationToken">A token to cancel waiting early.</param>
    public static async Task WithWaitLogContainsAsync(
        this IResource containerResource,
        ResourceLoggerService resourceLoggerService,
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            // Watch the logs of the container resource until we find the specified content
            await foreach (var batch in resourceLoggerService.WatchAsync(containerResource).WithCancellation(cancellationToken))
            {
                if (batch.Any(line => line.Content.Contains(content, StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation while listening to logs
        }
        await Task.CompletedTask;
    }
}
