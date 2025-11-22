#!/bin/bash
# Manual test script for directory validation with environment variables
# This script tests the /api/setup/check-directory endpoint

API_BASE_URL="http://localhost:5005"

echo "=========================================="
echo "Manual Test: Directory Validation"
echo "=========================================="
echo ""

# Test 1: Check directory with environment variable (Windows)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo "Test 1: Windows environment variable (%TEMP%)"
    curl -X POST "$API_BASE_URL/api/setup/check-directory" \
         -H "Content-Type: application/json" \
         -d '{"path":"%TEMP%"}' \
         -w "\nHTTP Status: %{http_code}\n\n"
    
    echo "Test 2: Windows environment variable (%USERPROFILE%\\Videos\\Aura)"
    curl -X POST "$API_BASE_URL/api/setup/check-directory" \
         -H "Content-Type: application/json" \
         -d '{"path":"%USERPROFILE%\\Videos\\Aura"}' \
         -w "\nHTTP Status: %{http_code}\n\n"
fi

# Test 3: Unix tilde expansion
if [[ "$OSTYPE" != "msys" && "$OSTYPE" != "win32" ]]; then
    echo "Test 3: Unix tilde expansion (~)"
    curl -X POST "$API_BASE_URL/api/setup/check-directory" \
         -H "Content-Type: application/json" \
         -d '{"path":"~"}' \
         -w "\nHTTP Status: %{http_code}\n\n"
    
    echo "Test 4: Unix tilde expansion (~/Videos/Aura)"
    curl -X POST "$API_BASE_URL/api/setup/check-directory" \
         -H "Content-Type: application/json" \
         -d '{"path":"~/Videos/Aura"}' \
         -w "\nHTTP Status: %{http_code}\n\n"
    
    echo "Test 5: Unix environment variable (\$HOME/Videos/Aura)"
    curl -X POST "$API_BASE_URL/api/setup/check-directory" \
         -H "Content-Type: application/json" \
         -d "{\"path\":\"\$HOME/Videos/Aura\"}" \
         -w "\nHTTP Status: %{http_code}\n\n"
fi

# Test 6: Invalid path
echo "Test 6: Invalid path (should fail)"
curl -X POST "$API_BASE_URL/api/setup/check-directory" \
     -H "Content-Type: application/json" \
     -d '{"path":""}' \
     -w "\nHTTP Status: %{http_code}\n\n"

# Test 7: Complete setup with environment variable
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo "Test 7: Complete setup with %TEMP%\\AuraTest"
    curl -X POST "$API_BASE_URL/api/setup/complete" \
         -H "Content-Type: application/json" \
         -d '{"outputDirectory":"%TEMP%\\AuraTest"}' \
         -w "\nHTTP Status: %{http_code}\n\n"
else
    echo "Test 7: Complete setup with ~/Videos/AuraTest"
    curl -X POST "$API_BASE_URL/api/setup/complete" \
         -H "Content-Type: application/json" \
         -d '{"outputDirectory":"~/Videos/AuraTest"}' \
         -w "\nHTTP Status: %{http_code}\n\n"
fi

echo ""
echo "=========================================="
echo "Tests Complete!"
echo "=========================================="
echo ""
echo "Expected Results:"
echo "  - Tests 1-5: Should return isValid=true with expandedPath"
echo "  - Test 6: Should return isValid=false with error message"
echo "  - Test 7: Should return success=true and create the directory"
echo ""
echo "Check the backend logs for detailed information about path expansion."
