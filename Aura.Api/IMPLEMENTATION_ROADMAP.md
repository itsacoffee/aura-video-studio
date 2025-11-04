# Aura.Api Modularization - Implementation Roadmap

## Quick Start for Developers

This document provides a step-by-step roadmap for completing the modularization of Aura.Api.

## Current State (as of this PR)

### ✅ Completed Infrastructure

1. **Configuration Layer**: 6 Options classes ready to use
2. **Service Registration**: 6 extension files organizing all 200+ services
3. **Logging**: Extracted to `LoggingConfiguration.cs`
4. **Endpoint Modules**: 3 complete examples (13 endpoints total)
5. **Testing Pattern**: Demonstrated with `SettingsEndpointsTests.cs`
6. **Documentation**: Two comprehensive guides (24KB total)

### 📋 Remaining Work

**37 endpoints** need to be modularized following the established pattern.

## How to Modularize an Endpoint

### Step-by-Step Guide

#### 1. Identify Endpoints to Modularize

Look in `Program.cs.backup` for endpoint definitions:
```csharp
apiGroup.MapPost("/your/endpoint", async (...) => { ... })
```

#### 2. Create Endpoint Module File

```bash
# Create new file
touch Aura.Api/Endpoints/YourDomainEndpoints.cs
```

#### 3. Copy Template

```csharp
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Endpoints;

/// <summary>
/// [Domain] endpoints for [description].
/// </summary>
public static class YourDomainEndpoints
{
    /// <summary>
    /// Maps [domain] endpoints to the API route group.
    /// </summary>
    public static IEndpointRouteBuilder MapYourDomainEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        // Copy endpoint definitions from Program.cs.backup here
        // Update them to include:
        // - .WithName("OperationName")
        // - .WithOpenApi(operation => { ... })
        // - .Produces<T>(statusCode)

        return endpoints;
    }
}
```

#### 4. Copy Endpoint Logic

Copy the endpoint from `Program.cs.backup` and enhance with OpenAPI:

```csharp
group.MapGet("/your/resource", async (YourService service, CancellationToken ct) =>
{
    try
    {
        // Copy existing logic from Program.cs.backup
        var result = await service.DoSomethingAsync(ct);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error in your endpoint");
        return Results.Problem("Error message", statusCode: 500);
    }
})
.WithName("YourOperationName")
.WithOpenApi(operation =>
{
    operation.Summary = "Short description";
    operation.Description = "Detailed explanation";
    return operation;
})
.Produces<YourResponseType>(200)
.ProducesProblem(500);
```

#### 5. Create Unit Tests

```csharp
// Aura.Tests/Endpoints/YourDomainEndpointsTests.cs
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests.Endpoints;

public class YourDomainEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public YourDomainEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task YourEndpoint_WithValidInput_ReturnsOk()
    {
        // Arrange
        var request = new { /* your request */ };

        // Act
        var response = await _client.PostAsJsonAsync("/api/your/resource", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task YourEndpoint_WithInvalidInput_ReturnsBadRequest()
    {
        // Test validation
    }
}
```

#### 6. Register in Program.cs

Once **all** endpoints in a domain are modularized, add to `Program.cs`:

```csharp
// After app is built
app.MapYourDomainEndpoints();
```

#### 7. Test

```bash
# Build
dotnet build Aura.Api/Aura.Api.csproj

# Run tests
dotnet test Aura.Tests/Aura.Tests.csproj --filter "YourDomainEndpointsTests"

# Manual test
dotnet run --project Aura.Api/Aura.Api.csproj
# Open http://localhost:5005/swagger
```

#### 8. Remove from Program.cs.backup

Once tested and working, the old endpoint can be commented out in the original Program.cs.

## Endpoint Migration Checklist

Track progress by domain:

### Domain 1: Planning (2 endpoints) - Priority: High
- [ ] `/api/plan` - POST - Create timeline plan
- [ ] `/api/planner/recommendations` - POST - Get planner recommendations
- [ ] Create `PlanningEndpoints.cs`
- [ ] Create `PlanningEndpointsTests.cs`
- [ ] Add `app.MapPlanningEndpoints()`

### Domain 2: Scripting (1 endpoint) - Priority: High
- [ ] `/api/script` - POST - Generate script from brief
- [ ] Create `ScriptingEndpoints.cs`
- [ ] Create `ScriptingEndpointsTests.cs`
- [ ] Add `app.MapScriptingEndpoints()`

### Domain 3: TTS (6 endpoints) - Priority: High
- [ ] `/api/tts` - POST - Synthesize speech
- [ ] `/api/captions/generate` - POST - Generate captions
- [ ] `/api/tts/azure/voices` - GET - List Azure voices
- [ ] `/api/tts/azure/voice/{voiceId}/capabilities` - GET - Get voice capabilities
- [ ] `/api/tts/azure/preview` - POST - Preview Azure TTS
- [ ] `/api/tts/azure/synthesize` - POST - Synthesize with Azure
- [ ] Create `TtsEndpoints.cs`
- [ ] Create `TtsEndpointsTests.cs`
- [ ] Add `app.MapTtsEndpoints()`

### Domain 4: Render (2 endpoints) - Priority: High
- [ ] `/api/jobs/{jobId}/stream` - GET - Stream job progress (SSE)
- [ ] `/api/logs/stream` - GET - Stream logs (SSE)
- [ ] Create `RenderEndpoints.cs`
- [ ] Create `RenderEndpointsTests.cs`
- [ ] Add `app.MapRenderEndpoints()`

### Domain 5: Diagnostics (4 endpoints) - Priority: Medium
- [ ] `/api/diagnostics` - GET - Get diagnostics HTML
- [ ] `/api/diagnostics/json` - GET - Get diagnostics JSON
- [ ] `/api/logs` - GET - Get log entries
- [ ] `/api/logs/open-folder` - POST - Open logs folder
- [ ] Create `DiagnosticsEndpoints.cs`
- [ ] Create `DiagnosticsEndpointsTests.cs`
- [ ] Add `app.MapDiagnosticsEndpoints()`

### Domain 6: Profiles (2 endpoints) - Priority: Medium
- [ ] `/api/profiles/list` - GET - List available profiles
- [ ] `/api/profiles/apply` - POST - Apply profile
- [ ] Create `ProfilesEndpoints.cs`
- [ ] Create `ProfilesEndpointsTests.cs`
- [ ] Add `app.MapProfilesEndpoints()`

### Domain 7: ML (1 endpoint) - Priority: Low
- [ ] `/api/ml/train/frame-importance` - POST - Train ML model
- [ ] Create `MLEndpoints.cs`
- [ ] Create `MLEndpointsTests.cs`
- [ ] Add `app.MapMLEndpoints()`

### Domain 8: Assets (4 endpoints) - Priority: Medium
- [ ] `/api/assets/search` - POST - Search for assets
- [ ] `/api/assets/generate` - POST - Generate assets
- [ ] `/api/assets/stock/providers` - GET - List stock providers
- [ ] `/api/assets/stock/quota/{provider}` - GET - Get provider quota
- [ ] Create `AssetsEndpoints.cs`
- [ ] Create `AssetsEndpointsTests.cs`
- [ ] Add `app.MapAssetsEndpoints()`

### Domain 9: Downloads (8 endpoints) - Priority: Medium
- [ ] `/api/downloads/manifest` - GET - Get downloads manifest
- [ ] `/api/downloads/{component}/status` - GET - Get download status
- [ ] `/api/downloads/{component}/install` - POST - Install component
- [ ] `/api/downloads/{component}/verify` - GET - Verify installation
- [ ] `/api/downloads/{component}/repair` - POST - Repair installation
- [ ] `/api/downloads/{component}` - DELETE - Uninstall component
- [ ] `/api/downloads/{component}/folder` - GET - Open component folder
- [ ] `/api/downloads/{component}/manual` - GET - Get manual install instructions
- [ ] Create `DownloadsEndpoints.cs`
- [ ] Create `DownloadsEndpointsTests.cs`
- [ ] Add `app.MapDownloadsEndpoints()`

### Domain 10: Dependencies (1 endpoint) - Priority: Low
- [ ] `/api/dependencies/rescan` - POST - Rescan dependencies
- [ ] Create `DependenciesEndpoints.cs`
- [ ] Create `DependenciesEndpointsTests.cs`
- [ ] Add `app.MapDependenciesEndpoints()`

### Domain 11: Providers (6 endpoints) - Priority: Medium
- [ ] `/api/apikeys/save` - POST - Save API keys
- [ ] `/api/apikeys/load` - GET - Load API keys
- [ ] `/api/providers/paths/save` - POST - Save provider paths
- [ ] `/api/providers/paths/load` - GET - Load provider paths
- [ ] `/api/providers/test/{provider}` - POST - Test provider
- [ ] `/api/providers/validate` - POST - Validate providers
- [ ] Create `ProvidersEndpoints.cs`
- [ ] Create `ProvidersEndpointsTests.cs`
- [ ] Add `app.MapProvidersEndpoints()`

## Final Program.cs Refactoring

Once all endpoints are modularized, refactor `Program.cs`:

### Before (Current - 4013 lines)
```csharp
// Hundreds of lines of service registrations
builder.Services.AddSingleton<ServiceA>();
builder.Services.AddSingleton<ServiceB>();
// ... 200+ more

// Hundreds of lines of endpoint definitions
apiGroup.MapGet("/endpoint1", ...);
apiGroup.MapPost("/endpoint2", ...);
// ... 50 more

var app = builder.Build();
app.Run();
```

### After (Target - ~100 lines)
```csharp
using Aura.Api.Endpoints;
using Aura.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
LoggingConfiguration.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    EnumJsonConverters.AddToOptions(options.SerializerOptions);
});

// Add services
builder.Services.AddApplicationOptions(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

// Add framework services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure middleware pipeline
app.UseCors();
app.UseExceptionHandler();
app.UseStaticFiles();

// Map endpoints
app.MapControllers();
app.MapHealthEndpoints();
app.MapCapabilitiesEndpoints();
app.MapSettingsEndpoints();
app.MapPlanningEndpoints();
app.MapScriptingEndpoints();
app.MapTtsEndpoints();
app.MapRenderEndpoints();
app.MapDiagnosticsEndpoints();
app.MapProfilesEndpoints();
app.MapMLEndpoints();
app.MapAssetsEndpoints();
app.MapDownloadsEndpoints();
app.MapDependenciesEndpoints();
app.MapProvidersEndpoints();

// Configure Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
```

## Validation Checklist

Before marking migration complete:

### Build Validation
- [ ] `dotnet build` succeeds with 0 errors
- [ ] No new warnings introduced
- [ ] Build time comparable to baseline

### Test Validation
- [ ] All existing tests pass
- [ ] New endpoint tests pass
- [ ] Integration tests pass
- [ ] Smoke test complete video generation workflow

### API Validation
- [ ] Generate OpenAPI spec before/after
- [ ] Compare specs for parity
- [ ] All routes return expected responses
- [ ] No breaking changes to request/response formats

### Frontend Validation
- [ ] Frontend can connect to API
- [ ] All features work end-to-end
- [ ] No console errors related to API calls

### Performance Validation
- [ ] Response times within baseline
- [ ] No memory leaks
- [ ] Load testing shows no degradation

## Tips for Success

### DO:
- ✅ Follow the established pattern exactly
- ✅ Copy logic precisely from Program.cs.backup
- ✅ Add comprehensive OpenAPI documentation
- ✅ Write tests before marking complete
- ✅ Test manually with Swagger UI
- ✅ Keep commits focused (one domain per commit)

### DON'T:
- ❌ Change endpoint logic or behavior
- ❌ Modify request/response formats
- ❌ Skip OpenAPI documentation
- ❌ Skip unit tests
- ❌ Modularize partially (complete one domain at a time)

## Estimated Effort

Based on the 3 completed endpoint groups:

- **Simple endpoint** (settings): ~30 minutes
- **Medium endpoint** (capabilities): ~45 minutes
- **Complex endpoint** (health with auto-fix): ~60 minutes

**Total estimated effort**: 20-30 hours for remaining 37 endpoints

**Recommended approach**:
- Tackle high-priority domains first (Planning, Scripting, TTS, Render)
- Work on one domain per session
- Get PR reviewed per domain or per 2-3 related domains

## Getting Help

If you encounter issues:

1. **Check examples**: `Aura.Api/Endpoints/HealthEndpoints.cs`
2. **Read guide**: `MODULARIZATION_GUIDE.md`
3. **Check tests**: `Aura.Tests/Endpoints/SettingsEndpointsTests.cs`
4. **Compare with backup**: `Program.cs.backup` shows original implementation

## Success Criteria

Modularization is complete when:

- [x] All 50 endpoints modularized (13/50 done)
- [ ] Program.cs reduced to ~100 lines
- [ ] All tests pass
- [ ] OpenAPI spec generated
- [ ] Frontend integration verified
- [ ] Documentation updated

---

**Let's build a maintainable, testable, world-class API! 🚀**
