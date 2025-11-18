# RunTelemetry v1 - Unified Telemetry Schema

## Overview

RunTelemetry v1 provides a standardized, versioned schema for capturing detailed telemetry data from video generation runs. It unifies cost tracking, performance metrics, and diagnostics into a single, consistent format.

## Features

### Unified Schema
- **Version 1.0** with JSON Schema definition
- Captures all pipeline stages: brief, plan, script, ssml, tts, visuals, render, post
- Per-stage and per-scene granularity
- Provider selection tracking (default, pinned, cli, fallback)
- Comprehensive cost and performance metrics

### Automatic Data Collection
- **Per-run persistence**: JSON file saved alongside job artifacts
- **Thread-safe collection**: Safe for concurrent operations
- **Automatic PII masking**: API keys, tokens, and secrets are redacted
- **Summary statistics**: Aggregated metrics for quick analysis

### Data Captured Per Stage

Each telemetry record includes:

| Field | Type | Description |
|-------|------|-------------|
| `job_id` | string | Unique job identifier |
| `correlation_id` | string | Request correlation ID |
| `project_id` | string? | Optional project identifier |
| `stage` | enum | Pipeline stage (brief, plan, script, etc.) |
| `scene_index` | int? | Scene number for per-scene operations |
| `model_id` | string? | Model identifier (e.g., "gpt-4") |
| `provider` | string? | Provider name (e.g., "OpenAI") |
| `selection_source` | enum? | How provider was selected |
| `fallback_reason` | string? | Reason if fallback was used |
| `tokens_in` | int? | Input tokens (LLM operations) |
| `tokens_out` | int? | Output tokens (LLM operations) |
| `cache_hit` | bool? | Whether cache was hit |
| `retries` | int | Number of retry attempts |
| `latency_ms` | int | Operation latency in milliseconds |
| `cost_estimate` | decimal? | Estimated cost |
| `currency` | string | Currency code (default: USD) |
| `pricing_version` | string? | Pricing data version |
| `result_status` | enum | ok, warn, or error |
| `error_code` | string? | Error code if failed |
| `message` | string? | Human-readable message |
| `started_at` | datetime | ISO 8601 start timestamp |
| `ended_at` | datetime | ISO 8601 end timestamp |
| `metadata` | object? | Stage-specific metadata |

## Usage

### Backend Integration

#### 1. Start Collection
```csharp
var collector = serviceProvider.GetRequiredService<RunTelemetryCollector>();
collector.StartCollection(jobId, correlationId);
```

#### 2. Record Telemetry

**Using TelemetryBuilder (recommended)**:
```csharp
var telemetry = TelemetryBuilder.Start(jobId, correlationId, RunStage.Script)
    .WithModel("gpt-4", "OpenAI")
    .WithTokens(500, 1000)
    .WithCost(0.045m)
    .WithStatus(ResultStatus.Ok, message: "Script generated successfully")
    .Build();

collector.Record(telemetry);
```

**Using Extension Methods**:
```csharp
// For LLM operations
var llmRecord = TelemetryExtensions.CreateLlmTelemetry(
    jobId, correlationId, RunStage.Script,
    "gpt-4", "OpenAI", 500, 1000, 2500, 0.045m);
collector.Record(llmRecord);

// For TTS operations
var ttsRecord = TelemetryExtensions.CreateTtsTelemetry(
    jobId, correlationId, sceneIndex: 0,
    "ElevenLabs", characters: 150, 
    durationSeconds: 8.5, latencyMs: 3000, cost: 0.045m);
collector.Record(ttsRecord);
```

**Using TelemetryIntegration** (for existing LlmTelemetry):
```csharp
var integration = serviceProvider.GetRequiredService<TelemetryIntegration>();
integration.RecordLlmOperation(jobId, correlationId, RunStage.Script, llmTelemetry);
```

#### 3. End Collection
```csharp
var filePath = collector.EndCollection();
// Telemetry is now persisted to: {jobDir}/telemetry.json
```

### API Endpoints

#### Get Job Telemetry
```http
GET /api/telemetry/{jobId}
```

**Response**:
```json
{
  "version": "1.0",
  "job_id": "abc123",
  "correlation_id": "xyz789",
  "collection_started_at": "2024-01-15T10:00:00Z",
  "collection_ended_at": "2024-01-15T10:05:30Z",
  "records": [...],
  "summary": {
    "total_operations": 12,
    "successful_operations": 11,
    "failed_operations": 1,
    "total_cost": 0.245,
    "currency": "USD",
    "total_latency_ms": 45000,
    "total_tokens_in": 2500,
    "total_tokens_out": 5000,
    "cache_hits": 3,
    "total_retries": 2,
    "cost_by_stage": {
      "script": 0.045,
      "tts": 0.135,
      "visuals": 0.050,
      "render": 0.015
    },
    "operations_by_provider": {
      "OpenAI": 4,
      "ElevenLabs": 3,
      "StableDiffusion": 3
    }
  }
}
```

#### Get Schema Info
```http
GET /api/telemetry/schema
```

**Response**:
```json
{
  "version": "1.0",
  "schemaUrl": "https://aura.studio/schemas/run-telemetry/v1",
  "description": "Unified telemetry schema for video generation run stages",
  "stages": ["brief", "plan", "script", "ssml", "tts", "visuals", "render", "post"],
  "resultStatuses": ["ok", "warn", "error"],
  "selectionSources": ["default", "pinned", "cli", "fallback"]
}
```

### Frontend Usage

#### 1. Fetch Telemetry
```typescript
import { getJobTelemetry } from '@/api/telemetryClient';

const telemetry = await getJobTelemetry(jobId);
```

#### 2. Display Run Details
```typescript
// Navigate to run details page
navigate(`/jobs/${jobId}/telemetry`);
```

The RunDetailsPage component displays:
- Summary metrics (operations, cost, latency, success rate)
- Stage breakdown table
- Cost by stage
- Operations by provider

## Security and Privacy

### Automatic PII Masking

The telemetry collector automatically masks:
- API keys (patterns like `sk-...`)
- Bearer tokens
- Secret values in metadata
- Any field containing "key", "token", "secret", or "password"

**Example**:
```csharp
// Input
Message = "API error: sk-1234567890abcdef failed"

// Output (masked)
Message = "API error: [REDACTED] failed"
```

### No Sensitive Data

Telemetry records **NEVER** include:
- API keys or credentials
- User passwords
- Personal identifiable information (PII)
- Prompt content (only metadata like token counts)
- Generated script text (only metadata)

## File Structure

Telemetry files are stored in the job artifacts directory:

```
%LOCALAPPDATA%/Aura/jobs/{jobId}/
├── job.json                 # Job state
├── telemetry.json          # RunTelemetry collection (this file)
├── output.mp4              # Generated video
└── ...                     # Other artifacts
```

## Schema Versioning

The schema follows semantic versioning:

- **v1.0**: Initial release (current)
- **v1.x**: Backward-compatible additions
- **v2.0**: Breaking changes (future)

The `version` field in every record ensures consumers can handle multiple schema versions gracefully.

## Integration with Existing Systems

### Cost Analytics
Cost analytics services should read telemetry records instead of maintaining separate cost logs:

```csharp
var telemetry = collector.LoadTelemetry(jobId);
var totalCost = telemetry.Summary.TotalCost;
var costByStage = telemetry.Summary.CostByStage;
```

### Diagnostics
Diagnostics can analyze failed stages:

```csharp
var failedStages = telemetry.Records
    .Where(r => r.ResultStatus == ResultStatus.Error)
    .Select(r => new { r.Stage, r.ErrorCode, r.Message });
```

### Performance Monitoring
Track latency and retries:

```csharp
var avgLatency = telemetry.Summary.TotalLatencyMs / telemetry.Summary.TotalOperations;
var highRetryStages = telemetry.Records
    .Where(r => r.Retries > 2)
    .Select(r => r.Stage);
```

## Testing

### Unit Tests
```bash
dotnet test --filter "FullyQualifiedName~RunTelemetryTests"
```

### Integration Tests
```bash
dotnet test --filter "FullyQualifiedName~TelemetryIntegrationTests"
```

## Migration from Existing Telemetry

Existing systems using `LlmOperationTelemetry` can migrate using `TelemetryIntegration`:

```csharp
// Old way (still works)
var llmTelemetry = new LlmOperationTelemetry { ... };
llmTelemetryCollector.Record(llmTelemetry);

// New way (unified)
var integration = serviceProvider.GetRequiredService<TelemetryIntegration>();
integration.RecordLlmOperation(jobId, correlationId, RunStage.Script, llmTelemetry);
```

Both systems can coexist during migration.

## Future Enhancements

Potential additions to v1.x (backward-compatible):
- Real-time streaming telemetry via SignalR
- Export to external analytics platforms (Prometheus, DataDog)
- Alerting based on cost thresholds or error rates
- Machine learning insights from historical telemetry

## Support

For issues or questions:
- GitHub Issues: [aura-video-studio/issues](https://github.com/Saiyan9001/aura-video-studio/issues)
- Schema Documentation: See `Aura.Core/Telemetry/RunTelemetry.schema.json`
