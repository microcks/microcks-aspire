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
using System.Linq;
using Aspire.Hosting;
using Microcks.Aspire.Async;
using Microcks.Aspire.Testing;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async.Mqtt;

/// <summary>
/// Unit tests for Microcks with MQTT (HiveMQ) builder configuration.
/// These tests validate the application model without starting any containers.
/// </summary>
public sealed class MicrocksHiveMqBuilderTests
{
    /// <summary>
    /// When the application builder is configured with Microcks and HiveMQ,
    /// then the MicrocksAsyncMinionResource and HiveMQ resources are present in the model.
    /// </summary>
    [Fact]
    public void WhenBuilderIsConfigured_ThenMicrocksAsyncResourceAndHiveMQAreAvailable()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create();

        // Add HiveMQ MQTT broker
        var hivemqBuilder = builder.AddContainer("hivemq", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        // Add Microcks with AsyncMinion configured for HiveMQ
        var microcksBuilder = builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(hivemqBuilder, port: 1883);
            });

        string expectedAsyncMinionName = "microcks-pastry-async-minion";

        // Act & Assert
        // Check Microcks Async Minion
        var microcksAsyncMinionResources = builder.Resources.OfType<MicrocksAsyncMinionResource>();
        MicrocksAsyncMinionResource asyncMinionResource = Assert.Single(microcksAsyncMinionResources);
        Assert.Equal(expectedAsyncMinionName, asyncMinionResource.Name);

        // Check HiveMQ resource
        var hivemqResources = builder.Resources.Where(r => r.Name == "hivemq");
        var hivemqResource = Assert.Single(hivemqResources);
        Assert.Equal("hivemq", hivemqResource.Name);
    }

    /// <summary>
    /// When GetMqttMockTopic is called, then it returns the correct topic format.
    /// </summary>
    [Fact]
    public void WhenGetMqttMockTopicIsCalled_ThenReturnsCorrectFormat()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create();

        var hivemqBuilder = builder.AddContainer("hivemq", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(hivemqBuilder, port: 1883);
            });

        var microcksAsyncMinionResource = builder.Resources
            .OfType<MicrocksAsyncMinionResource>()
            .Single();

        // Act
        var mqttTopic = microcksAsyncMinionResource
            .GetMqttMockTopic("Pastry orders API", "0.1.0", "SUBSCRIBE pastry/orders");

        // Assert
        Assert.Equal("PastryordersAPI-0.1.0-pastry/orders", mqttTopic);
    }

    /// <summary>
    /// When GetMqttMockTopic is called with operation starting with PUBLISH, then it extracts operation name correctly.
    /// </summary>
    [Fact]
    public void WhenGetMqttMockTopicIsCalled_WithPublishOperation_ThenExtractsOperationName()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create();

        var hivemqBuilder = builder.AddContainer("hivemq", "hivemq/hivemq-ce", "latest")
            .WithEndpoint(targetPort: 1883, name: "mqtt");

        builder.AddMicrocks("microcks-pastry")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature(minion =>
            {
                minion.WithMqttConnection(hivemqBuilder, port: 1883);
            });

        var microcksAsyncMinionResource = builder.Resources
            .OfType<MicrocksAsyncMinionResource>()
            .Single();

        // Act
        var mqttTopic = microcksAsyncMinionResource
            .GetMqttMockTopic("Pastry orders API", "0.1.0", "PUBLISH pastry/orders");

        // Assert
        Assert.Equal("PastryordersAPI-0.1.0-pastry/orders", mqttTopic);
    }
}
