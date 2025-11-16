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
using Aspire.Hosting.Microcks.Async;

namespace Aspire.Hosting.Microcks;

/// <summary>
/// Extension methods for configuring the Microcks Async Minion resource.
/// </summary>
public static class MicrocksAsyncMinionExtensions
{

    /// <summary>
    /// Configures the Microcks Async Minion to connect to a Kafka broker.
    /// </summary>
    /// <param name="microcksBuilder">The resource builder for the Microcks Async Minion resource.</param>
    /// <param name="kafkaBuilder">The resource builder for the Kafka resource.</param>
    /// <param name="port">The port on which Kafka is exposed. Defaults to 9093.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksAsyncMinionResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<MicrocksAsyncMinionResource> WithKafkaConnection(
        this IResourceBuilder<MicrocksAsyncMinionResource> microcksBuilder,
        IResourceBuilder<IResource> kafkaBuilder,
        int port = 9093
        )
    {
        ArgumentNullException.ThrowIfNull(microcksBuilder, nameof(microcksBuilder));
        ArgumentNullException.ThrowIfNull(kafkaBuilder, nameof(kafkaBuilder));

        microcksBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["KAFKA_BOOTSTRAP_SERVER"] = $"{kafkaBuilder.Resource.Name}:{port}";

            // If not already set, append KAFKA to ASYNC_PROTOCOLS
            // e.g. ASYNC_PROTOCOLS=KAFKA or ASYNC_PROTOCOLS=AMQP,KAFKA
            // ASYNC_PROTOCOLS is a comma-separated list of protocols
            const string asyncProtocolEnvVar = "ASYNC_PROTOCOLS";

            context.EnvironmentVariables.TryGetValue(asyncProtocolEnvVar, out var existingProtocolsObj);
            var existingProtocols = existingProtocolsObj as string ?? string.Empty;
            context.EnvironmentVariables[asyncProtocolEnvVar] = string.IsNullOrWhiteSpace(existingProtocols)
                ? ",KAFKA"
                : $"{existingProtocols},KAFKA";
        });

        return microcksBuilder;
    }
}
