# ML Training Backend Foundation Guide

This guide provides an overview of the ML training backend implementation for the Aura Video Studio frame importance retraining workflow.

## Overview

The ML training backend enables users to retrain the frame importance model using their own annotations. The system provides:

- Annotation storage and retrieval
- Background training job orchestration
- Model deployment with atomic swap and rollback
- RESTful API endpoints for all operations
- Comprehensive error handling and logging

## Architecture

### Components

#### 1. Data Models (`Aura.Api/Models/ApiModels.V1/Dtos.cs`)

**Annotation DTOs:**
- `AnnotationItemDto`: Single frame annotation with path, rating (0-1), and metadata
- `AnnotationBatchDto`: Batch of annotations for upload
- `AnnotationStatsDto`: Statistics about stored annotations

**Training DTOs:**
- `TrainingJobStatusDto`: Job state, progress, metrics, model path, and errors
- `TrainingMetricsDto`: Loss, samples, duration, and additional metrics
- `StartTrainingRequest`: Parameters for starting a training job
- `StartTrainingResponse`: Job ID and confirmation message

#### 2. Storage Service (`Aura.Core/Services/ML/AnnotationStorageService.cs`)

**Purpose:** Manages per-user annotation storage in JSONL format.

**Storage Location:**
```
%AppData%/Aura/ML/Annotations/{userId}/annotations.jsonl
```

**Key Methods:**
- `StoreAnnotationsAsync`: Append annotations to user's JSONL file
- `GetAnnotationsAsync`: Load all annotations for a user
- `GetStatsAsync`: Calculate statistics (count, average rating, date range)
- `ClearAnnotationsAsync`: Remove all annotations for a user

**Validation:**
- Frame path must not be empty
- Rating must be between 0.0 and 1.0
- User ID must be valid

#### 3. Training Worker (`Aura.Core/Services/ML/MlTrainingWorker.cs`)

**Purpose:** Background service that manages training job queue and execution.

**Job States:**
- `Queued`: Job submitted, waiting to start
- `Running`: Training in progress
- `Completed`: Training finished successfully
- `Failed`: Training failed with error
- `Cancelled`: User cancelled the job

**Key Methods:**
- `SubmitJobAsync`: Queue a new training job
- `GetJobStatus`: Get current status of a job
- `CancelJob`: Request cancellation of a running job

**Progress Reporting:**
- 0-10%: Loading annotations
- 10-20%: Preparing training data
- 20-80%: Model training (via ModelTrainingService)
- 80-100%: Model deployment

**Concurrency:** Uses a semaphore to ensure only one training job runs at a time.

#### 4. Model Manager (`Aura.Core/ML/ModelManager.cs`)

**Purpose:** Handles model deployment, validation, backup, and rollback.

**Model Files:**
- `frame-importance-model-default.zip`: Factory default model (fallback)
- `frame-importance-model.zip`: Active model (user-trained or default)
- `frame-importance-model.zip.backup`: Previous version backup

**Key Methods:**
- `GetActiveModelPathAsync`: Returns active model with fallback to default
- `DeployModelAsync`: Atomically deploy new model with backup
- `RevertToDefaultAsync`: Remove active model, use default
- `RestoreFromBackupAsync`: Restore previous model version

**Safety Features:**
- Validates model file before deployment
- Creates backup before overwriting
- Atomic file operations (temp file + move)
- Automatic fallback on validation failure

#### 5. Model Training Service (`Aura.Core/Services/ML/ModelTrainingService.cs`)

**Purpose:** Executes the actual ML.NET training pipeline.

**Note:** This is an existing service that was enhanced for integration with the new training backend.

#### 6. ML Controller (`Aura.Api/Controllers/MlController.cs`)

**Purpose:** RESTful API endpoints for ML operations.

**Endpoints:**

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/ml/annotations/upload` | Upload frame annotations |
| GET | `/api/ml/annotations/stats` | Get annotation statistics |
| POST | `/api/ml/train/frame-importance` | Start training job |
| GET | `/api/ml/train/{jobId}/status` | Get job status |
| POST | `/api/ml/train/{jobId}/cancel` | Cancel running job |
| POST | `/api/ml/model/revert` | Revert to default model |

**Error Handling:**
- 400 Bad Request: Invalid input, insufficient data
- 404 Not Found: Job not found
- 500 Internal Server Error: System errors
- All errors return ProblemDetails with correlation ID

**Logging:**
- Structured logging with Serilog
- Correlation IDs for request tracking
- Entry/exit logging for all operations

## Usage Examples

### 1. Upload Annotations

```bash
curl -X POST http://localhost:5005/api/ml/annotations/upload \
  -H "Content-Type: application/json" \
  -d '{
    "annotations": [
      { "framePath": "frame001.jpg", "rating": 0.9 },
      { "framePath": "frame002.jpg", "rating": 0.7 },
      { "framePath": "frame003.jpg", "rating": 0.5 }
    ]
  }'
```

### 2. Get Statistics

```bash
curl -X GET http://localhost:5005/api/ml/annotations/stats
```

Response:
```json
{
  "userId": "default-user",
  "totalAnnotations": 3,
  "averageRating": 0.7,
  "oldestAnnotation": "2025-11-03T12:00:00Z",
  "newestAnnotation": "2025-11-03T14:30:00Z"
}
```

### 3. Start Training

```bash
curl -X POST http://localhost:5005/api/ml/train/frame-importance \
  -H "Content-Type: application/json" \
  -d '{ "modelName": "my-custom-model" }'
```

Response:
```json
{
  "jobId": "abc123-def456-ghi789",
  "message": "Training job submitted successfully"
}
```

### 4. Monitor Progress

```bash
curl -X GET http://localhost:5005/api/ml/train/abc123-def456-ghi789/status
```

Response:
```json
{
  "jobId": "abc123-def456-ghi789",
  "state": "Running",
  "progress": 45.0,
  "metrics": null,
  "modelPath": null,
  "error": null,
  "createdAt": "2025-11-03T14:35:00Z",
  "completedAt": null
}
```

### 5. Cancel Job

```bash
curl -X POST http://localhost:5005/api/ml/train/abc123-def456-ghi789/cancel
```

### 6. Revert to Default

```bash
curl -X POST http://localhost:5005/api/ml/model/revert
```

## Testing

### Unit Tests (Aura.Tests/MlTrainingTests.cs)

**Annotation Storage (6 tests):**
- Store and retrieve annotations
- Get statistics
- Validate ratings (0-1 range)
- Validate frame paths (not empty)
- Clear annotations

**Model Manager (3 tests):**
- Deploy model with backup
- Revert to default
- Fallback to default when active missing

**Training Worker (2 tests):**
- Submit job returns job ID
- Get job status returns correct data

### Integration Tests (Aura.Tests/MlTrainingIntegrationTests.cs)

**End-to-End Workflows (5 tests):**
- Complete training workflow with small dataset
- Job cancellation
- Insufficient data handling
- Model deployment and revert
- Multiple jobs sequential execution

### Manual Testing

Run the manual test script:

```bash
chmod +x manual-test-ml-training.sh
./manual-test-ml-training.sh
```

Tests covered:
- Upload valid annotations
- Get statistics
- Start training and monitor progress
- Test with insufficient data
- Test with invalid ratings
- Test with empty frame paths
- Revert to default model
- Job cancellation

## Service Registration

All services are registered in `Aura.Api/Program.cs`:

```csharp
// Register ML Training services
builder.Services.AddSingleton<Aura.Core.Services.ML.AnnotationStorageService>();
builder.Services.AddSingleton<Aura.Core.Services.ML.ModelTrainingService>();
builder.Services.AddSingleton<Aura.Core.ML.ModelManager>();
builder.Services.AddSingleton<Aura.Core.Services.ML.MlTrainingWorker>();
```

## Error Handling

### Common Errors

**Insufficient Data:**
```json
{
  "title": "Insufficient Data",
  "status": 400,
  "detail": "No annotations available for training. Please upload annotations first.",
  "correlationId": "xyz789"
}
```

**Invalid Rating:**
```json
{
  "title": "Invalid Annotation Data",
  "status": 400,
  "detail": "Rating must be between 0.0 and 1.0, got 1.5",
  "correlationId": "xyz789"
}
```

**Job Not Found:**
```json
{
  "title": "Job Not Found",
  "status": 404,
  "detail": "Training job abc123 does not exist",
  "correlationId": "xyz789"
}
```

### Logging

All operations are logged with structured logging:

```
[2025-11-03 14:35:00.123] [INF] [xyz789] Uploading 5 annotations
[2025-11-03 14:35:01.456] [INF] [xyz789] Successfully uploaded 5 annotations for user default-user
[2025-11-03 14:35:10.789] [INF] [abc123] Training job abc123 submitted for user default-user
[2025-11-03 14:35:12.012] [INF] [abc123] Job abc123: Loaded 5 annotations
[2025-11-03 14:35:15.345] [INF] [abc123] Training job abc123 completed successfully
```

## Security Considerations

### Current Implementation

- **User ID**: Currently hardcoded to "default-user" in GetUserId()
- **Authentication**: Not implemented (backend foundation only)
- **Authorization**: Not implemented (backend foundation only)

### Future Enhancements

- Implement authentication middleware
- Extract user ID from JWT token or session
- Add authorization checks for annotation access
- Rate limiting on training job submission
- Validate file paths to prevent directory traversal

## Performance Considerations

### Annotation Storage

- JSONL format allows efficient appending
- Per-user files prevent large file issues
- Consider implementing pagination for large annotation sets

### Training Execution

- Semaphore limits to one concurrent training job
- Progress reporting via polling (future: SSE)
- Consider implementing job priority queue

### Model Deployment

- Atomic file operations prevent corruption
- Backups ensure rollback capability
- Consider implementing model versioning system

## Future Enhancements

### Planned Features

1. **Server-Sent Events (SSE)**: Real-time progress updates instead of polling
2. **Annotation Sampling**: Intelligent frame selection for annotation
3. **Model Versioning**: Track multiple model versions with metadata
4. **Batch Training**: Train multiple models in parallel
5. **Advanced Metrics**: ROC curves, confusion matrices, validation metrics
6. **Model Comparison**: A/B testing framework for model evaluation
7. **Auto-Retraining**: Scheduled retraining based on new annotations
8. **Export/Import**: Share annotation datasets between users

### Known Limitations

1. **Single Training Job**: Only one job can run at a time
2. **No Persistent Queue**: Job queue is in-memory (lost on restart)
3. **Limited Validation**: Basic model validation (file exists, non-zero size)
4. **No Model Versioning**: Only keeps latest backup, not full history
5. **Hardcoded User ID**: No authentication/authorization implemented

## Troubleshooting

### Training Job Stuck in Running State

**Cause:** Training service crashed or hung

**Solution:**
1. Check logs for errors: `logs/aura-api-*.log`
2. Restart the API service
3. Resubmit the training job

### Model Deployment Failed

**Cause:** Insufficient disk space, permissions, or model validation failed

**Solution:**
1. Check disk space
2. Verify write permissions to ML/PretrainedModels directory
3. Check logs for validation errors
4. Restore from backup: POST `/api/ml/model/revert`

### Annotations Not Loading

**Cause:** JSONL file corrupted or invalid JSON

**Solution:**
1. Check annotation file: `%AppData%/Aura/ML/Annotations/{userId}/annotations.jsonl`
2. Validate JSON format
3. Clear corrupted annotations: DELETE annotations file and reupload

## Summary

The ML training backend provides a complete foundation for in-app model retraining:

✅ **Complete API**: All required endpoints implemented  
✅ **Robust Storage**: Per-user JSONL with validation  
✅ **Safe Deployment**: Atomic swap with backup and rollback  
✅ **Error Handling**: Comprehensive error handling with ProblemDetails  
✅ **Testing**: 16 tests with 100% pass rate  
✅ **Documentation**: Complete guide and manual test script  

The implementation is production-ready for the backend foundation, with clear extension points for future UI integration and advanced features.
