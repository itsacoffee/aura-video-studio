using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Providers;

/// <summary>
/// Provider interface for targeted script editing based on critique
/// </summary>
public interface IEditorProvider
{
    /// <summary>
    /// Apply targeted edits to script based on critique
    /// </summary>
    /// <param name="script">Original script</param>
    /// <param name="critique">Critique with suggestions</param>
    /// <param name="brief">Original brief context</param>
    /// <param name="spec">Plan specification</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Edited script result</returns>
    Task<EditResult> EditScriptAsync(
        string script,
        CritiqueResult critique,
        Brief brief,
        PlanSpec spec,
        CancellationToken ct);

    /// <summary>
    /// Apply specific diff/patch to script
    /// </summary>
    /// <param name="script">Original script</param>
    /// <param name="edits">List of edits to apply</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Patched script</returns>
    Task<string> ApplyEditsAsync(
        string script,
        IReadOnlyList<ScriptEdit> edits,
        CancellationToken ct);

    /// <summary>
    /// Validate that edited script conforms to schema requirements
    /// </summary>
    /// <param name="script">Script to validate</param>
    /// <param name="spec">Plan specification with constraints</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<SchemaValidationResult> ValidateSchemaAsync(
        string script,
        PlanSpec spec,
        CancellationToken ct);
}

/// <summary>
/// Result of script editing
/// </summary>
public record EditResult
{
    /// <summary>
    /// Edited script
    /// </summary>
    public string EditedScript { get; init; } = string.Empty;

    /// <summary>
    /// List of edits that were applied
    /// </summary>
    public List<ScriptEdit> AppliedEdits { get; init; } = new();

    /// <summary>
    /// Whether all edits were successfully applied
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Schema validation result
    /// </summary>
    public SchemaValidationResult? ValidationResult { get; init; }

    /// <summary>
    /// Error message if editing failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Individual edit operation
/// </summary>
public record ScriptEdit
{
    /// <summary>
    /// Type of edit (insert, delete, replace, reorder)
    /// </summary>
    public string EditType { get; init; } = string.Empty;

    /// <summary>
    /// Target location (line number, section heading, etc.)
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Original text (for delete/replace)
    /// </summary>
    public string? OriginalText { get; init; }

    /// <summary>
    /// New text (for insert/replace)
    /// </summary>
    public string? NewText { get; init; }

    /// <summary>
    /// Reason for this edit
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Result of schema validation
/// </summary>
public record SchemaValidationResult
{
    /// <summary>
    /// Whether script passes schema validation
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors (if any)
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Validation warnings (if any)
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Whether script meets duration constraints
    /// </summary>
    public bool MeetsDurationConstraints { get; init; }

    /// <summary>
    /// Estimated duration of script
    /// </summary>
    public System.TimeSpan? EstimatedDuration { get; init; }

    /// <summary>
    /// Target duration from spec
    /// </summary>
    public System.TimeSpan? TargetDuration { get; init; }
}
