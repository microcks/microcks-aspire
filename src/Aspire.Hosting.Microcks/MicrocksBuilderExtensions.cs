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
using System.Linq;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Microcks;
using Aspire.Hosting.Microcks.Async;
using Aspire.Hosting.Microcks.FileArtifacts;
using Aspire.Hosting.Microcks.HealthCheck;
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
            .WithHttpHealthCheck("api/health", statusCode: 200)
            .WaitForConsoleOutput("Started MicrocksApplication", TimeSpan.FromSeconds(5))
            .WithImage(MicrocksContainerImageTags.Image, MicrocksContainerImageTags.Tag)
            .WithImageRegistry(MicrocksContainerImageTags.Registry)
            .WithEnvironment("OTEL_JAVAAGENT_ENABLED", "true")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["TEST_CALLBACK_URL"] = $"http://{context.Resource.Name}:8080";
            })
            .WithOtlpExporter();

        // Register lifecycle hook for Microcks (Import artifacts, etc.)
        builder.Services.TryAddLifecycleHook<MicrocksResourceLifecycleHook>();

        // Configure Client for Microcks API
        builder.Services.ConfigureMicrocksClient(microcksResource);

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


    /// <summary>
    /// Configures the Microcks resource to deploy an Async Minion alongside
    /// the main Microcks instance.
    /// </summary>
    /// <param name="builder">The resource builder for the Microcks resource.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksResource}"/>
    public static IResourceBuilder<MicrocksResource> WithAsyncFeature(
        this IResourceBuilder<MicrocksResource> microcksBuilder,
        Action<IResourceBuilder<MicrocksAsyncMinionResource>>? configureAsyncMinion = null)
    {
        ArgumentNullException.ThrowIfNull(microcksBuilder, nameof(microcksBuilder));

        var microcksResource = microcksBuilder.Resource;
        var applicationBuilder = microcksBuilder.ApplicationBuilder;

        // Check if an Async Minion resource already exists
        bool asyncMinionExists = applicationBuilder.Resources
            .OfType<MicrocksAsyncMinionResource>()
            .Any();

        if (asyncMinionExists)
        {
            return microcksBuilder; // Async Minion already configured
        }

        // Retrieve Microcks container image annotations
        var microcksAnnotations = microcksResource.Annotations;

        // Determine image name for Async Minion from Microcks resource
        var containerImageAnnotations = microcksAnnotations.OfType<ContainerImageAnnotation>();
        var containerImageAnnotation = containerImageAnnotations.Single();
        var microcksImage = containerImageAnnotation.Image;

        var microcksAsyncMinionImage = microcksImage.Replace("microcks-uber", "microcks-uber-async-minion");

        var microcksAsyncMinionTag = containerImageAnnotation.Tag;
        if (microcksAsyncMinionTag.EndsWith("-native"))
        {
            microcksAsyncMinionTag = microcksAsyncMinionTag.Replace("-native", "");
        }

        var asyncMicrocksBuilder = applicationBuilder.AddMicrocksAsyncMinion(microcksBuilder)
            .WithImage(microcksAsyncMinionImage, microcksAsyncMinionTag);

        // Apply optional configuration action
        configureAsyncMinion?.Invoke(asyncMicrocksBuilder);

        return microcksBuilder;
    }

    /// <summary>
    /// Adds a Microcks Async Minion resource to the distributed application,
    /// linked to the specified parent Microcks resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="parentMicrocksBuilder">The resource builder for the parent Microcks resource.</param>
    /// <returns>A resource builder for the newly added Microcks Async Minion resource.</returns>
    public static IResourceBuilder<MicrocksAsyncMinionResource> AddMicrocksAsyncMinion(
        this IDistributedApplicationBuilder builder, IResourceBuilder<MicrocksResource> parentMicrocksBuilder)
    {
        var parentMicrocksResource = parentMicrocksBuilder.Resource;
        string name = $"{parentMicrocksResource.Name}-async-minion";
        var asyncMinionResource = new MicrocksAsyncMinionResource(name);

        var resourceBuilder = builder
            .AddResource(asyncMinionResource)
            .WithHttpEndpoint(targetPort: 8081, name: MicrocksAsyncMinionResource.PrimaryEndpointName)
            .WaitForConsoleOutput("Profile prod activated.") // Check if Async Minion started correctly
            .WithParentRelationship(parentMicrocksResource)
            .WithEnvironment(context =>
            {
                var microcksEndpoint = parentMicrocksResource.GetEndpoint();
                var microcksUrl = microcksEndpoint.Property(EndpointProperty.HostAndPort);
                context.EnvironmentVariables["MICROCKS_HOST_PORT"] = microcksUrl;
            })
            .WaitFor(parentMicrocksBuilder); // Ensure Microcks starts before Async Minion


        parentMicrocksBuilder.WithEnvironment(context =>
        {
            var asyncMinionEndpoint = asyncMinionResource.GetEndpoint();
            var asyncMinionUrl = asyncMinionEndpoint.Property(EndpointProperty.Url);

            context.EnvironmentVariables["ASYNC_MINION_URL"] = asyncMinionUrl;
        });

        return resourceBuilder;
    }

    /// <summary>
    /// Configures the Microcks resource to include and enable a Postman runner.
    /// This allows Microcks to execute Postman collections for API mocking and testing.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
    /// <param name="configurePostman">An optional action to further configure the Postman resource.</param>
    /// <param name="containerName">An optional name for the Postman container. If not provided, a default name will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MicrocksResource> WithPostmanRunner(
        this IResourceBuilder<MicrocksResource> microcksBuilder,
        Action<IResourceBuilder<MicrocksPostmanResource>>? configurePostman = null,
        string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(microcksBuilder, nameof(microcksBuilder));

        containerName ??= $"{microcksBuilder.Resource.Name}-postman";

        var distributedAppBuilder = microcksBuilder.ApplicationBuilder;

        var postmanResource = new MicrocksPostmanResource(containerName);

        var postmanBuilder = distributedAppBuilder.AddResource(postmanResource)
            .WithImage("microcks/microcks-postman-runtime", "latest")
            .WaitForConsoleOutput("postman-runtime wrapper listening on port")
            .WithHttpEndpoint(targetPort: 3000, name: MicrocksPostmanResource.PrimaryEndpointName)
            .WithImageRegistry(MicrocksContainerImageTags.Registry)
            .WithParentRelationship(microcksBuilder);

        // Link Postman runner to Microcks main resource
        microcksBuilder.WaitFor(postmanBuilder)
            .WithEnvironment(context =>
            {
                var postmanEndpoint = postmanResource.GetEndpoint("http");
                var postmanUrl = postmanEndpoint.Property(EndpointProperty.Url);

                context.EnvironmentVariables["POSTMAN_RUNNER_URL"] = postmanUrl;
            });

        // Give access to Microcks Postman configuration (Custom Image, tags, ...)
        configurePostman?.Invoke(postmanBuilder);

        return microcksBuilder;
    }
}
