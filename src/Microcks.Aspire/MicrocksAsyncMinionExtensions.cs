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
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;

namespace Microcks.Aspire;

/// <summary>
/// Extension methods for configuring the Microcks Async Minion resource.
/// </summary>
public static class MicrocksAsyncMinionExtensions
{
    private const string AsyncProtocolsEnvVar = "ASYNC_PROTOCOLS";

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
        ArgumentNullException.ThrowIfNull(microcksBuilder);
        ArgumentNullException.ThrowIfNull(kafkaBuilder, nameof(kafkaBuilder));

        microcksBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["KAFKA_BOOTSTRAP_SERVER"] = $"{kafkaBuilder.Resource.Name}:{port}";

            // Append KAFKA to ASYNC_PROTOCOLS
            // e.g. ASYNC_PROTOCOLS=KAFKA or ASYNC_PROTOCOLS=AMQP,KAFKA
            // ASYNC_PROTOCOLS is a comma-separated list of protocols
            context.EnvironmentVariables.TryGetValue(AsyncProtocolsEnvVar, out var existingProtocolsObj);
            var existingProtocols = existingProtocolsObj as string ?? string.Empty;
            context.EnvironmentVariables[AsyncProtocolsEnvVar] = string.IsNullOrWhiteSpace(existingProtocols)
                ? ",KAFKA"
                : $"{existingProtocols},KAFKA";
        });

        return microcksBuilder;
    }

    /// <summary>
    /// Configures the Microcks Async Minion to connect to an AMQP broker.
    /// </summary>
    /// <param name="microcksBuilder">The resource builder for the Microcks Async Minion resource.</param>
    /// <param name="brokerBuilder">The resource builder for the AMQP broker resource.</param>
    /// <param name="username">The username parameter for authentication.</param>
    /// <param name="password">The password parameter for authentication.</param>
    /// <param name="port">The port on which the AMQP broker is exposed. Defaults to 5672.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksAsyncMinionResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<MicrocksAsyncMinionResource> WithAmqpConnection(
        this IResourceBuilder<MicrocksAsyncMinionResource> microcksBuilder,
        IResourceBuilder<IResource> brokerBuilder,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password,
        int port = 5672)
    {
        ArgumentNullException.ThrowIfNull(microcksBuilder, nameof(microcksBuilder));
        ArgumentNullException.ThrowIfNull(brokerBuilder, nameof(brokerBuilder));
        ArgumentNullException.ThrowIfNull(username, nameof(username));
        ArgumentNullException.ThrowIfNull(password, nameof(password));

        microcksBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["AMQP_SERVER"] = $"{brokerBuilder.Resource.Name}:{port}";
            context.EnvironmentVariables["AMQP_USERNAME"] = username.Resource;
            context.EnvironmentVariables["AMQP_PASSWORD"] = password.Resource;

            context.EnvironmentVariables.TryGetValue(AsyncProtocolsEnvVar, out var existingProtocolsObj);
            var existingProtocols = existingProtocolsObj as string ?? string.Empty;
            context.EnvironmentVariables[AsyncProtocolsEnvVar] = string.IsNullOrWhiteSpace(existingProtocols)
                ? ",AMQP"
                : $"{existingProtocols},AMQP";
        });

        microcksBuilder.WaitFor(brokerBuilder);

        return microcksBuilder;
    }

    /// <summary>
    /// Configures the Microcks Async Minion to connect to an MQTT broker.
    /// </summary>
    /// <param name="microcksBuilder">The resource builder for the Microcks Async Minion resource.</param>
    /// <param name="brokerBuilder">The resource builder for the MQTT broker resource.</param>
    /// <param name="port">The port on which the MQTT broker is exposed. Defaults to 1883.</param>
    /// <param name="username">Optional username parameter for authentication.</param>
    /// <param name="password">Optional password parameter for authentication.</param>
    /// <returns>The same <see cref="IResourceBuilder{MicrocksAsyncMinionResource}"/> instance for chaining.</returns>
    public static IResourceBuilder<MicrocksAsyncMinionResource> WithMqttConnection(
        this IResourceBuilder<MicrocksAsyncMinionResource> microcksBuilder,
        IResourceBuilder<IResource> brokerBuilder,
        int port = 1883,
        IResourceBuilder<ParameterResource>? username = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(microcksBuilder, nameof(microcksBuilder));
        ArgumentNullException.ThrowIfNull(brokerBuilder, nameof(brokerBuilder));

        microcksBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["MQTT_SERVER"] = $"{brokerBuilder.Resource.Name}:{port}";

            if (username != null)
            {
                context.EnvironmentVariables["MQTT_USERNAME"] = username.Resource;
            }

            if (password != null)
            {
                context.EnvironmentVariables["MQTT_PASSWORD"] = password.Resource;
            }

            context.EnvironmentVariables.TryGetValue(AsyncProtocolsEnvVar, out var existingProtocolsObj);
            var existingProtocols = existingProtocolsObj as string ?? string.Empty;
            context.EnvironmentVariables[AsyncProtocolsEnvVar] = string.IsNullOrWhiteSpace(existingProtocols)
                ? ",MQTT"
                : $"{existingProtocols},MQTT";
        });

        microcksBuilder.WaitFor(brokerBuilder);

        return microcksBuilder;
    }
}
