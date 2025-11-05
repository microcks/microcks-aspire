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
using System.IO;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Microcks.FileArtifacts;

internal sealed class SnapshotsAnnotation : IResourceAnnotation
{
    public string SnapshotsFilePath { get; }

    public SnapshotsAnnotation(string snapshotsFilePath)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(snapshotsFilePath, nameof(snapshotsFilePath));
        if (!File.Exists(snapshotsFilePath))
        {
            throw new FileNotFoundException($"Snapshots file not found: {snapshotsFilePath}", snapshotsFilePath);
        }
        SnapshotsFilePath = snapshotsFilePath;
    }
}
