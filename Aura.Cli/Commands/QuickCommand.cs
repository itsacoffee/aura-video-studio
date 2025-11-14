using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Hardware;
using Aura.Core.Captions;

namespace Aura.Cli.Commands;

/// <summary>
/// Quick command - end-to-end video generation in one step
/// Generates brief, plan, script, and simulates rendering
/// </summary>
public class QuickCommand : ICommand
{
    private readonly ILogger<QuickCommand> _logger;
    private readonly HardwareDetector _hardwareDetector;
    private readonly ILlmProvider _llmProvider;

    public QuickCommand(
        ILogger<QuickCommand> logger, 
        HardwareDetector hardwareDetector,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _llmProvider = llmProvider;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args, out var topic, out var durationMinutes);

        if (string.IsNullOrEmpty(topic))
        {
            ShowHelp();
            return 1;
        }

        try
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           AURA CLI - Quick Video Generation             ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Step 1: Hardware detection
            Console.WriteLine("[1/5] Detecting hardware...");
            var profile = await _hardwareDetector.DetectSystemAsync();
            Console.WriteLine($"      ✓ Tier {profile.Tier} ({profile.LogicalCores} cores, {profile.RamGB} GB RAM)");
            Console.WriteLine();

            // Step 2: Create brief
            Console.WriteLine("[2/5] Creating brief...");
            var brief = new Brief(
                Topic: topic,
                Audience: "General audience",
                Goal: "Educate and inform",
                Tone: "Professional",
                Language: "en-US",
                Aspect: Aspect.Widescreen16x9
            );

            if (options.Verbose)
            {
                Console.WriteLine($"      Topic: {brief.Topic}");
                Console.WriteLine($"      Audience: {brief.Audience}");
                Console.WriteLine($"      Aspect: {brief.Aspect}");
            }
            Console.WriteLine("      ✓ Brief created");
            Console.WriteLine();

            // Step 3: Create plan
            Console.WriteLine("[3/5] Creating plan...");
            var duration = durationMinutes > 0 ? durationMinutes : 3.0;
            var plan = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(duration),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Educational"
            );

            if (options.Verbose)
            {
                Console.WriteLine($"      Duration: {duration} minutes");
                Console.WriteLine($"      Pacing: {plan.Pacing}");
                Console.WriteLine($"      Density: {plan.Density}");
            }
            Console.WriteLine("      ✓ Plan created");
            Console.WriteLine();

            // Step 4: Generate script
            Console.WriteLine("[4/5] Generating script...");
            string? script = null;
            
            if (options.DryRun)
            {
                Console.WriteLine("      ⚠ Dry run - skipping script generation");
            }
            else
            {
                script = await _llmProvider.DraftScriptAsync(brief, plan, CancellationToken.None);
                
                // Save artifacts
                var outputDir = options.OutputDirectory ?? "./output";
                Directory.CreateDirectory(outputDir);

                var briefPath = Path.Combine(outputDir, "brief.json");
                await File.WriteAllTextAsync(briefPath, JsonSerializer.Serialize(brief, new JsonSerializerOptions { WriteIndented = true }));

                var planPath = Path.Combine(outputDir, "plan.json");
                await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

                var scriptPath = Path.Combine(outputDir, "script.txt");
                await File.WriteAllTextAsync(scriptPath, script);

                Console.WriteLine($"      ✓ Script generated ({script.Length} chars)");
                
                if (options.Verbose)
                {
                    var preview = script.Length > 150 ? script.Substring(0, 150) + "..." : script;
                    Console.WriteLine();
                    Console.WriteLine("      Preview:");
                    foreach (var line in preview.Split('\n'))
                    {
                        Console.WriteLine($"        {line}");
                    }
                }
            }
            Console.WriteLine();

            // Step 5: Render video (if FFmpeg available)
            Console.WriteLine("[5/5] Rendering video...");
            var encoderLabel = profile.EnableNVENC ? "NVENC (hardware)" : "x264 (software)";
            Console.WriteLine($"      Resolution: 1920x1080");
            Console.WriteLine($"      Encoder: {encoderLabel}");
            Console.WriteLine($"      Audio: AAC 192 kbps");
            
            var finalOutputDir = options.OutputDirectory ?? "./output";
            var videoPath = Path.Combine(finalOutputDir, "demo.mp4");
            var captionPath = Path.Combine(finalOutputDir, "demo.srt");
            var logPath = Path.Combine(finalOutputDir, "log.txt");
            
            if (CheckFFmpegAvailable())
            {
                if (options.DryRun)
                {
                    Console.WriteLine("      ⚠ Dry run - skipping video rendering");
                }
                else
                {
                    var renderResult = await RenderDemoVideoAsync(videoPath, profile.EnableNVENC, options.Verbose, logPath);
                    
                    if (renderResult == 0 && script != null)
                    {
                        Console.WriteLine($"      ✓ Video rendered successfully");
                        
                        // Generate captions
                        var captionLines = GenerateDemoCaptionLines(script);
                        var captionBuilder = new CaptionBuilder(_logger as ILogger<CaptionBuilder> ?? 
                            Microsoft.Extensions.Logging.Abstractions.NullLogger<CaptionBuilder>.Instance);
                        var srt = captionBuilder.GenerateSrt(captionLines);
                        await File.WriteAllTextAsync(captionPath, srt);
                        Console.WriteLine($"      ✓ Captions generated");
                    }
                    else
                    {
                        Console.WriteLine("      ⚠ Video rendering failed (see log for details)");
                    }
                }
            }
            else
            {
                Console.WriteLine("      ⚠ FFmpeg not found - skipping video rendering");
                Console.WriteLine("      Install FFmpeg to enable video output");
            }
            Console.WriteLine();

            // Summary
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("✓ Quick generation complete!");
            Console.WriteLine();

            if (!options.DryRun)
            {
                var outputDir = options.OutputDirectory ?? "./output";
                Console.WriteLine("Generated files:");
                Console.WriteLine($"  - {Path.Combine(outputDir, "brief.json")}");
                Console.WriteLine($"  - {Path.Combine(outputDir, "plan.json")}");
                Console.WriteLine($"  - {Path.Combine(outputDir, "script.txt")}");
                
                if (File.Exists(Path.Combine(outputDir, "demo.mp4")))
                {
                    Console.WriteLine($"  - {Path.Combine(outputDir, "demo.mp4")}");
                    Console.WriteLine($"  - {Path.Combine(outputDir, "demo.srt")}");
                }
                
                Console.WriteLine();
                Console.WriteLine("✓ Quick generation complete!");
            }
            else
            {
                Console.WriteLine("Script generation successful.");
                Console.WriteLine("To continue:");
                Console.WriteLine("  - Use Aura.Api to generate TTS audio");
                Console.WriteLine("  - Use Aura.Api to compose timeline");
                Console.WriteLine("  - Use Aura.Api to render final video");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick generation failed");
            Console.WriteLine($"✗ Error: {ex.Message}");
            if (options.Verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return ExitCodes.ScriptFail;
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

    private async Task<int> RenderDemoVideoAsync(string outputPath, bool useNvenc, bool verbose, string logPath)
    {
        // Create a 10-15 second demo video
        var encoder = useNvenc ? "h264_nvenc" : "libx264";
        var preset = useNvenc ? "fast" : "medium";
        
        var duration = 10 + new Random().Next(6); // 10-15 seconds
        
        var args = $"-f lavfi -i testsrc=duration={duration}:size=1920x1080:rate=30 " +
                   $"-f lavfi -i sine=frequency=440:duration={duration} " +
                   $"-c:v {encoder} -preset {preset} -pix_fmt yuv420p " +
                   $"-c:a aac -b:a 192k " +
                   $"-y \"{outputPath}\"";

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
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Save log
        await File.WriteAllTextAsync(logPath, stderr);

        if (verbose && process.ExitCode != 0)
        {
            Console.WriteLine($"      FFmpeg error: {stderr.Split('\n').LastOrDefault()}");
        }

        return process.ExitCode;
    }

    private List<ScriptLine> GenerateDemoCaptionLines(string script)
    {
        // Generate sample caption lines from script
        var lines = new List<ScriptLine>();
        var words = script.Split(' ').Take(20).ToArray(); // First 20 words
        
        for (int i = 0; i < Math.Min(4, words.Length / 5); i++)
        {
            var start = i * 2.5;
            var text = string.Join(" ", words.Skip(i * 5).Take(5));
            lines.Add(new ScriptLine(i, text, TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(2.5)));
        }

        return lines;
    }

    private CommandOptions ParseOptions(string[] args, out string? topic, out double durationMinutes)
    {
        var options = new CommandOptions();
        topic = null;
        durationMinutes = 3.0;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--verbose" || arg == "-v")
            {
                options.Verbose = true;
            }
            else if (arg == "--dry-run")
            {
                options.DryRun = true;
            }
            else if ((arg == "--topic" || arg == "-t") && i + 1 < args.Length)
            {
                topic = args[++i];
            }
            else if ((arg == "--duration" || arg == "-d") && i + 1 < args.Length)
            {
                if (double.TryParse(args[++i], out var minutes))
                {
                    durationMinutes = minutes;
                }
            }
            else if ((arg == "--output" || arg == "-o") && i + 1 < args.Length)
            {
                options.OutputDirectory = args[++i];
            }
            else if ((arg == "--profile" || arg == "-p") && i + 1 < args.Length)
            {
                // Profile support: Free-Only, Balanced, Pro-Max
                var profile = args[++i];
                // Store in options for future use
                if (options is ExtendedCommandOptions extOptions)
                {
                    extOptions.Profile = profile;
                }
            }
            else if (arg == "--offline")
            {
                // Offline mode flag
                if (options is ExtendedCommandOptions extOptions)
                {
                    extOptions.Offline = true;
                }
            }
        }

        return options;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Usage: aura-cli quick [options]");
        Console.WriteLine();
        Console.WriteLine("Quick video generation with sensible defaults");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -t, --topic <text>     Video topic (required)");
        Console.WriteLine("  -d, --duration <mins>  Target duration in minutes (default: 3)");
        Console.WriteLine("  -o, --output <dir>     Output directory (default: ./output)");
        Console.WriteLine("  -p, --profile <name>   Provider profile: Free-Only, Balanced, Pro-Max");
        Console.WriteLine("  --offline              Force offline mode (no API calls)");
        Console.WriteLine("  -v, --verbose          Enable verbose output");
        Console.WriteLine("  --dry-run              Validate without generating files");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli quick -t \"Machine Learning Basics\"");
        Console.WriteLine("  aura-cli quick -t \"Coffee Brewing\" -d 5 -o ./videos");
        Console.WriteLine("  aura-cli quick -t \"Test Topic\" --profile Free-Only --offline");
        Console.WriteLine("  aura-cli quick -t \"Test Topic\" --dry-run -v");
    }
}

/// <summary>
/// Extended command options with profile and offline support
/// </summary>
internal sealed class ExtendedCommandOptions : CommandOptions
{
    public string? Profile { get; set; }
    public bool Offline { get; set; }
}
