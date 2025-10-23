# API Contract V1 Documentation

## Overview

The Aura Video Studio API V1 contract is the authoritative interface between the backend and frontend. It is defined in C# under `Aura.Api/Models/ApiModels.V1/` and exposed via OpenAPI/Swagger.

## Design Principles

1. **Single Source of Truth**: All API types are defined in `ApiModels.V1` namespace
2. **Backward Compatibility**: Tolerant enum converters accept legacy aliases
3. **Type Safety**: TypeScript types are generated from OpenAPI spec
4. **Versioned**: V1 prefix allows future API evolution
5. **Documented**: XML comments and OpenAPI descriptions

## Contract Structure

```
Aura.Api/Models/ApiModels.V1/
├── Enums.cs          # Enum definitions (Pacing, Density, Aspect, PauseStyle, etc.)
├── Dtos.cs           # Request/Response DTOs
└── EnumMappings      # Conversion helpers to Core.Models types
```

## Enum Tolerant Mappings

The API accepts both canonical enum names and legacy aliases for backward compatibility.

### Aspect Ratio

**Canonical Values:**
- `Widescreen16x9` - Standard widescreen (YouTube, most platforms)
- `Vertical9x16` - Mobile/portrait (TikTok, Instagram Stories)
- `Square1x1` - Square format (Instagram feed)

**Legacy Aliases Accepted:**
- `"16:9"` → `Widescreen16x9`
- `"9:16"` → `Vertical9x16`
- `"1:1"` → `Square1x1`

**Example:**
```json
{
  "aspect": "16:9"
}
```
or
```json
{
  "aspect": "Widescreen16x9"
}
```

Both are valid and map to the same internal value.

### Density

**Canonical Values:**
- `Sparse` - Minimal information per scene
- `Balanced` - Moderate information density
- `Dense` - Maximum information per scene

**Legacy Aliases Accepted:**
- `"Normal"` → `Balanced`

**Example:**
```json
{
  "density": "Normal"
}
```
Maps to `Balanced` internally.

### Pacing

**Canonical Values:**
- `Chill` - Slow, relaxed narration
- `Conversational` - Natural speaking pace
- `Fast` - Quick, energetic narration

**Case Insensitive:**
All pacing values are case-insensitive: `"chill"`, `"CHILL"`, `"Chill"` all work.

### PauseStyle

**Canonical Values:**
- `Natural` - Standard pauses between sentences
- `Short` - Minimal pauses
- `Long` - Extended pauses for emphasis
- `Dramatic` - Variable pauses for dramatic effect

**Case Insensitive:**
All pause styles are case-insensitive.

## Key DTOs

### ScriptRequest

Request to generate a video script.

```typescript
interface ScriptRequest {
  topic: string;                    // Video topic
  audience: string;                 // Target audience
  goal: string;                     // Video goal (inform, entertain, etc.)
  tone: string;                     // Tone (informative, casual, etc.)
  language: string;                 // Language code (e.g., "en-US")
  aspect: Aspect;                   // Video aspect ratio
  targetDurationMinutes: number;    // Desired video length
  pacing: Pacing;                   // Narration pacing
  density: Density;                 // Information density
  style: string;                    // Video style
  providerTier?: string | null;     // Optional: "Free" or "Pro"
}
```

### TtsRequest

Request to synthesize text-to-speech audio.

```typescript
interface TtsRequest {
  lines: LineDto[];                 // Script lines to synthesize
  voiceName: string;                // TTS voice name
  rate: number;                     // Speech rate (0.5 - 2.0)
  pitch: number;                    // Voice pitch (-10 to +10)
  pauseStyle: PauseStyle;           // Pause style between sentences
}

interface LineDto {
  sceneIndex: number;
  text: string;
  startSeconds: number;
  durationSeconds: number;
}
```

### RenderRequest

Request to render a video.

```typescript
interface RenderRequest {
  timelineJson: string;             // Timeline JSON
  presetName: string;               // Render preset (e.g., "1080p-high")
  settings?: RenderSettingsDto;     // Optional: custom settings
}

interface RenderSettingsDto {
  width: number;
  height: number;
  fps: number;
  codec: string;
  container: string;
  qualityLevel: number;
  videoBitrateK: number;
  audioBitrateK: number;
  enableSceneCut: boolean;
}
```

## Error Handling

### Error Codes

- **E300**: General script generation error
- **E301**: Request cancelled/timeout
- **E303**: Invalid enum value or validation error
- **E304**: Invalid plan specification
- **E305**: Recommendation service error

### Error Response Format

All errors follow ProblemDetails (RFC 7807) format:

```json
{
  "type": "https://docs.aura.studio/errors/E303",
  "title": "Invalid Enum Value",
  "status": 400,
  "detail": "Unknown Aspect value: '4:3'. Valid values are: Widescreen16x9 (or 16:9), Vertical9x16 (or 9:16), Square1x1 (or 1:1)",
  "traceId": "00-abc123..."
}
```

**Invalid Enum Example:**

Request:
```json
{
  "aspect": "4:3",
  ...
}
```

Response (400 Bad Request):
```json
{
  "type": "https://docs.aura.studio/errors/E303",
  "title": "Invalid Enum Value",
  "status": 400,
  "detail": "Unknown Aspect value: '4:3'. Valid values are: Widescreen16x9 (or 16:9), Vertical9x16 (or 9:16), Square1x1 (or 1:1)"
}
```

## TypeScript Type Generation

TypeScript types are **automatically generated** from the OpenAPI specification to ensure perfect sync.

### Generating Types

```bash
# From repository root
node scripts/contract/generate-api-v1-types.js

# Or on Windows
.\scripts\contract\generate-api-v1-types.ps1
```

This generates `Aura.Web/src/types/api-v1.ts` with:
- All request/response interfaces
- Enum definitions
- Auto-generated header with timestamp

### Using Generated Types

```typescript
import { ScriptRequest, Pacing, Density, Aspect } from '@/types/api-v1';

const request: ScriptRequest = {
  topic: "AI in Healthcare",
  audience: "General",
  goal: "Inform",
  tone: "Informative",
  language: "en-US",
  aspect: Aspect.Widescreen16x9,  // or "16:9" as string
  targetDurationMinutes: 3.0,
  pacing: Pacing.Conversational,
  density: Density.Balanced,      // or "Normal" as string
  style: "Standard"
};

const response = await fetch('/api/script', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(request)
});
```

## Contract Tests

### Round-Trip Tests

Located in `Aura.Tests/Models/EnumRoundTripTests.cs`, these tests verify:
- Every enum value serializes to correct string
- Every enum value deserializes correctly
- Aliases are accepted and map to canonical values

### Binding Tests

Located in `Aura.Tests/ScriptApiTests.cs`, these tests verify:
- Valid payloads deserialize correctly
- Alias enums work (e.g., "16:9", "Normal")
- Case-insensitive enums work
- Invalid enums produce helpful error messages

### Running Tests

```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~EnumRoundTrip"
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~ScriptApiTests"
```

## Versioning Strategy

### Current: V1

API V1 is the current and only version. All types are under `Aura.Api.Models.ApiModels.V1`.

### Future: V2+

When breaking changes are needed:

1. Create `ApiModels.V2` namespace
2. Add new controllers/endpoints under `/api/v2/`
3. Keep V1 endpoints for backward compatibility
4. Generate separate TypeScript types: `api-v2.ts`

### Non-Breaking Changes

These can be added to V1 without versioning:

- **Adding optional fields** to existing DTOs
- **Adding new enum values** (with backward-compatible defaults)
- **Adding new endpoints**
- **Relaxing validation rules**

### Breaking Changes (require new version)

- **Removing fields** from DTOs
- **Changing field types**
- **Removing enum values**
- **Changing required fields**
- **Renaming anything**

## Best Practices

### For Backend Developers

1. ✅ Always define DTOs in `ApiModels.V1`
2. ✅ Add XML documentation comments
3. ✅ Use tolerant enum converters for enums with aliases
4. ✅ Return `ProblemDetails` for errors with error codes
5. ✅ Test enum round-trips and binding
6. ❌ Never change existing enum values
7. ❌ Never make optional fields required

### For Frontend Developers

1. ✅ Regenerate types after backend changes
2. ✅ Use generated types, not handwritten
3. ✅ Handle enum aliases if needed (though backend accepts both)
4. ✅ Check ProblemDetails error responses
5. ❌ Never modify `api-v1.ts` manually
6. ❌ Never commit outdated generated types

## Maintenance Checklist

- [ ] All public DTOs in `ApiModels.V1/Dtos.cs`
- [ ] All enums in `ApiModels.V1/Enums.cs`
- [ ] Tolerant converters registered in `EnumJsonConverters`
- [ ] XML documentation on all public types
- [ ] Round-trip tests for all enums
- [ ] Binding tests for key endpoints
- [ ] TypeScript types regenerated
- [ ] No duplicate DTOs elsewhere (e.g., Program.cs)
- [ ] No TODO/FIXME/PLACEHOLDER comments

## References

- OpenAPI Spec: `http://localhost:5000/swagger` (when running)
- Swagger UI: `http://localhost:5000/swagger/index.html`
- Type Generation: `scripts/contract/README.md`
- RFC 7807 (Problem Details): https://tools.ietf.org/html/rfc7807
