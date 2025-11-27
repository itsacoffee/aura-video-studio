using System.Text.Json;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Services;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for Ollama process control and status
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OllamaController : ControllerBase
{
    private readonly OllamaService _ollamaService;
    private readonly ProviderSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly OllamaDetectionService? _detectionService;
    private readonly IGpuDetectionService? _gpuDetectionService;

    public OllamaController(
        OllamaService ollamaService,
        ProviderSettings settings,
        IHttpClientFactory httpClientFactory,
        OllamaDetectionService? detectionService = null,
        IGpuDetectionService? gpuDetectionService = null)
    {
        _ollamaService = ollamaService;
        _settings = settings;
        _httpClient = httpClientFactory.CreateClient();
        _detectionService = detectionService;
        _gpuDetectionService = gpuDetectionService;
    }

    /// <summary>
    /// Get Ollama service status (running, PID, model info)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Status information</returns>
    [HttpGet("status")]
    public async Task<ActionResult<OllamaStatusResponse>> GetStatus(CancellationToken ct = default)
    {
        try
        {
            var baseUrl = _settings.GetOllamaUrl();
            var status = await _ollamaService.GetStatusAsync(baseUrl, ct).ConfigureAwait(false);

            // Check if Ollama is installed
            var executablePath = _settings.GetOllamaExecutablePath();
            var installed = !string.IsNullOrWhiteSpace(executablePath) ||
                           !string.IsNullOrWhiteSpace(OllamaService.FindOllamaExecutable());
            var installPath = executablePath ?? OllamaService.FindOllamaExecutable();

            var response = new OllamaStatusResponse(
                Running: status.Running,
                Pid: status.Pid,
                ManagedByApp: status.ManagedByApp,
                Model: status.Model,
                Error: status.Error
            );

            // Add installation info to response
            var enhancedResponse = new
            {
                running = response.Running,
                pid = response.Pid,
                managedByApp = response.ManagedByApp,
                model = response.Model,
                error = response.Error,
                installed,
                installPath,
                version = status.Running ? "Running" : null
            };

            Log.Information("Ollama status check: Running={Running}, PID={Pid}, ManagedByApp={Managed}, Installed={Installed}, CorrelationId={CorrelationId}",
                status.Running, status.Pid, status.ManagedByApp, installed, HttpContext.TraceIdentifier);

            return Ok(enhancedResponse);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking Ollama status, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error checking Ollama status",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// Start Ollama server process (Windows only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Start operation result</returns>
    [HttpPost("start")]
    public async Task<ActionResult<OllamaStartResponse>> Start(CancellationToken ct = default)
    {
        try
        {
            var executablePath = _settings.GetOllamaExecutablePath();

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                var autoDetectPath = OllamaService.FindOllamaExecutable();

                if (string.IsNullOrWhiteSpace(autoDetectPath))
                {
                    Log.Warning("Ollama executable path not configured and auto-detection failed, CorrelationId={CorrelationId}",
                        HttpContext.TraceIdentifier);

                    return BadRequest(new ProblemDetails
                    {
                        Title = "Ollama path not configured",
                        Detail = "Please configure the Ollama executable path in Settings → Providers → Ollama",
                        Status = 400,
                        Instance = HttpContext.TraceIdentifier
                    });
                }

                executablePath = autoDetectPath;
            }

            var baseUrl = _settings.GetOllamaUrl();

            Log.Information("Starting Ollama from {Path}, CorrelationId={CorrelationId}",
                executablePath, HttpContext.TraceIdentifier);

            var result = await _ollamaService.StartAsync(executablePath, baseUrl, ct).ConfigureAwait(false);

            var response = new OllamaStartResponse(
                Success: result.Success,
                Message: result.Message,
                Pid: result.Pid
            );

            if (result.Success)
            {
                Log.Information("Ollama started successfully (PID: {Pid}), CorrelationId={CorrelationId}",
                    result.Pid, HttpContext.TraceIdentifier);
                return Ok(response);
            }
            else
            {
                Log.Warning("Failed to start Ollama: {Message}, CorrelationId={CorrelationId}",
                    result.Message, HttpContext.TraceIdentifier);
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting Ollama, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error starting Ollama",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// Stop Ollama server process (only if started by this app)
    /// </summary>
    /// <returns>Stop operation result</returns>
    [HttpPost("stop")]
    public async Task<ActionResult<OllamaStopResponse>> Stop()
    {
        try
        {
            Log.Information("Stopping Ollama, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);

            var result = await _ollamaService.StopAsync().ConfigureAwait(false);

            var response = new OllamaStopResponse(
                Success: result.Success,
                Message: result.Message
            );

            if (result.Success)
            {
                Log.Information("Ollama stopped successfully, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
                return Ok(response);
            }
            else
            {
                Log.Warning("Failed to stop Ollama: {Message}, CorrelationId={CorrelationId}",
                    result.Message, HttpContext.TraceIdentifier);
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error stopping Ollama, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error stopping Ollama",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// Get recent Ollama log entries
    /// </summary>
    /// <param name="maxLines">Maximum number of log lines to return (default: 200)</param>
    /// <returns>Log entries</returns>
    [HttpGet("logs")]
    public async Task<ActionResult<OllamaLogsResponse>> GetLogs([FromQuery] int maxLines = 200)
    {
        try
        {
            var logs = await _ollamaService.GetLogsAsync(maxLines).ConfigureAwait(false);

            var response = new OllamaLogsResponse(
                Logs: logs,
                TotalLines: logs.Length
            );

            Log.Information("Retrieved {Count} Ollama log lines, CorrelationId={CorrelationId}",
                logs.Length, HttpContext.TraceIdentifier);

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving Ollama logs, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error retrieving logs",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// List available Ollama models
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of installed models</returns>
    [HttpGet("models")]
    public async Task<ActionResult<OllamaModelsListResponse>> GetModels(CancellationToken ct = default)
    {
        try
        {
            var baseUrl = _settings.GetOllamaUrl();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync($"{baseUrl}/api/tags", cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Ollama models endpoint returned {StatusCode}, CorrelationId={CorrelationId}",
                    response.StatusCode, HttpContext.TraceIdentifier);

                return Problem(
                    title: "Failed to retrieve models",
                    detail: $"Ollama API returned {response.StatusCode}",
                    statusCode: (int)response.StatusCode,
                    instance: HttpContext.TraceIdentifier
                );
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(json);

            var models = new List<OllamaModelDto>();

            if (doc.RootElement.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    var name = model.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                    var size = model.TryGetProperty("size", out var sizeEl) ? FormatSize(sizeEl.GetInt64()) : null;
                    var modifiedAt = model.TryGetProperty("modified_at", out var modEl) ? modEl.GetString() : null;

                    if (!string.IsNullOrEmpty(name))
                    {
                        models.Add(new OllamaModelDto(name, size, modifiedAt));
                    }
                }
            }

            Log.Information("Retrieved {Count} Ollama models, CorrelationId={CorrelationId}",
                models.Count, HttpContext.TraceIdentifier);

            return Ok(new OllamaModelsListResponse(models, models.Count));
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Ollama models request timed out, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Request timeout",
                detail: "Failed to retrieve models from Ollama (timeout)",
                statusCode: 504,
                instance: HttpContext.TraceIdentifier
            );
        }
        catch (HttpRequestException ex)
        {
            Log.Warning(ex, "Cannot connect to Ollama for models list, CorrelationId={CorrelationId}",
                HttpContext.TraceIdentifier);

            return Problem(
                title: "Cannot connect to Ollama",
                detail: "Please ensure Ollama is running",
                statusCode: 503,
                instance: HttpContext.TraceIdentifier
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving Ollama models, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error retrieving models",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// Get detailed information about a specific model
    /// </summary>
    [HttpGet("models/{modelName}/info")]
    public async Task<IActionResult> GetModelInfo(string modelName, CancellationToken ct)
    {
        if (_detectionService == null)
        {
            return Problem("Ollama detection service not available", statusCode: 503);
        }

        try
        {
            var info = await _detectionService.GetModelInfoAsync(modelName, ct).ConfigureAwait(false);

            if (info == null)
            {
                return NotFound(new { message = $"Model '{modelName}' not found or info unavailable" });
            }

            return Ok(new OllamaModelInfoDto
            {
                Name = info.Name,
                Parameters = info.Parameters,
                Modelfile = info.Modelfile,
                ContextWindow = info.ContextWindow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting model info for {ModelName}, CorrelationId={CorrelationId}",
                modelName, HttpContext.TraceIdentifier);
            return Problem($"Error getting model info for {modelName}", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if a specific model is available locally
    /// </summary>
    [HttpGet("models/{modelName}/available")]
    public async Task<IActionResult> CheckModelAvailability(string modelName, CancellationToken ct)
    {
        if (_detectionService == null)
        {
            return Problem("Ollama detection service not available", statusCode: 503);
        }

        try
        {
            var isAvailable = await _detectionService.IsModelAvailableAsync(modelName, ct).ConfigureAwait(false);

            return Ok(new { modelName, isAvailable });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking model availability for {ModelName}, CorrelationId={CorrelationId}",
                modelName, HttpContext.TraceIdentifier);
            return Problem($"Error checking model availability for {modelName}", statusCode: 500);
        }
    }

    /// <summary>
    /// Pull a model from the Ollama library (initiate async download)
    /// </summary>
    [HttpPost("models/{modelName}/pull")]
    public async Task<IActionResult> PullModel(string modelName, CancellationToken ct)
    {
        if (_detectionService == null)
        {
            return Problem("Ollama detection service not available", statusCode: 503);
        }

        try
        {
            Log.Information("Initiating pull for Ollama model: {ModelName}, CorrelationId={CorrelationId}",
                modelName, HttpContext.TraceIdentifier);

            var pullProgress = new Progress<OllamaPullProgress>(p =>
            {
                Log.Debug("Pull progress for {ModelName}: {Status} - {Percent:F1}%",
                    modelName, p.Status, p.PercentComplete);
            });

            var success = await _detectionService.PullModelAsync(modelName, pullProgress, ct).ConfigureAwait(false);

            if (success)
            {
                return Ok(new {
                    message = $"Model '{modelName}' pulled successfully",
                    modelName,
                    success = true
                });
            }

            return BadRequest(new {
                message = $"Failed to pull model '{modelName}'",
                modelName,
                success = false
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error pulling Ollama model: {ModelName}, CorrelationId={CorrelationId}",
                modelName, HttpContext.TraceIdentifier);
            return Problem($"Error pulling model {modelName}", statusCode: 500);
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Get GPU status and configuration for Ollama
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GPU configuration status</returns>
    [HttpGet("gpu/status")]
    public async Task<ActionResult<OllamaGpuStatusResponse>> GetGpuStatus(CancellationToken ct = default)
    {
        try
        {
            var gpuEnabled = _settings.GetOllamaGpuEnabled();
            var numGpu = _settings.GetOllamaNumGpu();
            var numCtx = _settings.GetOllamaNumCtx();
            var autoDetect = _settings.GetOllamaGpuAutoDetect();

            GpuDetectionResult? detectionResult = null;
            if (_gpuDetectionService != null)
            {
                detectionResult = await _gpuDetectionService.DetectGpuAsync(ct).ConfigureAwait(false);
            }

            var response = new OllamaGpuStatusResponse(
                GpuEnabled: gpuEnabled,
                NumGpu: numGpu,
                NumCtx: numCtx,
                AutoDetect: autoDetect,
                HasGpu: detectionResult?.HasGpu ?? false,
                GpuName: detectionResult?.GpuName,
                VramMB: detectionResult?.VramMB ?? 0,
                VramFormatted: detectionResult?.VramFormatted ?? "N/A",
                RecommendedNumGpu: detectionResult?.RecommendedNumGpu ?? 0,
                RecommendedNumCtx: detectionResult?.RecommendedNumCtx ?? 2048,
                DetectionMethod: detectionResult?.DetectionMethod ?? "NotAvailable"
            );

            Log.Information("GPU status check: Enabled={GpuEnabled}, NumGpu={NumGpu}, HasGpu={HasGpu}, GPU={GpuName}, CorrelationId={CorrelationId}",
                gpuEnabled, numGpu, detectionResult?.HasGpu, detectionResult?.GpuName, HttpContext.TraceIdentifier);

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking GPU status, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error checking GPU status",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// Update GPU configuration for Ollama
    /// </summary>
    /// <param name="request">GPU configuration request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated GPU configuration</returns>
    [HttpPut("gpu/config")]
    public async Task<ActionResult<OllamaGpuStatusResponse>> UpdateGpuConfig(
        [FromBody] OllamaGpuConfigRequest request,
        CancellationToken ct = default)
    {
        try
        {
            Log.Information("Updating GPU config: Enabled={Enabled}, NumGpu={NumGpu}, NumCtx={NumCtx}, AutoDetect={AutoDetect}, CorrelationId={CorrelationId}",
                request.GpuEnabled, request.NumGpu, request.NumCtx, request.AutoDetect, HttpContext.TraceIdentifier);

            _settings.SetOllamaGpuConfig(
                enabled: request.GpuEnabled,
                numGpu: request.NumGpu,
                numCtx: request.NumCtx,
                autoDetect: request.AutoDetect
            );

            // Return updated status
            return await GetGpuStatus(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating GPU config, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error updating GPU configuration",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }

    /// <summary>
    /// Auto-detect and apply optimal GPU settings
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detected and applied GPU configuration</returns>
    [HttpPost("gpu/auto-detect")]
    public async Task<ActionResult<OllamaGpuStatusResponse>> AutoDetectGpu(CancellationToken ct = default)
    {
        try
        {
            if (_gpuDetectionService == null)
            {
                return Problem(
                    title: "GPU detection service not available",
                    detail: "The GPU detection service is not configured",
                    statusCode: 503,
                    instance: HttpContext.TraceIdentifier
                );
            }

            // Force re-detection
            var detectionResult = await _gpuDetectionService.ForceDetectGpuAsync(ct).ConfigureAwait(false);

            Log.Information("GPU auto-detection result: HasGpu={HasGpu}, GPU={GpuName}, VRAM={VramMB}MB, RecommendedNumGpu={NumGpu}, CorrelationId={CorrelationId}",
                detectionResult.HasGpu, detectionResult.GpuName, detectionResult.VramMB,
                detectionResult.RecommendedNumGpu, HttpContext.TraceIdentifier);

            // Apply detected settings
            _settings.SetOllamaGpuConfig(
                enabled: detectionResult.HasGpu,
                numGpu: detectionResult.RecommendedNumGpu,
                numCtx: detectionResult.RecommendedNumCtx,
                autoDetect: true
            );

            var response = new OllamaGpuStatusResponse(
                GpuEnabled: detectionResult.HasGpu,
                NumGpu: detectionResult.RecommendedNumGpu,
                NumCtx: detectionResult.RecommendedNumCtx,
                AutoDetect: true,
                HasGpu: detectionResult.HasGpu,
                GpuName: detectionResult.GpuName,
                VramMB: detectionResult.VramMB,
                VramFormatted: detectionResult.VramFormatted,
                RecommendedNumGpu: detectionResult.RecommendedNumGpu,
                RecommendedNumCtx: detectionResult.RecommendedNumCtx,
                DetectionMethod: detectionResult.DetectionMethod
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error auto-detecting GPU, CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Problem(
                title: "Error auto-detecting GPU",
                detail: ex.Message,
                statusCode: 500,
                instance: HttpContext.TraceIdentifier
            );
        }
    }
}
