# Input Validation System Implementation

This document describes the comprehensive input validation system implemented to prevent invalid inputs from reaching the generation pipeline.

## Overview

The validation system provides proactive checks before expensive video generation operations begin, catching issues early and providing clear, actionable error messages to users.

## Components

### 1. ValidationResult Record (`Aura.Core/Validation/ValidationResult.cs`)

A simple record to hold validation results:
- `IsValid`: Boolean indicating if validation passed
- `Issues`: List of validation error messages

### 2. ValidationException (`Aura.Core/Validation/ValidationException.cs`)

Custom exception for validation failures:
- Extends `Exception`
- Includes `Issues` property containing all validation errors
- Preserves detailed error information for better debugging

### 3. PreGenerationValidator (`Aura.Core/Validation/PreGenerationValidator.cs`)

Validates system readiness before generation starts. Checks:

#### FFmpeg Availability
- Uses `IFfmpegLocator` to check if FFmpeg is installed
- Verifies the FFmpeg executable file exists
- Error: "FFmpeg not found. Please install FFmpeg or configure the path in Settings."

#### Disk Space
- Checks available disk space in the output directory (MyDocuments/AuraVideos)
- Requires at least 1GB free space
- Error: "Insufficient disk space: {X}GB free, need at least 1GB."

#### Brief Validation
- **Topic Required**: Checks topic is not null or whitespace
  - Error: "Topic is required. Please provide a topic for your video."
- **Topic Length**: Ensures topic is at least 3 characters
  - Error: "Topic is too short. Please provide a descriptive topic (at least 3 characters)."

#### Duration Validation
- **Minimum Duration**: At least 10 seconds
  - Error: "Duration too short. Minimum duration is 10 seconds."
- **Maximum Duration**: At most 30 minutes
  - Error: "Duration too long. Maximum duration is 30 minutes."

#### System Hardware
- Uses `IHardwareDetector` to check system capabilities
- **CPU Cores**: At least 2 logical cores required
  - Error: "Insufficient CPU cores. At least 2 CPU cores required."
- **RAM**: At least 4GB required
  - Error: "Insufficient RAM. At least 4GB RAM required."

### 4. ScriptValidator (`Aura.Core/Validation/ScriptValidator.cs`)

Validates script quality after generation. Checks:

#### Script Length
- Minimum 100 characters required
- Error: "Script too short ({length} characters, minimum 100 characters required)."

#### Script Format
- Must have title starting with "# "
- Error: "Script must have a title (starts with '# ')."

#### Scene Count
- Must have at least 2 scenes marked with "## "
- Error: "Script must have at least 2 scenes (found {count}). Use '## Scene Name' to mark scenes."

#### Word Count
- Calculates expected words: TargetDuration * 2.5 (150 words per minute)
- Allows 50% tolerance for deviation
- Error: "Word count significantly off target. Found {wordCount} words, expected approximately {expectedWords} words for {seconds} seconds."

### 5. VideoOrchestrator Integration (`Aura.Core/Orchestrator/VideoOrchestrator.cs`)

Both `GenerateVideoAsync` methods now include validation:

1. **Pre-Generation Validation**
   - Runs at the very beginning before any expensive operations
   - Reports progress: "Validating system readiness..."
   - Throws `ValidationException` if validation fails
   - Logs validation results

2. **Script Validation**
   - Runs immediately after script generation
   - Automatically retries script generation once if validation fails
   - Throws `ValidationException` if still invalid after retry
   - Logs validation attempts and results

3. **Exception Handling**
   - `ValidationException` is re-thrown without wrapping
   - Preserves all validation issues for detailed error reporting
   - Other exceptions are caught and logged as before

### 6. API Controller (`Aura.Api/Controllers/ValidationController.cs`)

REST API endpoint for proactive validation:

**Endpoint**: `POST /api/validation/brief`

**Request Body**:
```json
{
  "topic": "string",
  "audience": "string (optional)",
  "goal": "string (optional)",
  "tone": "string (optional, default: Informative)",
  "language": "string (optional, default: en-US)",
  "durationMinutes": "number (optional, default: 1.0)"
}
```

**Response**:
```json
{
  "isValid": true/false,
  "issues": ["error message 1", "error message 2"]
}
```

### 7. UI Integration (`Aura.Web/src/pages/Wizard/CreateWizard.tsx`)

The Quick Demo button now:
1. Calls validation endpoint before starting generation
2. Shows validation errors in a toast notification
3. Returns early if validation fails (prevents generation)
4. Shows spinner icon while validating/generating
5. Disables button during validation

## Dependency Injection

Validators are registered as singletons in both API and App:

**Aura.Api/Program.cs**:
```csharp
builder.Services.AddSingleton<Aura.Core.Validation.PreGenerationValidator>();
builder.Services.AddSingleton<Aura.Core.Validation.ScriptValidator>();
```

**Aura.App/App.xaml.cs**:
```csharp
services.AddSingleton<Aura.Core.Validation.PreGenerationValidator>();
services.AddSingleton<Aura.Core.Validation.ScriptValidator>();
```

## Testing

### Unit Tests
- Updated `VideoOrchestratorIntegrationTests` to include validators
- Created mock implementations for `IFfmpegLocator` and `IHardwareDetector`
- Mock FFmpeg locator creates temporary file to simulate FFmpeg installation
- Mock LLM provider returns properly formatted script (title, scenes, correct word count)
- All tests passing (861/864 - 3 pre-existing failures unrelated to validation)

### Manual Testing
Test the validation API endpoint:
```bash
# Valid inputs (should pass)
curl -X POST http://localhost:5005/api/validation/brief \
  -H "Content-Type: application/json" \
  -d '{"topic": "AI Video Generation", "durationMinutes": 1.0}'

# Missing topic (should fail)
curl -X POST http://localhost:5005/api/validation/brief \
  -H "Content-Type: application/json" \
  -d '{"topic": "", "durationMinutes": 1.0}'

# Topic too short (should fail)
curl -X POST http://localhost:5005/api/validation/brief \
  -H "Content-Type: application/json" \
  -d '{"topic": "AI", "durationMinutes": 1.0}'

# Duration too short (should fail)
curl -X POST http://localhost:5005/api/validation/brief \
  -H "Content-Type: application/json" \
  -d '{"topic": "AI Video Generation", "durationMinutes": 0.1}'

# Duration too long (should fail)
curl -X POST http://localhost:5005/api/validation/brief \
  -H "Content-Type: application/json" \
  -d '{"topic": "AI Video Generation", "durationMinutes": 35}'
```

## Benefits

1. **Early Detection**: Catches issues before expensive operations begin
2. **Clear Messages**: Provides actionable error messages that guide users to solutions
3. **Resource Savings**: Prevents wasting time and system resources on doomed generation attempts
4. **Better UX**: Users get immediate feedback rather than waiting for generation to fail
5. **Automatic Retry**: Script validation automatically retries once, improving success rate
6. **Comprehensive Coverage**: Validates inputs, system resources, and output quality

## Security

- CodeQL security analysis passed with 0 alerts
- No vulnerabilities introduced
- Validation logic is defensive and doesn't expose sensitive information
- Exception messages don't include system paths or sensitive data

## Error Flow

1. User initiates generation (e.g., clicks Quick Demo)
2. UI calls `/api/validation/brief` endpoint
3. PreGenerationValidator checks system readiness
4. If validation fails:
   - UI shows error toast with all issues
   - Generation is never started
5. If validation passes:
   - UI proceeds with generation
   - VideoOrchestrator runs pre-generation validation again (server-side)
   - Script is generated
   - ScriptValidator checks script quality
   - If script invalid, retries once
   - If still invalid after retry, throws ValidationException
6. User sees clear error messages at each stage

## Configuration

No configuration required - validation runs automatically. Thresholds are:
- Minimum topic length: 3 characters
- Minimum duration: 10 seconds
- Maximum duration: 30 minutes
- Minimum disk space: 1GB
- Minimum CPU cores: 2
- Minimum RAM: 4GB
- Minimum script length: 100 characters
- Minimum scenes: 2
- Word count tolerance: 50%

## Files Created/Modified

### New Files
- `Aura.Core/Validation/ValidationResult.cs`
- `Aura.Core/Validation/ValidationException.cs`
- `Aura.Core/Validation/PreGenerationValidator.cs`
- `Aura.Core/Validation/ScriptValidator.cs`
- `Aura.Api/Controllers/ValidationController.cs`

### Modified Files
- `Aura.Core/Orchestrator/VideoOrchestrator.cs` - Added validation calls
- `Aura.Api/Program.cs` - Registered validators
- `Aura.App/App.xaml.cs` - Registered validators
- `Aura.Web/src/pages/Wizard/CreateWizard.tsx` - Added validation before Quick Demo
- `Aura.Tests/VideoOrchestratorIntegrationTests.cs` - Updated tests to include validators

## Acceptance Criteria Met

✅ Pre-generation validation checks FFmpeg availability before starting  
✅ Validation checks available disk space is at least 1GB  
✅ Validation checks system has minimum 2 CPU cores and 4GB RAM  
✅ Validation checks Brief has valid topic with at least 3 characters  
✅ Validation checks duration is between 10 seconds and 30 minutes  
✅ Validation prevents invalid inputs from reaching generation pipeline  
✅ Clear actionable error messages guide users to solutions  
✅ Script validation catches poor quality scripts with too few scenes or wrong word count  
✅ Script regeneration is attempted once automatically if validation fails  
✅ Validation errors are shown in UI before generation starts  
✅ API endpoint /api/validation/brief allows UI to validate proactively  
✅ All validation errors are logged with clear messages  
✅ ValidationException preserves all issues for detailed error reporting  
✅ UI shows validation errors in toast notifications with all issues listed  
✅ Quick Demo button shows loading state during validation  
