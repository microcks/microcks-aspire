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
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Clients.Model;

namespace Microcks.Aspire.Secrets;

/// <summary>
/// Builder for creating <see cref="Secret"/> instances.
/// </summary>
public class SecretBuilder
{
    private string _name = string.Empty;
    private string? _description;
    private ParameterResource? _tokenParameter;
    private ParameterResource? _tokenHeaderParameter;
    private ParameterResource? _usernameParameter;
    private ParameterResource? _passwordParameter;
    private ParameterResource? _caCertPemParameter;

    internal SecretBuilder()
    {
    }

    /// <summary>
    /// Sets the name of the secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description of the secret.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the token for the secret using a parameter resource.
    /// </summary>
    /// <param name="token">The token parameter resource.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithToken(IResourceBuilder<ParameterResource> token)
    {
        _tokenParameter = token.Resource;
        return this;
    }

    /// <summary>
    /// Sets the token header for the secret.
    /// </summary>
    /// <param name="tokenHeader">The token header parameter resource.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithTokenHeader(IResourceBuilder<ParameterResource> tokenHeader)
    {
        _tokenHeaderParameter = tokenHeader.Resource;
        return this;
    }

    /// <summary>
    /// Sets the username for the secret using a parameter resource.
    /// </summary>
    /// <param name="username">The username parameter resource.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithUsername(IResourceBuilder<ParameterResource> username)
    {
        _usernameParameter = username.Resource;
        return this;
    }

    /// <summary>
    /// Sets the password for the secret using a parameter resource.
    /// </summary>
    /// <param name="password">The password parameter resource.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithPassword(IResourceBuilder<ParameterResource> password)
    {
        _passwordParameter = password.Resource;
        return this;
    }

    /// <summary>
    /// Sets the CA certificate in PEM format.
    /// </summary>
    /// <param name="caCertPem">The CA certificate parameter resource.</param>
    /// <returns>The builder instance.</returns>
    public SecretBuilder WithCaCertPem(IResourceBuilder<ParameterResource> caCertPem)
    {
        _caCertPemParameter = caCertPem.Resource;
        return this;
    }

    /// <summary>
    /// Builds the secret configuration.
    /// </summary>
    /// <param name="builder">The Microcks resource builder.</param>
    /// <returns>The secret configuration.</returns>
    public MicrocksSecretResource Build(IResourceBuilder<MicrocksResource> builder)
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException("Secret name must be provided.");
        }

        var microcksSecretResource = new MicrocksSecretResource(
            _name,
            _description,
            builder.Resource,
            _usernameParameter,
            _passwordParameter,
            _tokenParameter,
            _tokenHeaderParameter,
            _caCertPemParameter);

        return microcksSecretResource;
    }
}
