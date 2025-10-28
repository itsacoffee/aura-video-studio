# API Rate Limiting

Aura Video Studio implements production-grade rate limiting to protect against abuse and accidental denial-of-service scenarios. Rate limits are enforced per IP address with different limits for different endpoints based on their resource intensity.

## Overview

**Rate Limiting Library:** AspNetCoreRateLimit v5.0.0

**Enforcement Level:** Per IP address (can be changed to per client ID or per user in future)

**Response:** HTTP 429 Too Many Requests when limit exceeded

## Rate Limit Policies

### Endpoint-Specific Limits

| Endpoint Pattern | Limit | Window | Reason |
|-----------------|-------|--------|---------|
| `POST /api/jobs` | 10 requests | 1 minute | Resource-intensive video rendering jobs |
| `POST /api/quick/demo` | 5 requests | 1 minute | Quick demo creation (heavy resource use) |
| `POST /api/script` | 20 requests | 1 minute | Script generation (LLM API calls) |
| `*` (catch-all) | 100 requests | 1 minute | General API endpoints |

### Whitelisted Endpoints

The following endpoints are **exempt** from rate limiting:

- `GET /health/*` - Health check endpoints (liveness and readiness probes)
- `GET /healthz` - Legacy health check endpoint
- `GET /api/health/*` - API health check endpoints

**Reason:** Monitoring systems need to check health frequently without being blocked.

## Rate Limit Headers

Every API response includes rate limit information in headers:

```http
HTTP/1.1 200 OK
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 7
X-RateLimit-Reset: 1698451260
```

**Headers:**
- `X-RateLimit-Limit` - Maximum requests allowed in the window
- `X-RateLimit-Remaining` - Requests remaining in current window
- `X-RateLimit-Reset` - Unix timestamp when the limit resets

## Rate Limit Exceeded Response

When you exceed the rate limit, the API returns:

**Status Code:** `429 Too Many Requests`

**Response Body:**
```json
{
  "type": "https://docs.aura.studio/errors/E429",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "Too many requests. Please retry after the specified time."
}
```

**Additional Headers:**
```http
HTTP/1.1 429 Too Many Requests
Retry-After: 42
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1698451260
```

**Retry-After header:** Number of seconds to wait before retrying

## Configuration

Rate limits are configured in `appsettings.json`:

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "HttpStatusCode": 429,
    "RealIpHeader": "X-Real-IP",
    "QuotaExceededResponse": {
      "Content": "{\"type\":\"https://docs.aura.studio/errors/E429\",\"title\":\"Rate Limit Exceeded\",\"status\":429,\"detail\":\"Too many requests. Please retry after the specified time.\"}",
      "ContentType": "application/json"
    },
    "IpWhitelist": [],
    "EndpointWhitelist": [
      "get:/health/*",
      "get:/healthz",
      "get:/api/health/*"
    ],
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/jobs",
        "Period": "1m",
        "Limit": 10
      },
      {
        "Endpoint": "POST:/api/quick/demo",
        "Period": "1m",
        "Limit": 5
      },
      {
        "Endpoint": "POST:/api/script",
        "Period": "1m",
        "Limit": 20
      },
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### Configuration Options

**EnableEndpointRateLimiting** (boolean)
- Enables different rate limits per endpoint
- Set to `true` for production

**StackBlockedRequests** (boolean)
- Whether to queue blocked requests
- Set to `false` - rejected requests don't consume quota

**HttpStatusCode** (integer)
- HTTP status code for rate-limited requests
- Standard: `429 Too Many Requests`

**RealIpHeader** (string)
- Header containing the real client IP (when behind a proxy)
- Common values: `X-Real-IP`, `X-Forwarded-For`

**IpWhitelist** (array)
- IP addresses exempt from rate limiting
- Example: `["127.0.0.1", "10.0.0.0/8"]`
- Empty by default

**EndpointWhitelist** (array)
- Endpoint patterns exempt from rate limiting
- Format: `METHOD:/path/pattern`
- Wildcards supported: `/health/*`

**GeneralRules** (array)
- Rate limit rules per endpoint
- Most specific match takes precedence
- Catch-all `*` rule for unmatched endpoints

## Client Best Practices

### 1. Monitor Rate Limit Headers

Always check `X-RateLimit-Remaining` to avoid hitting limits:

```javascript
const response = await fetch('/api/jobs', {
  method: 'POST',
  body: JSON.stringify(jobData)
});

const remaining = response.headers.get('X-RateLimit-Remaining');
if (remaining < 2) {
  console.warn('Approaching rate limit. Slow down requests.');
}
```

### 2. Handle 429 Responses Gracefully

Implement exponential backoff when rate-limited:

```javascript
async function createJobWithRetry(jobData, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    const response = await fetch('/api/jobs', {
      method: 'POST',
      body: JSON.stringify(jobData)
    });

    if (response.status === 429) {
      const retryAfter = response.headers.get('Retry-After');
      const waitTime = retryAfter ? parseInt(retryAfter) * 1000 : Math.pow(2, i) * 1000;
      
      console.log(`Rate limited. Retrying after ${waitTime}ms`);
      await new Promise(resolve => setTimeout(resolve, waitTime));
      continue;
    }

    return response;
  }

  throw new Error('Max retries exceeded');
}
```

### 3. Respect Retry-After Header

When rate-limited, wait for the duration specified in `Retry-After`:

```javascript
if (response.status === 429) {
  const retryAfterSeconds = response.headers.get('Retry-After');
  await new Promise(resolve => 
    setTimeout(resolve, retryAfterSeconds * 1000)
  );
  // Retry request
}
```

### 4. Batch Requests When Possible

Instead of making many individual requests, batch operations:

```javascript
// ❌ Bad: 10 separate requests
for (const script of scripts) {
  await fetch('/api/script', { method: 'POST', body: script });
}

// ✅ Good: 1 batch request (if API supports it)
await fetch('/api/scripts/batch', {
  method: 'POST',
  body: JSON.stringify({ scripts })
});
```

### 5. Use Polling with Backoff

When polling for job status, use exponential backoff:

```javascript
async function pollJobStatus(jobId) {
  let delay = 1000; // Start with 1 second
  const maxDelay = 30000; // Max 30 seconds

  while (true) {
    const response = await fetch(`/api/jobs/${jobId}`);
    const job = await response.json();

    if (job.status === 'done' || job.status === 'failed') {
      return job;
    }

    await new Promise(resolve => setTimeout(resolve, delay));
    delay = Math.min(delay * 1.5, maxDelay); // Exponential backoff
  }
}
```

## Requesting Rate Limit Increases

If you consistently hit rate limits for legitimate use cases:

1. **Review your implementation** - Ensure you're following best practices above
2. **Identify your use case** - Document why you need higher limits
3. **Contact support** - Request a rate limit increase with justification

**Self-hosted deployments:** You can modify rate limits in `appsettings.json`

## Monitoring Rate Limits

### Application Logs

Rate limit violations are logged:

```log
[12:34:56 WRN] Rate limit exceeded for client 192.168.1.100 on path /api/jobs. Count: 11/10
```

### Metrics

Track rate limit metrics:
- Number of 429 responses per endpoint
- Clients hitting rate limits frequently
- Rate limit efficiency (blocked vs allowed requests)

### Alerts

Set up alerts for:
- Spike in 429 responses (possible attack or bug)
- Same IP hitting rate limits repeatedly
- Rate limit rules that are never triggered (too lenient)

## Security Considerations

### DDoS Protection

Rate limiting helps protect against:
- **Application-layer DDoS** - Overwhelming the server with requests
- **Resource exhaustion** - Expensive operations (video rendering, AI generation)
- **API abuse** - Scraping, credential stuffing, brute force

### Additional Protection Layers

Rate limiting is one layer. Also implement:
- **Firewall rules** - Block known malicious IPs at network level
- **CDN/WAF** - Use CloudFlare, AWS WAF, or similar for additional protection
- **Authentication** - Require API keys for sensitive endpoints
- **Input validation** - Prevent injection attacks
- **Request size limits** - Limit payload size to prevent memory exhaustion

## Troubleshooting

### "Getting 429 errors for health checks"

**Problem:** Monitoring system is rate-limited

**Solution:** Health check endpoints (`/health/*`, `/healthz`) are whitelisted. Ensure you're using the correct endpoint path.

### "Rate limit resets at unexpected times"

**Problem:** Rate limit window doesn't align with clock time

**Solution:** Rate limits use sliding windows, not fixed time periods. The window starts from your first request and runs for the specified period (e.g., 1 minute).

### "Different clients getting same rate limit"

**Problem:** Multiple clients behind same NAT/proxy share rate limit

**Solution:** 
1. Configure proxy to forward client IP in `X-Real-IP` or `X-Forwarded-For` header
2. Update `RealIpHeader` in configuration
3. For internal apps, whitelist proxy IP

### "Need temporary rate limit increase"

**Problem:** One-time bulk operation needs more quota

**Solution:**
1. **Self-hosted:** Temporarily adjust limits in `appsettings.json` and restart
2. **Cloud:** Whitelist your IP in `IpWhitelist` configuration
3. **Best practice:** Batch operations and spread over time

## Rate Limiting Behavior

### Sliding Window

Rate limits use a **sliding window** algorithm:

```
Example: 10 requests per minute

Time:    0s -------- 30s -------- 60s -------- 90s
Requests: 5           5            -            -

At 60s: Last minute had 10 requests (full quota used)
At 90s: Last minute had 5 requests (quota partially reset)
```

**Benefit:** Prevents burst at window boundaries

### Request Stacking

`StackBlockedRequests: false` means:
- Rejected requests (429) don't count toward quota
- You can retry immediately after reset without penalty

### Endpoint Matching

Most specific rule wins:

```json
"GeneralRules": [
  { "Endpoint": "POST:/api/jobs", "Limit": 10 },
  { "Endpoint": "POST:/api/*", "Limit": 50 },
  { "Endpoint": "*", "Limit": 100 }
]
```

Request to `POST /api/jobs` → Uses 10/min limit (most specific)
Request to `POST /api/script` → Uses 50/min limit (matches pattern)
Request to `GET /api/health` → Uses 100/min limit (catch-all)

## Future Enhancements

Planned improvements:
- **Per-user rate limiting** - Different limits for authenticated users
- **Tiered rate limits** - Premium users get higher limits  
- **Dynamic rate limiting** - Adjust based on server load
- **Rate limit quotas** - Daily/monthly quotas in addition to per-minute
- **Burst allowance** - Allow short bursts above limit

## Related Documentation

- [Health Check Endpoints](health.md) - API health monitoring
- [API Contract](API_CONTRACT_V1.md) - Full API specification
- [Error Handling](errors.md) - Error response formats