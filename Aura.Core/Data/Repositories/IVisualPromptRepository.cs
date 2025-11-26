using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;

namespace Aura.Core.Data.Repositories;

/// <summary>
/// Repository for visual prompt storage and retrieval
/// </summary>
public interface IVisualPromptRepository
{
    /// <summary>
    /// Save a visual prompt
    /// </summary>
    Task<StoredVisualPrompt> SaveAsync(StoredVisualPrompt prompt, CancellationToken ct = default);

    /// <summary>
    /// Get visual prompt by ID
    /// </summary>
    Task<StoredVisualPrompt?> GetByIdAsync(string promptId, CancellationToken ct = default);

    /// <summary>
    /// Get all visual prompts for a script
    /// </summary>
    Task<List<StoredVisualPrompt>> GetByScriptIdAsync(string scriptId, CancellationToken ct = default);

    /// <summary>
    /// Get visual prompt for a specific scene
    /// </summary>
    Task<StoredVisualPrompt?> GetBySceneAsync(string scriptId, int sceneNumber, CancellationToken ct = default);

    /// <summary>
    /// Get all visual prompts by correlation ID
    /// </summary>
    Task<List<StoredVisualPrompt>> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);

    /// <summary>
    /// Update a visual prompt
    /// </summary>
    Task<StoredVisualPrompt?> UpdateAsync(string promptId, UpdateVisualPromptRequest update, CancellationToken ct = default);

    /// <summary>
    /// Delete a visual prompt
    /// </summary>
    Task<bool> DeleteAsync(string promptId, CancellationToken ct = default);

    /// <summary>
    /// Delete all prompts for a script
    /// </summary>
    Task<int> DeleteByScriptIdAsync(string scriptId, CancellationToken ct = default);
}

