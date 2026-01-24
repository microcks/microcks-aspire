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

namespace Microcks.Aspire.Tests.Fixtures.Async.Mqtt;

/// <summary>
/// Collection definition for HiveMQ MQTT tests.
/// </summary>
[CollectionDefinition(CollectionName)]
public class MicrocksHiveMqCollection : ICollectionFixture<MicrocksHiveMqFixture>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string CollectionName = "MicrocksHiveMqCollection";
}
