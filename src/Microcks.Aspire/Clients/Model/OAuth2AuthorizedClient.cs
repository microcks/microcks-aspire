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

namespace Microcks.Aspire.Clients.Model;

/// <summary>
/// Persisted information for an OAuth2 authorization/authentication done before launching a test.
/// </summary>
public class OAuth2AuthorizedClient
{
    /// <summary>
    /// Gets or sets the principal name of the authorized OAuth2 client.
    /// </summary>
    [JsonPropertyName("principalName")]
    public string? PrincipalName { get; set; }

    /// <summary>
    /// Gets or sets the token URI used to obtain OAuth2 access tokens.
    /// </summary>
    [JsonPropertyName("tokenUri")]
    public string? TokenUri { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 scopes as a space-separated string.
    /// </summary>
    [JsonPropertyName("scopes")]
    public string? Scopes { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 grant type used for authorization.
    /// </summary>
    [JsonPropertyName("grantType")]
    public OAuth2GrantType? GrantType { get; set; }
}
