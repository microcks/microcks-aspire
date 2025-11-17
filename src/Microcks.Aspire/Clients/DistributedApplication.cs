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
using Microcks.Aspire.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for working with Microcks in a distributed application.
/// </summary>
public static class DistributedApplicationExtensions
{
    /// <summary>
    /// Creates an instance of IMicrocksClient using the application's service provider.
    /// </summary>
    /// <param name="app">The distributed application instance.</param>
    /// <param name="resourceName">The name of the Microcks resource to use.</param>
    /// <returns>An instance of IMicrocksClient.</returns>
    /// <exception cref="ArgumentNullException">Thrown if app is null.</exception>
    public static IMicrocksClient CreateMicrocksClient(this DistributedApplication app, string resourceName)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(resourceName);

        // Create a scope to resolve scoped services
        using var scope = app.Services.CreateScope();
        var microcksClient = scope.ServiceProvider.GetRequiredKeyedService<IMicrocksClient>(resourceName);
        return microcksClient;
    }

}
