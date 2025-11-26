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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microcks.Aspire.Clients.Model;

/// <summary>
/// Represents the result of a test case execution for a specific API operation.
/// </summary>
public class TestCaseResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the test case execution was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in milliseconds for the test case execution.
    /// </summary>
    [JsonPropertyName("elapsedTime")]
    public int ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets the name of the operation that was tested.
    /// </summary>
    [JsonPropertyName("operationName")]
    public string? OperationName { get; set; }

    /// <summary>
    /// Gets or sets the list of individual test step results that comprise this test case.
    /// </summary>
    [JsonPropertyName("testStepResults")]
    public List<TestStepResult>? TestStepResults { get; set; }
}
