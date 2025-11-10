# Resilience and Error Recovery

This guide explains Aura's resilience features including circuit breakers, retries, and automatic error recovery.

## Quick Navigation

- [Circuit Breaker](#circuit-breaker)
- [Retry Policies](#retry-policies)
- [Error Recovery](#error-recovery)
- [Fallback Strategies](#fallback-strategies)

---

## Circuit Breaker

### What is a Circuit Breaker?

A circuit breaker prevents repeated calls to a failing service, allowing it time to recover. It has three states:

1. **Closed** (Normal): Requests pass through
2. **Open** (Failing): Requests blocked, fail fast
3. **Half-Open** (Testing): Limited requests to test recovery

### Circuit Breaker Error

**Error Code**: **CIRCUIT_OPEN**

**Error Message**: "Service temporarily unavailable due to repeated failures. It will automatically recover."

**What This Means**:
- The provider has failed multiple times
- Circuit breaker has opened to prevent further failures
- Service is temporarily unavailable
- Will automatically attempt recovery

### How Long is Service Unavailable?

**Default Settings**:
- Circuit opens after: 5 consecutive failures
- Circuit stays open: 30 seconds
- Half-open test period: 10 seconds

### Solutions

#### 1. Wait for Automatic Recovery

**Best approach** - Circuit breaker will automatically:
1. Wait for recovery period (30s default)
2. Enter half-open state
3. Test with a single request
4. If successful: Close circuit (normal operation)
5. If fails: Re-open circuit for another period

**No action needed** - Just wait a minute and retry.

#### 2. Check Provider Status

While waiting, check if provider has known issues:
- OpenAI: https://status.openai.com/
- Anthropic: https://status.anthropic.com/
- Other providers: Check their status pages

#### 3. Use Alternative Provider

Switch to a working provider:
```json
{
  "Providers": {
    "LLM": {
      "Primary": "OpenAI",
      "Fallback": ["Anthropic", "LocalLLM"]
    }
  }
}
```

Or manually change provider in UI:
1. Settings → Providers
2. Select different provider
3. Retry operation

#### 4. Adjust Circuit Breaker Settings

If circuit opens too aggressively:
```json
{
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 5,        // Open after N failures
      "SamplingDuration": 60,       // In N seconds
      "MinimumThroughput": 3,       // Min requests before breaking
      "BreakDuration": 30,          // Stay open for N seconds
      "SuccessThreshold": 2         // Half-open: succeed N times to close
    }
  }
}
```

**Conservative Settings** (less aggressive):
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 10,    // Allow more failures
    "BreakDuration": 60       // Longer recovery time
  }
}
```

**Aggressive Settings** (fail fast):
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 3,     // Open quickly
    "BreakDuration": 15        // Shorter recovery time
  }
}
```

#### 5. Disable Circuit Breaker (NOT RECOMMENDED)

Only for debugging:
```json
{
  "Resilience": {
    "CircuitBreaker": {
      "Enabled": false  // All requests will be attempted
    }
  }
}
```

**Warning**: Disabling circuit breaker can lead to:
- Cascading failures
- Resource exhaustion
- Longer recovery times
- Higher costs (failed API calls may still be charged)

### Monitor Circuit Breaker State

#### Via API

```bash
GET /api/diagnostics/circuit-breakers
```

Response:
```json
{
  "circuitBreakers": [
    {
      "name": "OpenAI-LLM",
      "state": "Open",
      "failureCount": 5,
      "lastFailure": "2024-01-15T10:30:00Z",
      "nextAttempt": "2024-01-15T10:30:30Z"
    }
  ]
}
```

#### Via UI

1. Settings → Diagnostics
2. View "Resilience" tab
3. See circuit breaker states for each provider

#### Via Logs

```json
{
  "Logging": {
    "LogLevel": {
      "Aura.Core.Resilience": "Debug"
    }
  }
}
```

Look for log entries:
```
[Circuit Breaker] OpenAI-LLM: State changed to Open
[Circuit Breaker] OpenAI-LLM: Will attempt recovery in 30s
[Circuit Breaker] OpenAI-LLM: Entering Half-Open state
[Circuit Breaker] OpenAI-LLM: State changed to Closed (recovered)
```

---

## Retry Policies

### Automatic Retry

Aura automatically retries failed requests with exponential backoff.

### Retry Exhausted Error

**Error Code**: **RETRY_EXHAUSTED**

**Error Message**: "All retry attempts have been exhausted for this operation"

**What This Means**:
- Request failed
- All automatic retries also failed
- Manual intervention may be needed

### How Many Retries?

**Default Settings**:
- Max retry attempts: 3
- Initial delay: 1 second
- Backoff multiplier: 2x (exponential)
- Max delay: 30 seconds

**Timeline Example**:
1. Initial request: Fails
2. Retry 1: After 1s → Fails
3. Retry 2: After 2s → Fails
4. Retry 3: After 4s → Fails
5. Error returned: "RETRY_EXHAUSTED"

Total time: ~7 seconds before giving up

### Solutions

#### 1. Wait and Retry Manually

If retries exhausted:
1. Wait a minute for provider to stabilize
2. Retry your operation manually
3. Check provider status page

#### 2. Increase Retry Attempts

```json
{
  "Resilience": {
    "Retry": {
      "MaxRetryAttempts": 5,    // Increase from 3
      "InitialDelay": 1000,     // 1 second
      "MaxDelay": 60000,        // 60 seconds max
      "BackoffMultiplier": 2    // Exponential: 1s, 2s, 4s, 8s, 16s
    }
  }
}
```

#### 3. Adjust Retry Delay

**Slower retries** (better for rate limits):
```json
{
  "Retry": {
    "InitialDelay": 5000,      // Start at 5s
    "BackoffMultiplier": 3     // 5s, 15s, 45s
  }
}
```

**Faster retries** (better for transient errors):
```json
{
  "Retry": {
    "InitialDelay": 500,       // Start at 0.5s
    "BackoffMultiplier": 1.5   // 0.5s, 0.75s, 1.125s
  }
}
```

#### 4. Configure Per-Provider Retries

Different providers may need different retry strategies:
```json
{
  "Providers": {
    "OpenAI": {
      "Retry": {
        "MaxRetryAttempts": 5,
        "InitialDelay": 2000
      }
    },
    "Anthropic": {
      "Retry": {
        "MaxRetryAttempts": 3,
        "InitialDelay": 5000
      }
    }
  }
}
```

#### 5. Handle Specific Error Types

Configure retries for specific errors:
```json
{
  "Resilience": {
    "Retry": {
      "RetryableErrors": [
        "TransientNetworkFailure",
        "Timeout",
        "RateLimitExceeded",
        "ServiceUnavailable"
      ],
      "NonRetryableErrors": [
        "InvalidApiKey",
        "InvalidInput",
        "QuotaExceeded"
      ]
    }
  }
}
```

### When NOT to Retry

Retries are skipped for:
- **Authentication errors**: API key invalid/expired
- **Validation errors**: Bad input that won't change
- **Quota exceeded**: Need to upgrade or wait
- **Permanent failures**: 4xx errors (except 429 rate limit)

---

## Error Recovery

### Automatic Recovery Features

Aura includes several automatic recovery mechanisms:

#### 1. Graceful Degradation

When a feature fails, fall back to simpler operation:
```json
{
  "Features": {
    "GracefulDegradation": {
      "Enabled": true,
      "FallbackToBasic": true
    }
  }
}
```

Examples:
- Image generation fails → Use placeholder image
- TTS fails → Use system TTS or text display
- LLM fails → Use template or manual input

#### 2. Partial Success Handling

When batch operation partially succeeds:
```json
{
  "Resilience": {
    "PartialSuccess": {
      "Enabled": true,
      "ReturnPartialResults": true,
      "RetryFailedOnly": true
    }
  }
}
```

Example: Generating 10 images
- 7 succeed
- 3 fail
- Return 7 successful images
- Retry only the 3 failed ones

#### 3. State Persistence

Save progress to recover from crashes:
```json
{
  "Resilience": {
    "StatePersistence": {
      "Enabled": true,
      "SaveInterval": 30,  // Save every 30 seconds
      "AutoResume": true   // Resume on restart
    }
  }
}
```

#### 4. Checkpointing

For long operations, save checkpoints:
```json
{
  "Rendering": {
    "Checkpoints": {
      "Enabled": true,
      "Interval": 60,       // Every 60 seconds
      "ResumeFromCheckpoint": true
    }
  }
}
```

If rendering fails:
- Resume from last checkpoint
- Don't start from beginning
- Save time and resources

---

## Fallback Strategies

### Provider Fallback

Automatically switch to alternative provider:

```json
{
  "Providers": {
    "LLM": {
      "FallbackEnabled": true,
      "Providers": [
        {
          "Name": "OpenAI",
          "Priority": 1,
          "Enabled": true
        },
        {
          "Name": "Anthropic",
          "Priority": 2,
          "Enabled": true
        },
        {
          "Name": "LocalLLM",
          "Priority": 3,
          "Enabled": true
        }
      ],
      "FallbackTriggers": [
        "ServiceUnavailable",
        "RateLimitExceeded",
        "CircuitOpen"
      ]
    }
  }
}
```

**Fallback Flow**:
1. Try OpenAI (Priority 1)
2. If fails with trigger error → Try Anthropic (Priority 2)
3. If fails → Try LocalLLM (Priority 3)
4. If all fail → Return error

### Quality Fallback

Reduce quality to complete operation:
```json
{
  "Quality": {
    "FallbackEnabled": true,
    "Levels": [
      {
        "Name": "High",
        "Resolution": "1920x1080",
        "Bitrate": "5M",
        "Priority": 1
      },
      {
        "Name": "Medium",
        "Resolution": "1280x720",
        "Bitrate": "3M",
        "Priority": 2
      },
      {
        "Name": "Low",
        "Resolution": "854x480",
        "Bitrate": "1M",
        "Priority": 3
      }
    ]
  }
}
```

Example:
- Try high quality export
- If fails (insufficient resources) → Try medium
- If fails → Try low
- Complete with lower quality rather than fail

### Cached Response Fallback

Use cached responses when provider unavailable:
```json
{
  "Providers": {
    "CacheFallback": {
      "Enabled": true,
      "UseCacheOnError": true,
      "CacheExpiration": 86400  // 24 hours
    }
  }
}
```

---

## Timeout Configuration

### Timeout Hierarchy

1. **Request timeout**: Single HTTP request
2. **Operation timeout**: Entire operation (may include multiple requests)
3. **Job timeout**: Long-running background job

```json
{
  "Timeouts": {
    "Request": 30000,      // 30 seconds
    "Operation": 300000,   // 5 minutes
    "Job": 3600000        // 1 hour
  }
}
```

### Adjust Timeouts

#### Short Operations (API calls)
```json
{
  "Providers": {
    "OpenAI": {
      "RequestTimeout": 30000,    // 30s
      "OperationTimeout": 60000   // 1m
    }
  }
}
```

#### Long Operations (TTS generation)
```json
{
  "Providers": {
    "ElevenLabs": {
      "RequestTimeout": 120000,   // 2m
      "OperationTimeout": 300000  // 5m
    }
  }
}
```

#### Very Long Operations (Rendering)
```json
{
  "Rendering": {
    "JobTimeout": 7200000  // 2 hours
  }
}
```

### Infinite Timeout (Use Carefully)

For operations that must complete:
```json
{
  "Timeouts": {
    "Job": 0  // No timeout (wait forever)
  }
}
```

**Warning**: Can lead to hung processes if operation truly fails.

---

## Health Checks

### Enable Health Monitoring

```json
{
  "HealthChecks": {
    "Enabled": true,
    "Interval": 60000,  // Check every minute
    "Providers": {
      "Enabled": true,
      "TestConnection": true
    },
    "FFmpeg": {
      "Enabled": true,
      "TestExecution": true
    }
  }
}
```

### Health Check Endpoint

```bash
GET /api/health

# Response
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "ffmpeg": "Healthy",
    "openai": "Degraded",  // Circuit open
    "anthropic": "Healthy"
  }
}
```

### Automated Recovery Actions

Trigger actions based on health status:
```json
{
  "HealthChecks": {
    "AutomaticRecovery": {
      "Enabled": true,
      "Actions": {
        "RestartOnUnhealthy": false,  // Don't auto-restart
        "SwitchProvider": true,       // Switch to fallback
        "NotifyUser": true,          // Show notification
        "LogErrors": true            // Log for debugging
      }
    }
  }
}
```

---

## Monitoring and Alerts

### Enable Resilience Monitoring

```json
{
  "Monitoring": {
    "Resilience": {
      "Enabled": true,
      "TrackCircuitBreakerState": true,
      "TrackRetryAttempts": true,
      "TrackFallbacks": true,
      "AlertOnCircuitOpen": true,
      "AlertOnRetriesExhausted": true
    }
  }
}
```

### View Resilience Metrics

1. Settings → Diagnostics → Resilience
2. View:
   - Circuit breaker states
   - Retry statistics
   - Fallback usage
   - Recovery success rate

### Export Metrics

```bash
GET /api/diagnostics/resilience/metrics

# Response
{
  "circuitBreakers": {
    "total": 5,
    "closed": 4,
    "open": 1,
    "halfOpen": 0
  },
  "retries": {
    "total": 150,
    "successful": 120,
    "failed": 30,
    "averageAttempts": 1.5
  },
  "fallbacks": {
    "total": 10,
    "successful": 9,
    "failed": 1
  }
}
```

---

## Best Practices

### 1. Configure Appropriately for Your Use Case

**High-Volume Production**:
```json
{
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 5,
    "BreakDuration": 30
  },
  "Retry": {
    "MaxRetryAttempts": 5,
    "InitialDelay": 2000
  },
  "Fallback": {
    "Enabled": true
  }
}
```

**Development/Testing**:
```json
{
  "CircuitBreaker": {
    "Enabled": false  // See all errors
  },
  "Retry": {
    "MaxRetryAttempts": 1  // Fail fast
  }
}
```

### 2. Monitor and Adjust

- Watch circuit breaker metrics
- If opens frequently: Increase threshold or duration
- If never opens: May be too lenient

### 3. Use Multiple Providers

- Configure fallbacks for critical operations
- Distribute load across providers
- Reduce dependency on single service

### 4. Test Failure Scenarios

```json
{
  "Testing": {
    "SimulateFailures": true,
    "FailureRate": 0.1  // 10% of requests fail
  }
}
```

Verify resilience works as expected.

---

## Related Documentation

- [Provider Errors](provider-errors.md)
- [Network Errors](network-errors.md)
- [General Troubleshooting](Troubleshooting.md)
- [Error Handling Guide](../../ERROR_HANDLING_GUIDE.md)

## Need More Help?

If resilience features aren't working as expected:
1. Enable resilience logging:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Aura.Core.Resilience": "Debug"
       }
     }
   }
   ```
2. Check circuit breaker state via API or UI
3. Review retry attempt logs
4. Adjust settings based on your specific needs
5. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
