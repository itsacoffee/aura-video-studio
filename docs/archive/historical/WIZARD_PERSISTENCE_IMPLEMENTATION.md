# Wizard State Persistence & Provider Validation Implementation

## Overview

This implementation continues the future work from PR #328, adding wizard state persistence and enhanced provider validation with field-level error messages.

## Completed Features

### 1. Wizard State Persistence

#### Backend API Endpoints (SetupController.cs)

**POST `/api/setup/wizard/save-progress`**
- Saves current wizard state to database
- Parameters: `userId`, `currentStep`, `state`, `correlationId`
- Returns: success status and correlationId
- Use case: Auto-save during wizard progression

**GET `/api/setup/wizard/status`**
- Retrieves saved wizard progress
- Parameters: `userId` (optional, defaults to "default")
- Returns: completed status, currentStep, state, canResume flag
- Use case: Resume interrupted setup

**POST `/api/setup/wizard/complete`**
- Marks wizard as completed
- Parameters: `userId`, `finalStep`, `version`, `selectedTier`, `finalState`, `correlationId`
- Returns: success status
- Use case: Finalize wizard completion

**POST `/api/setup/wizard/reset`**
- Resets wizard state
- Parameters: `userId`, `preserveData`, `correlationId`
- Returns: success status
- Use case: Testing or re-running wizard

#### Frontend Integration (setupApi.ts)

Added TypeScript interfaces and API methods:
- `WizardProgressRequest` - Save progress request
- `WizardStatusResponse` - Status response with resume capability
- `WizardCompleteRequest` - Completion request
- `WizardResetRequest` - Reset request

All methods skip circuit breaker to prevent false unavailability errors during setup.

#### State Management (onboarding.ts)

**Helper Functions**:
- `saveWizardProgressToBackend()` - Persists serializable state to backend
- `loadWizardProgressFromBackend()` - Loads saved state for resume
- `completeWizardInBackend()` - Marks wizard complete
- `resetWizardInBackend()` - Resets wizard state

**Auto-Save Middleware**:
- `onboardingReducerWithAutoSave()` - Wrapper reducer with auto-save
- `shouldTriggerAutoSave()` - Determines which actions trigger save
- Auto-saves on: `SET_STEP`, `SET_TIER`, `INSTALL_COMPLETE`, `SKIP_INSTALL`, `API_KEY_VALID`, `SET_WORKSPACE_PREFERENCES`

### 2. Enhanced Provider Validation

#### New DTOs (ProviderValidationDtos.cs)

**EnhancedProviderValidationRequest**:
```csharp
public record EnhancedProviderValidationRequest(
    string Provider,
    Dictionary<string, string?> Configuration,
    bool PartialValidation = false,
    string? CorrelationId = null);
```

**FieldValidationError**:
```csharp
public record FieldValidationError(
    string FieldName,
    string ErrorCode,
    string ErrorMessage,
    string? SuggestedFix = null);
```

**EnhancedProviderValidationResponse**:
```csharp
public record EnhancedProviderValidationResponse(
    bool IsValid,
    string Status,
    string Provider,
    List<FieldValidationError>? FieldErrors = null,
    Dictionary<string, bool>? FieldValidationStatus = null,
    string? OverallMessage = null,
    string? CorrelationId = null,
    ValidationDetails? Details = null);
```

#### Backend Endpoints (ProvidersController.cs)

**POST `/api/providers/validate-enhanced`**
- Field-level validation for provider configuration
- Supports: OpenAI, ElevenLabs, PlayHT
- Returns: field-level errors with suggested fixes
- Supports partial validation mode

**POST `/api/providers/save-partial-config`**
- Saves partial provider configuration
- Stores fields individually to secure storage
- Returns: list of saved fields

#### Provider-Specific Validation

**OpenAI**:
- API Key format validation (must start with 'sk-')
- Base URL format validation (if provided)
- Suggests fix: "Obtain an API key from https://platform.openai.com/api-keys"

**ElevenLabs**:
- API Key presence validation
- Suggests fix: "Obtain an API key from https://elevenlabs.io/app/settings"

**PlayHT**:
- API Key presence validation
- User ID presence validation
- Suggests fix: "Find your User ID in the PlayHT API settings"

## Usage Examples

### Wizard State Persistence

```typescript
// Auto-save progress
import { saveWizardProgressToBackend } from '@/state/onboarding';

const state = useOnboardingState();
await saveWizardProgressToBackend(state);

// Load and resume
import { loadWizardProgressFromBackend } from '@/state/onboarding';

const savedState = await loadWizardProgressFromBackend();
if (savedState) {
  dispatch({ type: 'LOAD_FROM_STORAGE', payload: savedState });
}

// Complete wizard
import { completeWizardInBackend } from '@/state/onboarding';

await completeWizardInBackend(state, correlationId);
```

### Provider Validation

```http
POST /api/providers/validate-enhanced
Content-Type: application/json

{
  "provider": "OpenAI",
  "configuration": {
    "ApiKey": "sk-1234567890",
    "BaseUrl": "https://api.openai.com/v1"
  },
  "partialValidation": false,
  "correlationId": "test-123"
}
```

Response:
```json
{
  "isValid": true,
  "status": "Valid",
  "provider": "OpenAI",
  "fieldValidationStatus": {
    "ApiKey": true,
    "BaseUrl": true
  },
  "overallMessage": "All fields validated successfully",
  "correlationId": "test-123"
}
```

With field errors:
```json
{
  "isValid": false,
  "status": "Invalid",
  "provider": "OpenAI",
  "fieldErrors": [
    {
      "fieldName": "ApiKey",
      "errorCode": "INVALID_FORMAT",
      "errorMessage": "OpenAI API keys must start with 'sk-'",
      "suggestedFix": "Check your API key format"
    }
  ],
  "fieldValidationStatus": {
    "ApiKey": false,
    "BaseUrl": true
  },
  "overallMessage": "1 field(s) have validation errors",
  "correlationId": "test-123"
}
```

## State Machine Flow

### Wizard Progression with Auto-Save

```
NotStarted → [Auto-save triggered]
CheckingEnvironment → [Auto-save triggered] 
FFmpegCheck → 
FFmpegInstallInProgress → 
FFmpegInstalled → [Auto-save triggered]
ProviderConfig → [Auto-save triggered on API key validation]
ValidationInProgress → 
Completed → [Complete in backend]
```

### Resume After Interruption

```
1. User starts wizard
2. Completes steps 1-3
3. App crashes/closes
4. User restarts app
5. App checks wizard status via GET /api/setup/wizard/status
6. If canResume=true, load saved state
7. Resume from step 3
```

## Database Schema

Uses existing `UserSetupEntity` table with enhanced fields:
- `user_id` - User identifier (default: "default")
- `completed` - Wizard completion flag
- `completed_at` - Completion timestamp
- `version` - Wizard version completed
- `last_step` - Last completed step (for resume)
- `updated_at` - Last update timestamp
- `selected_tier` - Selected tier (free/pro)
- `wizard_state` - JSON blob for state storage

## Security Considerations

### State Storage
- No sensitive data in wizard state JSON
- API keys stored separately in secure storage
- Correlation IDs for debugging only (non-sensitive)

### Validation
- Field-level validation prevents invalid configurations
- Suggested fixes guide users without exposing internals
- Partial validation allows incremental configuration

### Auto-Save
- Only saves serializable state (no functions/callbacks)
- Lightweight operation (doesn't block UI)
- Handles errors gracefully (logs but doesn't fail)

## Performance Implications

### Auto-Save
- Triggered only on significant actions (not every keystroke)
- Debounced to prevent excessive API calls
- Async operation doesn't block UI
- Failed saves logged but don't interrupt workflow

### Provider Validation
- Field-level validation reduces unnecessary API calls
- Format validation happens before network requests
- Partial validation allows testing individual fields

## Future Enhancements

### State Persistence
- [ ] Background sync for offline changes
- [ ] State versioning and migration
- [ ] Multi-device state sync
- [ ] Encrypted state storage

### Provider Validation
- [ ] Live validation as user types
- [ ] Network connectivity validation
- [ ] Rate limit detection
- [ ] Cost estimation before validation

### Advanced Recovery
- [ ] Rollback failed installations
- [ ] Cleanup corrupted installations
- [ ] Download mirrors fallback
- [ ] Offline installation bundles

## Testing Recommendations

### State Persistence
1. Complete wizard with interruptions at various steps
2. Verify resume from saved state
3. Test with network failures
4. Verify data cleanup on reset

### Provider Validation
1. Test each provider with valid/invalid configurations
2. Test partial validation mode
3. Test field-level error messages
4. Verify suggested fixes are helpful

### Auto-Save
1. Monitor auto-save triggers in dev tools
2. Verify no excessive API calls
3. Test with slow network
4. Verify error handling

## Documentation Updates Needed

- [ ] Update `FIRST_RUN_WIZARD_STABILIZATION_SUMMARY.md` with persistence details
- [ ] Update API documentation with new endpoints
- [ ] Add user guide for wizard recovery
- [ ] Add developer guide for extending validation

## Correlation ID Usage

All operations include correlation IDs for debugging:
- Logged on backend with structured logging
- Returned in responses for client-side tracking
- Can be used to trace operations across logs
- Format: `wizard-save-{timestamp}`, `wizard-complete-{timestamp}`, etc.

## Code Quality

- ✅ Zero warnings in backend build
- ✅ Zero errors in backend build
- ✅ TypeScript strict mode compliance
- ✅ Proper error typing (no `any` types)
- ✅ Structured logging with Serilog
- ✅ Correlation ID tracking throughout
- ✅ ConfigureAwait(false) in library code
- ✅ Async/await best practices
- ✅ Null safety with nullable reference types

## Files Modified

1. **Aura.Api/Controllers/SetupController.cs** - Wizard state endpoints
2. **Aura.Api/Controllers/ProvidersController.cs** - Enhanced validation endpoints
3. **Aura.Api/Models/ApiModels.V1/ProviderValidationDtos.cs** - New DTOs
4. **Aura.Web/src/services/api/setupApi.ts** - Frontend API methods
5. **Aura.Web/src/state/onboarding.ts** - State persistence and auto-save
