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
using System.Net.Http;
using Aspire.Hosting.Microcks.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aspire.Hosting.Microcks;


/// <summary>
/// Extension methods to configure Aspire Microcks hosting services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Aspire Microcks hosting services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection ConfigureMicrocksClient(this IServiceCollection services, MicrocksResource microcksResource)
    {
        var name = microcksResource.Name;
        // Configure Refit to use System.Text.Json with explicit options so
        // enum values are serialized exactly as defined in the enum (no
        // naming policy that could change casing or underscores).
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            // Do not use a naming policy which could alter enum text
            PropertyNamingPolicy = null,
            // Ensure numbers are not used for enums
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
        };


        services.AddHttpClient(name, (sp, httpClient) =>
        {
            var endpointUrl = microcksResource.GetEndpoint().Url;
            var logger = sp.GetRequiredService<ILogger<MicrocksResource>>();
            logger.LogInformation("Configuring Microcks HttpClient for endpoint URL: {EndpointUrl}", endpointUrl);

            httpClient.BaseAddress = new Uri(endpointUrl);
            // Authentication
            // TODO: Add support for authentication
            // httpClient.DefaultRequestHeaders.Authorization = ...
            // Get information from MicrocksResource (e.g., labels, annotations)
        });

        // Explicitly register IMicrocksEndpoint as a keyed service to ensure it's available for DI
        services.AddKeyedScoped(name, (serviceProvider, serviceKey) =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(serviceKey.ToString());
            return RestService.For<IMicrocksEndpoint>(httpClient, refitSettings);
        });

        // Register keyed service for MicrocksClient
        services.AddKeyedScoped<IMicrocksClient>(name, (serviceProvider, serviceKey) =>
        {
            IMicrocksEndpoint microcksEndpoint = serviceProvider.GetRequiredKeyedService<IMicrocksEndpoint>(serviceKey);
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var client = new MicrocksClient(microcksEndpoint, loggerFactory);
            return client;
        });

        return services;
    }
}
