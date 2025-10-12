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

    public ModelsController(
        ILogger<ModelsController> logger,
        ModelInstaller modelInstaller)
    {
        _logger = logger;
        _modelInstaller = modelInstaller;
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
