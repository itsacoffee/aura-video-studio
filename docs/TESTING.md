# Testing Guide for Aura Video Studio

This document provides comprehensive information about the testing infrastructure, test suites, and best practices for Aura Video Studio.

## Table of Contents

- [Overview](#overview)
- [Test Categories](#test-categories)
- [Backend Tests](#backend-tests)
- [Frontend Tests](#frontend-tests)
- [Running Tests](#running-tests)
- [CI/CD Integration](#cicd-integration)
- [Writing New Tests](#writing-new-tests)
- [Test Coverage](#test-coverage)
- [Performance Benchmarks](#performance-benchmarks)
- [Troubleshooting](#troubleshooting)

## Overview

Aura Video Studio has a comprehensive test suite covering:

- **Backend**: Unit tests, integration tests, E2E tests, and performance benchmarks
- **Frontend**: Unit tests (Vitest), E2E tests (Playwright), accessibility tests

### Total Test Count: 352 tests

- Backend Unit Tests: 190
- Backend Integration Tests: 26
- Backend E2E Tests: 9
- Backend Performance Tests: 7
- Frontend Unit Tests: 66
- Frontend E2E Tests: 54

## Test Categories

### 1. Unit Tests

Test individual components and functions in isolation.

**Location**:
- Backend: `Aura.Tests/*Tests.cs`
- Frontend: `Aura.Web/src/**/*.test.ts(x)`

**Purpose**: Validate individual functions, classes, and components.

### 2. Integration Tests

Test interactions between multiple components and services.

**Location**: `Aura.Tests/Integration/`

**Coverage**:
- Full pipeline integration (`FullPipelineIntegrationTests.cs`)
- Server-Sent Events (`ServerSentEventsIntegrationTests.cs`)
- Concurrent job execution (`ConcurrentJobExecutionTests.cs`)

### 3. End-to-End Tests

Test complete user workflows and system behavior.

**Location**:
- Backend: `Aura.E2E/`
- Frontend: `Aura.Web/tests/e2e/`

**Coverage**:
- Complete video generation workflow
- Provider fallback and error recovery
- Real-time progress updates
- Accessibility compliance
- Responsive design
- Error handling

### 4. Performance Tests

Measure and benchmark system performance.

**Location**: `Aura.Tests/Performance/`

**Coverage**:
- Script generation timing for various durations
- Memory usage and leak detection
- Concurrent job throughput
- Provider selection performance
- Cache effectiveness
- Scalability under load

## Backend Tests

### Unit Tests (190 tests)

```bash
# Run all unit tests
dotnet test Aura.Tests/Aura.Tests.csproj --configuration Release

# Run specific test category
dotnet test --filter "FullyQualifiedName~LlmProvider"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

**Key Test Areas**:
- LLM provider integration
- TTS provider functionality
- Hardware detection
- Provider mixing and selection
- Script orchestration
- Job management
- FFmpeg command building
- Audio processing
- Content analysis
- Quality validation

### Integration Tests (26 tests)

```bash
# Run all integration tests
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~Integration"
```

**Test Suites**:

#### Full Pipeline Integration (6 tests)
- Complete workflow with Free tier
- Provider fallback recovery
- Stage validation
- Cancellation handling
- Output consistency

#### Server-Sent Events (6 tests)
- Progress event emission
- Correlation ID tracking
- Error event reporting
- Multiple subscriber support
- Stream lifecycle management
- Timing information accuracy

#### Concurrent Job Execution (7 tests)
- Parallel job execution
- Queue ordering
- State isolation
- Independent cancellation
- Resource cleanup
- Failure isolation
- Memory management

### E2E Tests (9 tests)

```bash
# Run all E2E tests
dotnet test Aura.E2E/Aura.E2E.csproj
```

**Coverage**:
- Complete offline workflow
- Pro tier with fallback
- Offline mode blocking
- Hardware detection
- Provider validation
- Pipeline execution

### Performance Tests (7 tests)

```bash
# Run performance benchmarks
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~Performance"
```

**Benchmarks**:

1. **Script Generation**: 10s, 30s, 60s, 120s, 300s videos
2. **Memory Usage**: Pipeline execution monitoring
3. **Throughput**: 1, 2, 5, 10 concurrent jobs
4. **Provider Selection**: 1000 iterations timing
5. **Cache Performance**: Cold vs warm execution
6. **Scalability**: 1, 5, 10, 20 job loads

**Performance Targets**:
- Script generation: < 5000ms for all durations
- Memory usage: < 50MB per job
- Memory leaks: < 5MB after GC
- Provider selection: < 1ms average
- Throughput: Scales with concurrency

## Frontend Tests

### Unit Tests (66 tests)

```bash
cd Aura.Web

# Run all unit tests
npm test

# Run with watch mode
npm run test:watch

# Run with coverage
npm run test:coverage

# Run with UI
npm run test:ui
```

**Location**: `Aura.Web/src/**/*.test.ts(x)`

**Coverage**:
- Component rendering
- State management (Zustand stores)
- Form validation
- API client functions
- Utility functions
- Custom hooks

### E2E Tests (54 tests)

```bash
cd Aura.Web

# Run all Playwright tests
npm run playwright

# Run specific test file
npx playwright test tests/e2e/complete-workflow.spec.ts

# Run in UI mode
npm run playwright:ui

# Run specific browser
npx playwright test --project=chromium
```

**Test Suites**:

#### Complete Workflow (7 tests)
- Full user journey (Brief → Plan → Voice → Generate → Export)
- Wizard navigation (forward/backward)
- Form validation
- Data persistence
- Real-time progress updates
- Job cancellation
- API error handling

#### Accessibility (15 tests)
- ARIA labels and roles
- Keyboard navigation
- Heading hierarchy
- Color contrast
- Form labels and errors
- Screen reader support
- Skip navigation
- Empty links/buttons check
- Focus indicators
- Alt text on images
- High contrast mode
- Language attribute
- Dynamic content announcements
- Keyboard-only usage
- Modal dialog accessibility

#### Responsive Design (17 tests)
- Desktop: 1920x1080, 1366x768
- Tablet: 768x1024, 1024x768, iPad
- Mobile: 375x667, 667x375, iPhone 12 Pro, iPhone SE
- No horizontal scroll
- Touch-friendly button sizes
- Orientation changes
- Layout adaptation
- Readable text sizes
- Form stacking on narrow screens
- Navigation adaptation
- Zoom level handling

#### Error Handling (15 tests)
- Network timeout
- HTTP 500 errors
- HTTP 404 errors
- HTTP 401 unauthorized
- Network disconnection
- Malformed API responses
- Required field validation
- Input length validation
- Special character handling
- Job failure display
- Transient error retry
- CORS errors
- Helpful error messages
- Rate limiting (429)
- Form data preservation

## Running Tests

### Quick Start

```bash
# Backend tests
dotnet test

# Frontend tests
cd Aura.Web
npm test
npm run playwright
```

### Continuous Testing

```bash
# Backend - watch mode (limited support)
dotnet watch test

# Frontend - watch mode
cd Aura.Web
npm run test:watch
```

### Running Specific Tests

```bash
# Backend - by name pattern
dotnet test --filter "FullyQualifiedName~PipelineIntegration"

# Frontend - by file
npx playwright test accessibility.spec.ts

# Frontend - by test name
npx playwright test -g "should have proper ARIA labels"
```

## CI/CD Integration

### Workflows

1. **Main CI** (`.github/workflows/ci.yml`)
   - Runs on all pushes and PRs
   - Backend unit tests
   - Frontend unit tests
   - Frontend E2E tests
   - Build validation

2. **Integration Tests** (`.github/workflows/integration-tests.yml`)
   - Runs on pushes, PRs, and nightly
   - Backend integration tests
   - Backend performance tests
   - Frontend E2E test suites
   - Coverage reporting

### Test Artifacts

All test runs produce artifacts:
- Test results (TRX format)
- Coverage reports (HTML/JSON)
- Playwright reports (HTML)
- Performance benchmarks (Markdown)
- Screenshots (on failure)

**Retention**: 30 days for test results, 7 days for screenshots

## Writing New Tests

### Backend Tests

#### Unit Test Template

```csharp
using Xunit;
using Aura.Core.Services;

namespace Aura.Tests;

public class MyServiceTests
{
    [Fact]
    public async Task MyMethod_Should_ReturnExpectedResult()
    {
        // Arrange
        var service = new MyService();
        var input = "test";
        
        // Act
        var result = await service.MyMethodAsync(input, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("expected", result);
    }
}
```

#### Integration Test Template

```csharp
using Xunit;
using Aura.Core.Orchestrator;

namespace Aura.Tests.Integration;

public class MyIntegrationTests
{
    [Fact]
    public async Task Integration_Should_WorkEndToEnd()
    {
        // Arrange - Setup multiple components
        var orchestrator = new ScriptOrchestrator(/* dependencies */);
        
        // Act - Execute integration
        var result = await orchestrator.ExecuteAsync(/* params */);
        
        // Assert - Verify integration
        Assert.True(result.Success);
    }
}
```

### Frontend Tests

#### Unit Test Template (Vitest)

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import MyComponent from './MyComponent';

describe('MyComponent', () => {
  it('should render correctly', () => {
    render(<MyComponent title="Test" />);
    expect(screen.getByText('Test')).toBeInTheDocument();
  });
});
```

#### E2E Test Template (Playwright)

```typescript
import { test, expect } from '@playwright/test';

test.describe('My Feature', () => {
  test('should complete workflow', async ({ page }) => {
    // Navigate
    await page.goto('/feature');
    
    // Interact
    await page.getByLabel('Input').fill('test value');
    await page.getByRole('button', { name: 'Submit' }).click();
    
    // Assert
    await expect(page.getByText('Success')).toBeVisible();
  });
});
```

## Test Coverage

### Current Coverage

- **Backend Critical Paths**: >80%
- **Frontend Critical Paths**: >70%
- **Integration Scenarios**: >80%
- **Error Recovery**: >80%
- **Accessibility**: Comprehensive (WCAG 2.1 AA)

### Coverage Reports

```bash
# Backend coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/

# Frontend coverage
cd Aura.Web
npm run test:coverage
# Open coverage/index.html
```

### Coverage Goals

- Unit tests: >80% line coverage
- Integration tests: All critical paths
- E2E tests: All major user workflows
- Performance tests: All common scenarios
- Accessibility: WCAG 2.1 AA compliance

## Performance Benchmarks

### Baseline Metrics

From `PipelineBenchmarkTests.cs`:

| Metric | Target | Current |
|--------|--------|---------|
| Script Gen (10s) | <5000ms | ✅ |
| Script Gen (30s) | <5000ms | ✅ |
| Script Gen (60s) | <5000ms | ✅ |
| Memory per Job | <50MB | ✅ |
| Memory Leak | <5MB | ✅ |
| Provider Select | <1ms | ✅ |
| Throughput (10 jobs) | >2 jobs/s | ✅ |

### Running Benchmarks

```bash
# Run all performance tests
dotnet test --filter "FullyQualifiedName~Performance"

# View detailed output
dotnet test --filter "FullyQualifiedName~Performance" --logger "console;verbosity=detailed"
```

### Interpreting Results

Performance test output includes:
- Execution time (milliseconds)
- Memory usage (MB)
- Throughput (operations/second)
- Comparison to baselines

If benchmarks fail, investigate:
1. Recent code changes
2. System resource availability
3. External dependencies
4. Test data size

## Troubleshooting

### Common Issues

#### Tests Timing Out

```bash
# Increase timeout for long-running tests
dotnet test --blame-hang-timeout 5m
```

#### Flaky E2E Tests

```bash
# Run with retries
npx playwright test --retries=2

# Run in headed mode to debug
npx playwright test --headed

# Run with slow motion
npx playwright test --slow-mo=100
```

#### Coverage Not Generated

```bash
# Ensure coverlet is installed
dotnet add package coverlet.collector

# Use correct command
dotnet test /p:CollectCoverage=true
```

#### Playwright Browser Issues

```bash
# Reinstall browsers
cd Aura.Web
npm run playwright:install
```

### Getting Help

- **Test Failures**: Check CI logs and test artifacts
- **Performance Issues**: Review benchmark reports
- **Coverage Gaps**: Run coverage reports
- **E2E Issues**: Check Playwright screenshots and traces

## Best Practices

### DO

✅ Write descriptive test names  
✅ Use AAA pattern (Arrange, Act, Assert)  
✅ Test edge cases and error conditions  
✅ Keep tests isolated and independent  
✅ Mock external dependencies  
✅ Use fixtures for test data  
✅ Assert on specific values, not just existence  
✅ Clean up resources after tests

### DON'T

❌ Test implementation details  
❌ Write flaky or timing-dependent tests  
❌ Share state between tests  
❌ Test third-party code  
❌ Ignore test failures  
❌ Skip writing tests for bug fixes  
❌ Use production data in tests

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Vitest Documentation](https://vitest.dev/)
- [Playwright Documentation](https://playwright.dev/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Testing Best Practices](https://martinfowler.com/testing/)

---

**Last Updated**: 2025-11-01  
**Maintained by**: Aura Development Team
