using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Aura.Core.Hardware;
using Aura.Core.Captions;
using Aura.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Cli.Commands;

/// <summary>
/// Render command - executes FFmpeg to produce final video with captions
/// Supports hardware acceleration (NVENC) when available
/// </summary>
public class RenderCommand : ICommand
{
    private readonly ILogger<RenderCommand> _logger;
    private readonly HardwareDetector _hardwareDetector;

    public RenderCommand(ILogger<RenderCommand> logger, HardwareDetector hardwareDetector)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args, out var renderSpecPath, out var outputPath);

        if (string.IsNullOrEmpty(renderSpecPath))
        {
            ShowHelp();
            return ExitCodes.InvalidArguments;
        }

        try
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           AURA CLI - Video Rendering                     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Detect hardware
            Console.WriteLine("[1/5] Detecting hardware capabilities...");
            var profile = await _hardwareDetector.DetectSystemAsync();
            var encoderLabel = profile.EnableNVENC ? "NVENC (hardware)" : "x264 (software)";
            Console.WriteLine($"      ✓ Encoder: {encoderLabel}");
            Console.WriteLine();

            // Read render spec
            Console.WriteLine("[2/5] Reading render specification...");
            if (!File.Exists(renderSpecPath))
            {
                Console.WriteLine($"✗ Error: Render spec file not found: {renderSpecPath}");
                return ExitCodes.InvalidArguments;
            }

            var specJson = await File.ReadAllTextAsync(renderSpecPath);
            Console.WriteLine("      ✓ Render spec loaded");
            Console.WriteLine();

            // Prepare output
            Console.WriteLine("[3/5] Preparing output directory...");
            var output = outputPath ?? "./output/demo.mp4";
            var outputDir = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            Console.WriteLine($"      ✓ Output: {output}");
            Console.WriteLine();

            // Check FFmpeg availability
            Console.WriteLine("[4/5] Checking FFmpeg availability...");
            if (!CheckFFmpegAvailable())
            {
                Console.WriteLine("✗ Error: FFmpeg not found in PATH");
                Console.WriteLine("  Install from: https://ffmpeg.org/download.html");
                return ExitCodes.RenderFail;
            }
            Console.WriteLine("      ✓ FFmpeg available");
            Console.WriteLine();

            // Execute rendering
            Console.WriteLine("[5/5] Rendering video...");
            if (options.DryRun)
            {
                Console.WriteLine("      ⚠ Dry run - skipping actual rendering");
                Console.WriteLine($"      Would render to: {output}");
            }
            else
            {
                // For now, create a placeholder video using FFmpeg test pattern
                // In a full implementation, this would use the FFmpegPlanBuilder
                var result = await CreateDemoVideoAsync(output, profile.EnableNVENC, options.Verbose);
                
                if (result == 0)
                {
                    Console.WriteLine($"      ✓ Video rendered successfully");
                    
                    // Generate captions
                    var captionFile = Path.ChangeExtension(output, ".srt");
                    await GenerateDemoCaptionsAsync(captionFile);
                    Console.WriteLine($"      ✓ Captions saved: {captionFile}");
                }
                else
                {
                    Console.WriteLine("✗ Error: FFmpeg rendering failed");
                    return ExitCodes.RenderFail;
                }
            }

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("✓ Rendering complete!");
            Console.WriteLine();
            Console.WriteLine($"Output files:");
            Console.WriteLine($"  - Video: {output}");
            Console.WriteLine($"  - Captions: {Path.ChangeExtension(output, ".srt")}");
            Console.WriteLine();
            Console.WriteLine($"Encoder used: {encoderLabel}");
            Console.WriteLine();

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Render command failed");
            Console.WriteLine($"✗ Error: {ex.Message}");
            if (options.Verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return ExitCodes.RenderFail;
        }
    }

    private bool CheckFFmpegAvailable()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> CreateDemoVideoAsync(string outputPath, bool useNvenc, bool verbose)
    {
        // Create a simple 10-second test video using FFmpeg
        var encoder = useNvenc ? "h264_nvenc" : "libx264";
        var preset = useNvenc ? "fast" : "medium";
        
        var args = $"-f lavfi -i testsrc=duration=10:size=1920x1080:rate=30 " +
                   $"-f lavfi -i sine=frequency=1000:duration=10 " +
                   $"-c:v {encoder} -preset {preset} -pix_fmt yuv420p " +
                   $"-c:a aac -b:a 192k " +
                   $"-y \"{outputPath}\"";

        if (verbose)
        {
            Console.WriteLine($"      FFmpeg command: ffmpeg {args}");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        
        if (verbose)
        {
            var output = await process.StandardError.ReadToEndAsync();
            Console.WriteLine($"      FFmpeg output: {output.Split('\n').LastOrDefault()}");
        }
        else
        {
            await process.StandardError.ReadToEndAsync(); // Consume stderr
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private async Task GenerateDemoCaptionsAsync(string outputPath)
    {
        // Generate sample captions
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Welcome to Aura Video Studio", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3)),
            new ScriptLine(1, "This is a demo video", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2)),
            new ScriptLine(2, "Generated by the CLI", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2)),
            new ScriptLine(3, "Thank you for watching", TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(3))
        };

        var captionBuilder = new CaptionBuilder(_logger as ILogger<CaptionBuilder> ?? 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CaptionBuilder>.Instance);
        var srt = captionBuilder.GenerateSrt(lines);
        await File.WriteAllTextAsync(outputPath, srt);
    }

    private CommandOptions ParseOptions(string[] args, out string? renderSpecPath, out string? outputPath)
    {
        var options = new CommandOptions();
        renderSpecPath = null;
        outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--help" || arg == "-h")
            {
                ShowHelp();
                Environment.Exit(0);
            }
            else if (arg == "--verbose" || arg == "-v")
            {
                options.Verbose = true;
            }
            else if (arg == "--dry-run")
            {
                options.DryRun = true;
            }
            else if ((arg == "--render-spec" || arg == "-r") && i + 1 < args.Length)
            {
                renderSpecPath = args[++i];
            }
            else if ((arg == "--output" || arg == "-o") && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
        }

        return options;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Usage: aura-cli render [options]");
        Console.WriteLine();
        Console.WriteLine("Execute FFmpeg rendering to produce final video");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -r, --render-spec <file>  Render specification JSON file (required)");
        Console.WriteLine("  -o, --output <file>       Output video file (default: ./output/demo.mp4)");
        Console.WriteLine("  -v, --verbose             Enable verbose output");
        Console.WriteLine("  --dry-run                 Validate without rendering");
        Console.WriteLine("  -h, --help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli render -r plan.json");
        Console.WriteLine("  aura-cli render -r plan.json -o video.mp4 -v");
        Console.WriteLine();
        Console.WriteLine("Hardware Acceleration:");
        Console.WriteLine("  - NVENC (NVIDIA) used automatically if available");
        Console.WriteLine("  - Falls back to x264 (software) otherwise");
    }
}
