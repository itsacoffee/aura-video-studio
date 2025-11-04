using Aura.Core.AI.Adapters;
using Aura.Core.Configuration;
using Aura.Core.Downloads;
using Aura.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/models")]
public class ModelsController : ControllerBase
{
    private readonly ILogger<ModelsController> _logger;
    private readonly ModelInstaller _modelInstaller;
    private readonly ModelCatalog? _modelCatalog;
    private readonly ProviderSettings? _providerSettings;

    public ModelsController(
        ILogger<ModelsController> logger,
        ModelInstaller modelInstaller,
        ModelCatalog? modelCatalog = null,
        ProviderSettings? providerSettings = null)
    {
        _logger = logger;
        _modelInstaller = modelInstaller;
        _modelCatalog = modelCatalog;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// List all installed models for an engine
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] string engineId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(engineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var models = await _modelInstaller.ListModelsAsync(engineId, ct);
            
            return Ok(new
            {
                engineId,
                models,
                totalCount = models.Count,
                totalSize = models.Sum(m => m.SizeBytes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list models for engine {EngineId}", engineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Install a model from mirrors
    /// </summary>
    [HttpPost("install")]
    public async Task<IActionResult> Install(
        [FromBody] InstallModelRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            if (request.Model == null)
            {
                return BadRequest(new { error = "Model is required" });
            }

            var progress = new Progress<ModelInstallProgress>(p =>
            {
                _logger.LogInformation("Model install progress: {Phase} {Percent}%", p.Phase, p.PercentComplete);
            });

            var installed = await _modelInstaller.InstallAsync(
                request.Model, 
                request.Destination, 
                progress, 
                ct);

            return Ok(new
            {
                success = true,
                model = installed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install model {ModelId}", request.Model?.Id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add an external folder to index models from
    /// </summary>
    [HttpPost("add-external")]
    public async Task<IActionResult> AddExternal(
        [FromBody] AddExternalFolderRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FolderPath))
            {
                return BadRequest(new { error = "FolderPath is required" });
            }

            if (!Directory.Exists(request.FolderPath))
            {
                return BadRequest(new { error = $"Folder not found: {request.FolderPath}" });
            }

            var count = await _modelInstaller.AddExternalFolderAsync(
                request.Kind, 
                request.FolderPath, 
                request.IsReadOnly, 
                ct);

            return Ok(new
            {
                success = true,
                folderPath = request.FolderPath,
                modelsDiscovered = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add external folder {Path}", request.FolderPath);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a model
    /// </summary>
    [HttpDelete("remove")]
    public async Task<IActionResult> Remove(
        [FromBody] RemoveModelRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModelId) || string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(new { error = "ModelId and FilePath are required" });
            }

            await _modelInstaller.RemoveModelAsync(request.ModelId, request.FilePath, ct);

            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove model {ModelId}", request.ModelId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Verify a model's checksum
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyModelRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(new { error = "FilePath is required" });
            }

            var (isValid, status) = await _modelInstaller.VerifyModelAsync(
                request.FilePath, 
                request.ExpectedSha256, 
                ct);

            return Ok(new
            {
                isValid,
                status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify model {Path}", request.FilePath);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Open the folder containing a model
    /// </summary>
    [HttpPost("open-folder")]
    public IActionResult OpenFolder([FromBody] OpenFolderRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(new { error = "FilePath is required" });
            }

            if (!System.IO.File.Exists(request.FilePath))
            {
                return NotFound(new { error = "File not found" });
            }

            var directory = Path.GetDirectoryName(request.FilePath);
            if (string.IsNullOrEmpty(directory))
            {
                return BadRequest(new { error = "Invalid file path" });
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", $"/select,\"{request.FilePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", directory);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", $"-R \"{request.FilePath}\"");
            }

            return Ok(new { success = true, path = directory });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder for {Path}", request.FilePath);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get external folders for a model kind
    /// </summary>
    [HttpGet("external-folders")]
    public IActionResult GetExternalFolders([FromQuery] string? kind = null)
    {
        try
        {
            ModelKind? modelKind = null;
            if (!string.IsNullOrEmpty(kind) && Enum.TryParse<ModelKind>(kind, true, out var parsed))
            {
                modelKind = parsed;
            }

            var folders = _modelInstaller.GetExternalFolders(modelKind);

            return Ok(new { folders });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get external folders");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove an external folder
    /// </summary>
    [HttpDelete("external-folder")]
    public IActionResult RemoveExternalFolder([FromBody] RemoveExternalFolderRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FolderPath))
            {
                return BadRequest(new { error = "FolderPath is required" });
            }

            _modelInstaller.RemoveExternalFolder(request.Kind, request.FolderPath);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove external folder {Path}", request.FolderPath);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all known LLM models with their capabilities and availability
    /// </summary>
    [HttpGet("llm/list")]
    public IActionResult ListLlmModels([FromQuery] string? provider = null)
    {
        try
        {
            if (_modelCatalog == null)
            {
                _logger.LogWarning("ModelCatalog not configured, returning static registry only");
                
                var staticModels = string.IsNullOrWhiteSpace(provider)
                    ? new[] { "OpenAI", "Anthropic", "Gemini", "Azure", "Ollama" }
                        .SelectMany(p => ModelRegistry.GetModelsForProvider(p))
                        .ToList()
                    : ModelRegistry.GetModelsForProvider(provider).ToList();

                return Ok(new
                {
                    models = staticModels.Select(m => new
                    {
                        provider = m.Provider,
                        modelId = m.ModelId,
                        maxTokens = m.MaxTokens,
                        contextWindow = m.ContextWindow,
                        aliases = m.Aliases ?? Array.Empty<string>(),
                        isDeprecated = m.DeprecationDate.HasValue && m.DeprecationDate.Value <= DateTime.UtcNow,
                        deprecationDate = m.DeprecationDate,
                        replacementModel = m.ReplacementModel,
                        source = "static"
                    }),
                    totalCount = staticModels.Count,
                    catalogLastRefresh = (DateTime?)null,
                    needsRefresh = false
                });
            }

            var models = string.IsNullOrWhiteSpace(provider)
                ? new[] { "OpenAI", "Anthropic", "Gemini", "Azure", "Ollama" }
                    .SelectMany(p => _modelCatalog.GetAllModels(p))
                    .ToList()
                : _modelCatalog.GetAllModels(provider);

            _logger.LogInformation("Listed {Count} LLM models for provider filter: {Provider}, CorrelationId: {CorrelationId}",
                models.Count, provider ?? "all", HttpContext.TraceIdentifier);

            return Ok(new
            {
                models = models.Select(m => new
                {
                    provider = m.Provider,
                    modelId = m.ModelId,
                    maxTokens = m.MaxTokens,
                    contextWindow = m.ContextWindow,
                    aliases = m.Aliases ?? Array.Empty<string>(),
                    isDeprecated = m.DeprecationDate.HasValue && m.DeprecationDate.Value <= DateTime.UtcNow,
                    deprecationDate = m.DeprecationDate,
                    replacementModel = m.ReplacementModel,
                    source = "catalog"
                }),
                totalCount = models.Count,
                catalogLastRefresh = _modelCatalog.GetLastRefreshTime(),
                needsRefresh = _modelCatalog.NeedsRefresh()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list LLM models, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            return StatusCode(500, new { error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Force refresh of the model catalog from all providers
    /// </summary>
    [HttpPost("llm/refresh")]
    public async Task<IActionResult> RefreshLlmModels(CancellationToken ct = default)
    {
        try
        {
            if (_modelCatalog == null)
            {
                _logger.LogWarning("ModelCatalog not configured");
                return BadRequest(new 
                { 
                    error = "Model catalog not configured",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            if (_providerSettings == null)
            {
                _logger.LogWarning("ProviderSettings not configured");
                return BadRequest(new 
                { 
                    error = "Provider settings not configured",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            _logger.LogInformation("Starting model catalog refresh, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);

            var apiKeys = new Dictionary<string, string>();
            
            // Collect API keys from ProviderSettings
            var openAiKey = _providerSettings.GetOpenAiApiKey();
            if (!string.IsNullOrWhiteSpace(openAiKey))
                apiKeys["openai"] = openAiKey;

            var geminiKey = _providerSettings.GetGeminiApiKey();
            if (!string.IsNullOrWhiteSpace(geminiKey))
                apiKeys["gemini"] = geminiKey;

            // Note: Anthropic and other keys would need to be added to ProviderSettings
            // For now, we'll work with what's available

            var ollamaUrl = _providerSettings.GetOllamaUrl();

            var success = await _modelCatalog.RefreshCatalogAsync(apiKeys, ollamaUrl, ct);

            if (success)
            {
                _logger.LogInformation("Model catalog refresh completed successfully, CorrelationId: {CorrelationId}", 
                    HttpContext.TraceIdentifier);
                
                return Ok(new 
                { 
                    success = true,
                    message = "Model catalog refreshed successfully",
                    timestamp = DateTime.UtcNow,
                    correlationId = HttpContext.TraceIdentifier
                });
            }
            else
            {
                _logger.LogWarning("Model catalog refresh completed with errors, CorrelationId: {CorrelationId}", 
                    HttpContext.TraceIdentifier);
                
                return Ok(new 
                { 
                    success = false,
                    message = "Model catalog refresh completed with errors. Using static registry.",
                    timestamp = DateTime.UtcNow,
                    correlationId = HttpContext.TraceIdentifier
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh model catalog, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            return StatusCode(500, new { error = ex.Message, correlationId = HttpContext.TraceIdentifier });
        }
    }
}

// Request models
public class InstallModelRequest
{
    public ModelDefinition? Model { get; set; }
    public string? Destination { get; set; }
}

public class AddExternalFolderRequest
{
    public ModelKind Kind { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = true;
}

public class RemoveModelRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public class VerifyModelRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? ExpectedSha256 { get; set; }
}

public class OpenFolderRequest
{
    public string FilePath { get; set; } = string.Empty;
}

public class RemoveExternalFolderRequest
{
    public ModelKind Kind { get; set; }
    public string FolderPath { get; set; } = string.Empty;
}
