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
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Microcks;

internal sealed class MicrocksPostmanResourceLifecycleHook : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();

    public MicrocksPostmanResourceLifecycleHook(
        ResourceLoggerService resourceLoggerService,
        DistributedApplicationExecutionContext executionContext)
    {
        _resourceLoggerService = resourceLoggerService;
        _executionContext = executionContext;
    }

    /// <summary>
    /// Called after all resources have been created.
    /// </summary>
    /// <param name="appModel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (_executionContext.IsPublishMode)
        {
            return;
        }

        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _shutdownCancellationTokenSource.Token,
            cancellationToken);

        var microcksPostmanResources = appModel.GetContainerResources()
            .OfType<MicrocksPostmanResource>();

        foreach (var microcksPostmanResource in microcksPostmanResources)
        {
            await microcksPostmanResource.WithWaitLogContainsAsync(
                _resourceLoggerService,
                "postman-runtime wrapper listening on port",
                cancellationTokenSource.Token
            );
        }

        // No-op
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the lifecycle hook, cancelling any ongoing operations.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        await _shutdownCancellationTokenSource.CancelAsync();
    }

}
