using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Ideation;
using Aura.Core.Services.Ideation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered ideation and brainstorming
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IdeationController : ControllerBase
{
    private readonly ILogger<IdeationController> _logger;
    private readonly IdeationService _ideationService;

    public IdeationController(
        ILogger<IdeationController> logger,
        IdeationService ideationService)
    {
        _logger = logger;
        _ideationService = ideationService;
    }

    /// <summary>
    /// Generate initial concept variations from a topic
    /// </summary>
    [HttpPost("brainstorm")]
    public async Task<IActionResult> Brainstorm(
        [FromBody] BrainstormRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
            {
                return BadRequest(new { error = "Topic is required" });
            }

            var response = await _ideationService.BrainstormConceptsAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                concepts = response.Concepts,
                originalTopic = response.OriginalTopic,
                generatedAt = response.GeneratedAt,
                count = response.Concepts.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error brainstorming concepts for topic: {Topic}", request.Topic);
            return StatusCode(500, new { error = "Failed to generate concepts" });
        }
    }

    /// <summary>
    /// Ask AI to expand/clarify the brief
    /// </summary>
    [HttpPost("expand-brief")]
    public async Task<IActionResult> ExpandBrief(
        [FromBody] ExpandBriefRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProjectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            if (request.CurrentBrief == null)
            {
                return BadRequest(new { error = "CurrentBrief is required" });
            }

            var response = await _ideationService.ExpandBriefAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                updatedBrief = response.UpdatedBrief,
                questions = response.Questions,
                aiResponse = response.AiResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding brief for project: {ProjectId}", request.ProjectId);
            return StatusCode(500, new { error = "Failed to expand brief" });
        }
    }

    /// <summary>
    /// Get trending topics for a niche
    /// </summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(
        [FromQuery] string? niche = null,
        [FromQuery] int? maxResults = 10,
        CancellationToken ct = default)
    {
        try
        {
            var request = new TrendingTopicsRequest(
                Niche: niche,
                MaxResults: maxResults
            );

            var response = await _ideationService.GetTrendingTopicsAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                topics = response.Topics,
                analyzedAt = response.AnalyzedAt,
                count = response.Topics.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending topics for niche: {Niche}", niche);
            return StatusCode(500, new { error = "Failed to get trending topics" });
        }
    }

    /// <summary>
    /// Analyze content gaps and opportunities
    /// </summary>
    [HttpPost("gap-analysis")]
    public async Task<IActionResult> AnalyzeGaps(
        [FromBody] GapAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _ideationService.AnalyzeContentGapsAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                missingTopics = response.MissingTopics,
                opportunities = response.Opportunities,
                oversaturatedTopics = response.OversaturatedTopics,
                uniqueAngles = response.UniqueAngles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content gaps for niche: {Niche}", request.Niche);
            return StatusCode(500, new { error = "Failed to analyze content gaps" });
        }
    }

    /// <summary>
    /// Gather facts and examples for a topic
    /// </summary>
    [HttpPost("research")]
    public async Task<IActionResult> GatherResearch(
        [FromBody] ResearchRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
            {
                return BadRequest(new { error = "Topic is required" });
            }

            var response = await _ideationService.GatherResearchAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                findings = response.Findings,
                topic = response.Topic,
                gatheredAt = response.GatheredAt,
                count = response.Findings.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error gathering research for topic: {Topic}", request.Topic);
            return StatusCode(500, new { error = "Failed to gather research" });
        }
    }

    /// <summary>
    /// Generate visual storyboard for a concept
    /// </summary>
    [HttpPost("storyboard")]
    public async Task<IActionResult> GenerateStoryboard(
        [FromBody] StoryboardRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.Concept == null)
            {
                return BadRequest(new { error = "Concept is required" });
            }

            if (request.TargetDurationSeconds <= 0)
            {
                return BadRequest(new { error = "TargetDurationSeconds must be positive" });
            }

            var response = await _ideationService.GenerateStoryboardAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                scenes = response.Scenes,
                conceptTitle = response.ConceptTitle,
                totalDurationSeconds = response.TotalDurationSeconds,
                generatedAt = response.GeneratedAt,
                sceneCount = response.Scenes.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating storyboard for concept: {ConceptTitle}", 
                request.Concept?.Title);
            return StatusCode(500, new { error = "Failed to generate storyboard" });
        }
    }

    /// <summary>
    /// Refine a selected concept
    /// </summary>
    [HttpPost("refine")]
    public async Task<IActionResult> RefineConcept(
        [FromBody] RefineConceptRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.Concept == null)
            {
                return BadRequest(new { error = "Concept is required" });
            }

            if (string.IsNullOrWhiteSpace(request.RefinementDirection))
            {
                return BadRequest(new { error = "RefinementDirection is required" });
            }

            var response = await _ideationService.RefineConceptAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                refinedConcept = response.RefinedConcept,
                changesSummary = response.ChangesSummary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refining concept: {ConceptTitle} with direction: {Direction}", 
                request.Concept?.Title, request.RefinementDirection);
            return StatusCode(500, new { error = "Failed to refine concept" });
        }
    }

    /// <summary>
    /// Get clarifying questions from AI
    /// </summary>
    [HttpPost("questions")]
    public async Task<IActionResult> GetQuestions(
        [FromBody] QuestionsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProjectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            var response = await _ideationService.GetClarifyingQuestionsAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                questions = response.Questions,
                context = response.Context,
                count = response.Questions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for project: {ProjectId}", request.ProjectId);
            return StatusCode(500, new { error = "Failed to get clarifying questions" });
        }
    }

    /// <summary>
    /// Convert freeform idea into structured brief with multiple variants
    /// </summary>
    [HttpPost("idea-to-brief")]
    public async Task<IActionResult> IdeaToBrief(
        [FromBody] IdeaToBriefRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Idea))
            {
                return BadRequest(new { error = "Idea is required" });
            }

            var response = await _ideationService.IdeaToBriefAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                variants = response.Variants,
                originalIdea = response.OriginalIdea,
                generatedAt = response.GeneratedAt,
                count = response.Variants.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting idea to brief: {Idea}", request.Idea);
            return StatusCode(500, new { error = "Failed to convert idea to brief" });
        }
    }

    /// <summary>
    /// Enhance/improve a video topic description using AI
    /// </summary>
    [HttpPost("enhance-topic")]
    public async Task<IActionResult> EnhanceTopic(
        [FromBody] EnhanceTopicRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
            {
                return BadRequest(new { error = "Topic is required" });
            }

            var response = await _ideationService.EnhanceTopicAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                enhancedTopic = response.EnhancedTopic,
                originalTopic = response.OriginalTopic,
                improvements = response.Improvements,
                generatedAt = response.GeneratedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing topic: {Topic}", request.Topic);
            return StatusCode(500, new { error = "Failed to enhance topic" });
        }
    }
}
