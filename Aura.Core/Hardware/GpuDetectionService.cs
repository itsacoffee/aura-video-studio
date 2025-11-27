using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Hardware;

/// <summary>
/// Service for detecting GPU capabilities and configuring optimal Ollama GPU settings.
/// Supports NVIDIA CUDA GPUs for hardware-accelerated LLM inference.
/// </summary>
public class GpuDetectionService : IGpuDetectionService
{
    private readonly ILogger<GpuDetectionService> _logger;

    // Cached GPU detection result
    private GpuDetectionResult? _cachedResult;
    private readonly object _cacheLock = new object();
    private DateTime _lastDetection = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public GpuDetectionService(ILogger<GpuDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects GPU capabilities and returns optimal configuration for Ollama.
    /// Results are cached for 5 minutes.
    /// </summary>
    public async Task<GpuDetectionResult> DetectGpuAsync(CancellationToken ct = default)
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (_cachedResult != null && DateTime.UtcNow - _lastDetection < _cacheDuration)
            {
                _logger.LogDebug("Using cached GPU detection result: HasGpu={HasGpu}, NumGpu={NumGpu}",
                    _cachedResult.HasGpu, _cachedResult.RecommendedNumGpu);
                return _cachedResult;
            }
        }

        _logger.LogInformation("Starting GPU detection for Ollama configuration");

        var result = new GpuDetectionResult();

        try
        {
            // Try nvidia-smi first for NVIDIA GPUs
            var nvidiaSmiResult = await DetectNvidiaGpuAsync(ct).ConfigureAwait(false);
            if (nvidiaSmiResult.HasGpu)
            {
                result = nvidiaSmiResult;
                _logger.LogInformation(
                    "Detected NVIDIA GPU: {GpuName}, VRAM: {VramMB}MB, Recommended numGpu={NumGpu}, numCtx={NumCtx}",
                    result.GpuName, result.VramMB, result.RecommendedNumGpu, result.RecommendedNumCtx);
            }
            else
            {
                _logger.LogInformation("No NVIDIA GPU detected, Ollama will use CPU mode (numGpu=0)");
                result = new GpuDetectionResult
                {
                    HasGpu = false,
                    GpuName = null,
                    VramMB = 0,
                    RecommendedNumGpu = 0, // CPU mode
                    RecommendedNumCtx = 2048, // Smaller context for CPU
                    DetectionMethod = "FallbackCpu",
                    ErrorMessage = null
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GPU detection failed, defaulting to CPU mode");
            result = new GpuDetectionResult
            {
                HasGpu = false,
                GpuName = null,
                VramMB = 0,
                RecommendedNumGpu = 0, // CPU fallback
                RecommendedNumCtx = 2048,
                DetectionMethod = "Error",
                ErrorMessage = ex.Message
            };
        }

        // Cache the result
        lock (_cacheLock)
        {
            _cachedResult = result;
            _lastDetection = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Detects NVIDIA GPU using nvidia-smi command
    /// </summary>
    private async Task<GpuDetectionResult> DetectNvidiaGpuAsync(CancellationToken ct)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogDebug("nvidia-smi detection only supported on Windows and Linux");
            return new GpuDetectionResult { HasGpu = false, DetectionMethod = "UnsupportedOS" };
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name,memory.total --format=csv,noheader,nounits",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("nvidia-smi returned exit code {ExitCode} or empty output", process.ExitCode);
                return new GpuDetectionResult { HasGpu = false, DetectionMethod = "NvidiaSmiNoGpu" };
            }

            // Parse output: "GeForce RTX 3080, 10240"
            var parts = output.Trim().Split(',');
            if (parts.Length < 2)
            {
                _logger.LogWarning("Unexpected nvidia-smi output format: {Output}", output);
                return new GpuDetectionResult { HasGpu = false, DetectionMethod = "NvidiaSmiParseError" };
            }

            var gpuName = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out var vramMB))
            {
                _logger.LogWarning("Could not parse VRAM from nvidia-smi output: {Output}", output);
                vramMB = 0;
            }

            // Calculate optimal settings based on VRAM
            var (numGpu, numCtx) = CalculateOptimalSettings(vramMB);

            return new GpuDetectionResult
            {
                HasGpu = true,
                GpuName = gpuName,
                VramMB = vramMB,
                RecommendedNumGpu = numGpu,
                RecommendedNumCtx = numCtx,
                DetectionMethod = "NvidiaSmi",
                ErrorMessage = null
            };
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // nvidia-smi not found in PATH
            _logger.LogDebug(ex, "nvidia-smi not found in PATH");
            return new GpuDetectionResult { HasGpu = false, DetectionMethod = "NvidiaSmiNotFound" };
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("nvidia-smi detection timed out");
            return new GpuDetectionResult { HasGpu = false, DetectionMethod = "NvidiaSmiTimeout" };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "nvidia-smi detection failed");
            return new GpuDetectionResult { HasGpu = false, DetectionMethod = "NvidiaSmiError", ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Calculates optimal numGpu and numCtx settings based on available VRAM.
    /// </summary>
    private (int numGpu, int numCtx) CalculateOptimalSettings(int vramMB)
    {
        // VRAM thresholds for optimal settings
        // Higher VRAM = more GPU layers and larger context window

        if (vramMB >= 24000) // 24GB+ (RTX 4090, A100)
        {
            return (-1, 8192); // All layers on GPU, large context
        }
        else if (vramMB >= 12000) // 12GB+ (RTX 4070, RTX 3060 12GB)
        {
            return (-1, 6144); // All layers on GPU, medium-large context
        }
        else if (vramMB >= 8000) // 8GB+ (RTX 3070, RTX 2080)
        {
            return (-1, 4096); // All layers on GPU, standard context
        }
        else if (vramMB >= 6000) // 6GB+ (GTX 1660, RTX 3060 Mobile)
        {
            return (-1, 3072); // All layers on GPU, reduced context
        }
        else if (vramMB >= 4000) // 4GB+ (GTX 1650)
        {
            return (-1, 2048); // All layers on GPU, minimal context
        }
        else if (vramMB > 0) // Less than 4GB
        {
            _logger.LogWarning(
                "Low VRAM detected ({VramMB}MB). Using partial GPU offload for better performance.",
                vramMB);
            return (-1, 1024); // Try GPU but with very small context
        }
        else
        {
            return (0, 2048); // CPU mode
        }
    }

    /// <summary>
    /// Forces a re-detection of GPU capabilities, bypassing the cache.
    /// </summary>
    public async Task<GpuDetectionResult> ForceDetectGpuAsync(CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            _cachedResult = null;
            _lastDetection = DateTime.MinValue;
        }

        return await DetectGpuAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears the cached GPU detection result.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedResult = null;
            _lastDetection = DateTime.MinValue;
        }
        _logger.LogDebug("GPU detection cache cleared");
    }
}

/// <summary>
/// Interface for GPU detection service
/// </summary>
public interface IGpuDetectionService
{
    /// <summary>
    /// Detects GPU capabilities and returns optimal configuration
    /// </summary>
    Task<GpuDetectionResult> DetectGpuAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces re-detection of GPU capabilities
    /// </summary>
    Task<GpuDetectionResult> ForceDetectGpuAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the cached detection result
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Result of GPU detection including recommended Ollama settings
/// </summary>
public class GpuDetectionResult
{
    /// <summary>
    /// Whether a compatible GPU was detected
    /// </summary>
    public bool HasGpu { get; init; }

    /// <summary>
    /// Name of the detected GPU (e.g., "GeForce RTX 3080")
    /// </summary>
    public string? GpuName { get; init; }

    /// <summary>
    /// Available VRAM in megabytes
    /// </summary>
    public int VramMB { get; init; }

    /// <summary>
    /// Recommended num_gpu setting for Ollama.
    /// -1 = use all GPUs, 0 = CPU only
    /// </summary>
    public int RecommendedNumGpu { get; init; }

    /// <summary>
    /// Recommended num_ctx (context window size) for Ollama
    /// </summary>
    public int RecommendedNumCtx { get; init; }

    /// <summary>
    /// Method used for detection (e.g., "NvidiaSmi", "FallbackCpu")
    /// </summary>
    public string DetectionMethod { get; init; } = "Unknown";

    /// <summary>
    /// Error message if detection failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Formatted VRAM string (e.g., "8 GB")
    /// </summary>
    public string VramFormatted => VramMB >= 1024
        ? $"{VramMB / 1024.0:F1} GB"
        : $"{VramMB} MB";
}
