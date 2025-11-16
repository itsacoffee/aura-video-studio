using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Single candidate check result for FFmpeg direct check
/// </summary>
public record FFmpegCandidateResult
{
    public string Label { get; init; } = "";
    public string? Path { get; init; }
    public bool Exists { get; init; }
    public bool ExecutionAttempted { get; init; }
    public int? ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public string? RawVersionOutput { get; init; }
    public string? VersionParsed { get; init; }
    public bool Valid { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Overall result of FFmpeg direct check
/// </summary>
public record FFmpegDirectCheckResult
{
    public List<FFmpegCandidateResult> Candidates { get; init; } = new();
    public bool Installed { get; init; }
    public bool Valid { get; init; }
    public string? Source { get; init; }
    public string? ChosenPath { get; init; }
    public string? Version { get; init; }
}

/// <summary>
/// Service for explicit, deterministic FFmpeg checking with full diagnostics
/// </summary>
public class FFmpegDirectCheckService
{
    private readonly ILogger<FFmpegDirectCheckService> _logger;
    private const int TimeoutSeconds = 3;
    private readonly string _managedInstallRoot;

    public FFmpegDirectCheckService(
        ILogger<FFmpegDirectCheckService> logger,
        string? managedInstallRoot = null)
    {
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(managedInstallRoot))
        {
            _managedInstallRoot = managedInstallRoot!;
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _managedInstallRoot = Path.Combine(localAppData, "AuraVideoStudio", "ffmpeg");
        }
    }

    /// <summary>
    /// Perform explicit, non-cached check of all FFmpeg candidates
    /// </summary>
    public async Task<FFmpegDirectCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting FFmpeg direct check with {Timeout}s timeout per candidate", TimeoutSeconds);

        var candidates = new List<FFmpegCandidateResult>();
        FFmpegCandidateResult? validCandidate = null;

        var envVarPath = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(envVarPath))
        {
            var envResult = await CheckCandidateAsync("EnvVar", envVarPath, cancellationToken).ConfigureAwait(false);
            candidates.Add(envResult);
            if (envResult.Valid && validCandidate == null)
            {
                validCandidate = envResult;
            }
        }

        var managedResult = await CheckManagedInstallAsync(cancellationToken).ConfigureAwait(false);
        candidates.Add(managedResult);
        if (managedResult.Valid && validCandidate == null)
        {
            validCandidate = managedResult;
        }

        var pathResult = await CheckPathAsync(cancellationToken).ConfigureAwait(false);
        candidates.Add(pathResult);
        if (pathResult.Valid && validCandidate == null)
        {
            validCandidate = pathResult;
        }

        return new FFmpegDirectCheckResult
        {
            Candidates = candidates,
            Installed = validCandidate != null,
            Valid = validCandidate != null,
            Source = validCandidate?.Label,
            ChosenPath = validCandidate?.Path,
            Version = validCandidate?.VersionParsed
        };
    }

    private async Task<FFmpegCandidateResult> CheckCandidateAsync(
        string label,
        string path,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking candidate {Label}: {Path}", label, path);

        var result = new FFmpegCandidateResult { Label = label, Path = path };

        if (!File.Exists(path))
        {
            _logger.LogDebug("Candidate {Label} does not exist: {Path}", label, path);
            return result with { Exists = false, Error = "FileNotFound" };
        }

        result = result with { Exists = true, ExecutionAttempted = true };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return result with { Error = "ProcessStartFailed" };
            }

            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            result = result with
            {
                ExitCode = process.ExitCode,
                RawVersionOutput = stdout,
                TimedOut = false
            };

            if (process.ExitCode != 0)
            {
                return result with { Error = $"ExitCode{process.ExitCode}" };
            }

            if (string.IsNullOrEmpty(stdout) || !stdout.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                return result with { Error = "InvalidVersionOutput" };
            }

            var version = ExtractVersion(stdout);
            if (version == null)
            {
                return result with { Error = "VersionParseError" };
            }

            if (!IsVersionValid(version))
            {
                return result with { VersionParsed = version, Error = "VersionTooOld" };
            }

            _logger.LogInformation("Valid FFmpeg found at {Label}: {Path}, Version: {Version}", label, path, version);
            return result with { VersionParsed = version, Valid = true };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("FFmpeg check timed out for {Label}: {Path}", label, path);
            return result with { TimedOut = true, Error = "Timeout" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking FFmpeg candidate {Label}: {Path}", label, path);
            return result with { Error = $"Exception:{ex.GetType().Name}" };
        }
    }

    private async Task<FFmpegCandidateResult> CheckManagedInstallAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking managed install: {Root}", _managedInstallRoot);

        var result = new FFmpegCandidateResult
        {
            Label = "Managed",
            Path = _managedInstallRoot
        };

        if (!Directory.Exists(_managedInstallRoot))
        {
            return result with { Exists = false, Error = "ManagedDirNotFound" };
        }

        result = result with { Exists = true };

        try
        {
            var versionDirs = Directory.GetDirectories(_managedInstallRoot);
            if (versionDirs.Length == 0)
            {
                return result with { Error = "NoVersionsInstalled" };
            }

            Array.Sort(versionDirs, StringComparer.OrdinalIgnoreCase);
            Array.Reverse(versionDirs);

            foreach (var versionDir in versionDirs)
            {
                var manifestPath = Path.Combine(versionDir, "install.json");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<ManagedInstallManifest>(manifestJson);

                if (manifest?.FfmpegPath == null || !File.Exists(manifest.FfmpegPath))
                {
                    continue;
                }

                var checkResult = await CheckCandidateAsync("Managed", manifest.FfmpegPath, cancellationToken)
                    .ConfigureAwait(false);
                
                if (checkResult.Valid)
                {
                    return checkResult with { Path = manifest.FfmpegPath };
                }
            }

            return result with { Error = "NoValidManagedInstall" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking managed install");
            return result with { Error = $"Exception:{ex.GetType().Name}" };
        }
    }

    private async Task<FFmpegCandidateResult> CheckPathAsync(CancellationToken cancellationToken)
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
        _logger.LogDebug("Checking PATH for: {ExeName}", exeName);

        var result = new FFmpegCandidateResult
        {
            Label = "PATH",
            Path = exeName
        };

        return await CheckCandidateAsync("PATH", exeName, cancellationToken).ConfigureAwait(false);
    }

    private string? ExtractVersion(string output)
    {
        try
        {
            var firstLine = output.Split('\n')[0];
            if (firstLine.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    return parts[2];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract version string");
        }

        return null;
    }

    private bool IsVersionValid(string version)
    {
        try
        {
            var versionParts = version.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (versionParts.Length > 0 && int.TryParse(versionParts[0], out var major))
            {
                return major >= 4;
            }
        }
        catch
        {
            // Version parsing failed
        }

        return false;
    }

    private class ManagedInstallManifest
    {
        public string? FfmpegPath { get; set; }
        public string? Version { get; set; }
    }
}
