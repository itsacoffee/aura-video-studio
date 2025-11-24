using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Agent responsible for generating and revising scripts
/// </summary>
public class ScreenwriterAgent : IAgent
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<ScreenwriterAgent> _logger;

    public string Name => "Screenwriter";

    public ScreenwriterAgent(
        ILlmProvider llmProvider,
        ILogger<ScreenwriterAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResponse> ProcessAsync(AgentMessage message, CancellationToken ct)
    {
        _logger.LogInformation("ScreenwriterAgent processing message type: {MessageType}", message.MessageType);

        return message.MessageType switch
        {
            "GenerateScript" => await GenerateScriptAsync(message, ct),
            "ReviseScript" => await ReviseScriptAsync(message, ct),
            _ => throw new ArgumentException($"Unknown message type: {message.MessageType}")
        };
    }

    private async Task<AgentResponse> GenerateScriptAsync(AgentMessage message, CancellationToken ct)
    {
        var brief = (Brief)message.Payload;
        var spec = (PlanSpec)message.Context!["planSpec"];

        _logger.LogInformation("Generating script for topic: {Topic}", brief.Topic);

        var scriptText = await _llmProvider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(scriptText))
        {
            _logger.LogWarning("LLM provider returned empty script");
            return new AgentResponse(
                Success: false,
                Result: null,
                FeedbackForRevision: "Script generation returned empty result",
                RequiresRevision: true
            );
        }

        var scriptDocument = new ScriptDocument(RawText: scriptText);

        _logger.LogInformation("Script generated successfully ({Length} characters)", scriptText.Length);

        return new AgentResponse(
            Success: true,
            Result: scriptDocument,
            FeedbackForRevision: null,
            RequiresRevision: false
        );
    }

    private async Task<AgentResponse> ReviseScriptAsync(AgentMessage message, CancellationToken ct)
    {
        var revisionRequest = (RevisionRequest)message.Payload;
        var brief = (Brief)message.Context!["brief"];
        var spec = (PlanSpec)message.Context!["planSpec"];

        _logger.LogInformation("Revising script based on feedback: {Feedback}", revisionRequest.Feedback);

        // Build revision prompt with feedback
        var revisionPrompt = BuildRevisionPrompt(brief, spec, revisionRequest);

        // Use CompleteAsync for revision since we need to pass the existing script and feedback
        var revisedScriptText = await _llmProvider.CompleteAsync(revisionPrompt, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(revisedScriptText))
        {
            _logger.LogWarning("Script revision returned empty result");
            return new AgentResponse(
                Success: false,
                Result: null,
                FeedbackForRevision: "Script revision returned empty result",
                RequiresRevision: true
            );
        }

        var revisedScriptDocument = new ScriptDocument(RawText: revisedScriptText);

        _logger.LogInformation("Script revised successfully ({Length} characters)", revisedScriptText.Length);

        return new AgentResponse(
            Success: true,
            Result: revisedScriptDocument,
            FeedbackForRevision: null,
            RequiresRevision: false
        );
    }

    private string BuildRevisionPrompt(Brief brief, PlanSpec spec, RevisionRequest revisionRequest)
    {
        var prompt = $@"You are revising a video script based on feedback from a quality critic.

ORIGINAL BRIEF:
Topic: {brief.Topic}
Audience: {brief.Audience ?? "General"}
Goal: {brief.Goal ?? "Inform and engage"}
Tone: {brief.Tone}
Target Duration: {spec.TargetDuration.TotalSeconds} seconds
Pacing: {spec.Pacing}
Density: {spec.Density}
Style: {spec.Style}

CURRENT SCRIPT:
{revisionRequest.CurrentScript.RawText}

CRITIC FEEDBACK:
{revisionRequest.Feedback ?? "No specific feedback provided"}

INSTRUCTIONS:
1. Revise the script to address the critic's feedback
2. Maintain the original topic, tone, and style
3. Ensure the script meets the target duration requirements
4. Keep the script structure (title with #, scenes with ##)
5. Improve quality based on the feedback provided

Please provide the revised script:";

        return prompt;
    }
}

