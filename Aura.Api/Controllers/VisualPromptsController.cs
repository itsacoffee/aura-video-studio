using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data.Repositories;
using Aura.Core.Models.Visual;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing visual prompts generated during script creation
/// </summary>
[ApiController]
[Route("api/visual-prompts")]
public class VisualPromptsController : ControllerBase
{
    private readonly IVisualPromptRepository _repository;
    private readonly ILogger<VisualPromptsController> _logger;

    public VisualPromptsController(
        IVisualPromptRepository repository,
        ILogger<VisualPromptsController> logger)
    {
        _repository = repository ?? throw new System.ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all visual prompts for a script
    /// </summary>
    [HttpGet("script/{scriptId}")]
    public async Task<ActionResult<List<StoredVisualPrompt>>> GetByScript(
        string scriptId,
        CancellationToken ct)
    {
        _logger.LogInformation("Getting visual prompts for script: {ScriptId}", scriptId);

        var prompts = await _repository.GetByScriptIdAsync(scriptId, ct);

        if (prompts.Count == 0)
        {
            _logger.LogInformation("No visual prompts found for script: {ScriptId}", scriptId);
            return NotFound(new { message = $"No visual prompts found for script {scriptId}" });
        }

        return Ok(prompts);
    }

    /// <summary>
    /// Get visual prompts by correlation ID
    /// </summary>
    [HttpGet("correlation/{correlationId}")]
    public async Task<ActionResult<List<StoredVisualPrompt>>> GetByCorrelation(
        string correlationId,
        CancellationToken ct)
    {
        _logger.LogInformation("Getting visual prompts for correlation: {CorrelationId}", correlationId);

        var prompts = await _repository.GetByCorrelationIdAsync(correlationId, ct);

        if (prompts.Count == 0)
        {
            return NotFound(new { message = $"No visual prompts found for correlation {correlationId}" });
        }

        return Ok(prompts);
    }

    /// <summary>
    /// Get a specific visual prompt by ID
    /// </summary>
    [HttpGet("{promptId}")]
    public async Task<ActionResult<StoredVisualPrompt>> GetById(
        string promptId,
        CancellationToken ct)
    {
        _logger.LogInformation("Getting visual prompt: {PromptId}", promptId);

        var prompt = await _repository.GetByIdAsync(promptId, ct);

        if (prompt == null)
        {
            return NotFound(new { message = $"Visual prompt {promptId} not found" });
        }

        return Ok(prompt);
    }

    /// <summary>
    /// Get visual prompt for a specific scene
    /// </summary>
    [HttpGet("script/{scriptId}/scene/{sceneNumber}")]
    public async Task<ActionResult<StoredVisualPrompt>> GetByScene(
        string scriptId,
        int sceneNumber,
        CancellationToken ct)
    {
        _logger.LogInformation("Getting visual prompt for script {ScriptId}, scene {SceneNumber}", scriptId, sceneNumber);

        var prompt = await _repository.GetBySceneAsync(scriptId, sceneNumber, ct);

        if (prompt == null)
        {
            return NotFound(new { message = $"Visual prompt not found for script {scriptId}, scene {sceneNumber}" });
        }

        return Ok(prompt);
    }

    /// <summary>
    /// Create a new visual prompt
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StoredVisualPrompt>> Create(
        [FromBody] CreateVisualPromptRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Creating visual prompt for script {ScriptId}, scene {SceneNumber}",
            request.ScriptId,
            request.SceneNumber);

        var prompt = new StoredVisualPrompt
        {
            ScriptId = request.ScriptId,
            CorrelationId = request.CorrelationId,
            SceneNumber = request.SceneNumber,
            SceneHeading = request.SceneHeading,
            DetailedPrompt = request.DetailedPrompt,
            CameraAngle = request.CameraAngle,
            Lighting = request.Lighting,
            NegativePrompts = request.NegativePrompts ?? new List<string>(),
            StyleKeywords = request.StyleKeywords
        };

        var saved = await _repository.SaveAsync(prompt, ct);

        return CreatedAtAction(nameof(GetById), new { promptId = saved.Id }, saved);
    }

    /// <summary>
    /// Update a visual prompt
    /// </summary>
    [HttpPut("{promptId}")]
    public async Task<ActionResult<StoredVisualPrompt>> Update(
        string promptId,
        [FromBody] UpdateVisualPromptRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Updating visual prompt: {PromptId}", promptId);

        var updated = await _repository.UpdateAsync(promptId, request, ct);

        if (updated == null)
        {
            return NotFound(new { message = $"Visual prompt {promptId} not found" });
        }

        return Ok(updated);
    }

    /// <summary>
    /// Delete a visual prompt
    /// </summary>
    [HttpDelete("{promptId}")]
    public async Task<IActionResult> Delete(string promptId, CancellationToken ct)
    {
        _logger.LogInformation("Deleting visual prompt: {PromptId}", promptId);

        var deleted = await _repository.DeleteAsync(promptId, ct);

        if (!deleted)
        {
            return NotFound(new { message = $"Visual prompt {promptId} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete all visual prompts for a script
    /// </summary>
    [HttpDelete("script/{scriptId}")]
    public async Task<ActionResult<int>> DeleteByScript(string scriptId, CancellationToken ct)
    {
        _logger.LogInformation("Deleting all visual prompts for script: {ScriptId}", scriptId);

        var deletedCount = await _repository.DeleteByScriptIdAsync(scriptId, ct);

        return Ok(new { deletedCount, message = $"Deleted {deletedCount} visual prompt(s) for script {scriptId}" });
    }
}

