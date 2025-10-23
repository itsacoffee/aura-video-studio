# API Reference

This section contains the automatically generated API documentation for Aura Video Studio.

## Overview

Aura Video Studio provides several APIs for different purposes:

### Backend API (REST)

The **Aura.Api** project exposes a RESTful API for the web frontend and external integrations.

- [API Contract v1](./API_CONTRACT_V1.md) - Complete REST API specification
- [Error Handling](./errors.md) - Error codes and responses
- [Health Endpoints](./health.md) - Health check and diagnostics
- [Job Management](./jobs.md) - Background job processing
- [Provider System](./providers.md) - Provider configuration and management

### Core Libraries

The following .NET libraries provide the core functionality:

- **Aura.Core** - Business logic and orchestration
- **Aura.Providers** - Provider implementations (LLM, TTS, Image, Video)
- **Aura.Api** - ASP.NET Core backend API

## API Documentation Generation

This API documentation is generated from XML documentation comments in the source code using DocFX.

### For C# Developers

To contribute to the API documentation:

1. Add XML documentation comments to public classes and methods:

```csharp
/// <summary>
/// Represents a video generation request.
/// </summary>
public class VideoRequest
{
    /// <summary>
    /// Gets or sets the brief for video generation.
    /// </summary>
    /// <value>The video brief with title and description.</value>
    public Brief Brief { get; set; }
    
    /// <summary>
    /// Generates a video from the request.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the video path.</returns>
    /// <exception cref="ValidationException">Thrown when the request is invalid.</exception>
    public async Task<string> GenerateAsync()
    {
        // Implementation
    }
}
```

2. Build the documentation locally:

```bash
dotnet tool install -g docfx
docfx docfx.json --serve
```

3. View the generated documentation at `http://localhost:8080`

### For TypeScript Developers

For the web frontend (Aura.Web), use JSDoc comments:

```typescript
/**
 * Represents a video generation request.
 */
export interface VideoRequest {
  /**
   * The brief for video generation.
   */
  brief: Brief;
  
  /**
   * Optional settings for generation.
   */
  settings?: GenerationSettings;
}

/**
 * Generates a video from the request.
 * @param request - The video generation request
 * @returns A promise that resolves to the video path
 * @throws {ValidationError} When the request is invalid
 */
export async function generateVideo(request: VideoRequest): Promise<string> {
  // Implementation
}
```

## REST API Endpoints

### Health & Status

```
GET /healthz           - Health check
GET /capabilities      - System capabilities
```

### Video Generation

```
POST /script          - Generate script from brief
POST /plan            - Create timeline plan
POST /tts             - Synthesize narration
POST /render          - Render video
```

### Settings & Configuration

```
GET /settings/load    - Load settings
POST /settings/save   - Save settings
```

### Asset Management

```
GET /assets/search    - Search for assets
POST /assets/upload   - Upload custom assets
```

### Provider Management

```
GET /providers        - List available providers
GET /providers/{id}   - Get provider details
POST /providers/{id}  - Configure provider
```

## Authentication

The API currently runs locally on `http://127.0.0.1:5005` and does not require authentication. For production deployments, implement appropriate authentication and authorization mechanisms.

## Rate Limiting

When using external providers (OpenAI, ElevenLabs, etc.), be aware of their rate limits:

- **OpenAI**: Varies by plan (typically 3-60 requests/minute)
- **ElevenLabs**: Character limits per month
- **Stock APIs**: Daily request limits

The system automatically handles rate limiting and retries with exponential backoff.

## Error Handling

All API endpoints return consistent error responses:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request parameters",
    "details": {
      "field": "brief.title",
      "issue": "Title is required"
    }
  }
}
```

See [Error Handling](./errors.md) for complete error code reference.

## Versioning

The API follows semantic versioning (SemVer):

- Current version: **v1.0.0**
- Breaking changes will increment major version
- New features increment minor version
- Bug fixes increment patch version

API v1 is the current stable version. When v2 is released, v1 will be maintained for at least 6 months.

## SDK & Client Libraries

### Official SDKs

- **C# / .NET**: Use Aura.Core directly
- **TypeScript / JavaScript**: Aura.Web provides client utilities

### Community SDKs

- Python SDK (coming soon)
- Go SDK (coming soon)

## Support

For API questions and issues:

- Check [Troubleshooting](../troubleshooting/Troubleshooting.md)
- Search [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues)
- Review [API Contract](./API_CONTRACT_V1.md) for specifications
- Join [GitHub Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions)

---

**API Version**: 1.0.0  
**Last Updated**: 2025-10-23
