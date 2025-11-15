# Error Taxonomy

This document describes the standardized error codes used throughout Aura Video Studio, along with their meanings, typical causes, and recommended remediation steps.

## Error Code Format

Error codes follow the pattern: `ErrorType[:Details]`

Examples:
- `MissingApiKey:STABLE_KEY`
- `FFmpegFailedExitCode:1`
- `StepTimeout:narration`

---

## Error Codes

### MissingApiKey:{KEY}

**Message:** Required API key '{KEY}' is not configured.

**Typical Causes:**
- API key not set in settings
- Environment variable not configured
- Key was deleted or revoked

**Remediation:**
1. Open Settings → Providers
2. Locate the provider requiring the key
3. Click "Add API Key" and enter your key
4. Save and retry

**Example:**
```json
{
  "code": "MissingApiKey:STABLE_KEY",
  "message": "Required API key 'STABLE_KEY' is not configured.",
  "remediation": "Add STABLE_KEY in Settings → Providers"
}
```

---

### Provider Validation Errors

These error codes are returned by the provider validation system (`/api/providers/{name}/validate-detailed` endpoint) when checking provider connectivity and configuration.

#### ProviderNotConfigured

**Message:** Provider is not configured. API key or required configuration is missing.

**HTTP Status:** 200 (returned in validation response, not as HTTP error)

**Typical Causes:**
- API key not set in application settings
- Environment variable for provider not configured
- Configuration was deleted or cleared

**Remediation:**
1. Go to Settings → Providers in the UI
2. Find the provider showing "Not Configured"
3. Click "Configure" or "Add API Key"
4. Enter your API key from the provider's dashboard
5. Save and click "Retry Validation"

**Response Example:**
```json
{
  "name": "OpenAI",
  "configured": false,
  "reachable": false,
  "errorCode": "ProviderNotConfigured",
  "errorMessage": "OpenAI API key is not configured",
  "howToFix": [
    "Go to Settings → Providers",
    "Add your OpenAI API key",
    "Get an API key from https://platform.openai.com/api-keys"
  ],
  "category": "LLM",
  "tier": "Premium"
}
```

---

#### ProviderKeyInvalid

**Message:** Provider API key is invalid or expired.

**HTTP Status:** 200 (validation failure)

**Typical Causes:**
- API key contains typos
- API key has been revoked by provider
- API key has expired
- Wrong key format (e.g., missing 'sk-' prefix for OpenAI)
- Account lacks required permissions

**Remediation:**
1. Check API key format (OpenAI: `sk-...`, Anthropic: `sk-ant-...`)
2. Copy key directly from provider dashboard to avoid typos
3. Regenerate API key if it may have been compromised
4. Verify account has required permissions/tier
5. Test key manually with curl (see examples in [provider-errors.md](../troubleshooting/provider-errors.md))

**Response Example:**
```json
{
  "name": "OpenAI",
  "configured": true,
  "reachable": false,
  "errorCode": "ProviderKeyInvalid",
  "errorMessage": "OpenAI API key is invalid or expired",
  "howToFix": [
    "Check your API key for typos",
    "Verify the key is still valid at https://platform.openai.com/api-keys",
    "Generate a new API key if needed"
  ]
}
```

---

#### ProviderNetworkError

**Message:** Cannot connect to provider. Network error, DNS failure, or connection timeout.

**HTTP Status:** 200 (validation failure)

**Typical Causes:**
- No internet connection
- Firewall blocking outbound HTTPS connections
- DNS resolution failure
- Provider endpoint unreachable
- Connection timeout (>5 seconds)
- Proxy configuration issues

**Remediation:**
1. Check internet connectivity: `ping 8.8.8.8`
2. Test DNS: `nslookup api.openai.com`
3. Check firewall allows outbound HTTPS (port 443)
4. Test provider endpoint manually: `curl -v https://api.openai.com/v1/models`
5. Try different network (mobile hotspot, different WiFi)
6. Check proxy settings if behind corporate firewall
7. Verify DNS servers are working (try 8.8.8.8, 1.1.1.1)

**Response Example:**
```json
{
  "name": "OpenAI",
  "configured": true,
  "reachable": false,
  "errorCode": "ProviderNetworkError",
  "errorMessage": "Connection to OpenAI timed out",
  "howToFix": [
    "Check your internet connection",
    "Verify firewall settings",
    "Try again later"
  ]
}
```

---

#### ProviderRateLimited

**Message:** Provider rate limit exceeded. Too many requests in given time period.

**HTTP Status:** 200 (provider is reachable but limiting requests)

**Typical Causes:**
- Exceeded requests per minute/hour limit
- Exceeded monthly token quota
- Account tier has low rate limits
- Multiple Aura instances using same API key
- Validation check triggered while generation in progress

**Remediation:**
1. Wait before retrying (typically 1 minute to 1 hour)
2. Check usage dashboard:
   - OpenAI: https://platform.openai.com/usage
   - Anthropic: https://console.anthropic.com/
   - ElevenLabs: https://elevenlabs.io/app/usage
3. Upgrade account tier for higher limits
4. Configure provider fallback chains
5. Avoid rapid validation retries

**Rate Limit Examples:**
- OpenAI Free: 3 requests/min
- OpenAI Tier 1: 500 requests/day
- ElevenLabs Free: 10,000 characters/month

**Response Example:**
```json
{
  "name": "OpenAI",
  "configured": true,
  "reachable": true,
  "errorCode": "ProviderRateLimited",
  "errorMessage": "OpenAI rate limit exceeded",
  "howToFix": [
    "Wait a few minutes before trying again",
    "Check your OpenAI usage dashboard",
    "Consider upgrading your OpenAI plan for higher limits"
  ]
}
```

---

#### ProviderServerError

**Message:** Provider service is experiencing server-side issues (HTTP 5xx).

**HTTP Status:** 200 (validation failure)

**Typical Causes:**
- Provider experiencing outage or maintenance
- Temporary server overload
- Internal provider bug
- Regional service degradation

**Remediation:**
1. Check provider status pages:
   - OpenAI: https://status.openai.com/
   - Anthropic: https://status.anthropic.com/
2. Wait 5-15 minutes and retry
3. Use fallback provider temporarily
4. Check community forums for outage reports
5. Contact provider support if issue persists >1 hour

**Response Example:**
```json
{
  "name": "OpenAI",
  "configured": true,
  "reachable": false,
  "errorCode": "ProviderServerError",
  "errorMessage": "OpenAI service is experiencing issues",
  "howToFix": [
    "Check OpenAI status at https://status.openai.com",
    "Try again in a few minutes",
    "Use a fallback provider temporarily"
  ]
}
```

---

#### ValidationError

**Message:** An unexpected error occurred during provider validation.

**HTTP Status:** 200

**Typical Causes:**
- Unexpected exception in validation logic
- Malformed provider response
- Internal service error

**Remediation:**
1. Check application logs for details
2. Try again later
3. If issue persists, report bug with correlation ID

---

### RequiresNvidiaGPU

**Message:** This operation requires an NVIDIA GPU with CUDA support.

**Typical Causes:**
- No NVIDIA GPU detected
- CUDA drivers not installed
- GPU not compatible with required CUDA version

**Remediation:**
1. Install NVIDIA GPU drivers
2. OR: Use CPU-based encoding (Settings → Rendering → Codec → H.264 Software)
3. OR: Use a different provider that doesn't require GPU

---

### UnsupportedOS:{OS}

**Message:** This feature is not supported on {OS}.

**Typical Causes:**
- Feature only available on Windows/Linux/macOS
- Platform-specific API not available
- Provider not compatible with OS

**Remediation:**
1. Use an alternative provider
2. Use the portable version on a supported platform
3. Check documentation for OS-specific requirements

---

### FFmpegNotFound

**Message:** FFmpeg executable not found. Please install FFmpeg to continue.

**Typical Causes:**
- FFmpeg not installed
- FFmpeg not in system PATH
- Permissions issue preventing access to FFmpeg

**Remediation:**
1. **Windows:** `choco install ffmpeg` or download from ffmpeg.org
2. **macOS:** `brew install ffmpeg`
3. **Linux:** `sudo apt install ffmpeg` or equivalent
4. Verify installation: `ffmpeg -version`
5. Restart Aura Video Studio

**See Also:** [PORTABLE_FIRST_RUN.md](../user-guide/PORTABLE_FIRST_RUN.md#2-install-ffmpeg)

---

### FFmpegFailedExitCode:{N}

**Message:** FFmpeg process failed with exit code {N}.

**Typical Causes:**
- Invalid input file
- Unsupported codec or format
- Insufficient permissions
- Out of disk space
- Hardware encoding failure

**Remediation:**
1. Check the technical details for FFmpeg stderr output
2. Verify input files are valid and readable
3. Try software encoding instead of hardware
4. Ensure sufficient disk space
5. Check output directory permissions

**Common Exit Codes:**
- `1`: Generic error (check stderr)
- `134`: Segmentation fault (try different codec)
- `255`: Hardware encoding failure (switch to software)

---

### OutOfDiskSpace

**Message:** Insufficient disk space to complete the operation.

**Typical Causes:**
- Disk full or nearly full
- Large temporary files
- Output directory on small partition

**Remediation:**
1. Free up disk space (at least 5GB recommended)
2. Delete old artifacts from output directory
3. Change output directory to a drive with more space
4. Clear temporary files

---

### OutputDirectoryNotWritable

**Message:** Cannot write to output directory. Please check permissions.

**Typical Causes:**
- Directory is read-only
- Insufficient user permissions
- Directory doesn't exist
- Network drive disconnected

**Remediation:**
1. Check directory permissions
2. Run Aura Video Studio with appropriate permissions
3. Change output directory to a writable location
4. Create the directory if it doesn't exist

---

### InvalidInput:{FIELD}

**Message:** Invalid input for '{FIELD}': {reason}

**Typical Causes:**
- Missing required field
- Invalid format or type
- Value out of acceptable range
- Conflicting parameters

**Remediation:**
1. Review the input requirements
2. Correct the field value
3. Check documentation for valid values
4. Try using a preset to see valid input

**Examples:**
- `InvalidInput:fps` - FPS must be positive number
- `InvalidInput:resolution` - Invalid resolution format
- `InvalidInput:codec` - Unsupported codec

---

### StepTimeout:{STEP}

**Message:** Step '{STEP}' timed out. The operation took too long to complete.

**Typical Causes:**
- Network request timeout (API calls)
- Long-running process exceeded limit
- System under heavy load
- Deadlock or infinite loop

**Remediation:**
1. Retry the operation
2. Check network connectivity (for API-based steps)
3. Check system resources (CPU, RAM)
4. Reduce video length or complexity
5. Contact support if timeout persists

**Default Timeouts:**
- `preflight`: 30 seconds
- `narration`: 5 minutes
- `broll`: 10 minutes
- `subtitles`: 2 minutes
- `mux`: 15 minutes

---

### TransientNetworkFailure

**Message:** Network request failed. Please check your internet connection.

**Typical Causes:**
- No internet connection
- API service temporarily unavailable
- Firewall blocking request
- DNS resolution failure
- Proxy configuration issue

**Remediation:**
1. Check internet connection
2. Verify API service status
3. Check firewall settings
4. Retry after a few seconds
5. Use local providers if available

**Auto-Retry:** This error triggers automatic retry with exponential backoff (max 2 retries).

---

### UnknownError

**Message:** (varies)

**Typical Causes:**
- Unexpected exception
- Unhandled edge case
- Internal bug

**Remediation:**
1. Check technical details for full exception
2. Copy correlation ID for support
3. Check logs in `AuraData/logs/`
4. Retry the operation
5. Report to GitHub issues with correlation ID

---

## Error Response Format

All errors follow this standard format:

```json
{
  "code": "ErrorCode:Details",
  "message": "Human-readable error message",
  "remediation": "Step-by-step fix instructions",
  "details": {
    "exception": "Full exception details",
    "step": "Step name where error occurred",
    "correlationId": "abc123...",
    "additionalContext": "..."
  }
}
```

---

## Viewing Errors

### In the UI

1. Errors appear in the Render Status Drawer
2. Each error shows:
   - Error message
   - Remediation button(s)
   - "Technical Details" accordion
3. Click "Copy" to copy error details for support

### In the Logs

Errors are logged to `AuraData/logs/aura-api-{date}.log`:

```
[ERR] [abc123...] Job J-xyz789... failed at step narration: 
      MissingApiKey:STABLE_KEY - Required API key 'STABLE_KEY' is not configured.
```

### Via API

GET `/api/jobs/{jobId}` includes all errors:

```json
{
  "jobId": "J-xyz789...",
  "status": "Failed",
  "errors": [
    {
      "code": "MissingApiKey:STABLE_KEY",
      "message": "Required API key 'STABLE_KEY' is not configured.",
      "remediation": "Add STABLE_KEY in Settings → Providers"
    }
  ]
}
```

---

## Correlation IDs

Every request and error includes a correlation ID for tracking:

- Appears in logs
- Included in all error responses
- Shown in UI toasts and error cards
- Used for support and diagnostics

**To use:**
1. Copy correlation ID from error message
2. Search logs: `grep "abc123..." AuraData/logs/*.log`
3. Include in support requests

---

## Prevention Best Practices

1. **Run System Check** before generating videos
   - Settings → System → Run Check
   - Validates dependencies, APIs, disk space

2. **Use "Try Sample"** to verify setup
   - Guaranteed to work with only FFmpeg
   - Tests entire pipeline
   - No API keys required

3. **Monitor Disk Space**
   - Keep at least 5GB free
   - Clean old artifacts regularly

4. **Keep Software Updated**
   - Update FFmpeg to latest version
   - Update GPU drivers
   - Update Aura Video Studio

5. **Configure Fallbacks**
   - Set backup TTS providers
   - Enable software encoding fallback
   - Configure local-first options

---

## Reporting Bugs

If you encounter an error that seems like a bug:

1. Copy the correlation ID
2. Copy the error details (JSON)
3. Check if issue already exists: https://github.com/Coffee285/aura-video-studio/issues
4. Create new issue with:
   - Correlation ID
   - Full error details
   - Steps to reproduce
   - Relevant logs from `AuraData/logs/`

---

## Related Documentation

- [Jobs API](./jobs.md) - API endpoints and event types
- [PORTABLE_FIRST_RUN.md](../user-guide/PORTABLE_FIRST_RUN.md) - Setup and troubleshooting
- System Check - Pre-flight validation
