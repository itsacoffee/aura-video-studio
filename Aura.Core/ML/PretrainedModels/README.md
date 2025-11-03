# Pre-trained ML Models

This directory contains pre-trained ML.NET models for video pacing optimization.

## Models

### frame-importance-model.zip
- **Purpose**: Predicts the importance/relevance of video frames for selection
- **Input Features**:
  - Visual complexity (0-1)
  - Color distribution (hue, saturation, variance)
  - Edge density (0-1)
  - Brightness level (0-1)
  - Contrast level (0-1)
  - Frame aspect ratio
  - Key frame indicator (boolean)
- **Output**: Importance score (0-1)
- **Training Data**: Placeholder - to be trained on annotated video frames
- **Model Type**: Regression (ML.NET FastTree)

### engagement-prediction-model.zip
- **Purpose**: Predicts viewer engagement and attention for video segments
- **Input Features**:
  - Words per second
  - Scene length
  - Relative position in video
  - Word count
  - Average word length
  - Opening/closing indicators
- **Output**: Engagement score (0-1)
- **Training Data**: Placeholder - to be trained on viewer analytics data
- **Model Type**: Regression (ML.NET FastTree)

## Training

### In-App Training (Recommended)

The frame importance model can now be retrained directly through the API using user annotations:

1. **Upload annotations**: POST `/api/ml/annotations/upload`
2. **Check stats**: GET `/api/ml/annotations/stats`
3. **Start training**: POST `/api/ml/train/frame-importance`
4. **Monitor progress**: GET `/api/ml/train/{jobId}/status`
5. **Revert if needed**: POST `/api/ml/model/revert`

Trained models are automatically deployed with backup and rollback support.

### Legacy Training Scripts

For batch training, use the scripts in `/Scripts/ModelTraining/`:

```bash
# Train frame importance model
dotnet run --project Scripts/ModelTraining -- train-frame-importance --data-path ./training-data/frames.csv

# Train engagement prediction model
dotnet run --project Scripts/ModelTraining -- train-engagement --data-path ./training-data/engagement.csv
```

## Usage

Models are loaded automatically by the respective services:
- `FrameAnalysisService` uses `frame-importance-model.zip`
- `AttentionPredictionService` uses `engagement-prediction-model.zip`

## Model Version

Current version: 1.0.0-placeholder
Last updated: 2025-10-23

## Model Management

### Active vs Default Models

- `frame-importance-model.zip`: Active model (user-trained or default)
- `frame-importance-model-default.zip`: Factory default model
- `frame-importance-model.zip.backup`: Previous version backup

The `ModelManager` class automatically handles:
- Atomic model deployment with backup
- Validation before activation
- Fallback to default if active model is invalid
- Rollback to previous version

### Model Storage Structure

```
Aura.Core/ML/PretrainedModels/
├── frame-importance-model-default.zip  (factory default)
├── frame-importance-model.zip          (active model)
├── frame-importance-model.zip.backup   (rollback backup)
└── engagement-prediction-model.zip     (other models)
```

User annotations are stored per-user:
```
%AppData%/Aura/ML/Annotations/{userId}/
└── annotations.jsonl
```

## Notes

- These are placeholder files. In production, replace with actual trained models.
- Models are retrained with user annotations through the ML API endpoints.
- Active models are validated before deployment with automatic fallback.
- Consider A/B testing model versions before production deployment.
