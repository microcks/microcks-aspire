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
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using Microcks.Aspire.Async;
using Microcks.Aspire.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microcks.Aspire.Tests.Features.Async;

/// <summary>
/// Tests for verifying the correct resolution for Microcks Async Minion resources
/// </summary>
public sealed class MicrocksAsyncMinionImageTests
{
    /// <summary>
    /// Ensures that when configuring a Microcks Async Minion, the correct image and tag are resolved
    /// based on the main Microcks image.
    /// </summary>
    [Theory]
    [InlineData("quay.io/microcks/microcks-uber:1.12.1", "quay.io/microcks/microcks-uber-async-minion", "1.12.1")]
    [InlineData("docker.io/microcks/microcks-uber:1.13.0-native", "docker.io/microcks/microcks-uber-async-minion", "1.13.0")]
    public void WhenMicrocksAsyncMinionIsConfigured_ThenImageAndTagAreResolved(string image, string expectedImage, string expectedTag)
    {
        var builder = TestDistributedApplicationBuilder.Create(o => { });
        // Microcks with AsyncMinion
        var microcksBuilder = builder.AddMicrocks("microcks-pastry")
            .WithImage(image)
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature();

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var microcksAsyncMinionResources = appModel.Resources.OfType<MicrocksAsyncMinionResource>();
        MicrocksAsyncMinionResource asyncMinionResource = Assert.Single(microcksAsyncMinionResources);

        var asyncMinionContainerImageAnnotation = asyncMinionResource.Annotations.OfType<ContainerImageAnnotation>();
        var asyncMinionContainerImage = Assert.Single(asyncMinionContainerImageAnnotation);

        Assert.Equal(expectedImage, asyncMinionContainerImage.Image);
        Assert.Equal(expectedTag, asyncMinionContainerImage.Tag);
    }

    /// <summary>
    /// Ensures that if an Async Minion resource already exists, no additional one is created.
    /// </summary>
    [Fact]
    public void WhenAsyncMinionResourceAlreadyExists_ThenNoAdditionalAsyncMinionIsCreated()
    {
        using var builder = TestDistributedApplicationBuilder.Create(o => { });

        // Microcks with AsyncMinion
        var microcksBuilder = builder.AddMicrocks("microcks-pastry")
            .WithImage("quay.io/microcks/microcks-uber:1.12.1")
            .WithMainArtifacts(
                Path.Combine(AppContext.BaseDirectory, "resources", "pastry-orders-asyncapi.yml")
            )
            .WithAsyncFeature()
            .WithAsyncFeature(); // Attempt to add Async Minion again

        // Uniquely one Async Minion resource should exist
        Assert.Single(builder.Resources.OfType<MicrocksAsyncMinionResource>());
    }
}
