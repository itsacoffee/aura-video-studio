using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Services.ScriptEnhancement;
using Aura.Core.Services.ScriptReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered script enhancement and storytelling
/// </summary>
[ApiController]
[Route("api/script")]
public class ScriptController : ControllerBase
{
    // Cached search values for sentence delimiter search (CA1870 compliance)
    private static readonly SearchValues<char> SentenceDelimiters = SearchValues.Create(".!?");

    private readonly ILogger<ScriptController> _logger;
    private readonly ScriptAnalysisService _analysisService;
    private readonly AdvancedScriptEnhancer _enhancer;
    private readonly ScriptSceneService _sceneService;

    public ScriptController(
        ILogger<ScriptController> logger,
        ScriptAnalysisService analysisService,
        AdvancedScriptEnhancer enhancer,
        ScriptSceneService sceneService)
    {
        _logger = logger;
        _analysisService = analysisService;
        _enhancer = enhancer;
        _sceneService = sceneService;
    }

    /// <summary>
    /// Analyze script structure and quality
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] ScriptAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new ScriptAnalysisResponse(
                    Success: false,
                    Analysis: null,
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Analyzing script (length: {Length} chars)", request.Script.Length);

            var analysis = await _analysisService.AnalyzeScriptAsync(
                request.Script,
                request.ContentType,
                request.TargetAudience,
                request.CurrentTone,
                ct).ConfigureAwait(false);

            return Ok(new ScriptAnalysisResponse(
                Success: true,
                Analysis: analysis,
                ErrorMessage: null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing script");
            return StatusCode(500, new ScriptAnalysisResponse(
                Success: false,
                Analysis: null,
                ErrorMessage: $"Failed to analyze script: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Apply comprehensive enhancements to a script
    /// </summary>
    [HttpPost("enhance")]
    public async Task<IActionResult> Enhance(
        [FromBody] ScriptEnhanceRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new ScriptEnhanceResponse(
                    Success: false,
                    EnhancedScript: null,
                    Suggestions: new List<EnhancementSuggestion>(),
                    ChangesSummary: null,
                    BeforeAnalysis: null,
                    AfterAnalysis: null,
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Enhancing script (length: {Length} chars)", request.Script.Length);

            var result = await _enhancer.EnhanceScriptAsync(
                request.Script,
                request.ContentType,
                request.TargetAudience,
                request.DesiredTone,
                request.FocusAreas,
                request.AutoApply,
                request.TargetFramework,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing script");
            return StatusCode(500, new ScriptEnhanceResponse(
                Success: false,
                EnhancedScript: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ChangesSummary: null,
                BeforeAnalysis: null,
                AfterAnalysis: null,
                ErrorMessage: $"Failed to enhance script: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Optimize the opening hook (first 15 seconds)
    /// </summary>
    [HttpPost("optimize-hook")]
    public async Task<IActionResult> OptimizeHook(
        [FromBody] OptimizeHookRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new OptimizeHookResponse(
                    Success: false,
                    OptimizedHook: null,
                    HookStrengthBefore: 0,
                    HookStrengthAfter: 0,
                    Techniques: new List<string>(),
                    Explanation: null,
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Optimizing hook for script");

            var result = await _enhancer.OptimizeHookAsync(
                request.Script,
                request.ContentType,
                request.TargetAudience,
                request.TargetSeconds,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing hook");
            return StatusCode(500, new OptimizeHookResponse(
                Success: false,
                OptimizedHook: null,
                HookStrengthBefore: 0,
                HookStrengthAfter: 0,
                Techniques: new List<string>(),
                Explanation: null,
                ErrorMessage: $"Failed to optimize hook: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Analyze and optimize emotional arc
    /// </summary>
    [HttpPost("emotional-arc")]
    public async Task<IActionResult> EmotionalArc(
        [FromBody] EmotionalArcRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new EmotionalArcResponse(
                    Success: false,
                    CurrentArc: null,
                    OptimizedArc: null,
                    Suggestions: new List<EnhancementSuggestion>(),
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Analyzing emotional arc");

            var result = await _enhancer.AnalyzeEmotionalArcAsync(
                request.Script,
                request.ContentType,
                request.TargetAudience,
                request.DesiredJourney,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing emotional arc");
            return StatusCode(500, new EmotionalArcResponse(
                Success: false,
                CurrentArc: null,
                OptimizedArc: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ErrorMessage: $"Failed to analyze emotional arc: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Enhance audience connection
    /// </summary>
    [HttpPost("audience-connect")]
    public async Task<IActionResult> AudienceConnect(
        [FromBody] AudienceConnectionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new AudienceConnectionResponse(
                    Success: false,
                    EnhancedScript: null,
                    Suggestions: new List<EnhancementSuggestion>(),
                    ConnectionScoreBefore: 0,
                    ConnectionScoreAfter: 0,
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Enhancing audience connection");

            var result = await _enhancer.EnhanceAudienceConnectionAsync(
                request.Script,
                request.TargetAudience,
                request.ContentType,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing audience connection");
            return StatusCode(500, new AudienceConnectionResponse(
                Success: false,
                EnhancedScript: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ConnectionScoreBefore: 0,
                ConnectionScoreAfter: 0,
                ErrorMessage: $"Failed to enhance audience connection: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Perform fact-checking on script claims
    /// </summary>
    [HttpPost("fact-check")]
    public async Task<IActionResult> FactCheck(
        [FromBody] FactCheckRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new FactCheckResponse(
                    Success: false,
                    Findings: new List<FactCheckFinding>(),
                    TotalClaims: 0,
                    VerifiedClaims: 0,
                    UncertainClaims: 0,
                    DisclaimerSuggestion: null,
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Fact-checking script");

            var result = await _enhancer.FactCheckScriptAsync(
                request.Script,
                request.IncludeSources,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fact-checking script");
            return StatusCode(500, new FactCheckResponse(
                Success: false,
                Findings: new List<FactCheckFinding>(),
                TotalClaims: 0,
                VerifiedClaims: 0,
                UncertainClaims: 0,
                DisclaimerSuggestion: null,
                ErrorMessage: $"Failed to fact-check script: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Adjust script tone and voice
    /// </summary>
    [HttpPost("tone-adjust")]
    public async Task<IActionResult> ToneAdjust(
        [FromBody] ToneAdjustRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new ToneAdjustResponse(
                    Success: false,
                    AdjustedScript: null,
                    OriginalTone: null,
                    AchievedTone: null,
                    Changes: new List<EnhancementSuggestion>(),
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Adjusting script tone");

            var result = await _enhancer.AdjustToneAsync(
                request.Script,
                request.TargetTone,
                request.ContentType,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting tone");
            return StatusCode(500, new ToneAdjustResponse(
                Success: false,
                AdjustedScript: null,
                OriginalTone: null,
                AchievedTone: null,
                Changes: new List<EnhancementSuggestion>(),
                ErrorMessage: $"Failed to adjust tone: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Apply specific storytelling framework
    /// </summary>
    [HttpPost("apply-framework")]
    public async Task<IActionResult> ApplyFramework(
        [FromBody] ApplyFrameworkRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new ApplyFrameworkResponse(
                    Success: false,
                    EnhancedScript: null,
                    AppliedFramework: null,
                    Suggestions: new List<EnhancementSuggestion>(),
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Applying storytelling framework: {Framework}", request.Framework);

            var result = await _enhancer.ApplyStorytellingFrameworkAsync(
                request.Script,
                request.Framework,
                request.ContentType,
                request.TargetAudience,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying framework");
            return StatusCode(500, new ApplyFrameworkResponse(
                Success: false,
                EnhancedScript: null,
                AppliedFramework: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ErrorMessage: $"Failed to apply framework: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Get individual enhancement suggestions
    /// </summary>
    [HttpPost("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromBody] GetSuggestionsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new GetSuggestionsResponse(
                    Success: false,
                    Suggestions: new List<EnhancementSuggestion>(),
                    TotalCount: 0,
                    ErrorMessage: "Script is required"
                ));
            }

            _logger.LogInformation("Getting enhancement suggestions");

            var result = await _enhancer.GetSuggestionsAsync(
                request.Script,
                request.ContentType,
                request.TargetAudience,
                request.FilterTypes,
                request.MaxSuggestions,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new GetSuggestionsResponse(
                Success: false,
                Suggestions: new List<EnhancementSuggestion>(),
                TotalCount: 0,
                ErrorMessage: $"Failed to get suggestions: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Compare two script versions
    /// </summary>
    [HttpPost("compare-versions")]
    public async Task<IActionResult> CompareVersions(
        [FromBody] CompareVersionsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.VersionA) || string.IsNullOrWhiteSpace(request.VersionB))
            {
                return BadRequest(new CompareVersionsResponse(
                    Success: false,
                    Differences: new List<ScriptDiff>(),
                    AnalysisA: null,
                    AnalysisB: null,
                    ImprovementMetrics: new Dictionary<string, double>(),
                    ErrorMessage: "Both versions are required"
                ));
            }

            _logger.LogInformation("Comparing script versions");

            var result = await _enhancer.CompareVersionsAsync(
                request.VersionA,
                request.VersionB,
                request.IncludeAnalysis,
                ct).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions");
            return StatusCode(500, new CompareVersionsResponse(
                Success: false,
                Differences: new List<ScriptDiff>(),
                AnalysisA: null,
                AnalysisB: null,
                ImprovementMetrics: new Dictionary<string, double>(),
                ErrorMessage: $"Failed to compare versions: {ex.Message}"
            ));
        }
    }

    /// <summary>
    /// Regenerate a single scene from context menu
    /// </summary>
    [HttpPost("regenerate-scene")]
    public async Task<IActionResult> RegenerateScene(
        [FromBody] RegenerateSceneContextRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Regenerating scene {SceneIndex} for job {JobId}",
                request.SceneIndex, request.JobId);

            var result = await _sceneService.RegenerateSceneAsync(
                request.JobId,
                request.SceneIndex,
                request.Brief,
                null,
                ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return StatusCode(500, new SceneModificationResponse
                {
                    Success = false,
                    Scene = null,
                    Error = result.Error
                });
            }

            return Ok(new SceneModificationResponse
            {
                Success = true,
                Scene = new ScriptSceneDto
                {
                    Number = request.SceneIndex + 1,
                    Narration = result.Text ?? string.Empty,
                    VisualPrompt = GenerateVisualPromptFromNarration(result.Text ?? string.Empty),
                    DurationSeconds = EstimateDuration(result.Text ?? string.Empty),
                    Transition = "Cut"
                },
                Error = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate scene {SceneIndex}", request.SceneIndex);
            return StatusCode(500, new SceneModificationResponse
            {
                Success = false,
                Scene = null,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Expand a scene to make it longer
    /// </summary>
    [HttpPost("expand-scene")]
    public async Task<IActionResult> ExpandScene(
        [FromBody] ExpandSceneRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Expanding scene {SceneIndex} by {Expansion}x for job {JobId}",
                request.SceneIndex, request.TargetExpansion, request.JobId);

            var result = await _sceneService.ExpandSceneAsync(
                request.JobId,
                request.SceneIndex,
                string.Empty,
                request.TargetExpansion,
                ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return StatusCode(500, new SceneModificationResponse
                {
                    Success = false,
                    Scene = null,
                    Error = result.Error
                });
            }

            return Ok(new SceneModificationResponse
            {
                Success = true,
                Scene = new ScriptSceneDto
                {
                    Number = request.SceneIndex + 1,
                    Narration = result.Text ?? string.Empty,
                    VisualPrompt = GenerateVisualPromptFromNarration(result.Text ?? string.Empty),
                    DurationSeconds = EstimateDuration(result.Text ?? string.Empty),
                    Transition = "Cut"
                },
                Error = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expand scene {SceneIndex}", request.SceneIndex);
            return StatusCode(500, new SceneModificationResponse
            {
                Success = false,
                Scene = null,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Shorten a scene to make it more concise
    /// </summary>
    [HttpPost("shorten-scene")]
    public async Task<IActionResult> ShortenScene(
        [FromBody] ShortenSceneRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Shortening scene {SceneIndex} to {Reduction}x for job {JobId}",
                request.SceneIndex, request.TargetReduction, request.JobId);

            var result = await _sceneService.ShortenSceneAsync(
                request.JobId,
                request.SceneIndex,
                string.Empty,
                request.TargetReduction,
                ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return StatusCode(500, new SceneModificationResponse
                {
                    Success = false,
                    Scene = null,
                    Error = result.Error
                });
            }

            return Ok(new SceneModificationResponse
            {
                Success = true,
                Scene = new ScriptSceneDto
                {
                    Number = request.SceneIndex + 1,
                    Narration = result.Text ?? string.Empty,
                    VisualPrompt = GenerateVisualPromptFromNarration(result.Text ?? string.Empty),
                    DurationSeconds = EstimateDuration(result.Text ?? string.Empty),
                    Transition = "Cut"
                },
                Error = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shorten scene {SceneIndex}", request.SceneIndex);
            return StatusCode(500, new SceneModificationResponse
            {
                Success = false,
                Scene = null,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate B-Roll visual suggestions for a scene
    /// </summary>
    [HttpPost("generate-broll")]
    public async Task<IActionResult> GenerateBRollSuggestions(
        [FromBody] GenerateBRollRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating B-Roll suggestions for scene {SceneIndex} in job {JobId}",
                request.SceneIndex, request.JobId);

            var result = await _sceneService.GenerateBRollSuggestionsAsync(
                request.JobId,
                request.SceneIndex,
                string.Empty,
                ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return StatusCode(500, new BRollSuggestionsResponse
                {
                    Success = false,
                    Suggestions = new List<string>(),
                    Error = result.Error
                });
            }

            return Ok(new BRollSuggestionsResponse
            {
                Success = true,
                Suggestions = result.Suggestions,
                Error = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate B-Roll suggestions for scene {SceneIndex}", request.SceneIndex);
            return StatusCode(500, new BRollSuggestionsResponse
            {
                Success = false,
                Suggestions = new List<string>(),
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Estimates duration based on word count (average 150 WPM)
    /// </summary>
    private static double EstimateDuration(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 5.0;

        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var durationSeconds = (wordCount / 150.0) * 60.0;
        return Math.Max(3.0, Math.Round(durationSeconds, 1));
    }

    /// <summary>
    /// Generates a visual prompt based on the narration text by extracting key themes
    /// </summary>
    private static string GenerateVisualPromptFromNarration(string narration)
    {
        if (string.IsNullOrWhiteSpace(narration))
            return "Visual representation of the scene content";

        // Extract the first sentence or up to 80 characters for the visual prompt
        var firstSentenceEnd = narration.AsSpan().IndexOfAny(SentenceDelimiters);
        var keyPhrase = firstSentenceEnd > 0 && firstSentenceEnd < 100
            ? narration.Substring(0, firstSentenceEnd)
            : narration.Length > 80
                ? narration.Substring(0, 80) + "..."
                : narration;

        return $"Visual representation: {keyPhrase.Trim()}";
    }
}
