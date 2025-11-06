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
using Aspire.Hosting.Microcks.Clients.Converter;

namespace Aspire.Hosting.Microcks.Clients.Model;

public class TestRequest
{
    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; }

    [JsonPropertyName("runnerType")]
    public TestRunnerType RunnerType { get; set; }

    [JsonPropertyName("testEndpoint")]
    public string TestEndpoint { get; set; }

    [JsonPropertyName("timeout")]
    [JsonConverter(typeof(TimeSpanToMillisecondsConverter))]
    public TimeSpan Timeout { get; set; }

    [JsonPropertyName("filteredOperations")]
    public List<string> FilteredOperations { get; set; }

    [JsonPropertyName("operationsHeaders")]
    public Dictionary<string, List<Header>> OperationsHeaders { get; set; }

    [JsonPropertyName("oAuth2Context")]
    public OAuth2ClientContext oAuth2Context { get; set; }
}
