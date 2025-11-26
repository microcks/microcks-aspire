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

namespace Microcks.Aspire;

/// <summary>
/// Provides default container image tags and configuration for the Microcks container.
/// </summary>
public static class MicrocksContainerImageTags
{
    /// <summary>
    /// Name of the Microcks container image to use for testing.
    /// </summary>
    public const string Image = "microcks/microcks-uber";

    /// <summary>
    /// The default Docker image tag (version) for the Microcks container.
    /// </summary>
    public const string Tag = "1.13.0";

    /// <summary>
    /// The default Docker registry where the Microcks container image is hosted.
    /// </summary>
    public const string Registry = "quay.io";
}
