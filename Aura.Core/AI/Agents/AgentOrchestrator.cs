using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Agents.Telemetry;
using Aura.Core.Data.Repositories;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Content;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Orchestrates the multi-agent script generation workflow
/// Coordinates Screenwriter, VisualDirector, and Critic agents
/// </summary>
public class AgentOrchestrator
{
    private readonly ScreenwriterAgent _screenwriter;
    private readonly VisualDirectorAgent _visualDirector;
    private readonly CriticAgent _critic;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ScriptParser _scriptParser;
    private readonly IVisualPromptRepository? _promptRepository;
    private readonly AgentTelemetry _telemetry;

    private const int MaxIterations = 3;

    public AgentOrchestrator(
        ScreenwriterAgent screenwriter,
        VisualDirectorAgent visualDirector,
        CriticAgent critic,
        ILogger<AgentOrchestrator> logger,
        ILoggerFactory loggerFactory,
        IVisualPromptRepository? promptRepository = null)
    {
        _screenwriter = screenwriter ?? throw new ArgumentNullException(nameof(screenwriter));
        _visualDirector = visualDirector ?? throw new ArgumentNullException(nameof(visualDirector));
        _critic = critic ?? throw new ArgumentNullException(nameof(critic));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _promptRepository = promptRepository;
        _scriptParser = new ScriptParser();
        
        // Create telemetry logger using the same factory
        var telemetryLogger = _loggerFactory.CreateLogger<AgentTelemetry>();
        _telemetry = new AgentTelemetry(telemetryLogger);
    }

    /// <summary>
    /// Generates a script using the multi-agent workflow
    /// </summary>
    public async Task<AgentOrchestratorResult> GenerateAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        var scriptId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Starting multi-agent script generation for topic: {Topic}, duration: {Duration}s, scriptId: {ScriptId}, correlationId: {CorrelationId}",
            brief.Topic,
            spec.TargetDuration.TotalSeconds,
            scriptId,
            correlationId);

        ScriptDocument? currentScript = null;
        List<VisualPrompt>? visualPrompts = null;
        var iterations = new List<AgentIteration>();

        _logger.LogInformation("Starting agentic script generation with up to {Max} iterations", MaxIterations);

        for (int i = 0; i < MaxIterations; i++)
        {
            var iterationNumber = i + 1;
            _logger.LogInformation("=== Agent Iteration {Current}/{Max} ===", iterationNumber, MaxIterations);

            try
            {
                // Step 1: Generate/revise script
                var messageType = currentScript == null ? "GenerateScript" : "ReviseScript";
                AgentResponse scriptResponse;
                using (_telemetry.TrackInvocation("Screenwriter", messageType))
                {
                    var scriptMessage = currentScript == null
                        ? new AgentMessage(
                            FromAgent: "Orchestrator",
                            ToAgent: "Screenwriter",
                            MessageType: "GenerateScript",
                            Payload: brief,
                            Context: new Dictionary<string, object> { ["planSpec"] = spec, ["brief"] = brief })
                        : new AgentMessage(
                            FromAgent: "Orchestrator",
                            ToAgent: "Screenwriter",
                            MessageType: "ReviseScript",
                            Payload: new RevisionRequest(currentScript, iterations.Last().CriticFeedback),
                            Context: new Dictionary<string, object> { ["planSpec"] = spec, ["brief"] = brief });

                    scriptResponse = await _screenwriter.ProcessAsync(scriptMessage, ct).ConfigureAwait(false);
                }

                if (!scriptResponse.Success || scriptResponse.Result == null)
                {
                    _logger.LogWarning("Screenwriter failed on iteration {Iteration}", iterationNumber);
                    iterations.Add(new AgentIteration(
                        IterationNumber: iterationNumber,
                        Script: currentScript ?? new ScriptDocument(""),
                        VisualPrompts: visualPrompts ?? new List<VisualPrompt>(),
                        CriticFeedback: scriptResponse.FeedbackForRevision ?? "Screenwriter generation failed"
                    ));
                    continue;
                }

                currentScript = (ScriptDocument)scriptResponse.Result;

                // Parse script text into structured format if needed
                if (currentScript.ParsedScript == null)
                {
                    currentScript = ParseScriptDocument(currentScript, spec);
                }

                _logger.LogInformation("Script generated/revised on iteration {Iteration} ({SceneCount} scenes)",
                    iterationNumber, currentScript.Scenes.Count);

                // Step 2: Generate visual prompts
                AgentResponse visualResponse;
                using (_telemetry.TrackInvocation("VisualDirector", "GeneratePrompts"))
                {
                    var visualMessage = new AgentMessage(
                        FromAgent: "Orchestrator",
                        ToAgent: "VisualDirector",
                        MessageType: "GeneratePrompts",
                        Payload: currentScript,
                        Context: new Dictionary<string, object> { ["brief"] = brief });

                    visualResponse = await _visualDirector.ProcessAsync(visualMessage, ct).ConfigureAwait(false);
                }

                if (!visualResponse.Success || visualResponse.Result == null)
                {
                    _logger.LogWarning("VisualDirector failed on iteration {Iteration}", iterationNumber);
                    visualPrompts = new List<VisualPrompt>();
                }
                else
                {
                    visualPrompts = (List<VisualPrompt>)visualResponse.Result;
                }

                _logger.LogInformation("Visual prompts generated on iteration {Iteration} ({Count} prompts)",
                    iterationNumber, visualPrompts.Count);

                // Step 3: Critic review
                AgentResponse criticResponse;
                using (_telemetry.TrackInvocation("Critic", "Review"))
                {
                    var criticMessage = new AgentMessage(
                        FromAgent: "Orchestrator",
                        ToAgent: "Critic",
                        MessageType: "Review",
                        Payload: currentScript,
                        Context: new Dictionary<string, object>
                        {
                            ["brief"] = brief,
                            ["planSpec"] = spec,
                            ["visualPrompts"] = visualPrompts
                        });

                    criticResponse = await _critic.ProcessAsync(criticMessage, ct).ConfigureAwait(false);
                }

                iterations.Add(new AgentIteration(
                    IterationNumber: iterationNumber,
                    Script: currentScript,
                    VisualPrompts: visualPrompts,
                    CriticFeedback: criticResponse.FeedbackForRevision
                ));

                // Record iteration decision
                _telemetry.RecordIteration(iterationNumber, !criticResponse.RequiresRevision, criticResponse.FeedbackForRevision);

                if (!criticResponse.RequiresRevision)
                {
                    _logger.LogInformation("✓ Script APPROVED by Critic on iteration {Iteration}", iterationNumber);
                    break;
                }
                else
                {
                    _logger.LogWarning("✗ Script requires revision (iteration {Iteration})", iterationNumber);
                }
            }
            catch (InvalidAgentMessageException ex)
            {
                _logger.LogError(ex, "Invalid message in iteration {Iteration}", iterationNumber);
                throw;
            }
            catch (UnknownMessageTypeException ex)
            {
                _logger.LogError(ex, "Unknown message type in iteration {Iteration}: {MessageType} for agent {AgentName}", 
                    iterationNumber, ex.MessageType, ex.AgentName);
                throw;
            }
            catch (UnknownAgentException ex)
            {
                _logger.LogError(ex, "Unknown agent referenced in iteration {Iteration}: {AgentName}", 
                    iterationNumber, ex.AgentName);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Agent orchestration cancelled on iteration {Iteration}", iterationNumber);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent iteration {Iteration}", iterationNumber);
                iterations.Add(new AgentIteration(
                    IterationNumber: iterationNumber,
                    Script: currentScript ?? new ScriptDocument(""),
                    VisualPrompts: visualPrompts ?? new List<VisualPrompt>(),
                    CriticFeedback: $"Error during iteration: {ex.Message}"
                ));
                
                // Re-throw to fail fast on unexpected errors
                throw;
            }
        }

        if (currentScript == null)
        {
            throw new InvalidOperationException("Failed to generate script after all iterations");
        }

        var approvedByCritic = iterations.Last().CriticFeedback == null;

        _logger.LogInformation(
            "Multi-agent script generation completed. Iterations: {IterationCount}, Approved: {Approved}",
            iterations.Count,
            approvedByCritic);

        // Log telemetry summary
        _telemetry.LogSummary();

        // Get performance report
        var performanceReport = _telemetry.GetReport();

        // Save visual prompts after successful generation
        if (visualPrompts != null && visualPrompts.Any() && _promptRepository != null)
        {
            _logger.LogInformation(
                "Saving {Count} visual prompts for script {ScriptId}, correlation {CorrelationId}",
                visualPrompts.Count,
                scriptId,
                correlationId);

            try
            {
                foreach (var vp in visualPrompts)
                {
                    var scene = currentScript.Scenes.ElementAtOrDefault(vp.SceneNumber - 1);
                    var storedPrompt = new StoredVisualPrompt
                    {
                        ScriptId = scriptId,
                        CorrelationId = correlationId,
                        SceneNumber = vp.SceneNumber,
                        SceneHeading = !string.IsNullOrWhiteSpace(scene?.Narration) 
                            ? scene.Narration.Substring(0, Math.Min(50, scene.Narration.Length)) 
                            : $"Scene {vp.SceneNumber}",
                        DetailedPrompt = vp.DetailedPrompt,
                        CameraAngle = vp.CameraAngle,
                        Lighting = vp.Lighting,
                        NegativePrompts = !string.IsNullOrWhiteSpace(vp.NegativePrompt)
                            ? vp.NegativePrompt.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                            : new List<string>(),
                        StyleKeywords = vp.StyleKeywords
                    };

                    await _promptRepository.SaveAsync(storedPrompt, ct).ConfigureAwait(false);
                }

                _logger.LogInformation("Successfully saved {Count} visual prompts", visualPrompts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save visual prompts, continuing without persistence");
                // Don't fail the entire operation if prompt saving fails
            }
        }

        return new AgentOrchestratorResult(
            Script: currentScript,
            VisualPrompts: visualPrompts ?? new List<VisualPrompt>(),
            Iterations: iterations,
            ApprovedByCritic: approvedByCritic,
            ScriptId: scriptId,
            CorrelationId: correlationId,
            PerformanceReport: performanceReport
        );
    }

    /// <summary>
    /// Parses script text into a structured ScriptDocument with parsed Script
    /// </summary>
    private ScriptDocument ParseScriptDocument(ScriptDocument document, PlanSpec spec)
    {
        try
        {
            var parsedScript = _scriptParser.ParseScript(document.RawText, spec);
            return document with { ParsedScript = parsedScript };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse script, using raw text only");
            return document;
        }
    }

    /// <summary>
    /// Simple script parser helper
    /// </summary>
    private class ScriptParser
    {
        public Models.Generation.Script ParseScript(string scriptText, PlanSpec spec)
        {
            var lines = scriptText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var title = lines.FirstOrDefault()?.Trim() ?? "Untitled Script";

            // Remove markdown title markers
            if (title.StartsWith("# ", StringComparison.OrdinalIgnoreCase))
            {
                title = title.Substring(2).Trim();
            }

            var scenes = new List<Models.Generation.ScriptScene>();
            var sceneNumber = 1;
            var totalDuration = spec.TargetDuration;

            // Parse scenes (looking for ## markers)
            string? currentHeading = null;
            var currentContent = new List<string>();

            foreach (var line in lines.Skip(1))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("## ", StringComparison.OrdinalIgnoreCase))
                {
                    // Save previous scene
                    if (currentHeading != null && currentContent.Count > 0)
                    {
                        var sceneText = string.Join("\n", currentContent).Trim();
                        if (!string.IsNullOrWhiteSpace(sceneText))
                        {
                            scenes.Add(new Models.Generation.ScriptScene
                            {
                                Number = sceneNumber++,
                                Narration = sceneText,
                                VisualPrompt = $"Visual for: {currentHeading}",
                                Duration = TimeSpan.FromSeconds(totalDuration.TotalSeconds / Math.Max(1, scenes.Count + 1)),
                                Transition = Models.Generation.TransitionType.Cut
                            });
                        }
                    }

                    currentHeading = trimmedLine.Substring(3).Trim();
                    currentContent.Clear();
                }
                else if (!trimmedLine.StartsWith("#", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    currentContent.Add(line);
                }
            }

            // Add last scene
            if (currentHeading != null && currentContent.Count > 0)
            {
                var sceneText = string.Join("\n", currentContent).Trim();
                if (!string.IsNullOrWhiteSpace(sceneText))
                {
                    scenes.Add(new Models.Generation.ScriptScene
                    {
                        Number = sceneNumber,
                        Narration = sceneText,
                        VisualPrompt = $"Visual for: {currentHeading}",
                        Duration = TimeSpan.FromSeconds(totalDuration.TotalSeconds / Math.Max(1, scenes.Count + 1)),
                        Transition = Models.Generation.TransitionType.Cut
                    });
                }
            }

            // If no scenes found, treat entire script as one scene
            if (scenes.Count == 0 && !string.IsNullOrWhiteSpace(scriptText))
            {
                scenes.Add(new Models.Generation.ScriptScene
                {
                    Number = 1,
                    Narration = scriptText.Trim(),
                    VisualPrompt = "Visual representation of script",
                    Duration = totalDuration,
                    Transition = Models.Generation.TransitionType.Cut
                });
            }

            // Adjust durations proportionally
            if (scenes.Count > 0)
            {
                var sceneDuration = TimeSpan.FromSeconds(totalDuration.TotalSeconds / scenes.Count);
                scenes = scenes.Select((s, i) => s with { Duration = sceneDuration }).ToList();
            }

            return new Models.Generation.Script
            {
                Title = title,
                Scenes = scenes,
                TotalDuration = totalDuration,
                Metadata = new Models.Generation.ScriptMetadata
                {
                    ProviderName = "AgentOrchestrator",
                    ModelUsed = "Multi-Agent",
                    GeneratedAt = DateTime.UtcNow,
                    Tier = Models.Generation.ProviderTier.Free
                },
                CorrelationId = Guid.NewGuid().ToString()
            };
        }
    }
}

