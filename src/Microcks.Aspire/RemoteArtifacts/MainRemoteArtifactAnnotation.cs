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

namespace Microcks.Aspire.RemoteArtifacts;

/// <summary>
/// Annotation for main remote artifact in a Microcks resource.
/// </summary>
internal sealed class MainRemoteArtifactAnnotation : IRemoteArtifactAnnotation
{
    /// <summary>
    /// Gets the main remote artifact.
    /// </summary>
    public RemoteArtifact RemoteArtifact { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainRemoteArtifactAnnotation"/> class.
    /// </summary>
    /// <param name="remoteArtifact">The main remote artifact.</param>
    public MainRemoteArtifactAnnotation(RemoteArtifact remoteArtifact)
    {
        ArgumentNullException.ThrowIfNull(remoteArtifact, nameof(remoteArtifact));
        RemoteArtifact = remoteArtifact;
    }
}
