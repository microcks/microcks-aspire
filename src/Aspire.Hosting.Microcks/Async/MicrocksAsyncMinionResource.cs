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

namespace Aspire.Hosting.Microcks.Async;

public class MicrocksAsyncMinionResource : ContainerResource
{
    public MicrocksAsyncMinionResource(string name) : base(name)
    {
    }
    private const string DestinationPattern = "{0}-{1}-{2}";

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
    /// Gets the WebSocket mock endpoint URI for a given service and operation.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="serviceVersion">The version of the service.</param>
    /// <param name="operationName">The name of the operation (may start with SUBSCRIBE or PUBLISH).</param>
    /// <returns>The WebSocket mock endpoint URI.</returns>
    public Uri GetWebSocketMockEndpoint(string serviceName, string serviceVersion, string operationName)
    {
        operationName = ExtractOperationName(operationName);
        var endpoint = this.GetEndpoint();

        var escapedService = serviceName.Replace(" ", "+");
        var escapedVersion = serviceVersion.Replace(" ", "+");

        return new Uri($"ws://{endpoint.Host}:{endpoint.Port}/api/ws/{escapedService}/{escapedVersion}/{operationName}");
    }

    /// <summary>
    /// Extracts the operation name from the provided operation name.
    /// </summary>
    /// <param name="operationName">operationName may start with SUBSCRIBE or PUBLISH.</param>
    /// <returns>The extracted operation name.</returns>
    private string ExtractOperationName(string operationName)
    {
        if (operationName.Contains(' '))
        {
            operationName = operationName.Split(' ')[1];
        }
        return operationName;
    }


    /// <summary>
    /// Generates a Kafka mock topic name based on the provided service, version, and operation name.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="operationName">The name of the operation, which may start with SUBSCRIBE or PUBLISH.</param>
    /// <returns>A formatted Kafka mock topic name.</returns>
    public string GetKafkaMockTopic(string service, string version, string operationName)
    {
        operationName = ExtractOperationName(operationName);

        return string.Format(DestinationPattern,
            service.Replace(" ", "").Replace("-", ""),
            version,
            operationName.Replace("/", "-"));
    }

}
