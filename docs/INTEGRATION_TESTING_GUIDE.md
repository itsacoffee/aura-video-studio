# Integration Testing and System Validation Guide

## Overview

This guide explains the comprehensive integration testing and validation system for Aura Video Studio, particularly focusing on the AI quality enhancement system introduced in PR #38.

## Test Structure

### Integration Tests (`/Aura.Tests/Integration/`)

#### AIQualitySystemIntegrationTests
Tests the integration between `EnhancedPromptTemplates` and `IntelligentContentAdvisor`.

**Key Tests:**
- `EnhancedPromptTemplates_Should_GenerateValidScriptPrompt` - Validates prompt generation
- `IntelligentContentAdvisor_Should_DetectLowQualityContent` - Tests quality detection
- `IntelligentContentAdvisor_Should_ApproveHighQualityContent` - Tests quality approval
- `EnhancedPromptTemplates_Should_GenerateQualityValidationPrompt` - Validates quality prompts
- `IntelligentContentAdvisor_Should_HandleTimeout` - Tests timeout handling

#### LLMProviderIntegrationTests
Tests all LLM providers (OpenAI, Ollama, Azure, Gemini, RuleBased) with enhanced prompts.

**Key Tests:**
- `RuleBasedLlmProvider_Should_WorkWithEnhancedPrompts` - Tests local provider
- `AllProviders_Should_ProduceAnalyzableContent` - Cross-provider compatibility
- `LlmProviders_Should_RespectPacing` - Tests pacing parameter handling
- `LlmProviders_Should_RespectDensity` - Tests density parameter handling
- `LlmProvider_Should_HandleVeryShortVideos` - Edge case testing
- `LlmProvider_Should_HandleLongVideos` - Large content testing

#### EndToEndVideoGenerationTests
Complete workflow tests from brief to final video generation.

**Key Tests:**
- `CompleteWorkflow_Should_GenerateVideoFromBrief` - Full pipeline test
- `CompleteWorkflow_Should_HandleMultipleContentTypes` - Multi-category testing
- `CompleteWorkflow_Should_RespectTargetDuration` - Duration validation
- `CompleteWorkflow_Should_ProvideQualityFeedback` - Quality analysis integration
- `CompleteWorkflow_Should_SupportMultipleLanguages` - Language support

#### ContentQualityPipelineTests
Tests the quality analysis → regeneration → validation loop.

**Key Tests:**
- `QualityPipeline_Should_IdentifyAndImproveContent` - Improvement workflow
- `QualityPipeline_Should_DetectAIGeneratedPatterns` - AI detection
- `QualityPipeline_Should_ValidateQualityThreshold` - Threshold validation
- `QualityPipeline_Should_PreventPoorContentProgression` - Gating logic

#### MultiProviderConsistencyTests
Ensures consistent quality across different LLM providers.

**Key Tests:**
- `AllProviders_Should_ProduceAnalyzableContent` - Cross-provider compatibility
- `AllProviders_Should_RespectEnhancedPrompts` - Prompt consistency
- `QualityScores_Should_BeConsistentAcrossProviders` - Scoring consistency
- `AllProviders_Should_DetectLowQuality` - Detection consistency

### Performance Tests (`/Aura.Tests/Performance/`)

#### PromptGenerationBenchmark
Performance benchmarks for prompt template generation.

**Benchmarks:**
- Script prompt generation: < 50ms
- Visual prompt generation: < 10ms
- Quality prompt generation: < 10ms
- System prompt retrieval: < 1ms
- Memory usage: < 10MB for 1000 generations

#### QualityAnalysisBenchmark
Performance benchmarks for content quality analysis.

**Benchmarks:**
- Heuristic analysis: < 5 seconds
- Linear scaling for multiple analyses
- Constant memory usage
- Large script (5-min video): < 10 seconds

### Smoke Tests (`/Aura.Tests/Smoke/`)

#### BasicSystemSmokeTests
Quick validation tests for CI/CD pipelines.

**Tests:**
- System initialization
- Prompt template access
- RuleBased provider availability
- Quality system initialization
- No runtime configuration errors

## Running Tests

### Run All Integration Tests
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

### Run Specific Test Suite
```bash
# AI Quality System tests
dotnet test --filter "FullyQualifiedName~AIQualitySystemIntegrationTests"

# LLM Provider tests
dotnet test --filter "FullyQualifiedName~LLMProviderIntegrationTests"

# End-to-end tests
dotnet test --filter "FullyQualifiedName~EndToEndVideoGenerationTests"
```

### Run Performance Benchmarks
```bash
dotnet test --filter "FullyQualifiedName~Performance"
```

### Run Smoke Tests (for CI/CD)
```bash
dotnet test --filter "FullyQualifiedName~Smoke"
```

## Health Checks and Diagnostics

### System Health Service

The `SystemHealthService` provides comprehensive system monitoring:

```csharp
// In your startup/DI configuration
services.AddSingleton<IHealthCheck, LLMProviderHealthCheck>();
services.AddSingleton<IHealthCheck, ContentQualityHealthCheck>();
services.AddSingleton<SystemHealthService>();
```

### Using the Diagnostics API

#### Check System Health
```bash
GET /api/diagnostics/health
```

Response:
```json
{
  "status": "Healthy",
  "checkResults": [...],
  "totalChecks": 3,
  "healthyChecks": 3,
  "unhealthyChecks": 0,
  "checkDuration": "00:00:02.5",
  "timestamp": "2025-10-24T02:00:00Z"
}
```

#### Test LLM Provider
```bash
POST /api/diagnostics/test-provider
Content-Type: application/json

{
  "providerName": "OpenAI"
}
```

#### Test Quality Analysis
```bash
POST /api/diagnostics/test-quality-analysis
Content-Type: application/json

{
  "script": "Your test script here",
  "topic": "Test Topic",
  "tone": "informative"
}
```

#### Run Integration Tests
```bash
POST /api/diagnostics/run-integration-tests
```

## Validation Services

### System Integrity Validator

Validates that all components are properly wired up:

```csharp
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

### Prompt Template Validator

Validates that `EnhancedPromptTemplates` generates valid prompts:

```csharp
var validator = new PromptTemplateValidator(logger);
var result = await validator.ValidateAsync();

Console.WriteLine($"Validation: {result.IsValid}");
Console.WriteLine($"Checks passed: {result.SuccessCount}/{result.TotalChecks}");
```

## Testing Best Practices

### 1. Test Isolation
Each test should be independent and not rely on external state:
```csharp
[Fact]
public async Task Test_Should_BeIndependent()
{
    // Arrange - Create all dependencies
    var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
    
    // Act & Assert
    // ...
}
```

### 2. Use RuleBased Provider for Unit Tests
For tests that don't need external LLM services:
```csharp
var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
```

### 3. Test Both Success and Failure Cases
```csharp
[Fact]
public async Task Should_DetectLowQuality() { /* ... */ }

[Fact]
public async Task Should_ApproveHighQuality() { /* ... */ }
```

### 4. Use Theory for Data-Driven Tests
```csharp
[Theory]
[InlineData("informative")]
[InlineData("professional")]
[InlineData("humorous")]
public async Task Should_HandleAllTones(string tone) { /* ... */ }
```

## Troubleshooting

### Common Test Failures

#### LLM Provider Timeout
**Issue:** Provider tests timing out  
**Solution:** Check provider configuration and network connectivity

#### Quality Scores Inconsistent
**Issue:** Quality analysis producing unexpected scores  
**Solution:** Review test script content - may need adjustment

#### Memory Test Failures
**Issue:** Performance tests showing high memory usage  
**Solution:** Ensure proper GC collections and check for memory leaks

### Debug Mode

Enable detailed logging for tests:
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Adding New Integration Tests

### Template for New Integration Test

```csharp
using System;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Integration;

public class MyNewIntegrationTests
{
    [Fact]
    public async Task MyTest_Should_WorkCorrectly()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(
            NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        // Act
        var result = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Run Integration Tests
  run: dotnet test --filter "FullyQualifiedName~Integration"

- name: Run Smoke Tests
  run: dotnet test --filter "FullyQualifiedName~Smoke"
```

### Quality Gates

Tests that must pass before deployment:
1. All Smoke Tests
2. Basic Integration Tests
3. System Health Checks
4. Performance Benchmarks within thresholds

## Support

For issues or questions:
1. Check this guide first
2. Review test failure logs
3. Use diagnostics API for debugging
4. Consult the AI Quality Implementation Summary
