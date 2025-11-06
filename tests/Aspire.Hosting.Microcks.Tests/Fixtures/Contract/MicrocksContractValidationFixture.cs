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

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Microcks.Testing.Fixtures.Contract;

/// <summary>
/// Example derived fixture that adds two container resources (bad/good implementations)
/// to the shared distributed application builder before Microcks is configured.
/// </summary>
public sealed class MicrocksContractValidationFixture : SharedMicrocksFixture
{
    private const string BAD_PASTRY_IMAGE = "quay.io/microcks/contract-testing-demo:01";
    private const string GOOD_PASTRY_IMAGE = "quay.io/microcks/contract-testing-demo:02";

    protected override void ConfigureBuilder(TestDistributedApplicationBuilder builder)
    {
        // Add bad implementation container
        var badImpl = new ContainerResource("bad-impl");
        builder.AddResource(badImpl)
            .WithImage(BAD_PASTRY_IMAGE)
            .WithHttpEndpoint(targetPort: 3001, name: "http");

        // Add good implementation container
        var goodImpl = new ContainerResource("good-impl");
        builder.AddResource(goodImpl)
            .WithImage(GOOD_PASTRY_IMAGE)
            .WithHttpEndpoint(targetPort: 3002, name: "http");
    }

}
