# Manual Testing Guide: OpenAI API Key Validation Fix

## Overview
This guide helps verify that the API key validation fix (camelCase JSON serialization) is working correctly in the running application.

## Prerequisites
- Running Aura.Api backend (http://localhost:5005 or configured port)
- Running Aura.Web frontend  (http://localhost:5173 or configured port)
- Valid OpenAI API key for testing (starts with `sk-` or `sk-proj-`)

## Test Procedure

### 1. Test via First-Run Wizard
1. Clear application data to trigger first-run experience:
   - Windows: Delete `%APPDATA%\Aura` folder
   - Linux/Mac: Delete `~/.config/Aura` folder
2. Start the application
3. Navigate through the wizard to the "API Key Setup" step
4. Enter a valid OpenAI API key in the OpenAI provider section
5. Click "Validate" button
6. **Expected Result**: Status should show "Valid ✓" with green badge
7. **Previous Bug**: Would show "Invalid ✕" immediately or show error "undefined"

### 2. Test via Settings Page
1. Open the application
2. Navigate to Settings → API Keys
3. Find the OpenAI section
4. Enter a valid OpenAI API key
5. Click "Test Connection" or "Validate"
6. **Expected Result**: Success message "API key is valid and verified with OpenAI"
7. **Previous Bug**: Would show error immediately even with valid key

### 3. Test API Endpoint Directly (Developer Test)

Using cURL or Postman:

```bash
# Test the OpenAI validation endpoint
curl -X POST http://localhost:5005/api/providers/openai/validate \
  -H "Content-Type: application/json" \
  -d '{
    "apiKey": "sk-YOUR_ACTUAL_OPENAI_KEY_HERE"
  }'
```

**Expected Response Format (camelCase)**:
```json
{
  "isValid": true,
  "status": "Valid",
  "message": "API key is valid and verified with OpenAI.",
  "correlationId": "...",
  "details": {
    "provider": "OpenAI",
    "keyFormat": "valid",
    "formatValid": true,
    "networkCheckPassed": true,
    "httpStatusCode": 200,
    "responseTimeMs": 150,
    "diagnosticInfo": "Validated successfully after 1 attempts"
  }
}
```

**IMPORTANT**: All property names should be camelCase (isValid, not IsValid).

### 4. Browser DevTools Test
1. Open browser DevTools (F12)
2. Go to Network tab
3. Perform API key validation (as in Test 1 or 2)
4. Find the `/api/providers/openai/validate` request
5. Inspect the Response
6. **Verify**: All JSON properties are camelCase
7. **Previous Bug**: Properties were PascalCase, causing frontend to not find them

## What to Look For

### Success Indicators
✅ Validation completes without errors  
✅ Valid keys show "Valid" status with green badge  
✅ Invalid keys show clear error messages (e.g., "Invalid authentication")  
✅ Network tab shows response with camelCase properties  
✅ No console errors in browser DevTools  

### Failure Indicators (If Bug Still Exists)
❌ Validation fails immediately even with valid key  
❌ Console shows "isValid is undefined" errors  
❌ Network response shows PascalCase properties (IsValid instead of isValid)  
❌ Validation status stuck in "Validating..." state  

## Additional Test Cases

### Test Invalid Key
1. Enter an invalid OpenAI key (e.g., `sk-invalid123`)
2. Click Validate
3. **Expected**: Clear error message "Invalid authentication" or similar
4. **Should NOT**: Show generic "undefined" error

### Test Timeout/Network Error  
1. Disconnect internet
2. Try to validate an API key
3. **Expected**: Message about network connectivity or offline mode
4. **Should NOT**: Crash or show undefined errors

### Test Rate Limiting
1. Validate the same key multiple times quickly (5+ times)
2. **Expected**: Either succeeds or shows rate limit message
3. **Should NOT**: Show undefined errors

## Rollback Procedure
If the fix causes issues:

1. Revert the change in `Aura.Api/Program.cs`:
```csharp
// Remove this line:
options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
```

2. Rebuild the API:
```bash
dotnet build Aura.Api/Aura.Api.csproj -c Release
```

3. Restart the backend

## Troubleshooting

### "isValid is undefined" still appearing
- Check that backend was restarted after the fix
- Verify Program.cs has the camelCase policy
- Check Network tab to see actual JSON response format

### All validations failing
- Check backend logs for errors
- Verify OpenAI API service is accessible
- Test with a known-good API key from OpenAI platform

### Frontend not receiving response
- Check CORS configuration
- Verify API is running on expected port
- Check for circuit breaker state (may need reset)

## Related Files
- **Backend**: `Aura.Api/Program.cs` - JSON serialization configuration
- **Frontend**: `Aura.Web/src/state/onboarding.ts` - Validation logic (line 816)
- **Frontend**: `Aura.Web/src/services/openAIValidationService.ts` - Service layer
- **Backend**: `Aura.Api/Controllers/ProvidersController.cs` - API endpoint
- **Backend**: `Aura.Core/Services/Providers/OpenAIKeyValidationService.cs` - Validation logic

## Success Criteria
The fix is considered successful when:
1. Valid OpenAI API keys validate successfully on first try
2. Error messages are clear and actionable
3. No "undefined" errors in console
4. JSON responses use camelCase throughout
5. User can complete first-run wizard without issues
