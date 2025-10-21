using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Services.ScriptEnhancement;
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
    private readonly ILogger<ScriptController> _logger;
    private readonly ScriptAnalysisService _analysisService;
    private readonly AdvancedScriptEnhancer _enhancer;

    public ScriptController(
        ILogger<ScriptController> logger,
        ScriptAnalysisService analysisService,
        AdvancedScriptEnhancer enhancer)
    {
        _logger = logger;
        _analysisService = analysisService;
        _enhancer = enhancer;
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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
                ct);

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
}
