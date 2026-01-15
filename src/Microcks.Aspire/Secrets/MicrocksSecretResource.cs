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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Clients.Model;

namespace Microcks.Aspire.Secrets;

/// <summary>
/// Represents a Microcks secret resource.
/// </summary>
public sealed class MicrocksSecretResource : Resource,
    IResourceWithParent<MicrocksResource>
{
    private readonly MicrocksResource _parent;
    private readonly string? _description;
    private readonly ParameterResource? _usernameParameter;
    private readonly ParameterResource? _passwordParameter;
    private readonly ParameterResource? _tokenParameter;
    private readonly ParameterResource? _tokenHeaderParameter;
    private readonly ParameterResource? _caCertPemParameter;

    /// <inheritdoc />
    public MicrocksResource Parent => _parent;

    /// <inheritdoc />
    public string? Description => _description;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrocksSecretResource"/> class.
    /// </summary>
    public MicrocksSecretResource(
        string name,
        string? description,
        MicrocksResource resource,
        ParameterResource? usernameParameter,
        ParameterResource? passwordParameter,
        ParameterResource? tokenParameter,
        ParameterResource? tokenHeaderParameter,
        ParameterResource? caCertPemParameter)
        : base(name)
    {
        _parent = resource;
        _description = description;
        _usernameParameter = usernameParameter;
        _passwordParameter = passwordParameter;
        _tokenParameter = tokenParameter;
        _tokenHeaderParameter = tokenHeaderParameter;
        _caCertPemParameter = caCertPemParameter;
    }

    /// <summary>
    /// Builds a <see cref="Secret"/> instance by resolving all parameter values.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Secret"/> with resolved values.</returns>
    public async Task<Secret> BuildSecretAsync(CancellationToken cancellationToken = default)
    {
        var secret = new Secret
        {
            Name = Name,
            Description = _description
        };

        if (_usernameParameter is not null)
        {
            secret.Username = await _usernameParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_passwordParameter is not null)
        {
            secret.Password = await _passwordParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_tokenParameter is not null)
        {
            secret.Token = await _tokenParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_tokenHeaderParameter is not null)
        {
            secret.TokenHeader = await _tokenHeaderParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_caCertPemParameter is not null)
        {
            secret.CaCertPem = await _caCertPemParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        return secret;
    }

}