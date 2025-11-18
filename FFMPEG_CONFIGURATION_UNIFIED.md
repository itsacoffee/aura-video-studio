# Unified FFmpeg Configuration Guide

## Overview

FFmpeg configuration is now unified across all parts of the Aura Video Studio stack (Electron, Aura.Api, and Aura.Core) through a single service: `IFfmpegConfigurationService`.

## Architecture

### Components

1. **IFfmpegConfigurationService** - Single authoritative access point for FFmpeg configuration
2. **FfmpegConfigurationService** - Implementation that combines multiple configuration sources
3. **FFmpegConfigurationStore** - Persistent storage for FFmpeg configuration
4. **FFmpegOptions** - Configuration from appsettings.json
5. **AURA_FFMPEG_PATH** - Environment hint from Electron

### Configuration Priority Order

The system applies configuration sources in the following priority (highest to lowest):

1. **Persisted Configuration** (highest priority)
   - Stored in `%LOCALAPPDATA%\AuraVideoStudio\ffmpeg-config.json`
   - Set by user through UI or API
   - Survives application restarts

2. **Environment Hint** (`AURA_FFMPEG_PATH`)
   - Set by Electron based on detected FFmpeg installation
   - Applied only if no persisted configuration exists
   - Primary mechanism for Electron to communicate FFmpeg location to backend

3. **Appsettings** (`FFmpegOptions.ExecutablePath`)
   - Configured in `appsettings.json`
   - Applied only if no persisted configuration or environment hint exists
   - Useful for development and deployment scenarios

4. **System PATH** (lowest priority)
   - Fallback when no other configuration is available
   - FfmpegLocator checks standard system locations

## Usage

### Backend (Aura.Api / Aura.Core)

#### Getting FFmpeg Configuration

```csharp
public class MyService
{
    private readonly IFfmpegConfigurationService _ffmpegConfig;
    
    public MyService(IFfmpegConfigurationService ffmpegConfig)
    {
        _ffmpegConfig = ffmpegConfig;
    }
    
    public async Task<string> GetFfmpegPathAsync()
    {
        var config = await _ffmpegConfig.GetEffectiveConfigurationAsync();
        return config.Path ?? "ffmpeg"; // Fallback to PATH
    }
}
```

#### Updating FFmpeg Configuration

```csharp
public async Task ConfigureFfmpegAsync(string path)
{
    var config = new FFmpegConfiguration
    {
        Path = path,
        Mode = FFmpegMode.Custom,
        Source = "User",
        LastValidatedAt = DateTime.UtcNow,
        LastValidationResult = FFmpegValidationResult.Ok
    };
    
    await _ffmpegConfig.UpdateConfigurationAsync(config);
}
```

### Electron (Desktop App)

Electron sets the `AURA_FFMPEG_PATH` environment variable for the backend process:

```javascript
// In backend-service.js
_prepareEnvironment() {
  const ffmpegPath = this._getFFmpegPath();

  return {
    ...process.env,
    // Primary hint for backend FFmpeg configuration pipeline
    AURA_FFMPEG_PATH: ffmpegPath,
    // Backwards-compatible env vars
    FFMPEG_PATH: ffmpegPath,
    FFMPEG_BINARIES_PATH: ffmpegPath,
  };
}
```

### ProviderSettings Integration

`ProviderSettings.GetFfmpegPath()` now delegates to the unified configuration service:

```csharp
public string GetFfmpegPath()
{
    // If configuration service is available, use it
    if (_ffmpegConfigService != null)
    {
        var config = _ffmpegConfigService.GetEffectiveConfigurationAsync()
            .GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(config.Path))
        {
            return config.Path!;
        }
    }
    
    // Fallback to legacy settings.json or system PATH
    return "ffmpeg";
}
```

## Configuration Flow

### Startup Flow

1. Electron detects FFmpeg on user's system (or uses bundled version)
2. Electron sets `AURA_FFMPEG_PATH` environment variable
3. Backend starts with `AURA_FFMPEG_PATH` in environment
4. `FfmpegConfigurationService` loads persisted config from disk
5. If no persisted config, applies `AURA_FFMPEG_PATH` from environment
6. If no environment hint, applies `FFmpegOptions.ExecutablePath` from appsettings
7. `FfmpegLocator` receives effective path and uses it for validation

### User Configuration Flow

1. User opens Settings UI and specifies FFmpeg path
2. Frontend sends path to backend API
3. Backend validates FFmpeg at specified path
4. Backend creates `FFmpegConfiguration` with validation results
5. Backend calls `IFfmpegConfigurationService.UpdateConfigurationAsync()`
6. Configuration persisted to `ffmpeg-config.json`
7. All subsequent operations use the new persisted configuration

## Benefits

1. **Single Source of Truth** - All code uses `IFfmpegConfigurationService` for FFmpeg path
2. **Consistent Behavior** - FFmpeg path resolution is identical across all components
3. **Testability** - Service interface allows easy mocking in unit tests
4. **Persistence** - User configuration survives application restarts
5. **Flexibility** - Supports multiple configuration sources with clear priority
6. **Backward Compatibility** - Maintains support for legacy env vars and settings.json

## Migration Guide

### For Existing Code

If you have code that:

1. **Reads from ProviderSettings.GetFfmpegPath()**: No changes needed, method now delegates to unified service
2. **Uses environment variables directly**: Should migrate to `IFfmpegConfigurationService`
3. **Reads from appsettings**: No changes needed, `FFmpegOptions` still supported
4. **Constructs FfmpegLocator**: Update to inject `IFfmpegLocator` instead of concrete type

### Example Migration

**Before:**
```csharp
var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";
```

**After:**
```csharp
var config = await _ffmpegConfigService.GetEffectiveConfigurationAsync();
var ffmpegPath = config.Path ?? "ffmpeg";
```

## Troubleshooting

### FFmpeg Not Found

1. Check persisted configuration: `%LOCALAPPDATA%\AuraVideoStudio\ffmpeg-config.json`
2. Check `AURA_FFMPEG_PATH` environment variable (set by Electron)
3. Check `FFmpegOptions.ExecutablePath` in appsettings.json
4. Verify FFmpeg is on system PATH

### Configuration Not Persisting

1. Ensure application has write permissions to `%LOCALAPPDATA%\AuraVideoStudio\`
2. Check logs for FFmpegConfigurationStore errors
3. Verify `UpdateConfigurationAsync()` is being called successfully

### Electron Not Setting Path

1. Check Electron logs for FFmpeg detection
2. Verify `_getFFmpegPath()` in backend-service.js is finding FFmpeg
3. Check that `_prepareEnvironment()` is setting `AURA_FFMPEG_PATH`

## Related Files

- `Aura.Core/Configuration/IFfmpegConfigurationService.cs` - Service interface
- `Aura.Core/Configuration/FfmpegConfigurationService.cs` - Service implementation
- `Aura.Core/Configuration/FFmpegConfigurationStore.cs` - Persistent storage
- `Aura.Core/Configuration/FFmpegOptions.cs` - Appsettings model
- `Aura.Core/Configuration/ProviderSettings.cs` - Provider settings integration
- `Aura.Core/Dependencies/FfmpegLocator.cs` - FFmpeg path resolution
- `Aura.Api/Program.cs` - Service registration
- `Aura.Desktop/electron/backend-service.js` - Electron integration
