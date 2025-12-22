# Microcks Aspire .NET

Aspire extension that enables hosting Microcks as a service, managing mocks for dependencies and contract-testing of your API endpoints in your .NET App.

[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/microcks/microcks-aspire/cicd.yml?logo=github&style=for-the-badge)](https://github.com/microcks/microcks-aspire/actions)
[![Version](https://img.shields.io/nuget/v/Microcks.Aspire?color=blue&style=for-the-badge)](https://www.nuget.org/packages/Microcks.Aspire)
[![License](https://img.shields.io/github/license/microcks/microcks-testcontainers-java?style=for-the-badge&logo=apache)](https://www.apache.org/licenses/LICENSE-2.0)
[![Project Chat](https://img.shields.io/badge/discord-microcks-pink.svg?color=7289da&style=for-the-badge&logo=discord)](https://microcks.io/discord-invite/)
[![Artifact HUB](https://img.shields.io/endpoint?url=https://artifacthub.io/badge/repository/microcks-uber-image&style=for-the-badge)](https://artifacthub.io/packages/search?repo=microcks-uber-image)
[![CNCF Landscape](https://img.shields.io/badge/CNCF%20Landscape-5699C6?style=for-the-badge&logo=cncf)](https://landscape.cncf.io/?item=app-definition-and-development--application-definition-image-build--microcks)

## Build Status

Current development version is `0.1.0`.

#### Sonarcloud Quality metrics

[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=microcks_microcks-aspire)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=microcks_microcks-aspire)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=bugs)](https://sonarcloud.io/summary/new_code?id=microcks_microcks-aspire)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=coverage)](https://sonarcloud.io/summary/new_code?id=microcks_microcks-aspire)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=microcks_microcks-aspire)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=microcks_microcks)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=microcks_microcks-aspire&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=microcks_microcks-aspire)

#### Fossa license and security scans

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fmicrocks%2Fmicrocks-aspire.svg?type=shield&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Fmicrocks%2Fmicrocks-aspire?ref=badge_shield&issueType=license)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fmicrocks%2Fmicrocks-aspire.svg?type=shield&issueType=security)](https://app.fossa.com/projects/git%2Bgithub.com%2Fmicrocks%2Fmicrocks-aspire?ref=badge_shield&issueType=security)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fmicrocks%2Fmicrocks-aspire.svg?type=small)](https://app.fossa.com/projects/git%2Bgithub.com%2Fmicrocks%2Fmicrocks-aspire?ref=badge_small)

#### OpenSSF best practices on Microcks core

[![CII Best Practices](https://bestpractices.coreinfrastructure.org/projects/7513/badge)](https://bestpractices.coreinfrastructure.org/projects/7513)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/microcks/microcks/badge)](https://securityscorecards.dev/viewer/?uri=github.com/microcks/microcks)

## Community

-   [Documentation](https://microcks.io/documentation/tutorials/getting-started/)
-   [Microcks Community](https://github.com/microcks/community) and community meeting
-   Join us on [Discord](https://microcks.io/discord-invite/), on [GitHub Discussions](https://github.com/orgs/microcks/discussions) or [CNCF Slack #microcks channel](https://cloud-native.slack.com/archives/C05BYHW1TNJ)

To get involved with our community, please make sure you are familiar with the project's [Code of Conduct](./CODE_OF_CONDUCT.md).

## How to use it?

### Include it into your project dependencies

```
dotnet add package Microcks.Aspire --version 0.1.0
```

### Setup with Aspire

With Aspire, you add Microcks as a resource to your distributed application. This library requires a Microcks `uber` distribution (with no MongoDB dependency).

In your AppHost Program.cs:

```csharp
var microcks = builder.AddMicrocks("microcks")
    .WithImage("microcks/microcks-uber", "latest");
```

### Import content in Microcks

To use Microcks mocks or contract-testing features, you first need to import OpenAPI, Postman Collection, GraphQL or gRPC artifacts.
Artifacts can be imported as main/Primary ones or as secondary ones. See [Multi-artifacts support](https://microcks.io/documentation/using/importers/#multi-artifacts-support) for details.

With Aspire, you import artifacts directly when adding the Microcks resource to your distributed application:

```csharp
var microcks = builder.AddMicrocks("microcks")
    .WithMainArtifacts(
        "resources/third-parties/apipastries-openapi.yaml",
        "resources/order-service-openapi.yaml"
    )
    .WithSecondaryArtifacts(
        "resources/order-service-postman-collection.json",
        "resources/third-parties/apipastries-postman-collection.json"
    );
```

You can also import full [repository snapshots](https://microcks.io/documentation/administrating/snapshots/) at once:

```csharp
var microcks = builder.AddMicrocks("microcks")
    .WithSnapshots("microcks-repository.json");
```

### Using mock endpoints for your dependencies

During your test setup, you'd probably need to retrieve mock endpoints provided by Microcks to
setup your base API url calls. With Aspire, you can do it like this:

```csharp
// From the MicrocksResource directly
var pastryApiUrl = microcksResource
    .GetRestMockEndpoint("API Pastries", "0.0.1")
    .ToString();
```

The MicrocksResource provides methods for different supported API styles/protocols (Soap, GraphQL, gRPC,...).

You can then use these endpoints to configure your HTTP clients in tests:

```csharp
var webApplicationFactory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("PastryApi:BaseUrl", pastryApiUrl);
    });
```

### Verifying mock endpoint has been invoked

Once the mock endpoint has been invoked, you'd probably need to ensure that the mock have been really invoked.

With Aspire, you can do it like this using the MicrocksClient:

```csharp
var app = orderHostAspireFactory.App;
var microcksClient = app.CreateMicrocksClient("microcks");

bool isVerified = await microcksClient.VerifyAsync(
    "API Pastries", "0.0.1", 
    cancellationToken: TestContext.Current.CancellationToken);
Assert.True(isVerified, "API should be verified successfully");
```

Or check the invocations count like this:

```csharp
double initialCount = await microcksClient.GetServiceInvocationsCountAsync(
    "API Pastries", "0.0.1", 
    cancellationToken: TestContext.Current.CancellationToken);

// ... perform your API calls ...

double finalCount = await microcksClient.GetServiceInvocationsCountAsync(
    "API Pastries", "0.0.1", 
    cancellationToken: TestContext.Current.CancellationToken);
Assert.Equal(initialCount + expectedCalls, finalCount);
```


### Launching contract-tests with Aspire

If you want to ensure that your application under test is conformant to an OpenAPI contract (or other type of contract), you can launch a Microcks contract/conformance test using Aspire's distributed application model and MicrocksClient integration.

For container-to-host communication, use `GetEndpointForNetwork` to retrieve an endpoint URL accessible from the Aspire container network, allowing Microcks (running in a container) to reach your application under test.

**Prerequisites:**
- Set the `ASPIRE_ENABLE_CONTAINER_TUNNEL` environment variable to `true` in your launch profile or environment.

> **Note:** The universal container-to-host communication is currently an experimental feature and needs to be enabled via environment variable. This fixes long-standing issues with container-to-host communication ([issue #6547](https://github.com/dotnet/aspire/issues/6547)).

Example in `launchSettings.json`:
```json
{
  "profiles": {
    "YourTestProject": {
      "environmentVariables": {
        "ASPIRE_ENABLE_CONTAINER_TUNNEL": "true"
      }
    }
  }
}
```

**Usage:**

```csharp
[Fact]
public async Task TestOpenApiContract()
{
    // Arrange
    var app = _fixture.App;

    // Use GetEndpointForNetwork with the container network context so that Microcks (running in a container)
    // can access the order-api service from the Aspire container network
    Uri endpoint = app.GetEndpointForNetwork("order-api", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);

    TestRequest request = new()
    {
        ServiceId = "Order Service API:0.1.0",
        RunnerType = TestRunnerType.OPEN_API_SCHEMA,
        TestEndpoint = $"{endpoint.Scheme}://{endpoint.Host}:{endpoint.Port}/api",
    };

    var microcksClient = app.CreateMicrocksClient("microcks");
    var testResult = await microcksClient.TestEndpointAsync(request, TestContext.Current.CancellationToken);

    // Assert
    // You may inspect complete response object with following:
    var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
    testOutputHelper.WriteLine(json);

    Assert.True(testResult.Success);
    Assert.False(testResult.InProgress, "Test should not be in progress");
    Assert.Single(testResult.TestCaseResults);
}
```

> **Note:** `KnownNetworkIdentifiers.DefaultAspireContainerNetwork` refers to the default Aspire container network (`aspire.dev.internal`).


#### Business conformance checks

You can retrieve and inspect exchanged messages for advanced business conformance validation. Here is an example that matches the test code:

```csharp
var messages = await microcksClient.GetMessagesForTestCaseAsync(testResult, "POST /orders", TestContext.Current.CancellationToken);
foreach (var message in messages)
{
    if ("201".Equals(message.Response.Status))
    {
        var responseDocument = JsonDocument.Parse(message.Response.Content);
        var requestDocument = JsonDocument.Parse(message.Request.Content);
        var requestProductQuantities = requestDocument.RootElement.GetProperty("productQuantities");
        var responseProductQuantities = responseDocument.RootElement.GetProperty("productQuantities");
        Assert.Equal(requestProductQuantities.GetArrayLength(), responseProductQuantities.GetArrayLength());
        for (int i = 0; i < requestProductQuantities.GetArrayLength(); i++)
        {
            var reqProductName = requestProductQuantities[i].GetProperty("productName").GetString();
            var respProductName = responseProductQuantities[i].GetProperty("productName").GetString();
            Assert.Equal(reqProductName, respProductName);
        }
    }
}
```


**Note:**
- With Aspire, endpoint discovery and Microcks integration are handled automatically. You do not need to manually expose ports or refactor your hosting model as with classic Testcontainers usage.
- Always ensure endpoint names (e.g., "order-api" or "Order-Api") match your distributed application configuration.
- Use `TestContext.Current.CancellationToken` for async test cancellation, and consider logging the serialized test result for easier debugging.

### Advanced features with Aspire

The Microcks Aspire extension supports essential features of Microcks:

-   Mocking of REST APIs using different kinds of artifacts,
-   Contract-testing of REST APIs using `OPEN_API_SCHEMA` runner/strategy,
-   Mocking and contract-testing of SOAP WebServices,
-   Mocking and contract-testing of GraphQL APIs,
-   Mocking and contract-testing of gRPC APIs.

With Aspire, all these features are automatically available when you add the Microcks resource to your distributed application. The integration handles the orchestration and networking automatically.

#### Advanced Configuration

You can configure additional features directly in your AppHost,
to support more advanced use-cases like AsyncAPI contract-testing or Postman contract-testing, we introduced dedicated extension methods for the MicrocksBuilder that allow managing additional Microcks services.

[Different levels of API contract testing](https://medium.com/@lbroudoux/different-levels-of-api-contract-testing-with-microcks-ccc0847f8c97)
in the Inner Loop with Testcontainers and available in Microcks .NET Aspire!

```csharp
var microcks = builder.AddMicrocks("microcks")
    .WithMainArtifacts("pastry-orders-asyncapi.yml")
    // Additional configuration can be added here
    .WithPostmanRunner(); // Enables Postman contract-testing support
```

#### Postman contract-testing

For Postman contract-testing, you can use the same TestRequest pattern with the MicrocksClient:

```csharp
var microcksClient = app.CreateMicrocksClient("microcks");
var testRequest = new TestRequest
{
    ServiceId = "API Pastries:0.0.1",
    RunnerType = TestRunnerType.POSTMAN,
    TestEndpoint = $"{app.GetEndpoint("my-api")}/api",
    Timeout = TimeSpan.FromSeconds(3)
};

TestResult testResult = await microcksClient.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);
```

If you want to configure Postman runner specific settings, you can do it when adding the Postman runner to the Microcks resource:

```csharp
var microcks = builder.AddMicrocks("microcks")
    .WithPostmanRunner(postmanBuilder =>
    {
        // Customize Postman runner settings
        postmanBuilder.WithEnvironment("MY_ENV_VAR", "sample");
    });
```

**Note:** With Aspire, the complexity of managing multiple containers and networks is abstracted away. The distributed application model handles service discovery, networking, and lifecycle management automatically.

### Async API contract-testing and mocking

Async APIs can also be mocked and contract-tested using Microcks with Aspire. You can import AsyncAPI artifacts and use the MicrocksClient to validate message exchanges.

This feature requires to be explicitly enabled when adding the Microcks resource.
It's done by calling the `WithAsyncFeature()` method as shown below:

```csharp
var kafkaBuilder = Builder.AddKafka("kafka")
    .WithKafkaUI();
var microcksBuilder = builder.AddMicrocks("microcks")
    .WithMainArtifacts("pastry-orders-asyncapi.yml")
    .WithAsyncFeature(minion => {
        minion.WithKafkaConnection(kafkaBuilder, 9093);
    });
```

As you can see, we inject the `KafkaBuilder` into the minion to ensure it can connect properly.

##### Using mock endpoints for your dependencies

Once started, you can retrieve mock topics from the `MicrocksAsyncMinionResource` for different
supported protocols (WebSocket, Kafka, etc.). Here's how to consume messages sent by the Microcks Async Minion:

```csharp
// Retrieve MicrocksAsyncMinionResource from application
var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
var microcksAsyncMinionResource = appModel.GetContainerResources()
    .OfType<MicrocksAsyncMinionResource>()
    .Single();

// Get the Kafka topic for mock messages
string kafkaTopic = microcksAsyncMinionResource
    .GetKafkaMockTopic("Pastry orders API", "0.1.0", "SUBSCRIBE pastry/orders");

// Subscribe to the topic and consume messages
consumer.Subscribe(kafkaTopic);
var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(5000));
```

##### Launching new contract-tests

Using contract-testing techniques on Asynchronous endpoints may require a different style of interacting with the Microcks
container. For example, you may need to:

1. Start the test making Microcks listen to the target async endpoint,
2. Activate your System Under Tests so that it produces an event,
3. Finalize the Microcks tests and actually ensure you received one or many well-formed events.

For that you can use the `MicrocksClient` which provides a `TestEndpointAsync(TestRequest request)` method that returns a `Task<TestResult>`.
Once invoked, you may trigger your application events and then `await` the future result to assert like this:

```csharp
var testRequest = new TestRequest
{
    ServiceId = "Pastry orders API:0.1.0",
    RunnerType = TestRunnerType.ASYNC_API_SCHEMA,
    TestEndpoint = "kafka://kafka:9093/pastry-orders",
    Timeout = TimeSpan.FromSeconds(5)
};

var microcksClient = app.CreateMicrocksClient("microcks");

// Start the test, making Microcks listen to the endpoint
var taskTestResult = microcksClient.TestEndpointAsync(testRequest, cancellationToken);

// Wait a bit to let the test initialize
await Task.Delay(750, cancellationToken);

// Produce your events to the Kafka topic
producer.Produce("pastry-orders", new Message<string, string>
{
    Key = Guid.NewGuid().ToString(),
    Value = yourMessage
});

// Now retrieve the final test result and assert
TestResult testResult = await taskTestResult;
Assert.True(testResult.Success);
```

##### Retrieving event messages

In addition, you can use the `GetEventMessagesForTestCaseAsync()` method to retrieve the events received during the test. This is particularly useful for inspecting message content and validating business logic:

```csharp
// Retrieve event messages for the failing test case
List<UnidirectionalEvent> events = await microcksClient.GetEventMessagesForTestCaseAsync(
    testResult, "SUBSCRIBE pastry/orders", TestContext.Current.CancellationToken);

// Inspect the events
Assert.True(events.Count >= 1);

// Check event message content
foreach (var eventItem in events)
{
    Assert.NotNull(eventItem.EventMessage);
    var messageContent = eventItem.EventMessage.Content;
    
    // Parse and validate message structure
    var jsonDocument = JsonDocument.Parse(messageContent);
    var root = jsonDocument.RootElement;
    
    // Validate required fields
    Assert.True(root.TryGetProperty("id", out _));
    Assert.True(root.TryGetProperty("customerId", out _));
    Assert.True(root.TryGetProperty("productQuantities", out _));
}
```

This allows developers to perform detailed validation of the async messages exchanged during contract testing.
