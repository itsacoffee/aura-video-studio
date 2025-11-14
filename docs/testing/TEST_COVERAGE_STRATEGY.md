# Test Coverage Strategy

Comprehensive strategy for achieving and maintaining 80% test coverage across the Aura project.

## Coverage Goals

| Component | Target | Current | Priority |
|-----------|--------|---------|----------|
| Core Services | 90% | TBD | P0 |
| API Controllers | 85% | TBD | P0 |
| Business Logic | 90% | TBD | P0 |
| UI Components | 75% | TBD | P1 |
| Utilities | 95% | TBD | P1 |
| Integration | 80% | TBD | P1 |

## Coverage Types

### Line Coverage
Measures which lines of code are executed during tests.
- **Target**: 80%
- **Enforcement**: CI/CD pipeline

### Branch Coverage
Measures which decision branches are tested.
- **Target**: 80%
- **Enforcement**: CI/CD pipeline

### Function Coverage
Measures which functions are called during tests.
- **Target**: 85%
- **Monitoring**: Weekly reports

### Statement Coverage
Measures which statements are executed.
- **Target**: 80%
- **Enforcement**: CI/CD pipeline

## Testing Pyramid

```
           E2E Tests (5%)
         /              \
        /                \
       /  Integration (15%)\
      /                      \
     /                        \
    /    Unit Tests (80%)      \
   /____________________________\
```

### Unit Tests (80%)
- **Purpose**: Test individual functions/components in isolation
- **Speed**: Fast (< 100ms per test)
- **Focus**: Business logic, utilities, pure functions
- **Tools**: xUnit, Vitest, Jest

### Integration Tests (15%)
- **Purpose**: Test component interactions
- **Speed**: Medium (< 1s per test)
- **Focus**: API endpoints, database operations, service integration
- **Tools**: WebApplicationFactory, MSW

### E2E Tests (5%)
- **Purpose**: Test complete user workflows
- **Speed**: Slow (> 1s per test)
- **Focus**: Critical user journeys
- **Tools**: Playwright

## Critical Path Coverage

### Must Cover (P0)

#### Backend
- [ ] Video job processing pipeline
- [ ] FFmpeg integration
- [ ] Provider API calls
- [ ] Authentication/Authorization
- [ ] Data persistence
- [ ] Error handling

#### Frontend
- [ ] Video creation wizard
- [ ] Timeline editor
- [ ] Project management
- [ ] File uploads
- [ ] API integration
- [ ] State management

### Should Cover (P1)

#### Backend
- [ ] Caching layer
- [ ] Background jobs
- [ ] Logging service
- [ ] Configuration management
- [ ] Health checks

#### Frontend
- [ ] Settings management
- [ ] Dashboard analytics
- [ ] Search functionality
- [ ] Notifications
- [ ] Keyboard shortcuts

### Nice to Cover (P2)

#### Backend
- [ ] Admin utilities
- [ ] Diagnostic tools
- [ ] Performance monitoring

#### Frontend
- [ ] Theme switching
- [ ] Accessibility features
- [ ] Advanced filters

## Coverage by Component

### Backend (.NET)

#### Aura.Core
```
Target: 90% coverage

High Priority:
- VideoProcessingService
- ScriptGenerationService
- AssetManagementService
- TimelineService
- JobOrchestrator

Medium Priority:
- CachingService
- LoggingService
- ConfigurationService

Low Priority:
- Models (POCOs)
- DTOs
- Constants
```

#### Aura.Api
```
Target: 85% coverage

High Priority:
- Controllers (all endpoints)
- Middleware
- Filters
- Error handlers

Medium Priority:
- Background services
- SignalR hubs
- Health checks

Low Priority:
- Startup configuration
- Program.cs
```

#### Aura.Providers
```
Target: 85% coverage

High Priority:
- OpenAI integration
- ElevenLabs integration
- FFmpeg wrapper
- Stock footage providers

Medium Priority:
- Provider abstraction
- Rate limiting
- Retry logic

Low Priority:
- Provider configuration
```

### Frontend (React)

#### Components
```
Target: 75% coverage

High Priority:
- Timeline components
- Wizard components
- Form components
- Modal dialogs

Medium Priority:
- Dashboard widgets
- Settings panels
- Navigation components

Low Priority:
- Layout components
- Theme components
- Icon components
```

#### Services
```
Target: 85% coverage

High Priority:
- API client
- State management
- Error handling
- Validation

Medium Priority:
- Local storage
- Caching
- Performance monitoring

Low Priority:
- Logging
- Analytics
```

#### Hooks
```
Target: 80% coverage

High Priority:
- useProjects
- useVideoGeneration
- useTimeline
- useAssets

Medium Priority:
- useAuth
- useSettings
- useNotifications

Low Priority:
- useTheme
- useKeyboard
```

## Exclusions

### Explicitly Excluded from Coverage

```xml
<!-- .NET -->
- Auto-generated code (*.Designer.cs, *.g.cs)
- Migrations
- Program.cs (startup code)
- DTOs/POCOs without logic
- Third-party code

<!-- TypeScript -->
- Test files (*.test.ts, *.spec.ts)
- Type definitions (*.d.ts)
- Configuration files
- Build artifacts
- Node modules
```

## Coverage Enforcement

### Pre-commit Hooks
```bash
# Run tests before commit
npm run test:coverage
dotnet test --collect:"XPlat Code Coverage"

# Fail if below threshold
if coverage < 80% then exit 1
```

### CI/CD Pipeline

```yaml
# Required checks
- Unit tests pass
- Coverage >= 80%
- No failing tests
- No flaky tests

# Warnings
- Coverage decreased
- New code without tests
- Skipped tests
```

### Pull Request Checks

```markdown
## Coverage Report

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Lines  | 82%    | 84%   | +2%    |
| Branch | 78%    | 80%   | +2%    |
| Func   | 85%    | 86%   | +1%    |

✅ Coverage increased
✅ All critical paths covered
✅ No regressions
```

## Measuring Coverage

### Backend

```bash
# Run tests with coverage
cd Aura.Tests
dotnet test --collect:"XPlat Code Coverage" --settings .runsettings

# Generate HTML report
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html;Badges;JsonSummary"

# View report
open coverage-report/index.html
```

### Frontend

```bash
# Run tests with coverage
cd Aura.Web
npm run test:coverage

# View report
open coverage/index.html

# Generate badge
npm run coverage:badge
```

## Coverage Improvement Plan

### Phase 1: Foundation (Weeks 1-2)
- [ ] Set up coverage tooling
- [ ] Add test data builders
- [ ] Create test utilities
- [ ] Achieve 50% coverage baseline

### Phase 2: Critical Paths (Weeks 3-4)
- [ ] Cover video processing
- [ ] Cover wizard flows
- [ ] Cover API endpoints
- [ ] Achieve 65% coverage

### Phase 3: Integration (Weeks 5-6)
- [ ] Add integration tests
- [ ] Cover service interactions
- [ ] Cover database operations
- [ ] Achieve 75% coverage

### Phase 4: Completeness (Weeks 7-8)
- [ ] Fill coverage gaps
- [ ] Add E2E tests
- [ ] Add performance tests
- [ ] Achieve 80% coverage

## Monitoring and Reporting

### Daily Metrics
- Test pass rate
- Test execution time
- Flaky test count

### Weekly Reports
```
Coverage Trend Report
====================
Week 1: 55%
Week 2: 62% (+7%)
Week 3: 70% (+8%)
Week 4: 77% (+7%)
Week 5: 82% (+5%) ✅ Goal reached

Top 5 Uncovered Areas:
1. AdminController - 45%
2. AdvancedFeatures - 52%
3. LegacyCode - 38%
4. ReportGenerator - 60%
5. ExperimentalFeatures - 55%
```

### Coverage Dashboard

```
┌─────────────────────────────────────┐
│      Test Coverage Dashboard        │
├─────────────────────────────────────┤
│                                     │
│  Overall Coverage:      82%  ✅     │
│  Backend (.NET):        84%  ✅     │
│  Frontend (React):      80%  ✅     │
│                                     │
│  Trend:                 ↗ +2%      │
│  Uncovered Lines:       1,234       │
│  Test Count:            2,847       │
│  Execution Time:        8m 32s      │
│                                     │
└─────────────────────────────────────┘
```

## Best Practices

### Writing Testable Code

```csharp
// ❌ Hard to test
public class Service
{
    public void DoWork()
    {
        var data = File.ReadAllText("file.txt");
        var result = ProcessData(data);
        Console.WriteLine(result);
    }
}

// ✅ Easy to test
public class Service
{
    private readonly IFileReader _fileReader;
    private readonly IOutputWriter _writer;

    public Service(IFileReader fileReader, IOutputWriter writer)
    {
        _fileReader = fileReader;
        _writer = writer;
    }

    public void DoWork()
    {
        var data = _fileReader.Read("file.txt");
        var result = ProcessData(data);
        _writer.Write(result);
    }

    internal string ProcessData(string data)
    {
        // Pure function - easy to test
        return data.ToUpper();
    }
}
```

### Focus on Value

```typescript
// ❌ Low value test
it('should have correct class name', () => {
  expect(element.className).toBe('btn btn-primary');
});

// ✅ High value test
it('should submit form when button clicked', async () => {
  const onSubmit = jest.fn();
  render(<Form onSubmit={onSubmit} />);
  
  await userEvent.click(screen.getByRole('button', { name: 'Submit' }));
  
  expect(onSubmit).toHaveBeenCalledWith({
    name: 'Test',
    email: 'test@example.com'
  });
});
```

### Incremental Coverage

```bash
# Add coverage for new code
git diff main --name-only | grep -E '\.(cs|ts|tsx)$' | xargs coverage check

# Prevent regressions
if new_coverage < previous_coverage; then
  echo "Coverage decreased!"
  exit 1
fi
```

## Troubleshooting

### Coverage Not Updating
```bash
# Clean coverage cache
rm -rf TestResults/ coverage/

# Rebuild and rerun
dotnet clean && dotnet build
dotnet test --collect:"XPlat Code Coverage"
```

### False Positives
```csharp
// Exclude from coverage
[ExcludeFromCodeCoverage]
public class GeneratedCode { }

// Exclude specific lines
#pragma warning disable // Coverage
var result = ThirdPartyLib.DoWork();
#pragma warning restore
```

### Slow Coverage Collection
```bash
# Use filters to test specific areas
dotnet test --filter "FullyQualifiedName~MyNamespace"

# Parallel execution
dotnet test --parallel
```

## Resources

- Coverage Reports
- [Test Writing Guide](./TEST_WRITING_GUIDE.md)
- CI/CD Configuration
- Test Data Builders
