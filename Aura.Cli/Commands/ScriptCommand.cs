using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Models;
using Aura.Core.Providers;

namespace Aura.Cli.Commands;

/// <summary>
/// Script command - generates a video script from a brief
/// </summary>
public class ScriptCommand : ICommand
{
    private readonly ILogger<ScriptCommand> _logger;
    private readonly ILlmProvider _llmProvider;

    public ScriptCommand(ILogger<ScriptCommand> logger, ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args, out var briefFile, out var planFile, out var outputFile);

        if (string.IsNullOrEmpty(briefFile) || string.IsNullOrEmpty(planFile))
        {
            ShowHelp();
            return 1;
        }

        try
        {
            // Load brief
            if (!File.Exists(briefFile))
            {
                Console.WriteLine($"✗ Brief file not found: {briefFile}");
                return 1;
            }

            var briefJson = await File.ReadAllTextAsync(briefFile);
            var brief = JsonSerializer.Deserialize<Brief>(briefJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (brief == null)
            {
                Console.WriteLine($"✗ Failed to parse brief file");
                return 1;
            }

            // Load plan
            if (!File.Exists(planFile))
            {
                Console.WriteLine($"✗ Plan file not found: {planFile}");
                return 1;
            }

            var planJson = await File.ReadAllTextAsync(planFile);
            var plan = JsonSerializer.Deserialize<PlanSpec>(planJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (plan == null)
            {
                Console.WriteLine($"✗ Failed to parse plan file");
                return 1;
            }

            if (options.Verbose)
            {
                Console.WriteLine("=== Aura CLI Script Generation ===");
                Console.WriteLine($"Topic: {brief.Topic}");
                Console.WriteLine($"Duration: {plan.TargetDuration.TotalMinutes} minutes");
                Console.WriteLine($"Pacing: {plan.Pacing}");
                Console.WriteLine($"Density: {plan.Density}");
                Console.WriteLine();
            }

            if (options.DryRun)
            {
                Console.WriteLine("✓ Dry run complete (no script generated)");
                return 0;
            }

            // Generate script
            Console.WriteLine("Generating script...");
            var script = await _llmProvider.DraftScriptAsync(brief, plan, CancellationToken.None);

            // Save script
            var output = outputFile ?? "script.txt";
            await File.WriteAllTextAsync(output, script);

            Console.WriteLine($"✓ Script generated: {output}");
            Console.WriteLine($"  Length: {script.Length} characters");

            if (options.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Preview:");
                var preview = script.Length > 200 ? script.Substring(0, 200) + "..." : script;
                Console.WriteLine(preview);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script generation failed");
            Console.WriteLine($"✗ Error: {ex.Message}");
            return 1;
        }
    }

    private CommandOptions ParseOptions(string[] args, out string? briefFile, out string? planFile, out string? outputFile)
    {
        var options = new CommandOptions();
        briefFile = null;
        planFile = null;
        outputFile = null;

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
            else if ((arg == "--brief" || arg == "-b") && i + 1 < args.Length)
            {
                briefFile = args[++i];
            }
            else if ((arg == "--plan" || arg == "-p") && i + 1 < args.Length)
            {
                planFile = args[++i];
            }
            else if ((arg == "--output" || arg == "-o") && i + 1 < args.Length)
            {
                outputFile = args[++i];
            }
        }

        return options;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Usage: aura-cli script [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -b, --brief <file>     Path to brief JSON file (required)");
        Console.WriteLine("  -p, --plan <file>      Path to plan JSON file (required)");
        Console.WriteLine("  -o, --output <file>    Output file path (default: script.txt)");
        Console.WriteLine("  -v, --verbose          Enable verbose output");
        Console.WriteLine("  --dry-run              Validate inputs without generating");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  aura-cli script -b brief.json -p plan.json -o output.txt");
    }
}
