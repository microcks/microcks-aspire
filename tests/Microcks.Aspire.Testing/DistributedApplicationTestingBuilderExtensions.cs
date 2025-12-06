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
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microcks.Aspire.Testing;

/// <summary>
/// Extensions for <see cref="IDistributedApplicationTestingBuilder"/>.
/// </summary>
public static class DistributedApplicationTestingBuilderExtensions
{
    /// <summary>
    /// Generates a volume prefix based on application name and sha256 hash.
    /// </summary>
    /// <param name="builder">IDistributedApplicationTestingBuilder instance to generate the prefix for.</param>
    /// <returns>A string representing the volume prefix.</returns>
    public static string GetVolumePrefix(this IDistributedApplicationTestingBuilder builder) =>
        $"{Sanitize(builder.Environment.ApplicationName).ToLowerInvariant()}-{builder.Configuration["AppHost:Sha256"]!.ToLowerInvariant()[..10]}";

    /// <summary>
    /// Adds logging capabilities to the builder for both test output and Aspire resources.
    /// </summary>
    /// <param name="builder">IDistributedApplicationTestingBuilder instance to configure.</param>
    /// <param name="testOutputHelper">ITestOutputHelper instance for capturing test output.</param>
    /// <returns>The configured IDistributedApplicationTestingBuilder instance.</returns>
    public static IDistributedApplicationTestingBuilder WithTestAndResourceLogging(this IDistributedApplicationTestingBuilder builder, ITestOutputHelper testOutputHelper)
    {

        builder.Services.AddLogging(builder =>
        {
            builder.ClearProviders();

            // Add xUnit logging with custom format
            builder.AddXUnit(testOutputHelper, options =>
            {
                options.TimestampFormat = "HH:mm:ss.fff ";
                options.IncludeScopes = false;
            });

            builder.AddFilter("Aspire.Hosting", LogLevel.Trace);
            // No Polly
            builder.AddFilter("Polly", LogLevel.None);
        });

        return builder;
    }

    /// <summary>
    /// Configures the builder to use a temporary Aspire Store directory.
    /// </summary>
    /// <param name="builder">IDistributedApplicationTestingBuilder instance to configure.</param>
    /// <param name="path">Optional path for the Aspire Store. If not provided, a temporary directory is created.</param>
    /// <returns>The configured IDistributedApplicationTestingBuilder instance.</returns>
    public static IDistributedApplicationTestingBuilder WithTempAspireStore(this IDistributedApplicationTestingBuilder builder, string? path = null)
    {
        // We create the Aspire Store in a folder with user-only access. This way non-root containers won't be able
        // to access the files unless they correctly assign the required permissions for the container to work.

        builder.Configuration["Aspire:Store:Path"] = path ?? Directory.CreateTempSubdirectory().FullName;
        return builder;
    }

    /// <summary>
    /// Configures the builder to enable or disable resource cleanup after tests.
    /// </summary>
    /// <param name="builder">IDistributedApplicationTestingBuilder instance to configure.</param>
    /// <param name="resourceCleanup">Boolean indicating whether to enable resource cleanup. If null, the default behavior is used.</param>
    /// <returns>The configured IDistributedApplicationTestingBuilder instance.</returns>
    public static IDistributedApplicationTestingBuilder WithResourceCleanUp(this IDistributedApplicationTestingBuilder builder, bool? resourceCleanup = null)
    {
        builder.Configuration["DcpPublisher:WaitForResourceCleanup"] = resourceCleanup.ToString();
        return builder;
    }

    /// <summary>
    /// Sanitizes a string to be a valid Docker volume name.
    /// </summary>
    /// <param name="name">The input string to sanitize.</param>
    /// <returns>A sanitized string that is a valid Docker volume name.</returns>
    static string Sanitize(string name)
    {
        return string.Create(name.Length, name, static (s, name) =>
        {
            // According to the error message from docker CLI, volume names must be of form "[a-zA-Z0-9][a-zA-Z0-9_.-]"
            var nameSpan = name.AsSpan();

            for (var i = 0; i < nameSpan.Length; i++)
            {
                var c = nameSpan[i];

                s[i] = IsValidChar(i, c) ? c : '_';
            }
        });
    }

    static bool IsValidChar(int i, char c)
    {
        if (i == 0 && !(char.IsAsciiLetter(c) || char.IsNumber(c)))
        {
            // First char must be a letter or number
            return false;
        }
        else if (!(char.IsAsciiLetter(c) || char.IsNumber(c) || c == '_' || c == '.' || c == '-'))
        {
            // Subsequent chars must be a letter, number, underscore, period, or hyphen
            return false;
        }

        return true;
    }
}
