# Quality Validation API Endpoints

This document describes the quality validation API endpoints available in Aura Video Studio.

## Overview

The Quality Validation API provides 5 endpoints for validating video quality metrics before processing or publication. These endpoints help ensure your videos meet technical requirements and platform specifications.

## Endpoints

### 1. Resolution Validation
**GET** `/api/quality/validate/resolution`

Validates video resolution against minimum requirements.

**Parameters:**
- `width` (required): Video width in pixels
- `height` (required): Video height in pixels
- `min_resolution` (optional): Minimum resolution specification (default: "1280x720")

**Example Request:**
```bash
curl -X GET "http://localhost:5005/api/quality/validate/resolution?width=1920&height=1080&min_resolution=1280x720"
```

**Example Response:**
```json
{
  "success": true,
  "result": {
    "width": 1920,
    "height": 1080,
    "aspectRatio": "16:9",
    "meetsMinimumResolution": true,
    "totalPixels": 2073600,
    "resolutionCategory": "Full HD 1080p",
    "isValid": true,
    "score": 100,
    "issues": [],
    "warnings": [],
    "validatedAt": "2025-10-23T12:00:00Z"
  }
}
```

### 2. Audio Quality Analysis
**POST** `/api/quality/validate/audio`

Analyzes audio file for quality issues including loudness, clarity, and noise.

**Request Body:**
```json
{
  "audioFilePath": "/path/to/audio.wav"
}
```

**Example Response:**
```json
{
  "success": true,
  "result": {
    "loudnessLUFS": -14.0,
    "peakLevel": -1.0,
    "noiseLevel": 10,
    "clarityScore": 85,
    "hasClipping": false,
    "sampleRate": 48000,
    "bitDepth": 16,
    "channels": 2,
    "dynamicRange": 12.0,
    "isValid": true,
    "score": 88,
    "issues": [],
    "warnings": []
  }
}
```

### 3. Frame Rate Validation
**GET** `/api/quality/validate/framerate`

Verifies frame rate consistency.

**Parameters:**
- `expected_fps` (required): Expected frame rate
- `actual_fps` (required): Actual detected frame rate
- `tolerance` (optional): Acceptable variance (default: 0.5)

**Example Request:**
```bash
curl -X GET "http://localhost:5005/api/quality/validate/framerate?expected_fps=30&actual_fps=30&tolerance=0.5"
```

**Example Response:**
```json
{
  "success": true,
  "result": {
    "actualFPS": 30.0,
    "expectedFPS": 30.0,
    "variance": 0.0,
    "isConsistent": true,
    "droppedFrames": 0,
    "totalFrames": 1000,
    "frameRateCategory": "NTSC 30 FPS",
    "isValid": true,
    "score": 100,
    "issues": [],
    "warnings": []
  }
}
```

### 4. Content Consistency Analysis
**POST** `/api/quality/validate/consistency`

Checks for content consistency across video frames.

**Request Body:**
```json
{
  "videoFilePath": "/path/to/video.mp4"
}
```

**Example Response:**
```json
{
  "success": true,
  "result": {
    "consistencyScore": 85,
    "sceneChanges": 5,
    "hasAbruptTransitions": false,
    "colorConsistency": 90,
    "brightnessConsistency": 88,
    "hasFlickering": false,
    "motionSmoothness": 82,
    "detectedArtifacts": [],
    "isValid": true,
    "score": 86,
    "issues": [],
    "warnings": []
  }
}
```

### 5. Platform Requirements Validation
**GET** `/api/quality/validate/platform-requirements`

Validates video against platform-specific requirements.

**Parameters:**
- `platform` (required): Target platform (youtube, tiktok, instagram, twitter)
- `width` (required): Video width in pixels
- `height` (required): Video height in pixels
- `file_size_bytes` (required): File size in bytes
- `duration_seconds` (required): Duration in seconds
- `codec` (optional): Video codec (default: "H.264")

**Example Request:**
```bash
curl -X GET "http://localhost:5005/api/quality/validate/platform-requirements?platform=youtube&width=1920&height=1080&file_size_bytes=52428800&duration_seconds=300&codec=H.264"
```

**Example Response:**
```json
{
  "success": true,
  "result": {
    "platform": "YouTube",
    "meetsResolutionRequirements": true,
    "meetsAspectRatioRequirements": true,
    "meetsDurationRequirements": true,
    "meetsFileSizeRequirements": true,
    "meetsCodecRequirements": true,
    "fileSizeBytes": 52428800,
    "durationSeconds": 300.0,
    "codec": "H.264",
    "recommendedOptimizations": [],
    "isValid": true,
    "score": 100,
    "issues": [],
    "warnings": []
  }
}
```

## Supported Platforms

The platform requirements endpoint supports the following platforms:

- **YouTube**: Max 256GB, up to 12 hours, supports H.264/H.265/VP9/AV1
- **TikTok**: Max 287MB, up to 10 minutes, 9:16 aspect ratio recommended
- **Instagram**: Max 650MB, up to 60 seconds, supports 1:1, 4:5, 9:16
- **Twitter/X**: Max 512MB, up to 140 seconds, supports H.264

## Response Fields

All endpoints return a common set of fields:

- `isValid`: Boolean indicating if validation passed
- `score`: Overall quality score (0-100)
- `issues`: Array of critical validation failures
- `warnings`: Array of non-critical issues
- `validatedAt`: Timestamp of validation

## Error Handling

All endpoints return appropriate HTTP status codes:

- `200 OK`: Validation completed successfully
- `400 Bad Request`: Invalid input parameters
- `404 Not Found`: File not found (for file-based endpoints)
- `500 Internal Server Error`: Server error during validation

## Integration Examples

### JavaScript/TypeScript
```typescript
async function validateResolution(width: number, height: number) {
  const response = await fetch(
    `http://localhost:5005/api/quality/validate/resolution?width=${width}&height=${height}`
  );
  const data = await response.json();
  return data.result;
}
```

### Python
```python
import requests

def validate_platform_requirements(platform, width, height, file_size, duration):
    url = "http://localhost:5005/api/quality/validate/platform-requirements"
    params = {
        "platform": platform,
        "width": width,
        "height": height,
        "file_size_bytes": file_size,
        "duration_seconds": duration
    }
    response = requests.get(url, params=params)
    return response.json()["result"]
```

### C#
```csharp
using System.Net.Http;
using System.Text.Json;

var client = new HttpClient();
var response = await client.GetAsync(
    "http://localhost:5005/api/quality/validate/resolution?width=1920&height=1080"
);
var content = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<dynamic>(content);
```

## Testing

The API includes comprehensive test coverage with 58 unit tests covering all endpoints and edge cases. Run tests with:

```bash
dotnet test --filter "FullyQualifiedName~QualityValidation"
```

## Production Considerations

The current implementation uses simulated analysis for demonstration. For production use, integrate:

1. **FFmpeg.NET** or **MediaToolkit** for actual video/audio analysis
2. **NAudio** or **CSCore** for detailed audio quality metrics
3. **ML.NET** for advanced frame consistency analysis

All service interfaces are designed to support these integrations with minimal code changes.
