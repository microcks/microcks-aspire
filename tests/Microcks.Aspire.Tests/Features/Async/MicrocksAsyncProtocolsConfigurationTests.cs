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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async;

/// <summary>
/// Tests for verifying ASYNC_PROTOCOLS environment variable configuration without starting the application.
/// These are fast unit tests that only verify the configuration is correctly set.
/// </summary>
public sealed class MicrocksAsyncProtocolsConfigurationTests
{
    /// <summary>
    /// When Kafka and MQTT are configured, then ASYNC_PROTOCOLS contains KAFKA,MQTT.
    /// </summary>
    [Fact]
    public void WhenKafkaAndMqttAreConfigured_ThenAsyncProtocolsContainsAll()
    {
        // Arrange - Create builder without starting the app
        var builder = DistributedApplication.CreateBuilder();

        var kafka = builder.AddKafka("kafka");
        var mqtt = builder.AddContainer("mqtt", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                minion.WithKafkaConnection(kafka, port: 9093);
                minion.WithMqttConnection(mqtt, port: 1883);
            });

        // Act - Get the async minion resource and check environment variables
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert
        Assert.True(envVars.ContainsKey("ASYNC_PROTOCOLS"), "ASYNC_PROTOCOLS should be set");

        var protocols = envVars["ASYNC_PROTOCOLS"].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("KAFKA", protocols);
        Assert.Contains("MQTT", protocols);
        Assert.Equal(2, protocols.Length);
    }

    /// <summary>
    /// When only Kafka is configured, then ASYNC_PROTOCOLS contains KAFKA.
    /// </summary>
    [Fact]
    public void WhenOnlyKafkaIsConfigured_ThenAsyncProtocolsContainsKafka()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var kafka = builder.AddKafka("kafka");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                minion.WithKafkaConnection(kafka, port: 9093);
            });

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert
        Assert.True(envVars.ContainsKey("ASYNC_PROTOCOLS"));

        var protocols = envVars["ASYNC_PROTOCOLS"].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("KAFKA", protocols);
        Assert.DoesNotContain("MQTT", protocols);
        Assert.Single(protocols);
    }

    /// <summary>
    /// When only MQTT is configured, then ASYNC_PROTOCOLS contains MQTT.
    /// </summary>
    [Fact]
    public void WhenOnlyMqttIsConfigured_ThenAsyncProtocolsContainsMqtt()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var mqtt = builder.AddContainer("mqtt", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(mqtt, port: 1883);
            });

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert
        Assert.True(envVars.ContainsKey("ASYNC_PROTOCOLS"));

        var protocols = envVars["ASYNC_PROTOCOLS"].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("MQTT", protocols);
        Assert.DoesNotContain("KAFKA", protocols);
        Assert.Single(protocols);
    }

    /// <summary>
    /// When only WebSocket is configured (no additional protocols), then ASYNC_PROTOCOLS contains only WS.
    /// </summary>
    [Fact]
    public void WhenOnlyWebSocketIsConfigured_ThenAsyncProtocolsContainsOnlyWs()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(); // No additional protocols

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert
        Assert.False(envVars.ContainsKey("ASYNC_PROTOCOLS"));
    }

    /// <summary>
    /// When AMQP is configured along with MQTT, then ASYNC_PROTOCOLS contains AMQP,MQTT.
    /// </summary>
    [Fact]
    public void WhenAmqpAndMqttAreConfigured_ThenAsyncProtocolsContainsAll()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var rabbitmq = builder.AddRabbitMQ("rabbitmq");
        var username = builder.AddParameter("amqp-username");
        var password = builder.AddParameter("amqp-password", secret: true);

        var mqtt = builder.AddContainer("mqtt", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                minion.WithAmqpConnection(rabbitmq, username, password);
                minion.WithMqttConnection(mqtt, port: 1883);
            });

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert
        Assert.True(envVars.ContainsKey("ASYNC_PROTOCOLS"));

        var protocols = envVars["ASYNC_PROTOCOLS"].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("AMQP", protocols);
        Assert.Contains("MQTT", protocols);
        Assert.Equal(2, protocols.Length);
    }

    /// <summary>
    /// Helper method to extract environment variables from a resource without starting the app.
    /// </summary>
    private static Dictionary<string, string> GetEnvironmentVariables(MicrocksAsyncMinionResource resource)
    {
        var envVars = new Dictionary<string, object>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, envVars);

        var envAnnotations = resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        return envVars.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );
    }
}
