# Video Generation Pipeline Pre-flight Validation

## Overview

This feature adds comprehensive pre-flight validation to the video generation pipeline, ensuring all required services are available and functional before starting expensive video generation operations.

## Problem Solved

Previously, the video generation pipeline could fail midway through with cryptic error messages because:
- Services were not checked before starting generation
- Users wasted time on partial generation that would inevitably fail
- Error messages were unclear about which service was missing or misconfigured

## Solution

### Backend Validation (`VideoOrchestrator.ValidatePipelineAsync`)

A new `ValidatePipelineAsync` method validates all required services:

1. **LLM Provider** - Checks if script generation service is available
   - Special handling for Ollama: Verifies the service is actually running
   
2. **TTS Provider** - Checks if text-to-speech service is available
   - Verifies at least one voice is available for narration
   
3. **FFmpeg** - Checks if video rendering tool is installed
   - Uses FFmpegResolver to search standard locations
   - Reports specific path attempts if not found
   
4. **Image Provider** - Checks if visual generation is available (optional)
   - Logs warning if missing but doesn't fail validation
   - System can use fallback placeholders
   
5. **Output Directory** - Checks if output location is writable
   - Tests write permissions before generation

### API Endpoints

#### GET /api/video/validate

Returns validation status without starting generation.

**Response (Success)**:
```json
{
  "isValid": true,
  "errors": [],
  "timestamp": "2025-11-21T02:39:37.532Z",
  "correlationId": "xyz789"
}
```

**Response (Failure)**:
```json
{
  "isValid": false,
  "errors": [
    "FFmpeg not found. Checked 5 locations. Please install FFmpeg or run setup.",
    "TTS Provider has no available voices. Check TTS configuration.",
    "Ollama LLM configured but not responding. Please start Ollama."
  ],
  "timestamp": "2025-11-21T02:39:37.532Z",
  "correlationId": "xyz789"
}
```

#### POST /api/video/generate (Updated)

Now includes automatic pre-flight validation before creating generation job.

**Error Response (400 Bad Request)**:
```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
  "title": "Pipeline Validation Failed",
  "status": 400,
  "detail": "The video generation pipeline is not ready. Please fix the issues below before generating videos.",
  "correlationId": "xyz789",
  "errors": [
    "FFmpeg not found. Checked 5 locations. Please install FFmpeg or run setup."
  ],
  "message": "Please fix the issues above before generating videos"
}
```

## Frontend Integration (Optional)

The validation endpoint can be called from the frontend before showing the "Generate" button:

```typescript
// Example frontend code (not implemented in this PR)
import { apiClient } from '@/services/api/apiClient';

interface PipelineValidation {
  isValid: boolean;
  errors: string[];
  timestamp: string;
  correlationId: string;
}

export async function validatePipeline(): Promise<PipelineValidation> {
  const response = await apiClient.get('/api/video/validate');
  return response.data;
}

// In your React component before generation:
const handleGenerate = async () => {
  // Check pipeline first
  const validation = await validatePipeline();
  
  if (!validation.isValid) {
    toast.error('Cannot generate video', {
      description: validation.errors.join('\n')
    });
    return;
  }

  // Proceed with generation
  await generateVideo(brief);
};
```

## Benefits

1. **Fail Fast** - Detects problems before starting expensive generation
2. **Clear Error Messages** - Each error message is actionable and specific
3. **Better User Experience** - Users know what to fix before waiting
4. **Resource Efficiency** - Avoids wasting CPU/GPU on doomed generation
5. **Debugging Support** - Correlation IDs enable better tracing

## Testing

### Unit Tests

`VideoOrchestratorValidationTests.cs` includes tests for:
- ✅ Successful validation with all services available
- ✅ Validation failure when TTS has no voices
- ✅ Validation passes without image provider (optional)
- ✅ Validation failure when FFmpeg is not found

### Manual Testing

1. **Test without FFmpeg**:
   ```bash
   curl http://localhost:5005/api/video/validate
   # Should return isValid: false with FFmpeg error
   ```

2. **Test with all services**:
   - Install FFmpeg
   - Start Ollama (if using Ollama LLM)
   - Configure TTS provider
   ```bash
   curl http://localhost:5005/api/video/validate
   # Should return isValid: true
   ```

3. **Test generation endpoint**:
   ```bash
   curl -X POST http://localhost:5005/api/video/generate \
     -H "Content-Type: application/json" \
     -d '{
       "brief": "Test video",
       "durationMinutes": 1
     }'
   # Should return 400 if validation fails, 202 if successful
   ```

## Implementation Notes

### Ollama Health Check

The Ollama health check uses reflection to avoid circular dependency:
- `Aura.Core` doesn't reference `Aura.Providers`
- Uses `GetType().Name == "OllamaLlmProvider"` to detect Ollama
- Calls `IsServiceAvailableAsync` via reflection
- Falls back gracefully if method not found

### FFmpeg Resolver Integration

The `FFmpegResolver` is injected optionally into `VideoOrchestrator`:
- Maintains backward compatibility with existing code
- Uses resolver to check FFmpeg availability if present
- Logs warning if resolver not available (but doesn't fail)

### Error Handling

All validation checks are wrapped in try-catch:
- Individual check failures don't crash entire validation
- Each failure adds to error list
- Returns all errors at once for better UX

## Future Enhancements

1. **Frontend Integration**: Add validation UI in wizard flow
2. **Provider-Specific Checks**: More detailed validation per provider type
3. **Estimated Fix Time**: Suggest time needed to fix each issue
4. **Auto-Repair**: Automatic installation of missing components
5. **Caching**: Cache validation results for short period

## Related PRs

- PR #1: FFmpeg resolver implementation
- PR #2: Ollama health check implementation

## Files Changed

- `Aura.Core/Orchestrator/VideoOrchestrator.cs` - Added validation method
- `Aura.Api/Controllers/VideoController.cs` - Added validation endpoint
- `Aura.Api/Models/ApiModels.V1/VideoDtos.cs` - Added response DTO
- `Aura.Tests/VideoOrchestratorValidationTests.cs` - Added unit tests

## Author

GitHub Copilot for Coffee285 (PR #5)
Date: 2025-11-21
