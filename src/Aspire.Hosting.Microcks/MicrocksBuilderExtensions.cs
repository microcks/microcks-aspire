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
using System.IO;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Microcks;
using Aspire.Hosting.Microcks.FileArtifacts;
using Aspire.Hosting.Microcks.MainRemoteArtifacts;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods to configure a Microcks resource on a distributed
/// application builder.
/// </summary>
public static class MicrocksBuilderExtensions
{
    /// <summary>
    /// Adds a Microcks resource to the distributed application and configures
    /// default HTTP endpoint, container image and registry.
    /// </summary>
    /// <param name="builder">The distributed application builder to extend.</param>
    /// <param name="name">The logical name of the Microcks resource. Must not be null or empty.</param>
    /// <returns>An <see cref="IResourceBuilder{MicrocksResource}"/> to further configure the resource.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public static IResourceBuilder<MicrocksResource> AddMicrocks(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        var microcksResource = new MicrocksResource(name);
        var resourceBuilder = builder
            .AddResource(microcksResource)
            .WithHttpEndpoint(targetPort: 8080, name: MicrocksResource.PrimaryEndpointName)
            .WithImage(MicrocksContainerImageTags.Image, MicrocksContainerImageTags.Tag)
            .WithImageRegistry(MicrocksContainerImageTags.Registry)
            .WithEnvironment("OTEL_JAVAAGENT_ENABLED", "true")
            .WithOtlpExporter();

        builder.Services.TryAddLifecycleHook<MicrocksResourceLifecycleHook>();

        // Configure Client for Microcks API
        builder.Services.ConfigureMicrocksProvider(microcksResource);

        return resourceBuilder;
    }

    /// <summary>
    /// Adds one or more main artifact file annotations to the Microcks resource.
    /// These artifacts will be uploaded to Microcks as primary artifacts when
    /// the resource is started.
    /// </summary>
    /// <param name="builder">The resource builder for the Microcks resource.</param>
    /// <param name="artifactFilePaths">File paths to the main artifact files to upload.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<MicrocksResource> WithMainArtifacts(this IResourceBuilder<MicrocksResource> builder, params string[] artifactFilePaths)
    {
        foreach (var sourcePath in artifactFilePaths)
        {
            string sourceFilePath = builder.ResolveFilePath(sourcePath);
            builder.WithAnnotation(new MainArtifactAnnotation(sourceFilePath));
        }

        return builder;
    }

    /// <summary>
    /// Adds remote artifact annotations (URLs) to be imported as main artifacts
    /// by the Microcks resource. These are useful to reference artifacts hosted
    /// externally (HTTP/HTTPS) instead of embedding files in the test resources.
    /// </summary>
    /// <param name="builder">The resource builder for the Microcks resource.</param>
    /// <param name="remoteArtifactUrls">Remote URLs pointing to artifact definitions.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<MicrocksResource> WithMainRemoteArtifacts(this IResourceBuilder<MicrocksResource> builder, params string[] remoteArtifactUrls)
    {
        foreach (var remoteArtifactUrl in remoteArtifactUrls)
        {
            builder.WithAnnotation(new MainRemoteArtifactAnnotation(remoteArtifactUrl));
        }

        return builder;
    }

    /// <summary>
    /// Adds one or more secondary artifact file annotations to the Microcks
    /// resource. Secondary artifacts may contain supplementary data (for
    /// example Postman collections) that complement main artifacts.
    /// </summary>
    /// <param name="builder">The resource builder for the Microcks resource.</param>
    /// <param name="artifactFilePaths">File paths to the secondary artifact files to upload.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<MicrocksResource> WithSecondaryArtifacts(this IResourceBuilder<MicrocksResource> builder, params string[] artifactFilePaths)
    {
        foreach (var sourcePath in artifactFilePaths)
        {
            string artifactFilePath = builder.ResolveFilePath(sourcePath);
            builder.WithAnnotation(new SecondaryArtifactAnnotation(artifactFilePath));
        }

        return builder;
    }

    /// <summary>
    /// Adds a snapshots annotation referencing a snapshots JSON file to the
    /// Microcks resource. Snapshots allow pre-populating Microcks with a
    /// previously exported repository state.
    /// </summary>
    /// <param name="builder">The resource builder for the Microcks resource.</param>
    /// <param name="snapshotsFilePath">The file path to the snapshots JSON file. Must not be null or whitespace.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksResource}"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="snapshotsFilePath"/> is null or whitespace.</exception>
    public static IResourceBuilder<MicrocksResource> WithSnapshots(this IResourceBuilder<MicrocksResource> builder, string snapshotsFilePath)
    {
        if (string.IsNullOrWhiteSpace(snapshotsFilePath))
        {
            throw new ArgumentException("Snapshots file path cannot be null or whitespace.", nameof(snapshotsFilePath));
        }
        var resolvedPath = builder.ResolveFilePath(snapshotsFilePath);

        builder.WithAnnotation(new SnapshotsAnnotation(resolvedPath));
        return builder;
    }

    /// <summary>
    /// Resolves a file path, making it absolute if it is relative
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="sourcePath"></param>
    /// <returns></returns>
    private static string ResolveFilePath(this IResourceBuilder<MicrocksResource> builder, string sourcePath)
    {
        // If the source is a rooted path, use it directly without resolution
        return Path.IsPathRooted(sourcePath)
            ? sourcePath
            : Path.GetFullPath(sourcePath, builder.ApplicationBuilder.AppHostDirectory);
    }


    /// <summary>
    /// Adds network access to the host machine from within the Microcks container.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
    /// <param name="hostAlias">The hostname alias to use for the host machine. Defaults to 'host.docker.internal'.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This allows the Microcks container to access services running on the host machine
    /// using the specified hostname alias. 'host.docker.internal' is Docker's standard
    /// hostname for accessing the host machine from containers.
    /// </remarks>
    public static IResourceBuilder<MicrocksResource> WithHostNetworkAccess(this IResourceBuilder<MicrocksResource> builder, string hostAlias = "host.docker.internal")
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        return builder.WithContainerRuntimeArgs($"--add-host={hostAlias}:host-gateway");
    }
}
