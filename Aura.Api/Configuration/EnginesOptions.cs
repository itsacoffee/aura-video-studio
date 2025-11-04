namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for external engines and their default ports.
/// </summary>
public sealed class EnginesOptions
{
    /// <summary>
    /// URL for the engines manifest file.
    /// </summary>
    public string ManifestUrl { get; set; } = string.Empty;

    /// <summary>
    /// Root directory for engine installations.
    /// </summary>
    public string InstallRoot { get; set; } = "%LOCALAPPDATA%/Aura/Tools";

    /// <summary>
    /// Default port mappings for various engines.
    /// </summary>
    public Dictionary<string, int> DefaultPorts { get; set; } = new()
    {
        ["stable-diffusion-webui"] = 7860,
        ["comfyui"] = 8188,
        ["piper"] = 0,
        ["mimic3"] = 59125
    };
}
