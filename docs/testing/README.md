# Testing Documentation

Welcome to the Aura testing documentation. This directory contains comprehensive guides for writing, running, and maintaining tests across the project.

## Quick Start

```bash
# Backend tests
cd Aura.Tests
dotnet test --collect:"XPlat Code Coverage"

# Frontend tests
cd Aura.Web
npm run test:coverage

# All tests with coverage
./scripts/test/run-tests-with-coverage.sh
```

## Documentation Index

### 📘 Essential Guides

1. **[Test Writing Guide](./TEST_WRITING_GUIDE.md)** - Start here!
   - Test structure and organization
   - Backend testing with xUnit and Moq
   - Frontend testing with Vitest and Testing Library
   - Using test data builders
   - Integration and performance tests
   - Best practices and examples

2. **[Mocking Guide](./MOCKING_GUIDE.md)** - Comprehensive mocking reference
   - Backend mocking strategies (Moq)
   - Frontend mocking strategies (Jest, MSW)
   - API mocking patterns
   - Common patterns and anti-patterns

3. **[E2E Testing Best Practices](./E2E_TESTING_BEST_PRACTICES.md)** - Playwright guide
   - Page Object Model
   - Selector strategies
   - Waiting and synchronization
   - Visual testing
   - Debugging E2E tests

4. **[Test Coverage Strategy](./TEST_COVERAGE_STRATEGY.md)** - Coverage goals and strategy
   - Coverage targets (80% minimum)
   - Testing pyramid
   - Critical path coverage
   - Coverage enforcement
   - Monitoring and reporting

## Test Infrastructure

### Backend (.NET)

```
Aura.Tests/
├── TestDataBuilders/          # Reusable test data builders
│   ├── VideoJobBuilder.cs
│   ├── ProjectBuilder.cs
│   ├── TimelineBuilder.cs
│   ├── AssetBuilder.cs
│   └── ApiKeyBuilder.cs
├── Integration/               # Integration test suite
│   ├── IntegrationTestBase.cs
│   ├── ApiIntegrationTestBase.cs
│   └── EndpointTests/
├── Performance/               # Performance tests
│   ├── PerformanceTestBase.cs
│   └── VideoProcessingPerformanceTests.cs
├── .runsettings              # Test configuration
└── Aura.Tests.csproj         # Project with coverage settings
```

### Frontend (React)

```
Aura.Web/
├── src/
│   ├── test/
│   │   ├── utils/
│   │   │   ├── testUtils.tsx        # Component test utilities
│   │   │   └── hookTestUtils.ts     # Hook test utilities
│   │   ├── setup.ts                 # Global test setup
│   │   └── **/*.test.tsx            # Unit tests
│   └── **/__tests__/                # Colocated tests
├── tests/
│   ├── e2e/                         # Playwright E2E tests
│   ├── integration/                 # Integration tests
│   └── smoke/                       # Smoke tests
├── vitest.config.coverage.ts        # Coverage configuration
└── playwright.config.ts             # E2E configuration
```

## Testing Tools

### Backend Stack
- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Assertion library
- **coverlet** - Coverage collection
- **ReportGenerator** - Coverage reports

### Frontend Stack
- **Vitest** - Test runner
- **Testing Library** - Component testing
- **Jest** - Mocking and assertions
- **Playwright** - E2E testing
- **MSW** - API mocking

## Running Tests

### Unit Tests

```bash
# Backend
cd Aura.Tests
dotnet test

# Frontend
cd Aura.Web
npm test

# Watch mode (frontend)
npm run test:watch
```

### Integration Tests

```bash
# Backend
dotnet test --filter "FullyQualifiedName~Integration"

# Frontend
npm run test:integration
```

### E2E Tests

```bash
cd Aura.Web
npm run playwright

# UI mode
npm run playwright:ui

# Headed mode
npm run playwright -- --headed
```

### Coverage

```bash
# Backend with coverage
dotnet test --collect:"XPlat Code Coverage" --settings Aura.Tests/.runsettings

# Frontend with coverage
npm run test:coverage

# All tests with coverage
./scripts/test/run-tests-with-coverage.sh

# View coverage reports
open Aura.Tests/TestResults/coverage/index.html
open Aura.Web/coverage/index.html
```

### Parallel Execution

```bash
# Run all test suites in parallel
./scripts/test/parallel-test-runner.sh
```

### Flaky Test Detection

```bash
# Run tests multiple times to detect flakiness
./scripts/test/detect-flaky-tests.sh 10

# Results in flaky-test-results/summary.txt
```

## Test Data Builders

Use builders for consistent test data:

```csharp
// Backend
var job = new VideoJobBuilder()
    .WithProjectId("test-123")
    .InProgress(0.5)
    .Build();

var project = new ProjectBuilder()
    .WithName("Test Project")
    .WithTag("tutorial")
    .Build();

var timeline = new TimelineBuilder()
    .WithDuration(120.0)
    .WithDefaultVideoTrack()
    .Build();
```

```typescript
// Frontend
import { renderWithProviders } from '@/test/utils/testUtils';

renderWithProviders(<MyComponent />, {
  withRouter: true,
  withQueryClient: true,
});
```

## Coverage Targets

| Component | Target | Priority |
|-----------|--------|----------|
| Core Services | 90% | P0 |
| API Controllers | 85% | P0 |
| Business Logic | 90% | P0 |
| UI Components | 75% | P1 |
| Utilities | 95% | P1 |
| Integration | 80% | P1 |

## CI/CD Integration

Tests run automatically on:
- Every commit (unit tests)
- Every PR (full test suite)
- Nightly (flaky test detection)

### PR Checks
- ✅ All tests pass
- ✅ Coverage ≥ 80%
- ✅ No flaky tests
- ✅ Execution time < 10 minutes

### Coverage Enforcement

Tests must maintain 80% coverage:
```yaml
# CI will fail if coverage drops below threshold
- Backend: 80% line and branch coverage
- Frontend: 80% line, branch, function, statement coverage
```

## Best Practices Summary

### General
1. ✅ Follow Arrange-Act-Assert pattern
2. ✅ Keep tests independent
3. ✅ Use descriptive test names
4. ✅ Test behavior, not implementation
5. ✅ Keep tests fast (< 100ms for unit tests)

### Backend
1. ✅ Use test data builders
2. ✅ Mock external dependencies
3. ✅ Use FluentAssertions for readability
4. ✅ Verify mock calls
5. ✅ Test edge cases and errors

### Frontend
1. ✅ Use testing utilities (`renderWithProviders`)
2. ✅ Test user interactions
3. ✅ Wait for async operations
4. ✅ Mock APIs with MSW
5. ✅ Test accessibility

### E2E
1. ✅ Use Page Object Model
2. ✅ Use stable selectors (data-testid)
3. ✅ Avoid fixed waits
4. ✅ Test critical user journeys
5. ✅ Keep E2E tests minimal

## Troubleshooting

### Tests Failing Locally

```bash
# Clean and rebuild
dotnet clean && dotnet build
npm run clean && npm install

# Clear test cache
rm -rf TestResults/ coverage/

# Run specific test
dotnet test --filter "TestName"
npm test -- ComponentName.test.tsx
```

### Coverage Not Collecting

```bash
# Backend: Check .runsettings
cat Aura.Tests/.runsettings

# Frontend: Check vitest config
cat Aura.Web/vitest.config.coverage.ts

# Verify coverage tools installed
dotnet tool list -g | grep reportgenerator
```

### Flaky Tests

```bash
# Run flaky test detection
./scripts/test/detect-flaky-tests.sh 10

# Debug specific test
npm test -- --reporter=verbose ComponentName.test.tsx
```

## Writing Your First Test

### Backend Example

```csharp
using Aura.Tests.TestDataBuilders;
using FluentAssertions;
using Xunit;

public class MyServiceTests
{
    [Fact]
    public async Task ProcessVideo_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var job = new VideoJobBuilder().Build();
        var service = new MyService();

        // Act
        var result = await service.ProcessAsync(job);

        // Assert
        result.Should().BeTrue();
    }
}
```

### Frontend Example

```typescript
import { renderWithProviders, screen, userEvent } from '@/test/utils/testUtils';
import { MyComponent } from './MyComponent';

describe('MyComponent', () => {
  it('should handle user interaction', async () => {
    // Arrange
    const user = userEvent.setup();
    renderWithProviders(<MyComponent />);

    // Act
    await user.click(screen.getByRole('button'));

    // Assert
    expect(screen.getByText('Success')).toBeInTheDocument();
  });
});
```

## Resources

### Internal
- [Test Writing Guide](./TEST_WRITING_GUIDE.md)
- [Mocking Guide](./MOCKING_GUIDE.md)
- [E2E Best Practices](./E2E_TESTING_BEST_PRACTICES.md)
- [Coverage Strategy](./TEST_COVERAGE_STRATEGY.md)

### External
- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions](https://fluentassertions.com/)
- [Vitest Guide](https://vitest.dev/guide/)
- [Testing Library](https://testing-library.com/)
- [Playwright Docs](https://playwright.dev/)

## Getting Help

- Check the [Test Writing Guide](./TEST_WRITING_GUIDE.md) first
- Review existing tests for examples
- Ask in the team chat
- Create an issue if you find gaps in documentation

## Contributing

When adding new test infrastructure:
1. Update relevant documentation
2. Add examples to test guides
3. Ensure tests are fast and reliable
4. Follow established patterns

---

**Need help?** Start with the [Test Writing Guide](./TEST_WRITING_GUIDE.md) or check existing tests for examples.
