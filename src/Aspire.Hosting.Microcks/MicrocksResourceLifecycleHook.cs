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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Microcks.Clients;
using Aspire.Hosting.Microcks.FileArtifacts;
using Aspire.Hosting.Microcks.MainRemoteArtifacts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Microcks;

/// <summary>
/// Lifecycle hook that initializes Microcks resources after containers are
/// created. When the distributed application is running (not in publish
/// mode), this hook watches the Microcks container logs for readiness,
/// uploads configured artifacts, imports remote artifacts and snapshots,
/// and waits for the service health endpoint to become available.
/// </summary>
internal sealed class MicrocksResourceLifecycleHook
    : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();
    private ILogger<MicrocksResource> _logger;
    private ResourceNotificationService _resourceNotificationService;
    private DistributedApplicationExecutionContext _executionContext;
    private IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrocksResourceLifecycleHook"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create loggers for resources.</param>
    /// <param name="resourceLoggerService">Service used to stream resource logs for readiness detection.</param>
    /// <param name="resourceNotificationService">Service used to publish resource state changes.</param>
    /// <param name="executionContext">Execution context describing run/publish mode.</param>
    /// <param name="serviceProvider">Service provider for resolving scoped services.</param>
    public MicrocksResourceLifecycleHook(
        ILoggerFactory loggerFactory,
        ResourceNotificationService resourceNotificationService,
        DistributedApplicationExecutionContext executionContext,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<MicrocksResource>();
        _resourceNotificationService = resourceNotificationService;
        _executionContext = executionContext;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Called after container resources have been created. For each Microcks
    /// resource this hook will attach a logger, wait for the service to be
    /// ready, and then upload/import configured artifacts and snapshots.
    /// </summary>
    /// <param name="appModel">The distributed application model containing created resources.</param>
    /// <param name="cancellationToken">A token to observe while waiting for resources or performing imports.</param>
    public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // MicrocksResourceLifecycleHook only applies to RunMode
        if (_executionContext.IsPublishMode)
        {
            return;
        }

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _shutdownCancellationTokenSource.Token,
            cancellationToken);

        var microcksResources = appModel.GetContainerResources()
            .OfType<MicrocksResource>();

        foreach (var microcksResource in microcksResources)
        {
            await this._resourceNotificationService.WaitForResourceHealthyAsync(
                microcksResource.Name,
                cancellationToken: cancellationTokenSource.Token)
                .ConfigureAwait(false);

            var endpoint = microcksResource.GetEndpoint();
            if (endpoint.IsAllocated)
            {
                // Get the provider from DI and use it for upload/import operations
                using var scope = _serviceProvider.CreateScope();
                var microcksClient = scope.ServiceProvider
                    .GetRequiredKeyedService<IMicrocksClient>(microcksResource.Name);

                ResourceAnnotationCollection annotations = microcksResource.Annotations;

                // Upload Microcks artifacts
                await UploadArtifactsAsync(microcksClient, annotations, cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                // Import Microcks remote artifacts
                var remoteArtifactUrls = annotations.OfType<MainRemoteArtifactAnnotation>();
                await ImportRemoteArtifactsAsync(microcksClient, remoteArtifactUrls, cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                var snapshotAnnotations = annotations.OfType<SnapshotsAnnotation>();
                await ImportSnapshotsAsync(microcksClient, snapshotAnnotations, cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                _logger.LogInformation("Microcks resource '{ResourceName}' is fully configured with all artifacts imported", microcksResource.Name);
            }
        }
    }

    private async Task UploadArtifactsAsync(
        IMicrocksClient microcksClient,
        ResourceAnnotationCollection annotations,
        CancellationToken cancellationToken)
    {
        var mainArtifactsAnnotations = annotations.OfType<MainArtifactAnnotation>();
        if (mainArtifactsAnnotations == null || !mainArtifactsAnnotations.Any())
        {
            return;
        }

        foreach (var artifactPath in mainArtifactsAnnotations.Select(a => a.SourcePath))
        {
            await microcksClient.ImportArtifactAsync(artifactPath, true, cancellationToken);
        }

        // Upload secondary artifacts
        var secondaryArtifactsAnnotations = annotations.OfType<SecondaryArtifactAnnotation>();
        foreach (var artifactPath in secondaryArtifactsAnnotations.Select(a => a.SourcePath))
        {
            await microcksClient.ImportArtifactAsync(artifactPath, false, cancellationToken);
        }
    }

    private async Task ImportSnapshotsAsync(IMicrocksClient microcksClient, IEnumerable<SnapshotsAnnotation> snapshotAnnotations, CancellationToken cancellationToken)
    {
        foreach (var artifactPath in snapshotAnnotations.Select(a => a.SnapshotsFilePath))
        {
            await microcksClient.ImportSnapshotAsync(artifactPath, cancellationToken);
        }
    }

    private async Task ImportRemoteArtifactsAsync(
        IMicrocksClient microcksClient,
        IEnumerable<MainRemoteArtifactAnnotation> remoteArtifactUrls,
        CancellationToken cancellationToken)
    {
        if (remoteArtifactUrls == null || !remoteArtifactUrls.Any())
        {
            return;
        }

        foreach (var remoteUrl in remoteArtifactUrls.Select(a => a.RemoteArtifactUrl))
        {
            await microcksClient.ImportRemoteArtifactAsync(remoteUrl, cancellationToken);
        }
    }

    /// <summary>
    /// Cancels any pending watch operations and disposes the internal
    /// cancellation token source used by this hook.
    /// </summary>
    /// <returns>A value task that completes when disposal is finished.</returns>
    public async ValueTask DisposeAsync()
    {
        await _shutdownCancellationTokenSource.CancelAsync();
    }
}
