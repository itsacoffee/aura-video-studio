using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Orchestrator;
using Aura.Core.Services.Generation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for script generation with provider selection
/// </summary>
[ApiController]
[Route("api/scripts")]
public class ScriptsController : ControllerBase
{
    private readonly ILogger<ScriptsController> _logger;
    private readonly ScriptOrchestrator _scriptOrchestrator;
    private readonly ScriptProcessor _scriptProcessor;
    private readonly ScriptCacheService _cacheService;
    private readonly ProviderMixer _providerMixer;
    
    private static readonly ConcurrentDictionary<string, Script> _scriptStore = new();

    public ScriptsController(
        ILogger<ScriptsController> logger,
        ScriptOrchestrator scriptOrchestrator,
        ScriptProcessor scriptProcessor,
        ScriptCacheService cacheService,
        ProviderMixer providerMixer)
    {
        _logger = logger;
        _scriptOrchestrator = scriptOrchestrator;
        _scriptProcessor = scriptProcessor;
        _cacheService = cacheService;
        _providerMixer = providerMixer;
    }

    /// <summary>
    /// Generate a new script using provider selection
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateScript(
        [FromBody] GenerateScriptRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] POST /api/scripts/generate - Topic: {Topic}, Provider: {Provider}",
                correlationId, request.Topic, request.PreferredProvider ?? "auto");

            var brief = new Brief(
                Topic: request.Topic,
                Audience: request.Audience,
                Goal: request.Goal,
                Tone: request.Tone,
                Language: request.Language,
                Aspect: ParseAspect(request.Aspect));

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(request.TargetDurationSeconds),
                Pacing: ParsePacing(request.Pacing),
                Density: ParseDensity(request.Density),
                Style: request.Style);

            var preferredTier = request.PreferredProvider ?? "Free";
            
            var result = await _scriptOrchestrator.GenerateScriptAsync(
                brief, planSpec, preferredTier, offlineOnly: false, ct);

            if (!result.Success || result.Script == null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Script generation failed: {ErrorCode} - {ErrorMessage}",
                    correlationId, result.ErrorCode, result.ErrorMessage);

                return StatusCode(500, new ProblemDetails
                {
                    Type = "https://docs.aura.studio/errors/E300",
                    Title = "Script Generation Failed",
                    Status = 500,
                    Detail = result.ErrorMessage ?? "Failed to generate script",
                    Extensions =
                    {
                        ["correlationId"] = correlationId,
                        ["errorCode"] = result.ErrorCode
                    }
                });
            }

            var scriptId = Guid.NewGuid().ToString();
            var script = ParseScriptFromText(result.Script, planSpec, result.ProviderUsed ?? "Unknown");
            
            script = script with { CorrelationId = correlationId };
            
            script = _scriptProcessor.ValidateSceneTiming(script, planSpec.TargetDuration);
            script = _scriptProcessor.OptimizeNarrationFlow(script);
            script = _scriptProcessor.ApplyTransitions(script, planSpec.Style);

            _scriptStore[scriptId] = script;

            _logger.LogInformation(
                "[{CorrelationId}] Script generated successfully with provider {Provider}, ID: {ScriptId}",
                correlationId, result.ProviderUsed, scriptId);

            var response = MapScriptToResponse(scriptId, script);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error generating script", correlationId);
            
            return StatusCode(500, new ProblemDetails
            {
                Type = "https://docs.aura.studio/errors/E500",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An error occurred while generating the script",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Get a previously generated script by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetScript(string id)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        if (_scriptStore.TryGetValue(id, out var script))
        {
            _logger.LogInformation("[{CorrelationId}] Retrieved script {ScriptId}", correlationId, id);
            var response = MapScriptToResponse(id, script);
            return Ok(response);
        }

        _logger.LogWarning("[{CorrelationId}] Script {ScriptId} not found", correlationId, id);
        
        return NotFound(new ProblemDetails
        {
            Type = "https://docs.aura.studio/errors/E404",
            Title = "Script Not Found",
            Status = 404,
            Detail = $"Script with ID '{id}' was not found",
            Extensions = { ["correlationId"] = correlationId }
        });
    }

    /// <summary>
    /// Update a specific scene in a script
    /// </summary>
    [HttpPut("{id}/scenes/{sceneNumber}")]
    public IActionResult UpdateScene(
        string id,
        int sceneNumber,
        [FromBody] UpdateSceneRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        if (!_scriptStore.TryGetValue(id, out var script))
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://docs.aura.studio/errors/E404",
                Title = "Script Not Found",
                Status = 404,
                Detail = $"Script with ID '{id}' was not found",
                Extensions = { ["correlationId"] = correlationId }
            });
        }

        var scene = script.Scenes.FirstOrDefault(s => s.Number == sceneNumber);
        if (scene == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://docs.aura.studio/errors/E404",
                Title = "Scene Not Found",
                Status = 404,
                Detail = $"Scene {sceneNumber} not found in script '{id}'",
                Extensions = { ["correlationId"] = correlationId }
            });
        }

        var updatedScene = scene;
        if (request.Narration != null)
        {
            updatedScene = updatedScene with { Narration = request.Narration };
        }
        if (request.VisualPrompt != null)
        {
            updatedScene = updatedScene with { VisualPrompt = request.VisualPrompt };
        }
        if (request.DurationSeconds.HasValue)
        {
            updatedScene = updatedScene with { Duration = TimeSpan.FromSeconds(request.DurationSeconds.Value) };
        }

        var updatedScenes = script.Scenes.Select(s => s.Number == sceneNumber ? updatedScene : s).ToList();
        var updatedScript = script with { Scenes = updatedScenes };
        
        _scriptStore[id] = updatedScript;

        _logger.LogInformation(
            "[{CorrelationId}] Updated scene {SceneNumber} in script {ScriptId}",
            correlationId, sceneNumber, id);

        var response = MapScriptToResponse(id, updatedScript);
        return Ok(response);
    }

    /// <summary>
    /// Regenerate a script with same or different provider
    /// </summary>
    [HttpPost("{id}/regenerate")]
    public async Task<IActionResult> RegenerateScript(
        string id,
        [FromBody] RegenerateScriptRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        if (!_scriptStore.TryGetValue(id, out var oldScript))
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://docs.aura.studio/errors/E404",
                Title = "Script Not Found",
                Status = 404,
                Detail = $"Script with ID '{id}' was not found",
                Extensions = { ["correlationId"] = correlationId }
            });
        }

        _logger.LogInformation(
            "[{CorrelationId}] Regenerating script {ScriptId} with provider {Provider}",
            correlationId, id, request.PreferredProvider ?? "auto");

        return StatusCode(501, new ProblemDetails
        {
            Type = "https://docs.aura.studio/errors/E501",
            Title = "Not Implemented",
            Status = 501,
            Detail = "Script regeneration is not yet implemented",
            Extensions = { ["correlationId"] = correlationId }
        });
    }

    /// <summary>
    /// List available LLM providers and their status
    /// </summary>
    [HttpGet("providers")]
    public async Task<IActionResult> ListProviders(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation("[{CorrelationId}] GET /api/scripts/providers", correlationId);

        var providers = new List<ProviderInfoDto>
        {
            new()
            {
                Name = "RuleBased",
                Tier = "Free",
                IsAvailable = true,
                RequiresInternet = false,
                RequiresApiKey = false,
                Capabilities = new List<string> { "offline", "deterministic", "template-based" },
                DefaultModel = "template-v1",
                EstimatedCostPer1KTokens = 0,
                AvailableModels = new List<string> { "template-v1" }
            },
            new()
            {
                Name = "Ollama",
                Tier = "Free",
                IsAvailable = false,
                RequiresInternet = false,
                RequiresApiKey = false,
                Capabilities = new List<string> { "offline", "local", "customizable" },
                DefaultModel = "llama2",
                EstimatedCostPer1KTokens = 0,
                AvailableModels = new List<string> { "llama2", "mistral", "codellama" }
            },
            new()
            {
                Name = "OpenAI",
                Tier = "Pro",
                IsAvailable = false,
                RequiresInternet = true,
                RequiresApiKey = true,
                Capabilities = new List<string> { "streaming", "function-calling", "json-mode" },
                DefaultModel = "gpt-4o-mini",
                EstimatedCostPer1KTokens = 0.0015m,
                AvailableModels = new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo" }
            },
            new()
            {
                Name = "Gemini",
                Tier = "Pro",
                IsAvailable = false,
                RequiresInternet = true,
                RequiresApiKey = true,
                Capabilities = new List<string> { "streaming", "multimodal", "long-context" },
                DefaultModel = "gemini-pro",
                EstimatedCostPer1KTokens = 0.00025m,
                AvailableModels = new List<string> { "gemini-pro", "gemini-ultra" }
            }
        };

        return Ok(new { providers, correlationId });
    }

    private Script ParseScriptFromText(string scriptText, PlanSpec planSpec, string provider)
    {
        var lines = scriptText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var title = lines.FirstOrDefault()?.Trim() ?? "Untitled Script";
        
        if (title.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
        {
            title = title.Substring(6).Trim();
        }

        var scenes = new List<ScriptScene>();
        var sceneNumber = 1;
        var totalDuration = planSpec.TargetDuration;
        var sceneDuration = TimeSpan.FromSeconds(totalDuration.TotalSeconds / Math.Max(1, lines.Length / 3));

        foreach (var line in lines.Skip(1))
        {
            if (!string.IsNullOrWhiteSpace(line) && line.Length > 10)
            {
                scenes.Add(new ScriptScene
                {
                    Number = sceneNumber++,
                    Narration = line.Trim(),
                    VisualPrompt = $"Visual for: {line.Trim().Substring(0, Math.Min(50, line.Length))}",
                    Duration = sceneDuration,
                    Transition = TransitionType.Cut
                });
            }
        }

        if (scenes.Count == 0)
        {
            scenes.Add(new ScriptScene
            {
                Number = 1,
                Narration = scriptText.Trim(),
                VisualPrompt = "Visual representation of script",
                Duration = totalDuration,
                Transition = TransitionType.Cut
            });
        }

        return new Script
        {
            Title = title,
            Scenes = scenes,
            TotalDuration = totalDuration,
            Metadata = new ScriptMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                ProviderName = provider,
                ModelUsed = "default",
                TokensUsed = scriptText.Length / 4,
                EstimatedCost = 0,
                Tier = provider == "RuleBased" ? ProviderTier.Free : ProviderTier.Pro
            }
        };
    }

    private GenerateScriptResponse MapScriptToResponse(string scriptId, Script script)
    {
        return new GenerateScriptResponse
        {
            ScriptId = scriptId,
            Title = script.Title,
            Scenes = script.Scenes.Select(s => new ScriptSceneDto
            {
                Number = s.Number,
                Narration = s.Narration,
                VisualPrompt = s.VisualPrompt,
                DurationSeconds = s.Duration.TotalSeconds,
                Transition = s.Transition.ToString()
            }).ToList(),
            TotalDurationSeconds = script.TotalDuration.TotalSeconds,
            Metadata = new ScriptMetadataDto
            {
                GeneratedAt = script.Metadata.GeneratedAt,
                ProviderName = script.Metadata.ProviderName,
                ModelUsed = script.Metadata.ModelUsed,
                TokensUsed = script.Metadata.TokensUsed,
                EstimatedCost = script.Metadata.EstimatedCost,
                Tier = script.Metadata.Tier.ToString(),
                GenerationTimeSeconds = script.Metadata.GenerationTime.TotalSeconds
            },
            CorrelationId = script.CorrelationId
        };
    }

    private Core.Models.Aspect ParseAspect(string aspect)
    {
        return aspect switch
        {
            "16:9" => Core.Models.Aspect.Widescreen16x9,
            "9:16" => Core.Models.Aspect.Vertical9x16,
            "1:1" => Core.Models.Aspect.Square1x1,
            _ => Core.Models.Aspect.Widescreen16x9
        };
    }

    private Core.Models.Pacing ParsePacing(string pacing)
    {
        return pacing.ToLowerInvariant() switch
        {
            "chill" => Core.Models.Pacing.Chill,
            "conversational" => Core.Models.Pacing.Conversational,
            "fast" => Core.Models.Pacing.Fast,
            _ => Core.Models.Pacing.Conversational
        };
    }

    private Core.Models.Density ParseDensity(string density)
    {
        return density.ToLowerInvariant() switch
        {
            "sparse" => Core.Models.Density.Sparse,
            "balanced" => Core.Models.Density.Balanced,
            "dense" => Core.Models.Density.Dense,
            _ => Core.Models.Density.Balanced
        };
    }
}
