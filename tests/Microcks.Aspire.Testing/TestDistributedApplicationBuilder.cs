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
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microcks.Aspire.Testing;

/// <summary>
/// DistributedApplication.CreateBuilder() creates a builder that includes configuration to read from appsettings.json.
/// The builder has a FileSystemWatcher, which can't be cleaned up unless a DistributedApplication is built and disposed.
/// This class wraps the builder and provides a way to automatically dispose it to prevent test failures from excessive
/// FileSystemWatcher instances from many tests.
/// </summary>
public static class TestDistributedApplicationBuilder
{
    /// <summary>
    /// Creates a testing builder for a distributed application with the specified operation and publishing options.
    /// </summary>
    /// <param name="operation">The operation to perform (Run or Publish).</param>
    /// <param name="publisher">The publisher type for the publish operation. Defaults to "manifest".</param>
    /// <param name="outputPath">The output path for the publish operation. Defaults to "./".</param>
    /// <param name="isDeploy">Indicates whether to deploy during the publish operation. Defaults to false.</param>
    /// <returns>A configured <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(DistributedApplicationOperation operation, string publisher = "manifest", string outputPath = "./", bool isDeploy = false)
    {
        var args = operation switch
        {
            DistributedApplicationOperation.Run => (string[])[],
            DistributedApplicationOperation.Publish => [$"Publishing:Publisher={publisher}", $"Publishing:OutputPath={outputPath}", $"Publishing:Deploy={isDeploy}"],
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        return Create(args);
    }

    /// <summary>
    /// Creates a testing builder for a distributed application with the specified command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to the application.</param>
    /// <returns>A configured <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(params string[] args)
    {
        return CreateCore(args, (_) => { });
    }

    /// <summary>
    /// Creates a testing builder for a distributed application with test output helper and command-line arguments.
    /// </summary>
    /// <param name="testOutputHelper">The xUnit test output helper for logging test output.</param>
    /// <param name="args">Command-line arguments to pass to the application.</param>
    /// <returns>A configured <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(ITestOutputHelper testOutputHelper, params string[] args)
    {
        return CreateCore(args, (_) => { }, testOutputHelper);
    }

    /// <summary>
    /// Creates a testing builder for a distributed application with custom configuration options.
    /// </summary>
    /// <param name="configureOptions">Action to configure distributed application options.</param>
    /// <param name="testOutputHelper">Optional xUnit test output helper for logging test output.</param>
    /// <returns>A configured <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        return CreateCore([], configureOptions, testOutputHelper);
    }

    /// <summary>
    /// Creates a testing builder configured with the Aspire test container registry.
    /// </summary>
    /// <param name="testOutputHelper">The xUnit test output helper for logging test output.</param>
    /// <returns>A configured <see cref="IDistributedApplicationTestingBuilder"/> instance with test container registry override.</returns>
    public static IDistributedApplicationTestingBuilder CreateWithTestContainerRegistry(ITestOutputHelper testOutputHelper) =>
        Create(o => o.ContainerRegistryOverride = ComponentTestConstants.AspireTestContainerRegistry, testOutputHelper);

    private static IDistributedApplicationTestingBuilder CreateCore(string[] args, Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        var builder = DistributedApplicationTestingBuilder.Create(args, (applicationOptions, hostBuilderOptions) => configureOptions?.Invoke(applicationOptions));

        // TODO: consider centralizing this to DistributedApplicationFactory by default once consumers have a way to opt-out
        // E.g., once https://github.com/dotnet/extensions/pull/5801 is released.
        // Discussion: https://github.com/dotnet/aspire/pull/7335/files#r1936799460
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        builder.WithTempAspireStore();

        return builder;
    }
}

/// <summary>
/// Constants used for component testing.
/// </summary>
public static class ComponentTestConstants
{
    /// <summary>
    /// The container registry used for Aspire testing.
    /// </summary>
    public const string AspireTestContainerRegistry = "netaspireci.azurecr.io";
}
