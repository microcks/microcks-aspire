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

using Xunit;

namespace Aspire.Microcks.Testing.Fixtures.Mock;

/// <summary>
/// Collection definition used to share the <see cref="SharedMicrocksFixture"/>
/// between tests. Tests that depend on a running Microcks instance should
/// belong to this collection.
/// </summary>
[CollectionDefinition(MicrocksMockingCollection.CollectionName)]
public class MicrocksMockingCollection : ICollectionFixture<MicrocksMockingFixture>
{
    // Collection definition for sharing a single Microcks instance across tests
    public const string CollectionName = "Microcks mocking collection";
}
