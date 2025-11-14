using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Conversation;

/// <summary>
/// Manages conversation contexts for projects, including message history and retrieval
/// </summary>
public class ConversationContextManager
{
    private readonly ILogger<ConversationContextManager> _logger;
    private readonly ContextPersistence _persistence;
    private readonly Dictionary<string, ConversationContext> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private const int DefaultMaxMessages = 100;

    public ConversationContextManager(
        ILogger<ConversationContextManager> logger,
        ContextPersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    /// <summary>
    /// Add a message to the conversation for a project
    /// </summary>
    public async Task AddMessageAsync(
        string projectId,
        string role,
        string content,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var context = await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
            
            var message = new Message(
                Role: role,
                Content: content,
                Timestamp: DateTime.UtcNow,
                Metadata: metadata
            );
            
            context.Messages.Add(message);
            
            // Update the context with new timestamp
            var updatedContext = context with { UpdatedAt = DateTime.UtcNow };
            _cache[projectId] = updatedContext;
            
            // Persist to disk
            await _persistence.SaveConversationAsync(updatedContext, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Added {Role} message to project {ProjectId}", role, projectId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Get conversation history for a project
    /// </summary>
    public async Task<IReadOnlyList<Message>> GetHistoryAsync(
        string projectId,
        int? maxMessages = null,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var context = await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
            
            var limit = maxMessages ?? DefaultMaxMessages;
            var messages = context.Messages.TakeLast(limit).ToList();
            
            return messages.AsReadOnly();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Get full conversation context for a project
    /// </summary>
    public async Task<ConversationContext> GetContextAsync(
        string projectId,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clear conversation history for a project
    /// </summary>
    public async Task ClearHistoryAsync(string projectId, CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _cache.Remove(projectId);
            await _persistence.DeleteConversationAsync(projectId, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Cleared conversation history for project {ProjectId}", projectId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Summarize conversation when it exceeds token limits
    /// </summary>
    public async Task<string> SummarizeConversationAsync(
        string projectId,
        int maxTokens = 4000,
        CancellationToken ct = default)
    {
        var history = await GetHistoryAsync(projectId, ct: ct).ConfigureAwait(false);
        
        // Simple summarization - take first and last N messages
        // In production, this would use an LLM to create a summary
        var recentMessages = history.TakeLast(10).ToList();
        var summary = $"Conversation summary for project {projectId}:\n";
        summary += $"Total messages: {history.Count}\n";
        summary += $"Recent messages:\n";
        
        foreach (var msg in recentMessages)
        {
            summary += $"[{msg.Role}]: {msg.Content.Substring(0, Math.Min(100, msg.Content.Length))}...\n";
        }
        
        return summary;
    }

    /// <summary>
    /// Get or create a conversation context for a project
    /// </summary>
    private async Task<ConversationContext> GetOrCreateContextAsync(
        string projectId,
        CancellationToken ct = default)
    {
        // Check cache first
        if (_cache.TryGetValue(projectId, out var cached))
        {
            return cached;
        }

        // Try to load from disk
        var loaded = await _persistence.LoadConversationAsync(projectId, ct).ConfigureAwait(false);
        if (loaded != null)
        {
            _cache[projectId] = loaded;
            return loaded;
        }

        // Create new context
        var newContext = new ConversationContext(
            ProjectId: projectId,
            Messages: new List<Message>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );
        
        _cache[projectId] = newContext;
        await _persistence.SaveConversationAsync(newContext, ct).ConfigureAwait(false);
        
        _logger.LogInformation("Created new conversation context for project {ProjectId}", projectId);
        
        return newContext;
    }
}
