using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Runtime.InteropServices;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IHardwareDetector _hardwareDetector;
    private readonly IKeyStore _keyStore;

    public ProvidersController(IHardwareDetector hardwareDetector, IKeyStore keyStore)
    {
        _hardwareDetector = hardwareDetector;
        _keyStore = keyStore;
    }

    /// <summary>
    /// Get provider capabilities based on hardware, API keys, and OS detection
    /// </summary>
    [HttpGet("capabilities")]
    public async Task<IActionResult> GetCapabilities()
    {
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync();
            var capabilities = new List<ProviderCapability>();

            // Stable Diffusion provider
            var sdCapability = GetStableDiffusionCapability(systemProfile);
            capabilities.Add(sdCapability);

            return Ok(capabilities);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error detecting provider capabilities");
            return Problem("Error detecting provider capabilities", statusCode: 500);
        }
    }

    private ProviderCapability GetStableDiffusionCapability(Aura.Core.Models.SystemProfile systemProfile)
    {
        var available = true;
        var reasonCodes = new List<string>();
        
        // Check for NVIDIA GPU
        var hasNvidiaGpu = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "NVIDIA";
        if (!hasNvidiaGpu)
        {
            available = false;
            reasonCodes.Add("RequiresNvidiaGPU");
        }

        // Check for STABLE_KEY API key
        var stableKey = _keyStore.GetKey("STABLE_KEY") ?? _keyStore.GetKey("stabilityai");
        if (string.IsNullOrWhiteSpace(stableKey))
        {
            available = false;
            reasonCodes.Add("MissingApiKey:STABLE_KEY");
        }

        // Check VRAM requirement (6GB minimum)
        if (systemProfile.Gpu?.VramGB < 6)
        {
            available = false;
            reasonCodes.Add("InsufficientVRAM");
        }

        // Check OS (Windows or Linux)
        var isWindowsOrLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                               RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (!isWindowsOrLinux)
        {
            available = false;
            reasonCodes.Add("UnsupportedOS");
        }

        return new ProviderCapability
        {
            Name = "StableDiffusion",
            Available = available,
            ReasonCodes = reasonCodes.ToArray(),
            Requirements = new ProviderRequirements
            {
                NeedsKey = new[] { "STABLE_KEY" },
                NeedsGPU = "nvidia",
                MinVRAMMB = 6144,
                Os = new[] { "windows", "linux" }
            }
        };
    }
}

/// <summary>
/// Represents a provider's capability and availability status
/// </summary>
public class ProviderCapability
{
    public string Name { get; set; } = string.Empty;
    public bool Available { get; set; }
    public string[] ReasonCodes { get; set; } = Array.Empty<string>();
    public ProviderRequirements Requirements { get; set; } = new();
}

/// <summary>
/// Represents requirements for a provider
/// </summary>
public class ProviderRequirements
{
    public string[] NeedsKey { get; set; } = Array.Empty<string>();
    public string? NeedsGPU { get; set; }
    public int? MinVRAMMB { get; set; }
    public string[] Os { get; set; } = Array.Empty<string>();
}
