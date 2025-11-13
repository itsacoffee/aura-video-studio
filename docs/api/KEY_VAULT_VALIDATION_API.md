# Key Vault API - Validation Status Endpoints

This document describes the new validation status endpoints added to the Key Vault API for provider API key lifecycle management.

## Base URL

```
/api/keys
```

## Endpoints

### Get All Keys Status

Retrieves validation status for all configured API keys.

**Endpoint**: `GET /api/keys/status`

**Authentication**: None (planned for future)

**Response**: `200 OK`

```json
{
  "success": true,
  "statuses": {
    "openai": {
      "success": true,
      "provider": "openai",
      "status": "Valid",
      "message": "API key is valid and working",
      "lastValidated": "2025-11-13T21:30:00Z",
      "validationStarted": null,
      "elapsedMs": 0,
      "remainingTimeoutMs": 0,
      "details": {
        "provider": "openai",
        "status": "connected"
      },
      "canRetry": false,
      "canManuallyRevalidate": true
    },
    "ollama": {
      "success": true,
      "provider": "ollama",
      "status": "ValidatingExtended",
      "message": "Validation in progress, taking longer than usual (45000ms elapsed).",
      "validationStarted": "2025-11-13T21:29:15Z",
      "elapsedMs": 45000,
      "remainingTimeoutMs": 135000,
      "details": {},
      "canRetry": true,
      "canManuallyRevalidate": false
    }
  },
  "totalKeys": 2,
  "validKeys": 1,
  "invalidKeys": 0,
  "pendingValidation": 1
}
```

**Status Codes**:
- `200 OK` - Success
- `500 Internal Server Error` - Server error

---

### Get Specific Key Status

Retrieves validation status for a specific provider's API key.

**Endpoint**: `GET /api/keys/status/{provider}`

**Path Parameters**:
- `provider` (string, required) - Provider name (e.g., "openai", "anthropic")

**Response**: `200 OK`

```json
{
  "success": true,
  "provider": "openai",
  "status": "Valid",
  "message": "API key is valid and working",
  "lastValidated": "2025-11-13T21:30:00Z",
  "validationStarted": null,
  "elapsedMs": 0,
  "remainingTimeoutMs": 0,
  "details": {
    "provider": "openai",
    "status": "connected"
  },
  "canRetry": false,
  "canManuallyRevalidate": true
}
```

**Error Response**: `404 Not Found`

```json
{
  "success": false,
  "message": "No API key configured for anthropic"
}
```

**Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - Missing or invalid provider name
- `404 Not Found` - No key configured for provider
- `500 Internal Server Error` - Server error

---

### Manually Revalidate API Key

Triggers manual revalidation of an API key. This is a user-initiated action that bypasses caching and forces a fresh validation.

**Endpoint**: `POST /api/keys/revalidate`

**Request Body**:

```json
{
  "provider": "openai",
  "apiKey": null
}
```

**Request Fields**:
- `provider` (string, required) - Provider name to revalidate
- `apiKey` (string, optional) - API key to test. If null/empty, uses stored key

**Response**: `200 OK`

```json
{
  "success": true,
  "provider": "openai",
  "status": "Valid",
  "message": "OpenAI API key is valid and working",
  "lastValidated": "2025-11-13T21:35:00Z",
  "details": {
    "provider": "openai",
    "status": "connected"
  },
  "canManuallyRevalidate": true
}
```

**Error Response**: `404 Not Found`

```json
{
  "success": false,
  "message": "No API key configured for ollama"
}
```

**Status Codes**:
- `200 OK` - Validation completed (check `success` field for result)
- `400 Bad Request` - Missing or invalid provider name
- `404 Not Found` - No key configured and no key provided
- `500 Internal Server Error` - Server error

**Audit Logging**: All revalidation requests are logged with:
- Provider name (sanitized)
- User identifier
- Timestamp
- Result (Valid/Invalid)
- Correlation ID

---

## Validation Status Values

### Status Enum

| Status | Description | Meaning |
|--------|-------------|---------|
| `NotValidated` | Key not yet tested | Initial state after configuration |
| `Validating` | Currently validating (normal) | Within normal timeout threshold |
| `ValidatingExtended` | Taking longer than usual | Between normal and extended threshold |
| `ValidatingMaxWait` | Approaching maximum wait | Between extended and max threshold |
| `Valid` | Validation succeeded | Key is working correctly |
| `Invalid` | Validation failed | Key is incorrect or provider error |
| `SlowButWorking` | Provider slow but valid | Key works but high latency detected |
| `TimedOut` | Exceeded maximum wait | Provider unresponsive or unreachable |

### Status Details Object

Contains provider-specific validation details:

```json
{
  "provider": "openai",
  "status": "connected",
  "reason": "success",
  "status_code": "200"
}
```

Common detail fields:
- `provider` - Provider identifier
- `status` - Connection status
- `reason` - Reason for status (success, timeout, error, unauthorized)
- `status_code` - HTTP status code (if applicable)
- `error` - Error type (network, timeout, authentication)

---

## Validation Timeouts

Timeouts vary by provider category (configured in `providerTimeoutProfiles.json`):

| Provider Category | Normal | Extended | Maximum |
|-------------------|--------|----------|---------|
| Cloud LLM | 15s | 60s | 120s |
| Local LLM (Ollama) | 30s | 180s | 300s |
| TTS Services | 20s | 60s | 120s |
| Image Generation | 60s | 240s | 480s |
| Video Rendering | 120s | 600s | 1800s |
| Fallback | 5s | 15s | 30s |

**Progressive Validation**:
1. Initial attempt within normal timeout
2. If fails, retry with exponential backoff
3. Continue up to max retries (typically 3)
4. Status updates progressively based on elapsed time
5. No auto-disable if provider is slow

---

## Integration Examples

### TypeScript (React)

```typescript
import { apiClient } from '@/services/api/apiClient';

// Get all key statuses
const getAllStatuses = async () => {
  try {
    const response = await apiClient.get('/api/keys/status');
    return response.data;
  } catch (error) {
    console.error('Failed to fetch key statuses:', error);
    throw error;
  }
};

// Get specific key status
const getKeyStatus = async (provider: string) => {
  try {
    const response = await apiClient.get(`/api/keys/status/${provider}`);
    return response.data;
  } catch (error) {
    console.error(`Failed to fetch status for ${provider}:`, error);
    throw error;
  }
};

// Manually revalidate
const revalidateKey = async (provider: string, apiKey?: string) => {
  try {
    const response = await apiClient.post('/api/keys/revalidate', {
      provider,
      apiKey: apiKey || null,
    });
    return response.data;
  } catch (error) {
    console.error(`Failed to revalidate ${provider}:`, error);
    throw error;
  }
};
```

### C# (.NET)

```csharp
using System.Net.Http;
using System.Text.Json;

public class KeyVaultClient
{
    private readonly HttpClient _httpClient;
    
    public KeyVaultClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<AllKeysStatusResponse> GetAllStatusesAsync()
    {
        var response = await _httpClient.GetAsync("/api/keys/status");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AllKeysStatusResponse>(json);
    }
    
    public async Task<KeyStatusResponse> RevalidateKeyAsync(string provider)
    {
        var request = new { provider };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");
            
        var response = await _httpClient.PostAsync("/api/keys/revalidate", content);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KeyStatusResponse>(json);
    }
}
```

---

## Best Practices

### Polling Strategy

When validating keys with long timeouts:

1. **Initial call**: Start validation with POST `/api/keys/revalidate`
2. **Poll for status**: GET `/api/keys/status/{provider}` every 5 seconds
3. **Check status**: Look for terminal states (Valid, Invalid, TimedOut)
4. **Stop polling**: When status is terminal or user cancels
5. **Max duration**: Don't poll longer than maximum timeout + buffer

### Rate Limiting

- **Revalidation**: Limit to once per 30 seconds per provider
- **Status checks**: Safe to poll frequently (no external API calls)
- **Batch operations**: Use GET `/api/keys/status` for multiple providers

### Error Handling

Always handle these scenarios:
- Network timeouts (longer than UI expects)
- Provider outages (persistent failures)
- Invalid keys (immediate failure)
- Rate limits (429 responses from providers)

### User Experience

- Show progress indicators for extended validations
- Display timeout countdown for max wait states
- Provide clear guidance on what user should do
- Never auto-disable slow providers without user confirmation

---

## Related Documentation

- [Validation Patience Strategy](./VALIDATION_PATIENCE_STRATEGY.md)
- [Provider Integration Guide](./PROVIDER_INTEGRATION_GUIDE.md)
- [Security Best Practices](./SECURITY.md)
