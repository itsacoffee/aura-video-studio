using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Aura.Core.Providers;
using Aura.Core.Services.Conversation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing AI conversation contexts and multi-turn conversations
/// </summary>
[ApiController]
[Route("api/conversation")]
public class ConversationController : ControllerBase
{
    private readonly ILogger<ConversationController> _logger;
    private readonly ConversationContextManager _conversationManager;
    private readonly ProjectContextManager _projectManager;
    private readonly ConversationalLlmService _llmService;
    private readonly ILlmProvider _llmProvider;

    public ConversationController(
        ILogger<ConversationController> logger,
        ConversationContextManager conversationManager,
        ProjectContextManager projectManager,
        ConversationalLlmService llmService,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _conversationManager = conversationManager;
        _projectManager = projectManager;
        _llmService = llmService;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Send a message with full context
    /// </summary>
    [HttpPost("{projectId}/message")]
    public async Task<IActionResult> SendMessage(
        string projectId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var response = await _llmService.SendMessageAsync(
                projectId,
                request.Message,
                _llmProvider,
                ct);

            return Ok(new
            {
                success = true,
                response,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to send message" });
        }
    }

    /// <summary>
    /// Get conversation history with optional pagination
    /// </summary>
    [HttpGet("{projectId}/history")]
    public async Task<IActionResult> GetHistory(
        string projectId,
        [FromQuery] int? maxMessages,
        CancellationToken ct)
    {
        try
        {
            var history = await _conversationManager.GetHistoryAsync(
                projectId,
                maxMessages,
                ct);

            return Ok(new
            {
                success = true,
                messages = history,
                count = history.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to get history" });
        }
    }

    /// <summary>
    /// Clear conversation context
    /// </summary>
    [HttpDelete("{projectId}")]
    public async Task<IActionResult> ClearConversation(
        string projectId,
        CancellationToken ct)
    {
        try
        {
            await _conversationManager.ClearHistoryAsync(projectId, ct);

            return Ok(new
            {
                success = true,
                message = "Conversation history cleared"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to clear conversation" });
        }
    }

    /// <summary>
    /// Get full project context
    /// </summary>
    [HttpGet("{projectId}/context")]
    public async Task<IActionResult> GetContext(
        string projectId,
        CancellationToken ct)
    {
        try
        {
            var projectContext = await _projectManager.GetContextAsync(projectId, ct);
            var conversationContext = await _conversationManager.GetContextAsync(projectId, ct);

            return Ok(new
            {
                success = true,
                project = projectContext,
                conversation = conversationContext
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting context for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to get context" });
        }
    }

    /// <summary>
    /// Update project metadata
    /// </summary>
    [HttpPut("{projectId}/context")]
    public async Task<IActionResult> UpdateContext(
        string projectId,
        [FromBody] UpdateContextRequest request,
        CancellationToken ct)
    {
        try
        {
            var metadata = new VideoMetadata(
                ContentType: request.ContentType,
                TargetPlatform: request.TargetPlatform,
                Audience: request.Audience,
                Tone: request.Tone,
                DurationSeconds: request.DurationSeconds,
                Keywords: request.Keywords
            );

            await _projectManager.UpdateVideoMetadataAsync(projectId, metadata, ct);

            return Ok(new
            {
                success = true,
                message = "Context updated"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating context for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to update context" });
        }
    }

    /// <summary>
    /// Record AI decision and user response
    /// </summary>
    [HttpPost("{projectId}/decision")]
    public async Task<IActionResult> RecordDecision(
        string projectId,
        [FromBody] RecordDecisionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Stage) ||
                string.IsNullOrWhiteSpace(request.Type) ||
                string.IsNullOrWhiteSpace(request.Suggestion) ||
                string.IsNullOrWhiteSpace(request.UserAction))
            {
                return BadRequest(new { error = "Stage, Type, Suggestion, and UserAction are required" });
            }

            await _projectManager.RecordDecisionAsync(
                projectId,
                request.Stage,
                request.Type,
                request.Suggestion,
                request.UserAction,
                request.UserModification,
                ct);

            return Ok(new
            {
                success = true,
                message = "Decision recorded"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording decision for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to record decision" });
        }
    }
}

/// <summary>
/// Request model for sending a message
/// </summary>
public record SendMessageRequest(string Message);

/// <summary>
/// Request model for updating project context
/// </summary>
public record UpdateContextRequest(
    string? ContentType,
    string? TargetPlatform,
    string? Audience,
    string? Tone,
    int? DurationSeconds,
    string[]? Keywords
);

/// <summary>
/// Request model for recording a decision
/// </summary>
public record RecordDecisionRequest(
    string Stage,
    string Type,
    string Suggestion,
    string UserAction,
    string? UserModification
);
