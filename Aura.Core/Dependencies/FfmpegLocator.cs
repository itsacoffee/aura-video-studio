using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

/// <summary>
/// FFmpeg validation result with detailed information
/// </summary>
public class FfmpegValidationResult
{
    public bool Found { get; set; }
    public string? FfmpegPath { get; set; }
    public string? VersionString { get; set; }
    public string? ValidationOutput { get; set; }
    public string? Reason { get; set; }
    public List<string> AttemptedPaths { get; set; } = new();
}

/// <summary>
/// Centralized FFmpeg detection and path resolution
/// </summary>
public class FfmpegLocator
{
    private readonly ILogger<FfmpegLocator> _logger;
    private readonly string _toolsDirectory;
    private readonly string _dependenciesDirectory;

    public FfmpegLocator(ILogger<FfmpegLocator> logger, string? toolsDirectory = null)
    {
        _logger = logger;
        
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _toolsDirectory = toolsDirectory ?? Path.Combine(localAppData, "Aura", "Tools");
        
        // If custom tools directory provided, derive dependencies from it
        if (toolsDirectory != null)
        {
            var parentDir = Path.GetDirectoryName(_toolsDirectory);
            _dependenciesDirectory = parentDir != null 
                ? Path.Combine(parentDir, "dependencies") 
                : Path.Combine(localAppData, "Aura", "dependencies");
        }
        else
        {
            _dependenciesDirectory = Path.Combine(localAppData, "Aura", "dependencies");
        }
    }

    /// <summary>
    /// Check all candidate locations for FFmpeg and return first valid one
    /// </summary>
    public async Task<FfmpegValidationResult> CheckAllCandidatesAsync(
        string? configuredPath = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Checking all FFmpeg candidate locations");
        
        var result = new FfmpegValidationResult();
        var candidates = GetCandidatePaths(configuredPath);

        foreach (var candidate in candidates)
        {
            result.AttemptedPaths.Add(candidate);
            
            if (!File.Exists(candidate))
            {
                _logger.LogDebug("FFmpeg not found at: {Path}", candidate);
                continue;
            }

            var validation = await ValidateFfmpegBinaryAsync(candidate, ct);
            if (validation.success)
            {
                _logger.LogInformation("FFmpeg found and validated at: {Path}", candidate);
                result.Found = true;
                result.FfmpegPath = candidate;
                result.VersionString = ExtractVersionString(validation.output);
                result.ValidationOutput = validation.output;
                result.Reason = "Valid FFmpeg binary found";
                return result;
            }
            else
            {
                _logger.LogDebug("FFmpeg validation failed at {Path}: {Error}", candidate, validation.error);
            }
        }

        // Also check PATH environment variable
        var pathResult = await CheckPathEnvironmentAsync(ct);
        if (pathResult.Found)
        {
            result.AttemptedPaths.Add("PATH");
            return pathResult;
        }

        result.Found = false;
        result.Reason = $"FFmpeg not found in any of {result.AttemptedPaths.Count} candidate locations";
        _logger.LogWarning("FFmpeg not found after checking {Count} locations", result.AttemptedPaths.Count);
        
        return result;
    }

    /// <summary>
    /// Validate a specific FFmpeg path
    /// </summary>
    public async Task<FfmpegValidationResult> ValidatePathAsync(
        string ffmpegPath,
        CancellationToken ct = default)
    {
        var result = new FfmpegValidationResult();
        result.AttemptedPaths.Add(ffmpegPath);

        // Resolve path - could be exe or directory
        string resolvedPath;
        
        if (File.Exists(ffmpegPath))
        {
            resolvedPath = ffmpegPath;
        }
        else if (Directory.Exists(ffmpegPath))
        {
            // Try to find ffmpeg executable in directory
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            var exePath = Path.Combine(ffmpegPath, exeName);
            
            if (File.Exists(exePath))
            {
                resolvedPath = exePath;
            }
            else
            {
                // Try bin subdirectory
                exePath = Path.Combine(ffmpegPath, "bin", exeName);
                if (File.Exists(exePath))
                {
                    resolvedPath = exePath;
                }
                else
                {
                    result.Found = false;
                    result.Reason = $"FFmpeg executable not found in directory: {ffmpegPath}";
                    return result;
                }
            }
        }
        else
        {
            result.Found = false;
            result.Reason = $"Path not found: {ffmpegPath}";
            return result;
        }

        var validation = await ValidateFfmpegBinaryAsync(resolvedPath, ct);
        
        if (validation.success)
        {
            result.Found = true;
            result.FfmpegPath = resolvedPath;
            result.VersionString = ExtractVersionString(validation.output);
            result.ValidationOutput = validation.output;
            result.Reason = "Valid FFmpeg binary";
        }
        else
        {
            result.Found = false;
            result.FfmpegPath = resolvedPath;
            result.Reason = $"Validation failed: {validation.error}";
        }

        return result;
    }

    /// <summary>
    /// Get list of candidate paths to check
    /// </summary>
    private List<string> GetCandidatePaths(string? configuredPath)
    {
        var candidates = new List<string>();
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

        // 1. Configured path from registry/install.json (if provided)
        if (!string.IsNullOrEmpty(configuredPath))
        {
            candidates.Add(configuredPath);
        }

        // 2. App-specific paths - dependencies folder (user manual copy location)
        var depsBin = Path.Combine(_dependenciesDirectory, "bin", exeName);
        candidates.Add(depsBin);
        
        // Also check without bin subdirectory
        var depsRoot = Path.Combine(_dependenciesDirectory, exeName);
        candidates.Add(depsRoot);

        // 3. Tools directory - managed installations
        var toolsFFmpegDir = Path.Combine(_toolsDirectory, "ffmpeg");
        if (Directory.Exists(toolsFFmpegDir))
        {
            // Check for version subdirectories
            var versionDirs = Directory.GetDirectories(toolsFFmpegDir)
                .OrderByDescending(d => d)
                .ToList();
            
            foreach (var versionDir in versionDirs)
            {
                // Try bin subdirectory first
                var binPath = Path.Combine(versionDir, "bin", exeName);
                candidates.Add(binPath);
                
                // Try root of version directory
                var rootPath = Path.Combine(versionDir, exeName);
                candidates.Add(rootPath);
            }
        }

        // Also check direct ffmpeg executable in tools root
        var toolsDirectExe = Path.Combine(_toolsDirectory, "ffmpeg", exeName);
        candidates.Add(toolsDirectExe);

        return candidates;
    }

    /// <summary>
    /// Check PATH environment variable for FFmpeg
    /// </summary>
    private async Task<FfmpegValidationResult> CheckPathEnvironmentAsync(CancellationToken ct)
    {
        var result = new FfmpegValidationResult();
        result.AttemptedPaths.Add("PATH");

        try
        {
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            var validation = await ValidateFfmpegBinaryAsync(exeName, ct);
            
            if (validation.success)
            {
                result.Found = true;
                result.FfmpegPath = "ffmpeg"; // On PATH
                result.VersionString = ExtractVersionString(validation.output);
                result.ValidationOutput = validation.output;
                result.Reason = "Found on PATH";
                _logger.LogInformation("FFmpeg found on PATH");
            }
            else
            {
                result.Found = false;
                result.Reason = "Not found on PATH";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check PATH for FFmpeg");
            result.Found = false;
            result.Reason = $"PATH check failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Validate FFmpeg binary by running -version
    /// </summary>
    private async Task<(bool success, string? output, string? error)> ValidateFfmpegBinaryAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, null, "Failed to start process");
            }

            await process.WaitForExitAsync(ct);

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            if (process.ExitCode != 0)
            {
                return (false, null, $"Exit code {process.ExitCode}: {stderr}");
            }

            if (string.IsNullOrEmpty(stdout) || !stdout.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                return (false, stdout, "Output does not contain version information");
            }

            return (true, stdout, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Extract version string from ffmpeg -version output
    /// </summary>
    private string? ExtractVersionString(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            // First line typically contains: "ffmpeg version N-xxxxx-..."
            var firstLine = output.Split('\n')[0];
            if (firstLine.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    return parts[2]; // Version string
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract version string");
        }

        return null;
    }
}
