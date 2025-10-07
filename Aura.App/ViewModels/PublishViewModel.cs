using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Publish view (YouTube metadata and upload).
/// </summary>
public partial class PublishViewModel : ObservableObject
{
    private readonly ILogger<PublishViewModel> _logger;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private string _privacyStatus = "private";

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private double _uploadProgress;

    [ObservableProperty]
    private string _statusMessage = "Ready to publish";

    public PublishViewModel(ILogger<PublishViewModel> logger)
    {
        _logger = logger;
    }
}
