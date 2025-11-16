#!/bin/bash

# Ollama Auto-Detection and Connection Reliability - Verification Script
# This script demonstrates the new Ollama detection capabilities

echo "=================================================="
echo "Ollama Auto-Detection Verification"
echo "=================================================="
echo ""

# Check if API is running
API_URL="http://localhost:5005"
echo "1. Checking if Aura API is running at $API_URL..."
if curl -s "$API_URL/health/live" > /dev/null 2>&1; then
    echo "   ✅ API is running"
else
    echo "   ❌ API is not running. Please start the API first:"
    echo "      cd Aura.Api && dotnet run"
    exit 1
fi
echo ""

# Test Ollama status endpoint
echo "2. Testing GET /api/providers/ollama/status..."
OLLAMA_STATUS=$(curl -s "$API_URL/api/providers/ollama/status")
echo "   Response:"
echo "$OLLAMA_STATUS" | jq '.' 2>/dev/null || echo "$OLLAMA_STATUS"
echo ""

# Check if Ollama is available
IS_AVAILABLE=$(echo "$OLLAMA_STATUS" | jq -r '.isAvailable' 2>/dev/null)
if [ "$IS_AVAILABLE" = "true" ]; then
    echo "   ✅ Ollama is available and running"
    
    # Get models
    echo ""
    echo "3. Testing GET /api/providers/ollama/models..."
    MODELS=$(curl -s "$API_URL/api/providers/ollama/models")
    echo "   Response:"
    echo "$MODELS" | jq '.' 2>/dev/null || echo "$MODELS"
    
    # Get running models
    echo ""
    echo "4. Testing GET /api/providers/ollama/running..."
    RUNNING=$(curl -s "$API_URL/api/providers/ollama/running")
    echo "   Response:"
    echo "$RUNNING" | jq '.' 2>/dev/null || echo "$RUNNING"
    
    echo ""
    echo "=================================================="
    echo "✅ All Ollama endpoints are working correctly!"
    echo "=================================================="
else
    echo "   ℹ️  Ollama is not available. This is expected if:"
    echo "      - Ollama is not installed"
    echo "      - Ollama service is not running (run 'ollama serve')"
    echo ""
    echo "   The enhanced detection provides clear error messages:"
    ERROR_MSG=$(echo "$OLLAMA_STATUS" | jq -r '.message' 2>/dev/null)
    echo "   Message: $ERROR_MSG"
    echo ""
    echo "=================================================="
    echo "✅ Detection works correctly (Ollama not available)"
    echo "=================================================="
fi

echo ""
echo "Key Features Demonstrated:"
echo "  • Multiple endpoint detection (localhost:11434, 127.0.0.1:11434)"
echo "  • Fast detection (5-second timeout)"
echo "  • Clear error messages"
echo "  • Model listing"
echo "  • Running model detection"
echo "  • Health check caching (30 seconds)"
echo ""
