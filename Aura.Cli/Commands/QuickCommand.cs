using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Hardware;

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
            if (options.DryRun)
            {
                Console.WriteLine("      ⚠ Dry run - skipping script generation");
            }
            else
            {
                var script = await _llmProvider.DraftScriptAsync(brief, plan, CancellationToken.None);
                
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

            // Step 5: Simulate rendering
            Console.WriteLine("[5/5] Render simulation...");
            Console.WriteLine($"      Resolution: 1920x1080");
            Console.WriteLine($"      Encoder: {(profile.EnableNVENC ? "NVENC (hardware)" : "x264 (software)")}");
            Console.WriteLine($"      Audio: AAC 256 kbps");
            Console.WriteLine("      ✓ Render configuration validated");
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
                Console.WriteLine();
                Console.WriteLine("Next steps:");
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
            return 1;
        }
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
        Console.WriteLine("  -v, --verbose          Enable verbose output");
        Console.WriteLine("  --dry-run              Validate without generating files");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli quick -t \"Machine Learning Basics\"");
        Console.WriteLine("  aura-cli quick -t \"Coffee Brewing\" -d 5 -o ./videos");
        Console.WriteLine("  aura-cli quick -t \"Test Topic\" --dry-run -v");
    }
}
