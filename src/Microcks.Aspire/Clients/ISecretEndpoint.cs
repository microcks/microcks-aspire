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

using Microcks.Aspire.Clients.Model;
using Refit;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microcks.Aspire.Clients;

/// <summary>
/// Defines the endpoint for managing secrets in Microcks.
/// </summary>
public interface ISecretEndpoint
{
    /// <summary>
    /// Creates a new secret in Microcks.
    /// </summary>
    /// <param name="secret">The secret to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The HTTP response from Microcks.</returns>
    [Post("/api/secrets")]
    [Headers("Content-Type: application/json")]
    Task<HttpResponseMessage> CreateSecretAsync(
        [Body] Secret secret,
        CancellationToken cancellationToken = default);
}
