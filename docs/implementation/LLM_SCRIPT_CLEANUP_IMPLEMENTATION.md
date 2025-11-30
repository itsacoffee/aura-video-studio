# LLM Script Output Cleanup - Implementation Summary

## Overview

Implemented comprehensive LLM script output cleanup for **ALL LLM providers** to remove meta-information that should not be included in TTS narration. This ensures clean, professional narration output regardless of which LLM provider is used.

## Problem

LLM providers (OpenAI, Anthropic, Gemini, Azure OpenAI, Ollama) sometimes add metadata to their script outputs, such as:
- Word Count: X
- TTS Pacing Check: ...
- AI Detection Avoided: ...
- Visual Synergy: ...
- Emotional Flow: ...
- Accuracy notes
- Various other meta-commentary

This metadata was previously only cleaned up for Ollama, but all LLM providers can produce similar artifacts. These need to be removed before TTS synthesis to ensure clean narration.

## Solution

Created a shared utility class `LlmScriptCleanup` that provides comprehensive cleanup for all LLM providers:

1. **Centralized Cleanup Utility** (`Aura.Core/Utilities/LlmScriptCleanup.cs`)
   - Contains all regex patterns for detecting LLM meta-information
   - Provides `IsMetadataLine()`, `IsLlmMetaCommentary()`, and `CleanNarration()` methods
   - Used consistently across all providers and script parsing locations

2. **Updated All Script Parsing Locations**
   - `VideoOrchestrator.ParseScriptIntoScenes()` - Enhanced with cleanup
   - `PipelineOrchestrationEngine.ParseScriptIntoScenes()` - Enhanced with cleanup
   - `VoiceStage.ParseScriptIntoScenes()` - Enhanced with cleanup
   - `EnhancedVideoOrchestrator.ParseScriptIntoScenes()` - Enhanced with cleanup
   - `ScriptsController.CleanupNarrationText()` - Now uses shared utility
   - `BaseLlmScriptProvider` - Updated to use shared utility
   - `OllamaLlmProvider` - Refactored to use shared utility (removed duplicate code)

3. **Comprehensive Pattern Detection**
   - Word Count patterns: `Word Count: X`, `Words: X`
   - TTS Pacing patterns: `TTS Pacing Check: ...`
   - AI Detection patterns: `AI Detection Avoided: ...`
   - Visual Synergy patterns: `Visual Synergy: ...`
   - Emotional Flow patterns: `Emotional Flow: ...`
   - Accuracy notes: `Accuracy: ...`
   - Meta labels: `Note:`, `P.S.:`, `Disclaimer:`, etc.
   - WPM patterns: `150 WPM`
   - Horizontal rules: `---`, `===`
   - Bracketed metadata: `[Visual: ...]`, `[Music: ...]`, etc.

## Files Modified

### Core Utility
- **NEW**: `Aura.Core/Utilities/LlmScriptCleanup.cs` - Shared cleanup utility

### Orchestrators
- `Aura.Core/Orchestrator/VideoOrchestrator.cs` - Enhanced script parsing
- `Aura.Core/Orchestrator/EnhancedVideoOrchestrator.cs` - Enhanced script parsing
- `Aura.Core/Orchestrator/Stages/VoiceStage.cs` - Enhanced script parsing
- `Aura.Core/Services/Orchestration/PipelineOrchestrationEngine.cs` - Enhanced script parsing

### Providers
- `Aura.Providers/Llm/BaseLlmScriptProvider.cs` - Uses shared utility
- `Aura.Providers/Llm/OllamaLlmProvider.cs` - Refactored to use shared utility

### API Controllers
- `Aura.Api/Controllers/ScriptsController.cs` - Uses shared utility

## Implementation Details

### Regex Patterns

The cleanup utility includes 10+ compiled regex patterns:

```csharp
- WordCountRegex: Detects "Word Count: X" patterns
- TtsPacingRegex: Detects "TTS Pacing Check:" patterns
- AiDetectionRegex: Detects "AI Detection Avoided:" patterns
- VisualSynergyRegex: Detects "Visual Synergy:" patterns
- EmotionalFlowRegex: Detects "Emotional Flow:" patterns
- AccuracyNoteRegex: Detects "Accuracy:" patterns
- HorizontalRuleRegex: Detects separator lines (---, ===)
- MetaLabelRegex: Detects meta labels (Note:, P.S., etc.)
- WpmRegex: Detects WPM patterns
- BracketedMetaRegex: Detects [Visual: ...], [Music: ...], etc.
```

### Cleanup Process

1. **Line-by-line filtering**: Scripts are processed line-by-line to remove metadata lines
2. **Pattern matching**: Each line is checked against comprehensive regex patterns
3. **Meta-commentary detection**: Additional checks for LLM meta-commentary patterns
4. **Final cleanup**: Removes multiple spaces and normalizes whitespace

### Integration Points

Cleanup is applied at multiple points in the pipeline:

1. **During Script Parsing**: Metadata lines are filtered out when parsing scripts into scenes
2. **Before Scene Creation**: Scene narration is cleaned before creating Scene objects
3. **Before TTS Synthesis**: Final cleanup applied in `ConvertScenesToScriptLines()` to ensure no metadata leaks through

## Benefits

1. **Consistency**: All LLM providers now use the same cleanup logic
2. **Maintainability**: Single source of truth for cleanup patterns
3. **Completeness**: Comprehensive pattern detection catches all known metadata types
4. **Performance**: Compiled regex patterns for efficient matching
5. **Extensibility**: Easy to add new patterns as they are discovered

## Testing Recommendations

1. Test with each LLM provider (OpenAI, Anthropic, Gemini, Azure OpenAI, Ollama)
2. Verify that metadata lines are removed from narration
3. Verify that actual narrative content is preserved
4. Test edge cases (metadata in middle of text, multiple metadata lines, etc.)
5. Verify TTS output is clean and professional

## Future Enhancements

- Add provider-specific patterns if needed (some LLMs may have unique metadata formats)
- Consider machine learning approach for detecting metadata (if patterns become too complex)
- Add logging/metrics for metadata detection (to track which patterns are most common)

## Related Work

This implementation extends the work done in a previous PR for Ollama cleanup:
- Original Ollama cleanup: `Aura.Providers/Llm/OllamaLlmProvider.cs`
- Now generalized for all providers via shared utility

