#!/bin/bash
set -e

echo "Setting up Aura Video Studio development environment..."

# Restore .NET dependencies
echo "Restoring .NET dependencies..."
dotnet restore

# Install Node.js dependencies for web frontend
echo "Installing Node.js dependencies..."
cd Aura.Web
npm ci
cd ..

# Set up git hooks
echo "Setting up git hooks..."
if [ -d .husky ]; then
  chmod +x .husky/*
fi

# Create necessary directories
mkdir -p logs
mkdir -p output
mkdir -p temp

echo "Development environment setup complete!"
echo ""
echo "To start developing:"
echo "  1. Run 'make dev' to start all services"
echo "  2. Open http://localhost:3000 in your browser"
echo ""
echo "Common commands:"
echo "  - make help    : Show all available commands"
echo "  - make test    : Run all tests"
echo "  - make logs    : View service logs"
echo "  - make health  : Check service health"
