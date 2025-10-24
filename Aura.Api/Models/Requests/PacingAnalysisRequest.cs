using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aura.Core.Models;

namespace Aura.Api.Models.Requests;

/// <summary>
/// Request for pacing analysis with script and scenes.
/// </summary>
public class PacingAnalysisRequest
{
    /// <summary>
    /// Script text to analyze
    /// </summary>
    [Required(ErrorMessage = "Script is required")]
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// Scenes to analyze for pacing
    /// </summary>
    [Required(ErrorMessage = "Scenes are required")]
    [MinLength(1, ErrorMessage = "At least one scene is required")]
    public List<Scene> Scenes { get; set; } = new();

    /// <summary>
    /// Target platform for optimization (YouTube, TikTok, Instagram Reels, YouTube Shorts, Facebook)
    /// </summary>
    [Required(ErrorMessage = "Target platform is required")]
    public string TargetPlatform { get; set; } = "YouTube";

    /// <summary>
    /// Target video duration in seconds
    /// </summary>
    [Range(1, 3600, ErrorMessage = "Target duration must be between 1 and 3600 seconds")]
    public double TargetDuration { get; set; }

    /// <summary>
    /// Target audience description
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Brief with project context
    /// </summary>
    [Required(ErrorMessage = "Brief is required")]
    public Brief Brief { get; set; } = null!;
}

/// <summary>
/// Request to reanalyze with different parameters.
/// </summary>
public class ReanalyzeRequest
{
    /// <summary>
    /// Optimization level (Low, Medium, High, Maximum)
    /// </summary>
    [Required(ErrorMessage = "Optimization level is required")]
    public string OptimizationLevel { get; set; } = "Medium";

    /// <summary>
    /// Target platform for reanalysis
    /// </summary>
    [Required(ErrorMessage = "Target platform is required")]
    public string TargetPlatform { get; set; } = "YouTube";
}
