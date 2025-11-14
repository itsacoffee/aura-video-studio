using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Hardware;

namespace Aura.Cli.Commands;

/// <summary>
/// Preflight command - validates system configuration and dependencies
/// </summary>
public class PreflightCommand : ICommand
{
    private readonly ILogger<PreflightCommand> _logger;
    private readonly HardwareDetector _hardwareDetector;

    public PreflightCommand(ILogger<PreflightCommand> logger, HardwareDetector hardwareDetector)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args);

        if (options.Verbose)
        {
            Console.WriteLine("=== Aura CLI Preflight Check ===");
            Console.WriteLine();
        }

        try
        {
            // 1. Hardware Detection
            Console.WriteLine("Checking hardware capabilities...");
            var profile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);

            if (options.Verbose)
            {
                Console.WriteLine($"  CPU: {profile.LogicalCores} threads ({profile.PhysicalCores} cores)");
                Console.WriteLine($"  RAM: {profile.RamGB} GB");
                
                if (profile.Gpu != null)
                {
                    Console.WriteLine($"  GPU: {profile.Gpu.Vendor} {profile.Gpu.Model}");
                    Console.WriteLine($"  VRAM: {profile.Gpu.VramGB} GB");
                }
                else
                {
                    Console.WriteLine("  GPU: Not detected");
                }
                
                Console.WriteLine($"  Hardware Tier: {profile.Tier}");
                Console.WriteLine($"  NVENC Available: {profile.EnableNVENC}");
                Console.WriteLine($"  Stable Diffusion Available: {profile.EnableSD}");
            }

            Console.WriteLine($"✓ Hardware detected: Tier {profile.Tier}");
            Console.WriteLine();

            // 2. Check FFmpeg
            Console.WriteLine("Checking dependencies...");
            
            var ffmpegAvailable = CheckFFmpeg();
            if (ffmpegAvailable)
            {
                Console.WriteLine("✓ FFmpeg found");
            }
            else
            {
                Console.WriteLine("⚠ FFmpeg not found in PATH");
                Console.WriteLine("  Install from: https://ffmpeg.org/download.html");
            }

            Console.WriteLine();

            // 3. Provider Status
            Console.WriteLine("Provider availability:");
            Console.WriteLine($"  ✓ Rule-based Script Generator (free, offline)");
            Console.WriteLine($"  ✓ Pexels/Pixabay Images (free, requires internet)");
            
            if (profile.Gpu != null && profile.Gpu.Vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  ✓ Local Stable Diffusion (NVIDIA GPU detected)");
            }
            else
            {
                Console.WriteLine($"  ⚠ Local Stable Diffusion (requires NVIDIA GPU)");
            }

            if (OperatingSystem.IsWindows())
            {
                Console.WriteLine($"  ✓ Windows TTS (free, offline)");
            }
            else
            {
                Console.WriteLine($"  ⚠ Windows TTS (requires Windows)");
            }

            Console.WriteLine();
            Console.WriteLine("✓ Preflight check complete");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preflight check failed");
            Console.WriteLine($"✗ Error: {ex.Message}");
            return 1;
        }
    }

    private bool CheckFFmpeg()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            return process != null;
        }
        catch
        {
            return false;
        }
    }

    private CommandOptions ParseOptions(string[] args)
    {
        var options = new CommandOptions();

        foreach (var arg in args)
        {
            if (arg == "--verbose" || arg == "-v")
            {
                options.Verbose = true;
            }
        }

        return options;
    }
}
