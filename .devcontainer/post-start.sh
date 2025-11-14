#!/bin/bash
set -e

echo "Starting development environment..."

# Check service health
echo "Checking Redis connectivity..."
if redis-cli ping >/dev/null 2>&1; then
  echo "✓ Redis is running"
else
  echo "⚠ Redis is not accessible"
fi

# Check FFmpeg
echo "Checking FFmpeg..."
if ffmpeg -version >/dev/null 2>&1; then
  echo "✓ FFmpeg is available"
else
  echo "⚠ FFmpeg is not available"
fi

echo ""
echo "Development environment is ready!"
echo "Run 'make dev' to start the application"
