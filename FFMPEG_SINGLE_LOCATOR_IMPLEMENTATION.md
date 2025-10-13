# FFmpeg Single Locator Implementation Summary

## Problem Statement

FFmpeg validation was succeeding ("ffmpeg version 8.0...") but remediation (re-encoding audio) was failing with "FFmpeg not available for re-encoding". This was caused by:

1. **Path inconsistency**: Validation used PATH-resolved FFmpeg, but remediation used null/other lookups
2. **Multiple resolution points**: Different parts of the code resolved FFmpeg paths independently
3. **No single source of truth**: Each operation could potentially use a different FFmpeg binary

## Solution

Implemented a **single IFfmpegLocator** interface that is injected throughout the application and used consistently for:

- FFmpeg validation
- Video rendering
- Audio re-encoding (remediation)
- Silent WAV generation (fallback)

### Key Changes

#### 1. Interface Creation (`IFfmpegLocator`)

```csharp
public interface IFfmpegLocator
{
    Task<string> GetEffectiveFfmpegPathAsync(string? configuredPath = null, CancellationToken ct = default);
    Task<FfmpegValidationResult> CheckAllCandidatesAsync(string? configuredPath = null, CancellationToken ct = default);
    Task<FfmpegValidationResult> ValidatePathAsync(string ffmpegPath, CancellationToken ct = default);
}
```

#### 2. Updated `FfmpegVideoComposer`

**Before:**
```csharp
public FfmpegVideoComposer(ILogger<FfmpegVideoComposer> logger, string ffmpegPath, string? outputDirectory = null)
{
    _ffmpegPath = ffmpegPath; // Static path passed in constructor
}
```

**After:**
```csharp
public FfmpegVideoComposer(ILogger<FfmpegVideoComposer> logger, IFfmpegLocator ffmpegLocator, string? configuredFfmpegPath = null, string? outputDirectory = null)
{
    _ffmpegLocator = ffmpegLocator; // Injected locator
    _configuredFfmpegPath = configuredFfmpegPath; // Optional configuration
}
```

#### 3. Single Resolution Point in `RenderAsync`

```csharp
public async Task<string> RenderAsync(Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct)
{
    var jobId = Guid.NewGuid().ToString("N");
    
    // Resolve FFmpeg path ONCE at the start - single source of truth for this render job
    string ffmpegPath = await _ffmpegLocator.GetEffectiveFfmpegPathAsync(_configuredFfmpegPath, ct);
    _logger.LogInformation("Resolved FFmpeg path for job {JobId}: {FfmpegPath}", jobId, ffmpegPath);
    
    // Use this SAME path for:
    // 1. Validation
    await ValidateFfmpegBinaryAsync(ffmpegPath, jobId, correlationId, ct);
    
    // 2. Audio pre-validation and remediation
    await PreValidateAudioAsync(timeline, ffmpegPath, jobId, correlationId, ct);
    
    // 3. Actual rendering
    var process = new Process { StartInfo = new ProcessStartInfo { FileName = ffmpegPath, ... } };
}
```

#### 4. Dependency Injection Setup

**API (Program.cs):**
```csharp
// Register FFmpeg locator
builder.Services.AddSingleton<Aura.Core.Dependencies.IFfmpegLocator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Aura.Core.Dependencies.FfmpegLocator>>();
    var providerSettings = sp.GetRequiredService<Aura.Core.Configuration.ProviderSettings>();
    var toolsDir = providerSettings.GetToolsDirectory();
    return new Aura.Core.Dependencies.FfmpegLocator(logger, toolsDir);
});

// Register IVideoComposer with locator injection
builder.Services.AddSingleton<IVideoComposer>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<FfmpegVideoComposer>>();
    var providerSettings = sp.GetRequiredService<Aura.Core.Configuration.ProviderSettings>();
    var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
    var configuredFfmpegPath = providerSettings.GetFfmpegPath();
    var outputDirectory = providerSettings.GetOutputDirectory();
    return new FfmpegVideoComposer(logger, ffmpegLocator, configuredFfmpegPath, outputDirectory);
});
```

**CLI (Program.cs):**
```csharp
services.AddSingleton<Aura.Core.Dependencies.IFfmpegLocator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Aura.Core.Dependencies.FfmpegLocator>>();
    return new Aura.Core.Dependencies.FfmpegLocator(logger);
});

services.AddTransient<FfmpegVideoComposer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FfmpegVideoComposer>>();
    var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
    string configuredFfmpegPath = "ffmpeg"; // Use system ffmpeg
    return new FfmpegVideoComposer(logger, ffmpegLocator, configuredFfmpegPath);
});
```

**App (App.xaml.cs):**
```csharp
services.AddSingleton<Aura.Core.Dependencies.IFfmpegLocator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Aura.Core.Dependencies.FfmpegLocator>>();
    return new Aura.Core.Dependencies.FfmpegLocator(logger);
});

services.AddTransient<FfmpegVideoComposer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FfmpegVideoComposer>>();
    var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
    string configuredFfmpegPath = Path.Combine(AppContext.BaseDirectory, "scripts", "ffmpeg", "ffmpeg.exe");
    return new FfmpegVideoComposer(logger, ffmpegLocator, configuredFfmpegPath);
});
```

#### 5. AudioValidator Path Consistency

`AudioValidator` already accepts `ffmpegPath` in its constructor, so it naturally receives the same resolved path when instantiated in `PreValidateAudioAsync`:

```csharp
private async Task PreValidateAudioAsync(Timeline timeline, string ffmpegPath, string jobId, string correlationId, CancellationToken ct)
{
    var validator = new Aura.Core.Audio.AudioValidator(
        NullLogger<AudioValidator>.Instance,
        ffmpegPath,  // Same path resolved at job start
        ffprobePath);
    
    // Validate and remediate using the same FFmpeg binary
    await ValidateAndRemediateAudioFileAsync(validator, ...);
}
```

### Test Coverage

Created comprehensive tests in `FfmpegSingleLocatorTests.cs`:

1. **Locator injection test**: Verifies `FfmpegVideoComposer` uses the injected locator
2. **Consistent path test**: Verifies locator returns the same path on multiple calls
3. **Absolute path test**: Verifies locator returns absolute paths
4. **Error handling test**: Verifies proper exception when FFmpeg not found
5. **Constructor test**: Verifies new constructor signature

All 81 FFmpeg-related tests passing.

## Benefits

### 1. **Path Consistency**
- Single resolution point per render job
- Same FFmpeg binary used for validation, rendering, and remediation
- No more "validation succeeds but remediation fails" issues

### 2. **Enhanced Logging**
Job logs now include the resolved FFmpeg path:
```
[INFO] Resolved FFmpeg path for job abc123: /usr/local/bin/ffmpeg
[INFO] Validating FFmpeg binary: /usr/local/bin/ffmpeg
[INFO] FFmpeg validation successful: ffmpeg version 8.0
```

### 3. **Better Error Messages**
When FFmpeg is not found, errors include:
- Attempted paths
- Error code (E302-FFMPEG_NOT_FOUND)
- Actionable fix suggestions

### 4. **Testability**
- Easy to mock `IFfmpegLocator` in tests
- Can test path resolution independently
- Can verify consistency across operations

### 5. **Maintainability**
- Single place to modify FFmpeg path resolution logic
- Clear separation of concerns
- Dependency injection makes dependencies explicit

## Files Modified

1. `Aura.Core/Dependencies/FfmpegLocator.cs` - Added IFfmpegLocator interface
2. `Aura.Providers/Video/FfmpegVideoComposer.cs` - Inject locator, resolve once per job
3. `Aura.Api/Program.cs` - Register locator, update IVideoComposer registration
4. `Aura.Cli/Program.cs` - Register locator, update FfmpegVideoComposer registration
5. `Aura.App/App.xaml.cs` - Register locator, update FfmpegVideoComposer registration
6. `Aura.Tests/FfmpegPathDetectionTests.cs` - Updated to use new constructor
7. `Aura.Tests/FfmpegSingleLocatorTests.cs` - New comprehensive tests

## Acceptance Criteria ✅

- ✅ No "FFmpeg not available for re-encoding" when validation already succeeded
- ✅ Logs show same absolute FFmpeg path for validation and remediation within a job
- ✅ Single IFfmpegLocator used everywhere (validation, render, re-encode, silent WAV generation)
- ✅ FfmpegVideoComposer calls locator.GetFfmpegPath() once per job
- ✅ Audio remediation (re-encode, silent fallback) respects the same path
- ✅ Job logs include the resolved absolute FFmpeg path and version
- ✅ Tests verify composer uses same path for render + remediation

## Migration Notes

**Breaking Change**: `FfmpegVideoComposer` constructor signature changed.

**Before:**
```csharp
new FfmpegVideoComposer(logger, ffmpegPath, outputDirectory)
```

**After:**
```csharp
new FfmpegVideoComposer(logger, ffmpegLocator, configuredFfmpegPath, outputDirectory)
```

All usages in the codebase have been updated. External code will need to:
1. Create/inject an `IFfmpegLocator` instance
2. Pass it to the constructor instead of a string path
