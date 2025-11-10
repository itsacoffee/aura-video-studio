# Validation Errors

This guide helps you troubleshoot validation errors and invalid input issues in Aura Video Studio.

## Quick Navigation

- [Common Validation Errors](#common-validation-errors)
- [Input Validation](#invalid-input)
- [Request Validation](#request-validation)
- [Form Validation](#form-validation)

---

## Common Validation Errors

### Error Codes

- **E001**: General validation error
- **E002**: Invalid input parameter
- **E003**: Access denied
- **E400**: Bad request

---

## Invalid Input

### Missing Required Fields

**Error**: "Required field is missing"

**Common Fields**:
- Project name
- Video title
- Script content
- Output path
- Provider API key

**Solutions**:
1. Review form and ensure all required fields are filled
2. Check for fields marked with asterisk (*)
3. Hover over field labels for requirements

### Invalid Field Format

**Error**: "Field value is not in the correct format"

**Common Format Issues**:

#### Invalid Email
```
Invalid: user@example
Valid: user@example.com
```

#### Invalid URL
```
Invalid: htp://example.com
Valid: https://example.com
```

#### Invalid API Key Format
```
OpenAI: Must start with "sk-"
Anthropic: Must start with "sk-ant-"
ElevenLabs: UUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
```

#### Invalid Path
```
Windows Invalid: C:\Users\Name\<invalid>
Windows Valid: C:\Users\Name\Videos

Linux Invalid: /home/user/file|name
Linux Valid: /home/user/filename
```

### Value Out of Range

**Error**: "Value must be between X and Y"

**Common Range Issues**:

| Field | Valid Range | Notes |
|-------|-------------|-------|
| Video Duration | 1-600 seconds | 10 minutes max |
| Frame Rate | 1-60 fps | Typically 24, 30, or 60 |
| Resolution Width | 128-3840 px | Up to 4K |
| Resolution Height | 128-2160 px | Up to 4K |
| Audio Bitrate | 64-320 kbps | 192k recommended |
| Video Bitrate | 500k-50M | Depends on resolution |

**Solutions**:
1. Check the error message for valid range
2. Adjust value to within acceptable limits
3. Use presets for common configurations

### String Length Validation

**Error**: "Text exceeds maximum length"

**Common Limits**:

| Field | Max Length | Notes |
|-------|------------|-------|
| Project Name | 100 chars | |
| Video Title | 200 chars | |
| Description | 2000 chars | |
| Script | 50,000 chars | ~10,000 words |
| File Path | 260 chars (Windows) | Path length limit |
| API Key | 200 chars | Varies by provider |

**Solutions**:
1. Shorten text to within limit
2. For scripts, split into multiple videos
3. Use shorter file paths

---

## Request Validation

### Invalid JSON

**Error**: "Invalid JSON in request body"

**Common Causes**:
- Missing quotes around strings
- Trailing commas
- Unescaped special characters
- Incorrect nesting

**Example Invalid JSON**:
```json
{
  "title": "My Video",
  "duration": 30,  // No trailing comma before last item
}
```

**Example Valid JSON**:
```json
{
  "title": "My Video",
  "duration": 30
}
```

**Solutions**:
1. Use JSON validator (https://jsonlint.com/)
2. Check for common syntax errors
3. Use proper JSON editing tools

### Missing Required Parameters

**Error**: "Required parameter 'X' is missing"

**API Endpoint Examples**:

#### POST /api/videos/generate
Required:
- `title` (string)
- `scriptId` (string)
- `providerId` (string)

#### POST /api/projects
Required:
- `name` (string)
- `templateId` (string, optional)

**Solutions**:
1. Review API documentation for required parameters
2. Check request payload includes all required fields
3. Verify parameter names are spelled correctly

### Invalid Parameter Type

**Error**: "Parameter 'X' must be of type Y"

**Common Type Mismatches**:
```javascript
// Invalid
{ "duration": "30" }  // String instead of number

// Valid
{ "duration": 30 }  // Number

// Invalid
{ "enabled": "true" }  // String instead of boolean

// Valid
{ "enabled": true }  // Boolean

// Invalid
{ "tags": "tag1,tag2" }  // String instead of array

// Valid
{ "tags": ["tag1", "tag2"] }  // Array
```

---

## Form Validation

### Project Configuration Validation

#### Invalid Project Name
- Cannot be empty
- Cannot contain special characters: `< > : " / \ | ? *`
- Maximum 100 characters

**Valid Examples**:
```
My Video Project
Tutorial-2024
Marketing_Campaign_Q1
```

**Invalid Examples**:
```
My/Project        (contains /)
<Project>         (contains < >)
Project:Final     (contains :)
```

### Timeline Validation

**Error**: "Timeline configuration is invalid"

**Common Issues**:
1. **No content**: Timeline has no video, image, or audio tracks
2. **Overlapping clips**: Clips overlap on same track
3. **Invalid timing**: Start time after end time
4. **Missing assets**: Referenced media files don't exist

**Solutions**:
```javascript
// Valid timeline structure
{
  "duration": 30,
  "tracks": [
    {
      "type": "video",
      "clips": [
        {
          "start": 0,
          "end": 10,
          "assetId": "valid-asset-id"
        },
        {
          "start": 10,  // Non-overlapping
          "end": 20,
          "assetId": "valid-asset-id-2"
        }
      ]
    }
  ]
}
```

### Rendering Configuration Validation

**Error**: "Invalid rendering configuration"

**Common Issues**:

1. **Incompatible resolution and codec**:
   ```json
   // Some codecs have resolution limits
   {
     "resolution": "8192x4096",  // Too high for most codecs
     "codec": "libx264"
   }
   ```

2. **Invalid codec combination**:
   ```json
   {
     "videoCodec": "libx264",
     "audioCodec": "opus",  // Not compatible with MP4
     "format": "mp4"
   }
   ```

3. **Conflicting settings**:
   ```json
   {
     "useHardwareAcceleration": true,
     "hardwareEncoder": "h264_nvenc",  // NVIDIA
     "device": "AMD GPU"  // Mismatch
   }
   ```

**Solutions**:
1. Use preset configurations for common use cases
2. Refer to [Rendering Settings Reference](rendering-errors.md#rendering-settings-reference)
3. Test with default settings first

---

## Provider Configuration Validation

### Invalid API Key

**Error**: "API key format is invalid"

**Provider-Specific Formats**:

```javascript
// OpenAI
{
  "provider": "OpenAI",
  "apiKey": "sk-proj-abc123..."  // Must start with "sk-" or "sk-proj-"
}

// Anthropic
{
  "provider": "Anthropic",
  "apiKey": "sk-ant-api03-abc123..."  // Must start with "sk-ant-"
}

// ElevenLabs
{
  "provider": "ElevenLabs",
  "apiKey": "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6"  // UUID format
}

// Stability AI
{
  "provider": "StabilityAI",
  "apiKey": "sk-abc123XYZ..."  // Alphanumeric
}
```

### Invalid Model Selection

**Error**: "Selected model is not available"

**Common Issues**:
1. Model not available on your account tier
2. Model name typo
3. Model deprecated or removed

**Solutions**:
1. Click "Refresh Models" to get latest available models
2. Check provider documentation for available models
3. Verify account has access to selected model

---

## File System Validation

### Invalid Output Path

**Error**: "Output path is invalid or not writable"

**Common Issues**:

1. **Path doesn't exist**:
   ```
   Invalid: C:\NonExistent\Output
   Solution: Create directory first or use existing path
   ```

2. **No write permission**:
   ```
   Invalid: C:\Windows\System32\output.mp4
   Solution: Use user-writable directory like Documents or Videos
   ```

3. **Invalid characters**:
   ```
   Windows Invalid: output<video>.mp4
   Valid: output_video.mp4
   ```

4. **Path too long** (Windows):
   ```
   Invalid: C:\Very\Long\Path\That\Exceeds\260\Characters\...\video.mp4
   Solution: Use shorter path or enable long path support
   ```

### Insufficient Disk Space

**Error**: "Insufficient disk space for operation"

**Required Space Estimates**:
- 720p 5min video: ~100-200 MB
- 1080p 5min video: ~200-500 MB
- 4K 5min video: ~500MB-2GB
- Cache and temp files: 1-5 GB

**Solutions**:
1. Free up disk space
2. Change output location to drive with more space
3. Clear Aura cache (Settings → Storage → Clear Cache)
4. Remove temporary files

---

## Access Control Validation

### Error Code E003: Access Denied

**Common Causes**:

1. **Feature requires Advanced Mode**:
   ```
   Error: "This feature requires Advanced Mode"
   Solution: Enable Advanced Mode in Settings
   ```

2. **Insufficient Permissions**:
   ```
   Error: "You don't have permission to access this resource"
   Solution: Check user role and permissions
   ```

3. **API Key Quota Exceeded**:
   ```
   Error: "API key quota exceeded"
   Solution: Upgrade plan or wait for quota reset
   ```

4. **Feature Not Available**:
   ```
   Error: "Feature not available in your region/plan"
   Solution: Check feature availability or upgrade
   ```

---

## Validation Best Practices

### Client-Side Validation

Always validate input before submission:
```typescript
// Example validation
function validateVideoConfig(config) {
  if (!config.title || config.title.length === 0) {
    throw new Error("Title is required");
  }
  
  if (config.duration < 1 || config.duration > 600) {
    throw new Error("Duration must be between 1 and 600 seconds");
  }
  
  if (config.resolution.width > 3840) {
    throw new Error("Width cannot exceed 3840 pixels");
  }
  
  return true;
}
```

### Server-Side Validation

Server always validates even if client validates:
```csharp
// C# validation example
[Required]
[StringLength(100, MinimumLength = 1)]
public string Title { get; set; }

[Range(1, 600)]
public int Duration { get; set; }

[Range(128, 3840)]
public int Width { get; set; }
```

### Error Message Guidelines

Good error messages include:
1. What went wrong
2. Why it's wrong
3. How to fix it

**Good Example**:
```
Error: Duration exceeds maximum limit
Duration must be between 1 and 600 seconds
Current value: 750 seconds
```

**Bad Example**:
```
Error: Invalid input
```

---

## Common Validation Scenarios

### Creating a New Video

**Required Validation**:
- [ ] Project name is not empty
- [ ] Script is provided
- [ ] Valid provider is selected
- [ ] API key is configured
- [ ] Output path is writable
- [ ] Sufficient disk space

### Updating Configuration

**Required Validation**:
- [ ] Configuration format is valid JSON
- [ ] All values are within valid ranges
- [ ] File paths exist and are accessible
- [ ] API keys are in correct format

### Submitting a Render Job

**Required Validation**:
- [ ] Timeline has content
- [ ] All referenced assets exist
- [ ] FFmpeg is installed and working
- [ ] Output format is supported
- [ ] Rendering settings are compatible

---

## Related Documentation

- [Access Errors](access-errors.md)
- [Provider Errors](provider-errors.md)
- [API Reference](../api/README.md)
- [General Troubleshooting](Troubleshooting.md)

## Need More Help?

If validation errors persist:
1. Enable detailed validation logging:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Aura.Core.Validation": "Debug"
       }
     }
   }
   ```
2. Review the full error message and stack trace
3. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
4. Create a new issue with:
   - Exact error message
   - Input values causing error
   - Expected vs actual behavior
   - Steps to reproduce
