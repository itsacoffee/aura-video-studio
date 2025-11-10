# Mocking Best Practices

This guide covers mocking strategies and best practices for the Aura project.

## Table of Contents

- [Backend Mocking (.NET)](#backend-mocking-net)
- [Frontend Mocking (TypeScript)](#frontend-mocking-typescript)
- [API Mocking](#api-mocking)
- [Common Patterns](#common-patterns)

## Backend Mocking (.NET)

### Using Moq

#### Basic Setup

```csharp
using Moq;

public class ServiceTests
{
    private readonly Mock<IDependency> _mockDependency;
    private readonly ServiceUnderTest _sut;

    public ServiceTests()
    {
        _mockDependency = new Mock<IDependency>();
        _sut = new ServiceUnderTest(_mockDependency.Object);
    }
}
```

#### Method Setup

```csharp
// Simple return value
_mockDependency
    .Setup(d => d.GetValue())
    .Returns(42);

// Async method
_mockDependency
    .Setup(d => d.GetValueAsync())
    .ReturnsAsync(42);

// With parameters
_mockDependency
    .Setup(d => d.GetById(It.IsAny<string>()))
    .Returns((string id) => new Entity { Id = id });

// Conditional setup
_mockDependency
    .Setup(d => d.GetById(It.Is<string>(id => id.StartsWith("test"))))
    .Returns(testEntity);

// Throwing exceptions
_mockDependency
    .Setup(d => d.GetById("invalid"))
    .Throws<NotFoundException>();

// Callback
_mockDependency
    .Setup(d => d.Save(It.IsAny<Entity>()))
    .Callback<Entity>(e => Console.WriteLine($"Saving {e.Id}"))
    .Returns(true);
```

#### Verification

```csharp
// Verify method was called
_mockDependency.Verify(
    d => d.GetById("123"),
    Times.Once
);

// Verify never called
_mockDependency.Verify(
    d => d.Delete(It.IsAny<string>()),
    Times.Never
);

// Verify call count
_mockDependency.Verify(
    d => d.Save(It.IsAny<Entity>()),
    Times.Exactly(3)
);

// Verify no other calls
_mockDependency.VerifyNoOtherCalls();
```

### Mocking HttpClient

```csharp
using System.Net;
using System.Net.Http;
using Moq;
using Moq.Protected;

public class HttpClientTests
{
    [Fact]
    public async Task GetData_ReturnsExpectedResult()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"value\":42}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new HttpService(httpClient);

        // Act
        var result = await service.GetDataAsync();

        // Assert
        Assert.Equal(42, result.Value);
    }
}
```

### Mocking DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class RepositoryTests
{
    [Fact]
    public async Task GetAll_ReturnsAllEntities()
    {
        // Arrange - Use InMemory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        context.Entities.AddRange(
            new Entity { Id = "1", Name = "Test 1" },
            new Entity { Id = "2", Name = "Test 2" }
        );
        await context.SaveChangesAsync();

        var repository = new Repository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }
}
```

## Frontend Mocking (TypeScript)

### Mocking Functions

```typescript
// Using jest.fn()
const mockCallback = jest.fn();

// With implementation
const mockCallback = jest.fn((x) => x * 2);

// With return value
const mockGetValue = jest.fn().mockReturnValue(42);

// With async return
const mockFetchData = jest.fn().mockResolvedValue({ data: 'test' });

// With rejection
const mockFail = jest.fn().mockRejectedValue(new Error('Failed'));

// Assertions
expect(mockCallback).toHaveBeenCalled();
expect(mockCallback).toHaveBeenCalledWith('arg1', 'arg2');
expect(mockCallback).toHaveBeenCalledTimes(3);
expect(mockCallback).not.toHaveBeenCalled();
```

### Mocking Modules

```typescript
// Mock entire module
jest.mock('@/services/api');

// Mock with factory
jest.mock('@/services/api', () => ({
  fetchProjects: jest.fn().mockResolvedValue([]),
  createProject: jest.fn(),
}));

// Partial mock
jest.mock('@/services/api', () => ({
  ...jest.requireActual('@/services/api'),
  fetchProjects: jest.fn(),
}));

// Import mocked module
import { fetchProjects } from '@/services/api';

// Type-safe mock
const mockFetchProjects = fetchProjects as jest.MockedFunction<typeof fetchProjects>;
mockFetchProjects.mockResolvedValue([{ id: '1', name: 'Test' }]);
```

### Mocking React Query

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderWithProviders } from '@/test/utils/testUtils';

describe('Component with queries', () => {
  it('should display data from query', async () => {
    // Create test query client
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });

    // Pre-populate cache
    queryClient.setQueryData(['projects'], [
      { id: '1', name: 'Test Project' },
    ]);

    renderWithProviders(<MyComponent />, { queryClient });

    expect(screen.getByText('Test Project')).toBeInTheDocument();
  });
});
```

### Mocking Zustand Stores

```typescript
import { renderHook } from '@testing-library/react';
import { useStore } from '@/stores/myStore';

// Mock store state
jest.mock('@/stores/myStore', () => ({
  useStore: jest.fn(),
}));

describe('Component using store', () => {
  it('should use store data', () => {
    // Setup mock store state
    const mockStore = {
      value: 42,
      setValue: jest.fn(),
    };
    
    (useStore as jest.Mock).mockReturnValue(mockStore);

    const { result } = renderHook(() => useStore());
    
    expect(result.current.value).toBe(42);
  });
});
```

## API Mocking

### Using MSW (Mock Service Worker)

```typescript
// src/test/mocks/handlers.ts
import { rest } from 'msw';

export const handlers = [
  rest.get('/api/projects', (req, res, ctx) => {
    return res(
      ctx.status(200),
      ctx.json([
        { id: '1', name: 'Project 1' },
        { id: '2', name: 'Project 2' },
      ])
    );
  }),

  rest.post('/api/projects', async (req, res, ctx) => {
    const body = await req.json();
    return res(
      ctx.status(201),
      ctx.json({ ...body, id: 'new-id' })
    );
  }),

  rest.get('/api/projects/:id', (req, res, ctx) => {
    const { id } = req.params;
    return res(
      ctx.status(200),
      ctx.json({ id, name: `Project ${id}` })
    );
  }),

  // Error response
  rest.delete('/api/projects/:id', (req, res, ctx) => {
    return res(
      ctx.status(404),
      ctx.json({ error: 'Project not found' })
    );
  }),
];
```

```typescript
// src/test/mocks/server.ts
import { setupServer } from 'msw/node';
import { handlers } from './handlers';

export const server = setupServer(...handlers);
```

```typescript
// src/test/setup.ts
import { server } from './mocks/server';

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

```typescript
// In tests
import { server } from '@/test/mocks/server';
import { rest } from 'msw';

describe('API Integration', () => {
  it('should handle API error', async () => {
    // Override handler for this test
    server.use(
      rest.get('/api/projects', (req, res, ctx) => {
        return res(ctx.status(500), ctx.json({ error: 'Server error' }));
      })
    );

    renderWithProviders(<ProjectList />);

    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });
});
```

### Axios Mock Adapter

```typescript
import axios from 'axios';
import MockAdapter from 'axios-mock-adapter';

describe('API Service', () => {
  let mock: MockAdapter;

  beforeEach(() => {
    mock = new MockAdapter(axios);
  });

  afterEach(() => {
    mock.restore();
  });

  it('should fetch projects', async () => {
    mock.onGet('/api/projects').reply(200, [
      { id: '1', name: 'Test' },
    ]);

    const result = await fetchProjects();
    
    expect(result).toHaveLength(1);
  });

  it('should handle network error', async () => {
    mock.onGet('/api/projects').networkError();

    await expect(fetchProjects()).rejects.toThrow();
  });

  it('should handle timeout', async () => {
    mock.onGet('/api/projects').timeout();

    await expect(fetchProjects()).rejects.toThrow();
  });
});
```

## Common Patterns

### Mock Factory

```csharp
public static class MockFactory
{
    public static Mock<IRepository<T>> CreateRepository<T>() where T : class
    {
        var mock = new Mock<IRepository<T>>();
        mock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<T>());
        return mock;
    }

    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}
```

```typescript
// mockFactory.ts
export const createMockApi = () => ({
  fetchProjects: jest.fn().mockResolvedValue([]),
  createProject: jest.fn().mockResolvedValue({ id: 'new' }),
  updateProject: jest.fn().mockResolvedValue({}),
  deleteProject: jest.fn().mockResolvedValue(undefined),
});
```

### Spy Pattern

```csharp
// Spy on real implementation
var realService = new RealService();
var spy = Mock.Get(realService);

spy.Setup(s => s.MethodToSpy()).CallBase();

// Use real service
await realService.DoWork();

// Verify spy
spy.Verify(s => s.MethodToSpy(), Times.Once);
```

```typescript
// Jest spy
const api = {
  fetchData: () => Promise.resolve([1, 2, 3]),
};

const spy = jest.spyOn(api, 'fetchData');

await api.fetchData();

expect(spy).toHaveBeenCalled();
spy.mockRestore();
```

### Partial Mocking

```csharp
// Mock only specific methods
var mock = new Mock<IService>();
mock.CallBase = true; // Use real implementation for unmocked methods

mock.Setup(s => s.SpecificMethod()).Returns(42);
```

```typescript
// Partial module mock
jest.mock('@/services/api', () => ({
  ...jest.requireActual('@/services/api'),
  fetchProjects: jest.fn(), // Only mock this one
}));
```

## Best Practices

1. **Mock at the boundaries**: Mock external dependencies, not internal logic
2. **Use real objects when possible**: Prefer in-memory databases over mocked DbContext
3. **Reset mocks between tests**: Avoid state leakage
4. **Verify behavior, not implementation**: Focus on observable outcomes
5. **Keep mocks simple**: Complex mocks indicate design problems
6. **Document mock behavior**: Explain why mocking is necessary
7. **Avoid over-mocking**: Too many mocks make tests brittle
8. **Use test doubles appropriately**:
   - **Stub**: Provides predetermined responses
   - **Mock**: Verifies interactions
   - **Spy**: Records calls on real objects
   - **Fake**: Working implementation with shortcuts

## Anti-Patterns to Avoid

```csharp
// ❌ Don't: Mock everything
var mockA = new Mock<IServiceA>();
var mockB = new Mock<IServiceB>();
var mockC = new Mock<IServiceC>();
// ... this is too much

// ✅ Do: Mock only external dependencies
var mockExternalApi = new Mock<IExternalApi>();
var realServiceA = new ServiceA();
var realServiceB = new ServiceB();
```

```typescript
// ❌ Don't: Mock implementation details
const mock = jest.fn().mockImplementation(() => {
  // Complex logic that duplicates production code
});

// ✅ Do: Mock at appropriate abstraction level
const mock = jest.fn().mockResolvedValue(expectedResult);
```

## Troubleshooting

### Mock Not Being Called

```csharp
// Check setup matches exactly
_mock.Setup(s => s.Method("exact-string")).Returns(42);
await _sut.DoWork(); // Calls Method("other-string")

// Use It.IsAny for flexible matching
_mock.Setup(s => s.Method(It.IsAny<string>())).Returns(42);
```

### Async Method Not Awaited

```csharp
// ❌ Wrong
_mock.Setup(s => s.MethodAsync()).Returns(Task.FromResult(42));

// ✅ Correct
_mock.Setup(s => s.MethodAsync()).ReturnsAsync(42);
```

### Mock Verification Fails

```csharp
// Check exact match
_mock.Verify(s => s.Method("test"), Times.Once);

// Use ItExpr for flexible matching
_mock.Verify(
    s => s.Method(It.Is<string>(x => x.StartsWith("t"))),
    Times.Once
);
```
