# Error UX Implementation - Toasts, Retry, and Open Logs

## Overview

This implementation enhances error handling and user experience by providing:
- **Human-readable error messages** with structured ProblemDetails
- **Correlation IDs** for tracking errors across frontend and backend
- **Retry functionality** to quickly reattempt failed operations
- **Open Logs button** to navigate directly to log files for debugging
- **Platform-aware log folder opening** (Windows, macOS, Linux)

## Components

### Frontend (TypeScript/React)

#### 1. Enhanced Toasts (`Aura.Web/src/components/Notifications/Toasts.tsx`)

**Changes:**
- Updated `FailureToastOptions` interface to support:
  - `correlationId?: string` - Display correlation ID for tracking
  - `errorCode?: string` - Display error code (e.g., E300-E311)
  - `onRetry?: () => void` - Handler for retry button (replaces `onFix`)
  - `onOpenLogs?: () => void` - Handler for opening logs folder
- Enhanced `showFailureToast` to display correlation ID and error code
- Changed button text from "Fix" to "Retry" and "View logs" to "Open Logs"
- Error toasts don't auto-dismiss (`timeout: -1`) to ensure users see them

**Usage:**
```typescript
import { useNotifications } from '../components/Notifications/Toasts';
import { openLogsFolder } from '../utils/apiErrorHandler';

const { showFailureToast } = useNotifications();

showFailureToast({
  title: 'Generation failed',
  message: 'The script generation service encountered an error',
  correlationId: 'abc123',
  errorCode: 'E300',
  errorDetails: 'Provider authentication failed',
  onRetry: () => handleRetry(),
  onOpenLogs: openLogsFolder,
});
```

#### 2. API Error Handler Utility (`Aura.Web/src/utils/apiErrorHandler.ts`)

**Purpose:** Parse error responses from API and extract ProblemDetails

**Key Functions:**

- `parseApiError(error: any): Promise<ParsedApiError>`
  - Parses Response objects, ProblemDetails, Error objects, or unknown errors
  - Extracts correlation ID from response body or `X-Correlation-ID` header
  - Extracts error code from type URI (e.g., `https://docs.aura.studio/errors/E300`)
  - Returns structured error information for display

- `openLogsFolder(): void`
  - Calls `POST /api/logs/open-folder` to open logs in file explorer
  - Falls back to navigating to `/logs` page if API fails

**Usage:**
```typescript
import { parseApiError, openLogsFolder } from '../utils/apiErrorHandler';

try {
  const response = await fetch('/api/jobs', { method: 'POST', ... });
  if (!response.ok) {
    const errorInfo = await parseApiError(response);
    showFailureToast({
      title: errorInfo.title,
      message: errorInfo.message,
      correlationId: errorInfo.correlationId,
      errorCode: errorInfo.errorCode,
      onRetry: () => handleRetry(),
      onOpenLogs: openLogsFolder,
    });
  }
} catch (error) {
  const errorInfo = await parseApiError(error);
  // ... show toast
}
```

#### 3. Updated Components

**GenerationPanel** (`Aura.Web/src/components/Generation/GenerationPanel.tsx`)
- Shows error toast when job fails with correlation ID
- Provides Retry button that closes panel (user can start over)
- Provides Open Logs button using `openLogsFolder()`

**CreateWizard** (`Aura.Web/src/pages/Wizard/CreateWizard.tsx`)
- Enhanced error handling for job creation failures
- Parses ProblemDetails from API errors
- Shows toasts with retry functionality
- Uses `parseApiError` utility for consistent error parsing

### Backend (C#)

#### 1. Correlation ID Middleware (`Aura.Api/Middleware/CorrelationIdMiddleware.cs`)

**Already implemented** - ensures all requests have a correlation ID:
- Generates or accepts correlation ID from `X-Correlation-ID` header
- Adds correlation ID to response headers
- Pushes correlation ID to Serilog's LogContext (appears in all logs)
- Stores correlation ID in `HttpContext.Items["CorrelationId"]`

#### 2. ProblemDetails Helper (`Aura.Api/Helpers/ProblemDetailsHelper.cs`)

**Enhancements:**
- All error creation methods now accept `HttpContext? httpContext = null`
- `CreateScriptError()` includes correlation ID in ProblemDetails extensions
- Added `CreateProblem()` generic helper for non-script errors
- Error codes E300-E311 map to appropriate HTTP status codes

**Error Code Standards:**
- **E300**: General script provider failure (500)
- **E301**: Request timeout or cancellation (408)
- **E302**: Provider returned empty/invalid script (500)
- **E303**: Invalid enum value or input validation (400)
- **E304**: Invalid plan parameters (400)
- **E305**: Provider not available (500)
- **E306**: Authentication failure (401)
- **E307**: Offline mode restriction (403)
- **E308**: Rate limit exceeded (429)
- **E309**: Invalid script format (422)
- **E310**: Content policy violation (400)
- **E311**: Insufficient resources (503)

**Usage:**
```csharp
public static IResult MyEndpoint(HttpContext httpContext)
{
    try
    {
        // ... operation
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Operation failed");
        return ProblemDetailsHelper.CreateScriptError(
            "E300", 
            "Failed to process request",
            httpContext  // Pass HttpContext to include correlation ID
        );
    }
}
```

#### 3. Open Logs Endpoint (`Aura.Api/Program.cs`)

**New Endpoint:** `POST /api/logs/open-folder`

**Purpose:** Opens the logs folder in the native file explorer

**Platform Support:**
- **Windows**: Uses `explorer.exe`
- **macOS**: Uses `open` command
- **Linux**: Uses `xdg-open`, falls back to `nautilus` or `dolphin`

**Response:**
```json
{
  "success": true,
  "path": "/path/to/logs"
}
```

**Error Handling:**
- Returns 501 if platform is not supported
- Returns 500 if command execution fails
- Creates logs directory if it doesn't exist

#### 4. Updated Endpoints

**Script Generation** (`POST /api/script`)
- Now passes `HttpContext` to all `ProblemDetailsHelper` calls
- Ensures correlation ID is included in all error responses
- Maintains existing error code structure (E300-E311)

## Tests

### Unit Tests (Vitest)

**Toast Error UX** (`Aura.Web/src/test/toasts-error-ux.test.tsx`)
- 5 tests covering:
  - Retry button support
  - Open Logs button support
  - Correlation ID and error code display
  - Optional callbacks
  - Toaster ID generation

**API Error Handler** (`Aura.Web/src/test/api-error-handler.test.ts`)
- 7 tests covering:
  - ProblemDetails parsing from Response objects
  - Error code extraction from type URI
  - Correlation ID extraction from headers and body
  - Non-JSON response handling
  - Direct ProblemDetails object parsing
  - Error object parsing
  - Unknown error type fallback

**Results:** All 12 new tests passing ✓

### Integration Tests (.NET)

**ProblemDetailsHelper** (`Aura.Tests/ProblemDetailsHelperTests.cs`)
- 6 existing tests covering:
  - Correlation ID inclusion from HttpContext
  - Working without correlation ID
  - Status codes and titles
  - Different error codes
  - Status code retrieval
  - Guidance messages

**Results:** All 6 tests passing ✓

### E2E Tests (Playwright)

**Error UX Toasts** (`Aura.Web/tests/e2e/error-ux-toasts.spec.ts`)
- 6 scenarios testing:
  - Error toast display on job failure
  - ProblemDetails parsing and display
  - Open logs API call
  - Correlation ID extraction from headers
  - Retry functionality
  - Multiple retry attempts

## Acceptance Criteria

✅ **Replace silent failures with toasts**
- All error paths now show toasts with human-readable messages
- Toasts include title, message, error details, correlation ID, and error code

✅ **Toasts contain: human-readable reason, "Retry", and "Open Logs"**
- `FailureToastOptions` includes all required fields
- Retry button allows quick reattempt
- Open Logs button opens logs folder or navigates to logs page

✅ **Each stage raises structured error with correlationId and ProblemDetails**
- Script generation uses ProblemDetailsHelper with HttpContext
- Correlation ID middleware ensures all requests have IDs
- ProblemDetails follow RFC 7807 standard

✅ **Wire "Open Logs" to %LOCALAPPDATA%\Aura\logs (platform-aware)**
- Backend endpoint handles Windows, macOS, and Linux
- Falls back to web-based logs viewer if platform not supported
- Creates logs directory if missing

✅ **Users always see what failed and how to fix/retry**
- Error messages include actionable guidance
- Error codes map to specific issues with clear explanations
- Retry button allows immediate reattempt
- Open Logs provides access to detailed diagnostics

## Log Location

**Development:**
- Logs are stored in: `{API_Root}/logs/aura-api-YYYYMMDD.log`

**Production (Portable):**
- Logs are stored in: `%LOCALAPPDATA%\Aura\logs\`
- Can be opened via "Open Logs" button or `/logs` page

## Future Enhancements

1. **Retry with Exponential Backoff**: Automatically retry failed requests with delays
2. **Error Tracking Integration**: Send correlation IDs to error tracking service
3. **Logs Streaming**: Real-time log viewing in web UI without downloading files
4. **Stage-Specific Error Codes**: Extend E3xx range to cover TTS, Visuals, and Render stages
5. **User-Friendly Error Page**: Dedicated error details page accessible via correlation ID

## Migration Notes

**No breaking changes** - this is an additive enhancement:
- Existing error handling continues to work
- New features are opt-in via updated components
- Correlation ID middleware is already active
- ProblemDetailsHelper gracefully handles missing HttpContext

## Related Documentation

- `AGENT_11_IMPLEMENTATION.md` - Original diagnostics and log viewer implementation
- `Aura.Api/Helpers/ProblemDetailsHelper.cs` - Error code definitions and guidance
- `PORTABLE.md` - Information about logs location for end users
