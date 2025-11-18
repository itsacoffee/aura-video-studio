# Testing Quick Start Guide

Quick reference for running tests and checking coverage in the Aura project.

## 🚀 Quick Commands

### Backend Tests (.NET)

```bash
# Run all unit tests
dotnet test Aura.Tests/Aura.Tests.csproj

# Run with coverage
dotnet test Aura.Tests/Aura.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults

# Run specific category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Performance"

# Run specific test class
dotnet test --filter "FullyQualifiedName~VideoJobBuilderTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Frontend Tests (React)

```bash
cd Aura.Web

# Run all unit tests
npm test

# Run with coverage
npm run test:coverage

# Run in watch mode
npm run test:watch

# Run with UI
npm run test:ui

# Run specific test file
npm test -- src/components/VideoPlayer.test.tsx

# Run E2E tests
npm run playwright

# Run E2E tests in UI mode
npm run playwright:ui

# Run specific E2E test
npx playwright test tests/e2e/critical-user-journeys.spec.ts
```

### Coverage Analysis

```bash
# Run comprehensive coverage analysis
./scripts/test/coverage-analysis.sh

# View backend coverage report
open TestResults/coverage-report/index.html

# View frontend coverage report
open Aura.Web/coverage/index.html
```

### Performance Testing

```bash
# Run performance benchmarks
./scripts/test/performance-benchmark.sh

# View benchmark report
cat TestResults/benchmarks/BENCHMARK_REPORT.md
```

### Test Dashboard

```bash
# Generate test dashboard
./scripts/test/generate-test-report.sh

# View dashboard
open TestResults/dashboard/index.html
```

## 📊 Coverage Thresholds

| Component | Target | Status |
|-----------|--------|--------|
| Backend | 80% | ✅ Infrastructure Ready |
| Frontend | 80% | ✅ Infrastructure Ready |
| Integration | 80% | ✅ Infrastructure Ready |

## 🏗️ Writing Tests

### Backend Test Example

```csharp
using Xunit;
using FluentAssertions;
using Aura.Tests.TestDataBuilders;

public class VideoJobServiceTests
{
    [Fact]
    public void Should_Create_Video_Job()
    {
        // Arrange
        var job = new VideoJobBuilder()
            .WithTitle("Test Video")
            .WithStatus(JobStatus.Pending)
            .Build();
        
        // Act
        var result = _service.CreateJob(job);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
    }
    
    [Theory]
    [InlineData(JobStatus.Pending)]
    [InlineData(JobStatus.Processing)]
    public void Should_Accept_Valid_Status(JobStatus status)
    {
        // Arrange
        var job = new VideoJobBuilder()
            .WithStatus(status)
            .Build();
        
        // Act & Assert
        job.Status.Should().Be(status);
    }
}
```

### Frontend Test Example

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { VideoPlayer } from './VideoPlayer';

describe('VideoPlayer', () => {
  it('should render video player', () => {
    render(<VideoPlayer src="/test-video.mp4" />);
    
    const video = screen.getByRole('video');
    expect(video).toBeInTheDocument();
  });
  
  it('should handle play/pause', async () => {
    const { user } = render(<VideoPlayer src="/test-video.mp4" />);
    
    const playButton = screen.getByRole('button', { name: /play/i });
    await user.click(playButton);
    
    const pauseButton = screen.getByRole('button', { name: /pause/i });
    expect(pauseButton).toBeInTheDocument();
  });
});
```

### E2E Test Example

```typescript
import { test, expect } from '@playwright/test';
import { PageFactory } from './helpers/page-objects';

test('should create video successfully', async ({ page }) => {
  const pages = new PageFactory(page);
  
  // Navigate to dashboard
  const dashboard = pages.dashboardPage();
  await dashboard.goto('/dashboard');
  
  // Create new project
  await dashboard.createNewProject();
  
  // Fill wizard
  const wizard = pages.videoWizardPage();
  await wizard.fillBasicInfo('Test Video', 'Description', '30');
  await wizard.submit();
  
  // Verify success
  const toast = pages.toast();
  const message = await toast.waitForToast('success');
  expect(message).toContain('created');
});
```

## 🔧 Test Utilities

### Test Data Builders

```csharp
// Use fluent API to create test data
var project = new ProjectBuilder()
    .WithName("Test Project")
    .WithDescription("A test project")
    .WithAsset(new AssetBuilder().WithType(AssetType.Video).Build())
    .Build();
```

### Test Helpers

```typescript
import { waitFor, sleep, generateTestData } from './test-helpers';

// Wait for condition
await waitFor(() => element.isVisible(), { timeout: 5000 });

// Generate random data
const email = generateTestData.email();
const uuid = generateTestData.uuid();

// Create mock file
const file = createMockVideoFile('test.mp4', 1024 * 1024);
```

### Performance Testing

```csharp
using (var timer = new PerformanceTimer("Video Processing", 
    threshold: TimeSpan.FromSeconds(5)))
{
    // Code to measure
    await _service.ProcessVideo(video);
    
    // Timer logs duration on disposal
}
```

## 📈 Continuous Improvement

### Daily Tasks
- [ ] Run tests before committing
- [ ] Check coverage on new code
- [ ] Fix any failing tests immediately

### Weekly Tasks
- [ ] Review coverage trends
- [ ] Address flaky tests
- [ ] Update test data as needed

### Monthly Tasks
- [ ] Review and update performance benchmarks
- [ ] Audit test suite for outdated tests
- [ ] Update documentation

## 🐛 Troubleshooting

### Tests Failing Locally

```bash
# Clean and rebuild
dotnet clean
dotnet build
npm run build

# Clear test cache
rm -rf TestResults/
rm -rf Aura.Web/coverage/
rm -rf Aura.Web/test-results/

# Run tests again
./scripts/test/run-tests-with-coverage.sh
```

### Coverage Not Updating

```bash
# Delete coverage files
find . -name "coverage.*.xml" -delete
find . -name "*.coverage" -delete

# Run coverage again
dotnet test --collect:"XPlat Code Coverage"
npm run test:coverage
```

### E2E Tests Failing

```bash
# Update Playwright browsers
cd Aura.Web
npx playwright install --with-deps

# Run in debug mode
npx playwright test --debug

# Generate trace
npx playwright test --trace on
```

### Performance Tests Slow

```bash
# Run with fewer iterations
ITERATIONS=1 ./scripts/test/performance-benchmark.sh

# Run specific category only
dotnet test --filter "Category=Performance&Priority=High"
```

## 📚 Documentation

- [Test Coverage Strategy](docs/testing/TEST_COVERAGE_STRATEGY.md)
- [Test Writing Guide](docs/testing/TEST_WRITING_GUIDE.md)
- [E2E Testing Guide](E2E_TESTING_GUIDE.md)
- [Mocking Guide](docs/testing/MOCKING_GUIDE.md)
- Test Data Builders

## 🎯 Quality Gates

### Before Committing
- ✅ All tests pass
- ✅ No linting errors
- ✅ Coverage maintained or improved

### Before Merging PR
- ✅ All CI checks pass
- ✅ Coverage ≥ 80%
- ✅ E2E tests pass
- ✅ No security vulnerabilities

### Before Release
- ✅ All test categories pass
- ✅ Performance benchmarks meet thresholds
- ✅ No known flaky tests
- ✅ Documentation updated

## 🚨 Getting Help

1. Check documentation first
2. Look for similar tests as examples
3. Review test utilities and helpers
4. Ask team members
5. Create an issue with details

---

**Remember**: Good tests are:
- **Fast** - Run quickly
- **Independent** - Don't rely on other tests
- **Repeatable** - Same result every time
- **Self-validating** - Pass or fail clearly
- **Timely** - Written with the code

Happy Testing! 🧪
