# Provider API Key Validation Patience Strategy

## Overview

Aura Video Studio implements a patient, latency-aware validation strategy for API keys that respects the performance characteristics of different provider types. This approach prevents premature failures for slow providers while maintaining fast validation for responsive services.

## Validation Status States

### 1. **Not Validated**
- **What it means**: API key has been configured but not yet tested
- **What to do**: Click "Revalidate" to test the connection
- **Icon**: Clock (gray)

### 2. **Validating** (Normal)
- **What it means**: Validation in progress, within normal timeout threshold
- **What to do**: Wait for completion (typically 15-30 seconds)
- **Icon**: Clock (blue)
- **Duration**: Up to normal threshold (e.g., 15s for cloud APIs, 30s for local)

### 3. **Validating Extended**
- **What it means**: Validation taking longer than usual but still acceptable
- **What to do**: Continue waiting - this is normal for some providers
- **Icon**: Clock (orange)
- **Duration**: Between normal and extended threshold (e.g., 15-60s)
- **Note**: Common for local LLMs (Ollama), complex TTS requests, or slow networks

### 4. **Validating Max Wait**
- **What it means**: Validation approaching maximum patience threshold
- **What to do**: Wait for completion or manually cancel if needed
- **Icon**: Clock (red)
- **Duration**: Between extended and maximum threshold (e.g., 60-180s)
- **Note**: Rare, indicates very slow provider or network issues

### 5. **Valid**
- **What it means**: API key successfully validated and working
- **What to do**: Nothing - key is ready to use
- **Icon**: Checkmark (green)
- **Cache**: Status cached until manual revalidation

### 6. **Slow But Working**
- **What it means**: Provider is slow but responding correctly
- **What to do**: Consider checking network connectivity or provider status
- **Icon**: Warning (yellow)
- **Note**: Key works but performance may impact generation speed

### 7. **Invalid**
- **What it means**: API key failed validation
- **What to do**: Check the key for typos, verify it's active, try revalidating
- **Icon**: Error (red)
- **Reasons**: Incorrect key, expired key, rate limit, or provider issues

### 8. **Timed Out**
- **What it means**: Validation exceeded maximum patience threshold
- **What to do**: Check provider status, network connectivity, or try again later
- **Icon**: Error (red)
- **Note**: Does NOT mean key is invalid - provider may be down or unreachable

## Provider-Specific Timeouts

### Cloud LLM APIs (OpenAI, Anthropic, Gemini, Azure)
- **Normal**: 15 seconds
- **Extended**: 60 seconds
- **Maximum**: 120 seconds
- **Rationale**: Generally fast with good infrastructure

### Local LLM (Ollama)
- **Normal**: 30 seconds
- **Extended**: 180 seconds (3 minutes)
- **Maximum**: 300 seconds (5 minutes)
- **Rationale**: Local processing, may need model loading, CPU-intensive

### TTS Services (ElevenLabs, PlayHT)
- **Normal**: 20 seconds
- **Extended**: 60 seconds
- **Maximum**: 120 seconds
- **Rationale**: Audio processing takes time, scales with text length

### Fallback Providers (RuleBased, local utilities)
- **Normal**: 5 seconds
- **Extended**: 15 seconds
- **Maximum**: 30 seconds
- **Rationale**: Should be very fast, minimal latency

## Patience Philosophy

### Why We Wait

1. **Local Models Need Time**: Ollama and other local LLMs may need to:
   - Load models into memory (can take 30-60 seconds)
   - Process validation request
   - First-run initialization

2. **Network Variability**: Slow networks shouldn't fail validation
   - Users on slower connections need more time
   - International requests have higher latency
   - VPNs and proxies add overhead

3. **Provider Spikes**: Occasional slowness shouldn't disable providers
   - API rate limiting may cause delays
   - Provider infrastructure issues are temporary
   - Retry with backoff gives providers recovery time

### User Control

**We never auto-cycle away from a slow provider** - the user chose it for a reason:
- **Manual Revalidation**: User explicitly triggers validation
- **Manual Fallback**: User decides when to switch providers
- **Clear Status**: UI shows what's happening and why
- **Actionable Guidance**: Tooltips explain next steps

## Configuration

Timeout thresholds are configured in `providerTimeoutProfiles.json`:

```json
{
  "profiles": {
    "local_llm": {
      "normalThresholdMs": 30000,
      "extendedThresholdMs": 180000,
      "deepWaitThresholdMs": 300000,
      "heartbeatIntervalMs": 15000,
      "description": "Local LLM models (Ollama)"
    }
  }
}
```

### Customization

Administrators can adjust timeouts per deployment:
1. Edit `providerTimeoutProfiles.json`
2. Modify threshold values for specific provider categories
3. Add new provider mappings as needed
4. Restart application to reload configuration

## Best Practices

### For Users

1. **First Validation**: First-time validation may take longer (model loading)
2. **Patience Pays**: Wait for "Extended" status before cancelling
3. **Network Check**: If multiple providers timeout, check your connection
4. **Revalidate Sparingly**: Only revalidate when necessary (keys rarely change)

### For Administrators

1. **Adjust Thresholds**: Increase timeouts if users frequently see "Timed Out"
2. **Monitor Patterns**: Look for providers that consistently hit extended timeouts
3. **Provider Status**: Check provider status pages if timeouts increase
4. **Network Analysis**: Investigate network path if specific providers are slow

## Troubleshooting

### Provider Always Times Out
- **Check**: Provider status page for outages
- **Check**: Network connectivity and firewall rules
- **Check**: API key is valid and active
- **Try**: Increase timeout threshold in configuration

### Validation Stuck at "Validating"
- **Wait**: Allow full timeout period (check remaining time)
- **Cancel**: Click "Revalidate" again to restart
- **Check**: Application logs for detailed error messages

### "Slow But Working" Status
- **Accept**: Key is valid, just slow
- **Investigate**: Network speed, provider load
- **Consider**: Alternative provider if speed is critical

## Security Considerations

- **No Auto-Rotation**: Slow providers are never auto-disabled
- **Integrity Checks**: All validation state is integrity-verified
- **Audit Trail**: All manual revalidation actions are logged
- **Rate Limiting**: Revalidation respects provider rate limits

## Related Documentation

- [Provider Configuration Guide](../PROVIDER_INTEGRATION_GUIDE.md)
- [Latency Management](../LLM_LATENCY_MANAGEMENT.md)
- Timeout Profiles Configuration
