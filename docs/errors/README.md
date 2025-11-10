# Error Code Reference

This document provides a comprehensive reference of all error codes used in Aura Video Studio, their meanings, and links to detailed troubleshooting guides.

## Quick Navigation

- [Error Code Categories](#error-code-categories)
- [Provider Errors (E100-E400)](#provider-errors-e100-e400)
- [Rendering Errors (E500)](#rendering-errors-e500)
- [Validation Errors (E001-E003)](#validation-errors-e001-e003)
- [HTTP Error Codes](#http-error-codes)
- [System Errors](#system-errors)

---

## Error Code Categories

| Range | Category | Description |
|-------|----------|-------------|
| E100-E199 | LLM Provider Errors | Language model provider issues |
| E200-E299 | TTS Provider Errors | Text-to-speech provider issues |
| E300-E399 | System Errors | FFmpeg, dependencies, system issues |
| E400-E499 | Visual Provider Errors | Image generation provider issues |
| E500-E599 | Rendering Errors | Video rendering and export issues |
| E001-E003 | Validation Errors | Input validation failures |
| E997-E999 | General Errors | Uncategorized errors |

---

## Provider Errors (E100-E400)

### LLM Provider Errors (E100-E199)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E100** | LLM Provider Error | General LLM provider failure | [See Guide](../troubleshooting/provider-errors.md#llm-errors) |
| **E100-401** | LLM Authentication Failed | Invalid or expired API key | [See Guide](../troubleshooting/provider-errors.md#authentication) |
| **E100-429** | LLM Rate Limit Exceeded | Too many requests to provider | [See Guide](../troubleshooting/provider-errors.md#rate-limits) |
| **E100-500** | LLM Service Unavailable | Provider service is down | [See Guide](../troubleshooting/provider-errors.md#llm-errors) |
| **E100-timeout** | LLM Request Timeout | Request took too long | [See Guide](../troubleshooting/network-errors.md#network-timeouts) |

### TTS Provider Errors (E200-E299)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E200** | TTS Provider Error | General TTS provider failure | [See Guide](../troubleshooting/provider-errors.md#tts-errors) |
| **E200-401** | TTS Authentication Failed | Invalid or expired API key | [See Guide](../troubleshooting/provider-errors.md#authentication) |
| **E200-429** | TTS Rate Limit Exceeded | Too many TTS requests | [See Guide](../troubleshooting/provider-errors.md#rate-limits) |
| **E200-invalid-voice** | Voice Not Available | Selected voice not available | [See Guide](../troubleshooting/provider-errors.md#tts-errors) |

### System Errors (E300-E399)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E301** | HTTP to HTTPS Required | Must use HTTPS | Check server configuration |
| **E302** | FFmpeg Not Ready | FFmpeg not installed/configured | [See Guide](../troubleshooting/ffmpeg-errors.md) |
| **E303** | Configuration Error | Invalid configuration | [See Guide](../troubleshooting/general-errors.md#configuration-errors) |
| **E304** | Database Error | Database connection/query failed | [See Guide](../troubleshooting/general-errors.md#database-errors) |
| **E305** | Service Unavailable | Internal service unavailable | Check logs, restart service |

### Visual Provider Errors (E400-E499)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E400** | Visual Provider Error | General image generation error | [See Guide](../troubleshooting/provider-errors.md#visual-errors) |
| **E400-401** | Visual Authentication Failed | Invalid or expired API key | [See Guide](../troubleshooting/provider-errors.md#authentication) |
| **E400-content-policy** | Content Policy Violation | Prompt violates content policy | [See Guide](../troubleshooting/provider-errors.md#visual-errors) |
| **E400-nsfw** | NSFW Content Detected | Content detected as inappropriate | Modify prompt |

---

## Rendering Errors (E500)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E500** | Rendering Error | General rendering failure | [See Guide](../troubleshooting/rendering-errors.md) |
| **E500-ffmpeg** | FFmpeg Processing Failed | FFmpeg encountered an error | [See Guide](../troubleshooting/ffmpeg-errors.md#processing-errors) |
| **E500-timeout** | Rendering Timeout | Rendering took too long | Increase timeout or reduce complexity |
| **E500-disk-space** | Insufficient Disk Space | Not enough space for output | [See Guide](../troubleshooting/resource-errors.md#disk-space-issues) |
| **E500-codec** | Codec Error | Codec not available or failed | [See Guide](../troubleshooting/ffmpeg-errors.md#codec-issues) |

---

## Validation Errors (E001-E003)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E001** | Validation Error | General input validation failure | [See Guide](../troubleshooting/validation-errors.md) |
| **E002** | Invalid Input | Specific parameter is invalid | [See Guide](../troubleshooting/validation-errors.md#invalid-input) |
| **E003** | Access Denied | Insufficient permissions | [See Guide](../troubleshooting/access-errors.md) |

---

## HTTP Error Codes

Standard HTTP status codes used in API responses:

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **400** | Bad Request | Invalid request format or parameters | Check request syntax |
| **401** | Unauthorized | Authentication required | Provide valid API key |
| **403** | Forbidden | Access denied | Check permissions |
| **404** | Not Found | Resource not found | Check resource ID/path |
| **413** | Payload Too Large | Request body too large | Reduce request size |
| **429** | Too Many Requests | Rate limit exceeded | Wait and retry |
| **500** | Internal Server Error | Server-side error occurred | Check logs, retry later |
| **503** | Service Unavailable | Service temporarily down | Retry later |

---

## System Errors

### FFmpeg-Specific Errors

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **FFmpegNotFound** | FFmpeg Not Found | FFmpeg not installed | [Install FFmpeg](../setup/dependencies.md#ffmpeg) |
| **FFmpegCorrupted** | FFmpeg Corrupted | FFmpeg installation damaged | [Reinstall FFmpeg](../troubleshooting/ffmpeg-errors.md#corrupted-installation) |
| **FFmpegFailed** | FFmpeg Processing Failed | FFmpeg command failed | [See Guide](../troubleshooting/ffmpeg-errors.md#processing-errors) |

### Resource Errors

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **OutOfDiskSpace** | Insufficient Disk Space | Not enough disk space | [Free up space](../troubleshooting/resource-errors.md#disk-space-issues) |
| **OutputDirectoryNotWritable** | Output Directory Not Writable | Cannot write to output directory | [Fix permissions](../troubleshooting/resource-errors.md#file-permission-errors) |
| **OutOfMemory** | Out of Memory | Insufficient RAM | [Reduce memory usage](../troubleshooting/resource-errors.md#memory-issues) |

### API Key Errors

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **MissingApiKey** | Missing API Key | No API key configured | [Configure API key](../setup/api-keys.md) |
| **MissingApiKey:OPENAI_KEY** | Missing OpenAI API Key | OpenAI key not configured | [Get OpenAI key](../setup/api-keys.md#openai-api-key) |
| **MissingApiKey:ANTHROPIC_KEY** | Missing Anthropic API Key | Anthropic key not configured | [Get Anthropic key](../setup/api-keys.md#anthropic-api-key) |
| **MissingApiKey:ELEVENLABS_KEY** | Missing ElevenLabs API Key | ElevenLabs key not configured | [Get ElevenLabs key](../setup/api-keys.md#elevenlabs-api-key) |
| **MissingApiKey:STABILITY_KEY** | Missing Stability AI API Key | Stability AI key not configured | [Get Stability key](../setup/api-keys.md#stability-ai-api-key) |

### Network Errors

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **TransientNetworkFailure** | Network Connection Error | Unable to connect to service | [See Guide](../troubleshooting/network-errors.md) |
| **ConnectionTimeout** | Connection Timeout | Request timed out | [See Guide](../troubleshooting/network-errors.md#network-timeouts) |
| **SSLCertificateError** | SSL Certificate Error | Certificate validation failed | [See Guide](../troubleshooting/network-errors.md#ssltls-errors) |

### Hardware Errors

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **RequiresNvidiaGPU** | NVIDIA GPU Required | Feature requires NVIDIA GPU | [See Requirements](../setup/system-requirements.md#gpu-requirements) |
| **InsufficientVRAM** | Insufficient VRAM | Not enough GPU memory | Reduce quality or use CPU |

### Platform Errors

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **UnsupportedOS** | Unsupported Operating System | OS not supported | [See Requirements](../setup/system-requirements.md#operating-system-support) |
| **UnsupportedFeature** | Feature Not Supported | Feature unavailable on platform | Check roadmap or use alternative |

---

## General Errors (E997-E999)

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **E997** | Not Implemented | Feature not yet implemented | [Check Roadmap](../roadmap.md) |
| **E998** | Operation Cancelled | User or system cancelled operation | [See Guide](../troubleshooting/general-errors.md#operation-cancelled) |
| **E999** | Unexpected Error | Unhandled exception occurred | [See Guide](../troubleshooting/general-errors.md#unexpected-errors) |

---

## Resilience Errors

### Circuit Breaker

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **CIRCUIT_OPEN** | Service Temporarily Unavailable | Circuit breaker opened | [See Guide](../troubleshooting/resilience.md#circuit-breaker) |
| **CIRCUIT_HALF_OPEN** | Service Testing | Circuit breaker testing recovery | Wait for circuit to close |

### Retry

| Code | Error | Description | Resolution |
|------|-------|-------------|------------|
| **RETRY_EXHAUSTED** | Retry Attempts Exhausted | All retries failed | [See Guide](../troubleshooting/resilience.md#retry-policies) |

---

## Error Response Format

All API errors follow RFC 7807 Problem Details format:

```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#e100",
  "title": "LLM Provider Error",
  "status": 500,
  "detail": "Failed to generate script: Rate limit exceeded",
  "errorCode": "E100-429",
  "correlationId": "abc-123-def-456",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Fields**:
- `type`: URI identifying the error type (this document)
- `title`: Short, human-readable summary
- `status`: HTTP status code
- `detail`: Human-readable explanation
- `errorCode`: Specific error code
- `correlationId`: For tracking across services
- `timestamp`: When error occurred

---

## Troubleshooting Workflow

When you encounter an error:

1. **Note the Error Code**: E.g., "E100-429"

2. **Find the Category**: 
   - E100-E199: LLM Provider
   - E200-E299: TTS Provider
   - E300-E399: System
   - E400-E499: Visual Provider
   - E500-E599: Rendering

3. **Look Up the Code**: Use tables above

4. **Follow the Resolution Link**: Goes to detailed guide

5. **Try Suggested Solutions**: Step-by-step fixes

6. **Check Logs** (if needed):
   - Windows: `%APPDATA%\Aura\logs\`
   - Linux/Mac: `~/.config/aura/logs/`

7. **Still Stuck?**: 
   - Search [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
   - Create new issue with error code and details

---

## Error Prevention

### Best Practices

1. **Configure API Keys Properly**: [Setup Guide](../setup/api-keys.md)
2. **Install All Dependencies**: [Dependencies Guide](../setup/dependencies.md)
3. **Meet System Requirements**: [Requirements Guide](../setup/system-requirements.md)
4. **Keep Software Updated**: Regularly update Aura and dependencies
5. **Monitor Resources**: Check disk space, memory, etc.
6. **Use Fallback Providers**: Configure backup providers
7. **Enable Resilience Features**: Circuit breakers, retries
8. **Test Configuration**: Use connection tests in Settings

---

## Related Documentation

- [General Troubleshooting](../troubleshooting/Troubleshooting.md)
- [Provider Errors](../troubleshooting/provider-errors.md)
- [Rendering Errors](../troubleshooting/rendering-errors.md)
- [FFmpeg Errors](../troubleshooting/ffmpeg-errors.md)
- [Network Errors](../troubleshooting/network-errors.md)
- [Validation Errors](../troubleshooting/validation-errors.md)
- [Access Errors](../troubleshooting/access-errors.md)
- [Resource Errors](../troubleshooting/resource-errors.md)
- [Resilience Guide](../troubleshooting/resilience.md)

---

## Contributing

Found an error code not listed here? Please:
1. Open an issue or PR
2. Include error code and message
3. Describe when it occurs
4. Suggest resolution if known

---

*Last Updated: January 2024*
