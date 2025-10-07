using System;
using Aura.Core.Models;
using Aura.Core.Rendering;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Render/Export view.
/// </summary>
public partial class RenderViewModel : ObservableObject
{
    private readonly ILogger<RenderViewModel> _logger;

    [ObservableProperty]
    private string _selectedPreset = "YouTube 1080p";

    [ObservableProperty]
    private Resolution _resolution = new Resolution(1920, 1080);

    [ObservableProperty]
    private string _container = "mp4";

    [ObservableProperty]
    private int _videoBitrateK = 12000;

    [ObservableProperty]
    private int _audioBitrateK = 256;

    [ObservableProperty]
    private bool _isRendering;

    [ObservableProperty]
    private double _renderProgress;

    [ObservableProperty]
    private string _statusMessage = "Ready to render";

    [ObservableProperty]
    private TimeSpan _estimatedTimeRemaining;

    public RenderViewModel(ILogger<RenderViewModel> logger)
    {
        _logger = logger;
    }

    partial void OnSelectedPresetChanged(string value)
    {
        var preset = RenderPresets.GetPresetByName(value);
        if (preset != null)
        {
            Resolution = preset.Res;
            Container = preset.Container;
            VideoBitrateK = preset.VideoBitrateK;
            AudioBitrateK = preset.AudioBitrateK;
        }
    }
}
