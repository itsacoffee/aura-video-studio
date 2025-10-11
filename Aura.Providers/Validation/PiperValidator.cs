using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Validates Piper TTS installation and functionality
/// </summary>
public class PiperValidator : IProviderValidator
{
    private readonly ILogger<PiperValidator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Piper";

    public PiperValidator(ILogger<PiperValidator> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Piper is a CLI tool, so we check if it's installed and can be executed
            // The executable path should be provided via configUrl or found in PATH
            var piperPath = configUrl ?? "piper";

            // Check if piper executable exists
            if (!string.IsNullOrEmpty(configUrl) && !File.Exists(configUrl))
            {
                sw.Stop();
                _logger.LogWarning("Piper validation failed: Executable not found at {Path}", configUrl);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"Piper executable not found at {configUrl}",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            // Try to run piper --version to verify it's installed
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var startInfo = new ProcessStartInfo
            {
                FileName = piperPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    sw.Stop();
                    return new ProviderValidationResult
                    {
                        Name = ProviderName,
                        Ok = false,
                        Details = "Failed to start Piper process",
                        ElapsedMs = sw.ElapsedMilliseconds
                    };
                }

                await process.WaitForExitAsync(cts.Token);
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                sw.Stop();

                if (process.ExitCode == 0 || !string.IsNullOrEmpty(output))
                {
                    _logger.LogInformation("Piper validation successful");
                    return new ProviderValidationResult
                    {
                        Name = ProviderName,
                        Ok = true,
                        Details = "Piper TTS is installed and ready",
                        ElapsedMs = sw.ElapsedMilliseconds
                    };
                }
                else
                {
                    return new ProviderValidationResult
                    {
                        Name = ProviderName,
                        Ok = false,
                        Details = $"Piper exited with code {process.ExitCode}",
                        ElapsedMs = sw.ElapsedMilliseconds
                    };
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Process not found
                sw.Stop();
                _logger.LogWarning("Piper validation failed: Executable not found");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Piper not installed or not in PATH. Install from Downloads page.",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Piper validation timed out");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = "Validation timed out",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Piper validation failed: Unexpected error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        await Task.CompletedTask;
        return new ProviderValidationResult
        {
            Name = ProviderName,
            Ok = false,
            Details = "Validation incomplete",
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }
}
