# 002. ASP.NET Core Backend

Date: 2024-01-15
Status: Accepted

## Context

Aura Video Studio requires a backend API to handle video generation orchestration, LLM interactions, asset management, and job processing. We needed to choose a backend framework and technology stack.

Key requirements:
- High performance for video processing operations
- Strong typing to prevent runtime errors
- Good async/await support for I/O-bound operations
- Cross-platform support (Windows, Linux, macOS)
- Strong ecosystem for HTTP APIs
- Good tooling and debugging experience

Options considered:
- ASP.NET Core (C#)
- Node.js (TypeScript)
- Python (FastAPI)
- Go

## Decision

We will use **ASP.NET Core with C# .NET 8** for the backend API.

Key architectural choices:
- Minimal APIs for lightweight endpoint definitions
- Dependency injection for service management
- Serilog for structured logging
- FluentValidation for request validation
- Entity Framework Core for database access
- Server-Sent Events (SSE) for real-time updates

## Consequences

### Positive Consequences

- **Strong typing**: C# static typing catches errors at compile time
- **Performance**: Excellent performance for CPU and I/O-bound operations
- **Async/await**: First-class async support for concurrent operations
- **Mature ecosystem**: Rich library ecosystem for enterprise features
- **Tooling**: Excellent IDE support (Visual Studio, VS Code, Rider)
- **Cross-platform**: Runs on Windows, Linux, and macOS
- **Memory management**: Automatic garbage collection with predictable behavior
- **Language features**: Modern C# features (records, pattern matching, nullable reference types)
- **Testing**: Strong testing frameworks (xUnit, NUnit, MSTest)
- **Documentation**: Comprehensive Microsoft documentation

### Negative Consequences

- **Learning curve**: C# and .NET may be unfamiliar to some developers
- **Verbosity**: More verbose than some dynamic languages
- **Ecosystem bias**: .NET ecosystem is Microsoft-centric
- **Container size**: Larger Docker images compared to Node.js
- **Build time**: Slower compilation compared to interpreted languages

## Alternatives Considered

### Alternative 1: Node.js with TypeScript

**Description:** Use Node.js runtime with TypeScript for type safety.

**Pros:**
- JavaScript/TypeScript knowledge shared with frontend
- Smaller Docker images
- Large npm ecosystem
- Rapid development

**Cons:**
- Weaker type system compared to C#
- Less performant for CPU-intensive operations
- Single-threaded by default (requires clustering)
- Runtime errors more common despite TypeScript
- Less mature enterprise patterns

**Why Rejected:** Video processing is CPU-intensive and benefits from .NET's performance. Type safety is critical for complex orchestration logic, and C# provides stronger guarantees than TypeScript.

### Alternative 2: Python with FastAPI

**Description:** Use Python with FastAPI framework.

**Pros:**
- Excellent for ML/AI integrations
- Simple syntax and rapid development
- Large data science ecosystem
- Good async support with FastAPI

**Cons:**
- Performance limitations compared to compiled languages
- Type hints not enforced at runtime
- GIL limits parallel processing
- Weaker IDE tooling compared to C#/Java
- Package management challenges

**Why Rejected:** Performance concerns for video processing and weaker type safety. While Python excels at ML integrations, Aura interacts with ML services via HTTP APIs, so direct Python ML library access is not critical.

### Alternative 3: Go

**Description:** Use Go for backend services.

**Pros:**
- Excellent performance
- Built-in concurrency primitives
- Fast compilation
- Simple deployment (single binary)
- Strong standard library

**Cons:**
- Less expressive type system
- Smaller ecosystem for enterprise features
- No generics until recently
- Error handling can be verbose
- Fewer developers familiar with Go

**Why Rejected:** While Go offers excellent performance, C# provides a better balance of performance, type safety, and ecosystem maturity. The .NET ecosystem has more mature libraries for video processing, database access, and enterprise patterns.

## References

- [ASP.NET Core Performance](https://www.techempower.com/benchmarks/)
- [.NET 8 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

## Notes

The decision to use ASP.NET Core aligns with the project's Windows-first approach (due to WinUI desktop app) while maintaining cross-platform capability for server deployments.

The combination of C#'s type safety, .NET's performance, and ASP.NET Core's features provides a solid foundation for the complex orchestration and video processing requirements of Aura Video Studio.
