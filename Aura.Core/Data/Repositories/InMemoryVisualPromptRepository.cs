using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;

namespace Aura.Core.Data.Repositories;

/// <summary>
/// In-memory implementation of visual prompt repository
/// Can be replaced with database-backed implementation later
/// </summary>
public class InMemoryVisualPromptRepository : IVisualPromptRepository
{
    private readonly ConcurrentDictionary<string, StoredVisualPrompt> _prompts = new();

    public Task<StoredVisualPrompt> SaveAsync(StoredVisualPrompt prompt, CancellationToken ct = default)
    {
        _prompts[prompt.Id] = prompt;
        return Task.FromResult(prompt);
    }

    public Task<StoredVisualPrompt?> GetByIdAsync(string promptId, CancellationToken ct = default)
    {
        _prompts.TryGetValue(promptId, out var prompt);
        return Task.FromResult(prompt);
    }

    public Task<List<StoredVisualPrompt>> GetByScriptIdAsync(string scriptId, CancellationToken ct = default)
    {
        var prompts = _prompts.Values
            .Where(p => p.ScriptId == scriptId)
            .OrderBy(p => p.SceneNumber)
            .ToList();

        return Task.FromResult(prompts);
    }

    public Task<StoredVisualPrompt?> GetBySceneAsync(string scriptId, int sceneNumber, CancellationToken ct = default)
    {
        var prompt = _prompts.Values
            .FirstOrDefault(p => p.ScriptId == scriptId && p.SceneNumber == sceneNumber);

        return Task.FromResult(prompt);
    }

    public Task<List<StoredVisualPrompt>> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        var prompts = _prompts.Values
            .Where(p => p.CorrelationId == correlationId)
            .OrderBy(p => p.SceneNumber)
            .ToList();

        return Task.FromResult(prompts);
    }

    public Task<StoredVisualPrompt?> UpdateAsync(string promptId, UpdateVisualPromptRequest update, CancellationToken ct = default)
    {
        if (!_prompts.TryGetValue(promptId, out var existing))
            return Task.FromResult<StoredVisualPrompt?>(null);

        var updated = existing with
        {
            DetailedPrompt = update.DetailedPrompt ?? existing.DetailedPrompt,
            CameraAngle = update.CameraAngle ?? existing.CameraAngle,
            Lighting = update.Lighting ?? existing.Lighting,
            NegativePrompts = update.NegativePrompts ?? existing.NegativePrompts,
            StyleKeywords = update.StyleKeywords ?? existing.StyleKeywords,
            UpdatedAt = DateTime.UtcNow
        };

        _prompts[promptId] = updated;

        return Task.FromResult<StoredVisualPrompt?>(updated);
    }

    public Task<bool> DeleteAsync(string promptId, CancellationToken ct = default)
    {
        return Task.FromResult(_prompts.TryRemove(promptId, out _));
    }

    public Task<int> DeleteByScriptIdAsync(string scriptId, CancellationToken ct = default)
    {
        var toRemove = _prompts.Values
            .Where(p => p.ScriptId == scriptId)
            .Select(p => p.Id)
            .ToList();

        int removed = 0;
        foreach (var id in toRemove)
        {
            if (_prompts.TryRemove(id, out _))
                removed++;
        }

        return Task.FromResult(removed);
    }
}

