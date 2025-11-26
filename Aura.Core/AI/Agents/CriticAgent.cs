using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Agent responsible for validating scripts and providing feedback
/// </summary>
public class CriticAgent : IAgent
{
    private readonly ILlmProvider _llmProvider;
    private readonly ScriptSchemaValidator _validator;
    private readonly ILogger<CriticAgent> _logger;
    private readonly AgentMessageValidator _messageValidator;

    public string Name => "Critic";

    public IReadOnlyList<string> SupportedMessageTypes { get; } = new[]
    {
        "Review"
    };

    public CriticAgent(
        ILlmProvider llmProvider,
        ScriptSchemaValidator validator,
        ILogger<CriticAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageValidator = new AgentMessageValidator();
    }

    public bool CanHandle(string messageType)
    {
        return SupportedMessageTypes.Contains(messageType);
    }

    public async Task<AgentResponse> ProcessAsync(AgentMessage message, CancellationToken ct)
    {
        // Validate message
        var messageValidationResult = _messageValidator.Validate(message);
        if (!messageValidationResult.IsValid)
        {
            var errors = string.Join("; ", messageValidationResult.Errors);
            _logger.LogError("Invalid message received by CriticAgent: {Errors}", errors);
            throw new InvalidAgentMessageException(errors, message);
        }

        // Check if we can handle this message type
        if (!CanHandle(message.MessageType))
        {
            _logger.LogError(
                "CriticAgent cannot handle message type: {MessageType}",
                message.MessageType);
            throw new UnknownMessageTypeException(message.MessageType, Name);
        }

        _logger.LogInformation(
            "CriticAgent processing message type: {MessageType} from {FromAgent}",
            message.MessageType,
            message.FromAgent);

        var scriptDocument = (ScriptDocument)message.Payload;
        var brief = (Brief)message.Context!["brief"];
        var spec = (PlanSpec)message.Context!["planSpec"];
        var visualPrompts = message.Context?.ContainsKey("visualPrompts") == true
            ? message.Context["visualPrompts"] as List<VisualPrompt>
            : null;

        _logger.LogInformation("Reviewing script with {SceneCount} scenes", scriptDocument.Scenes.Count);

        // Step 1: Validate script structure and quality
        var validationResult = _validator.Validate(scriptDocument.RawText, brief, spec);

        if (validationResult.QualityScore < 0.7)
        {
            _logger.LogWarning(
                "Script quality score {QualityScore:F2} below threshold 0.7. Errors: {Errors}",
                validationResult.QualityScore,
                string.Join("; ", validationResult.Errors));

            var feedback = await GenerateFeedbackAsync(scriptDocument, validationResult, brief, spec, ct).ConfigureAwait(false);

            return new AgentResponse(
                Success: false,
                Result: null,
                FeedbackForRevision: feedback,
                RequiresRevision: true
            );
        }

        // Step 2: Check visual consistency if visual prompts are provided
        if (visualPrompts != null && visualPrompts.Any())
        {
            var consistencyIssues = await CheckVisualConsistencyAsync(visualPrompts, scriptDocument, ct).ConfigureAwait(false);
            if (consistencyIssues.Any())
            {
                _logger.LogWarning("Visual consistency issues detected: {Issues}", string.Join("; ", consistencyIssues));

                return new AgentResponse(
                    Success: false,
                    Result: null,
                    FeedbackForRevision: $"Visual consistency issues: {string.Join(", ", consistencyIssues)}",
                    RequiresRevision: true
                );
            }
        }

        // Step 3: Check narrative coherence
        var coherenceResult = await CheckNarrativeCoherenceAsync(scriptDocument, brief, ct).ConfigureAwait(false);
        if (!coherenceResult.IsCoherent)
        {
            _logger.LogWarning("Narrative coherence issues: {Issues}", coherenceResult.Issues);

            return new AgentResponse(
                Success: false,
                Result: null,
                FeedbackForRevision: $"Narrative coherence issues: {string.Join(", ", coherenceResult.Issues)}",
                RequiresRevision: true
            );
        }

        // Script approved
        _logger.LogInformation("Script approved by Critic with quality score {QualityScore:F2}", validationResult.QualityScore);

        return new AgentResponse(
            Success: true,
            Result: new ApprovedScript(scriptDocument, visualPrompts),
            FeedbackForRevision: null,
            RequiresRevision: false
        );
    }

    private async Task<string> GenerateFeedbackAsync(
        ScriptDocument script,
        ScriptSchemaValidator.ValidationResult validationResult,
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        var feedbackPrompt = $@"You are a script quality critic. Review the following script and provide specific, actionable feedback for improvement.

BRIEF:
Topic: {brief.Topic}
Audience: {brief.Audience ?? "General"}
Goal: {brief.Goal ?? "Inform and engage"}
Tone: {brief.Tone}
Target Duration: {spec.TargetDuration.TotalSeconds} seconds

SCRIPT:
{script.RawText}

VALIDATION RESULTS:
Quality Score: {validationResult.QualityScore:F2}
Errors Found: {string.Join("; ", validationResult.Errors)}
Scene Count: {validationResult.Metrics.SceneCount}
Total Characters: {validationResult.Metrics.TotalCharacters}
Has Introduction: {validationResult.Metrics.HasIntroduction}
Has Conclusion: {validationResult.Metrics.HasConclusion}
Readability Score: {validationResult.Metrics.ReadabilityScore:F2}

Provide concise, actionable feedback (2-3 sentences) that will help improve the script quality. Focus on the most critical issues first.";

        try
        {
            var feedback = await _llmProvider.CompleteAsync(feedbackPrompt, ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(feedback) 
                ? $"Script quality score {validationResult.QualityScore:F2} is below threshold. Issues: {string.Join("; ", validationResult.Errors)}"
                : feedback.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate LLM feedback, using validation errors");
            return $"Script quality score {validationResult.QualityScore:F2} is below threshold. Issues: {string.Join("; ", validationResult.Errors)}";
        }
    }

    private Task<List<string>> CheckVisualConsistencyAsync(
        List<VisualPrompt> visualPrompts,
        ScriptDocument script,
        CancellationToken ct)
    {
        var issues = new List<string>();

        // Basic consistency checks
        if (visualPrompts.Count != script.Scenes.Count)
        {
            issues.Add($"Visual prompt count ({visualPrompts.Count}) doesn't match scene count ({script.Scenes.Count})");
        }

        // Check for scene number consistency
        var sceneNumbers = visualPrompts.Select(vp => vp.SceneNumber).OrderBy(n => n).ToList();
        var expectedNumbers = Enumerable.Range(1, script.Scenes.Count).ToList();
        if (!sceneNumbers.SequenceEqual(expectedNumbers))
        {
            issues.Add($"Visual prompt scene numbers don't match expected sequence");
        }

        // Could add more sophisticated checks using LLM for visual style consistency
        // For now, we'll keep it simple

        return Task.FromResult(issues);
    }

    private async Task<CoherenceCheckResult> CheckNarrativeCoherenceAsync(
        ScriptDocument script,
        Brief brief,
        CancellationToken ct)
    {
        if (script.Scenes.Count < 2)
        {
            return new CoherenceCheckResult(IsCoherent: true, Issues: new List<string>());
        }

        try
        {
            // Use LLM to check narrative coherence between scenes
            var sceneTexts = script.Scenes.Select(s => s.Narration).ToList();
            var narrativeResult = await _llmProvider.ValidateNarrativeArcAsync(
                sceneTexts,
                brief.Goal ?? brief.Topic,
                "Educational", // Could be derived from brief
                ct
            ).ConfigureAwait(false);

            if (narrativeResult != null && !narrativeResult.IsValid)
            {
                return new CoherenceCheckResult(
                    IsCoherent: false,
                    Issues: narrativeResult.StructuralIssues?.ToList() ?? new List<string>()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check narrative coherence, assuming coherent");
        }

        return new CoherenceCheckResult(IsCoherent: true, Issues: new List<string>());
    }

    private record CoherenceCheckResult(bool IsCoherent, List<string> Issues);
}

