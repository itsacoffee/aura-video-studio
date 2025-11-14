# Menu Command System Architecture

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           USER CLICKS MENU ITEM                              │
│                         (e.g., File → New Project)                           │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        MAIN PROCESS (Electron)                               │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ menu-builder.js                                                        │  │
│  │                                                                         │  │
│  │  1. User clicks "New Project"                                          │  │
│  │  2. Call: _sendToRenderer('menu:newProject', {})                       │  │
│  │                                                                         │  │
│  │  3. Validate payload:                                                  │  │
│  │     const validation = validateCommandPayload(                         │  │
│  │       'menu:newProject', {}                                            │  │
│  │     );                                                                  │  │
│  │     ✓ Validation passed                                                │  │
│  │                                                                         │  │
│  │  4. Generate correlation ID:                                           │  │
│  │     correlationId = "cmd_1763080295_abc123"                            │  │
│  │                                                                         │  │
│  │  5. Log:                                                               │  │
│  │     [MenuBuilder] Sending command to renderer                          │  │
│  │       correlationId: cmd_1763080295_abc123                             │  │
│  │       channel: menu:newProject                                         │  │
│  │       command: New Project                                             │  │
│  │                                                                         │  │
│  │  6. Send IPC: webContents.send(channel, payload)                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ IPC Message
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        PRELOAD SCRIPT (Sandboxed)                            │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ menu-command-handler.js                                                │  │
│  │                                                                         │  │
│  │  1. Receive IPC event on 'menu:newProject'                             │  │
│  │                                                                         │  │
│  │  2. Re-validate payload:                                               │  │
│  │     const validation = validateCommandPayload(                         │  │
│  │       'menu:newProject', payload                                       │  │
│  │     );                                                                  │  │
│  │     ✓ Validation passed                                                │  │
│  │                                                                         │  │
│  │  3. Enhance payload:                                                   │  │
│  │     enhancedPayload = {                                                │  │
│  │       _correlationId: "cmd_1763080295_abc123",                         │  │
│  │       _timestamp: "2025-11-14T00:31:35.219Z",                          │  │
│  │       _command: {                                                      │  │
│  │         label: "New Project",                                          │  │
│  │         category: "File",                                              │  │
│  │         description: "Create a new video project"                      │  │
│  │       }                                                                 │  │
│  │     }                                                                   │  │
│  │                                                                         │  │
│  │  4. Log:                                                               │  │
│  │     [Preload:MenuCommand] Validation passed, dispatching               │  │
│  │       correlationId: cmd_1763080295_abc123                             │  │
│  │       channel: menu:newProject                                         │  │
│  │                                                                         │  │
│  │  5. Call user callback with enhanced payload                           │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ Enhanced Payload
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       RENDERER PROCESS (React)                               │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ menuCommandDispatcher.ts                                               │  │
│  │                                                                         │  │
│  │  1. dispatch('menu:newProject', enhancedPayload) called                │  │
│  │                                                                         │  │
│  │  2. Check for validation errors:                                       │  │
│  │     if (payload._validationError) {                                    │  │
│  │       showToast(error);                                                │  │
│  │       return;                                                           │  │
│  │     }                                                                   │  │
│  │     ✓ No validation errors                                             │  │
│  │                                                                         │  │
│  │  3. Find registered handlers:                                          │  │
│  │     handlers = getHandlers('menu:newProject')                          │  │
│  │     ✓ Found 1 handler                                                  │  │
│  │                                                                         │  │
│  │  4. Check context availability:                                        │  │
│  │     currentContext = GLOBAL                                            │  │
│  │     requiredContext = GLOBAL                                           │  │
│  │     ✓ Command available in current context                            │  │
│  │                                                                         │  │
│  │  5. Execute handler:                                                   │  │
│  │     handler(enhancedPayload)                                           │  │
│  │     → navigate('/create')                                              │  │
│  │                                                                         │  │
│  │  6. Log:                                                               │  │
│  │     [MenuCommand] Command completed                                    │  │
│  │       correlationId: cmd_1763080295_abc123                             │  │
│  │       duration: 15ms                                                   │  │
│  │                                                                         │  │
│  │  Result: User navigated to /create page ✓                              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Error Scenarios

### Scenario 1: Validation Error

```
User clicks: File → Open Recent Project (without data)
                                    ↓
Main: Validates payload {} against schema
      ✗ Error: "path" is required
      → Still sends IPC but includes _validationError
                                    ↓
Preload: Receives payload with _validationError
         → Calls callback with error context
                                    ↓
Renderer: Checks payload._validationError
          → Shows toast: "Command validation failed: path is required"
          → Does NOT execute handler
```

### Scenario 2: Context Mismatch

```
User clicks: File → Save Project
                                    ↓
Main: Validates and sends (validation passes)
                                    ↓
Preload: Validates and enhances (validation passes)
                                    ↓
Renderer: Checks context
          currentContext = GLOBAL (no project loaded)
          requiredContext = PROJECT_LOADED
          ✗ Context mismatch
          → Shows toast: "Save Project is not available in this view"
          → Does NOT execute handler
```

### Scenario 3: Handler Error

```
User clicks: Tools → Clear Cache
                                    ↓
Main: Validates and sends (validation passes)
                                    ↓
Preload: Validates and enhances (validation passes)
                                    ↓
Renderer: Context check passes, executes handler
          → handler throws: Error("Network timeout")
          → Caught by dispatcher
          → Shows toast: "Failed to execute Clear Cache: Network timeout"
          → Logs error with correlation ID
```

## Component Interaction

```
┌────────────────┐
│  MenuBuilder   │  Builds Electron menu, handles clicks
│  (main.js)     │  Validates, generates correlation IDs
└────────┬───────┘
         │ IPC: webContents.send()
         ▼
┌────────────────┐
│ MenuCommand    │  Validates payloads, enhances with metadata
│ Handler        │  Wraps callbacks with error handling
│ (preload.js)   │  Tracks promise completion
└────────┬───────┘
         │ Callback: window.electron.menu.onXXX()
         ▼
┌────────────────┐
│ MenuCommand    │  Checks context, finds handlers
│ Dispatcher     │  Executes handlers, shows feedback
│ (React)        │  Logs with correlation IDs
└────────────────┘
```

## Data Flow

```
Menu Click
    │
    ├─► CommandId: "menu:newProject"
    ├─► Payload: {}
    │
    ▼
Validation (Main)
    │
    ├─► Schema: EmptyPayloadSchema
    ├─► Result: ✓ Valid
    │
    ▼
Correlation ID
    │
    ├─► Generated: "cmd_1763080295_abc123"
    │
    ▼
IPC Message
    │
    ├─► Channel: "menu:newProject"
    ├─► Payload: { _correlationId, _timestamp }
    │
    ▼
Validation (Preload)
    │
    ├─► Re-validate against same schema
    ├─► Result: ✓ Valid
    │
    ▼
Enhancement
    │
    ├─► Add _command metadata
    ├─► Add _timestamp
    │
    ▼
Dispatch (Renderer)
    │
    ├─► Check context: ✓ Available
    ├─► Find handler: ✓ Found
    ├─► Execute: navigate('/create')
    │
    ▼
Result
    │
    └─► User sees new page ✓
```

## Context System

```
┌─────────────────────────────────────────┐
│         Application Contexts             │
├─────────────────────────────────────────┤
│                                          │
│  GLOBAL                                  │
│  ├─ Always available                    │
│  ├─ Commands: New, Open, Preferences    │
│  └─ Used when: No specific state needed │
│                                          │
│  PROJECT_LOADED                          │
│  ├─ Available: When project open        │
│  ├─ Commands: Save, Export              │
│  └─ Used when: User has active project  │
│                                          │
│  TIMELINE                                │
│  ├─ Available: In timeline view         │
│  ├─ Commands: Export Timeline           │
│  └─ Used when: Editing timeline         │
│                                          │
│  MEDIA_LIBRARY                           │
│  ├─ Available: In media library         │
│  ├─ Commands: Import Video/Audio/Images │
│  └─ Used when: Managing media           │
│                                          │
│  SETTINGS                                │
│  ├─ Available: In settings view         │
│  ├─ Commands: Provider Settings         │
│  └─ Used when: Configuring app          │
│                                          │
│  HELP                                    │
│  ├─ Available: In help/docs             │
│  ├─ Commands: Getting Started           │
│  └─ Used when: Viewing documentation    │
│                                          │
└─────────────────────────────────────────┘

Context Checking Flow:
1. Dispatcher: getCurrentContext() → "GLOBAL"
2. Handler: requiredContext → "PROJECT_LOADED"
3. Check: GLOBAL ≠ PROJECT_LOADED
4. Result: Command not available
5. Action: Show toast notification
```

## Logging Flow

```
[Main Process]
  [MenuBuilder] Sending command to renderer
    timestamp: 2025-11-14T00:31:35.219Z
    correlationId: cmd_1763080295_abc123
    channel: menu:newProject
    command: New Project
    category: File

[Preload Script]
  [Preload:MenuCommand] Received command
    correlationId: cmd_1763080295_abc123
    channel: menu:newProject
    timestamp: 2025-11-14T00:31:35.219Z

  [Preload:MenuCommand] Validation passed, dispatching to renderer
    correlationId: cmd_1763080295_abc123
    channel: menu:newProject
    command: New Project

[Renderer Process]
  [MenuCommand] Dispatching menu command
    correlationId: cmd_1763080295_abc123
    commandId: menu:newProject
    command: New Project
    category: File
    currentContext: GLOBAL

  [MenuCommand] Executing command handler
    correlationId: cmd_1763080295_abc123
    commandId: menu:newProject
    feature: project-management
    context: global

  [MenuCommand] Command handler completed
    correlationId: cmd_1763080295_abc123
    commandId: menu:newProject
    feature: project-management
    duration: 15ms

  [MenuCommand] Command dispatch completed
    correlationId: cmd_1763080295_abc123
    commandId: menu:newProject
    handlersExecuted: 1
    duration: 15ms
```

## Key Concepts

### Correlation ID
- Format: `cmd_timestamp_random`
- Generated: Once in main process
- Propagated: Through entire flow
- Used for: Tracking, debugging, analytics

### Validation Layers
1. **Main Process**: First validation, log if failed
2. **Preload**: Second validation, can still dispatch with error
3. **Dispatcher**: Final check, respects context

### Context Awareness
- Commands declare required contexts
- Dispatcher checks current context
- Mismatch = friendly error message
- No silent failures

### User Feedback
- Toast for validation errors
- Toast for context mismatches
- Toast for handler failures
- Always includes command label
