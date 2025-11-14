#!/bin/bash

# Aura Video Studio - First Time Setup Script
# This script builds the frontend and backend in the correct order

set -e # Exit on error

echo "======================================"
echo "Aura Video Studio - First Time Setup"
echo "======================================"
echo ""

# Check Node.js
if ! command -v node &>/dev/null; then
  echo "❌ Error: Node.js is not installed"
  echo "Please install Node.js 18.0.0+ from https://nodejs.org/"
  exit 1
fi

NODE_VERSION=$(node --version)
echo "✓ Node.js found: $NODE_VERSION"

# Check .NET
if ! command -v dotnet &>/dev/null; then
  echo "❌ Error: .NET SDK is not installed"
  echo "Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
  exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✓ .NET SDK found: $DOTNET_VERSION"

echo ""
echo "======================================"
echo "Step 1: Building Frontend"
echo "======================================"
cd Aura.Web

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
  echo "Installing npm dependencies..."
  npm install
else
  echo "✓ npm dependencies already installed"
fi

echo "Building frontend (this may take a moment)..."
npm run build

if [ ! -d "dist" ] || [ ! -f "dist/index.html" ]; then
  echo "❌ Frontend build failed - dist folder not created"
  exit 1
fi

echo "✓ Frontend built successfully"
echo ""

echo "======================================"
echo "Step 2: Building Backend"
echo "======================================"
cd ..

echo "Building .NET solution..."
dotnet build Aura.sln --configuration Release

echo "✓ Backend built successfully"
echo ""

echo "======================================"
echo "Step 3: Verifying Setup"
echo "======================================"

WWWROOT_PATH="Aura.Api/bin/Release/net8.0/wwwroot"
if [ ! -d "$WWWROOT_PATH" ] || [ ! -f "$WWWROOT_PATH/index.html" ]; then
  echo "❌ Warning: Frontend not copied to wwwroot"
  echo "The build process should have copied dist to wwwroot automatically."
  exit 1
fi

echo "✓ Frontend copied to wwwroot"
echo "✓ Setup complete!"
echo ""

echo "======================================"
echo "Ready to Run!"
echo "======================================"
echo ""
echo "To start the application:"
echo "  cd Aura.Api"
echo "  dotnet run --configuration Release"
echo ""
echo "Then open your browser to: http://127.0.0.1:5005"
echo ""
echo "For development mode (with hot reload), see FIRST_RUN_GUIDE.md"
echo ""
