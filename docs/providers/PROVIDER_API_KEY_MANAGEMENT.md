# Provider API Key Management and Validation

## Overview

Aura Video Studio provides secure API key storage and validation for multiple cloud service providers. This document describes the provider management system including secure storage, validation endpoints, and fallback strategies.

## Features

### Secure Key Storage
- **Windows**: Uses Windows Data Protection API (DPAPI) for encryption
- **Linux/macOS**: Uses AES-256 encryption with machine-specific keys
- **Storage Location**: `%LOCALAPPDATA%\Aura\secure\apikeys.dat` (Windows) or `~/.local/share/Aura/secure/apikeys.dat` (Linux/macOS)
- **Permissions**: Files are automatically restricted to owner-only access (600 on Unix)

### Supported Providers

#### LLM Providers (Script Generation)
- **OpenAI** - GPT-4, GPT-3.5 models
- **Anthropic** - Claude models
- **Google Gemini** - Gemini models
- **Azure OpenAI** - Microsoft Azure hosted OpenAI
- **Ollama** - Local AI models (free, offline)
- **RuleBased** - Template-based generation (free, always available, offline)

#### Text-to-Speech Providers
- **ElevenLabs** - Premium voice synthesis
- **PlayHT** - Cloud voice synthesis with cloning
- **Windows SAPI** - Built-in Windows TTS (free, always available)
- **Piper** - Local neural TTS (free, offline)
- **Mimic3** - Local neural TTS (free, offline)

#### Image Providers
- **Stability AI** - AI image generation
- **Pexels** - Free stock images
- **Pixabay** - Free stock images
- **Unsplash** - High-quality stock images
- **Local SD** - Local Stable Diffusion (free, requires GPU)

## API Endpoints

### Validation Endpoints

All validation endpoints perform live network checks to verify API keys work correctly.

#### POST /api/providers/openai/validate
Validates OpenAI API key with network check.

**Request**:
```json
{
  "apiKey": "sk-...",
  "baseUrl": null,
  "organizationId": null,
  "projectId": null
}
```

**Response**:
```json
{
  "isValid": true,
  "status": "Valid",
  "message": "OpenAI API key is valid and working",
  "correlationId": "abc123",
  "details": {
    "provider": "OpenAI",
    "keyFormat": "valid",
    "formatValid": true,
    "networkCheckPassed": true,
    "httpStatusCode": 200,
    "responseTimeMs": 150
  }
}
```

#### POST /api/providers/elevenlabs/validate
Validates ElevenLabs API key.

**Request**:
```json
{
  "apiKey": "your-elevenlabs-api-key"
}
```

**Response**: Same format as OpenAI validation

#### POST /api/providers/playht/validate
Validates PlayHT API key.

**Request**:
```json
{
  "apiKey": "your-playht-api-key"
}
```

**Response**: Same format as OpenAI validation

### Status Endpoint

#### GET /api/providers/status
Returns the configuration status of all providers.

**Response**:
```json
[
  {
    "name": "OpenAI",
    "isConfigured": true,
    "isAvailable": true,
    "status": "Configured",
    "lastValidated": "2025-01-15T10:30:00Z"
  },
  {
    "name": "ElevenLabs",
    "isConfigured": false,
    "isAvailable": false,
    "status": "Not Configured"
  },
  {
    "name": "RuleBased",
    "isConfigured": true,
    "isAvailable": true,
    "status": "Always Available (Offline)"
  }
]
```

## Frontend Integration

### Provider Status Dashboard

The Provider Status Dashboard component (`ProviderStatusDashboard.tsx`) displays:
- Visual grid of all providers
- Configuration status (green checkmark = configured, gray dismiss icon = not configured)
- Last validation timestamp
- Refresh button to reload status

**Usage in Settings**:
```tsx
import { ProviderStatusDashboard } from './ProviderStatusDashboard';

// In your settings component:
<ProviderStatusDashboard />
```

### State Management

Provider state is managed using Zustand in `src/state/providers.ts`:

```typescript
import { useProviderStore } from '@/state/providers';

// In your component:
const { providerStatuses, refreshProviderStatuses } = useProviderStore();

// Refresh provider statuses:
await refreshProviderStatuses();
```

## Provider Fallback Strategy

When premium providers are unavailable, the system automatically falls back to free alternatives:

### LLM Fallback Chain
1. **OpenAI/Anthropic/Gemini** (premium, requires API key)
2. **Ollama** (free, local, requires installation)
3. **RuleBased** (free, always available, template-based)

### TTS Fallback Chain
1. **ElevenLabs/PlayHT** (premium, requires API key)
2. **Piper/Mimic3** (free, local, requires installation)
3. **Windows SAPI** (free, always available on Windows)

### Image Fallback Chain
1. **Stability AI** (premium, requires API key)
2. **Local SD** (free, requires GPU with 6GB+ VRAM)
3. **Stock Images** (free, always available)

## Security Considerations

### API Key Storage
- Keys are encrypted before storage using platform-specific encryption
- Windows: DPAPI with CurrentUser scope
- Linux/macOS: AES-256 with machine-specific key
- File permissions restricted to owner only
- Keys never logged or exposed in error messages

### Key Validation
- Network validation uses minimal API calls
- Extended timeout of 90 seconds for patience-centric validation
- Automatic retry with exponential backoff for transient failures (up to 2 retries)
- Proxy support: automatically detects Windows proxy settings and HTTP_PROXY/HTTPS_PROXY environment variables
- Comprehensive error categorization (DNS, TLS, proxy, timeout, rate limit, etc.)
- Secure handling of validation errors
- **Patience Strategy**: Validation failures don't block provider usage - users can "Continue Anyway"

## Patience Strategy for Validation

Aura Video Studio implements a **patience-centric validation approach** that never blocks users due to network or validation issues:

### Core Principles

1. **Non-Blocking**: Validation failures (timeout, network errors, etc.) never prevent saving or using a provider
2. **Deferred Validation**: If validation cannot complete, the key is saved and validated on first actual use
3. **Continue Anyway**: All transient failures show a "Continue Anyway" option
4. **Clear Categorization**: Errors are categorized clearly (DNS, TLS, proxy, timeout, etc.) with actionable guidance

### Validation States

- **Valid** ✓: Key verified successfully with provider
- **Invalid** ✕: Key definitively invalid (401 Unauthorized) - requires correction
- **Rate Limited** ⚠: Key valid but temporarily rate limited - can continue
- **Offline** 🌐: No internet connection - can continue in offline mode
- **Timeout** ⏱: Request timed out (90s) - can continue with deferred validation
- **Network Error** 🔌: Network issue (DNS, TLS, proxy) - can continue anyway
- **Service Issue** 🛠: Provider service problem (5xx) - can continue and retry later
- **Permission Denied** 🚫: Valid key but insufficient permissions - check account

### Error Categorization

The validation system categorizes errors to provide specific troubleshooting guidance:

- **DNS_Error**: Unable to resolve hostname - check internet connection or DNS settings
- **TLS_Error**: Certificate/TLS handshake failure - may be proxy or certificate issue
- **Proxy_Error**: Proxy connection failed - check HTTP_PROXY environment variable or Windows proxy settings
- **Connection_Timeout**: Network latency - retry or continue with deferred validation
- **Connection_Refused**: Firewall or network blocking - check firewall settings
- **Network_Unreachable**: No internet - can work in offline mode
- **Offline**: Network connectivity check failed - offline mode available

### Proxy Support

Automatic proxy detection and configuration:
- **Windows**: Uses system proxy settings automatically
- **Environment Variables**: Respects HTTP_PROXY and HTTPS_PROXY
- **Authentication**: Uses default credentials (Windows) for authenticated proxies
- **Diagnostics**: Proxy errors are detected and reported with specific guidance

### Retry Strategy

- **Initial attempt**: 90-second timeout
- **First retry**: After 1 second delay
- **Second retry**: After 2 second delay (exponential backoff)
- **Service errors (5xx)**: Automatic retry
- **Client errors (4xx)**: No retry (except rate limit which allows save)
- **Network errors**: Retry if transient, fail fast if permanent (DNS, TLS)

### Best Practices
1. Never commit API keys to source control
2. Store keys in secure storage service, not configuration files
3. Test keys regularly using validation endpoints
4. Rotate keys periodically for security
5. Use fallback providers when possible to reduce costs
6. **NEW**: If validation fails, you can save and continue - the key will be validated on first use
7. **NEW**: Check diagnostic information in validation errors for specific troubleshooting steps

## Troubleshooting

### Key Validation Fails

**Symptoms**: "Could not validate API key" or network error messages

**Solutions**:
1. **Timeout or Network Error**: Click "Continue Anyway" - validation will happen on first use
2. **Check internet connectivity**:
   - Try accessing https://api.openai.com in your browser
   - Check if other internet-dependent apps work
3. **Proxy Configuration**:
   - Windows: Check Settings > Network & Internet > Proxy
   - Set HTTP_PROXY environment variable: `export HTTP_PROXY=http://proxy.example.com:8080`
   - For authenticated proxies, credentials are used automatically on Windows
4. **DNS Issues**:
   - Try `nslookup api.openai.com` to verify DNS resolution
   - Try alternate DNS servers (8.8.8.8, 1.1.1.1)
5. **TLS/Certificate Issues**:
   - Update system root certificates
   - Check corporate proxy doesn't intercept HTTPS
6. **Firewall/Security Software**:
   - Check if api.openai.com is blocked
   - Temporarily disable VPN to test
7. **Rate Limiting**: Wait 60 seconds and retry, or save and continue anyway
8. **Verify API key**:
   - Check it's copied completely including prefix (sk-, sk-proj-, etc.)
   - Generate new key if needed
9. **Check provider service status**: Visit status pages (status.openai.com, etc.)
10. **Review diagnostic information**: Validation errors include specific category and guidance

### Offline Mode

If you have no internet connection:
1. Validation will detect offline state automatically
2. Click "Continue in Offline Mode"
3. Key will be saved and validated when internet is available
4. Local providers (RuleBased LLM, Windows SAPI TTS, Stock Images) work offline

### Storage Errors
- Check file system permissions
- Verify %LOCALAPPDATA% or ~/.local exists
- Check disk space availability
- Review security software blocking file access

### Provider Not Available
- Check if API key is configured
- Verify provider requirements (GPU, OS, etc.)
- Check if provider service is running (for local providers)
- Review error messages in provider status dashboard

## Implementation Notes

The provider API key management system leverages several existing services:
- `ISecureStorageService` - Handles encrypted key storage (DPAPI/AES-256)
- `IKeyValidationService` - Performs network validation tests
- `KeyVaultController` - (deprecated in favor of ProvidersController)
- `ProvidersController` - Main API controller for provider operations

All services are registered in dependency injection and are available throughout the application.
