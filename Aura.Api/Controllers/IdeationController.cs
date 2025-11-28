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
    private readonly Aura.Core.Services.RAG.VectorIndex? _vectorIndex;

    public IdeationController(
        ILogger<IdeationController> logger,
        IdeationService ideationService,
        Aura.Core.Services.RAG.VectorIndex? vectorIndex = null)
    {
        _logger = logger;
        _ideationService = ideationService;
        _vectorIndex = vectorIndex;
    }

    /// <summary>
    /// Generate initial concept variations from a topic
    /// </summary>
    [HttpPost("brainstorm")]
    public async Task<IActionResult> Brainstorm(
        [FromBody] BrainstormRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
            {
                return BadRequest(new { error = "Topic is required" });
            }

            _logger.LogInformation(
                "[{CorrelationId}] POST /api/ideation/brainstorm - Topic: {Topic}, ConceptCount: {Count}",
                correlationId, request.Topic, request.ConceptCount ?? 3);

            // Handle RAG configuration: use from request, or auto-enable if documents exist
            Aura.Core.Models.RagConfiguration? ragConfig = request.RagConfiguration;

            // If no explicit configuration, auto-enable if documents exist in the index
            if (ragConfig == null && _vectorIndex != null)
            {
                try
                {
                    var stats = await _vectorIndex.GetStatisticsAsync(ct).ConfigureAwait(false);
                    if (stats.TotalDocuments > 0)
                    {
                        _logger.LogInformation(
                            "[{CorrelationId}] RAG index contains {DocumentCount} documents, auto-enabling RAG for ideation",
                            correlationId, stats.TotalDocuments);
                        ragConfig = new Aura.Core.Models.RagConfiguration(
                            Enabled: true,
                            TopK: 5,
                            MinimumScore: 0.6f,
                            MaxContextTokens: 2000,
                            IncludeCitations: true,
                            TightenClaims: false);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "[{CorrelationId}] RAG index is empty, skipping RAG enhancement for ideation",
                            correlationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[{CorrelationId}] Failed to check RAG index status, continuing without RAG",
                        correlationId);
                }
            }
            else if (ragConfig != null && ragConfig.Enabled)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] RAG explicitly enabled via request with TopK={TopK}, MinScore={MinScore}",
                    correlationId, ragConfig.TopK, ragConfig.MinimumScore);
            }

            // Create updated request with RAG configuration
            var requestWithRag = request with
            {
                RagConfiguration = ragConfig,
                LlmParameters = request.LlmParameters
            };

            var response = await _ideationService.BrainstormConceptsAsync(requestWithRag, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                concepts = response.Concepts,
                originalTopic = response.OriginalTopic,
                generatedAt = response.GeneratedAt,
                count = response.Concepts.Count
            });
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "[{CorrelationId}] Invalid argument for brainstorming: {Message}", correlationId, argEx.Message);
            return BadRequest(new { error = argEx.Message });
        }
        catch (InvalidOperationException invOpEx)
        {
            _logger.LogError(invOpEx, "[{CorrelationId}] Ideation operation failed: {Message}",
                correlationId, invOpEx.Message);

            // Provide detailed error message to help user diagnose
            var errorMessage = invOpEx.Message;
            var suggestions = new List<string>();

            // Add Ollama-specific error handling
            if (errorMessage.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase))
            {
                if (errorMessage.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add("Ensure Ollama is running: Open a terminal and run 'ollama serve'");
                    suggestions.Add("Verify Ollama is installed: Visit https://ollama.com to download");
                    suggestions.Add("Check Ollama base URL in Settings (default: http://localhost:11434)");
                }
                else if (errorMessage.Contains("model", StringComparison.OrdinalIgnoreCase) &&
                         errorMessage.Contains("not installed", StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add("Install the requested model: Run 'ollama pull <model-name>' in terminal");
                    suggestions.Add("List available models: Run 'ollama list' to see installed models");
                    suggestions.Add("Check model name in Settings matches an installed model");
                }
                else
                {
                    suggestions.Add("Check Ollama service status: Run 'ollama list' to verify it's working");
                    suggestions.Add("Restart Ollama service if needed");
                    suggestions.Add("Verify GPU drivers are installed for GPU acceleration (optional)");
                }
            }
            // Add helpful context based on error type
            else if (errorMessage.Contains("generic placeholder content", StringComparison.OrdinalIgnoreCase))
            {
                // This error can occur with Ollama if the model is too small or not properly configured
                errorMessage = "Failed to generate concepts. " +
                              "The LLM generated generic placeholder content instead of specific concepts. " +
                              "This usually means: (1) The LLM provider is not properly configured, " +
                              "(2) The model is not responding correctly, or (3) The prompt needs adjustment.";
                suggestions.Add("If using Ollama: Ensure the model is fully loaded (check 'ollama list')");
                suggestions.Add("If using Ollama: Try a larger model (e.g., llama3.1:8b or larger)");
                suggestions.Add("If using Ollama: Check that Ollama is using GPU acceleration if available");
                suggestions.Add("If using Ollama: Verify the model is responding correctly (test with 'ollama run <model-name>')");
                suggestions.Add("Verify your LLM provider is configured correctly in Settings");
                suggestions.Add("Try using a different LLM model");
                suggestions.Add("Simplify your topic description - make it more specific");
            }
            else if (errorMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage += " The LLM provider may not be configured to return JSON format. " +
                               "Try using a different model or provider.";
                suggestions.Add("Verify your LLM provider is configured correctly in Settings");
                suggestions.Add("Try using a different LLM model");
            }
            else if (errorMessage.Contains("empty response", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage += " The LLM provider may be unavailable or rate-limited. " +
                               "Please check your API key and quota.";
                suggestions.Add("Check that your API key has sufficient quota");
                suggestions.Add("Verify your LLM provider is configured correctly in Settings");
            }
            else
            {
                // Generic suggestions
                suggestions.Add("Verify your LLM provider is configured correctly in Settings");
                suggestions.Add("Check that your API key has sufficient quota");
                suggestions.Add("Try using a different LLM model");
            }

            // Always add these general suggestions
            if (!suggestions.Contains("Simplify your topic description"))
            {
                suggestions.Add("Simplify your topic description");
            }

            return StatusCode(500, new {
                error = errorMessage,
                correlationId,
                suggestions = suggestions.ToArray()
            });
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogError(timeoutEx, "[{CorrelationId}] Timeout during brainstorming for topic: {Topic}", correlationId, request?.Topic ?? "unknown");
            return StatusCode(504, new {
                error = timeoutEx.Message,
                correlationId,
                suggestions = new[] {
                    "The request took too long. Try simplifying your topic",
                    "Check your network connection",
                    "Try using a faster LLM model"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Unexpected error in ideation", correlationId);

            var errorMessage = $"An unexpected error occurred: {ex.Message}";
            var suggestions = new List<string>();

            // Check for Ollama-specific errors
            if (ex.Message.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
                ex is HttpRequestException httpEx && httpEx.Message.Contains("localhost:11434", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("Ensure Ollama is running: Open a terminal and run 'ollama serve'");
                suggestions.Add("Verify Ollama is installed: Visit https://ollama.com to download");
                suggestions.Add("Check Ollama base URL in Settings (default: http://localhost:11434)");
                suggestions.Add("Check GPU drivers are installed for GPU acceleration (optional)");
            }
            else
            {
                suggestions.Add("Verify your LLM provider is configured correctly in Settings");
                suggestions.Add("Check that your API key has sufficient quota");
                suggestions.Add("Try using a different LLM model");
            }

            suggestions.Add("Simplify your topic description");

            return StatusCode(500, new {
                error = errorMessage,
                correlationId,
                type = ex.GetType().Name,
                suggestions = suggestions.ToArray()
            });
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

    /// <summary>
    /// Analyze prompt quality using LLM-based analysis with RAG support
    /// </summary>
    [HttpPost("analyze-prompt-quality")]
    public async Task<IActionResult> AnalyzePromptQuality(
        [FromBody] AnalyzePromptQualityRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
            {
                return BadRequest(new { error = "Topic is required" });
            }

            var response = await _ideationService.AnalyzePromptQualityAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                score = response.Score,
                level = response.Level,
                metrics = response.Metrics,
                suggestions = response.Suggestions.Select(s => new
                {
                    type = s.Type,
                    message = s.Message
                }),
                generatedAt = response.GeneratedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing prompt quality for topic: {Topic}", request.Topic);
            return StatusCode(500, new { error = "Failed to analyze prompt quality" });
        }
    }
}
