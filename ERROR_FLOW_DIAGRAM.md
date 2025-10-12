# Error Flow Diagram

## User Journey: Error Handling

```
┌─────────────────────────────────────────────────────────────────┐
│                    User Initiates Action                         │
│              (e.g., Generate Video, Create Script)               │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Frontend Makes API Call                       │
│                  POST /api/script, /api/jobs, etc.               │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│              Backend: CorrelationIdMiddleware                    │
│  • Generates/extracts correlation ID from X-Correlation-ID       │
│  • Adds to response headers                                      │
│  • Stores in HttpContext.Items["CorrelationId"]                  │
│  • Pushes to Serilog LogContext (all logs include it)           │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Backend: Endpoint Processing                   │
│  • Execute business logic                                        │
│  • Script generation, TTS, rendering, etc.                       │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                ┌───────────┴───────────┐
                │                       │
            ✓ Success               ✗ Error
                │                       │
                ▼                       ▼
    ┌────────────────────┐  ┌────────────────────────────────┐
    │  Return 200 OK     │  │  ProblemDetailsHelper          │
    │  with result       │  │  • CreateScriptError()         │
    └─────────┬──────────┘  │  • Includes correlation ID     │
              │             │  • Maps to error code (E3xx)   │
              │             │  • Returns RFC 7807 response   │
              │             └─────────────┬──────────────────┘
              │                           │
              │                           ▼
              │             ┌────────────────────────────────┐
              │             │  HTTP Response                 │
              │             │  Status: 400/401/408/429/500   │
              │             │  Headers:                      │
              │             │    X-Correlation-ID: abc123    │
              │             │  Body (ProblemDetails):        │
              │             │  {                             │
              │             │    "title": "Script Failed",   │
              │             │    "detail": "...",            │
              │             │    "type": ".../E300",         │
              │             │    "status": 500,              │
              │             │    "correlationId": "abc123"   │
              │             │  }                             │
              │             └─────────────┬──────────────────┘
              │                           │
              └───────────────┬───────────┘
                              │
                              ▼
            ┌──────────────────────────────────────────┐
            │        Frontend: Response Handler         │
            │  • parseApiError() utility                │
            │  • Extracts correlation ID from:          │
            │    - Response body (correlationId field)  │
            │    - Response header (X-Correlation-ID)   │
            │  • Extracts error code from type URI      │
            │  • Formats for display                    │
            └─────────────────┬────────────────────────┘
                              │
                              ▼
            ┌──────────────────────────────────────────┐
            │        Error Toast Display                │
            │  ╔════════════════════════════════════╗  │
            │  ║ ⚠️  Generation Failed              ║  │
            │  ║                                    ║  │
            │  ║ The script generation service      ║  │
            │  ║ encountered an error.              ║  │
            │  ║                                    ║  │
            │  ║ Correlation ID: abc123             ║  │
            │  ║ Error Code: E300                   ║  │
            │  ║                                    ║  │
            │  ║ [Retry]  [Open Logs]              ║  │
            │  ╚════════════════════════════════════╝  │
            └─────────────────┬────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
            User clicks...      User clicks...
                "Retry"           "Open Logs"
                    │                   │
                    ▼                   ▼
    ┌──────────────────────┐   ┌──────────────────────────┐
    │  onRetry() Handler   │   │  openLogsFolder()        │
    │  • Re-executes the   │   │  • POST /api/logs/       │
    │    failed operation  │   │    open-folder           │
    │  • Same parameters   │   │  • Opens file explorer:  │
    │  • New correlation   │   │    - Windows: explorer   │
    │    ID generated      │   │    - macOS: open         │
    └──────────┬───────────┘   │    - Linux: xdg-open     │
               │               │  • Falls back to /logs   │
               │               │    page if fails         │
               │               └──────────────────────────┘
               │
               └──► [Loop back to "Frontend Makes API Call"]
```

## Error Code Flow

```
Error Occurs → Map to E3xx Code → Return Appropriate HTTP Status

┌─────────────────────────────────────────────────────────────────┐
│                    Error Code Mapping (E3xx)                     │
├─────────┬───────────────────────────────────────────┬───────────┤
│  Code   │ Description                               │ Status    │
├─────────┼───────────────────────────────────────────┼───────────┤
│  E300   │ General script provider failure           │   500     │
│  E301   │ Request timeout or cancellation           │   408     │
│  E302   │ Provider returned empty/invalid script    │   500     │
│  E303   │ Invalid enum value / validation error     │   400     │
│  E304   │ Invalid plan parameters                   │   400     │
│  E305   │ Provider not available                    │   500     │
│  E306   │ Authentication failure (API keys)         │   401     │
│  E307   │ Offline mode restriction                  │   403     │
│  E308   │ Rate limit exceeded                       │   429     │
│  E309   │ Invalid script format/structure           │   422     │
│  E310   │ Content policy violation                  │   400     │
│  E311   │ Insufficient system resources             │   503     │
└─────────┴───────────────────────────────────────────┴───────────┘
```

## Correlation ID Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│  1. Request Arrives                                              │
│     ├─ Client sends X-Correlation-ID: existing-id (optional)    │
│     └─ OR Middleware generates new GUID                          │
│                                                                  │
│  2. Throughout Request Processing                                │
│     ├─ Stored in HttpContext.Items["CorrelationId"]             │
│     ├─ Pushed to Serilog LogContext                             │
│     └─ All logs include: [CorrelationId] field                  │
│                                                                  │
│  3. Error Occurs                                                 │
│     ├─ ProblemDetailsHelper retrieves from HttpContext          │
│     └─ Includes in response body extensions                     │
│                                                                  │
│  4. Response Sent                                                │
│     ├─ X-Correlation-ID header: abc123                          │
│     ├─ Body includes correlationId: "abc123"                    │
│     └─ All related logs have same ID                            │
│                                                                  │
│  5. Frontend Displays                                            │
│     ├─ Toast shows: "Correlation ID: abc123"                    │
│     ├─ User can copy for support                                │
│     └─ Can search logs by ID                                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Log Access Flow

```
User Clicks "Open Logs" Button
            │
            ▼
    openLogsFolder() in frontend
            │
            ▼
    POST /api/logs/open-folder
            │
            ▼
    Backend detects platform:
            │
    ┌───────┼───────┐
    │       │       │
Windows   macOS   Linux
    │       │       │
    ▼       ▼       ▼
explorer  open  xdg-open
   .exe   cmd   (or fallback)
    │       │       │
    └───────┼───────┘
            │
            ▼
    File Explorer Opens
    Showing: logs/aura-api-YYYYMMDD.log
            │
            ▼
    User Can:
    • View recent logs
    • Search by correlation ID
    • Copy logs for support ticket
    • Diagnose issues
```

## Benefits

### For Users
- ✅ **Clear feedback**: Know exactly what went wrong
- ✅ **Self-service**: Retry failed operations immediately
- ✅ **Debugging**: Access logs without technical knowledge
- ✅ **Support**: Provide correlation ID for faster help

### For Developers
- ✅ **Traceability**: Track errors across frontend/backend
- ✅ **Debugging**: Find all logs for a specific request
- ✅ **Consistency**: All errors follow same structure
- ✅ **Standards**: RFC 7807 ProblemDetails compliance

### For Support Teams
- ✅ **Quick diagnosis**: Correlation ID leads straight to logs
- ✅ **Context**: Error code explains category of issue
- ✅ **User experience**: Users can self-resolve many issues
- ✅ **Efficiency**: Less back-and-forth for error details
