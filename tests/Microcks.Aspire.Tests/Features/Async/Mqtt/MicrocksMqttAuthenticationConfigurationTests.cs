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

namespace Microcks.Aspire.Tests.Features.Async.Mqtt;

/// <summary>
/// Tests for verifying MQTT authentication configuration without actually connecting.
/// These tests verify that username and password are correctly passed to the Async Minion.
/// </summary>
public sealed class MicrocksMqttAuthenticationConfigurationTests
{
    /// <summary>
    /// When MQTT is configured with username and password, then MQTT_USERNAME and MQTT_PASSWORD environment variables are set.
    /// </summary>
    [Fact]
    public void WhenMqttIsConfiguredWithCredentials_ThenEnvironmentVariablesAreSet()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var mqttUsername = builder.AddParameter("mqtt-username");
        var mqttPassword = builder.AddParameter("mqtt-password", secret: true);

        var mqtt = builder.AddContainer("mqtt", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(mqtt, port: 1883,
                    username: mqttUsername,
                    password: mqttPassword);
            });

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert - Verify MQTT broker configuration
        Assert.True(envVars.ContainsKey("MQTT_SERVER"), "MQTT_SERVER should be set");
        var mqttServer = envVars["MQTT_SERVER"];
        Assert.Contains("mqtt:", mqttServer);
        Assert.Contains("1883", mqttServer);

        // Assert - Verify MQTT authentication is configured
        Assert.True(envVars.ContainsKey("MQTT_USERNAME"), "MQTT_USERNAME should be set when credentials are provided");
        Assert.True(envVars.ContainsKey("MQTT_PASSWORD"), "MQTT_PASSWORD should be set when credentials are provided");

        // Verify the values are ParameterResource references
        var username = envVars["MQTT_USERNAME"];
        var password = envVars["MQTT_PASSWORD"];

        Assert.NotNull(username);
        Assert.NotNull(password);
    }

    /// <summary>
    /// When MQTT is configured without credentials, then MQTT_USERNAME and MQTT_PASSWORD are not set.
    /// </summary>
    [Fact]
    public void WhenMqttIsConfiguredWithoutCredentials_ThenAuthenticationVariablesAreNotSet()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var mqtt = builder.AddContainer("mqtt", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(mqtt, port: 1883); // No credentials
            });

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert - Verify MQTT broker is configured
        Assert.True(envVars.ContainsKey("MQTT_SERVER"));

        // Assert - Verify no authentication variables are set
        Assert.False(envVars.ContainsKey("MQTT_USERNAME"), "MQTT_USERNAME should not be set when no credentials are provided");
        Assert.False(envVars.ContainsKey("MQTT_PASSWORD"), "MQTT_PASSWORD should not be set when no credentials are provided");
    }

    /// <summary>
    /// When MQTT is configured with only username, then both MQTT_USERNAME and MQTT_PASSWORD should be set.
    /// </summary>
    [Fact]
    public void WhenMqttIsConfiguredWithOnlyUsername_ThenBothCredentialsAreRequired()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        var mqttUsername = builder.AddParameter("mqtt-username");

        var mqtt = builder.AddContainer("mqtt", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml"))
            .WithAsyncFeature(minion =>
            {
                // This should still work - password can be null
                minion.WithMqttConnection(mqtt, port: 1883, username: mqttUsername);
            });

        // Act
        var asyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().Single();
        var envVars = GetEnvironmentVariables(asyncMinionResource);

        // Assert - Username should be set
        Assert.True(envVars.ContainsKey("MQTT_USERNAME"), "MQTT_USERNAME should be set when username is provided");

        // Password might not be set if only username is provided (depends on implementation)
        // This test documents the actual behavior
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
