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

using System.Text.Json.Serialization;
using Microcks.Aspire.Clients.Converter;

namespace Microcks.Aspire.Clients.Model;

/// <summary>
/// A Header definition.
/// </summary>
public sealed class Header
{
    /// <summary>
    /// Gets or sets the name of the header.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the values of the header as a comma-separated string.
    /// </summary>
    [JsonPropertyName("values")]
    [JsonConverter(typeof(ArrayToStringConverter))]
    public string? Values { get; set; }
}
