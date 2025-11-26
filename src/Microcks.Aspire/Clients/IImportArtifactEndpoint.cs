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

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace Microcks.Aspire.Clients;

/// <summary>
/// Defines the endpoint for importing artifacts into Microcks.
/// </summary>
public interface IImportArtifactEndpoint
{
    /// <summary>
    /// Imports an artifact file (such as OpenAPI, AsyncAPI, or other API definitions) into Microcks.
    /// </summary>
    /// <param name="file">The artifact file to import as a stream.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An HTTP response message indicating the result of the import operation.</returns>
    [Multipart]
    [Headers("Accept: application/json")]
    [Post("/api/import")]
    Task<HttpResponseMessage> ImportArtifactAsync(
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);
}
