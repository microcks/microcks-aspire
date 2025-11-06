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
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Microcks;

/// <summary>
/// Represents a Microcks container resource within an Aspire distributed application.
/// This class provides methods to retrieve mock endpoints for different service protocols, including SOAP, REST, GraphQL, and gRPC.
/// It is intended for use in service discovery and integration testing scenarios.
/// </summary>
public class MicrocksResource(string name) : ContainerResource(name),
    IResourceWithServiceDiscovery
{
    internal const string PrimaryEndpointName = "http";

    /// <summary>
    /// Gets an endpoint reference.
    /// </summary>
    /// <returns>An <see cref="EndpointReference"/> object representing the endpoint reference.</returns>
    public EndpointReference GetEndpoint()
    {
        return new EndpointReference(this, PrimaryEndpointName);
    }

    /// <summary>
    /// Returns the SOAP mock endpoint for a given service name and version.
    /// </summary>
    /// <param name="serviceName">The name of the target service.</param>
    /// <param name="serviceVersion">The version of the target service.</param>
    /// <returns>The URI of the corresponding SOAP mock endpoint.</returns>
    public Uri GetSoapMockEndpoint(string serviceName, string serviceVersion)
    {
        var httpEndpoint = this.GetEndpoint();
        return new UriBuilder(httpEndpoint.Url)
        {
            Path = $"soap/{serviceName}/{serviceVersion}"
        }.Uri;
    }

    /// <summary>
    /// Returns the REST mock endpoint for a given service name and version.
    /// </summary>
    /// <param name="serviceName">The name of the target service.</param>
    /// <param name="serviceVersion">The version of the target service.</param>
    /// <returns>The URI of the corresponding REST mock endpoint.</returns>
    public Uri GetRestMockEndpoint(string serviceName, string serviceVersion)
    {
        var httpEndpoint = this.GetEndpoint();
        return new UriBuilder(httpEndpoint.Url)
        {
            Path = $"rest/{serviceName}/{serviceVersion}"
        }.Uri;
    }

    /// <summary>
    /// Returns the GraphQL mock endpoint for a given service name and version.
    /// </summary>
    /// <param name="serviceName">The name of the target service.</param>
    /// <param name="serviceVersion">The version of the target service.</param>
    /// <returns>The URI of the corresponding GraphQL mock endpoint.</returns>
    public Uri GetGraphQLMockEndpoint(string serviceName, string serviceVersion)
    {
        var httpEndpoint = this.GetEndpoint();
        return new UriBuilder(httpEndpoint.Url)
        {
            Path = $"graphql/{serviceName}/{serviceVersion}"
        }.Uri;
    }

    /// <summary>
    /// Returns the gRPC mock endpoint for the Microcks resource.
    /// </summary>
    /// <returns>The URI of the corresponding Grpc mock endpoint.</returns>
    public Uri GetGrpcMockEndpoint()
    {
        var httpEndpoint = this.GetEndpoint();

        return new UriBuilder(httpEndpoint.Url)
        {
            Scheme = "grpc"
        }.Uri;
    }

}
