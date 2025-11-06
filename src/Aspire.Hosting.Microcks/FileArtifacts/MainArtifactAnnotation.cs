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

internal sealed class MainArtifactAnnotation : IResourceAnnotation
{
    public string SourcePath { get; }

    public MainArtifactAnnotation(string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(sourcePath, nameof(sourcePath));

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Artifact file not found: {sourcePath}");
        }

        SourcePath = sourcePath;
    }
}
