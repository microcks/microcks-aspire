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

using Aspire.Microcks.Testing.Fixtures.Contract;
using Xunit;

namespace Aspire.Microcks.Testing.Features.Mocking.Contract;

/// <summary>
/// Collection definition used to share the <see cref="MicrocksContractValidationFixture"/>
/// between tests. Tests that depend on a running Microcks instance should
/// belong to this collection.
/// </summary>
[CollectionDefinition(MicrocksContractValidationCollection.CollectionName)]
public class MicrocksContractValidationCollection : ICollectionFixture<MicrocksContractValidationFixture>
{
    // Collection definition for sharing Microcks with Bad-Implementation and Good-Implementation resources.
    public const string CollectionName = "Microcks contract collection";
}
