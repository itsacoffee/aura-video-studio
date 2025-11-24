using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
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
    private readonly ScriptParser _scriptParser;

    private const int MaxIterations = 3;

    public AgentOrchestrator(
        ScreenwriterAgent screenwriter,
        VisualDirectorAgent visualDirector,
        CriticAgent critic,
        ILogger<AgentOrchestrator> logger)
    {
        _screenwriter = screenwriter ?? throw new ArgumentNullException(nameof(screenwriter));
        _visualDirector = visualDirector ?? throw new ArgumentNullException(nameof(visualDirector));
        _critic = critic ?? throw new ArgumentNullException(nameof(critic));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scriptParser = new ScriptParser();
    }

    /// <summary>
    /// Generates a script using the multi-agent workflow
    /// </summary>
    public async Task<AgentOrchestratorResult> GenerateAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting multi-agent script generation for topic: {Topic}, duration: {Duration}s",
            brief.Topic,
            spec.TargetDuration.TotalSeconds);

        ScriptDocument? currentScript = null;
        List<VisualPrompt>? visualPrompts = null;
        var iterations = new List<AgentIteration>();

        for (int i = 0; i < MaxIterations; i++)
        {
            var iterationNumber = i + 1;
            _logger.LogInformation("Agent iteration {Iteration}/{Max}", iterationNumber, MaxIterations);

            try
            {
                // Step 1: Generate/revise script
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

                var scriptResponse = await _screenwriter.ProcessAsync(scriptMessage, ct).ConfigureAwait(false);

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
                var visualMessage = new AgentMessage(
                    FromAgent: "Orchestrator",
                    ToAgent: "VisualDirector",
                    MessageType: "GeneratePrompts",
                    Payload: currentScript,
                    Context: new Dictionary<string, object> { ["brief"] = brief });

                var visualResponse = await _visualDirector.ProcessAsync(visualMessage, ct).ConfigureAwait(false);

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

                var criticResponse = await _critic.ProcessAsync(criticMessage, ct).ConfigureAwait(false);

                iterations.Add(new AgentIteration(
                    IterationNumber: iterationNumber,
                    Script: currentScript,
                    VisualPrompts: visualPrompts,
                    CriticFeedback: criticResponse.FeedbackForRevision
                ));

                if (!criticResponse.RequiresRevision)
                {
                    _logger.LogInformation("Script approved by Critic on iteration {Iteration}", iterationNumber);
                    break;
                }

                _logger.LogInformation("Critic requested revision on iteration {Iteration}: {Feedback}",
                    iterationNumber, criticResponse.FeedbackForRevision);
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

        return new AgentOrchestratorResult(
            Script: currentScript,
            VisualPrompts: visualPrompts ?? new List<VisualPrompt>(),
            Iterations: iterations,
            ApprovedByCritic: approvedByCritic
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

