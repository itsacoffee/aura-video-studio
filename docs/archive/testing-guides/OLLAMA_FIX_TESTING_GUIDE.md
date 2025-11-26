# Ollama Integration Fix - Testing Guide

This guide helps you verify that the Ollama integration bug fixes are working correctly.

## Problem Statement (Resolved)

The following issues have been fixed:

1. ✅ **Model Override Not Sent**: Frontend was not sending `modelOverride` when user selected a model that matched the default
2. ✅ **Insufficient Logging**: Hard to debug provider and model selection
3. ✅ **Incorrect Metadata**: Script metadata showed "default" instead of actual model used

## Prerequisites

### 1. Install and Start Ollama

```bash
# Install Ollama (if not already installed)
# Visit: https://ollama.com/download

# Start Ollama service
ollama serve
```

### 2. Pull a Test Model

```bash
# Pull qwen3:8b (or any model you want to test)
ollama pull qwen3:8b

# Verify model is available
ollama list
```

### 3. Build and Run Aura Video Studio

```bash
# Build backend
cd Aura.Api
dotnet build -c Release

# Build frontend
cd ../Aura.Web
npm ci
npm run build

# Run the application
cd ../Aura.Api
dotnet run
```

## Test Scenarios

### Test 1: Basic Model Override ✅

**Objective**: Verify that user-selected model is actually used

**Steps**:
1. Open Aura Video Studio UI
2. Navigate to script generation wizard
3. In the provider dropdown, select **"Ollama (qwen3:8b)"**
4. Fill in script details:
   - Topic: "Introduction to AI"
   - Duration: 60 seconds
5. Click "Generate Script"

**Expected Results**:
- ✅ Script generation starts
- ✅ Ollama process shows activity (check system monitor)
- ✅ Backend logs show:
  ```
  [INFO] Script generation requested. PreferredProvider: Ollama, ModelOverride: qwen3:8b
  [INFO] Generating script with Ollama (model: qwen3:8b)
  ```
- ✅ Script is generated successfully
- ✅ Script metadata shows: `"Model: qwen3:8b"`

**How to Check Logs**:
```bash
# View backend logs
tail -f logs/aura-api-*.log | grep -E "(ModelOverride|Ollama)"

# Or check console output if running with dotnet run
```

### Test 2: Different Models ✅

**Objective**: Verify that different models can be selected and used

**Steps**:
1. Pull multiple models:
   ```bash
   ollama pull llama3.2
   ollama pull qwen3:8b
   ollama pull mistral
   ```
2. For each model, repeat Test 1
3. Verify logs show the correct model being used

**Expected Results**:
- ✅ Each model selection results in that model being used
- ✅ Logs clearly show which model is being used
- ✅ Script metadata reflects the correct model

### Test 3: Provider Fallback (Ollama Unavailable) ✅

**Objective**: Verify graceful fallback when Ollama is not running

**Steps**:
1. Stop Ollama: `pkill ollama` or close the Ollama application
2. In UI, select "Ollama (qwen3:8b)" provider
3. Attempt to generate script

**Expected Results**:
- ✅ Error message: "Ollama service is not running. Please start Ollama or select a different provider."
- ✅ Backend logs show:
  ```
  [WARN] Ollama service not available at http://127.0.0.1:11434
  [ERROR] Requested provider 'Ollama' not available
  ```
- ✅ No silent failure
- ✅ User-friendly error message displayed

### Test 4: RuleBased Fallback ✅

**Objective**: Verify RuleBased fallback works when no providers available

**Steps**:
1. Ensure Ollama is stopped
2. Don't configure any API keys (OpenAI, Gemini, etc.)
3. Select "Free" tier or "Auto" in provider selection
4. Generate script

**Expected Results**:
- ✅ Falls back to RuleBased provider
- ✅ Script is generated with template-based approach
- ✅ Logs show: `ProviderUsed: RuleBased`
- ✅ No errors shown to user

### Test 5: Logging Verification ✅

**Objective**: Verify comprehensive logging at each layer

**Steps**:
1. Start Ollama with verbose logging:
   ```bash
   OLLAMA_DEBUG=1 ollama serve
   ```
2. Generate a script with model "qwen3:8b"
3. Check logs at each layer

**Expected Log Entries**:

**Frontend (Browser Console)**:
```
Generating script with provider: Ollama, model: qwen3:8b
```

**Backend API Controller**:
```
[INFO] Script generation requested. Topic: Introduction to AI, PreferredProvider: Ollama (resolved to: Ollama), ModelOverride: qwen3:8b
```

**Provider Orchestrator**:
```
[INFO] Selecting LLM provider for Script stage (preferred: Ollama)
[INFO] ✓ Provider Ollama is available and will be used
```

**Ollama LLM Provider**:
```
[INFO] Generating script with Ollama (model: qwen3:8b) at http://127.0.0.1:11434 for topic: Introduction to AI. ModelOverride: qwen3:8b, DefaultModel: llama3.1:8b-q4_k_m
```

**Ollama Script Provider**:
```
[INFO] Generating script with Ollama for topic: Introduction to AI. ModelOverride: qwen3:8b, DefaultModel: llama3.1:8b-q4_k_m, UsingModel: qwen3:8b
```

**Result**:
```
[INFO] Script generated successfully with provider Ollama, ID: <guid>
```

### Test 6: Model Metadata Display ✅

**Objective**: Verify UI shows correct model in metadata

**Steps**:
1. Generate script with model "qwen3:8b"
2. After generation completes, check script metadata section

**Expected Results**:
- ✅ Provider: "Ollama"
- ✅ Model: "qwen3:8b" (NOT "default" or "provider-default")
- ✅ Metadata accurately reflects what was used

## Verification Checklist

Use this checklist to confirm all fixes are working:

### Frontend
- [ ] Model override is ALWAYS sent when user selects a model
- [ ] Provider name is correctly normalized (e.g., "Ollama (qwen3:8b)" → "Ollama")
- [ ] Model selection dropdown shows available Ollama models
- [ ] Error messages are clear and helpful

### Backend
- [ ] ScriptsController logs show PreferredProvider and ModelOverride
- [ ] OllamaLlmProvider logs show which model is being used
- [ ] OllamaScriptProvider logs show ModelOverride, DefaultModel, UsingModel
- [ ] Script metadata captures actual model used

### Integration
- [ ] User-selected model is passed through entire stack
- [ ] Ollama API is called with correct model parameter
- [ ] Ollama process shows activity during generation
- [ ] Script is generated successfully
- [ ] Fallback works when Ollama unavailable

### Logging
- [ ] Correlation IDs present in all logs
- [ ] Each layer logs its actions
- [ ] Model information visible at each step
- [ ] Easy to trace request from UI to Ollama

## Common Issues and Solutions

### Issue: "Provider 'Ollama' not available"

**Possible Causes**:
1. Ollama is not running
2. Ollama is running on different port
3. Firewall blocking connection

**Solutions**:
```bash
# Check if Ollama is running
ps aux | grep ollama

# Check Ollama port
netstat -an | grep 11434

# Try accessing Ollama API directly
curl http://localhost:11434/api/tags

# Check firewall
sudo ufw status
```

### Issue: Model not found

**Error**: `Model 'qwen3:8b' not found`

**Solution**:
```bash
# List available models
ollama list

# Pull the model
ollama pull qwen3:8b

# Verify it's available
ollama list | grep qwen3
```

### Issue: Script metadata shows "provider-default"

**This is expected** when:
- No model was explicitly selected (user picked "Auto" or left default)
- Shows that provider's default model was used

**To see specific model**:
- Always select a specific model in the UI dropdown
- Check logs for actual model name

## Performance Testing

### Baseline Metrics

With Ollama on local hardware:
- **First Token Time**: 1-3 seconds (model loading)
- **Generation Speed**: 5-20 tokens/second (depending on hardware)
- **Total Time**: 30-120 seconds for 60-second script

### Monitoring Ollama Activity

```bash
# Monitor Ollama logs
tail -f ~/.ollama/logs/server.log

# Monitor system resources
htop
# or
nvidia-smi  # If using NVIDIA GPU
```

### Expected Behavior
- ✅ Ollama process CPU/GPU usage increases during generation
- ✅ Memory usage increases (model loaded into RAM/VRAM)
- ✅ Generation completes successfully
- ✅ Logs show token generation progress

## Debugging Tips

### Enable Verbose Logging

**Backend**:
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Aura.Providers.Llm": "Debug",
      "Aura.Core.Orchestrator": "Debug"
    }
  }
}
```

**Ollama**:
```bash
OLLAMA_DEBUG=1 ollama serve
```

**Frontend**:
```javascript
// Browser Console
localStorage.setItem('debug', 'aura:*');
```

### Check Network Traffic

```bash
# Monitor HTTP requests to Ollama
sudo tcpdump -i lo port 11434 -A

# Or use mitmproxy
mitmproxy -p 11434
```

### Verify Provider Registration

```csharp
// Check Program.cs registration
// Look for:
builder.Services.AddSingleton<OllamaLlmProvider>();
builder.Services.AddKeyedSingleton<ILlmProvider, OllamaLlmProvider>("Ollama");
```

## Success Indicators

✅ **Fix is Working When**:
1. Logs show: `ModelOverride: qwen3:8b` (your selected model)
2. Ollama process shows activity
3. Script is generated successfully
4. Script metadata shows: `Model: qwen3:8b`
5. No silent failures or fallbacks to RuleBased when Ollama is running

❌ **Fix NOT Working If**:
1. Logs show: `ModelOverride: null` when you selected a model
2. Script metadata shows: `Model: provider-default` for all generations
3. Ollama process shows no activity
4. Falls back to RuleBased even though Ollama is running

## Reporting Issues

If you encounter problems:

1. **Collect Logs**:
   ```bash
   # Backend logs
   cat logs/aura-api-*.log > issue-logs.txt
   
   # Ollama logs
   cat ~/.ollama/logs/server.log >> issue-logs.txt
   ```

2. **Include**:
   - Steps to reproduce
   - Expected vs actual behavior
   - Log excerpts showing the issue
   - System information (OS, .NET version, Node version)

3. **Check**:
   - Ollama is running: `curl http://localhost:11434/api/tags`
   - Model is available: `ollama list`
   - Backend is running: `curl http://localhost:5005/health/live`

## Additional Resources

- **Ollama Documentation**: https://github.com/ollama/ollama/blob/main/docs/api.md
- **Aura Architecture**: See `ARCHITECTURE_AND_TYPO_VERIFICATION.md`
- **Provider System**: See `docs/providers/README.md`
- **Logging Guide**: See `ERROR_HANDLING_GUIDE.md`

---

**Last Updated**: After PR implementing Ollama integration fixes  
**Related Files**:
- `Aura.Web/src/components/VideoWizard/steps/ScriptReview.tsx`
- `Aura.Api/Controllers/ScriptsController.cs`
- `Aura.Providers/Llm/OllamaLlmProvider.cs`
- `Aura.Providers/Llm/OllamaScriptProvider.cs`
