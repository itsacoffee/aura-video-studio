# Testing Guide

Comprehensive guide to testing Aura Video Studio across all layers.

## Overview

Aura Video Studio has comprehensive test coverage:

- **Frontend Tests**: Vitest for unit/integration tests
- **Backend Tests**: xUnit for .NET tests
- **E2E Tests**: Playwright for full workflow testing
- **Coverage Goals**: 80%+ for core logic, 60%+ for UI components

## Quick Start

### Run All Tests

```bash
# All tests with coverage (recommended)
./scripts/test-local.sh

# Frontend only
./scripts/test-local.sh --frontend-only

# Backend only
./scripts/test-local.sh --dotnet-only

# Include E2E tests
./scripts/test-local.sh --e2e
```

## Frontend Testing (Vitest)

### Running Tests

```bash
cd Aura.Web

# Run once
npm test

# Watch mode (auto-rerun on changes)
npm run test:watch

# With coverage
npm run test:coverage

# Interactive UI
npm run test:ui

# CI mode with detailed output
npm run test:coverage:ci
```

### Test Organization

```
Aura.Web/
├── src/
│   ├── components/
│   │   └── MyComponent.test.tsx
│   ├── services/
│   │   └── apiClient.test.ts
│   └── stores/
│       └── useJobStore.test.ts
└── tests/
    ├── unit/
    ├── integration/
    └── smoke/
```

### Writing Component Tests

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import MyComponent from './MyComponent';

describe('MyComponent', () => {
  it('renders with title', () => {
    render(<MyComponent title="Test" onSave={vi.fn()} />);
    expect(screen.getByText('Test')).toBeInTheDocument();
  });

  it('calls onSave when button clicked', async () => {
    const onSave = vi.fn();
    render(<MyComponent title="Test" onSave={onSave} />);
    
    const button = screen.getByRole('button', { name: /save/i });
    fireEvent.click(button);
    
    expect(onSave).toHaveBeenCalledWith(expect.any(String));
  });
});
```

### Testing Zustand Stores

```typescript
import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useJobStore } from './jobStore';

describe('useJobStore', () => {
  beforeEach(() => {
    useJobStore.setState({ jobs: [], selectedJobId: null });
  });

  it('adds job', () => {
    const { result } = renderHook(() => useJobStore());
    
    act(() => {
      result.current.addJob({ id: '1', status: 'queued' });
    });
    
    expect(result.current.jobs).toHaveLength(1);
    expect(result.current.jobs[0].id).toBe('1');
  });
});
```

### Testing API Calls

```typescript
import { describe, it, expect, vi } from 'vitest';
import MockAdapter from 'axios-mock-adapter';
import { apiClient } from './apiClient';
import axios from 'axios';

const mock = new MockAdapter(axios);

describe('apiClient', () => {
  afterEach(() => {
    mock.reset();
  });

  it('fetches job successfully', async () => {
    const jobData = { id: '1', status: 'completed' };
    mock.onGet('/api/jobs/1').reply(200, jobData);

    const result = await apiClient.get('/api/jobs/1');
    
    expect(result.data).toEqual(jobData);
  });

  it('handles errors with circuit breaker', async () => {
    mock.onGet('/api/jobs/1').networkError();

    await expect(apiClient.get('/api/jobs/1')).rejects.toThrow();
  });
});
```

### Coverage Reports

```bash
# Generate coverage
npm run test:coverage

# View HTML report
open coverage/index.html
```

**Coverage Goals**:
- **Utilities/Services**: 80%+
- **Components**: 60%+
- **Stores**: 80%+

## Backend Testing (xUnit)

### Running Tests

```bash
# Run all tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test Aura.Tests/Aura.Tests.csproj

# Filter by name
dotnet test --filter "FullyQualifiedName~VideoService"

# Detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Test Organization

```
Aura.Tests/
├── Unit/
│   ├── Services/
│   │   └── VideoServiceTests.cs
│   └── Providers/
│       └── OpenAIProviderTests.cs
└── Integration/
    └── JobOrchestrationTests.cs
```

### Writing Unit Tests

```csharp
public class VideoServiceTests
{
    private readonly Mock<ILogger<VideoService>> _loggerMock;
    private readonly Mock<IFFmpegService> _ffmpegMock;
    private readonly VideoService _service;
    
    public VideoServiceTests()
    {
        _loggerMock = new Mock<ILogger<VideoService>>();
        _ffmpegMock = new Mock<IFFmpegService>();
        _service = new VideoService(_loggerMock.Object, _ffmpegMock.Object);
    }
    
    [Fact]
    public async Task RenderVideoAsync_ValidSpec_ReturnsOutputPath()
    {
        // Arrange
        var spec = new RenderSpecification { JobId = "test123" };
        _ffmpegMock
            .Setup(x => x.RenderAsync(
                It.IsAny<RenderSpecification>(),
                It.IsAny<IProgress<RenderProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/output/video.mp4");
        
        // Act
        var result = await _service.RenderVideoAsync(
            spec, 
            null, 
            CancellationToken.None);
        
        // Assert
        result.Should().Be("/output/video.mp4");
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("test123")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
            Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task RenderVideoAsync_FFmpegFails_ThrowsException()
    {
        // Arrange
        var spec = new RenderSpecification { JobId = "test123" };
        _ffmpegMock
            .Setup(x => x.RenderAsync(
                It.IsAny<RenderSpecification>(),
                It.IsAny<IProgress<RenderProgress>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FFmpegException("Render failed"));
        
        // Act & Assert
        await Assert.ThrowsAsync<FFmpegException>(
            () => _service.RenderVideoAsync(spec, null, CancellationToken.None));
    }
}
```

### Integration Tests

```csharp
public class JobOrchestrationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public JobOrchestrationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateJob_ValidRequest_ReturnsJobId()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Brief = "Test video about cats",
            Audience = "General",
            Goal = "Educate",
            Tone = "Friendly"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<JobResponse>();
        result.Id.Should().NotBeNullOrEmpty();
        result.Status.Should().Be("Queued");
    }
}
```

### Coverage Reports

```bash
# Generate coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html"

# View report
open TestResults/CoverageReport/index.html
```

**Coverage Goals**:
- **Services**: 80%+
- **Orchestrators**: 80%+
- **Providers**: 70%+
- **Controllers**: 60%+

## E2E Testing (Playwright)

### Running E2E Tests

```bash
cd Aura.Web

# Install browsers (first time only)
npx playwright install --with-deps chromium

# Run tests
npm run playwright

# Interactive UI mode (debugging)
npm run playwright:ui

# Specific test
npx playwright test tests/e2e/full-pipeline.spec.ts

# With browser visible (headed mode)
npx playwright test --headed

# View report
npx playwright show-report
```

### Test Organization

```
Aura.Web/tests/e2e/
├── full-pipeline.spec.ts      # Complete video generation
├── job-management.spec.ts     # Job CRUD operations
├── provider-config.spec.ts    # Provider configuration
└── memory-regression.spec.ts  # Performance/memory tests
```

### Writing E2E Tests

```typescript
import { test, expect } from '@playwright/test';

test.describe('Video Generation Pipeline', () => {
  test('creates video from brief', async ({ page }) => {
    // Navigate to home
    await page.goto('http://localhost:5173');

    // Fill brief form
    await page.fill('[name="topic"]', 'Artificial Intelligence');
    await page.fill('[name="audience"]', 'Students');
    await page.selectOption('[name="tone"]', 'Educational');
    
    // Submit
    await page.click('button:has-text("Generate Script")');

    // Wait for script generation
    await expect(page.locator('.script-editor')).toBeVisible({ timeout: 30000 });
    
    // Continue to voice selection
    await page.click('button:has-text("Next: Voice")');
    await page.selectOption('[name="ttsProvider"]', 'windows-sapi');
    
    // Generate video
    await page.click('button:has-text("Generate Video")');
    
    // Wait for completion
    await expect(page.locator('.job-status'))
      .toContainText('Completed', { timeout: 120000 });
    
    // Verify video download link
    const downloadLink = page.locator('a:has-text("Download Video")');
    await expect(downloadLink).toBeVisible();
  });

  test('handles errors gracefully', async ({ page }) => {
    await page.goto('http://localhost:5173');
    
    // Submit empty form
    await page.click('button:has-text("Generate Script")');
    
    // Expect validation error
    await expect(page.locator('.error-message'))
      .toContainText('Topic is required');
  });
});
```

### Performance Tests

```typescript
test.describe('Performance', () => {
  test('timeline scrolling is smooth', async ({ page }) => {
    await page.goto('http://localhost:5173/timeline');
    
    // Measure frame rate during scroll
    const metrics = await page.evaluate(async () => {
      let frameCount = 0;
      const start = performance.now();
      
      return new Promise(resolve => {
        const measureFrame = () => {
          frameCount++;
          const elapsed = performance.now() - start;
          
          if (elapsed >= 1000) {
            resolve(frameCount);
          } else {
            requestAnimationFrame(measureFrame);
          }
        };
        
        measureFrame();
        
        // Simulate scrolling
        window.scrollBy(0, 10);
      });
    });
    
    expect(metrics).toBeGreaterThan(30); // At least 30 FPS
  });
});
```

### Memory Regression Tests

```typescript
test.describe('Memory', () => {
  test('no memory leaks in job list', async ({ page }) => {
    await page.goto('http://localhost:5173/jobs');
    
    // Get initial memory
    const initialMemory = await page.evaluate(() => 
      (performance as any).memory.usedJSHeapSize
    );
    
    // Load 1000 jobs and scroll
    for (let i = 0; i < 10; i++) {
      await page.mouse.wheel(0, 1000);
      await page.waitForTimeout(100);
    }
    
    // Force GC if available
    await page.evaluate(() => {
      if ((window as any).gc) (window as any).gc();
    });
    
    // Get final memory
    const finalMemory = await page.evaluate(() => 
      (performance as any).memory.usedJSHeapSize
    );
    
    // Memory should not grow more than 50%
    const growth = (finalMemory - initialMemory) / initialMemory;
    expect(growth).toBeLessThan(0.5);
  });
});
```

## Continuous Integration

### CI Workflows

All tests run automatically on pull requests:

```yaml
# .github/workflows/ci-unified.yml
- .NET Build & Test (Linux-compatible projects)
- Frontend Build, Lint & Test
- E2E Tests (Playwright with chromium)
- Coverage collection and upload
```

### Local Pre-commit Checks

Run before committing:

```bash
# 1. Scan for placeholders
node scripts/audit/find-placeholders.js

# 2. Lint and type check
cd Aura.Web
npm run lint
npm run typecheck

# 3. Run tests
npm test

# 4. Backend tests
cd ..
dotnet test
```

### Quality Gates

All PRs must pass:

1. ✅ All tests passing
2. ✅ No linting errors or warnings
3. ✅ No TypeScript errors
4. ✅ Coverage maintained or improved
5. ✅ No placeholder markers (TODO/FIXME)

## Best Practices

### Test Naming

**Good**:
- `it('renders loading spinner when isLoading is true')`
- `it('calls onSubmit with form data')`
- `it('retries failed API call with exponential backoff')`

**Bad**:
- `it('works')`
- `it('test1')`
- `it('handles stuff')`

### Test Structure

Use AAA pattern (Arrange, Act, Assert):

```typescript
it('adds two numbers', () => {
  // Arrange
  const a = 2;
  const b = 3;
  const calculator = new Calculator();
  
  // Act
  const result = calculator.add(a, b);
  
  // Assert
  expect(result).toBe(5);
});
```

### Mocking

Mock external dependencies, not implementation details:

**Good**:
```typescript
// Mock HTTP client
mock.onGet('/api/jobs').reply(200, { jobs: [] });
```

**Bad**:
```typescript
// Mock internal function
vi.spyOn(component, 'internalHelper').mockReturnValue('test');
```

### Async Testing

Always await async operations:

```typescript
// ✅ Good
it('fetches data', async () => {
  const data = await fetchData();
  expect(data).toBeDefined();
});

// ❌ Bad
it('fetches data', () => {
  fetchData().then(data => {
    expect(data).toBeDefined(); // Assertion might not run
  });
});
```

### Test Independence

Each test should be independent:

```typescript
describe('UserStore', () => {
  beforeEach(() => {
    // Reset state before each test
    useUserStore.setState({ users: [], selectedUser: null });
  });
  
  it('test 1', () => { /* ... */ });
  it('test 2', () => { /* ... */ });
});
```

## Troubleshooting

### Tests Failing Locally

```bash
# Clear caches
cd Aura.Web
rm -rf node_modules coverage dist
npm install
npm test

# Backend
dotnet clean
dotnet restore
dotnet test
```

### E2E Tests Timing Out

- Increase timeout in playwright.config.ts
- Run in headed mode to debug: `npx playwright test --headed`
- Check if backend is running: `curl http://localhost:5005/health/live`

### Coverage Not Generated

```bash
# Frontend
cd Aura.Web
npm run test:coverage:ci

# Backend
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## Additional Resources

- [Vitest Documentation](https://vitest.dev/)
- [Playwright Documentation](https://playwright.dev/)
- [xUnit Documentation](https://xunit.net/)
- [E2E Testing Guide](E2E_TESTING_GUIDE.md)
- [Quality Gates](QUALITY_GATES.md)

---

Last updated: 2025-11-18
