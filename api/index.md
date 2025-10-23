# Aura Video Studio API Documentation

This section contains automatically generated API documentation from XML comments in the source code.

## .NET APIs

### Aura.Core

Core business logic and models for video generation.

### Aura.Api

ASP.NET Core backend API with RESTful endpoints.

### Aura.Providers

Provider implementations for LLM, TTS, image, and video generation.

## Building Documentation Locally

To build and preview the documentation:

```bash
# Install DocFX
dotnet tool install -g docfx

# Build documentation
docfx docfx.json

# Serve locally
docfx serve _site
```

Visit http://localhost:8080 to view the documentation.

## Contributing

See the [API Reference](../docs/api/README.md) for information on documenting your code.
