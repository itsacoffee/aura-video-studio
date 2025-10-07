using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Settings view.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string _providerMode = "Free";

    [ObservableProperty]
    private string _llmProvider = "RuleBased";

    [ObservableProperty]
    private string _ttsProvider = "Windows";

    [ObservableProperty]
    private string _ffmpegPath = "scripts/ffmpeg/ffmpeg.exe";

    [ObservableProperty]
    private bool _offlineMode;

    [ObservableProperty]
    private string _cachePath = string.Empty;

    [ObservableProperty]
    private string _projectsPath = string.Empty;

    public SettingsViewModel(ILogger<SettingsViewModel> logger)
    {
        _logger = logger;
    }
}
