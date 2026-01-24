---
name: aspire-test-strategy
description: Guide for choosing between builder tests (fast, no containers) and integration tests (full container startup) when writing or optimizing tests in the Microcks Aspire project. Use when creating new tests, optimizing existing tests, or when the user asks about test performance or structure.
---

# Aspire Test Strategy

## Overview

This skill guides the selection between **builder tests** (fast, no containers) and **integration tests** (full container startup) to optimize test execution time in Aspire-based projects.

## Core Principle

**Always choose the lightest test approach that validates the requirement.**

- Builder tests are milliseconds fast - use them for configuration verification
- Integration tests are seconds/minutes slow - use them only when runtime behavior must be validated

## Decision Matrix

### Use Builder Tests When

Testing aspects that can be verified at **build/configuration time**:

- ✅ Resource registration and naming
- ✅ Resource dependencies and relationships  
- ✅ Environment variables configuration
- ✅ Annotations presence and values
- ✅ Resource builder chaining
- ✅ Parameter wiring and references

**Pattern:**
```csharp
public void WhenApplicationIsBuilt_ThenResourcesAreConfigured()
{
    var builder = DistributedApplication.CreateBuilder();
    // ... configure resources
    using var app = builder.Build();
    var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
    // ... verify configuration without starting containers
}
```

### Use Integration Tests When

Testing aspects that require **runtime behavior**:

- ✅ Actual container startup and health
- ✅ Network communication between services
- ✅ Message publishing and consumption
- ✅ HTTP endpoints responding correctly
- ✅ Database connections and queries
- ✅ End-to-end workflows

**Pattern:**
```csharp
[Collection("FixtureName")]
public class IntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task WhenMessageIsSent_ThenItIsReceived()
    {
        // Uses fixture with started containers
        // ... test runtime behavior
    }
}
```

## Implementation Guidelines

### When Splitting Existing Tests

If a test file mixes configuration and runtime concerns:

1. Create a new `*BuilderTests.cs` file for configuration tests
2. Keep runtime tests in the original `*Tests.cs` file with fixture
3. Builder tests should NOT use collections or fixtures that start containers

### Naming Conventions

- **Builder tests**: `*BuilderTests.cs` - Tests configuration without starting containers
- **Integration tests**: `*Tests.cs` - Tests runtime behavior with containers

### Example Refactoring

**Before (slow):**
```csharp
[Collection(MicrocksAmqpCollection.CollectionName)] // Starts containers!
public class MicrocksAmqpTests(MicrocksAmqpFixture fixture)
{
    [Fact]
    public void WhenApplicationIsStarted_ThenResourcesExist()
    {
        // Just checking configuration but starting containers unnecessarily
        var appModel = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.NotNull(appModel.Resources.OfType<MicrocksAsyncMinionResource>().Single());
    }
}
```

**After (fast):**
```csharp
// MicrocksAmqpBuilderTests.cs - No fixture, no containers
public class MicrocksAmqpBuilderTests
{
    [Fact]
    public void WhenApplicationIsBuilt_ThenResourcesAreConfigured()
    {
        var builder = DistributedApplication.CreateBuilder();
        // ... configure
        using var app = builder.Build();
        // ... verify configuration
    }
}
```

## When Creating New Tests

1. **Start with builder tests** for configuration aspects
2. **Add integration tests** only for features requiring runtime validation
3. **Keep builder tests separate** from integration test files

This approach ensures fast feedback loops while maintaining comprehensive coverage.

[TODO: Choose the structure that best fits this skill's purpose. Common patterns:

**1. Workflow-Based** (best for sequential processes)
- Works well when there are clear step-by-step procedures
- Example: DOCX skill with "Workflow Decision Tree" → "Reading" → "Creating" → "Editing"
- Structure: ## Overview → ## Workflow Decision Tree → ## Step 1 → ## Step 2...

**2. Task-Based** (best for tool collections)
- Works well when the skill offers different operations/capabilities
- Example: PDF skill with "Quick Start" → "Merge PDFs" → "Split PDFs" → "Extract Text"
- Structure: ## Overview → ## Quick Start → ## Task Category 1 → ## Task Category 2...

**3. Reference/Guidelines** (best for standards or specifications)
- Works well for brand guidelines, coding standards, or requirements
- Example: Brand styling with "Brand Guidelines" → "Colors" → "Typography" → "Features"
- Structure: ## Overview → ## Guidelines → ## Specifications → ## Usage...

**4. Capabilities-Based** (best for integrated systems)
- Works well when the skill provides multiple interrelated features
- Example: Product Management with "Core Capabilities" → numbered capability list
- Structure: ## Overview → ## Core Capabilities → ### 1. Feature → ### 2. Feature...

Patterns can be mixed and matched as needed. Most skills combine patterns (e.g., start with task-based, add workflow for complex operations).

Delete this entire "Structuring This Skill" section when done - it's just guidance.]

## [TODO: Replace with the first main section based on chosen structure]

[TODO: Add content here. See examples in existing skills:
- Code samples for technical skills
- Decision trees for complex workflows
- Concrete examples with realistic user requests
- References to scripts/templates/references as needed]

## Resources

This skill includes example resource directories that demonstrate how to organize different types of bundled resources:

### scripts/
Executable code (Python/Bash/etc.) that can be run directly to perform specific operations.

**Examples from other skills:**
- PDF skill: `fill_fillable_fields.py`, `extract_form_field_info.py` - utilities for PDF manipulation
- DOCX skill: `document.py`, `utilities.py` - Python modules for document processing

**Appropriate for:** Python scripts, shell scripts, or any executable code that performs automation, data processing, or specific operations.

**Note:** Scripts may be executed without loading into context, but can still be read by Claude for patching or environment adjustments.

### references/
Documentation and reference material intended to be loaded into context to inform Claude's process and thinking.

**Examples from other skills:**
- Product management: `communication.md`, `context_building.md` - detailed workflow guides
- BigQuery: API reference documentation and query examples
- Finance: Schema documentation, company policies

**Appropriate for:** In-depth documentation, API references, database schemas, comprehensive guides, or any detailed information that Claude should reference while working.

### assets/
Files not intended to be loaded into context, but rather used within the output Claude produces.

**Examples from other skills:**
- Brand styling: PowerPoint template files (.pptx), logo files
- Frontend builder: HTML/React boilerplate project directories
- Typography: Font files (.ttf, .woff2)

**Appropriate for:** Templates, boilerplate code, document templates, images, icons, fonts, or any files meant to be copied or used in the final output.

---

**Any unneeded directories can be deleted.** Not every skill requires all three types of resources.
