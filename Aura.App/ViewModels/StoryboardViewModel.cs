using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Storyboard/Timeline editor view.
/// </summary>
public partial class StoryboardViewModel : ObservableObject
{
    private readonly ILogger<StoryboardViewModel> _logger;

    [ObservableProperty]
    private string _projectName = "Untitled Project";

    [ObservableProperty]
    private bool _isProjectLoaded;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _playheadPosition;

    public StoryboardViewModel(ILogger<StoryboardViewModel> logger)
    {
        _logger = logger;
    }
}
