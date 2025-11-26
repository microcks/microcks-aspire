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
/// Represents a test request that will be executed against a service endpoint in Microcks.
/// </summary>
public class TestRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the service to test.
    /// </summary>
    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; set; }

    /// <summary>
    /// Gets or sets the test runner type to use for executing the test.
    /// </summary>
    [JsonPropertyName("runnerType")]
    public TestRunnerType RunnerType { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL where the service under test is running.
    /// </summary>
    [JsonPropertyName("testEndpoint")]
    public string? TestEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the timeout duration for the test execution.
    /// </summary>
    [JsonPropertyName("timeout")]
    [JsonConverter(typeof(TimeSpanToMillisecondsConverter))]
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets the list of operations to test. If null or empty, all operations will be tested.
    /// </summary>
    [JsonPropertyName("filteredOperations")]
    public List<string>? FilteredOperations { get; set; }

    /// <summary>
    /// Gets or sets custom headers to include for specific operations during the test.
    /// </summary>
    [JsonPropertyName("operationsHeaders")]
    public Dictionary<string, List<Header>>? OperationsHeaders { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 client context for authentication during the test.
    /// </summary>
    [JsonPropertyName("oAuth2Context")]
    public OAuth2ClientContext? oAuth2Context { get; set; }
}
