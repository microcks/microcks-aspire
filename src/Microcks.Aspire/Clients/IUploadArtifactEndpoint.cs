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

/// <summary>Upload an artifact</summary>
public interface IUploadArtifactEndpoint
{
    [Multipart]
    [Post("/api/artifact/upload?mainArtifact={mainArtifact}")]
    [Headers("Accept: application/json")]
    Task<HttpResponseMessage> UploadArtifactAsync(
        [Query] bool mainArtifact,
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);
}
