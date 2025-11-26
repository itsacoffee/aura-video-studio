# Ollama Integration Fix - Testing Guide

## Overview
This guide provides comprehensive testing procedures for the Ollama LLM provider integration fix.

## Changes Summary

### Primary Fix
Fixed two critical issues in Ollama provider availability checks:

1. **Independent Cancellation Tokens**: Availability checks now use independent `CancellationTokenSource` instances instead of linked tokens, preventing premature cancellation from parent HTTP request contexts.

2. **Result Caching**: Availability check results are now cached for 30 seconds, reducing redundant network calls and improving performance.

## Prerequisites

### Required Software
- Ollama installed and accessible
- .NET 8 SDK
- Running instance of Aura Video Studio backend

### Ollama Setup
```bash
# Install Ollama (if not already installed)
# Visit: https://ollama.com

# Pull a model (if not already downloaded)
ollama pull llama3.1:8b-q4_k_m

# Start Ollama service
ollama serve
```

## Testing Procedures

### Test 1: Basic Diagnostic Check

**Objective**: Verify Ollama endpoints are accessible

**Steps**:
1. Ensure Ollama is running (`ollama serve`)
2. Run the PowerShell diagnostic script:
   ```powershell
   .\test-ollama-provider.ps1
   ```
3. Verify all checks pass:
   - ✓ Ollama is running
   - ✓ Models are available
   - ✓ Simple generation works

**Expected Result**: All three checks should pass with green checkmarks

---

### Test 2: Script Generation via UI

**Objective**: Test Ollama provider through the main application workflow

**Steps**:
1. Start the Aura Video Studio application
2. Navigate to "Create" wizard
3. Proceed to Step 3 of 5 (Script Generation)
4. Select "Ollama" as the LLM provider
5. Configure brief parameters:
   - Topic: "Introduction to AI"
   - Audience: "Students"
   - Goal: "Educate"
   - Tone: "Friendly"
   - Duration: 30 seconds
6. Click "Generate Script"

**Expected Result**:
- Script generates successfully without errors
- Response time: < 60 seconds (depends on model and hardware)
- Generated script appears in the preview area
- No error messages about Ollama connectivity

**Common Issues**:
- If generation fails, check logs for specific error messages
- Verify Ollama is still running (`ps aux | grep ollama` or Task Manager)
- Check that model is loaded (`ollama list`)

---

### Test 3: Caching Behavior

**Objective**: Verify availability check caching works correctly

**Steps**:
1. Complete Test 2 successfully
2. Immediately generate another script (same or different parameters)
3. Note the response time

**Expected Result**:
- Second generation should be slightly faster
- Logs should show "Using cached Ollama availability result" message
- No additional availability check network calls for 30 seconds

**Verification**:
Check backend logs for:
```
[Debug] Using cached Ollama availability result: True (age: 5s)
```

---

### Test 4: Failover and Recovery

**Objective**: Test error handling when Ollama becomes unavailable

**Steps**:
1. Generate a script successfully (establish baseline)
2. Stop Ollama service
3. Wait 31 seconds (for cache to expire)
4. Try to generate another script
5. Observe error message
6. Restart Ollama service
7. Wait 1-2 seconds for startup
8. Try to generate script again

**Expected Results**:
- Step 4: Should fail with clear error message: "Cannot connect to Ollama at http://127.0.0.1:11434..."
- Step 8: Should succeed once Ollama is fully started

**Important Notes**:
- The 30-second cache means Ollama being down won't be detected immediately
- After cache expires, the next request will perform a fresh availability check
- Ollama startup may take a few seconds before it's ready to accept requests

---

### Test 5: Timeout Resilience

**Objective**: Verify independent timeout handling

**Steps**:
1. Start Ollama with a slow/large model that takes time to load
2. Attempt script generation immediately after model loading starts
3. Monitor logs for availability check behavior

**Expected Result**:
- Availability check should complete within 15 seconds (primary + fallback)
- Should not fail due to parent request timeout
- May fail if model isn't loaded yet, but should succeed on retry after model loads

**Log Messages to Look For**:
```
[Information] Checking Ollama service availability at http://127.0.0.1:11434
[Information] Ollama service detected at http://127.0.0.1:11434 via /api/version
```

---

### Test 6: Integration Test Execution

**Objective**: Run automated integration tests

**Steps**:
1. Ensure Ollama is running
2. Navigate to test directory:
   ```bash
   cd Aura.Tests
   ```
3. Run Ollama integration tests:
   ```bash
   dotnet test --filter "FullyQualifiedName~OllamaProviderIntegrationTest"
   ```
4. Remove `Skip` attribute from tests in `OllamaProviderIntegrationTest.cs` if you want them to run automatically

**Expected Result**:
- `TestOllamaServiceAvailability`: Pass
- `TestOllamaScriptGeneration`: Pass
- Detailed logs in test output showing each step

---

## Troubleshooting

### Issue: "Cannot connect to Ollama"

**Possible Causes**:
1. Ollama not running
2. Wrong base URL configured
3. Firewall blocking localhost connections
4. Port 11434 in use by another application

**Solutions**:
```bash
# Check if Ollama is running
ps aux | grep ollama  # Linux/Mac
# or check Task Manager on Windows

# Verify Ollama is listening on correct port
curl http://127.0.0.1:11434/api/version

# Check configuration in appsettings.json
# Ensure "ollamaUrl": "http://127.0.0.1:11434"

# Test with alternative URLs
curl http://localhost:11434/api/version
```

### Issue: Generation Timeout

**Possible Causes**:
1. Model still loading
2. Model too large for available RAM
3. System under heavy load

**Solutions**:
- Wait for model to fully load before testing
- Use a smaller/faster model: `ollama pull llama3.2:3b`
- Check system resources (RAM, CPU)
- Increase timeout in provider configuration (currently 300s)

### Issue: Inconsistent Behavior

**Possible Causes**:
1. Cache timing issues
2. Ollama service restarting
3. Network latency

**Solutions**:
- Wait 31+ seconds between tests to ensure cache expiry
- Check Ollama logs: `journalctl -u ollama` (Linux) or service logs
- Verify stable network (even localhost can have issues with VPNs)

---

## Performance Benchmarks

### Expected Performance
| Operation | Expected Time | Notes |
|-----------|---------------|-------|
| First availability check | 0.1-0.5s | Direct network call |
| Cached availability check | < 0.001s | Memory lookup |
| Script generation (30s video) | 10-60s | Depends on model & hardware |
| Script generation (cached) | 10-60s | Same (actual generation not cached) |

### Hardware Dependencies
- **CPU**: Faster is better for Ollama
- **RAM**: 8GB minimum, 16GB+ recommended
- **Model Size**: Smaller models (3B, 8B) faster than large (70B)

---

## Validation Checklist

Before marking the fix as complete, verify:

- [ ] Ollama service is detected when running
- [ ] Script generation works through UI
- [ ] Script generation works via API
- [ ] Caching reduces redundant availability checks
- [ ] Error messages are clear when Ollama is unavailable
- [ ] Service recovery works after Ollama restart
- [ ] No timeout conflicts with parent cancellation tokens
- [ ] Integration tests pass (when enabled)
- [ ] Diagnostic script reports success
- [ ] Multiple consecutive generations work correctly
- [ ] No memory leaks from caching mechanism

---

## Monitoring and Logs

### Key Log Messages

**Success Path**:
```
[Information] Checking Ollama service availability at http://127.0.0.1:11434
[Information] Ollama service detected at http://127.0.0.1:11434 via /api/version
[Information] Generating script with Ollama (model: llama3.1:8b-q4_k_m) at http://127.0.0.1:11434 for topic: Introduction to AI
[Information] Script generated successfully with Ollama (1234 characters) in 15.3s
```

**Cached Check**:
```
[Debug] Using cached Ollama availability result: True (age: 5.2s)
[Information] Generating script with Ollama (model: llama3.1:8b-q4_k_m)...
```

**Failure Path**:
```
[Information] Checking Ollama service availability at http://127.0.0.1:11434
[Warning] Ollama /api/version endpoint failed: HttpRequestException: No connection could be made...
[Warning] Ollama connection refused at http://127.0.0.1:11434. Ensure Ollama is running: 'ollama serve'
[Error] Ollama availability check failed. Cannot connect to Ollama at http://127.0.0.1:11434. Please ensure Ollama is running...
```

---

## Success Criteria

The fix is considered successful when:

1. ✅ **Reliability**: Ollama provider works consistently when service is running
2. ✅ **Performance**: Caching reduces latency for repeated operations
3. ✅ **Resilience**: Independent timeouts prevent premature cancellation
4. ✅ **User Experience**: Clear error messages when Ollama is unavailable
5. ✅ **Recovery**: Service automatically works again after Ollama restart (after cache expiry)

---

## Next Steps

If issues persist after applying this fix:

1. **Capture Detailed Logs**: Enable Debug level logging for `Aura.Providers.Llm.OllamaLlmProvider`
2. **Network Analysis**: Use Wireshark or tcpdump to verify HTTP requests reach Ollama
3. **Ollama Logs**: Check Ollama service logs for request processing issues
4. **Configuration Review**: Verify all Ollama-related settings in appsettings.json
5. **Model Testing**: Try with different models to isolate model-specific issues

---

## Conclusion

This fix addresses the root causes of Ollama integration failures by:
- Implementing independent timeout management for availability checks
- Adding intelligent caching to reduce redundant network calls
- Maintaining existing functionality while improving reliability

The changes are minimal, focused, and production-ready with comprehensive error handling and logging.
