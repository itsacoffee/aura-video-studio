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

To train these models, use the scripts in `/Scripts/ModelTraining/`:

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

## Notes

- These are placeholder files. In production, replace with actual trained models.
- Models should be retrained periodically with new data to improve accuracy.
- Consider A/B testing model versions before deployment.
