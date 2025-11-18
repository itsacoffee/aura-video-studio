# Latency & Patience Policy

## Overview

Aura Video Studio adopts a **patience-centric** approach to provider management. This policy recognizes that slow responses from providers (especially local LLMs, TTS engines, and cloud APIs under load) are **normal behavior**, not failures. The system prioritizes **consistency** and **user trust** over speed, ensuring the user's chosen provider remains locked for the entire job duration unless explicitly overridden.

## Core Principles

### 1. Provider Stickiness

Once a user selects a provider for a job, that provider is **locked** for the entire video generation pipeline:
- Planning/Brief generation
- Script generation
- Script refinement
- TTS synthesis
- Visual prompt generation
- All stages use the same locked provider

**No silent provider switching occurs.** If the locked provider becomes unavailable, the system presents explicit fallback options to the user.

### 2. Latency ≠ Failure

Slow responses are treated as normal, expected behavior:
- Local LLM models (Ollama) can take minutes to process complex prompts
- Cloud APIs under load may have extended response times
- TTS synthesis for long scripts is inherently slow
- Image generation can be time-consuming

The system **never** auto-fails a request based purely on elapsed time.

### 3. Adaptive Patience Windows

Three latency tiers guide UI messaging and user expectations:

| Tier | Duration | UI State | User Experience |
|------|----------|----------|-----------------|
| **Normal** | 0-30s | `Active • Generating` | Standard progress indicators |
| **Extended** | 30-180s | `Extended Wait • Still Working` | Contextual hints displayed |
| **Deep-Wait** | 180s+ | `Deep Wait • Long-Form Processing` | Detailed explanation shown |

These are **informational only** and do not trigger automatic actions.

### 4. Heartbeat Detection

Providers report progress through heartbeat signals:
- **LLM**: Partial token emission, chunk streaming
- **TTS**: Audio chunk generation, segment completion
- **Image**: Progress percentage, intermediate steps

Heartbeat presence distinguishes:
- **Active Slow**: Provider is working but slow (acceptable)
- **Stalled**: No heartbeat beyond threshold (suspicious)

### 5. Stall Detection vs Auto-Fallback

When no heartbeat is detected for a provider-specific threshold (e.g., 3× expected interval):
- System emits `STALL_SUSPECTED` event
- User sees dialog: "Provider appears stalled. Continue Waiting / Try Alternative / Cancel"
- **No automatic provider switch occurs**

## Configuration

### Provider Timeout Profiles

Defined in `providerTimeoutProfiles.json`:

```json
{
  "profiles": {
    "local_llm": {
      "normalThresholdMs": 30000,
      "extendedThresholdMs": 180000,
      "deepWaitThresholdMs": 300000,
      "heartbeatIntervalMs": 15000,
      "stallSuspicionMultiplier": 3,
      "description": "Local LLM models (Ollama) - expect multi-minute processing"
    },
    "cloud_llm": {
      "normalThresholdMs": 15000,
      "extendedThresholdMs": 60000,
      "deepWaitThresholdMs": 120000,
      "heartbeatIntervalMs": 5000,
      "stallSuspicionMultiplier": 4,
      "description": "Cloud LLM APIs (OpenAI, Anthropic) - generally faster"
    },
    "tts": {
      "normalThresholdMs": 45000,
      "extendedThresholdMs": 180000,
      "deepWaitThresholdMs": 360000,
      "heartbeatIntervalMs": 10000,
      "stallSuspicionMultiplier": 3,
      "description": "TTS synthesis - scales with text length"
    },
    "image_gen": {
      "normalThresholdMs": 60000,
      "extendedThresholdMs": 240000,
      "deepWaitThresholdMs": 480000,
      "heartbeatIntervalMs": 20000,
      "stallSuspicionMultiplier": 2,
      "description": "Image generation - inherently slow, especially high-quality"
    }
  },
  "patienceProfiles": {
    "conservative": {
      "label": "Conservative (Quick Results)",
      "timeoutMultiplier": 0.7,
      "description": "Prefer faster responses, earlier stall detection"
    },
    "balanced": {
      "label": "Balanced (Default)",
      "timeoutMultiplier": 1.0,
      "description": "Standard patience windows"
    },
    "longForm": {
      "label": "Long-Form (Maximum Patience)",
      "timeoutMultiplier": 2.0,
      "description": "Extended patience for complex content, local models"
    }
  }
}
```

### User Settings

Users can configure:
- **Default Patience Profile**: Conservative / Balanced / Long-Form
- **Per-Job Override**: "Treat all latency as acceptable unless cancelled"
- **Fallback Suggestions**: "Offer alternatives only on hard error" (enabled by default)
- **Stall Notification**: Enable/disable stall detection dialog

## Error Classification

### Hard Errors (Immediate Failure)

These trigger immediate error state and offer fallback suggestions:
- HTTP 4xx/5xx responses with definitive error messages
- Serialization/deserialization failures
- Authentication failures (invalid API keys)
- Resource exhaustion (quota exceeded, out of memory)
- Provider explicitly reporting fatal error

### Soft Latency (Patient Waiting)

These do NOT trigger errors:
- Request in progress, no response yet
- Periodic heartbeat detected (tokens/chunks emitted)
- Connection alive, no explicit error

### Stall Suspicion (User Decision Required)

Triggers dialog when:
- No heartbeat received for `heartbeatIntervalMs × stallSuspicionMultiplier`
- Connection appears alive but no progress indicators
- User presented with options (continue/fallback/cancel)

## User Workflows

### Normal Flow (No Issues)

1. User selects provider (e.g., Ollama local model)
2. Job starts, provider locked
3. Progress updates via heartbeat
4. Transitions through patience tiers as needed
5. Completes successfully with locked provider

### Extended Latency Flow

1. Job exceeds Normal threshold (30s)
2. UI updates: `Extended Wait • Still Working`
3. Contextual hint: "Local models can take several minutes for complex content"
4. Heartbeat continues showing progress
5. Eventually completes

### Stall Detection Flow

1. Heartbeat stops unexpectedly
2. Stall timer expires (3× heartbeat interval)
3. Dialog appears: "Provider appears stalled. Options: Continue Waiting (30s) / Try Alternative / Cancel"
4. User chooses:
   - **Continue**: Reset stall timer, keep waiting
   - **Alternative**: Show fallback panel, require confirmation
   - **Cancel**: Job ends, partial artifacts offered if available

### Hard Error Flow

1. Provider returns HTTP 500 or explicit error
2. UI immediately shows error state
3. Fallback panel appears: "Primary provider failed. Select alternative:"
4. User explicitly chooses new provider
5. Decision logged with reason code `PROVIDER_FATAL_ERROR`

### User-Initiated Fallback

1. User clicks "Show Alternatives" button (always available)
2. Fallback panel lists available providers with trade-offs
3. User selects alternative
4. Confirmation dialog: "Switching may alter output style/tone. Proceed?"
5. On confirmation, provider switch logged with reason `USER_REQUEST`

## Decision Trace Model

Every provider transition is logged in `FallbackDecision`:

```typescript
interface FallbackDecision {
  jobId: string;
  timestamp: Date;
  fromProvider: string;
  toProvider: string;
  reasonCode: 'USER_REQUEST' | 'PROVIDER_FATAL_ERROR' | 'USER_AFTER_STALL' | 'LEGACY_AUTO';
  elapsedBeforeSwitch: number; // milliseconds
  userConfirmed: boolean;
  affectedStages: string[]; // e.g., ['script-generation', 'refinement']
}
```

No fallback decision = provider remained locked throughout job.

## Monitoring & Observability

### Log Events

```
[INFO] PROVIDER_REQUEST_START {providerId: "Ollama", stage: "script-generation", correlationId: "abc-123"}
[INFO] PROVIDER_HEARTBEAT {providerId: "Ollama", elapsedMs: 15234, tokensGenerated: 45}
[INFO] PROVIDER_LATENCY_CATEGORY_CHANGE {providerId: "Ollama", from: "Normal", to: "Extended"}
[WARN] PROVIDER_STALL_SUSPECTED {providerId: "Ollama", elapsedMsSinceLastHeartbeat: 48000}
[INFO] USER_FALLBACK_INITIATED {fromProvider: "Ollama", toProvider: "OpenAI", reasonCode: "USER_REQUEST"}
[ERROR] PROVIDER_HARD_ERROR {providerId: "OpenAI", errorCode: "quota_exceeded", message: "Rate limit exceeded"}
```

### Telemetry Metrics

Track (non-intrusive, analytics only):
- Count of jobs by latency category at completion (Normal/Extended/Deep-Wait)
- User fallback initiation rate (should be low)
- Stall detection events per provider type
- Average heartbeat intervals per provider

**These metrics inform UI/UX improvements but never trigger automatic behavior.**

## Migration from Legacy Fallback

If automatic fallback logic exists:
1. Wrap in user confirmation dialog
2. Mark legacy decisions with `reasonCode: 'LEGACY_AUTO'` in trace
3. Display warning: "Automatic fallback disabled. Enable explicit control in settings."
4. Provide migration guide for users accustomed to auto-fallback

## FAQ

**Q: Why not auto-fallback on timeout?**  
A: Users typically have one configured provider. Silent switching breaks consistency, especially for local models where slow = normal.

**Q: What if my provider is actually broken?**  
A: Hard errors trigger immediate fallback suggestions. Stalls present user dialog. You remain in control.

**Q: Can I disable patience and prefer speed?**  
A: Select "Conservative" patience profile for earlier stall detection. Fallback still requires confirmation.

**Q: What if I'm offline?**  
A: Provider lock ensures offline-capable providers (Ollama, RuleBased) continue working. Online providers fail fast with clear error.

**Q: How do I know my provider is working?**  
A: Heartbeat indicators in UI, elapsed time counter, patience tier messaging. Status drawer shows real-time state.

## Future Enhancements

- Statistical latency prediction based on recent runs
- Provider performance benchmarks per hardware tier
- Adaptive heartbeat intervals based on provider behavior
- Cross-job latency analytics dashboard

## References

- `PROVIDER_INTEGRATION_GUIDE.md` - Provider implementation details
- `TROUBLESHOOTING.md` - Latency debugging section
- `providerTimeoutProfiles.json` - Configuration reference
