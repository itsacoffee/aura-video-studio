# Aura.Api Modularization Guide

## Overview

This guide documents the modularization refactoring of Aura.Api/Program.cs (originally 4013 lines) into maintainable, testable modules following best practices.

## Architecture

### Directory Structure

```
Aura.Api/
├── Configuration/           # Options classes for typed configuration
│   ├── HealthChecksOptions.cs
│   ├── EnginesOptions.cs
│   ├── PerformanceOptions.cs
│   ├── LlmTimeoutsOptions.cs
│   ├── PromptEngineeringOptions.cs
│   └── ValidationOptions.cs
├── Endpoints/              # Modular endpoint groups by domain
│   ├── HealthEndpoints.cs
│   ├── CapabilitiesEndpoints.cs
│   ├── SettingsEndpoints.cs
│   └── [Additional endpoint modules...]
├── Startup/                # DI and configuration extensions
│   ├── ServiceCollectionExtensions.cs
│   ├── CoreServicesExtensions.cs
│   ├── ProviderServicesExtensions.cs
│   ├── OrchestratorServicesExtensions.cs
│   ├── RemainingServicesExtensions.cs
│   └── LoggingConfiguration.cs
└── Program.cs              # Minimal bootstrapping code
```

### Options Pattern

All configuration sections from `appsettings.json` are mapped to strongly-typed Options classes:

```csharp
// Configuration/HealthChecksOptions.cs
public sealed class HealthChecksOptions
{
    public double DiskSpaceThresholdGB { get; set; } = 1.0;
    public double DiskSpaceCriticalGB { get; set; } = 0.5;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

// Program.cs - Registration
builder.Services.AddApplicationOptions(builder.Configuration);
```

**Benefits:**
- Type safety and IntelliSense
- Validation at startup
- Easier testing with mock configurations
- No magic strings for configuration keys

### Service Registration Pattern

Services are organized by domain into extension methods:

```csharp
// Startup/ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.AddCoreServices();
    services.AddProviderServices();
    services.AddOrchestratorServices(configuration);
    services.AddHealthServices();
    // ... additional domains
    return services;
}
```

**Benefits:**
- Clear separation of concerns
- Easier to find and modify service registrations
- Better testability
- Reduces Program.cs complexity

### Endpoint Modules Pattern

Each domain has its own endpoint module with route registrations:

```csharp
// Endpoints/HealthEndpoints.cs
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/health/live", (HealthCheckService healthService) =>
        {
            var result = healthService.CheckLiveness();
            return Results.Ok(result);
        })
        .WithName("HealthLive")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Check API liveness";
            operation.Description = "Returns a simple liveness indicator.";
            return operation;
        })
        .Produces<object>(200);

        // Additional endpoints...
        return endpoints;
    }
}
```

**Benefits:**
- Domain-driven organization
- Self-contained endpoint logic
- Easier to write focused unit tests
- Better OpenAPI documentation
- Clear ownership of routes

### OpenAPI Enhancement

All endpoints include:
- **Summary**: Short description of the endpoint
- **Description**: Detailed explanation
- **Produces**: Expected response types and status codes
- **Tags**: Logical grouping in Swagger UI

```csharp
.WithOpenApi(operation =>
{
    operation.Summary = "Get system health status";
    operation.Description = "Performs dependency checks and returns health status.";
    operation.Tags = new[] { new OpenApiTag { Name = "Health" } };
    return operation;
})
.Produces<HealthResponse>(200)
.Produces(503)
.ProducesProblem(500);
```

## Implementation Status

### ✅ Completed

- [x] Directory structure created
- [x] Options classes for all configuration sections
- [x] Service registration extension methods (by domain)
- [x] Logging configuration extracted
- [x] Health endpoints module (demonstration)
- [x] Capabilities endpoints module
- [x] Settings endpoints module
- [x] Build validation passes

### 🔄 In Progress

- [ ] Remaining 47 endpoint groups modularization
- [ ] Program.cs refactoring to use modular structure
- [ ] Unit tests for endpoint modules
- [ ] OpenAPI spec generation as build artifact

### 📋 Pending

- [ ] Integration tests for modular endpoints
- [ ] Performance benchmarking (before/after)
- [ ] Documentation updates
- [ ] Migration guide for future endpoints

## How to Add a New Endpoint Module

### Step 1: Create Endpoint Module

```csharp
// Endpoints/MyDomainEndpoints.cs
namespace Aura.Api.Endpoints;

public static class MyDomainEndpoints
{
    public static IEndpointRouteBuilder MapMyDomainEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/mydomain/resource", (MyService service) =>
        {
            // Endpoint logic
        })
        .WithName("GetMyDomainResource")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get domain resource";
            return operation;
        })
        .Produces<MyResponse>(200);

        return endpoints;
    }
}
```

### Step 2: Register in Program.cs

```csharp
// Program.cs
app.MapMyDomainEndpoints();
```

### Step 3: Add Unit Tests

```csharp
// Tests/Endpoints/MyDomainEndpointsTests.cs
public class MyDomainEndpointsTests
{
    [Fact]
    public async Task GetResource_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<MyService>();
        // ... setup

        // Act & Assert
        // ... test logic
    }
}
```

## Testing Strategy

### Unit Tests

Test each endpoint module independently:

```csharp
public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health/live");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_WhenHealthy_Returns200()
    {
        // Arrange with healthy dependencies
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock healthy dependencies
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_WhenUnhealthy_Returns503()
    {
        // Arrange with unhealthy dependencies
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock unhealthy dependencies
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
```

### Integration Tests

Test complete workflows across multiple endpoints:

```csharp
[Collection("Integration")]
public class VideoGenerationWorkflowTests
{
    [Fact]
    public async Task CompleteVideoGeneration_WithValidInput_Succeeds()
    {
        // 1. Check capabilities
        // 2. Create brief
        // 3. Generate script
        // 4. Synthesize TTS
        // 5. Compose video
        // 6. Verify output
    }
}
```

## OpenAPI Configuration

### Swagger Generation

Configured in `ServiceCollectionExtensions`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aura Video Studio API",
        Version = "v1",
        Description = "AI-powered video generation API"
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add operation filters
    options.OperationFilter<CorrelationIdOperationFilter>();
});
```

### Build Artifact

Generate `openapi.json` during build:

```xml
<!-- Aura.Api.csproj -->
<Target Name="GenerateOpenApiSpec" AfterTargets="Build">
  <Exec Command="dotnet swagger tofile --output $(OutputPath)openapi.json $(OutputPath)Aura.Api.dll v1" />
</Target>
```

## Benefits of Modularization

### Developer Experience
- **Faster navigation**: Find endpoints by domain
- **Easier code review**: Smaller, focused PRs
- **Better onboarding**: Clear structure for new developers
- **Reduced merge conflicts**: Changes isolated to specific modules

### Code Quality
- **Testability**: Unit test individual endpoint modules
- **Maintainability**: Single Responsibility Principle
- **Type safety**: Options pattern eliminates magic strings
- **Documentation**: OpenAPI integrated at endpoint level

### Performance
- **No runtime impact**: Same compiled code
- **Better build caching**: Smaller files = faster incremental builds
- **Parallel development**: Multiple devs can work on different modules

## Migration Notes

### Backward Compatibility

All routes remain identical:
- ✅ Same HTTP methods
- ✅ Same paths
- ✅ Same request/response formats
- ✅ Same behavior

### Breaking Changes

**None.** This is a pure refactoring with no functional changes.

### Rollback Strategy

If issues arise:
1. Revert to `Program.cs.backup`
2. Remove new Endpoints/ and Startup/ directories
3. Rebuild and deploy

## Future Enhancements

1. **Rate Limiting per Endpoint**: Module-specific rate limits
2. **Endpoint Versioning**: `/api/v2/health/live`
3. **Feature Flags**: Enable/disable modules dynamically
4. **Per-Module Middleware**: Domain-specific authentication
5. **Code Generation**: Generate endpoint modules from OpenAPI spec

## References

- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Options Pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [OpenAPI (Swagger)](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)

## Questions or Issues

For questions about the modularization approach, refer to:
- Architecture Decision Records in `/docs/adr/`
- Team wiki: Aura API Architecture
- Slack: #aura-api-dev
