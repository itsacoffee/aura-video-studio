using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Templates;

namespace Aura.Core.Services.Templates;

/// <summary>
/// Service for managing and applying video structure templates.
/// </summary>
public interface IVideoTemplateService
{
    /// <summary>
    /// Gets all available video templates.
    /// </summary>
    IReadOnlyList<VideoTemplate> GetAllTemplates();

    /// <summary>
    /// Gets a template by its ID.
    /// </summary>
    VideoTemplate? GetTemplateById(string id);

    /// <summary>
    /// Gets templates filtered by category.
    /// </summary>
    IReadOnlyList<VideoTemplate> GetTemplatesByCategory(string category);

    /// <summary>
    /// Searches templates by query string.
    /// </summary>
    IReadOnlyList<VideoTemplate> SearchTemplates(string query);

    /// <summary>
    /// Applies a template with the provided variable values to create a TemplatedBrief.
    /// </summary>
    Task<TemplatedBrief> ApplyTemplateAsync(
        string templateId,
        IDictionary<string, string> variableValues,
        string? language = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a script preview from a template without creating a full brief.
    /// </summary>
    Task<ScriptPreviewResponse> PreviewScriptAsync(
        string templateId,
        IDictionary<string, string> variableValues,
        CancellationToken ct = default);

    /// <summary>
    /// Validates variable values against a template's requirements.
    /// </summary>
    (bool IsValid, IReadOnlyList<string> Errors) ValidateVariables(
        string templateId,
        IDictionary<string, string> variableValues);
}
