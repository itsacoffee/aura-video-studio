# Aura.Api - Backend API Service

## Overview

Aura.Api is the ASP.NET Core backend API for Aura Video Studio. It provides RESTful endpoints for the web UI and handles all business logic through integration with Aura.Core and Aura.Providers.

## Features

- **Minimal API**: Lightweight ASP.NET Core 8 minimal API
- **Health Checks**: `/healthz` endpoint for monitoring
- **Hardware Detection**: Real-time system capabilities
- **Script Generation**: LLM-based script creation
- **TTS Integration**: Text-to-speech synthesis
- **Settings Persistence**: Save and load user settings
- **Structured Logging**: Serilog with file rolling
- **CORS Support**: Configured for local web UI development

## Quick Start

### Prerequisites
- .NET 8 SDK
- Windows 11 (for full functionality) or Linux (for development)

### Running the API

```bash
# Restore dependencies
dotnet restore

# Run in development mode
dotnet run

# Or run in release mode
dotnet run --configuration Release
```

The API will start on `http://127.0.0.1:5005` by default.

### Testing the API

```bash
# Health check
curl http://127.0.0.1:5005/healthz

# Get system capabilities
curl http://127.0.0.1:5005/capabilities

# Access Swagger UI
# Open browser to http://127.0.0.1:5005/swagger
```

## API Endpoints

### Core Endpoints

#### `GET /healthz`
Health check endpoint.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### `GET /capabilities`
Returns detected system hardware capabilities.

**Response**:
```json
{
  "tier": "B",
  "cpu": { "cores": 8, "threads": 16 },
  "ram": { "gb": 16 },
  "gpu": { "model": "NVIDIA RTX 3070", "vramGB": 8, "vendor": "NVIDIA" },
  "enableNVENC": true,
  "enableSD": true,
  "offlineOnly": false
}
```

#### `POST /plan`
Create a timeline plan.

**Request**:
```json
{
  "targetDurationMinutes": 5,
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "Informative"
}
```

#### `POST /script`
Generate a script from a brief.

**Request**:
```json
{
  "topic": "Introduction to Machine Learning",
  "audience": "Beginners",
  "goal": "Educational",
  "tone": "Informative",
  "language": "English",
  "aspect": "Widescreen16x9",
  "targetDurationMinutes": 5,
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "Informative"
}
```

**Response**:
```json
{
  "success": true,
  "script": [
    {
      "index": 0,
      "heading": "Introduction",
      "script": "Welcome to our guide on Machine Learning...",
      "start": "00:00:00",
      "duration": "00:00:30"
    }
  ]
}
```

#### `POST /tts`
Synthesize audio from script lines.

**Request**:
```json
{
  "lines": [
    {
      "sceneIndex": 0,
      "text": "Welcome to the tutorial",
      "startSeconds": 0,
      "durationSeconds": 5
    }
  ],
  "voiceName": "Microsoft David Desktop",
  "rate": 1.0,
  "pitch": 0,
  "pauseStyle": "Natural"
}
```

#### `POST /compose`
Compose a timeline for rendering.

**Request**:
```json
{
  "timelineJson": "{...}"
}
```

**Response**:
```json
{
  "success": true,
  "jobId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### `POST /render`
Start a render job.

**Request**:
```json
{
  "timelineJson": "{...}",
  "presetName": "YouTube 1080p"
}
```

**Response**:
```json
{
  "success": true,
  "jobId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### `GET /render/{id}/progress`
Get render progress for a specific job.

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "rendering",
  "progress": 45.5,
  "outputPath": null,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### `POST /render/{id}/cancel`
Cancel a render job.

**Response**:
```json
{
  "success": true
}
```

#### `GET /queue`
Get all render jobs.

**Response**:
```json
{
  "jobs": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "status": "completed",
      "progress": 100,
      "outputPath": "/path/to/output.mp4",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

#### `GET /logs/stream`
Server-Sent Events (SSE) endpoint for streaming logs.

**Response** (text/event-stream):
```
data: {"timestamp":"2024-01-15T10:30:00Z","level":"INFO","message":"Log stream connected"}

data: {"timestamp":"2024-01-15T10:30:01Z","level":"INFO","message":"Rendering started"}
```

#### `GET /downloads/manifest`
Get download manifest for dependencies.

**Response**:
```json
{
  "items": [
    {
      "name": "FFmpeg",
      "version": "6.1",
      "url": "https://...",
      "sha256": "abc123...",
      "sizeBytes": 89000000,
      "installPath": "C:\\Aura\\ffmpeg\\bin",
      "required": true
    }
  ]
}
```

#### `POST /settings/save`
Save user settings.

**Request**:
```json
{
  "offlineMode": false,
  "profile": "Balanced Mix",
  "theme": "light"
}
```

**Response**:
```json
{
  "success": true
}
```

#### `GET /settings/load`
Load user settings.

**Response**:
```json
{
  "offlineMode": false,
  "profile": "Balanced Mix",
  "theme": "light"
}
```

#### `POST /probes/run`
Run hardware probes.

**Response**:
```json
{
  "success": true,
  "profile": {
    "tier": "B",
    "enableNVENC": true,
    "enableSD": true
  }
}
```

#### `GET /diagnostics`
Generate comprehensive diagnostics report (text format).

**Response**:
```json
{
  "success": true,
  "report": "=== Aura Video Studio Diagnostics Report ===\nGenerated: 2025-10-11 17:30:00 UTC\n\n--- System Profile ---\nTier: A\nCPU Cores: 8 physical, 16 logical\nRAM: 32 GB\nGPU: NVIDIA GeForce RTX 3080\nVRAM: 10 GB\n..."
}
```

**Use Cases**:
- Copy diagnostics to clipboard for support requests
- Generate bug reports
- Troubleshoot installation issues

#### `GET /diagnostics/json`
Generate diagnostics report (JSON format).

**Response**:
```json
{
  "timestamp": "2025-10-11T17:30:00Z",
  "systemProfile": {
    "tier": "A",
    "cpu": { "physical": 8, "logical": 16 },
    "ram": { "gb": 32 },
    "gpu": {
      "vendor": "NVIDIA",
      "model": "GeForce RTX 3080",
      "vramGB": 10,
      "series": "30"
    },
    "enableNVENC": true,
    "enableSD": true,
    "offlineOnly": false
  },
  "environment": {
    "os": "Microsoft Windows NT 10.0.19045.0",
    "platform": "Win32NT",
    "is64BitOS": true,
    "is64BitProcess": true,
    "dotnetVersion": "8.0.0"
  },
  "logsLocation": "C:\\Users\\...\\AppData\\Local\\Aura\\logs"
}
```

#### `GET /profiles/list`
List available provider profiles.

**Response**:
```json
{
  "profiles": [
    {
      "name": "Free-Only",
      "description": "Uses only free providers (no API keys required)"
    },
    {
      "name": "Balanced Mix",
      "description": "Pro providers with free fallbacks"
    },
    {
      "name": "Pro-Max",
      "description": "All pro providers (requires API keys)"
    }
  ]
}
```

#### `POST /profiles/apply`
Apply a provider profile.

**Request**:
```json
{
  "profileName": "Balanced Mix"
}
```

**Response**:
```json
{
  "success": true
}
```

**Response**:
```json
{
  "success": true,
  "audioPath": "C:\\Users\\...\\narration.wav"
}
```

#### `POST /settings/save`
Save user settings.

**Request**:
```json
{
  "theme": "dark",
  "apiKeys": {
    "openai": "sk-..."
  }
}
```

#### `GET /settings/load`
Load user settings.

**Response**:
```json
{
  "theme": "dark",
  "apiKeys": {}
}
```

#### `POST /ml/train/frame-importance`
Train the frame importance ML model with user-annotated frame data.

**Request**:
```json
{
  "annotations": [
    {
      "framePath": "/path/to/frame1.jpg",
      "rating": 0.85
    },
    {
      "framePath": "/path/to/frame2.jpg",
      "rating": 0.42
    }
  ]
}
```

**Response**:
```json
{
  "success": true,
  "modelPath": "/path/to/ML/PretrainedModels/frame-importance-model.zip",
  "trainingSamples": 2,
  "trainingDurationSeconds": 1.234,
  "errorMessage": null
}
```

**Description**:
- Accepts user-annotated frame data to retrain the frame importance model
- `framePath`: Path to the frame image file
- `rating`: Importance rating from 0.0 (not important) to 1.0 (very important)
- The newly trained model replaces the existing one in `Aura.Core/ML/PretrainedModels/`
- A backup of the previous model is created automatically

#### `GET /downloads/manifest`
Get the dependency download manifest.

**Response**: Returns the `manifest.json` file with component versions, URLs, and SHA-256 hashes.

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://127.0.0.1:5005"
}
```

### Logging

Logs are written to:
- Console (always)
- `logs/aura-api-{date}.log` (rolling daily, 7 days retention)

Log files are created in the API's working directory.

## Development

### Adding a New Endpoint

1. Open `Program.cs`
2. Add your endpoint using minimal API syntax:

```csharp
app.MapGet("/myendpoint", () =>
{
    return Results.Ok(new { message = "Hello" });
})
.WithName("MyEndpoint")
.WithOpenApi();
```

3. Restart the API to see changes

### Dependency Injection

Services are registered in `Program.cs`:

```csharp
builder.Services.AddSingleton<HardwareDetector>();
builder.Services.AddSingleton<ILlmProvider, RuleBasedLlmProvider>();
```

Access them via parameters in endpoint handlers:

```csharp
app.MapGet("/myendpoint", (HardwareDetector detector) =>
{
    var profile = await detector.DetectSystemAsync();
    return Results.Ok(profile);
});
```

## CORS Configuration

CORS is configured for local development:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

Update origins as needed for production deployment.

## Error Handling

All endpoints include try-catch blocks and return structured error responses:

```csharp
try {
    // ... operation
} catch (Exception ex) {
    Log.Error(ex, "Error message");
    return Results.Problem("User-friendly error message", statusCode: 500);
}
```

## Testing

### Unit Tests

Unit tests should mock the API dependencies:

```csharp
[Fact]
public async Task GetCapabilities_ShouldReturnProfile()
{
    // Arrange
    var mockDetector = new Mock<HardwareDetector>();
    mockDetector.Setup(d => d.DetectSystemAsync())
        .ReturnsAsync(new SystemProfile { ... });
    
    // Act & Assert
    // ...
}
```

### Integration Tests

Start the API in test mode:

```csharp
var app = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            // Override services for testing
        });
    });

var client = app.CreateClient();
var response = await client.GetAsync("/healthz");
```

## Deployment

### Development
```bash
dotnet run
```

### Production (Windows)
```bash
dotnet publish -c Release -r win-x64 --self-contained -o publish/
```

The API can be:
1. Hosted as a child process by Aura.Host.Win shells
2. Run standalone for testing
3. Deployed as a Windows Service (future)

## Troubleshooting

### Port Already in Use
Change the port in `appsettings.json` or use command-line override:
```bash
dotnet run --urls "http://127.0.0.1:5006"
```

### CORS Errors
Ensure the web UI origin is listed in the CORS policy.

### Windows-Only Features Not Working on Linux
Expected behavior. Windows-specific providers (TTS, hardware detection details) require Windows. Use mocks for Linux development.

## Contributing

See the main repository README for contribution guidelines.

## License

See LICENSE in the root of the repository.
