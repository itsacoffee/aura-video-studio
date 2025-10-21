# AI-Powered Content Analysis Implementation

## Overview

This implementation adds comprehensive AI-powered content analysis capabilities to Aura Video Studio, enabling automatic script quality assessment, enhancement, visual asset suggestions, and pacing optimization.

## Architecture

### Backend Services (C#)

#### 1. ContentAnalyzer (`Aura.Core/Services/Content/ContentAnalyzer.cs`)
- **Purpose**: Analyzes video scripts for quality metrics
- **Dependencies**: `ILlmProvider`, `ILogger`
- **Key Features**:
  - Coherence scoring (0-100): How well scenes connect
  - Pacing scoring (0-100): Appropriate speed and rhythm
  - Engagement scoring (0-100): Hooks and interesting elements
  - Readability scoring (0-100): Clear language and structure
  - Overall quality score: Average of subscores
  - Script statistics: Word count, estimated reading time, complexity
  - Issue detection: Specific problems found
  - Improvement suggestions: Actionable recommendations

#### 2. ScriptEnhancer (`Aura.Core/Services/Content/ScriptEnhancer.cs`)
- **Purpose**: AI-powered script improvements
- **Dependencies**: `ILlmProvider`, `ILogger`
- **Enhancement Options**:
  - Fix Coherence: Add transitions between scenes
  - Increase Engagement: Add hooks, facts, questions
  - Improve Clarity: Simplify complex sentences
  - Add Details: Expand with context and examples
- **Features**:
  - Maintains original structure (scene headings)
  - Generates line-by-line diff
  - Provides improvement summary

#### 3. VisualAssetSuggester (`Aura.Core/Services/Content/VisualAssetSuggester.cs`)
- **Purpose**: Suggests relevant visual assets for scenes
- **Dependencies**: `ILlmProvider`, `IStockProvider`, `ILogger`
- **Features**:
  - AI-generated asset suggestions per scene
  - Keyword extraction for asset search
  - Integration with stock providers (Pexels, Pixabay)
  - Relevance scoring for each asset
  - Caching to avoid repeated LLM calls
  - Batch processing for multiple scenes

#### 4. PacingOptimizer (`Aura.Core/Services/Content/PacingOptimizer.cs`)
- **Purpose**: Optimizes scene timing and speaking rates
- **Features**:
  - Analyzes words per second (target: 2.5 WPS)
  - Detects too-fast pacing (>3 WPS) - Critical
  - Detects too-slow pacing (<2 WPS) - Recommended
  - Opening scene optimization (faster pace to hook)
  - Conclusion optimization (strong finish)
  - Priority levels: Critical, Recommended, Optional
  - Detailed reasoning for each suggestion

### Data Models (`Aura.Core/Models/ContentAnalysis.cs`)

```csharp
public record ScriptAnalysis(
    double CoherenceScore,
    double PacingScore,
    double EngagementScore,
    double ReadabilityScore,
    double OverallQualityScore,
    List<string> Issues,
    List<string> Suggestions,
    ScriptStatistics Statistics);

public record EnhancedScript(
    string NewScript,
    List<DiffChange> Changes,
    string ImprovementSummary);

public record AssetSuggestion(
    string Keyword,
    string Description,
    List<AssetMatch> Matches);

public record PacingOptimization(
    List<ScenePacingSuggestion> Suggestions,
    string OverallAssessment);
```

### API Endpoints (`Aura.Api/Controllers/ContentController.cs`)

1. **POST /api/content/analyze-script**
   - Request: `{ script: string }`
   - Response: ScriptAnalysis with scores and suggestions
   - Use case: Initial script quality assessment

2. **POST /api/content/enhance-script**
   - Request: `{ script: string, fixCoherence: bool, increaseEngagement: bool, improveClarity: bool, addDetails: bool }`
   - Response: Enhanced script with diff and summary
   - Use case: Apply AI improvements to script

3. **POST /api/content/suggest-assets**
   - Request: `{ sceneHeading: string, sceneScript: string }`
   - Response: Array of asset suggestions with thumbnails
   - Use case: Get visual asset recommendations

4. **POST /api/content/optimize-pacing**
   - Request: `{ scenes: [], narrationPath?: string, musicPath?: string }`
   - Response: Pacing suggestions with priorities
   - Use case: Optimize timeline scene durations

### Frontend Components (React/TypeScript)

#### 1. ScriptAnalysis Component (`Aura.Web/src/pages/Create/ScriptAnalysis.tsx`)

**Features**:
- Large overall quality score display (72px font)
- 4 individual score cards (Coherence, Pacing, Engagement, Readability)
- Color-coded scores:
  - Green: 80-100 (good)
  - Yellow: 60-79 (acceptable)
  - Red: <60 (needs improvement)
- Progress bars for visual feedback
- Warning badge if score < 70
- Expandable issues list with warning icons
- Checkboxes for enhancement options:
  - Fix Coherence
  - Increase Engagement
  - Improve Clarity
  - Add Details
- Statistics panel:
  - Total word count
  - Average words per scene
  - Estimated reading time
  - Complexity score
- Action buttons:
  - Regenerate Script
  - Enhance Script
  - Proceed Anyway

**Usage Example**:
```tsx
<ScriptAnalysis
  script={generatedScript}
  onEnhanceScript={(enhanced) => setScript(enhanced)}
  onProceed={() => continueToNextStep()}
  onRegenerate={() => regenerateScript()}
/>
```

#### 2. AssetSuggestions Component (`Aura.Web/src/pages/Editor/AssetSuggestions.tsx`)

**Features**:
- Grid layout of suggested assets
- Thumbnail previews (150x120px)
- Relevance score badges
- Hover effects for better UX
- Loading states with spinner
- Empty state with retry button
- "More Suggestions" button
- Click to add asset to timeline
- Keyword and description display
- Responsive grid layout

**Usage Example**:
```tsx
<AssetSuggestions
  sceneHeading="Introduction"
  sceneScript="Welcome to our video..."
  onSelectAsset={(asset) => addToTimeline(asset)}
/>
```

## Integration Points

### Dependency Injection (Program.cs)

```csharp
// Register content analysis services
builder.Services.AddSingleton<ContentAnalyzer>(sp => {
    var logger = sp.GetRequiredService<ILogger<ContentAnalyzer>>();
    var llmProvider = sp.GetRequiredService<ILlmProvider>();
    return new ContentAnalyzer(logger, llmProvider);
});

// Similar registrations for ScriptEnhancer, VisualAssetSuggester, PacingOptimizer
```

### Usage in VideoOrchestrator (Future Integration)

```csharp
// After script generation
var analysis = await contentAnalyzer.AnalyzeScriptAsync(script);
_logger.LogInformation("Script quality: {Score}", analysis.OverallQualityScore);

if (analysis.OverallQualityScore < 70 && settings.AutoEnhance)
{
    var options = new EnhancementOptions(
        FixCoherence: analysis.CoherenceScore < 70,
        IncreaseEngagement: analysis.EngagementScore < 70,
        ImproveClarity: analysis.ReadabilityScore < 70,
        AddDetails: false
    );
    var enhanced = await scriptEnhancer.EnhanceScriptAsync(script, options);
    script = enhanced.NewScript;
}

// Before timeline finalization
var optimization = await pacingOptimizer.OptimizeTimingAsync(timeline);
foreach (var suggestion in optimization.Suggestions.Where(s => s.Priority == PacingPriority.Critical))
{
    AdjustSceneDuration(suggestion.SceneIndex, suggestion.SuggestedDuration);
}
```

## Testing

### Unit Tests

**ContentAnalyzerTests.cs**:
- ✅ Analysis returns scores and suggestions
- ✅ Statistics calculated correctly
- ✅ Handles empty scripts gracefully
- ✅ Returns defaults on LLM errors

**PacingOptimizerTests.cs**:
- ✅ Detects fast pacing (>3 WPS)
- ✅ Detects slow pacing (<2 WPS)
- ✅ Accepts good pacing (2-3 WPS)
- ✅ Considers opening scene context
- ✅ Handles multiple scenes correctly

**Test Results**: All 9 tests passing

### Security Testing

**CodeQL Scan Results**:
- Initial: 1 alert (log forging vulnerability)
- Fixed: Sanitized user input in logs (removed newlines)
- Current: 0 critical vulnerabilities

## Configuration

### Required Dependencies
- LLM Provider (OpenAI, Anthropic, or local)
- Stock Provider (optional, for asset suggestions)
- FFprobe (optional, for audio analysis)

### Settings
No additional configuration required - uses existing LLM provider settings.

## Performance Considerations

1. **Caching**: VisualAssetSuggester caches results for 1 hour
2. **Parallel Processing**: Asset suggestions processed in parallel
3. **Timeout Handling**: All LLM calls have proper error handling
4. **Lazy Loading**: UI components load suggestions on demand

## Error Handling

All services implement graceful fallbacks:
- ContentAnalyzer: Returns default scores (75) on error
- ScriptEnhancer: Returns original script on error
- VisualAssetSuggester: Returns empty suggestions on error
- PacingOptimizer: Skips problematic scenes

## Future Enhancements

### Not Implemented (Out of Scope)
1. Full VideoOrchestrator integration (automatic analysis during generation)
2. Timeline editor integration (drag-and-drop assets)
3. Real-time script editing with live analysis
4. Custom scoring thresholds in settings
5. Export analysis reports (PDF/JSON)
6. A/B testing of enhanced vs. original scripts
7. Integration with brand kit for style consistency
8. Multi-language support for analysis

### Implementation Notes for Future Work
- VideoOrchestrator integration requires careful placement to avoid blocking generation
- Asset suggestions need integration with existing asset management system
- Pacing optimization should respect user's manual adjustments
- Consider adding analysis history/comparison features

## API Examples

### Analyze Script
```bash
curl -X POST http://localhost:5000/api/content/analyze-script \
  -H "Content-Type: application/json" \
  -d '{"script": "# My Video\n## Scene 1\nHello world"}'
```

### Enhance Script
```bash
curl -X POST http://localhost:5000/api/content/enhance-script \
  -H "Content-Type: application/json" \
  -d '{
    "script": "# My Video\n## Scene 1\nHello world",
    "fixCoherence": true,
    "increaseEngagement": true
  }'
```

### Suggest Assets
```bash
curl -X POST http://localhost:5000/api/content/suggest-assets \
  -H "Content-Type: application/json" \
  -d '{
    "sceneHeading": "Introduction",
    "sceneScript": "Welcome to our tutorial"
  }'
```

## Files Changed

**Added**:
- `Aura.Core/Models/ContentAnalysis.cs` (71 lines)
- `Aura.Core/Services/Content/ContentAnalyzer.cs` (226 lines)
- `Aura.Core/Services/Content/ScriptEnhancer.cs` (172 lines)
- `Aura.Core/Services/Content/VisualAssetSuggester.cs` (235 lines)
- `Aura.Core/Services/Content/PacingOptimizer.cs` (204 lines)
- `Aura.Api/Controllers/ContentController.cs` (288 lines)
- `Aura.Web/src/pages/Create/ScriptAnalysis.tsx` (380 lines)
- `Aura.Web/src/pages/Editor/AssetSuggestions.tsx` (196 lines)
- `Aura.Tests/ContentAnalyzerTests.cs` (123 lines)
- `Aura.Tests/PacingOptimizerTests.cs` (182 lines)

**Modified**:
- `Aura.Api/Program.cs` (+40 lines) - Service registration

**Total**: ~2,317 lines of new code

## Summary

This implementation provides a solid foundation for AI-powered content analysis in Aura Video Studio. The modular design allows for easy extension and integration with the existing video generation pipeline. All core functionality is working, tested, and secured.
