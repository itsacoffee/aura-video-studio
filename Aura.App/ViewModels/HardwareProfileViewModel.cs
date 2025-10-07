using Aura.Core.Hardware;
using Aura.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Aura.App.ViewModels;

/// <summary>
/// ViewModel for the Hardware Profile view.
/// </summary>
public partial class HardwareProfileViewModel : ObservableObject
{
    private readonly ILogger<HardwareProfileViewModel> _logger;
    private readonly HardwareDetector _hardwareDetector;

    [ObservableProperty]
    private SystemProfile? _currentProfile;

    [ObservableProperty]
    private bool _isDetecting;

    [ObservableProperty]
    private string _statusMessage = "Hardware not detected yet";

    [ObservableProperty]
    private HardwareTier _detectedTier = HardwareTier.D;

    [ObservableProperty]
    private string _cpuInfo = "Unknown";

    [ObservableProperty]
    private int _ramGB;

    [ObservableProperty]
    private string _gpuInfo = "Unknown";

    [ObservableProperty]
    private bool _nvencAvailable;

    [ObservableProperty]
    private bool _sdAvailable;

    public HardwareProfileViewModel(
        ILogger<HardwareProfileViewModel> logger,
        HardwareDetector hardwareDetector)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
    }

    [RelayCommand]
    private async Task DetectHardwareAsync()
    {
        if (IsDetecting)
            return;

        try
        {
            IsDetecting = true;
            StatusMessage = "Detecting hardware...";

            var profile = await _hardwareDetector.DetectAsync();
            CurrentProfile = profile;

            // Update UI-friendly properties
            DetectedTier = profile.Tier;
            RamGB = profile.RamGB;
            CpuInfo = $"{profile.LogicalCores} cores";
            
            if (profile.Gpu != null)
            {
                GpuInfo = $"{profile.Gpu.Vendor} {profile.Gpu.Model} ({profile.Gpu.VramGB}GB)";
            }
            else
            {
                GpuInfo = "No dedicated GPU detected";
            }

            NvencAvailable = profile.EnableNVENC;
            SdAvailable = profile.EnableSD;

            StatusMessage = $"Hardware detected - Tier {profile.Tier}";
            _logger.LogInformation("Hardware detection completed: Tier {Tier}", profile.Tier);
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Detection failed: {ex.Message}";
            _logger.LogError(ex, "Hardware detection failed");
        }
        finally
        {
            IsDetecting = false;
        }
    }
}
