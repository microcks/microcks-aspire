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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microcks.Aspire.Clients.Converter;

namespace Microcks.Aspire.Clients.Model;

/// <summary>
/// TestResult model for Microcks.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the test result.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the version number of the test result.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the test number in the sequence of tests executed.
    /// </summary>
    [JsonPropertyName("testNumber")]
    public int TestNumber { get; set; }

    /// <summary>
    /// Gets or sets the timestamp (in milliseconds since epoch) when the test was executed.
    /// </summary>
    [JsonPropertyName("testDate")]
    public long TestDate { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL that was tested.
    /// </summary>
    [JsonPropertyName("testedEndpoint")]
    public string? TestedEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the service that was tested.
    /// </summary>
    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; set; }

    /// <summary>
    /// Gets or sets the timeout duration for the test execution.
    /// </summary>
    [JsonConverter(typeof(TimeSpanToMillisecondsConverter))]
    [JsonPropertyName("timeout")]
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in milliseconds for the entire test execution.
    /// </summary>
    [JsonPropertyName("elapsedTime")]
    public int ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test execution was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test is currently in progress.
    /// </summary>
    [JsonPropertyName("inProgress")]
    public bool InProgress { get; set; }

    /// <summary>
    /// Gets or sets the type of test runner used to execute the test.
    /// </summary>
    [JsonPropertyName("runnerType")]
    public TestRunnerType RunnerType { get; set; }

    /// <summary>
    /// Gets or sets the list of test case results for each operation tested.
    /// </summary>
    [JsonPropertyName("testCaseResults")]
    public List<TestCaseResult>? TestCaseResults { get; set; }

    /// <summary>
    /// Gets or sets custom headers that were included for specific operations during the test.
    /// </summary>
    [JsonPropertyName("operationsHeaders")]
    public Dictionary<string, List<Header>>? OperationsHeaders { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 authorized client information if authentication was used.
    /// </summary>
    [JsonPropertyName("authorizedClient")]
    public OAuth2AuthorizedClient? AuthorizedClient { get; set; }
}
