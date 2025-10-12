# First-Run Wizard State Machine Diagram

## State Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                      First-Run Wizard State Machine                  │
└──────────────────────────────────────────────────────────────────────┘

                                   ┌─────┐
                                   │START│
                                   └──┬──┘
                                      │
                                      ▼
                              ┌──────────────┐
                              │              │
                              │     IDLE     │◄────────────────┐
                              │              │                 │
                              └──────┬───────┘                 │
                                     │                         │
                   Click "Validate"  │                         │
                                     │                         │
                                     ▼                         │
                              ┌──────────────┐                 │
                              │              │                 │
                              │  VALIDATING  │                 │
                              │              │                 │
                              └──────┬───────┘                 │
                                     │                         │
                          ┌──────────┴──────────┐              │
                          │                     │              │
            API Success   │                     │   API Fail   │
                          ▼                     ▼              │
                   ┌──────────────┐      ┌──────────────┐     │
                   │              │      │              │     │
                   │    VALID     │      │   INVALID    │─────┘
                   │              │      │              │ Reset/Retry
                   └──────┬───────┘      └──────────────┘
                          │                     ▲
           Auto-advance   │                     │
           (step 3 only)  │                     │ User fixes
                          │                     │ and retries
                          ▼                     │
                   ┌──────────────┐             │
                   │              │             │
                   │    READY     │─────────────┘
                   │              │
                   └──────┬───────┘
                          │
            Complete/     │
            Continue      │
                          ▼
                   ┌──────────────┐
                   │              │
                   │   COMPLETE   │
                   │   (Exit)     │
                   │              │
                   └──────────────┘


┌──────────────────────────────────────────────────────────────────────┐
│                      Installation Sub-Flow                            │
└──────────────────────────────────────────────────────────────────────┘

                   ┌──────────────┐
                   │              │
                   │     IDLE     │
                   │              │
                   └──────┬───────┘
                          │
         Click "Install"  │
                          │
                          ▼
                   ┌──────────────┐
                   │              │
                   │  INSTALLING  │
                   │              │
                   └──────┬───────┘
                          │
                 ┌────────┴────────┐
                 │                 │
     Success     │                 │     Failure
                 ▼                 ▼
          ┌──────────────┐  ┌──────────────┐
          │              │  │              │
          │  INSTALLED   │  │     IDLE     │
          │              │  │   (w/error)  │
          └──────────────┘  └──────────────┘
```

## State Transitions

### States

| State | Description | Button Label | Button Disabled | Can Advance |
|-------|-------------|--------------|-----------------|-------------|
| **idle** | Initial/waiting state | "Validate" (step 3) or "Next" | No | No (except non-validation steps) |
| **validating** | Running preflight checks | "Validating…" | Yes | No |
| **valid** | Validation passed | "Next" | No | Yes |
| **invalid** | Validation failed | "Fix Issues" | No | No |
| **installing** | Installing dependencies | "Installing…" | Yes | No |
| **installed** | Installation complete | "Validate" | No | No |
| **ready** | All checks passed | "Continue" | No | Yes |

### Actions

| Action | From State | To State | Trigger |
|--------|-----------|----------|---------|
| **START_VALIDATION** | idle, installed | validating | User clicks Validate |
| **VALIDATION_SUCCESS** | validating | valid | API returns ok: true |
| **VALIDATION_FAILED** | validating | invalid | API returns ok: false |
| **RESET_VALIDATION** | valid, invalid | idle | User goes back |
| **START_INSTALL** | idle | installing | User clicks Install |
| **INSTALL_COMPLETE** | installing | installed | Download completes |
| **INSTALL_FAILED** | installing | idle | Download fails |
| **MARK_READY** | valid | ready | Auto on step 3 |

## Button State Matrix

| Wizard Step | State | Button Label | Icon | Action on Click |
|-------------|-------|--------------|------|-----------------|
| 0-2 | Any | "Next" | ChevronRight | Advance to next step |
| 3 | idle | "Validate" | Play | Start validation |
| 3 | validating | "Validating…" | Spinner | Disabled |
| 3 | valid | "Next" | ChevronRight | Auto-advance to ready |
| 3 | invalid | "Fix Issues" | Warning | Show fix actions |
| 3 | installing | "Installing…" | Spinner | Disabled |
| 3 | installed | "Validate" | Play | Retry validation |
| 3 | ready | "Continue" | VideoClip | Complete wizard |

## Error Recovery Flows

### Validation Failure → Retry

```
IDLE → VALIDATING → INVALID
  ▲                    │
  │    Fix issues      │
  │    Click Validate  │
  └────────────────────┘
```

### Installation Failure → Retry

```
IDLE → INSTALLING → IDLE (w/error)
  ▲                      │
  │    Click Install     │
  └──────────────────────┘
```

### User Navigation (Back Button)

```
ANY_STATE → RESET → IDLE (previous step)
```

## Fix Actions Flow

When validation fails (INVALID state):

```
┌──────────────────────────────────────────────────┐
│           INVALID STATE - Fix Actions             │
├──────────────────────────────────────────────────┤
│                                                   │
│  Failed Stage: Script (OpenAI)                   │
│  Message: API key not configured                 │
│  Hint: Configure OpenAI key in Settings          │
│                                                   │
│  Suggestions:                                     │
│  • Get key from platform.openai.com              │
│  • Add key in Settings → API Keys                │
│                                                   │
│  Quick Fixes:                                     │
│  ┌──────────────┐  ┌──────────────┐             │
│  │ Add API Key  │  │ Get API Key  │             │
│  │ (OpenSettings)│  │   (Help)     │             │
│  └──────────────┘  └──────────────┘             │
│                                                   │
└──────────────────────────────────────────────────┘
                       │
                       ▼
              User clicks action
                       │
         ┌─────────────┴─────────────┐
         │                           │
         ▼                           ▼
   Navigate to         Open external
   /settings?tab=      help URL
   api-keys            
         │                           │
         └─────────────┬─────────────┘
                       │
            User completes action
                       │
                       ▼
             Return to wizard
                       │
                       ▼
             Click "Validate" again
                       │
                       ▼
              IDLE → VALIDATING → VALID
```

## Auto-Advance Logic

On step 3 (validation step) only:

```
VALIDATING → VALID ──(useEffect)──> MARK_READY → READY
                                           │
                                           ▼
                              Show success screen
                              with completion buttons
```

## Hardware Detection Sub-Flow

```
Step 1: Hardware Detection
         │
         ▼
  User clicks Next
         │
         ▼
  START_HARDWARE_DETECTION
         │
         ▼
  isDetectingHardware: true
         │
    API Call
         │
    ┌────┴────┐
    │         │
Success     Fail
    │         │
    ▼         ▼
HARDWARE_   HARDWARE_
DETECTED    DETECTION_
            FAILED
    │         │
    └────┬────┘
         │
         ▼
  isDetectingHardware: false
         │
         ▼
  Display results
```

## State Persistence

- **step**: Tracked in state, not persisted
- **mode**: Tracked in state, not persisted
- **status**: Tracked in state, not persisted
- **lastValidation**: Tracked in state, includes correlationId
- **hasSeenOnboarding**: Stored in localStorage on completion

## Edge Cases Handled

1. **Network failure during validation**: Synthetic error report created
2. **User goes back after validation**: State resets to idle
3. **Multiple rapid clicks**: Button disabled during async operations
4. **Validation succeeds on non-final step**: Doesn't auto-advance
5. **Page refresh**: Returns to step 0 (onboarding not persisted)
6. **Already completed onboarding**: Redirects to home page

## Testing Coverage

### Unit Tests (37 tests)
- All state transitions
- Button label mapping
- Button disabled logic
- Error handling
- Complete state machine flows

### E2E Tests (8 tests)
- Happy path (Free-Only)
- Error path (Pro without keys)
- Navigation (Back/Forward)
- Skip flow
- Button states
- Already completed check

## Future Considerations

1. **Persistence**: Save wizard progress to localStorage
2. **Retry logic**: Automatic retry with exponential backoff
3. **Real-time updates**: WebSocket for install progress
4. **Telemetry**: Track wizard completion metrics
5. **Animations**: Smooth transitions between states
