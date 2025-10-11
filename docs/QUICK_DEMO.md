# Quick Demo (Safe) Feature

## Overview

The Quick Demo feature provides a **one-click, guaranteed-success path** for new users to generate their first video without any configuration or setup.

## What it does

- **Forces Free-only providers**: RuleBased LLM, Windows TTS (or Piper if installed), and Stock visuals
- **Locks render settings**: 1080p30 H.264 MP4 for maximum compatibility
- **Generates short demo**: 10-15 second video with captions
- **Opens Review step**: Automatically shows the generation panel when complete

## User Experience

1. New user opens the Wizard (Step 1: Brief)
2. Sees prominent "Quick Demo (Safe)" button with clear messaging:
   - "New to Aura?"
   - "Try a Quick Demo - No setup required!"
   - "Generates a 10-15 second demo video with safe defaults"
3. Clicks button (optional: can provide custom topic)
4. Generation starts immediately with safe defaults
5. Generation panel opens showing progress
6. User can view their first video in seconds

## Technical Details

### Backend

- **Endpoint**: `POST /api/quick/demo`
- **Controller**: `Aura.Api/Controllers/QuickController.cs`
- **Service**: `Aura.Core/Orchestrator/QuickService.cs`
- **Request**: `{ "topic": "optional custom topic" }`
- **Response**: `{ "jobId": "...", "status": "queued", "message": "..." }`

### Frontend

- **Component**: `Aura.Web/src/pages/Wizard/CreateWizard.tsx`
- **Location**: Step 1 (Brief) - below the form fields, above navigation buttons
- **Handler**: `handleQuickDemo()` - calls `/api/quick/demo` endpoint

### Safe Defaults

```csharp
// Brief
Topic: "Welcome to Aura Video Studio" (or user-provided)
Audience: "General"
Goal: "Demonstrate"
Tone: "Informative"
Language: "en-US"
Aspect: Widescreen16x9

// Plan
Duration: 12 seconds
Pacing: Fast
Density: Sparse
Style: "Demo"

// Voice
VoiceName: "en-US-Standard-A"
Rate: 1.0
Pitch: 0.0
Pause: Short

// Render
Resolution: 1920x1080
FPS: 30
Codec: H264
Container: mp4
VideoBitrate: 5000 kbps
AudioBitrate: 192 kbps
QualityLevel: 75
EnableSceneCut: true
```

## Testing

### Playwright Tests

Two E2E tests added to `Aura.Web/tests/e2e/wizard.spec.ts`:

1. **should start quick demo with one click**: Verifies button is visible and clickable
2. **quick demo should work without filling topic**: Ensures no fields are required

### Manual Testing

1. Start API: `dotnet run --project Aura.Api`
2. Start Web: `cd Aura.Web && npm run dev`
3. Navigate to: `http://localhost:5173/create`
4. Click "Quick Demo (Safe)" button
5. Verify generation panel opens and video generation starts

## Benefits

- **Zero configuration**: Works immediately for new users
- **No API keys required**: Uses only free, built-in providers
- **Maximum compatibility**: H.264 MP4 plays everywhere
- **Quick feedback**: 10-15 second videos render fast
- **Learning tool**: Shows users what Aura can do before deep configuration

## Future Enhancements

- Add option to customize topic from the button UI
- Show estimated time to completion
- Add "Try another demo" button after completion
- Provide template variations (different styles/tones)
