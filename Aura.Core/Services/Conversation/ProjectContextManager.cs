using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Conversation;

/// <summary>
/// Manages project contexts including video metadata and decision history
/// </summary>
public class ProjectContextManager
{
    private readonly ILogger<ProjectContextManager> _logger;
    private readonly ContextPersistence _persistence;
    private readonly Dictionary<string, ProjectContext> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public ProjectContextManager(
        ILogger<ProjectContextManager> logger,
        ContextPersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    /// <summary>
    /// Get or create project context
    /// </summary>
    public async Task<ProjectContext> GetOrCreateContextAsync(
        string projectId,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Check cache first
            if (_cache.TryGetValue(projectId, out var cached))
            {
                return cached;
            }

            // Try to load from disk
            var loaded = await _persistence.LoadProjectContextAsync(projectId, ct).ConfigureAwait(false);
            if (loaded != null)
            {
                _cache[projectId] = loaded;
                return loaded;
            }

            // Create new context
            var newContext = new ProjectContext(
                ProjectId: projectId,
                VideoMetadata: null,
                DecisionHistory: new List<AiDecision>(),
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                Metadata: null
            );
            
            _cache[projectId] = newContext;
            await _persistence.SaveProjectContextAsync(newContext, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Created new project context for project {ProjectId}", projectId);
            
            return newContext;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Update video metadata for a project
    /// </summary>
    public async Task UpdateVideoMetadataAsync(
        string projectId,
        VideoMetadata metadata,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var context = await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
            
            var updatedContext = context with
            {
                VideoMetadata = metadata,
                UpdatedAt = DateTime.UtcNow
            };
            
            _cache[projectId] = updatedContext;
            await _persistence.SaveProjectContextAsync(updatedContext, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Updated video metadata for project {ProjectId}", projectId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Record an AI decision and user response
    /// </summary>
    public async Task RecordDecisionAsync(
        string projectId,
        string stage,
        string type,
        string suggestion,
        string userAction,
        string? userModification = null,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var context = await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
            
            var decision = new AiDecision(
                DecisionId: Guid.NewGuid().ToString(),
                Stage: stage,
                Type: type,
                Suggestion: suggestion,
                UserAction: userAction,
                UserModification: userModification,
                Timestamp: DateTime.UtcNow
            );
            
            context.DecisionHistory.Add(decision);
            
            var updatedContext = context with { UpdatedAt = DateTime.UtcNow };
            _cache[projectId] = updatedContext;
            
            await _persistence.SaveProjectContextAsync(updatedContext, ct).ConfigureAwait(false);
            
            _logger.LogInformation(
                "Recorded decision for project {ProjectId}: {Stage}/{Type} -> {Action}",
                projectId, stage, type, userAction);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Get decision history for a project
    /// </summary>
    public async Task<IReadOnlyList<AiDecision>> GetDecisionHistoryAsync(
        string projectId,
        string? stage = null,
        CancellationToken ct = default)
    {
        var context = await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
        
        var decisions = context.DecisionHistory.AsEnumerable();
        
        if (!string.IsNullOrEmpty(stage))
        {
            decisions = decisions.Where(d => d.Stage == stage);
        }
        
        return decisions.ToList().AsReadOnly();
    }

    /// <summary>
    /// Get project context
    /// </summary>
    public async Task<ProjectContext> GetContextAsync(
        string projectId,
        CancellationToken ct = default)
    {
        return await GetOrCreateContextAsync(projectId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete project context
    /// </summary>
    public async Task DeleteContextAsync(string projectId, CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _cache.Remove(projectId);
            await _persistence.DeleteProjectContextAsync(projectId, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Deleted project context for project {ProjectId}", projectId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Get list of all project IDs
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAllProjectIdsAsync()
    {
        return await _persistence.GetAllProjectIdsAsync().ConfigureAwait(false);
    }
}
