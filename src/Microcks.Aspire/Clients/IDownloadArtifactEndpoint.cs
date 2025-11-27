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

using Refit;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microcks.Aspire.Clients;

/// <summary>
/// Defines the endpoint for instructing Microcks to download an artifact from a remote URL.
/// </summary>
public interface IDownloadArtifactEndpoint
{
    /// <summary>
    /// Instructs Microcks to download an artifact from a remote URL and import it.
    /// </summary>
    /// <param name="mainArtifact">Indicates if the artifact is the main one.</param>
    /// <param name="url">The remote URL of the artifact.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The HTTP response from Microcks.</returns>
    [Post("/api/artifact/download")]
    [Headers("Accept: application/json")]
    Task<HttpResponseMessage> DownloadArtifactAsync(
        [Query] bool mainArtifact,
        [Query] string url,
        CancellationToken cancellationToken = default);
}
