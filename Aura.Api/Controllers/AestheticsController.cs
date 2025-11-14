using Aura.Core.AI.Aesthetics;
using Aura.Core.AI.Aesthetics.ColorGrading;
using Aura.Core.AI.Aesthetics.Composition;
using Aura.Core.AI.Aesthetics.QualityAssurance;
using Aura.Core.AI.Aesthetics.VisualCoherence;
using Aura.Core.Services.Effects.MotionDesign;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for AI-powered visual aesthetics enhancement
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AestheticsController : ControllerBase
{
    private readonly MoodBasedColorGrader _colorGrader;
    private readonly CompositionAnalyzer _compositionAnalyzer;
    private readonly CoherenceAnalyzer _coherenceAnalyzer;
    private readonly QualityAssessmentEngine _qualityEngine;
    private readonly MotionDesignLibrary _motionLibrary;

    public AestheticsController()
    {
        _colorGrader = new MoodBasedColorGrader();
        _compositionAnalyzer = new CompositionAnalyzer();
        _coherenceAnalyzer = new CoherenceAnalyzer();
        _qualityEngine = new QualityAssessmentEngine();
        _motionLibrary = new MotionDesignLibrary();
    }

    /// <summary>
    /// Analyzes and suggests color grading based on content
    /// </summary>
    [HttpPost("color-grading/analyze")]
    public async Task<ActionResult<ColorGradingProfile>> AnalyzeColorGrading(
        [FromBody] ColorGradingRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _colorGrader.SelectColorGradingAsync(
            request.ContentType,
            request.Sentiment,
            request.TimeOfDay,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(profile);
    }

    /// <summary>
    /// Enforces color consistency across scenes
    /// </summary>
    [HttpPost("color-grading/consistency")]
    public async Task<ActionResult<List<ColorGradingProfile>>> EnforceColorConsistency(
        [FromBody] List<SceneVisualContext> scenes,
        CancellationToken cancellationToken)
    {
        var profiles = await _colorGrader.EnforceColorConsistencyAsync(scenes, cancellationToken).ConfigureAwait(false);
        return Ok(profiles);
    }

    /// <summary>
    /// Detects time of day from visual content
    /// </summary>
    [HttpPost("color-grading/detect-time")]
    public async Task<ActionResult<TimeOfDay>> DetectTimeOfDay(
        [FromBody] Dictionary<string, float> colorHistogram,
        CancellationToken cancellationToken)
    {
        var timeOfDay = await _colorGrader.DetectTimeOfDayAsync(colorHistogram, cancellationToken).ConfigureAwait(false);
        return Ok(timeOfDay);
    }

    /// <summary>
    /// Analyzes image composition
    /// </summary>
    [HttpPost("composition/analyze")]
    public async Task<ActionResult<CompositionAnalysisResult>> AnalyzeComposition(
        [FromBody] CompositionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _compositionAnalyzer.AnalyzeCompositionAsync(
            request.ImageWidth,
            request.ImageHeight,
            request.SubjectPosition,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Detects focal point in image
    /// </summary>
    [HttpPost("composition/focal-point")]
    public async Task<ActionResult<Point>> DetectFocalPoint(
        [FromBody] ImageDimensionsRequest request,
        CancellationToken cancellationToken)
    {
        var focalPoint = await _compositionAnalyzer.DetectFocalPointAsync(
            request.Width,
            request.Height,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(focalPoint);
    }

    /// <summary>
    /// Suggests optimal reframing
    /// </summary>
    [HttpPost("composition/reframe")]
    public async Task<ActionResult<Rectangle>> SuggestReframing(
        [FromBody] ReframingRequest request,
        CancellationToken cancellationToken)
    {
        var crop = await _compositionAnalyzer.SuggestReframingAsync(
            request.FocalPoint,
            request.ImageWidth,
            request.ImageHeight,
            request.Rule,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(crop);
    }

    /// <summary>
    /// Analyzes visual coherence across scenes
    /// </summary>
    [HttpPost("coherence/analyze")]
    public async Task<ActionResult<VisualCoherenceReport>> AnalyzeCoherence(
        [FromBody] List<SceneVisualContext> scenes,
        CancellationToken cancellationToken)
    {
        var report = await _coherenceAnalyzer.AnalyzeCoherenceAsync(scenes, cancellationToken).ConfigureAwait(false);
        return Ok(report);
    }

    /// <summary>
    /// Analyzes lighting consistency
    /// </summary>
    [HttpPost("coherence/lighting")]
    public async Task<ActionResult<float>> AnalyzeLightingConsistency(
        [FromBody] List<SceneVisualContext> scenes,
        CancellationToken cancellationToken)
    {
        var score = await _coherenceAnalyzer.AnalyzeLightingConsistencyAsync(scenes, cancellationToken).ConfigureAwait(false);
        return Ok(score);
    }

    /// <summary>
    /// Detects visual theme
    /// </summary>
    [HttpPost("coherence/theme")]
    public async Task<ActionResult<string>> DetectVisualTheme(
        [FromBody] List<SceneVisualContext> scenes,
        CancellationToken cancellationToken)
    {
        var theme = await _coherenceAnalyzer.DetectVisualThemeAsync(scenes, cancellationToken).ConfigureAwait(false);
        return Ok(theme);
    }

    /// <summary>
    /// Assesses technical quality
    /// </summary>
    [HttpPost("quality/assess")]
    public async Task<ActionResult<QualityMetrics>> AssessQuality(
        [FromBody] QualityAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var metrics = await _qualityEngine.AssessQualityAsync(
            request.ResolutionWidth,
            request.ResolutionHeight,
            request.Sharpness,
            request.NoiseLevel,
            request.CompressionQuality,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(metrics);
    }

    /// <summary>
    /// Calculates perceptual quality score
    /// </summary>
    [HttpPost("quality/perceptual")]
    public async Task<ActionResult<float>> CalculatePerceptualQuality(
        [FromBody] QualityMetrics metrics,
        CancellationToken cancellationToken)
    {
        var score = await _qualityEngine.CalculatePerceptualQualityAsync(metrics, cancellationToken).ConfigureAwait(false);
        return Ok(score);
    }

    /// <summary>
    /// Suggests quality enhancements
    /// </summary>
    [HttpPost("quality/enhance")]
    public async Task<ActionResult<Dictionary<string, float>>> SuggestEnhancements(
        [FromBody] QualityMetrics metrics,
        CancellationToken cancellationToken)
    {
        var enhancements = await _qualityEngine.SuggestEnhancementsAsync(metrics, cancellationToken).ConfigureAwait(false);
        return Ok(enhancements);
    }

    /// <summary>
    /// Compares quality before and after enhancements
    /// </summary>
    [HttpPost("quality/compare")]
    public async Task<ActionResult<Dictionary<string, object>>> CompareQuality(
        [FromBody] QualityComparisonRequest request,
        CancellationToken cancellationToken)
    {
        var comparison = await _qualityEngine.CompareQualityAsync(
            request.Before,
            request.After,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(comparison);
    }

    /// <summary>
    /// Gets content-based transition effect
    /// </summary>
    [HttpPost("motion/transition")]
    public async Task<ActionResult<MotionDesignLibrary.MotionEffect>> GetTransition(
        [FromBody] TransitionRequest request,
        CancellationToken cancellationToken)
    {
        var effect = await _motionLibrary.GetContentBasedTransitionAsync(
            request.ContentType,
            request.FromScene,
            request.ToScene,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(effect);
    }

    /// <summary>
    /// Creates animated lower third
    /// </summary>
    [HttpPost("motion/lower-third")]
    public async Task<ActionResult<MotionDesignLibrary.LowerThird>> CreateLowerThird(
        [FromBody] LowerThirdRequest request,
        CancellationToken cancellationToken)
    {
        var lowerThird = await _motionLibrary.CreateLowerThirdAsync(
            request.Text,
            request.SubText,
            request.Style,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(lowerThird);
    }

    /// <summary>
    /// Applies Ken Burns effect to static image
    /// </summary>
    [HttpPost("motion/ken-burns")]
    public async Task<ActionResult<MotionDesignLibrary.KenBurnsEffect>> ApplyKenBurnsEffect(
        [FromBody] KenBurnsRequest request,
        CancellationToken cancellationToken)
    {
        var effect = await _motionLibrary.ApplyKenBurnsEffectAsync(
            request.ImageWidth,
            request.ImageHeight,
            request.Duration,
            request.FocusPoint,
            cancellationToken
        ).ConfigureAwait(false);

        return Ok(effect);
    }

    /// <summary>
    /// Gets motion design presets library
    /// </summary>
    [HttpGet("motion/presets")]
    public async Task<ActionResult<List<MotionDesignLibrary.MotionEffect>>> GetMotionPresets(
        CancellationToken cancellationToken)
    {
        var presets = await _motionLibrary.GetMotionDesignPresetsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(presets);
    }
}

// Request DTOs
public record ColorGradingRequest(string ContentType, string Sentiment, TimeOfDay TimeOfDay);
public record CompositionRequest(int ImageWidth, int ImageHeight, Point? SubjectPosition);
public record ImageDimensionsRequest(int Width, int Height);
public record ReframingRequest(Point FocalPoint, int ImageWidth, int ImageHeight, CompositionRule Rule);
public record QualityAssessmentRequest(int ResolutionWidth, int ResolutionHeight, float? Sharpness, float? NoiseLevel, float? CompressionQuality);
public record QualityComparisonRequest(QualityMetrics Before, QualityMetrics After);
public record TransitionRequest(string ContentType, string FromScene, string ToScene);
public record LowerThirdRequest(string Text, string SubText, string Style);
public record KenBurnsRequest(int ImageWidth, int ImageHeight, float Duration, string FocusPoint);
