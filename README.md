# microcks-aspire
Aspire extension that enables hosting Microcks as a service, managing mocks for dependencies and contract-testing of your API endpoints

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
    .WithImage("microcks/microcks-uber:latest");
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

With Aspire, you can do it like this using the MicrocksProvider:

```csharp
var app = orderHostAspireFactory.App;
var microcksProvider = app.CreateMicrocksProvider("microcks");

bool isVerified = await microcksProvider.VerifyAsync(
    "API Pastries", "0.0.1", 
    cancellationToken: TestContext.Current.CancellationToken);
Assert.True(isVerified, "API should be verified successfully");
```

Or check the invocations count like this:

```csharp
double initialCount = await microcksProvider.GetServiceInvocationsCountAsync(
    "API Pastries", "0.0.1", 
    cancellationToken: TestContext.Current.CancellationToken);

// ... perform your API calls ...

double finalCount = await microcksProvider.GetServiceInvocationsCountAsync(
    "API Pastries", "0.0.1", 
    cancellationToken: TestContext.Current.CancellationToken);
Assert.Equal(initialCount + expectedCalls, finalCount);
```


### Launching contract-tests with Aspire

If you want to ensure that your application under test is conformant to an OpenAPI contract (or other type of contract), you can launch a Microcks contract/conformance test using Aspire's distributed application model and MicrocksProvider integration.


Here is an example using Aspire and the Microcks extension, fully aligned with the test code:

```csharp
[Fact]
public async Task TestOpenApiContract()
{
    // Arrange
    var app = orderHostAspireFactory.App;
    int port = app.GetEndpoint("order-api").Port;

    // Act
    TestRequest request = new()
    {
        ServiceId = "Order Service API:0.1.0",
        RunnerType = TestRunnerType.OPEN_API_SCHEMA,
        TestEndpoint = $"http://host.docker.internal:{port}/api"
    };
    var microcksProvider = app.CreateMicrocksProvider("microcks");
    var testResult = await microcksProvider.TestEndpointAsync(request, TestContext.Current.CancellationToken);

    // Assert
    // You may inspect complete response object with following:
    var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
    testOutputHelper.WriteLine(json);

    Assert.True(testResult.Success);
    Assert.False(testResult.InProgress, "Test should not be in progress");
    Assert.Single(testResult.TestCaseResults);
}
```

You can also use service discovery for endpoint resolution (note: endpoint name is case-sensitive and must match your configuration, e.g., "order-api" or "Order-Api"):

```csharp
[Fact]
public async Task TestOpenApiContract_WithServiceDiscovery()
{
    var app = orderHostAspireFactory.App;
    TestRequest request = new()
    {
        ServiceId = "Order Service API:0.1.0",
        RunnerType = TestRunnerType.OPEN_API_SCHEMA,
        TestEndpoint = $"{app.GetEndpoint(\"order-api\")}/api"
    };
    var microcksProvider = app.CreateMicrocksProvider("microcks");
    var testResult = await microcksProvider.TestEndpointAsync(request, TestContext.Current.CancellationToken);
    var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
    testOutputHelper.WriteLine(json);
    Assert.True(testResult.Success);
    Assert.False(testResult.InProgress, "Test should not be in progress");
    Assert.Single(testResult.TestCaseResults);
}
```


#### Business conformance checks

You can retrieve and inspect exchanged messages for advanced business conformance validation. Here is an example that matches the test code:

```csharp
var messages = await microcksProvider.GetMessagesForTestCaseAsync(testResult, "POST /orders", TestContext.Current.CancellationToken);
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

You can configure additional features directly in your AppHost:

```csharp
var microcks = builder.AddMicrocks("microcks")
    .WithMainArtifacts("pastry-orders-asyncapi.yml")
    // Additional configuration can be added here
    ;
```

#### Postman contract-testing

For Postman contract-testing, you can use the same TestRequest pattern with the MicrocksProvider:

```csharp
var microcksProvider = app.CreateMicrocksProvider("microcks");
var testRequest = new TestRequest
{
    ServiceId = "API Pastries:0.0.1",
    RunnerType = TestRunnerType.POSTMAN,
    TestEndpoint = $"{app.GetEndpoint("my-api")}/api",
    Timeout = TimeSpan.FromSeconds(3)
};

TestResult testResult = await microcksProvider.TestEndpointAsync(testRequest, TestContext.Current.CancellationToken);
```

**Note:** With Aspire, the complexity of managing multiple containers and networks is abstracted away. The distributed application model handles service discovery, networking, and lifecycle management automatically.

### Async API contract-testing and mocking

Async APIs can also be mocked and contract-tested using Microcks with Aspire. You can import AsyncAPI artifacts and use the MicrocksProvider to validate message exchanges.

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
