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
/// A volatile OAuth2 client context usually associated with a Test request.
/// </summary>
public class OAuth2ClientContext
{
    /// <summary>
    /// Gets or sets the OAuth2 client identifier.
    /// </summary>
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 client secret.
    /// </summary>
    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }

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
    /// Gets or sets the username for OAuth2 password grant type.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for OAuth2 password grant type.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 refresh token.
    /// </summary>
    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 grant type used for authorization.
    /// </summary>
    [JsonPropertyName("grantType")]
    public OAuth2GrantType? GrantType { get; set; }
}

/// <summary>
/// A Builder to create OAuth2ClientContext using a fluid API.
/// </summary>
public class OAuth2ClientContextBuilder
{
    private readonly OAuth2ClientContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2ClientContextBuilder"/> class.
    /// </summary>
    public OAuth2ClientContextBuilder()
    {
        _context = new OAuth2ClientContext();
    }

    /// <summary>
    /// Sets the OAuth2 client identifier.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithClientId(string clientId)
    {
        _context.ClientId = clientId;
        return this;
    }

    /// <summary>
    /// Sets the OAuth2 client secret.
    /// </summary>
    /// <param name="clientSecret">The client secret.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithClientSecret(string clientSecret)
    {
        _context.ClientSecret = clientSecret;
        return this;
    }

    /// <summary>
    /// Sets the token URI used to obtain OAuth2 access tokens.
    /// </summary>
    /// <param name="tokenUri">The token URI.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithTokenUri(string tokenUri)
    {
        _context.TokenUri = tokenUri;
        return this;
    }

    /// <summary>
    /// Sets the OAuth2 scopes as a space-separated string.
    /// </summary>
    /// <param name="scopes">The OAuth2 scopes.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithScopes(string scopes)
    {
        _context.Scopes = scopes;
        return this;
    }

    /// <summary>
    /// Sets the username for OAuth2 password grant type.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithUsername(string username)
    {
        _context.Username = username;
        return this;
    }

    /// <summary>
    /// Sets the password for OAuth2 password grant type.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithPassword(string password)
    {
        _context.Password = password;
        return this;
    }

    /// <summary>
    /// Sets the OAuth2 refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithRefreshToken(string refreshToken)
    {
        _context.RefreshToken = refreshToken;
        return this;
    }

    /// <summary>
    /// Sets the OAuth2 grant type used for authorization.
    /// </summary>
    /// <param name="grantType">The OAuth2 grant type.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public OAuth2ClientContextBuilder WithGrantType(OAuth2GrantType grantType)
    {
        _context.GrantType = grantType;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="OAuth2ClientContext"/> instance.
    /// </summary>
    /// <returns>The configured OAuth2 client context.</returns>
    public OAuth2ClientContext Build()
    {
        return _context;
    }
}
