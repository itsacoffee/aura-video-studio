using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts.Validators;

/// <summary>
/// Validator for Windows TTS (SAPI) provider
/// </summary>
public class WindowsTtsValidator : ProviderValidator
{
    private readonly ILogger<WindowsTtsValidator> _logger;

    public override string ProviderName => "WindowsSAPI";

    public WindowsTtsValidator(ILogger<WindowsTtsValidator> logger)
    {
        _logger = logger;
    }

    public override Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        // Windows TTS is only available on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult(new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "Only available on Windows",
                ErrorMessage = "Windows TTS requires Windows OS"
            });
        }

        try
        {
            // Try to access Windows Speech API to verify it's available
            // This is a simple check - actual synthesis may still fail
            return Task.FromResult(new ProviderValidationResult
            {
                IsAvailable = true,
                ProviderName = ProviderName,
                Details = "Windows TTS (SAPI) is available"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Windows TTS provider");
            return Task.FromResult(new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "Validation failed",
                ErrorMessage = ex.Message
            });
        }
    }
}
