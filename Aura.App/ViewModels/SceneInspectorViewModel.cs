using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Scene Inspector - allows per-scene visual overrides.
/// </summary>
public partial class SceneInspectorViewModel : ObservableObject
{
    private readonly ILogger<SceneInspectorViewModel> _logger;

    [ObservableProperty]
    private int _sceneIndex;

    [ObservableProperty]
    private string _sceneHeading = string.Empty;

    [ObservableProperty]
    private string _sceneScript = string.Empty;

    // Per-scene visual override flags
    [ObservableProperty]
    private bool _overrideVisuals;

    [ObservableProperty]
    private string? _overrideProvider; // "stock", "sd", "local", null = use default

    // Stock provider overrides
    [ObservableProperty]
    private string? _overrideSearchQuery;

    [ObservableProperty]
    private int _overrideAssetCount = 1;

    // Stable Diffusion overrides
    [ObservableProperty]
    private string? _overrideSdPrompt;

    [ObservableProperty]
    private int? _overrideSdSteps;

    [ObservableProperty]
    private double? _overrideSdCfgScale;

    [ObservableProperty]
    private int? _overrideSdSeed;

    [ObservableProperty]
    private string? _overrideSdStyle;

    [ObservableProperty]
    private int? _overrideSdWidth;

    [ObservableProperty]
    private int? _overrideSdHeight;

    [ObservableProperty]
    private string? _overrideSdModel;

    [ObservableProperty]
    private string? _overrideSdSampler;

    public SceneInspectorViewModel(ILogger<SceneInspectorViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load scene data into the inspector
    /// </summary>
    public void LoadScene(int index, string heading, string script)
    {
        SceneIndex = index;
        SceneHeading = heading;
        SceneScript = script;
    }

    /// <summary>
    /// Clear all per-scene overrides
    /// </summary>
    public void ClearOverrides()
    {
        OverrideVisuals = false;
        OverrideProvider = null;
        OverrideSearchQuery = null;
        OverrideSdPrompt = null;
        OverrideSdSteps = null;
        OverrideSdCfgScale = null;
        OverrideSdSeed = null;
        OverrideSdStyle = null;
        OverrideSdWidth = null;
        OverrideSdHeight = null;
        OverrideSdModel = null;
        OverrideSdSampler = null;
    }
}
