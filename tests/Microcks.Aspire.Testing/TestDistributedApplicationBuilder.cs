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
    /// Creates a new instance of <see cref="IDistributedApplicationTestingBuilder"/> with the specified operation.
    /// </summary>
    /// <param name="operation">The distributed application operation (Run or Publish).</param>
    /// <param name="publisher">The publisher type for publish operations.</param>
    /// <param name="outputPath">The output path for publish operations.</param>
    /// <param name="isDeploy">Whether this is a deployment operation.</param>
    /// <returns>A new <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
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
    /// Creates a new instance of <see cref="IDistributedApplicationTestingBuilder"/> with the specified command-line arguments.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>A new <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(params string[] args)
    {
        return CreateCore(args, (_) => { });
    }

    /// <summary>
    /// Creates a new instance of <see cref="IDistributedApplicationTestingBuilder"/> with the specified test output helper and command-line arguments.
    /// </summary>
    /// <param name="testOutputHelper">The xUnit test output helper for logging.</param>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>A new <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(ITestOutputHelper testOutputHelper, params string[] args)
    {
        return CreateCore(args, (_) => { }, testOutputHelper);
    }

    /// <summary>
    /// Creates a new instance of <see cref="IDistributedApplicationTestingBuilder"/> with optional configuration and test output helper.
    /// </summary>
    /// <param name="configureOptions">An optional action to configure the distributed application options.</param>
    /// <param name="testOutputHelper">An optional xUnit test output helper for logging.</param>
    /// <returns>A new <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
    public static IDistributedApplicationTestingBuilder Create(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        return CreateCore([], configureOptions, testOutputHelper);
    }

    /// <summary>
    /// Creates a new instance with test container registry override.
    /// </summary>
    /// <param name="testOutputHelper">The xUnit test output helper for logging.</param>
    /// <returns>A new <see cref="IDistributedApplicationTestingBuilder"/> instance.</returns>
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
    /// The Aspire test container registry URL.
    /// </summary>
    public const string AspireTestContainerRegistry = "netaspireci.azurecr.io";
}
