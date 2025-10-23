using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentPlanning;
using Aura.Core.Services.ContentPlanning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-driven content planning features
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentPlanningController : ControllerBase
{
    private readonly ILogger<ContentPlanningController> _logger;
    private readonly TrendAnalysisService _trendAnalysisService;
    private readonly TopicGenerationService _topicGenerationService;
    private readonly ContentSchedulingService _schedulingService;
    private readonly AudienceAnalysisService _audienceService;

    public ContentPlanningController(
        ILogger<ContentPlanningController> logger,
        TrendAnalysisService trendAnalysisService,
        TopicGenerationService topicGenerationService,
        ContentSchedulingService schedulingService,
        AudienceAnalysisService audienceService)
    {
        _logger = logger;
        _trendAnalysisService = trendAnalysisService;
        _topicGenerationService = topicGenerationService;
        _schedulingService = schedulingService;
        _audienceService = audienceService;
    }

    /// <summary>
    /// Analyzes trends for content planning
    /// </summary>
    [HttpPost("trends/analyze")]
    public async Task<IActionResult> AnalyzeTrends(
        [FromBody] TrendAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Analyzing trends for category: {Category}", request.Category);
            var response = await _trendAnalysisService.AnalyzeTrendsAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing trends");
            return StatusCode(500, new { error = "Failed to analyze trends" });
        }
    }

    /// <summary>
    /// Gets trending topics for a specific platform
    /// </summary>
    [HttpGet("trends/platform/{platform}")]
    public async Task<IActionResult> GetPlatformTrends(
        string platform,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting platform trends for {Platform}", platform);
            var trends = await _trendAnalysisService.GetPlatformTrendsAsync(platform, category, ct);
            return Ok(new { success = true, trends, platform, category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform trends");
            return StatusCode(500, new { error = "Failed to get platform trends" });
        }
    }

    /// <summary>
    /// Generates AI-powered topic suggestions
    /// </summary>
    [HttpPost("topics/generate")]
    public async Task<IActionResult> GenerateTopics(
        [FromBody] TopicSuggestionRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating topics for category: {Category}", request.Category);
            var response = await _topicGenerationService.GenerateTopicsAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating topics");
            return StatusCode(500, new { error = "Failed to generate topics" });
        }
    }

    /// <summary>
    /// Generates topic suggestions based on current trends
    /// </summary>
    [HttpPost("topics/trend-based")]
    public async Task<IActionResult> GenerateTrendBasedTopics(
        [FromBody] TrendAnalysisRequest trendRequest,
        [FromQuery] int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating trend-based topics");
            
            var trendResponse = await _trendAnalysisService.AnalyzeTrendsAsync(trendRequest, ct);
            var topics = await _topicGenerationService.GenerateTrendBasedTopicsAsync(
                trendResponse.Trends, count, ct);

            return Ok(new { success = true, topics, count = topics.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trend-based topics");
            return StatusCode(500, new { error = "Failed to generate trend-based topics" });
        }
    }

    /// <summary>
    /// Gets scheduling recommendations for content
    /// </summary>
    [HttpPost("schedule/recommendations")]
    public async Task<IActionResult> GetSchedulingRecommendations(
        [FromBody] ContentSchedulingRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting scheduling recommendations for {Platform}", request.Platform);
            var response = await _schedulingService.GetSchedulingRecommendationsAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduling recommendations");
            return StatusCode(500, new { error = "Failed to get scheduling recommendations" });
        }
    }

    /// <summary>
    /// Schedules content for a specific time
    /// </summary>
    [HttpPost("schedule/content")]
    public async Task<IActionResult> ScheduleContent(
        [FromBody] ScheduleContentRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Scheduling content");
            
            var plan = new ContentPlan
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                Category = request.Category ?? string.Empty,
                TargetPlatform = request.Platform,
                Tags = request.Tags ?? new List<string>()
            };

            var scheduled = await _schedulingService.ScheduleContentAsync(
                plan, request.ScheduledDateTime, ct);

            return Ok(new { success = true, scheduled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling content");
            return StatusCode(500, new { error = "Failed to schedule content" });
        }
    }

    /// <summary>
    /// Gets scheduled content within a date range
    /// </summary>
    [HttpGet("schedule/calendar")]
    public async Task<IActionResult> GetScheduledContent(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? platform = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting scheduled content from {StartDate} to {EndDate}",
                startDate, endDate);
            
            var content = await _schedulingService.GetScheduledContentAsync(
                startDate, endDate, platform, ct);

            return Ok(new { success = true, content, count = content.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduled content");
            return StatusCode(500, new { error = "Failed to get scheduled content" });
        }
    }

    /// <summary>
    /// Analyzes target audience for content planning
    /// </summary>
    [HttpPost("audience/analyze")]
    public async Task<IActionResult> AnalyzeAudience(
        [FromBody] AudienceAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Analyzing audience for platform: {Platform}", request.Platform);
            var response = await _audienceService.AnalyzeAudienceAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing audience");
            return StatusCode(500, new { error = "Failed to analyze audience" });
        }
    }

    /// <summary>
    /// Gets demographic information for a platform
    /// </summary>
    [HttpGet("audience/demographics/{platform}")]
    public async Task<IActionResult> GetDemographics(
        string platform,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting demographics for platform: {Platform}", platform);
            var demographics = await _audienceService.GetDemographicsAsync(platform, ct);
            return Ok(new { success = true, demographics, platform });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting demographics");
            return StatusCode(500, new { error = "Failed to get demographics" });
        }
    }

    /// <summary>
    /// Gets top interests for a category
    /// </summary>
    [HttpGet("audience/interests/{category}")]
    public async Task<IActionResult> GetTopInterests(
        string category,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting top interests for category: {Category}", category);
            var interests = await _audienceService.GetTopInterestsAsync(category, ct);
            return Ok(new { success = true, interests, category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top interests");
            return StatusCode(500, new { error = "Failed to get top interests" });
        }
    }
}

/// <summary>
/// Request model for scheduling content
/// </summary>
public class ScheduleContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public List<string>? Tags { get; set; }
}
