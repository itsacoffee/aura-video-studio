using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Conversation;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Conversation;

/// <summary>
/// Enhanced LLM service that supports multi-turn conversations with context enrichment
/// </summary>
public class ConversationalLlmService
{
    private readonly ILogger<ConversationalLlmService> _logger;
    private readonly ConversationContextManager _conversationManager;
    private readonly ProjectContextManager _projectManager;

    public ConversationalLlmService(
        ILogger<ConversationalLlmService> logger,
        ConversationContextManager conversationManager,
        ProjectContextManager projectManager)
    {
        _logger = logger;
        _conversationManager = conversationManager;
        _projectManager = projectManager;
    }

    /// <summary>
    /// Send a message with full conversation context
    /// </summary>
    public async Task<string> SendMessageAsync(
        string projectId,
        string userMessage,
        ILlmProvider llmProvider,
        CancellationToken ct = default)
    {
        // Add user message to conversation history
        await _conversationManager.AddMessageAsync(
            projectId,
            "user",
            userMessage,
            ct: ct).ConfigureAwait(false);

        // Get project context for enrichment
        var projectContext = await _projectManager.GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
        
        // Build enriched context
        var enrichedContext = await BuildEnrichedContextAsync(projectId, projectContext, ct).ConfigureAwait(false);
        
        // Create brief with enriched context
        var brief = new Brief(
            Topic: enrichedContext,
            Audience: projectContext.VideoMetadata?.Audience ?? "General",
            Goal: "Assist with video creation",
            Tone: projectContext.VideoMetadata?.Tone ?? "Professional",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(projectContext.VideoMetadata?.DurationSeconds ?? 60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Conversational"
        );
        
        // Get response from LLM
        string response;
        try
        {
            response = await llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LLM response for project {ProjectId}", projectId);
            response = "I apologize, but I encountered an error processing your request. Please try again.";
        }
        
        // Add assistant response to conversation history
        await _conversationManager.AddMessageAsync(
            projectId,
            "assistant",
            response,
            ct: ct).ConfigureAwait(false);
        
        return response;
    }

    /// <summary>
    /// Build enriched context string with conversation history and project metadata
    /// </summary>
    private async Task<string> BuildEnrichedContextAsync(
        string projectId,
        ProjectContext projectContext,
        CancellationToken ct = default)
    {
        var context = new StringBuilder();
        
        // Add project metadata
        if (projectContext.VideoMetadata != null)
        {
            context.AppendLine("Project Context:");
            context.AppendLine($"- Content Type: {projectContext.VideoMetadata.ContentType}");
            context.AppendLine($"- Target Platform: {projectContext.VideoMetadata.TargetPlatform}");
            context.AppendLine($"- Audience: {projectContext.VideoMetadata.Audience}");
            context.AppendLine($"- Tone: {projectContext.VideoMetadata.Tone}");
            context.AppendLine($"- Duration: {projectContext.VideoMetadata.DurationSeconds}s");
            
            if (projectContext.VideoMetadata.Keywords?.Length > 0)
            {
                context.AppendLine($"- Keywords: {string.Join(", ", projectContext.VideoMetadata.Keywords)}");
            }
            
            context.AppendLine();
        }
        
        // Add recent conversation history
        var history = await _conversationManager.GetHistoryAsync(projectId, maxMessages: 10, ct: ct).ConfigureAwait(false);
        
        if (history.Count > 0)
        {
            context.AppendLine("Recent Conversation:");
            foreach (var message in history)
            {
                context.AppendLine($"[{message.Role}]: {message.Content}");
            }
            context.AppendLine();
        }
        
        // Add decision history summary
        var decisions = await _projectManager.GetDecisionHistoryAsync(projectId, ct: ct).ConfigureAwait(false);
        if (decisions.Count > 0)
        {
            context.AppendLine("Previous Decisions:");
            var recentDecisions = decisions.TakeLast(5);
            foreach (var decision in recentDecisions)
            {
                context.AppendLine($"- {decision.Stage}: {decision.UserAction} ({decision.Type})");
            }
            context.AppendLine();
        }
        
        return context.ToString();
    }
}
