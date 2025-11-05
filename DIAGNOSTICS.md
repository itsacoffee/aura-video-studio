# Diagnostics Bundle and Failure Analysis Guide

## Overview

Aura Video Studio includes powerful diagnostic tools to help troubleshoot failed jobs quickly and efficiently. The diagnostics system provides:

1. **Diagnostic Bundles**: One-click download of comprehensive diagnostic data with automatic redaction of sensitive information
2. **Failure Analysis**: AI-powered root cause analysis with actionable recommendations

## Features

### Diagnostic Bundles

Diagnostic bundles are ZIP archives containing everything needed to diagnose a job failure:

**Contents:**
- **manifest.json**: Complete metadata about the job and bundle
- **system-info.json**: System and hardware information
- **timeline.json**: Job execution timeline with stage durations and correlation IDs
- **logs-redacted.txt**: Anonymized application logs (API keys and credentials redacted)
- **model-decisions.json**: AI model selection decisions (if applicable)
- **ffmpeg-commands.json**: FFmpeg commands executed (if applicable)
- **cost-report.json**: Cost breakdown by stage and provider (if applicable)
- **README.txt**: Human-readable overview of bundle contents

**Privacy:**
All diagnostic bundles are automatically redacted to remove:
- API keys and authentication tokens
- Passwords and credentials
- Personal identifying information
- Machine names are anonymized

Bundles are safe to share for troubleshooting purposes.

### Failure Analysis

The AI-powered failure analysis system examines error messages, logs, and job telemetry to:

1. Identify the primary root cause with confidence score
2. List secondary possible causes
3. Provide prioritized recommended actions with step-by-step instructions
4. Link to relevant documentation
5. Estimate time to resolve

**Supported Root Cause Types:**
- **RateLimit**: API rate limit exceeded
- **InvalidApiKey**: Invalid or expired API key
- **MissingApiKey**: No API key configured
- **NetworkError**: Network connectivity issues
- **MissingCodec**: Required video codec not available
- **FFmpegNotFound**: FFmpeg not found or not configured
- **InsufficientResources**: Not enough memory, disk space, or other resources
- **InvalidInput**: Invalid configuration or request data
- **ProviderUnavailable**: Provider service temporarily down
- **Timeout**: Operation timeout
- **BudgetExceeded**: Budget or quota exceeded
- **FileSystemError**: File permissions or disk space issues
- **Unknown**: Unclassified errors

## Usage

### From the UI

#### Download Diagnostic Bundle

1. Navigate to a failed job
2. Click the "Diagnostics" tab or button
3. Click "Download Diagnostics"
4. The bundle is generated and downloaded automatically
5. Extract the ZIP file to view contents

**Bundle Expiration:**
Bundles expire after 24 hours and are automatically cleaned up.

#### Analyze Failure

1. Navigate to a failed job
2. Click the "Diagnostics" tab or button
3. Click "Analyze Failure"
4. Review the root cause analysis
5. Follow recommended actions in priority order

### From the API

#### Generate Diagnostic Bundle

```bash
POST /api/diagnostics/bundle/{jobId}
```

**Response:**
```json
{
  "bundleId": "abc123...",
  "jobId": "job-456",
  "fileName": "diagnostic-bundle-job-456-2025-11-05-140000.zip",
  "createdAt": "2025-11-05T14:00:00Z",
  "expiresAt": "2025-11-06T14:00:00Z",
  "sizeBytes": 123456,
  "downloadUrl": "/api/diagnostics/bundle/abc123.../download"
}
```

#### Download Bundle

```bash
GET /api/diagnostics/bundle/{bundleId}/download
```

Returns the ZIP file as application/zip.

#### Explain Failure

```bash
POST /api/diagnostics/explain-failure
Content-Type: application/json

{
  "jobId": "job-456",
  "stage": "TTS",
  "errorMessage": "Rate limit exceeded",
  "errorCode": "E429"
}
```

**Response:**
```json
{
  "jobId": "job-456",
  "analyzedAt": "2025-11-05T14:00:00Z",
  "summary": "Job job-456 failed during the TTS stage. API rate limit exceeded...",
  "primaryRootCause": {
    "type": "RateLimit",
    "description": "API rate limit exceeded - Too many requests sent to the provider in a short time",
    "confidence": 90,
    "evidence": ["Rate limit exceeded"],
    "stage": "TTS",
    "provider": "ElevenLabs"
  },
  "secondaryRootCauses": [],
  "recommendedActions": [
    {
      "priority": 1,
      "title": "Wait and Retry",
      "description": "Rate limits typically reset after a few minutes...",
      "steps": [
        "Wait 5-10 minutes before retrying",
        "Check your provider dashboard for rate limit details"
      ],
      "canAutomate": false,
      "estimatedMinutes": 10,
      "type": "WaitAndRetry"
    }
  ],
  "documentationLinks": [
    {
      "title": "Managing API Rate Limits",
      "url": "https://docs.aura.studio/troubleshooting/rate-limits",
      "description": "Learn how to handle and avoid rate limit errors"
    }
  ],
  "confidenceScore": 90
}
```

## Bundle Manifest Schema

The bundle manifest follows a strict JSON schema for compatibility across versions.

**Schema Location:** `diagnostics/BundleManifest.schema.json`

**Schema Version:** 1.0

Key sections:
- **job**: Job information (ID, status, stage, timestamps, error details)
- **systemProfile**: Hardware and OS information
- **timeline**: Stage-by-stage execution timeline with durations and correlation IDs
- **modelDecisions**: AI model selection decisions
- **ffmpegCommands**: FFmpeg commands with exit codes and durations
- **exportManifests**: Export metadata
- **costReport**: Cost breakdown by stage and provider
- **logs**: Anonymized log entries
- **files**: List of files in the bundle

## Correlation IDs

Every stage of job execution is assigned a unique correlation ID for tracking:

**Purpose:**
- Track requests across services
- Correlate logs from different components
- Debug distributed systems
- Link telemetry data

**Format:** UUID v4 or custom identifier

**Locations:**
- HTTP headers: `X-Correlation-ID`
- Log entries: `CorrelationId` field
- Timeline entries: `correlationId` field
- Model decisions: `correlationId` field
- FFmpeg commands: `correlationId` field

**Best Practices:**
- Generate correlation ID at job creation
- Pass through all service calls
- Include in all log statements
- Index correlation IDs for search

## Integration with SSE

Diagnostic events are available via Server-Sent Events (SSE) for real-time monitoring:

**SSE Endpoint:** `/api/jobs/{jobId}/events`

**Event Types:**
- `job-status`: Job status changes (includes correlation ID)
- `step-progress`: Step progress updates (includes step name and correlation ID)
- `job-completed`: Job completed successfully
- `job-failed`: Job failed (includes error message and correlation ID)

**Correlation IDs in SSE:**
Each SSE event includes a correlation ID for tracking. Use this ID to:
1. Filter logs for specific job stages
2. Correlate frontend events with backend logs
3. Debug SSE connection issues
4. Track end-to-end request flow

For more details, see: `SSE_INTEGRATION_TESTING_GUIDE.md`

## Best Practices

### When to Download Bundles

Download diagnostic bundles when:
- A job fails unexpectedly
- You need to share debugging information with support
- You're troubleshooting a complex issue
- You want to analyze performance patterns
- You need cost analysis data

### When to Use Failure Analysis

Use failure analysis when:
- You encounter an error for the first time
- The error message is unclear
- You're not sure what to do next
- You want to understand the root cause
- You need step-by-step remediation guidance

### Interpreting Confidence Scores

Confidence scores indicate how certain the analysis is about the root cause:

- **90-100%**: Very high confidence - follow these recommendations first
- **70-89%**: High confidence - likely the correct cause
- **50-69%**: Medium confidence - possible cause, investigate further
- **Below 50%**: Low confidence - consider other possibilities

Multiple root causes with similar confidence scores may indicate:
- Multiple contributing factors
- Complex failure scenarios
- Need for deeper investigation

### Privacy and Security

**What is Redacted:**
- API keys (patterns like `sk-...`, `Bearer ...`)
- Password fields
- Token fields
- Personal information

**What is NOT Redacted:**
- Job IDs and correlation IDs
- Error messages (technical details)
- System information (OS, hardware specs)
- Timing and performance data
- Provider names
- Model names

**Safe to Share:**
Diagnostic bundles are designed to be safe to share with:
- Internal team members
- Support staff
- Community forums (if comfortable)

Always review the bundle contents before sharing externally if you have concerns.

## Troubleshooting

### Bundle Generation Fails

If bundle generation fails:
1. Check disk space (need at least 100MB free)
2. Verify write permissions to application directory
3. Check logs for specific error messages
4. Try generating a bundle for a different job
5. Contact support if issue persists

### Failure Analysis Returns "Unknown"

If analysis returns "Unknown" root cause:
1. Download the diagnostic bundle for more details
2. Review error messages in the bundle
3. Check FFmpeg command output if applicable
4. Review timeline for stuck stages
5. Submit the bundle to support for manual review

### Missing Data in Bundle

If bundle is missing expected data:
1. Verify the job actually reached that stage
2. Check if telemetry was enabled during the run
3. Review bundle manifest for included files
4. Regenerate the bundle if data is stale
5. Report missing data to support

## API Reference

### POST /api/diagnostics/bundle/{jobId}

Generate a comprehensive diagnostic bundle for a specific job.

**Parameters:**
- `jobId` (path): Job ID to generate bundle for

**Returns:**
- `bundleId`: Unique bundle identifier
- `jobId`: Job ID
- `fileName`: Name of the generated file
- `createdAt`: When bundle was created
- `expiresAt`: When bundle expires
- `sizeBytes`: Size of bundle in bytes
- `downloadUrl`: URL to download the bundle

**Status Codes:**
- `200 OK`: Bundle generated successfully
- `400 Bad Request`: Invalid job ID
- `404 Not Found`: Job not found
- `500 Internal Server Error`: Bundle generation failed

### GET /api/diagnostics/bundle/{bundleId}/download

Download a diagnostic bundle by ID.

**Parameters:**
- `bundleId` (path): Bundle ID to download

**Returns:**
- ZIP file (application/zip)

**Status Codes:**
- `200 OK`: Bundle downloaded successfully
- `404 Not Found`: Bundle not found or expired
- `500 Internal Server Error`: Download failed

### POST /api/diagnostics/explain-failure

Analyze a job failure and provide recommendations.

**Request Body:**
```json
{
  "jobId": "string",
  "stage": "string (optional)",
  "errorMessage": "string (optional)",
  "errorCode": "string (optional)"
}
```

**Returns:**
- `jobId`: Job ID
- `analyzedAt`: When analysis was performed
- `summary`: Human-readable summary
- `primaryRootCause`: Primary identified cause
- `secondaryRootCauses`: Other possible causes
- `recommendedActions`: Prioritized actions
- `documentationLinks`: Relevant documentation
- `confidenceScore`: Overall confidence (0-100)

**Status Codes:**
- `200 OK`: Analysis completed successfully
- `400 Bad Request`: Invalid request
- `500 Internal Server Error`: Analysis failed

## Examples

### Example 1: Rate Limit Error

**Scenario:** ElevenLabs TTS hits rate limit during generation.

**Root Cause:** `RateLimit` (90% confidence)

**Recommendations:**
1. Wait 5-10 minutes and retry
2. Enable request caching
3. Upgrade provider tier

### Example 2: Missing API Key

**Scenario:** Job fails at script generation stage with "unauthorized" error.

**Root Cause:** `MissingApiKey` (98% confidence)

**Recommendations:**
1. Add API key in Settings > Providers
2. Verify key is valid
3. Test connection

### Example 3: FFmpeg Not Found

**Scenario:** Job fails at rendering stage with "ffmpeg not found" error.

**Root Cause:** `FFmpegNotFound` (95% confidence)

**Recommendations:**
1. Install FFmpeg from ffmpeg.org
2. Add FFmpeg to system PATH
3. Configure FFmpeg path in Settings

## Support

If you need help:
1. Download diagnostic bundle
2. Run failure analysis
3. Check documentation links
4. Search community forums
5. Submit bundle to support with issue description

For urgent issues, include:
- Job ID
- Diagnostic bundle
- Failure analysis output
- Steps to reproduce

## Related Documentation

- [SSE Integration Testing Guide](SSE_INTEGRATION_TESTING_GUIDE.md)
- [Cost Analytics Implementation](COST_ANALYTICS_IMPLEMENTATION.md)
- [Production Readiness Checklist](PRODUCTION_READINESS_CHECKLIST.md)
- [Oncall Runbook](OncallRunbook.md)
