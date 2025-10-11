using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Cli.Commands;

/// <summary>
/// Compose command - creates render plan from assets (visuals, audio, timeline)
/// Validates inputs and generates a preview of the final composition
/// </summary>
public class ComposeCommand : ICommand
{
    private readonly ILogger<ComposeCommand> _logger;

    public ComposeCommand(ILogger<ComposeCommand> logger)
    {
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args, out var inputPath, out var outputPath);

        if (string.IsNullOrEmpty(inputPath))
        {
            ShowHelp();
            return ExitCodes.InvalidArguments;
        }

        try
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         AURA CLI - Compose Timeline Preview             ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Read input file
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"✗ Error: Input file not found: {inputPath}");
                return ExitCodes.InvalidArguments;
            }

            Console.WriteLine($"[1/3] Reading input from: {inputPath}");
            var inputJson = await File.ReadAllTextAsync(inputPath);
            
            // Validate JSON structure
            try
            {
                using var jsonDoc = JsonDocument.Parse(inputJson);
                Console.WriteLine("      ✓ Input JSON is valid");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"✗ Error: Invalid JSON: {ex.Message}");
                return ExitCodes.InvalidArguments;
            }

            // Create composition plan
            Console.WriteLine();
            Console.WriteLine("[2/3] Creating composition plan...");
            
            var plan = new
            {
                timeline = inputJson,
                assets = new
                {
                    visuals = "Stock images or Stable Diffusion outputs",
                    audio = "TTS narration + background music",
                    overlays = "Text captions, transitions"
                },
                renderSettings = new
                {
                    resolution = "1920x1080",
                    fps = 30,
                    codec = "H.264",
                    quality = 75
                }
            };

            Console.WriteLine("      ✓ Composition plan created");
            if (options.Verbose)
            {
                Console.WriteLine("      Resolution: 1920x1080");
                Console.WriteLine("      Frame Rate: 30 fps");
                Console.WriteLine("      Codec: H.264");
            }

            // Save output
            Console.WriteLine();
            Console.WriteLine("[3/3] Saving composition plan...");
            
            var output = outputPath ?? Path.Combine(Path.GetDirectoryName(inputPath) ?? ".", "compose-plan.json");
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
            
            Console.WriteLine($"      ✓ Plan saved to: {output}");
            Console.WriteLine();

            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("✓ Composition plan ready!");
            Console.WriteLine();
            Console.WriteLine("To complete rendering:");
            Console.WriteLine("  - Review the composition plan");
            Console.WriteLine("  - Run 'aura-cli render' to produce the final video");
            Console.WriteLine();

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compose command failed");
            Console.WriteLine($"✗ Error: {ex.Message}");
            if (options.Verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return ExitCodes.UnexpectedError;
        }
    }

    private CommandOptions ParseOptions(string[] args, out string? inputPath, out string? outputPath)
    {
        var options = new CommandOptions();
        inputPath = null;
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
            else if ((arg == "--input" || arg == "-i") && i + 1 < args.Length)
            {
                inputPath = args[++i];
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
        Console.WriteLine("Usage: aura-cli compose [options]");
        Console.WriteLine();
        Console.WriteLine("Create a composition plan from assets and timeline");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input <file>     Input timeline JSON file (required)");
        Console.WriteLine("  -o, --output <file>    Output plan JSON file (default: compose-plan.json)");
        Console.WriteLine("  -v, --verbose          Enable verbose output");
        Console.WriteLine("  -h, --help             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli compose -i timeline.json");
        Console.WriteLine("  aura-cli compose -i timeline.json -o plan.json -v");
    }
}
