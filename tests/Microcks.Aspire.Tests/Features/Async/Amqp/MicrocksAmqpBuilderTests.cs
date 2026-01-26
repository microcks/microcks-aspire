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

using System.Linq;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async.Amqp;

/// <summary>
/// Tests for the Microcks Async Minion with RabbitMQ (AMQP) resource builder configuration.
/// These tests verify the builder configuration without starting the application.
/// </summary>
public sealed class MicrocksAmqpBuilderTests
{
    /// <summary>
    /// When the application is built with Microcks and Async Minion for RabbitMQ,
    /// then the MicrocksAsyncMinionResource and RabbitMQ are properly configured.
    /// </summary>
    [Fact]
    public void WhenApplicationIsBuilt_ThenMicrocksAsyncResourceAndRabbitMQAreConfigured()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        
        var username = builder.AddParameter("username", () => "test", secret: true);
        var password = builder.AddParameter("password", () => "test", secret: true);

        // Add RabbitMQ
        var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password);

        // Add Microcks with Async Minion for RabbitMQ
        var microcks = builder.AddMicrocks("microcks")
            .WithAsyncFeature(minion =>
            {
                minion.WithAmqpConnection(rabbitmq, username, password);
            });

        string expectedAsyncMinionName = $"{microcks.Resource.Name}-async-minion";

        // Build the application model (without starting it)
        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert - Check Microcks Async Minion
        var microcksAsyncMinionResources = appModel.Resources.OfType<MicrocksAsyncMinionResource>();
        MicrocksAsyncMinionResource asyncMinionResource = Assert.Single(microcksAsyncMinionResources);
        Assert.Equal(expectedAsyncMinionName, asyncMinionResource.Name);

        // Assert - Check RabbitMQ resource
        var rabbitmqResources = appModel.Resources.Where(r => r.Name == "rabbitmq");
        var rabbitmqResource = Assert.Single(rabbitmqResources);
        Assert.Equal("rabbitmq", rabbitmqResource.Name);
    }
}
