using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Errors;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Video;

public class FfmpegVideoComposer : IVideoComposer
{
    private readonly ILogger<FfmpegVideoComposer> _logger;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly string? _configuredFfmpegPath;
    private readonly string _workingDirectory;
    private string _outputDirectory;
    private readonly string _logsDirectory;

    public FfmpegVideoComposer(ILogger<FfmpegVideoComposer> logger, IFfmpegLocator ffmpegLocator, string? configuredFfmpegPath = null, string? outputDirectory = null)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _configuredFfmpegPath = configuredFfmpegPath;
        _workingDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "Render");
        _outputDirectory = outputDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "AuraVideoStudio");
        _logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Logs", "ffmpeg");
        
        // Ensure working directory exists
        if (!Directory.Exists(_workingDirectory))
        {
            Directory.CreateDirectory(_workingDirectory);
        }
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
        
        // Ensure logs directory exists
        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
        }
    }

    public async Task<string> RenderAsync(Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? jobId;
        
        _logger.LogInformation("Starting FFmpeg render (JobId={JobId}, CorrelationId={CorrelationId}) at {Resolution}p", 
            jobId, correlationId, spec.Res.Height);
        
        // Set up FFmpeg log file path
        var ffmpegLogPath = Path.Combine(_logsDirectory, $"{jobId}.log");
        StreamWriter? logWriter = null;
        
        // Resolve FFmpeg path once at the start - this is the single source of truth for this render job
        string ffmpegPath;
        try
        {
            ffmpegPath = await _ffmpegLocator.GetEffectiveFfmpegPathAsync(_configuredFfmpegPath, ct);
            _logger.LogInformation("Resolved FFmpeg path for job {JobId}: {FfmpegPath}", jobId, ffmpegPath);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to resolve FFmpeg path for job {JobId}", jobId);
            throw FfmpegException.NotFound(correlationId: correlationId);
        }
        
        // Validate FFmpeg binary before starting
        await ValidateFfmpegBinaryAsync(ffmpegPath, jobId, correlationId, ct);
        
        // Pre-validate audio files - pass the resolved ffmpeg path
        await PreValidateAudioAsync(timeline, ffmpegPath, jobId, correlationId, ct);
        
        // Create output file path using configured output directory
        string outputFilePath = Path.Combine(
            _outputDirectory,
            $"AuraVideoStudio_{DateTime.Now:yyyyMMddHHmmss}.{spec.Container}");
        
        // Build the FFmpeg command
        string ffmpegCommand = BuildFfmpegCommand(timeline, spec, outputFilePath);
        
        _logger.LogInformation("FFmpeg command (JobId={JobId}): {FFmpegPath} {Command}", 
            jobId, ffmpegPath, ffmpegCommand);
        
        // Create process to run FFmpeg with stderr/stdout capture
        var stderrBuilder = new StringBuilder();
        var stdoutBuilder = new StringBuilder();
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = ffmpegCommand,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            },
            EnableRaisingEvents = true
        };
        
        // Track progress
        var totalDuration = timeline.Scenes.Count > 0 
            ? timeline.Scenes[^1].Start + timeline.Scenes[^1].Duration 
            : TimeSpan.FromMinutes(1);
        
        var startTime = DateTime.Now;
        var lastReportTime = DateTime.Now;
        
        // Initialize log writer for FFmpeg output
        try
        {
            logWriter = new StreamWriter(ffmpegLogPath, append: false, encoding: Encoding.UTF8)
            {
                AutoFlush = true
            };
            logWriter.WriteLine($"FFmpeg Render Log - Job ID: {jobId}");
            logWriter.WriteLine($"Correlation ID: {correlationId}");
            logWriter.WriteLine($"Started: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            logWriter.WriteLine($"Resolution: {spec.Res.Width}x{spec.Res.Height}");
            logWriter.WriteLine($"FFmpeg Path: {ffmpegPath}");
            logWriter.WriteLine($"Command: {ffmpegCommand}");
            logWriter.WriteLine(new string('-', 80));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create FFmpeg log file at {LogPath}", ffmpegLogPath);
        }

        // Set up output handler to parse progress and capture output
        process.ErrorDataReceived += (sender, args) =>
        {
            if (string.IsNullOrEmpty(args.Data)) return;
            
            // Capture for error reporting
            stderrBuilder.AppendLine(args.Data);
            
            // Write to log file
            try
            {
                logWriter?.WriteLine($"[stderr] {args.Data}");
            }
            catch
            {
                // Ignore log write errors
            }
            
            // Log the output
            _logger.LogTrace("FFmpeg stderr: {Output}", args.Data);
            
            // Parse progress if it contains time information
            if (args.Data.Contains("time="))
            {
                // Parse the time value (format: time=00:00:12.34)
                var timeMatch = System.Text.RegularExpressions.Regex.Match(args.Data, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
                if (timeMatch.Success)
                {
                    var timeStr = timeMatch.Groups[1].Value;
                    if (TimeSpan.TryParse(timeStr, out var currentTime))
                    {
                        var now = DateTime.Now;
                        
                        // Report progress at most once per second
                        if ((now - lastReportTime).TotalSeconds >= 1)
                        {
                            lastReportTime = now;
                            
                            // Calculate progress percentage
                            float percentage = (float)(currentTime.TotalSeconds / totalDuration.TotalSeconds * 100);
                            percentage = Math.Clamp(percentage, 0, 100);
                            
                            // Calculate time remaining
                            var elapsed = now - startTime;
                            var estimatedTotal = TimeSpan.FromSeconds(
                                elapsed.TotalSeconds / (percentage / 100));
                            var remaining = estimatedTotal - elapsed;
                            
                            // Report progress
                            progress.Report(new RenderProgress(
                                percentage,
                                elapsed,
                                remaining,
                                "Rendering video"));
                        }
                    }
                }
            }
        };
        
        // Capture stdout as well
        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                stdoutBuilder.AppendLine(args.Data);
                
                // Write to log file
                try
                {
                    logWriter?.WriteLine($"[stdout] {args.Data}");
                }
                catch
                {
                    // Ignore log write errors
                }
                
                _logger.LogTrace("FFmpeg stdout: {Output}", args.Data);
            }
        };
        
        // Set up task to wait for process completion
        var tcs = new TaskCompletionSource<bool>();
        
        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0)
            {
                tcs.SetResult(true);
            }
            else
            {
                var stderr = stderrBuilder.ToString();
                var exception = FfmpegException.FromProcessFailure(
                    process.ExitCode, 
                    stderr, 
                    jobId, 
                    correlationId);
                tcs.SetException(exception);
            }
        };
        
        // Start the process
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        
        // Register cancellation with graceful termination
        var cancellationRegistration = ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    _logger.LogWarning("Cancelling FFmpeg render (JobId={JobId})", jobId);
                    
                    // Try graceful termination first (send 'q' to stdin if possible)
                    try
                    {
                        process.StandardInput?.Write('q');
                        process.StandardInput?.Flush();
                        
                        // Give it 2 seconds to exit gracefully
                        if (!process.WaitForExit(2000))
                        {
                            _logger.LogWarning("Graceful termination timeout, killing process (JobId={JobId})", jobId);
                            process.Kill(entireProcessTree: true);
                        }
                    }
                    catch
                    {
                        // If graceful termination fails, kill immediately
                        process.Kill(entireProcessTree: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to terminate FFmpeg process during cancellation");
            }
        });
        
        // Wait for completion or cancellation with timeout
        try
        {
            // Set a reasonable timeout (30 minutes for renders)
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30), ct);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                _logger.LogError("FFmpeg render timeout after 30 minutes (JobId={JobId})", jobId);
                
                // Kill the process
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception killEx)
                {
                    _logger.LogWarning(killEx, "Failed to kill timed-out FFmpeg process");
                }
                
                throw new FfmpegException(
                    "FFmpeg render operation timed out after 30 minutes",
                    FfmpegErrorCategory.Timeout,
                    jobId: jobId,
                    correlationId: correlationId,
                    suggestedActions: new[]
                    {
                        "Try with shorter content or lower resolution",
                        "Check system resources (CPU, disk space)",
                        "Ensure FFmpeg is not hanging on a corrupted input file"
                    });
            }
            
            await tcs.Task; // This will throw if the process failed
        }
        catch (FfmpegException)
        {
            // Already properly formatted FFmpeg exception
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected error
            _logger.LogError(ex, "Unexpected FFmpeg error (JobId={JobId})", jobId);
            throw new InvalidOperationException($"FFmpeg render failed unexpectedly: {ex.Message} (JobId: {jobId}, CorrelationId: {correlationId})", ex);
        }
        finally
        {
            // Dispose cancellation registration
            cancellationRegistration.Dispose();
            
            // Ensure process is cleaned up
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            // Close log file
            try
            {
                logWriter?.WriteLine(new string('-', 80));
                logWriter?.WriteLine($"Completed: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                logWriter?.WriteLine($"Exit Code: {process.ExitCode}");
                logWriter?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            // Dispose process
            process.Dispose();
        }
        
        _logger.LogInformation("Render completed successfully (JobId={JobId}): {OutputPath}", jobId, outputFilePath);
        _logger.LogInformation("FFmpeg log written to: {LogPath}", ffmpegLogPath);
        
        // Report 100% completion
        progress.Report(new RenderProgress(
            100,
            DateTime.Now - startTime,
            TimeSpan.Zero,
            "Render complete"));
        
        return outputFilePath;
    }
    
    private string BuildFfmpegCommand(Timeline timeline, RenderSpec spec, string outputPath)
    {
        var args = new List<string>();
        
        // Input file(s)
        args.Add("-i"); // Narration
        args.Add($"\"{timeline.NarrationPath}\"");
        
        if (!string.IsNullOrEmpty(timeline.MusicPath))
        {
            args.Add("-i"); // Music
            args.Add($"\"{timeline.MusicPath}\"");
        }
        
        // Scene assets would be added here in a more complete implementation
        // For simplicity, we'll assume a basic slideshow with narration and music
        
        // Output file format settings
        args.Add("-c:v");
        args.Add("libx264"); // Default to h264 for compatibility
        
        args.Add("-preset");
        args.Add("medium"); // Balance quality and speed
        
        args.Add("-crf");
        args.Add("23"); // Good quality
        
        // Resolution
        args.Add("-s");
        args.Add($"{spec.Res.Width}x{spec.Res.Height}");
        
        // Audio settings
        args.Add("-c:a");
        args.Add("aac");
        
        args.Add("-b:a");
        args.Add($"{spec.AudioBitrateK}k");
        
        // Video bitrate
        args.Add("-b:v");
        args.Add($"{spec.VideoBitrateK}k");
        
        // Framerate
        args.Add("-r");
        args.Add("30");
        
        // Pixel format
        args.Add("-pix_fmt");
        args.Add("yuv420p");
        
        // Overwrite output file if it exists
        args.Add("-y");
        
        // Output file path
        args.Add($"\"{outputPath}\"");
        
        return string.Join(" ", args);
    }
    
    /// <summary>
    /// Validate FFmpeg binary before use
    /// </summary>
    private async Task ValidateFfmpegBinaryAsync(string ffmpegPath, string jobId, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("Validating FFmpeg binary: {Path}", ffmpegPath);
        
        // For executables in PATH (like "ffmpeg"), File.Exists() will return false
        // So we skip the file existence check and go straight to running the command
        // The command execution will fail if FFmpeg is not found, giving us better error info
        bool isPathExecutable = !Path.IsPathRooted(ffmpegPath) && 
                                !ffmpegPath.Contains(Path.DirectorySeparatorChar) &&
                                !ffmpegPath.Contains(Path.AltDirectorySeparatorChar);
        
        // Check file exists only if it's an absolute or relative path (not just an executable name)
        if (!isPathExecutable && !File.Exists(ffmpegPath))
        {
            var error = new
            {
                code = "E302-FFMPEG_VALIDATION",
                message = "FFmpeg binary not found",
                ffmpegPath = ffmpegPath,
                correlationId,
                howToFix = new[]
                {
                    "Install FFmpeg via Download Center",
                    "Attach an existing FFmpeg installation",
                    "Check FFmpeg installation path in settings",
                    "Add FFmpeg to system PATH"
                }
            };
            
            _logger.LogError("FFmpeg validation failed: {Error}", System.Text.Json.JsonSerializer.Serialize(error));
            throw new InvalidOperationException($"FFmpeg binary not found at {ffmpegPath}. " +
                $"Install FFmpeg via Download Center or attach an existing installation. (CorrelationId: {correlationId})");
        }
        
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
                throw new InvalidOperationException("Failed to start FFmpeg process for validation");
            }
            
            await process.WaitForExitAsync(ct);
            
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = new
                {
                    code = "E302-FFMPEG_VALIDATION",
                    message = "FFmpeg validation failed",
                    exitCode = process.ExitCode,
                    stderr = stderr.Length > 1000 ? stderr.Substring(0, 1000) + "..." : stderr,
                    correlationId,
                    howToFix = new[]
                    {
                        "FFmpeg binary may be corrupted - try reinstalling",
                        "Check system dependencies (Visual C++ Redistributable on Windows)",
                        "Use 'Repair' option in Download Center"
                    }
                };
                
                _logger.LogError("FFmpeg validation failed: {Error}", System.Text.Json.JsonSerializer.Serialize(error));
                throw new InvalidOperationException($"FFmpeg validation failed with exit code {process.ExitCode}. " +
                    $"See logs for details. (CorrelationId: {correlationId})");
            }
            
            _logger.LogInformation("FFmpeg validation successful: {Version}", 
                output.Split('\n').FirstOrDefault(l => l.Contains("ffmpeg version")) ?? "version unknown");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            var error = new
            {
                code = "E302-FFMPEG_VALIDATION",
                message = "FFmpeg validation exception",
                exception = ex.Message,
                correlationId,
                howToFix = new[]
                {
                    "Ensure FFmpeg has execute permissions",
                    "Check antivirus/security software blocking FFmpeg",
                    "Try attaching a different FFmpeg installation"
                }
            };
            
            _logger.LogError(ex, "FFmpeg validation exception: {Error}", System.Text.Json.JsonSerializer.Serialize(error));
            throw new InvalidOperationException($"FFmpeg validation failed: {ex.Message} (CorrelationId: {correlationId})", ex);
        }
    }
    
    /// <summary>
    /// Pre-validate audio files before rendering and attempt remediation if needed
    /// </summary>
    private async Task PreValidateAudioAsync(Timeline timeline, string ffmpegPath, string jobId, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("Pre-validating audio files (JobId={JobId})", jobId);
        
        // Get ffprobe path (same directory as ffmpeg)
        var ffmpegDir = Path.GetDirectoryName(ffmpegPath);
        var ffprobePath = ffmpegDir != null ? Path.Combine(ffmpegDir, "ffprobe.exe") : null;
        if (ffprobePath != null && !File.Exists(ffprobePath))
        {
            ffprobePath = null;
        }
        
        var validator = new Aura.Core.Audio.AudioValidator(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Aura.Core.Audio.AudioValidator>.Instance,
            ffmpegPath,
            ffprobePath);
        
        // Validate narration
        if (!string.IsNullOrEmpty(timeline.NarrationPath))
        {
            await ValidateAndRemediateAudioFileAsync(
                validator, 
                timeline.NarrationPath, 
                "narration", 
                jobId, 
                correlationId, 
                ct);
        }
        
        // Validate music if present
        if (!string.IsNullOrEmpty(timeline.MusicPath))
        {
            await ValidateAndRemediateAudioFileAsync(
                validator, 
                timeline.MusicPath, 
                "music", 
                jobId, 
                correlationId, 
                ct);
        }
        
        _logger.LogInformation("Audio pre-validation complete (JobId={JobId})", jobId);
    }
    
    /// <summary>
    /// Validate a single audio file and attempt remediation if corrupted
    /// </summary>
    private async Task ValidateAndRemediateAudioFileAsync(
        Aura.Core.Audio.AudioValidator validator,
        string audioPath,
        string audioType,
        string jobId,
        string correlationId,
        CancellationToken ct)
    {
        _logger.LogInformation("Validating {AudioType} audio: {Path} (JobId={JobId})", 
            audioType, audioPath, jobId);
        
        var validation = await validator.ValidateAsync(audioPath, ct);
        
        if (validation.IsValid)
        {
            _logger.LogInformation("{AudioType} audio validation passed (JobId={JobId})", audioType, jobId);
            return;
        }
        
        // Audio is invalid - attempt remediation
        _logger.LogWarning("{AudioType} audio validation failed: {Error} (JobId={JobId})", 
            audioType, validation.ErrorMessage, jobId);
        
        if (validation.IsCorrupted)
        {
            _logger.LogInformation("Attempting to re-encode corrupted {AudioType} audio (JobId={JobId})", 
                audioType, jobId);
            
            // Try re-encoding to a clean WAV
            var reEncodedPath = Path.Combine(
                Path.GetDirectoryName(audioPath) ?? Path.GetTempPath(),
                $"{Path.GetFileNameWithoutExtension(audioPath)}_reencoded.wav");
            
            var (success, errorMessage) = await validator.ReencodeAsync(audioPath, reEncodedPath, ct);
            
            if (success)
            {
                _logger.LogInformation("Successfully re-encoded {AudioType} audio (JobId={JobId})", 
                    audioType, jobId);
                
                // Replace the original file with the re-encoded version
                File.Delete(audioPath);
                File.Move(reEncodedPath, audioPath);
                
                _logger.LogInformation("Replaced corrupted {AudioType} with re-encoded version (JobId={JobId})", 
                    audioType, jobId);
                return;
            }
            
            _logger.LogError("Re-encoding failed: {Error}. Attempting to generate silent fallback (JobId={JobId})", 
                errorMessage, jobId);
            
            // Re-encoding failed - generate silent WAV as fallback
            var (silentSuccess, silentError) = await validator.GenerateSilentWavAsync(
                audioPath + ".silent.wav", 
                10.0, // 10 seconds default
                ct);
            
            if (silentSuccess)
            {
                _logger.LogWarning("Generated silent {AudioType} fallback (JobId={JobId})", audioType, jobId);
                
                // Replace with silent version
                File.Delete(audioPath);
                File.Move(audioPath + ".silent.wav", audioPath);
                return;
            }
            
            // All remediation attempts failed
            var error = new
            {
                code = "E305-AUDIO_VALIDATION",
                message = $"{audioType} audio file is corrupted and could not be repaired",
                audioPath,
                validationError = validation.ErrorMessage,
                reencodeError = errorMessage,
                silentFallbackError = silentError,
                jobId,
                correlationId,
                howToFix = new[]
                {
                    $"Re-generate the {audioType} audio",
                    "Ensure TTS provider is working correctly",
                    "Check audio file is not in use by another application",
                    "Try using a different TTS provider"
                }
            };
            
            _logger.LogError("All audio remediation attempts failed: {Error}", 
                System.Text.Json.JsonSerializer.Serialize(error));
            
            throw new InvalidOperationException(
                $"{audioType} audio validation failed and remediation unsuccessful: {validation.ErrorMessage}. " +
                $"CorrelationId: {correlationId}, JobId: {jobId}");
        }
        
        // Not corrupted but still invalid (e.g., file not found)
        throw new InvalidOperationException(
            $"{audioType} audio validation failed: {validation.ErrorMessage}. " +
            $"CorrelationId: {correlationId}, JobId: {jobId}");
    }
    

}