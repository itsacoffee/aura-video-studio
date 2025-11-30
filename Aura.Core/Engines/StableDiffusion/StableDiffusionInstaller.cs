using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Engines.StableDiffusion;

/// <summary>
/// Result of GPU requirements check for Stable Diffusion
/// </summary>
public record GpuCheckResult(
    bool MeetsRequirements,
    bool IsNvidiaGpu,
    int VramGB,
    string Message,
    string? Recommendation = null
);

/// <summary>
/// Result of Stable Diffusion installation operations
/// </summary>
public record StableDiffusionInstallResult(
    bool Success,
    string? InstallPath = null,
    string? ErrorMessage = null,
    string? Phase = null
);

/// <summary>
/// Progress information for Stable Diffusion installation
/// </summary>
public record StableDiffusionInstallProgress(
    string Phase,
    float PercentComplete,
    string Message,
    string? SubPhase = null
);

/// <summary>
/// Information about an available Stable Diffusion model
/// </summary>
public record StableDiffusionModel(
    string Id,
    string Name,
    string Description,
    long SizeBytes,
    string DownloadUrl,
    string? Sha256 = null,
    bool IsDefault = false,
    int MinVramGB = 6
);

/// <summary>
/// Handles installation and management of Stable Diffusion WebUI
/// </summary>
public class StableDiffusionInstaller
{
    private readonly ILogger<StableDiffusionInstaller> _logger;
    private readonly HttpClient _httpClient;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly string _installRoot;
    
    // Default installation paths
    private const string SD_WEBUI_DIR = "stable-diffusion-webui";
    private const string MODELS_DIR = "models/Stable-diffusion";
    private const string SD_WEBUI_REPO = "https://github.com/AUTOMATIC1111/stable-diffusion-webui.git";
    
    // Minimum requirements
    private const int MIN_VRAM_GB = 6;
    private const int RECOMMENDED_VRAM_GB = 8;

    /// <summary>
    /// Available models for download
    /// </summary>
    public static readonly StableDiffusionModel[] AvailableModels = new[]
    {
        new StableDiffusionModel(
            "sd-v1-5",
            "Stable Diffusion 1.5",
            "The original SD 1.5 model - good balance of quality and speed. Works with 6GB+ VRAM.",
            4265380864, // ~4GB
            "https://huggingface.co/runwayml/stable-diffusion-v1-5/resolve/main/v1-5-pruned-emaonly.safetensors",
            "6ce0161689b3853acaa03779ec93eafe75a02f4ced659bee03f50797806fa2fa",
            IsDefault: true,
            MinVramGB: 6
        ),
        new StableDiffusionModel(
            "sd-v2-1",
            "Stable Diffusion 2.1",
            "Improved SD 2.1 model with better quality. Requires 8GB+ VRAM.",
            5214865152, // ~5GB
            "https://huggingface.co/stabilityai/stable-diffusion-2-1/resolve/main/v2-1_768-ema-pruned.safetensors",
            null,
            IsDefault: false,
            MinVramGB: 8
        )
    };

    public StableDiffusionInstaller(
        ILogger<StableDiffusionInstaller> logger,
        HttpClient httpClient,
        IHardwareDetector hardwareDetector,
        string installRoot)
    {
        _logger = logger;
        _httpClient = httpClient;
        _hardwareDetector = hardwareDetector;
        _installRoot = installRoot;
        
        if (!Directory.Exists(_installRoot))
        {
            Directory.CreateDirectory(_installRoot);
        }
    }

    /// <summary>
    /// Get the installation path for SD WebUI
    /// </summary>
    public string GetInstallPath() => Path.Combine(_installRoot, SD_WEBUI_DIR);

    /// <summary>
    /// Check if SD WebUI is installed
    /// </summary>
    public bool IsInstalled()
    {
        var installPath = GetInstallPath();
        var launcherScript = OperatingSystem.IsWindows()
            ? Path.Combine(installPath, "webui.bat")
            : Path.Combine(installPath, "webui.sh");
            
        return Directory.Exists(installPath) && File.Exists(launcherScript);
    }

    /// <summary>
    /// Check GPU requirements for Stable Diffusion
    /// </summary>
    public async Task<GpuCheckResult> CheckGpuRequirementsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Checking GPU requirements for Stable Diffusion");
        
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            var gpuInfo = systemProfile.Gpu;
            
            bool isNvidia = gpuInfo?.Vendor?.ToUpperInvariant() == "NVIDIA";
            int vramGB = gpuInfo?.VramGB ?? 0;
            
            if (!isNvidia)
            {
                return new GpuCheckResult(
                    MeetsRequirements: false,
                    IsNvidiaGpu: false,
                    VramGB: vramGB,
                    Message: "Stable Diffusion requires an NVIDIA GPU with CUDA support.",
                    Recommendation: "Consider using cloud-based image generation providers like DALL-E or Stability AI, or use stock images instead."
                );
            }
            
            if (vramGB < MIN_VRAM_GB)
            {
                return new GpuCheckResult(
                    MeetsRequirements: false,
                    IsNvidiaGpu: true,
                    VramGB: vramGB,
                    Message: $"Your GPU has {vramGB}GB VRAM, but Stable Diffusion requires at least {MIN_VRAM_GB}GB.",
                    Recommendation: $"Your {gpuInfo?.Model ?? "GPU"} may not be able to run SD 1.5. Consider using cloud providers or stock images."
                );
            }
            
            string message = vramGB >= RECOMMENDED_VRAM_GB
                ? $"Your {gpuInfo?.Model ?? "GPU"} with {vramGB}GB VRAM meets all requirements for Stable Diffusion."
                : $"Your {gpuInfo?.Model ?? "GPU"} with {vramGB}GB VRAM meets minimum requirements. 8GB+ recommended for best performance.";
                
            return new GpuCheckResult(
                MeetsRequirements: true,
                IsNvidiaGpu: true,
                VramGB: vramGB,
                Message: message,
                Recommendation: vramGB >= 12 ? "Your GPU can also run SDXL models for higher quality images." : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking GPU requirements");
            return new GpuCheckResult(
                MeetsRequirements: false,
                IsNvidiaGpu: false,
                VramGB: 0,
                Message: "Could not detect GPU. Please ensure your GPU drivers are installed.",
                Recommendation: "If you have an NVIDIA GPU, try updating your drivers and restarting."
            );
        }
    }

    /// <summary>
    /// Install Stable Diffusion WebUI
    /// </summary>
    public async Task<StableDiffusionInstallResult> InstallAsync(
        IProgress<StableDiffusionInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Stable Diffusion WebUI installation");
        
        var installPath = GetInstallPath();
        
        try
        {
            // Phase 1: Check prerequisites (5%)
            progress?.Report(new StableDiffusionInstallProgress(
                "Checking prerequisites", 5, "Verifying Git and Python are available..."));
            
            if (!await CheckPrerequisitesAsync(ct).ConfigureAwait(false))
            {
                return new StableDiffusionInstallResult(
                    Success: false,
                    ErrorMessage: "Prerequisites not met. Git is required for installation.",
                    Phase: "prerequisites"
                );
            }
            
            // Phase 2: Clone repository (5-25%)
            progress?.Report(new StableDiffusionInstallProgress(
                "Cloning repository", 10, "Cloning Stable Diffusion WebUI from GitHub...", "git clone"));
                
            if (!await CloneRepositoryAsync(installPath, progress, ct).ConfigureAwait(false))
            {
                return new StableDiffusionInstallResult(
                    Success: false,
                    ErrorMessage: "Failed to clone Stable Diffusion WebUI repository.",
                    Phase: "clone"
                );
            }
            
            // Phase 3: Configure for API mode (25-30%)
            progress?.Report(new StableDiffusionInstallProgress(
                "Configuring", 25, "Configuring for API-only mode..."));
                
            await ConfigureForApiModeAsync(installPath, ct).ConfigureAwait(false);
            
            // Phase 4: Initial setup will happen on first run
            progress?.Report(new StableDiffusionInstallProgress(
                "Finalizing", 30, "Installation complete. First run will download dependencies."));
            
            _logger.LogInformation("Stable Diffusion WebUI installed successfully at {Path}", installPath);
            
            return new StableDiffusionInstallResult(
                Success: true,
                InstallPath: installPath,
                Phase: "complete"
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Installation cancelled by user");
            
            // Clean up partial installation
            if (Directory.Exists(installPath))
            {
                try { Directory.Delete(installPath, true); } catch { /* Ignore cleanup errors */ }
            }
            
            return new StableDiffusionInstallResult(
                Success: false,
                ErrorMessage: "Installation was cancelled.",
                Phase: "cancelled"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing Stable Diffusion WebUI");
            
            return new StableDiffusionInstallResult(
                Success: false,
                ErrorMessage: $"Installation failed: {ex.Message}",
                Phase: "error"
            );
        }
    }

    /// <summary>
    /// Download a model for Stable Diffusion
    /// </summary>
    public async Task<StableDiffusionInstallResult> DownloadModelAsync(
        string modelId,
        IProgress<StableDiffusionInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        var model = Array.Find(AvailableModels, m => m.Id == modelId);
        if (model == null)
        {
            return new StableDiffusionInstallResult(
                Success: false,
                ErrorMessage: $"Unknown model: {modelId}",
                Phase: "error"
            );
        }
        
        _logger.LogInformation("Downloading model {ModelId}: {ModelName}", modelId, model.Name);
        
        var installPath = GetInstallPath();
        var modelsDir = Path.Combine(installPath, MODELS_DIR);
        var modelFileName = $"{modelId}.safetensors";
        var modelPath = Path.Combine(modelsDir, modelFileName);
        
        try
        {
            // Create models directory
            Directory.CreateDirectory(modelsDir);
            
            progress?.Report(new StableDiffusionInstallProgress(
                "Downloading model", 0, $"Starting download of {model.Name}...", modelId));
            
            // Download with progress
            using var response = await _httpClient.GetAsync(
                model.DownloadUrl, 
                HttpCompletionOption.ResponseHeadersRead, 
                ct).ConfigureAwait(false);
                
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? model.SizeBytes;
            var buffer = new byte[81920]; // 80KB buffer
            long bytesRead = 0;
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            int read;
            while ((read = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                bytesRead += read;
                
                float percent = (float)bytesRead / totalBytes * 100;
                progress?.Report(new StableDiffusionInstallProgress(
                    "Downloading model",
                    percent,
                    $"Downloaded {FormatBytes(bytesRead)} / {FormatBytes(totalBytes)}",
                    modelId
                ));
            }
            
            progress?.Report(new StableDiffusionInstallProgress(
                "Download complete", 100, $"{model.Name} downloaded successfully.", modelId));
            
            _logger.LogInformation("Model {ModelId} downloaded successfully to {Path}", modelId, modelPath);
            
            return new StableDiffusionInstallResult(
                Success: true,
                InstallPath: modelPath,
                Phase: "complete"
            );
        }
        catch (OperationCanceledException)
        {
            // Clean up partial download
            if (File.Exists(modelPath))
            {
                try { File.Delete(modelPath); } catch { /* Ignore */ }
            }
            
            return new StableDiffusionInstallResult(
                Success: false,
                ErrorMessage: "Download was cancelled.",
                Phase: "cancelled"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading model {ModelId}", modelId);
            
            return new StableDiffusionInstallResult(
                Success: false,
                ErrorMessage: $"Download failed: {ex.Message}",
                Phase: "error"
            );
        }
    }

    /// <summary>
    /// Get list of installed models
    /// </summary>
    public string[] GetInstalledModels()
    {
        var modelsDir = Path.Combine(GetInstallPath(), MODELS_DIR);
        
        if (!Directory.Exists(modelsDir))
        {
            return Array.Empty<string>();
        }
        
        return Directory.GetFiles(modelsDir, "*.safetensors")
            .Concat(Directory.GetFiles(modelsDir, "*.ckpt"))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .ToArray()!;
    }

    /// <summary>
    /// Remove the Stable Diffusion installation
    /// </summary>
    public async Task<bool> RemoveAsync(CancellationToken ct = default)
    {
        var installPath = GetInstallPath();
        
        if (!Directory.Exists(installPath))
        {
            return true;
        }
        
        _logger.LogInformation("Removing Stable Diffusion WebUI from {Path}", installPath);
        
        try
        {
            await Task.Run(() => Directory.Delete(installPath, true), ct).ConfigureAwait(false);
            _logger.LogInformation("Stable Diffusion WebUI removed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Stable Diffusion WebUI");
            return false;
        }
    }

    private async Task<bool> CheckPrerequisitesAsync(CancellationToken ct)
    {
        // Check for Git
        try
        {
            var gitProcess = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(gitProcess);
            if (process == null) return false;
            
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            _logger.LogWarning("Git not found in PATH");
            return false;
        }
    }

    private async Task<bool> CloneRepositoryAsync(
        string installPath,
        IProgress<StableDiffusionInstallProgress>? progress,
        CancellationToken ct)
    {
        if (Directory.Exists(installPath))
        {
            _logger.LogInformation("Installation directory already exists, skipping clone");
            return true;
        }
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 \"{SD_WEBUI_REPO}\" \"{installPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        
        // Read output asynchronously
        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);
        
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        
        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        
        if (process.ExitCode != 0)
        {
            _logger.LogError("Git clone failed: {Error}", error);
            return false;
        }
        
        _logger.LogInformation("Repository cloned successfully");
        return true;
    }

    private Task ConfigureForApiModeAsync(string installPath, CancellationToken ct)
    {
        // Create/update webui-user.bat or webui-user.sh with API mode settings
        if (OperatingSystem.IsWindows())
        {
            var configPath = Path.Combine(installPath, "webui-user.bat");
            var config = @"@echo off

set COMMANDLINE_ARGS=--api --listen

call webui.bat
";
            return File.WriteAllTextAsync(configPath, config, ct);
        }
        else
        {
            var configPath = Path.Combine(installPath, "webui-user.sh");
            var config = @"#!/bin/bash

export COMMANDLINE_ARGS=""--api --listen""

./webui.sh ""$@""
";
            return File.WriteAllTextAsync(configPath, config, ct);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
