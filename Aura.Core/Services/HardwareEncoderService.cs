using System.Threading.Tasks;
using Aura.Core.Services.Render;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for detecting and configuring hardware video encoders
/// </summary>
public interface IHardwareEncoderService
{
    /// <summary>
    /// Detects available hardware encoding capabilities
    /// </summary>
    Task<HardwareCapabilities> DetectCapabilitiesAsync();
    
    /// <summary>
    /// Gets the best encoder configuration for the given preset
    /// </summary>
    Task<EncoderConfig> GetBestEncoderAsync(Models.Export.ExportPreset preset, bool preferHardware = true);
}

/// <summary>
/// Implementation of hardware encoder service
/// </summary>
public class HardwareEncoderService : IHardwareEncoderService
{
    private readonly HardwareEncoder _hardwareEncoder;
    private readonly ILogger<HardwareEncoderService> _logger;

    public HardwareEncoderService(
        HardwareEncoder hardwareEncoder,
        ILogger<HardwareEncoderService> logger)
    {
        _hardwareEncoder = hardwareEncoder;
        _logger = logger;
    }

    public async Task<HardwareCapabilities> DetectCapabilitiesAsync()
    {
        _logger.LogInformation("Detecting hardware encoding capabilities");
        return await _hardwareEncoder.DetectHardwareCapabilitiesAsync();
    }

    public async Task<EncoderConfig> GetBestEncoderAsync(
        Models.Export.ExportPreset preset, 
        bool preferHardware = true)
    {
        _logger.LogInformation(
            "Selecting best encoder for preset {Preset} (preferHardware={PreferHardware})", 
            preset.Name, 
            preferHardware);
        
        return await _hardwareEncoder.SelectBestEncoderAsync(preset, preferHardware);
    }
}
