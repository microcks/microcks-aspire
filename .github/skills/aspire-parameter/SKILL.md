---
name: aspire-parameter
description: Manage parameters and secrets in .NET Aspire applications. Use when creating, configuring, or referencing ParameterResource for credentials (username, password), connection strings, API keys, or any configuration value.
---

# aspire-parameter Skill

## Methods

```csharp
// From configuration (Parameters:name)
builder.AddParameter("name");
builder.AddParameter("name", secret: true);

// With default value
builder.AddParameter("name", "defaultValue");
builder.AddParameter("name", "value", secret: true);

// Generated password (persist keeps value stable across runs)
builder.AddParameter("pwd", new GenerateParameterDefault { MinLength = 16 }, secret: true, persist: true);
```

## Get Value

```csharp
// In WithEnvironment callback - use .Resource directly (implements IValueProvider)
var username = builder.AddParameter("user", "admin");
var password = builder.AddParameter("pass", secret: true);

builder.AddContainer("service", "image")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["USER"] = username.Resource;  // ParameterResource
        context.EnvironmentVariables["PASS"] = password.Resource;  // ParameterResource
    });

// Direct string value access (when needed)
string value = parameterBuilder.Resource.Value;
```

## Patterns

```csharp
// Database credentials
var user = builder.AddParameter("pg-user", "postgres");
var pass = builder.AddParameter("pg-pass", secret: true);
builder.AddPostgres("db").WithUserName(user).WithPassword(pass);

// Message broker
var user = builder.AddParameter("mq-user", "test", secret: true);
var pass = builder.AddParameter("mq-pass", "test", secret: true);
builder.AddRabbitMQ("mq", user, pass);

// Environment variable
var key = builder.AddParameter("api-key", secret: true);
builder.AddProject<Projects.Api>("api").WithEnvironment("API_KEY", key);
```

## Configuration

Parameters read from `Parameters:name` in: user-secrets → env vars → appsettings.json

```bash
dotnet user-secrets set "Parameters:db-password" "secret"
```

## MCP

Use Microsoft Docs MCP for latest API details:
```
mcp_microsoftdocs_microsoft_docs_search(query: "Aspire AddParameter")
```
