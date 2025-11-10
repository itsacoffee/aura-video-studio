# Comprehensive Error Handling and Recovery Guide

**PR #7: Comprehensive Error Handling and Recovery**  
**Priority:** P2 - RELIABILITY  
**Status:** ✅ COMPLETED  
**Implementation Date:** 2025-11-10

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Global Error Handling](#global-error-handling)
- [User-Friendly Error Messages](#user-friendly-error-messages)
- [Provider Error Handling](#provider-error-handling)
- [Recovery Mechanisms](#recovery-mechanisms)
- [Error Prevention](#error-prevention)
- [Testing](#testing)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

This guide documents the comprehensive error handling and recovery system implemented in Aura Video Studio. The system ensures that:

- ✅ No unhandled exceptions reach users
- ✅ All errors have recovery options
- ✅ Error messages are helpful and actionable
- ✅ System recovers from crashes automatically
- ✅ Users don't lose work

## Architecture

### Backend Error Handling Stack

```
┌─────────────────────────────────────┐
│   ExceptionHandlingMiddleware       │  ← Catches all exceptions
├─────────────────────────────────────┤
│   GlobalExceptionHandler            │  ← IExceptionHandler impl
├─────────────────────────────────────┤
│   ErrorMetricsCollector              │  ← Tracks error patterns
├─────────────────────────────────────┤
│   ErrorDocumentation                 │  ← Maps to help docs
├─────────────────────────────────────┤
│   AuraException Hierarchy            │  ← Typed exceptions
└─────────────────────────────────────┘
```

### Frontend Error Handling Stack

```
┌─────────────────────────────────────┐
│   GlobalErrorBoundary                │  ← App-level boundary
├─────────────────────────────────────┤
│   RouteErrorBoundary                 │  ← Per-route boundaries
├─────────────────────────────────────┤
│   ComponentErrorBoundary             │  ← Component-level
├─────────────────────────────────────┤
│   CrashRecoveryService               │  ← Detects crashes
├─────────────────────────────────────┤
│   ApiErrorDisplay                    │  ← API error UI
└─────────────────────────────────────┘
```

## Global Error Handling

### Backend Exception Handler

**Location:** `Aura.Api/Middleware/ExceptionHandlingMiddleware.cs`

#### Features:
- Catches all unhandled exceptions
- Maps exceptions to appropriate HTTP status codes
- Adds correlation IDs for tracking
- Integrates with ErrorMetricsCollector
- Returns standardized JSON responses

#### Example Response:

```json
{
  "errorCode": "E100-401",
  "errorTitle": "LLM Authentication Failed",
  "message": "The API key for your language model provider is invalid or expired",
  "technicalDetails": "OpenAI API returned 401 Unauthorized",
  "suggestedActions": [
    "Check your API key in Settings → Providers",
    "Verify the key hasn't expired",
    "Try regenerating the key from OpenAI dashboard"
  ],
  "learnMoreUrl": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/provider-errors#authentication",
  "correlationId": "a1b2c3d4-e5f6-7890",
  "timestamp": "2025-11-10T12:34:56Z",
  "isTransient": false
}
```

### Frontend Error Boundaries

**Location:** `Aura.Web/src/components/ErrorBoundary/`

#### Component Hierarchy:

1. **GlobalErrorBoundary** - Wraps entire app
   - Shows full-page error screen
   - Offers reload, go home, report options
   - Integrates with crash recovery

2. **RouteErrorBoundary** - Wraps individual routes
   - Shows route-specific error
   - Offers retry with parent callback
   - Doesn't crash entire app

3. **ComponentErrorBoundary** - Wraps critical components
   - Shows component-specific fallback
   - Allows rest of app to function
   - Logs error with component name

#### Usage Example:

```tsx
import { RouteErrorBoundary } from './components/ErrorBoundary';

function MyPage() {
  const fetchData = async () => {
    // Data fetching logic
  };

  return (
    <RouteErrorBoundary onRetry={fetchData}>
      <MyPageContent />
    </RouteErrorBoundary>
  );
}
```

## User-Friendly Error Messages

### Error Code System

All errors have structured error codes for easy reference:

| Code Range | Category | Example |
|------------|----------|---------|
| E001-E099 | Validation Errors | E001: Validation Error |
| E100-E199 | LLM Provider Errors | E100-401: Auth Failed |
| E200-E299 | TTS Provider Errors | E200-429: Rate Limited |
| E400-E499 | Visual Provider Errors | E400: Image Gen Failed |
| E500-E599 | Rendering Errors | E500: FFmpeg Failed |
| E900-E999 | System Errors | E999: Unexpected Error |

### Error Documentation Links

**Location:** `Aura.Core/Errors/ErrorDocumentation.cs`

Every error code maps to a documentation URL with:
- **Title** - Short, descriptive name
- **Description** - What the error means
- **URL** - Link to detailed troubleshooting guide

#### Adding New Error Documentation:

```csharp
["MY_ERROR_CODE"] = new(
    "User-Friendly Title",
    "Clear description of what this error means",
    "https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/my-error"
)
```

### API Error Display Component

**Location:** `Aura.Web/src/components/ErrorBoundary/ApiErrorDisplay.tsx`

Displays API errors with:
- ✅ Error code and title
- ✅ User-friendly message
- ✅ Suggested actions (bulleted list)
- ✅ "Learn More" link to documentation
- ✅ Optional retry button for transient errors
- ✅ Technical details (collapsible for developers)

#### Usage:

```tsx
import { ApiErrorDisplay, parseApiError } from './components/ErrorBoundary';

function MyComponent() {
  const [error, setError] = useState<ApiError | null>(null);

  const handleSubmit = async () => {
    try {
      const response = await fetch('/api/generate');
      if (!response.ok) {
        const apiError = await parseApiError(response);
        setError(apiError);
      }
    } catch (err) {
      // Handle error
    }
  };

  if (error) {
    return (
      <ApiErrorDisplay
        error={error}
        onRetry={handleSubmit}
        showTechnicalDetails={true}
      />
    );
  }

  return <MyForm onSubmit={handleSubmit} />;
}
```

## Provider Error Handling

### API Key Validation

**Location:** `Aura.Core/Validation/ApiKeyValidator.cs`

Validates API keys before making requests:

```csharp
var result = ApiKeyValidator.ValidateKey("OPENAI_KEY", apiKey);

if (!result.IsSuccess)
{
    throw ProviderException.MissingApiKey("OpenAI", ProviderType.LLM, "OPENAI_KEY");
}
```

#### Features:
- Format validation (e.g., OpenAI keys start with `sk-`)
- Whitespace detection
- Common mistake detection (Bearer prefix, etc.)
- Key masking for safe display

### Rate Limiting Handling

**Location:** `Aura.Core/Errors/ProviderException.cs`

Special handling for rate limit errors:

```csharp
throw ProviderException.RateLimited(
    providerName,
    providerType,
    retryAfterSeconds: 60,
    correlationId
);
```

Returns HTTP 429 with:
- Clear "rate limit exceeded" message
- Retry-after time if available
- Suggestions to upgrade plan or use different provider

### Automatic Fallback

**Location:** `Aura.Core/Services/Providers/ProviderFallbackService.cs`

Automatically tries alternative providers when primary fails:

```csharp
var result = await _fallbackService.ExecuteWithLlmFallbackAsync(
    async (provider) => await provider.GenerateTextAsync(prompt),
    providers,
    cancellationToken
);
```

#### Fallback Chain:
1. Primary provider (e.g., OpenAI)
2. Secondary provider (e.g., Anthropic)
3. Tertiary provider (e.g., Google Gemini)
4. Local provider (e.g., Ollama)
5. Rule-based fallback (templates)

### Circuit Breaker Integration

Providers with repeated failures are automatically skipped via circuit breaker:

```csharp
var status = _circuitBreakerService.GetStatus(providerName);
if (status.State == CircuitState.Open)
{
    // Skip this provider, try next in chain
    continue;
}
```

## Recovery Mechanisms

### Crash Recovery Service

**Location:** `Aura.Web/src/services/crashRecoveryService.ts`

Detects crashes on application startup:

```typescript
// In App.tsx
useEffect(() => {
  const state = crashRecoveryService.initialize();
  
  if (crashRecoveryService.shouldShowRecoveryScreen()) {
    setShowRecoveryScreen(true);
  }

  // Mark clean shutdown on beforeunload
  const handleBeforeUnload = () => {
    crashRecoveryService.markCleanShutdown();
  };
  window.addEventListener('beforeunload', handleBeforeUnload);

  return () => {
    window.removeEventListener('beforeunload', handleBeforeUnload);
  };
}, []);
```

#### How It Works:
1. Sets `sessionStorage` flag when app starts
2. On next startup, checks if flag is still set (indicating crash)
3. Tracks consecutive crashes
4. Shows recovery screen after 3+ consecutive crashes

### Crash Recovery Screen

**Location:** `Aura.Web/src/components/ErrorBoundary/CrashRecoveryScreen.tsx`

Displayed when multiple crashes detected:

#### Recovery Options:
- **Continue in Safe Mode** - Reset crash counter, go to home
- **Restore Auto-save** - Recover last saved state
- **Clear All Data** - Nuclear option for corrupted data

### Auto-Save Integration

The error fallback components integrate with existing auto-save service:

```tsx
const hasAutosave = autoSaveService.hasRecoverableData();

{hasAutosave && (
  <Button onClick={handleRestoreAutosave}>
    Restore Auto-save
  </Button>
)}
```

### Undo/Redo System

Already implemented (see `UNDO_REDO_GUIDE.md`):
- Global undo/redo with Ctrl+Z / Ctrl+Y
- Command history with 100 actions
- Persisted across sessions (Phase 2)

## Error Prevention

### Input Validation

**Location:** `Aura.Web/src/utils/inputValidation.ts`

Validates user input before submission:

```typescript
import { validateVideoTitle, validateApiKey } from './utils/inputValidation';

const titleValidation = validateVideoTitle(title);
if (!titleValidation.isValid) {
  setError(titleValidation.error);
  return;
}

if (titleValidation.warning) {
  showWarning(titleValidation.warning);
}
```

#### Available Validators:
- `validateVideoTitle` - Checks length, invalid characters
- `validateVideoDescription` - Min/max length checks
- `validateApiKey` - Format and common mistakes
- `validateDuration` - Range and warning for long videos
- `validateFileSize` - Max size with warnings
- `validateImageResolution` - Dimension limits
- `validateUrl` - Valid HTTP/HTTPS URLs
- `validateEmail` - Email format
- `validateNumber` - Range validation
- `validateArrayLength` - Min/max items

### Confirmation Dialogs

**Location:** `Aura.Web/src/components/Common/ConfirmationDialog.tsx`

Confirms risky operations:

```tsx
import { ConfirmationDialog } from './components/Common/ConfirmationDialog';

function DangerousOperation() {
  const [open, setOpen] = useState(false);

  const handleDelete = async () => {
    // Perform delete
  };

  return (
    <ConfirmationDialog
      trigger={<Button>Delete All Projects</Button>}
      title="Delete All Projects?"
      message="This action cannot be undone."
      consequences={[
        "All projects will be permanently deleted",
        "Project history will be lost",
        "This cannot be recovered"
      ]}
      severity="danger"
      confirmText="Delete All"
      onConfirm={handleDelete}
      open={open}
      onOpenChange={setOpen}
    />
  );
}
```

#### Severity Levels:
- **danger** - Destructive, irreversible actions
- **warning** - Risky actions with consequences
- **info** - Informational confirmations

### System Requirements Checker

**Location:** `Aura.Web/src/components/Common/SystemRequirementsChecker.tsx`

Validates system capabilities on startup:

```tsx
import { SystemRequirementsChecker } from './components/Common/SystemRequirementsChecker';

function SetupWizard() {
  const [showChecker, setShowChecker] = useState(true);

  if (showChecker) {
    return (
      <SystemRequirementsChecker
        onContinue={() => setShowChecker(false)}
      />
    );
  }

  return <MainApp />;
}
```

#### Checks:
- ✅ Browser support (fetch, localStorage, Promise)
- ✅ Local storage availability
- ✅ Memory usage (if available)
- ✅ Network connectivity
- ✅ WebGL support
- ✅ Web Workers support
- ✅ IndexedDB availability
- ✅ Screen resolution

## Testing

### Backend Tests

**Location:** `Aura.Tests/Errors/` and `Aura.Tests/Validation/`

#### Test Coverage:
- `ErrorDocumentationTests` - 8 tests
- `ApiKeyValidatorTests` - 10 tests
- Existing resilience tests - 49 tests (from PR #8)

#### Running Tests:

```bash
# Run all error handling tests
dotnet test --filter "FullyQualifiedName~Aura.Tests.Errors"

# Run validation tests
dotnet test --filter "FullyQualifiedName~Aura.Tests.Validation"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

**Location:** `Aura.Web/src/**/__tests__/`

#### Test Coverage:
- `crashRecoveryService.test.ts` - 9 tests
- `inputValidation.test.ts` - 14 tests
- Existing error boundary tests

#### Running Tests:

```bash
cd Aura.Web

# Run all tests
npm test

# Run specific test file
npm test crashRecoveryService.test

# Run with coverage
npm test -- --coverage
```

### E2E Error Scenarios

Consider adding E2E tests for:
- Invalid API key error flow
- Rate limit handling with retry
- Crash recovery after simulated crash
- Form validation preventing submission
- Confirmation dialog workflow

## Best Practices

### 1. Always Use Typed Exceptions

❌ Don't:
```csharp
throw new Exception("API key not found");
```

✅ Do:
```csharp
throw ProviderException.MissingApiKey("OpenAI", ProviderType.LLM, "OPENAI_KEY", correlationId);
```

### 2. Provide Actionable Error Messages

❌ Don't:
```
Error: Failed to generate video
```

✅ Do:
```
Failed to generate video: OpenAI API key is invalid

What you can do:
- Check your API key in Settings → Providers
- Verify the key hasn't expired
- Try regenerating the key from OpenAI dashboard

Learn more: https://github.com/Coffee285/aura-video-studio/blob/main/docs/setup/api-keys.md
```

### 3. Validate Early

❌ Don't wait for API to reject:
```typescript
// Let API validate
await fetch('/api/generate', { body: JSON.stringify({ title }) });
```

✅ Do validate client-side first:
```typescript
const validation = validateVideoTitle(title);
if (!validation.isValid) {
  showError(validation.error);
  return;
}

await fetch('/api/generate', { body: JSON.stringify({ title }) });
```

### 4. Use Error Boundaries Strategically

❌ Don't wrap entire app in one boundary:
```tsx
<ErrorBoundary>
  <EntireApp />
</ErrorBoundary>
```

✅ Do use hierarchical boundaries:
```tsx
<GlobalErrorBoundary>
  <App>
    <RouteErrorBoundary>
      <VideoEditorPage />
    </RouteErrorBoundary>
    <RouteErrorBoundary>
      <SettingsPage />
    </RouteErrorBoundary>
  </App>
</GlobalErrorBoundary>
```

### 5. Log Errors with Context

❌ Don't log bare errors:
```typescript
console.error(error);
```

✅ Do log with context:
```typescript
loggingService.error(
  'Failed to save project',
  error,
  'ProjectEditor',
  'handleSave',
  {
    projectId,
    hasUnsavedChanges,
    autoSaveEnabled
  }
);
```

## Troubleshooting

### Error Not Showing "Learn More" Link

**Cause:** Error code not in ErrorDocumentation map

**Solution:**
1. Add error code to `ErrorDocumentation.cs`
2. Or ensure error uses existing error code
3. Fallback URL will be used if specific doc not found

### Crash Recovery Not Detecting Crashes

**Possible Causes:**
- User closed tab normally (beforeunload fired)
- Browser cleared sessionStorage
- Service not initialized in App.tsx

**Solution:**
```typescript
// Ensure crash recovery is initialized early
useEffect(() => {
  crashRecoveryService.initialize();
}, []);
```

### API Errors Not Displaying Nicely

**Cause:** Error response not following standard format

**Solution:** Ensure backend returns:
```json
{
  "errorCode": "E100",
  "message": "User-friendly message",
  "suggestedActions": ["Action 1", "Action 2"],
  "learnMoreUrl": "https://...",
  "isTransient": false
}
```

### Validation Not Preventing Submission

**Cause:** Validation not checked before submission

**Solution:**
```typescript
const handleSubmit = () => {
  const validation = validateInput(input);
  if (!validation.isValid) {
    setError(validation.error);
    return; // Don't proceed
  }
  
  // Proceed with submission
};
```

## Acceptance Criteria Status

✅ **No unhandled exceptions reach users**
- Global exception handler catches all backend exceptions
- Error boundaries catch all frontend errors
- Crash recovery handles app-level failures

✅ **All errors have recovery options**
- Transient errors have retry buttons
- Crash recovery provides multiple recovery paths
- Auto-save integration for work recovery
- Confirmation dialogs prevent accidental actions

✅ **Error messages are helpful**
- All errors have error codes for reference
- User-friendly messages explain what happened
- Suggested actions tell users what to do
- "Learn More" links provide detailed help

✅ **System recovers from crashes**
- Crash detection on startup
- Recovery screen after repeated crashes
- Auto-save restoration
- Reset crash counter on successful recovery

✅ **Users don't lose work**
- Auto-save service (already implemented)
- Undo/redo system (already implemented)
- Crash recovery with auto-save integration
- Warning dialogs before destructive actions

## Related Documentation

- [ERROR_RECOVERY_RESILIENCE_IMPLEMENTATION.md](./ERROR_RECOVERY_RESILIENCE_IMPLEMENTATION.md) - PR #8 Resilience patterns
- [UNDO_REDO_GUIDE.md](./UNDO_REDO_GUIDE.md) - Undo/redo system
- [DEVELOPMENT.md](./DEVELOPMENT.md) - Development guidelines

## Future Enhancements

### Phase 2 (Optional):
1. **Server-Side Error Aggregation** - Track error patterns across users
2. **Smart Error Recovery** - ML-based error recovery suggestions
3. **Automated Bug Reports** - One-click error reporting to GitHub
4. **Error Analytics Dashboard** - Admin view of error trends
5. **Predictive Error Prevention** - Warn before errors occur based on patterns

## Summary

This implementation provides comprehensive error handling and recovery:

✅ Global error handling on backend and frontend  
✅ User-friendly error messages with documentation links  
✅ Provider-specific error handling with fallbacks  
✅ Crash recovery with auto-save integration  
✅ Input validation and confirmation dialogs  
✅ Comprehensive test coverage  
✅ Complete documentation  

The system ensures users have a reliable, frustration-free experience even when errors occur.

---

**Last Updated:** 2025-11-10  
**Implemented By:** PR #7 - Comprehensive Error Handling and Recovery
