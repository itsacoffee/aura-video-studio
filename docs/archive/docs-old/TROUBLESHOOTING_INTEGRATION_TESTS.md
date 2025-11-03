> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---


# Troubleshooting Integration Tests

## Common Issues and Solutions

### Test Execution Issues

#### Tests Not Running

**Symptom:** No tests execute when running test commands

**Possible Causes:**
1. Build failure
2. Test filter too restrictive
3. Test project not referenced

**Solutions:**
```bash
# 1. Check build status
dotnet build

# 2. Try running without filter
dotnet test

# 3. Verify test project is in solution
dotnet sln list
```

#### Tests Timeout

**Symptom:** Tests fail with `OperationCanceledException` or timeout errors

**Possible Causes:**
1. LLM provider unavailable
2. Network issues
3. Insufficient timeout value

**Solutions:**
```csharp
// Increase timeout in test
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5)); // Increase from default

// Or test with local provider only
var provider = new RuleBasedLlmProvider(
    NullLogger<RuleBasedLlmProvider>.Instance);
```

### Quality Analysis Issues

#### Inconsistent Quality Scores

**Symptom:** Same script produces different quality scores on different runs

**Possible Causes:**
1. AI-based analysis variability
2. Random seed issues
3. Provider differences

**Solutions:**
```csharp
// Focus on heuristic-based tests for consistency
// Use thresholds rather than exact values
Assert.True(analysis.OverallScore >= 70 && analysis.OverallScore <= 90);

// Test patterns rather than specific scores
Assert.True(analysis.PassesQualityThreshold == (analysis.OverallScore >= 75));
```

#### Quality Check Not Detecting Issues

**Symptom:** Poor quality content passes validation

**Possible Causes:**
1. Test script not actually poor quality
2. Thresholds too lenient
3. Heuristics not triggered

**Solutions:**
```csharp
// Use more obvious quality issues
var poorScript = @"
    In today's video. Delve into topic.
    It's important to note. Game changer.
    Revolutionary. Don't forget to subscribe.
";

// Verify specific issues are detected
Assert.Contains("AI", string.Join(" ", analysis.Issues), 
    StringComparison.OrdinalIgnoreCase);
```

### LLM Provider Issues

#### Provider Not Found

**Symptom:** `Provider 'X' not found` error

**Possible Causes:**
1. Provider not registered in DI
2. Incorrect provider name
3. Provider not installed

**Solutions:**
```csharp
// Check available providers
var providers = serviceProvider.GetService<IEnumerable<ILlmProvider>>();
foreach (var p in providers)
{
    Console.WriteLine(p.GetType().Name);
}

// Use correct provider name
// ❌ "OpenAi"
// ✅ "OpenAiLlmProvider"
```

#### API Key Issues

**Symptom:** Authentication errors from LLM providers

**Possible Causes:**
1. Missing API key
2. Invalid API key
3. Key not configured

**Solutions:**
```bash
# Set environment variables
export OPENAI_API_KEY="your-key-here"
export AZURE_OPENAI_KEY="your-key-here"

# Or use appsettings.json
{
  "LLM": {
    "OpenAI": {
      "ApiKey": "your-key-here"
    }
  }
}
```

### Health Check Issues

#### Health Checks Fail

**Symptom:** `/api/diagnostics/health` returns unhealthy status

**Possible Causes:**
1. Services not registered
2. Dependencies unavailable
3. Configuration issues

**Solutions:**
```csharp
// Check service registration
services.AddSingleton<IHealthCheck, LLMProviderHealthCheck>();
services.AddSingleton<IHealthCheck, ContentQualityHealthCheck>();
services.AddSingleton<SystemHealthService>();

// Verify dependencies
services.AddSingleton<IntelligentContentAdvisor>();
services.AddTransient<ILlmProvider, RuleBasedLlmProvider>();
```

#### Individual Check Fails

**Symptom:** Specific health check reports unhealthy

**Diagnostic Steps:**
```bash
# Test individual component
POST /api/diagnostics/test-provider
{
  "providerName": "RuleBased"
}

# Check configuration
GET /api/diagnostics/configuration
```

### Performance Issues

#### Tests Running Slowly

**Symptom:** Tests take much longer than expected

**Possible Causes:**
1. Using external LLM providers
2. No test parallelization
3. Memory issues

**Solutions:**
```bash
# Run tests in parallel
dotnet test --parallel

# Use local provider for performance tests
var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

# Profile test execution
dotnet test --logger "console;verbosity=detailed"
```

#### Memory Test Failures

**Symptom:** Memory usage exceeds thresholds

**Possible Causes:**
1. Memory leaks
2. Large object retention
3. GC not running

**Solutions:**
```csharp
// Force GC before measurement
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

// Check for leaks
var initialMemory = GC.GetTotalMemory(true);
// ... run tests ...
var finalMemory = GC.GetTotalMemory(true);
```

### Compilation Issues

#### Namespace Conflicts

**Symptom:** `The type or namespace name 'X' does not exist`

**Possible Causes:**
1. Missing using statements
2. Namespace ambiguity
3. Assembly reference missing

**Solutions:**
```csharp
// Use type aliases to avoid conflicts
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

// Or fully qualify types
var pacing = Aura.Core.Models.Pacing.Conversational;
```

#### Build Errors in Tests

**Symptom:** Test project won't build

**Solutions:**
```bash
# Clean and rebuild
dotnet clean
dotnet build

# Check project references
dotnet list reference

# Restore packages
dotnet restore
```

### Integration Issues

#### Services Not Wired Up

**Symptom:** `NullReferenceException` when accessing services

**Possible Causes:**
1. DI not configured
2. Service not registered
3. Wrong service lifetime

**Solutions:**
```csharp
// Use SystemIntegrityValidator to check
var validator = new SystemIntegrityValidator(logger, serviceProvider);
var result = await validator.ValidateAsync();

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

#### EnhancedPromptTemplates Not Working

**Symptom:** Empty or invalid prompts generated

**Solutions:**
```csharp
// Validate prompt templates
var validator = new PromptTemplateValidator(logger);
var result = await validator.ValidateAsync();

Assert.True(result.IsValid, 
    string.Join(", ", result.Errors));
```

## Diagnostic Tools

### Running Diagnostics API

```bash
# Start the API
cd Aura.Api
dotnet run

# Test endpoints
curl http://localhost:5000/api/diagnostics/health
curl http://localhost:5000/api/diagnostics/configuration
```

### Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Aura.Core.AI": "Debug",
      "Aura.Core.Services": "Debug"
    }
  }
}
```

### Test-Specific Debugging

```csharp
[Fact]
public async Task Debug_ProviderIssue()
{
    var factory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Debug);
    });
    
    var logger = factory.CreateLogger<RuleBasedLlmProvider>();
    var provider = new RuleBasedLlmProvider(logger);
    
    // Now you'll see debug output
    var result = await provider.DraftScriptAsync(brief, spec);
}
```

## Environment-Specific Issues

### CI/CD Pipeline Failures

**Symptom:** Tests pass locally but fail in CI/CD

**Common Causes:**
1. Environment variables not set
2. Different .NET SDK version
3. Missing dependencies

**Solutions:**
```yaml
# GitHub Actions example
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'

- name: Set environment variables
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}

- name: Run tests
  run: dotnet test --filter "FullyQualifiedName~Smoke"
```

### Docker Container Issues

**Symptom:** Tests fail in Docker but work locally

**Solutions:**
```dockerfile
# Ensure all dependencies are installed
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet build
RUN dotnet test --filter "FullyQualifiedName~Smoke"
```

## Getting Help

### Information to Provide

When reporting issues, include:
1. Test name and full output
2. .NET SDK version (`dotnet --version`)
3. OS and environment
4. Relevant configuration (sanitized)
5. Steps to reproduce

### Useful Commands

```bash
# Get test details
dotnet test --list-tests --filter "FullyQualifiedName~YourTest"

# Run single test with full output
dotnet test --filter "FullyQualifiedName~YourTest" --logger "console;verbosity=detailed"

# Check system info
dotnet --info

# Verify project structure
tree /Aura.Tests
```

### Debug Mode Test Execution

```bash
# Run tests with debugger
dotnet test --filter "FullyQualifiedName~YourTest" --logger "console;verbosity=detailed" -- RunConfiguration.DebugTrace=true
```

## Prevention Tips

### 1. Run Tests Frequently
```bash
# Before committing
dotnet test --filter "FullyQualifiedName~Smoke"
```

### 2. Use Local Providers in Development
```csharp
// Development: Use RuleBased
var provider = new RuleBasedLlmProvider(logger);

// Production: Use configured provider
var provider = serviceProvider.GetRequiredService<ILlmProvider>();
```

### 3. Keep Tests Independent
```csharp
// ❌ Don't rely on test order
[Fact] public void Test1() { _sharedState = "x"; }
[Fact] public void Test2() { Assert.Equal("x", _sharedState); }

// ✅ Each test is self-contained
[Fact] public void Test1() { var state = "x"; /* use state */ }
[Fact] public void Test2() { var state = "x"; /* use state */ }
```

### 4. Use Appropriate Timeouts
```csharp
// Short timeout for fast operations
[Fact(Timeout = 1000)] // 1 second
public async Task FastTest() { /* ... */ }

// Longer timeout for LLM operations
[Fact(Timeout = 30000)] // 30 seconds
public async Task LlmTest() { /* ... */ }
```

## Quick Reference

### Test Filters
```bash
# All integration tests
--filter "FullyQualifiedName~Integration"

# Specific test class
--filter "FullyQualifiedName~AIQualitySystemIntegrationTests"

# Specific test method
--filter "FullyQualifiedName~Should_DetectLowQuality"

# Multiple filters
--filter "FullyQualifiedName~Integration|FullyQualifiedName~Smoke"
```

### Common Test Patterns
```csharp
// Arrange-Act-Assert
[Fact]
public async Task Pattern()
{
    // Arrange
    var sut = CreateSystemUnderTest();
    
    // Act
    var result = await sut.DoSomething();
    
    // Assert
    Assert.NotNull(result);
}

// Theory with data
[Theory]
[InlineData("value1")]
[InlineData("value2")]
public void DataDriven(string value) { /* ... */ }
```
