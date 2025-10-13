# FFmpeg Single Locator Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Render Job Starts                            │
│                 RenderAsync(timeline, spec)                      │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│   Step 1: Resolve FFmpeg Path (ONCE per job)                   │
│                                                                  │
│   ffmpegPath = await _ffmpegLocator                             │
│       .GetEffectiveFfmpegPathAsync(_configuredFfmpegPath)       │
│                                                                  │
│   LOG: "Resolved FFmpeg path for job abc123: /usr/bin/ffmpeg"  │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│   Step 2: Validate Binary                                       │
│                                                                  │
│   await ValidateFfmpegBinaryAsync(ffmpegPath, ...)              │
│                                                                  │
│   Uses: ffmpegPath (same as resolved above)                     │
│   LOG: "Validating FFmpeg binary: /usr/bin/ffmpeg"             │
│   LOG: "FFmpeg validation successful: ffmpeg version 8.0"       │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│   Step 3: Pre-validate Audio & Remediation                      │
│                                                                  │
│   await PreValidateAudioAsync(timeline, ffmpegPath, ...)        │
│                                                                  │
│   Creates: new AudioValidator(logger, ffmpegPath, ffprobePath)  │
│   Uses: ffmpegPath (same as resolved above)                     │
│                                                                  │
│   ┌──────────────────────────────────────────────────┐          │
│   │ AudioValidator Operations                        │          │
│   │  - ValidateAsync()    → uses _ffmpegPath         │          │
│   │  - ReencodeAsync()    → uses _ffmpegPath         │          │
│   │  - GenerateSilentWav() → uses _ffmpegPath        │          │
│   └──────────────────────────────────────────────────┘          │
│                                                                  │
│   LOG: "Successfully re-encoded narration audio"                │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│   Step 4: Build FFmpeg Command & Render                         │
│                                                                  │
│   process = new Process {                                        │
│       StartInfo = new ProcessStartInfo {                         │
│           FileName = ffmpegPath,  ← Same path!                  │
│           Arguments = ffmpegCommand                              │
│       }                                                          │
│   }                                                              │
│                                                                  │
│   LOG: "FFmpeg command (JobId=abc123): /usr/bin/ffmpeg ..."    │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Render Complete                               │
│              Same FFmpeg used throughout!                        │
└─────────────────────────────────────────────────────────────────┘
```

## Before vs After

### Before (Problem)
```
Validation:    PATH lookup → finds /usr/bin/ffmpeg → ✅ SUCCESS
               LOG: "ffmpeg version 8.0"

Remediation:   null/_ffmpegPath → not set → ❌ FAIL
               ERROR: "FFmpeg not available for re-encoding"
```

### After (Solution)
```
Resolution:    IFfmpegLocator → resolves once → /usr/bin/ffmpeg
               LOG: "Resolved FFmpeg path: /usr/bin/ffmpeg"

Validation:    Uses /usr/bin/ffmpeg → ✅ SUCCESS
               LOG: "FFmpeg validation successful: ffmpeg version 8.0"

Remediation:   Uses /usr/bin/ffmpeg → ✅ SUCCESS
               LOG: "Successfully re-encoded audio"

Rendering:     Uses /usr/bin/ffmpeg → ✅ SUCCESS
               LOG: "Render completed successfully"
```

## Key Benefits

1. **Single Source of Truth**: FFmpeg path resolved once per job
2. **Consistency**: Same binary used for all operations
3. **Traceability**: All logs show the same path
4. **Error Prevention**: No more validation/remediation mismatches
5. **Testability**: Easy to mock and test the locator
