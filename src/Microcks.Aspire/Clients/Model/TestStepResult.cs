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

using System.Text.Json.Serialization;

namespace Microcks.Aspire.Clients.Model;

/// <summary>
/// Represents the result of an individual test step execution within a test case.
/// </summary>
public class TestStepResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the test step execution was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in milliseconds for the test step execution.
    /// </summary>
    [JsonPropertyName("elapsedTime")]
    public int ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets the name of the request that was executed in this test step.
    /// </summary>
    [JsonPropertyName("requestName")]
    public string? RequestName { get; set; }

    /// <summary>
    /// Gets or sets the name of the event message associated with this test step.
    /// </summary>
    [JsonPropertyName("eventMessageName")]
    public string? EventMessageName { get; set; }

    /// <summary>
    /// Gets or sets a message providing additional details about the test step result (e.g., error message if failed).
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
