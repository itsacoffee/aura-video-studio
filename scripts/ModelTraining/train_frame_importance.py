#!/usr/bin/env python3
"""
Frame Importance Model Training Script

This script trains an ML model to predict frame importance scores
for video pacing optimization. In production, this would use actual
annotated training data.

Requirements:
    - Python 3.8+
    - scikit-learn
    - pandas
    - numpy

Usage:
    python train_frame_importance.py --data-path ./training-data/frames.csv --output-path ./models/frame-importance-model.pkl
"""

import argparse
import sys
import pandas as pd
import numpy as np
from pathlib import Path

try:
    from sklearn.ensemble import RandomForestRegressor
    from sklearn.model_selection import train_test_split
    from sklearn.metrics import mean_squared_error, r2_score
    import joblib
except ImportError:
    print("Error: Required packages not installed.")
    print("Install with: pip install scikit-learn pandas numpy joblib")
    sys.exit(1)


def load_training_data(data_path: Path) -> tuple:
    """Load and prepare training data."""
    print(f"Loading training data from {data_path}...")
    
    try:
        df = pd.read_csv(data_path)
    except FileNotFoundError:
        print(f"Error: Training data file not found: {data_path}")
        print("Creating sample training data...")
        df = create_sample_data()
    
    # Features: VisualComplexity, ColorVariance, EdgeDensity, Brightness, Contrast, AspectRatio
    feature_columns = [
        'VisualComplexity', 'ColorVariance', 'EdgeDensity', 
        'Brightness', 'Contrast', 'AspectRatio'
    ]
    
    X = df[feature_columns]
    y = df['ImportanceScore']  # Target: importance score (0-1)
    
    return X, y


def create_sample_data() -> pd.DataFrame:
    """Create sample training data for demonstration."""
    np.random.seed(42)
    n_samples = 1000
    
    data = {
        'FrameIndex': range(n_samples),
        'Timestamp': np.random.uniform(0, 300, n_samples),
        'IsKeyFrame': np.random.choice([True, False], n_samples),
        'VisualComplexity': np.random.uniform(0.3, 0.9, n_samples),
        'ColorVariance': np.random.uniform(0.2, 0.8, n_samples),
        'EdgeDensity': np.random.uniform(0.1, 0.9, n_samples),
        'Brightness': np.random.uniform(0.2, 0.9, n_samples),
        'Contrast': np.random.uniform(0.3, 0.8, n_samples),
        'AspectRatio': 1.778,  # 16:9 aspect ratio
    }
    
    # Generate synthetic importance scores based on features
    # Higher complexity, edges, and contrast = higher importance
    importance = (
        data['VisualComplexity'] * 0.3 +
        data['EdgeDensity'] * 0.25 +
        data['Contrast'] * 0.2 +
        data['ColorVariance'] * 0.15 +
        np.abs(data['Brightness'] - 0.5) * 0.1  # Moderate brightness is best
    )
    
    # Add key frame bonus
    importance += np.where(data['IsKeyFrame'], 0.15, 0)
    
    # Add some noise
    importance += np.random.normal(0, 0.05, n_samples)
    
    # Clip to [0, 1]
    data['ImportanceScore'] = np.clip(importance, 0, 1)
    
    return pd.DataFrame(data)


def train_model(X_train, y_train, X_test, y_test):
    """Train Random Forest regression model."""
    print("Training Random Forest model...")
    
    model = RandomForestRegressor(
        n_estimators=100,
        max_depth=10,
        min_samples_split=5,
        min_samples_leaf=2,
        random_state=42,
        n_jobs=-1
    )
    
    model.fit(X_train, y_train)
    
    # Evaluate
    train_pred = model.predict(X_train)
    test_pred = model.predict(X_test)
    
    train_mse = mean_squared_error(y_train, train_pred)
    test_mse = mean_squared_error(y_test, test_pred)
    train_r2 = r2_score(y_train, train_pred)
    test_r2 = r2_score(y_test, test_pred)
    
    print("\nTraining Results:")
    print(f"  Train MSE: {train_mse:.4f}")
    print(f"  Test MSE:  {test_mse:.4f}")
    print(f"  Train R²:  {train_r2:.4f}")
    print(f"  Test R²:   {test_r2:.4f}")
    
    # Feature importance
    feature_names = X_train.columns
    importances = model.feature_importances_
    
    print("\nFeature Importances:")
    for name, importance in sorted(zip(feature_names, importances), 
                                   key=lambda x: x[1], reverse=True):
        print(f"  {name:20s}: {importance:.4f}")
    
    return model


def save_model(model, output_path: Path):
    """Save trained model to disk."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(model, output_path)
    print(f"\nModel saved to: {output_path}")


def main():
    parser = argparse.ArgumentParser(
        description="Train frame importance prediction model"
    )
    parser.add_argument(
        "--data-path",
        type=Path,
        default=Path("training-data/frames.csv"),
        help="Path to training data CSV file"
    )
    parser.add_argument(
        "--output-path",
        type=Path,
        default=Path("models/frame-importance-model.pkl"),
        help="Path to save trained model"
    )
    parser.add_argument(
        "--test-size",
        type=float,
        default=0.2,
        help="Proportion of data to use for testing (default: 0.2)"
    )
    
    args = parser.parse_args()
    
    print("=" * 60)
    print("Frame Importance Model Training")
    print("=" * 60)
    
    # Load data
    X, y = load_training_data(args.data_path)
    
    print(f"Dataset size: {len(X)} samples")
    print(f"Features: {list(X.columns)}")
    
    # Split data
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=args.test_size, random_state=42
    )
    
    print(f"Training set: {len(X_train)} samples")
    print(f"Test set: {len(X_test)} samples")
    
    # Train model
    model = train_model(X_train, y_train, X_test, y_test)
    
    # Save model
    save_model(model, args.output_path)
    
    print("\n" + "=" * 60)
    print("Training complete!")
    print("=" * 60)


if __name__ == "__main__":
    main()
