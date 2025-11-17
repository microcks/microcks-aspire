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
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting.Microcks.Clients.Model;
using Refit;

namespace Aspire.Hosting.Microcks.Clients;

/// <summary>
/// Client interface for Microcks test endpoint operations.
/// </summary>
public interface ITestEndpoint
{
    /// <summary>
    /// Tests an endpoint based on the provided test request.
    /// </summary>
    /// <param name="testRequest">The test request containing the necessary information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [Post("/api/tests")]
    Task<TestResult> TestEndpointAsync(TestRequest testRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes the test result by its ID.
    /// </summary>
    /// <param name="testResultId">ID of the test result to refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [Get("/api/tests/{testResultId}")]
    Task<TestResult> RefreshTestResultAsync(string testResultId, CancellationToken cancellationToken);


    /// <summary>
    /// Retrieves messages for a specific test case within a test result.
    /// </summary>
    /// <param name="testResultId">The ID of the test result.</param>
    /// <param name="testCaseId">The ID of the test case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of request/response pairs for the specified test case.</returns>
    [Get("/api/tests/{testResultId}/messages/{testCaseId}")]
    Task<List<RequestResponsePair>> GetMessagesForTestCaseAsync(string testResultId, string testCaseId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves unidirectional event messages for a specific test case within a test result.
    /// </summary>
    /// <param name="testResultId">The ID of the test result.</param>
    /// <param name="testCaseId">The ID of the test case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unidirectional event messages for the specified test case.</returns>
    [Get("/api/tests/{testResultId}/events/{testCaseId}")]
    Task<List<UnidirectionalEvent>> GetEventMessagesForTestCaseAsync(string testResultId, string testCaseId, CancellationToken cancellationToken);
}
