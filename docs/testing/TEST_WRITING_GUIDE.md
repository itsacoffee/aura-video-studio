# Test Writing Guide

This guide provides best practices and patterns for writing tests in the Aura project.

## Table of Contents

- [Test Structure](#test-structure)
- [Backend Testing (.NET)](#backend-testing-net)
- [Frontend Testing (React)](#frontend-testing-react)
- [Test Data Builders](#test-data-builders)
- [Integration Tests](#integration-tests)
- [Performance Tests](#performance-tests)
- [Best Practices](#best-practices)

## Test Structure

### Test Organization

```
Aura.Tests/
├── TestDataBuilders/      # Reusable test data builders
├── Integration/           # Integration test suites
│   ├── EndpointTests/    # API endpoint tests
│   └── *.cs              # Base classes
├── Performance/          # Performance test suites
└── *.cs                  # Unit tests (colocated with tested code)

Aura.Web/
├── src/
│   ├── test/
│   │   ├── utils/        # Test utilities
│   │   ├── setup.ts      # Global test setup
│   │   └── *.test.tsx    # Unit tests
│   └── **/__tests__/     # Tests colocated with code
└── tests/
    ├── e2e/              # End-to-end tests
    ├── integration/      # Integration tests
    └── smoke/            # Smoke tests
```

### Naming Conventions

#### Backend (.NET)

```csharp
// File: ServiceNameTests.cs
public class ServiceNameTests
{
    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

#### Frontend (TypeScript)

```typescript
// File: ComponentName.test.tsx
describe('ComponentName', () => {
  it('should render correctly', () => {
    // Arrange
    // Act
    // Assert
  });
  
  it('should handle user interaction', async () => {
    // Arrange
    // Act
    // Assert
  });
});
```

## Backend Testing (.NET)

### Unit Tests with xUnit

```csharp
using Aura.Tests.TestDataBuilders;
using FluentAssertions;
using Moq;
using Xunit;

public class VideoServiceTests
{
    private readonly Mock<IVideoRepository> _mockRepository;
    private readonly VideoService _sut; // System Under Test

    public VideoServiceTests()
    {
        _mockRepository = new Mock<IVideoRepository>();
        _sut = new VideoService(_mockRepository.Object);
    }

    [Fact]
    public async Task ProcessVideo_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var job = new VideoJobBuilder()
            .WithProjectId("test-123")
            .Build();
        
        _mockRepository
            .Setup(r => r.SaveAsync(It.IsAny<VideoJob>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ProcessVideoAsync(job);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.SaveAsync(job), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProcessVideo_WithInvalidProjectId_ThrowsException(string? projectId)
    {
        // Arrange
        var job = new VideoJobBuilder()
            .WithProjectId(projectId!)
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ProcessVideoAsync(job)
        );
    }
}
```

### Integration Tests

```csharp
using Aura.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;

public class ProjectsApiTests : ApiIntegrationTestBase
{
    public ProjectsApiTests(WebApplicationFactory<Program> factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateProject_ReturnsCreatedProject()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "Test Project",
            Description = "Integration test"
        };

        // Act
        var response = await PostAsync<CreateProjectRequest, ProjectDto>(
            "/api/projects", 
            request
        );

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Name, response.Name);
    }
}
```

### Using Test Data Builders

```csharp
// Simple usage
var project = new ProjectBuilder().Build();

// Customized
var project = new ProjectBuilder()
    .WithName("My Project")
    .WithOwnerId("user-123")
    .WithTag("tutorial")
    .Archived()
    .Build();

// Complex scenarios
var timeline = new TimelineBuilder()
    .WithDuration(120.0)
    .WithTrack(new TrackBuilder()
        .WithType(TrackType.Video)
        .WithClip(new ClipBuilder()
            .AtTime(0.0)
            .WithDuration(5.0)
            .Build())
        .Build())
    .Build();
```

## Frontend Testing (React)

### Component Tests

```typescript
import { renderWithProviders, screen, userEvent } from '@/test/utils/testUtils';
import { MyComponent } from './MyComponent';

describe('MyComponent', () => {
  it('should render with props', () => {
    renderWithProviders(<MyComponent title="Test" />);
    
    expect(screen.getByText('Test')).toBeInTheDocument();
  });

  it('should handle button click', async () => {
    const onClickMock = jest.fn();
    const user = userEvent.setup();
    
    renderWithProviders(<MyComponent onClick={onClickMock} />);
    
    await user.click(screen.getByRole('button'));
    
    expect(onClickMock).toHaveBeenCalledTimes(1);
  });

  it('should display loading state', () => {
    renderWithProviders(<MyComponent isLoading />);
    
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });
});
```

### Hook Tests

```typescript
import { renderHookWithProviders, waitFor } from '@/test/utils/hookTestUtils';
import { useProjects } from './useProjects';

describe('useProjects', () => {
  it('should fetch projects on mount', async () => {
    const { result } = renderHookWithProviders(() => useProjects());

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.projects).toHaveLength(0);
  });

  it('should handle errors gracefully', async () => {
    // Mock API to return error
    const { result } = renderHookWithProviders(() => useProjects());

    await waitFor(() => {
      expect(result.current.error).toBeDefined();
    });
  });
});
```

### Testing with React Query

```typescript
import { renderWithProviders, waitFor } from '@/test/utils/testUtils';
import { QueryClient } from '@tanstack/react-query';
import { server } from '@/test/mocks/server';
import { rest } from 'msw';

describe('ProjectList', () => {
  it('should display projects from API', async () => {
    // Mock API response
    server.use(
      rest.get('/api/projects', (req, res, ctx) => {
        return res(ctx.json([
          { id: '1', name: 'Project 1' },
          { id: '2', name: 'Project 2' },
        ]));
      })
    );

    renderWithProviders(<ProjectList />);

    await waitFor(() => {
      expect(screen.getByText('Project 1')).toBeInTheDocument();
      expect(screen.getByText('Project 2')).toBeInTheDocument();
    });
  });
});
```

### E2E Tests with Playwright

```typescript
import { test, expect } from '@playwright/test';

test.describe('Video Creation Workflow', () => {
  test('should create video from wizard', async ({ page }) => {
    // Navigate to create page
    await page.goto('/create');

    // Fill in form
    await page.fill('input[name="title"]', 'My Test Video');
    await page.fill('textarea[name="description"]', 'Test description');

    // Submit
    await page.click('button[type="submit"]');

    // Verify redirect and success message
    await expect(page).toHaveURL(/\/projects\/\w+/);
    await expect(page.locator('.success-message')).toBeVisible();
  });

  test('should handle validation errors', async ({ page }) => {
    await page.goto('/create');

    // Submit without filling required fields
    await page.click('button[type="submit"]');

    // Verify error messages
    await expect(page.locator('.error')).toContainText('Title is required');
  });
});
```

## Test Data Builders

Always use test data builders instead of creating test objects manually:

### Benefits

1. **Consistency**: Same defaults across all tests
2. **Readability**: Fluent interface is self-documenting
3. **Maintainability**: Changes to models require updates in one place
4. **Flexibility**: Easy to customize while keeping defaults

### Creating a New Builder

```csharp
public class MyModelBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Default Name";
    private DateTime _createdAt = DateTime.UtcNow;

    public MyModelBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public MyModelBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public MyModelBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public MyModel Build()
    {
        return new MyModel
        {
            Id = _id,
            Name = _name,
            CreatedAt = _createdAt
        };
    }
}
```

## Integration Tests

Integration tests verify that multiple components work together correctly.

### API Integration Tests

```csharp
[Collection("Integration Tests")]
public class VideoWorkflowTests : IntegrationTestBase
{
    public VideoWorkflowTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompleteVideoWorkflow_CreatesToCompletion()
    {
        // Create project
        var project = await CreateProjectAsync();

        // Create video job
        var job = await CreateVideoJobAsync(project.Id);

        // Process job
        await ProcessJobAsync(job.Id);

        // Verify completion
        var result = await GetJobStatusAsync(job.Id);
        Assert.Equal(JobStatus.Completed, result.Status);
    }
}
```

## Performance Tests

Performance tests ensure operations complete within acceptable time limits.

```csharp
public class TimelinePerformanceTests : PerformanceTestBase
{
    public TimelinePerformanceTests(ITestOutputHelper output) 
        : base(output)
    {
    }

    [Fact]
    public async Task TimelineRendering_CompletesUnderThreshold()
    {
        // Arrange
        var timeline = new TimelineBuilder()
            .WithDuration(300.0) // 5 minutes
            .WithDefaultVideoTrack()
            .WithDefaultAudioTrack()
            .Build();

        var threshold = TimeSpan.FromMilliseconds(100);

        // Act
        var duration = await MeasureAsync(async () =>
        {
            await RenderTimelineAsync(timeline);
        }, "Timeline Rendering");

        // Assert
        AssertPerformance(duration, threshold);
        PrintPerformanceSummary();
    }
}
```

## Best Practices

### General

1. **Arrange-Act-Assert Pattern**: Structure all tests this way
2. **One Assertion Per Test**: Focus on testing one thing
3. **Descriptive Test Names**: Explain what is being tested and expected
4. **Independent Tests**: Tests should not depend on each other
5. **Fast Tests**: Keep unit tests fast (< 100ms)

### Test Coverage

- **Aim for 80% coverage** minimum
- **Focus on critical paths** first
- **Test edge cases** and error conditions
- **Don't test framework code** (e.g., React internals)

### Mocking

```csharp
// Mock setup
_mockService
    .Setup(s => s.GetAsync(It.IsAny<string>()))
    .ReturnsAsync(expectedResult);

// Verify calls
_mockService.Verify(
    s => s.GetAsync("123"),
    Times.Once,
    "Service should be called exactly once"
);

// Verify no unexpected calls
_mockService.VerifyNoOtherCalls();
```

### Async Testing

```csharp
// Backend
[Fact]
public async Task AsyncMethod_CompletesSuccessfully()
{
    var result = await _sut.DoWorkAsync();
    Assert.NotNull(result);
}

// Frontend
it('should handle async operation', async () => {
    renderWithProviders(<AsyncComponent />);
    
    await waitFor(() => {
        expect(screen.getByText('Loaded')).toBeInTheDocument();
    });
});
```

### Cleanup

```csharp
// Backend
public void Dispose()
{
    _scope?.Dispose();
    _context?.Dispose();
}

// Frontend
afterEach(() => {
    jest.clearAllMocks();
    cleanup();
});
```

### Avoiding Flaky Tests

1. **Avoid timing dependencies**: Use `waitFor` instead of fixed delays
2. **Clean up resources**: Dispose properly
3. **Use deterministic data**: Avoid random data in assertions
4. **Isolate tests**: Don't share state between tests
5. **Mock external dependencies**: Don't rely on real APIs

## Code Examples Repository

See the `Aura.Tests/Examples/` directory for complete, runnable examples of:

- Unit tests
- Integration tests
- Performance tests
- E2E tests
- Custom matchers and utilities

## Further Reading

- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [Testing Library Best Practices](https://testing-library.com/docs/guiding-principles)
- [Playwright Best Practices](https://playwright.dev/docs/best-practices)
