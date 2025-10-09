using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Create/Brief wizard view.
/// </summary>
public partial class CreateViewModel : ObservableObject
{
    private readonly ILogger<CreateViewModel> _logger;
    private readonly VideoOrchestrator _orchestrator;

    [ObservableProperty]
    private string _topic = string.Empty;

    [ObservableProperty]
    private string? _audience;

    [ObservableProperty]
    private string? _goal;

    [ObservableProperty]
    private string _tone = "Informative";

    [ObservableProperty]
    private string _language = "en-US";

    [ObservableProperty]
    private Aspect _aspect = Aspect.Widescreen16x9;

    [ObservableProperty]
    private int _durationMinutes = 6;

    [ObservableProperty]
    private Pacing _pacing = Pacing.Conversational;

    [ObservableProperty]
    private Density _density = Density.Balanced;

    [ObservableProperty]
    private string _style = "Educational";

    [ObservableProperty]
    private string _voiceName = "Microsoft David Desktop";

    [ObservableProperty]
    private double _voiceRate = 1.0;

    [ObservableProperty]
    private double _voicePitch = 0.0;

    [ObservableProperty]
    private PauseStyle _pauseStyle = PauseStyle.Natural;

    // Visual/Assets settings
    [ObservableProperty]
    private string _visualMode = "StockOrLocal"; // Free, StockOrLocal, Pro

    [ObservableProperty]
    private bool _enablePexels = true;

    [ObservableProperty]
    private bool _enablePixabay = true;

    [ObservableProperty]
    private bool _enableUnsplash = true;

    [ObservableProperty]
    private bool _enableLocalAssets = true;

    [ObservableProperty]
    private bool _enableStableDiffusion = false;

    [ObservableProperty]
    private string? _pexelsApiKey;

    [ObservableProperty]
    private string? _pixabayApiKey;

    [ObservableProperty]
    private string? _unsplashApiKey;

    [ObservableProperty]
    private string? _localAssetsDirectory;

    [ObservableProperty]
    private string? _stableDiffusionUrl = "http://127.0.0.1:7860";

    // Stable Diffusion parameters
    [ObservableProperty]
    private string? _sdModel; // null = auto-detect

    [ObservableProperty]
    private int _sdSteps = 20;

    [ObservableProperty]
    private double _sdCfgScale = 7.0;

    [ObservableProperty]
    private int _sdSeed = -1;

    [ObservableProperty]
    private int _sdWidth = 1024;

    [ObservableProperty]
    private int _sdHeight = 576;

    [ObservableProperty]
    private string _sdStyle = "high quality, detailed, professional";

    [ObservableProperty]
    private string _sdSamplerName = "DPM++ 2M Karras";

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _statusMessage = "Ready to generate";

    [ObservableProperty]
    private double _progressPercentage;

    public CreateViewModel(ILogger<CreateViewModel> logger, VideoOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;
    }

    [RelayCommand]
    private async Task GenerateVideoAsync(CancellationToken cancellationToken)
    {
        if (IsGenerating)
            return;

        try
        {
            IsGenerating = true;
            ProgressPercentage = 0;
            StatusMessage = "Starting video generation...";

            var brief = new Brief(
                Topic: Topic,
                Audience: Audience,
                Goal: Goal,
                Tone: Tone,
                Language: Language,
                Aspect: Aspect
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(DurationMinutes),
                Pacing: Pacing,
                Density: Density,
                Style: Style
            );

            var voiceSpec = new VoiceSpec(
                VoiceName: VoiceName,
                Rate: VoiceRate,
                Pitch: VoicePitch,
                Pause: PauseStyle
            );

            var renderSpec = new RenderSpec(
                Res: new Resolution(1920, 1080),
                Container: "mp4",
                VideoBitrateK: 12000,
                AudioBitrateK: 256
            );

            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                _logger.LogInformation("Progress: {Message}", message);
            });

            string outputPath = await _orchestrator.GenerateVideoAsync(
                brief,
                planSpec,
                voiceSpec,
                renderSpec,
                progress,
                cancellationToken
            );

            StatusMessage = $"Video generated successfully: {outputPath}";
            ProgressPercentage = 100;

            _logger.LogInformation("Video generation completed: {OutputPath}", outputPath);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Video generation cancelled";
            _logger.LogInformation("Video generation was cancelled");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error generating video");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        Topic = string.Empty;
        Audience = null;
        Goal = null;
        Tone = "Informative";
        Language = "en-US";
        Aspect = Aspect.Widescreen16x9;
        DurationMinutes = 6;
        Pacing = Pacing.Conversational;
        Density = Density.Balanced;
        Style = "Educational";
        VoiceName = "Microsoft David Desktop";
        VoiceRate = 1.0;
        VoicePitch = 0.0;
        PauseStyle = PauseStyle.Natural;
        StatusMessage = "Ready to generate";
        ProgressPercentage = 0;
    }
}
