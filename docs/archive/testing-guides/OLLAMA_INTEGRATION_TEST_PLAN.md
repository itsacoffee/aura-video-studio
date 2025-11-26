# Ollama Integration Test Plan

## Overview
This document outlines comprehensive testing for Ollama integration across all LLM/RAG usage points in the application.

## Test Areas

### 1. Script Generation
**Endpoint**: `POST /api/scripts/generate`
**Provider**: `ScriptOrchestrator` → `OllamaLlmProvider`
**Test Cases**:
- [ ] Generate script with Ollama when selected as provider
- [ ] Verify availability check runs before generation
- [ ] Test error handling when Ollama is not running
- [ ] Test retry logic with exponential backoff
- [ ] Verify streaming generation works (`POST /api/scripts/generate/stream`)
- [ ] Test model override functionality
- [ ] Verify LLM parameters (temperature, top_p, top_k, max_tokens) are passed correctly

### 2. Ideation Service
**Endpoints**: 
- `POST /api/ideation/brainstorm`
- `POST /api/ideation/expand-brief`
- `POST /api/ideation/gap-analysis`
- `POST /api/ideation/research`
- `POST /api/ideation/storyboard`
- `POST /api/ideation/refine`
- `POST /api/ideation/questions`
- `POST /api/ideation/idea-to-brief`
- `POST /api/ideation/enhance-topic`

**Provider**: `IdeationService` → `LlmStageAdapter` → `OllamaLlmProvider` (via factory)
**Test Cases**:
- [ ] Test each ideation endpoint with Ollama
- [ ] Verify RAG context is retrieved when available
- [ ] Test fallback to direct provider when orchestrator fails
- [ ] Verify conversation context is maintained
- [ ] Test error handling and graceful degradation

### 3. RAG Integration
**Usage Points**:
- Script generation with RAG enabled
- Ideation with RAG context
- Video orchestrator with RAG

**Test Cases**:
- [ ] Verify RAG context is built before LLM call
- [ ] Test RAG-enhanced prompts are sent to Ollama
- [ ] Verify citations are included when configured
- [ ] Test RAG with multiple document sources
- [ ] Verify RAG works with Ollama streaming

### 4. Connection & Availability Checks
**Components**: `OllamaLlmProvider.IsServiceAvailableAsync()`, `OllamaScriptProvider.IsServiceAvailableAsync()`
**Test Cases**:
- [ ] Test availability check with Ollama running
- [ ] Test availability check with Ollama stopped
- [ ] Verify timeout handling (15s primary, 10s fallback)
- [ ] Test both `/api/version` and `/api/tags` endpoints
- [ ] Verify proper error messages when unavailable
- [ ] Test connection diagnostics provide helpful information

### 5. Error Handling & Fallbacks
**Test Cases**:
- [ ] Test fallback to RuleBased when Ollama fails
- [ ] Verify error messages are user-friendly
- [ ] Test retry logic (max 3 retries with exponential backoff)
- [ ] Test timeout scenarios (300s timeout for generation)
- [ ] Verify model not found errors are handled
- [ ] Test connection refused errors

### 6. Streaming Support
**Endpoints**: `POST /api/scripts/generate/stream`
**Test Cases**:
- [ ] Verify streaming works with Ollama
- [ ] Test real-time token-by-token updates
- [ ] Verify progress callbacks are fired
- [ ] Test cancellation during streaming
- [ ] Verify streaming error handling

### 7. Model Management
**Endpoints**: `GET /api/ollama/models`
**Test Cases**:
- [ ] Verify model list is fetched correctly
- [ ] Test model selection in UI
- [ ] Verify default model is used when none selected
- [ ] Test model refresh functionality
- [ ] Verify model override in requests

## Integration Points Verification

### Script Generation Flow
1. User selects Ollama provider in UI
2. `ScriptReview.tsx` calls `generateScript()` API
3. `ScriptsController.GenerateScript()` receives request
4. `ScriptOrchestrator.GenerateScriptAsync()` routes to provider
5. `OllamaLlmProvider.DraftScriptAsync()` is called
6. Availability check runs (`IsServiceAvailableAsync()`)
7. Request sent to Ollama API (`/api/generate`)
8. Response parsed and returned
9. Script scenes created and displayed

### Ideation Flow
1. User triggers ideation feature (brainstorm, expand, etc.)
2. `IdeationController` receives request
3. `IdeationService` method called (e.g., `BrainstormConceptsAsync()`)
4. RAG context retrieved if available
5. `GenerateWithLlmAsync()` called
6. `LlmStageAdapter` or direct `_llmProvider` used
7. `OllamaLlmProvider.DraftScriptAsync()` called
8. Response parsed and returned

### RAG Integration Flow
1. RAG enabled in brief configuration
2. `RagContextBuilder.BuildContextAsync()` retrieves relevant chunks
3. RAG context added to prompt
4. Enhanced prompt sent to Ollama
5. Response includes citations if configured

## Manual Testing Checklist

### Prerequisites
- [ ] Ollama installed and running (`ollama serve`)
- [ ] At least one model pulled (e.g., `ollama pull qwen3:4b`)
- [ ] Application running and connected

### Test Script Generation
1. [ ] Navigate to Create Video wizard
2. [ ] Fill in brief (topic, audience, goal)
3. [ ] Select Ollama as LLM provider
4. [ ] Select model (e.g., qwen3:4b)
5. [ ] Click "Generate Script"
6. [ ] Verify script is generated successfully
7. [ ] Check logs for availability check
8. [ ] Verify script scenes are created

### Test Ideation
1. [ ] Navigate to Ideation page
2. [ ] Enter a topic
3. [ ] Click "Brainstorm"
4. [ ] Verify concepts are generated
5. [ ] Test other ideation features (expand, research, etc.)
6. [ ] Verify all use Ollama when available

### Test Error Scenarios
1. [ ] Stop Ollama service
2. [ ] Attempt script generation
3. [ ] Verify clear error message displayed
4. [ ] Verify fallback to RuleBased works
5. [ ] Restart Ollama
6. [ ] Verify generation works again

### Test RAG Integration
1. [ ] Upload documents to RAG index
2. [ ] Enable RAG in brief configuration
3. [ ] Generate script with RAG enabled
4. [ ] Verify RAG context is included
5. [ ] Check citations if configured

## Automated Test Recommendations

### Unit Tests
- `OllamaLlmProviderTests.cs` - Test availability checks
- `OllamaScriptProviderTests.cs` - Test script generation
- `IdeationServiceTests.cs` - Test ideation with Ollama
- `ScriptOrchestratorTests.cs` - Test provider routing

### Integration Tests
- End-to-end script generation with Ollama
- Ideation flow with Ollama
- RAG integration with Ollama
- Error handling and fallbacks

## Success Criteria

✅ All script generation endpoints work with Ollama
✅ All ideation endpoints work with Ollama  
✅ RAG integration works correctly with Ollama
✅ Availability checks are reliable and fast
✅ Error messages are clear and actionable
✅ Fallback to RuleBased works when Ollama unavailable
✅ Streaming generation works correctly
✅ Model selection and override work correctly
✅ Connection diagnostics provide helpful information

## Known Issues Fixed

1. ✅ Improved `IsServiceAvailableAsync()` with better timeout handling
2. ✅ Added detailed error logging for connection failures
3. ✅ Fixed HttpClient timeout conflicts
4. ✅ Improved error messages with diagnostics
5. ✅ Fixed cost estimator to check actual image provider

## Next Steps

1. Run manual tests following checklist above
2. Verify all integration points work correctly
3. Test error scenarios and fallbacks
4. Verify RAG integration works end-to-end
5. Document any remaining issues

