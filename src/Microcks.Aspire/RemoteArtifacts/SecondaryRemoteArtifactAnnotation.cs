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

namespace Microcks.Aspire.RemoteArtifacts;

/// <summary>
/// Annotation for secondary remote artifact in a Microcks resource.
/// </summary>
internal sealed class SecondaryRemoteArtifactAnnotation : IRemoteArtifactAnnotation
{
    /// <summary>
    /// Gets the secondary remote artifact.
    /// </summary>
    public RemoteArtifact RemoteArtifact { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecondaryRemoteArtifactAnnotation"/> class.
    /// </summary>
    /// <param name="remoteArtifact">The secondary remote artifact.</param>
    public SecondaryRemoteArtifactAnnotation(RemoteArtifact remoteArtifact)
    {
        ArgumentNullException.ThrowIfNull(remoteArtifact);
        RemoteArtifact = remoteArtifact;
    }
}
