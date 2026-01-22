---
name: dotnet-package
description: Manage NuGet packages using dotnet CLI commands. Use when adding, removing, or updating packages in .NET projects. Never edit .csproj or Directory.Packages.props files directly.
---

# dotnet-package Skill

## Core Rules

1. **NEVER** directly edit `.csproj`, `.props`, or `Directory.Packages.props` files
2. **ALWAYS** use `dotnet` CLI commands for all package operations
3. **VERIFY** changes with `dotnet restore` or `dotnet build`

## Commands

### Add Package

```bash
dotnet add [<PROJECT>] package <PACKAGE_NAME> [--version <VERSION>]
```

**Latest version** (no --version flag):
```bash
dotnet add src/Microcks.Aspire/Microcks.Aspire.csproj package Aspire.Hosting.Kafka
```

**Specific version**:
```bash
dotnet add src/Microcks.Aspire/Microcks.Aspire.csproj package Testcontainers --version 3.7.0
```

### Remove Package

```bash
dotnet remove [<PROJECT>] package <PACKAGE_NAME>
```

Example:
```bash
dotnet remove tests/Microcks.Aspire.Tests/Microcks.Aspire.Tests.csproj package Refit
```

### Update Package

Use `dotnet add package` with `--version` to update existing packages:

```bash
dotnet add <PROJECT> package <PACKAGE_NAME> --version <VERSION>
```

**When version not specified**, find latest:
```bash
LATEST_VERSION=$(dotnet package search <PACKAGE_NAME> --exact-match --format json | jq -r '.searchResult[].packages[0].version')
dotnet add <PROJECT> package <PACKAGE_NAME> --version $LATEST_VERSION
```

### Verify Changes

Always verify after operations:
```bash
dotnet restore
# or
dotnet build
```

## Examples

**Add latest version:**
```bash
dotnet add src/Microcks.Aspire/Microcks.Aspire.csproj package Aspire.Hosting.Redis
```

**Add specific version:**
```bash
dotnet add tests/Microcks.Aspire.Testing/Microcks.Aspire.Testing.csproj package Aspire.Hosting.Testing --version 9.1.0
```

**Update to specific version:**
```bash
dotnet add src/Microcks.Aspire/Microcks.Aspire.csproj package Testcontainers --version 3.9.0
dotnet restore
```

**Update to latest version:**
```bash
LATEST_VERSION=$(dotnet package search Aspire.Hosting.Kafka --exact-match --format json | jq -r '.searchResult[].packages[0].version')
dotnet add src/Microcks.Aspire/Microcks.Aspire.csproj package Aspire.Hosting.Kafka --version $LATEST_VERSION
dotnet restore
```

**Remove package:**
```bash
dotnet remove tests/Microcks.Aspire.Tests/Microcks.Aspire.Tests.csproj package Refit
```

## Notes

- Central Package Management (`Directory.Packages.props`) is handled automatically by the CLI
- `dotnet add package` without `--version` uses latest stable version
- Failures leave project files unchanged, ensuring integrity