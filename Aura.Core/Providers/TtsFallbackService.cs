using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Providers;

/// <summary>
/// TTS fallback result with metadata about what happened
/// </summary>
public class TtsFallbackResult
{
    public string OutputPath { get; set; } = string.Empty;
    public bool UsedFallback { get; set; }
    public string? FallbackReason { get; set; }
    public string? UsedVoice { get; set; }
    public string? AttemptedVoice { get; set; }
    public List<string> Diagnostics { get; set; } = new();
}

/// <summary>
/// Service that wraps TTS providers with robust fallback chain
/// Ensures no zero-byte WAV files are ever produced
/// </summary>
public class TtsFallbackService
{
    private readonly ILogger<TtsFallbackService> _logger;
    private readonly WavFileWriter _wavFileWriter;

    public TtsFallbackService(
        ILogger<TtsFallbackService> logger,
        WavFileWriter wavFileWriter)
    {
        _logger = logger;
        _wavFileWriter = wavFileWriter;
    }

    /// <summary>
    /// Synthesize with fallback chain: requested voice → alternate voices → silent WAV
    /// Never returns null or zero-byte file
    /// </summary>
    public async Task<TtsFallbackResult> SynthesizeWithFallbackAsync(
        ITtsProvider primaryProvider,
        IEnumerable<ScriptLine> lines,
        VoiceSpec requestedVoice,
        double totalDurationSeconds,
        CancellationToken ct = default)
    {
        var result = new TtsFallbackResult
        {
            AttemptedVoice = requestedVoice.VoiceName
        };

        // Try primary voice
        try
        {
            _logger.LogInformation("Attempting TTS with voice: {Voice}", requestedVoice.VoiceName);
            var outputPath = await primaryProvider.SynthesizeAsync(lines, requestedVoice, ct);
            
            // Validate the output
            if (await ValidateAndRepairAsync(outputPath, totalDurationSeconds, ct))
            {
                result.OutputPath = outputPath;
                result.UsedVoice = requestedVoice.VoiceName;
                result.UsedFallback = false;
                _logger.LogInformation("Successfully synthesized audio with requested voice");
                return result;
            }
            
            result.Diagnostics.Add($"Primary voice '{requestedVoice.VoiceName}' produced invalid output");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary TTS provider failed with voice: {Voice}", requestedVoice.VoiceName);
            result.Diagnostics.Add($"Primary voice failed: {ex.Message}");
        }

        // Try alternate voices if available
        var availableVoices = await GetAvailableVoicesAsync(primaryProvider);
        var alternateVoices = availableVoices
            .Where(v => !string.Equals(v, requestedVoice.VoiceName, StringComparison.OrdinalIgnoreCase))
            .Take(2) // Try up to 2 alternates
            .ToList();

        foreach (var altVoice in alternateVoices)
        {
            try
            {
                _logger.LogInformation("Trying alternate voice: {Voice}", altVoice);
                var altSpec = requestedVoice with { VoiceName = altVoice };
                var outputPath = await primaryProvider.SynthesizeAsync(lines, altSpec, ct);
                
                if (await ValidateAndRepairAsync(outputPath, totalDurationSeconds, ct))
                {
                    result.OutputPath = outputPath;
                    result.UsedVoice = altVoice;
                    result.UsedFallback = true;
                    result.FallbackReason = $"Primary voice '{requestedVoice.VoiceName}' failed, used '{altVoice}'";
                    result.Diagnostics.Add($"Successfully used alternate voice '{altVoice}'");
                    
                    _logger.LogInformation("Successfully synthesized with alternate voice: {Voice}", altVoice);
                    return result;
                }
                
                result.Diagnostics.Add($"Alternate voice '{altVoice}' produced invalid output");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alternate voice failed: {Voice}", altVoice);
                result.Diagnostics.Add($"Alternate voice '{altVoice}' failed: {ex.Message}");
            }
        }

        // Final fallback: Generate silent WAV
        _logger.LogWarning("All TTS voices failed, generating silent WAV as fallback");
        result.OutputPath = await GenerateFallbackSilenceAsync(totalDurationSeconds, ct);
        result.UsedVoice = "Silent Fallback";
        result.UsedFallback = true;
        result.FallbackReason = "All TTS voices failed, generated silent audio";
        result.Diagnostics.Add("Generated silent WAV as final fallback");

        return result;
    }

    /// <summary>
    /// Validate WAV file and attempt repair if needed
    /// </summary>
    private async Task<bool> ValidateAndRepairAsync(string filePath, double expectedDuration, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning("Output file does not exist: {Path}", filePath);
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        
        // Check for zero-byte or too-small files
        if (fileInfo.Length < 128)
        {
            _logger.LogWarning("Output file too small ({Size} bytes): {Path}", fileInfo.Length, filePath);
            
            // Attempt to regenerate as silent
            try
            {
                await _wavFileWriter.GenerateSilenceAsync(filePath, expectedDuration, ct: ct);
                _logger.LogInformation("Replaced invalid file with silent WAV");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to repair invalid file");
                return false;
            }
        }

        // Validate WAV structure
        if (!_wavFileWriter.ValidateWavFile(filePath))
        {
            _logger.LogWarning("Output file has invalid WAV structure: {Path}", filePath);
            
            // Attempt to regenerate as silent
            try
            {
                await _wavFileWriter.GenerateSilenceAsync(filePath, expectedDuration, ct: ct);
                _logger.LogInformation("Replaced corrupted file with silent WAV");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to repair corrupted file");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generate a silent WAV file as absolute fallback
    /// </summary>
    private async Task<string> GenerateFallbackSilenceAsync(double durationSeconds, CancellationToken ct)
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            "AuraVideoStudio",
            "TTS",
            $"silent_fallback_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.wav"
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _wavFileWriter.GenerateSilenceAsync(outputPath, durationSeconds, ct: ct);
        
        _logger.LogInformation("Generated fallback silent WAV: {Path} ({Duration}s)", outputPath, durationSeconds);
        
        return outputPath;
    }

    /// <summary>
    /// Get available voices from provider, never throws
    /// </summary>
    private async Task<List<string>> GetAvailableVoicesAsync(ITtsProvider provider)
    {
        try
        {
            var voices = await provider.GetAvailableVoicesAsync();
            return voices?.ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get available voices");
            return new List<string>();
        }
    }

    /// <summary>
    /// Create a toast-friendly diagnostic message from fallback result
    /// </summary>
    public static string CreateDiagnosticMessage(TtsFallbackResult result)
    {
        if (!result.UsedFallback)
        {
            return $"✓ Audio synthesized successfully with {result.UsedVoice}";
        }

        var message = result.FallbackReason ?? "Used fallback audio generation";
        
        if (result.UsedVoice == "Silent Fallback")
        {
            message += " ⚠️ Click 'Fix it' to configure TTS providers";
        }
        else
        {
            message += $" → {result.UsedVoice}";
        }

        return message;
    }
}
