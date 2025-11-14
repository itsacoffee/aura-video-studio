# Provider Capabilities API

## Overview

The Provider Capabilities API allows clients to query which providers are available on the current system based on hardware detection, API key presence, and operating system compatibility.

## Endpoint

### GET /api/providers/capabilities

Returns a list of all providers with their availability status and requirements.

**Response Format:**

```json
[
  {
    "name": "StableDiffusion",
    "available": false,
    "reasonCodes": ["RequiresNvidiaGPU", "MissingApiKey:STABLE_KEY"],
    "requirements": {
      "needsKey": ["STABLE_KEY"],
      "needsGPU": "nvidia",
      "minVRAMMB": 6144,
      "os": ["windows", "linux"]
    }
  }
]
```

## Response Fields

### ProviderCapability

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Provider name (e.g., "StableDiffusion") |
| `available` | boolean | Whether the provider is currently available for use |
| `reasonCodes` | string[] | List of reasons why the provider is unavailable (empty if available) |
| `requirements` | ProviderRequirements | Provider requirements for availability |

### ProviderRequirements

| Field | Type | Description |
|-------|------|-------------|
| `needsKey` | string[] | API keys required for this provider |
| `needsGPU` | string? | GPU vendor required (e.g., "nvidia", "amd", null for any) |
| `minVRAMMB` | int? | Minimum VRAM in megabytes (null if not applicable) |
| `os` | string[] | Supported operating systems ("windows", "linux", "macos") |

## Reason Codes

The `reasonCodes` array contains human-readable codes explaining why a provider is unavailable:

| Reason Code | Description | Remediation |
|-------------|-------------|-------------|
| `RequiresNvidiaGPU` | Provider requires an NVIDIA GPU | Install an NVIDIA GPU or use alternative providers |
| `MissingApiKey:STABLE_KEY` | The STABLE_KEY API key is not configured | Add API key in Settings |
| `InsufficientVRAM` | GPU does not meet minimum VRAM requirement | Use a GPU with more VRAM or reduce quality settings |
| `UnsupportedOS` | Operating system is not supported | Use a supported OS or alternative provider |

## Detection Logic

### Hardware Detection

The API automatically detects:
- **GPU**: Vendor, model, and VRAM
- **CPU**: Core count and capabilities
- **RAM**: Total available memory
- **OS**: Current operating system

### API Key Detection

API keys are checked from the secure key store. Keys can be configured via:
- Settings UI in Aura.Web
- Direct API calls to `/api/apikeys/save`

### Availability Calculation

A provider is marked as `available: true` only when ALL requirements are met:
1. Required API keys are present
2. GPU requirements are satisfied (if applicable)
3. VRAM requirements are met (if applicable)
4. Operating system is supported

## Example Use Cases

### Frontend Gating

Use capabilities to disable UI elements for unavailable providers:

```typescript
const capabilities = await fetch('/api/providers/capabilities').then(r => r.json());
const sd = capabilities.find(c => c.name === 'StableDiffusion');

if (!sd.available) {
  // Disable Stable Diffusion options
  // Show tooltip with reason codes
  console.log('Stable Diffusion unavailable:', sd.reasonCodes);
}
```

### Provider Selection

Automatically fallback to available providers:

```typescript
const capabilities = await fetch('/api/providers/capabilities').then(r => r.json());
const availableImageProviders = capabilities
  .filter(c => c.available && c.category === 'image')
  .map(c => c.name);

// Use first available provider
const selectedProvider = availableImageProviders[0] || 'local';
```

## Contract Stability

The `/api/providers/capabilities` endpoint follows semantic versioning:
- **Field additions** are non-breaking changes
- **Field removal or type changes** require a new API version
- **Reason codes** may be added but never removed in a version

Current contract version: **v1.0.0**

## See Also

- Hardware Detection
- [API Keys Management](../setup/api-keys.md)
- Provider Fallback Logic
