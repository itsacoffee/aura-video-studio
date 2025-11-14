using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Hardware;

/// <summary>
/// Helper class for generating and managing diagnostic information
/// </summary>
public class DiagnosticsHelper
{
    private readonly ILogger<DiagnosticsHelper> _logger;
    private readonly HardwareDetector _hardwareDetector;
    private readonly ProviderSettings _providerSettings;

    public DiagnosticsHelper(
        ILogger<DiagnosticsHelper> logger, 
        HardwareDetector hardwareDetector,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Generates a comprehensive diagnostics report
    /// </summary>
    public async Task<string> GenerateDiagnosticsReportAsync()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("=== Aura Video Studio Diagnostics Report ===");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // System Profile
        try
        {
            var profile = await _hardwareDetector.DetectSystemAsync();
            sb.AppendLine("--- System Profile ---");
            sb.AppendLine($"Tier: {profile.Tier}");
            sb.AppendLine($"CPU Cores: {profile.PhysicalCores} physical, {profile.LogicalCores} logical");
            sb.AppendLine($"RAM: {profile.RamGB} GB");
            
            if (profile.Gpu != null)
            {
                sb.AppendLine($"GPU: {profile.Gpu.Vendor} {profile.Gpu.Model}");
                sb.AppendLine($"VRAM: {profile.Gpu.VramGB} GB");
                sb.AppendLine($"Series: {profile.Gpu.Series ?? "N/A"}");
            }
            else
            {
                sb.AppendLine("GPU: Not detected");
            }
            
            sb.AppendLine($"NVENC Enabled: {profile.EnableNVENC}");
            sb.AppendLine($"Stable Diffusion Enabled: {profile.EnableSD}");
            sb.AppendLine($"Offline Mode: {profile.OfflineOnly}");
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Error detecting system: {ex.Message}");
            sb.AppendLine();
        }

        // Environment Info
        sb.AppendLine("--- Environment ---");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"Platform: {Environment.OSVersion.Platform}");
        sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
        sb.AppendLine($".NET Version: {Environment.Version}");
        sb.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
        sb.AppendLine();

        // Recent Logs (last 50 lines from latest log file)
        try
        {
            sb.AppendLine("--- Recent Logs (Last 50 lines) ---");
            var logsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "logs");
            
            if (Directory.Exists(logsDirectory))
            {
                var logFiles = Directory.GetFiles(logsDirectory, "aura-api-*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (logFiles.Count != 0)
                {
                    var latestLog = logFiles[0];
                    var lines = File.ReadAllLines(latestLog);
                    var recentLines = lines.TakeLast(50);
                    
                    foreach (var line in recentLines)
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    sb.AppendLine("No log files found");
                }
            }
            else
            {
                sb.AppendLine("Logs directory not found");
            }
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Error reading logs: {ex.Message}");
            sb.AppendLine();
        }

        sb.AppendLine("=== End of Diagnostics Report ===");
        
        return sb.ToString();
    }

    /// <summary>
    /// Generates a JSON diagnostics report for API consumption
    /// </summary>
    public async Task<object> GenerateDiagnosticsJsonAsync()
    {
        SystemProfile? profile = null;
        Exception? profileError = null;

        try
        {
            profile = await _hardwareDetector.DetectSystemAsync();
        }
        catch (Exception ex)
        {
            profileError = ex;
        }

        // Load user settings to check advanced mode
        bool advancedModeEnabled = false;
        try
        {
            var auraDataDir = _providerSettings.GetAuraDataDirectory();
            var userSettingsPath = Path.Combine(auraDataDir, "user-settings.json");
            
            if (File.Exists(userSettingsPath))
            {
                var json = await File.ReadAllTextAsync(userSettingsPath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                advancedModeEnabled = settings?.General?.AdvancedModeEnabled ?? false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read advanced mode setting");
        }

        var diagnostics = new
        {
            timestamp = DateTime.UtcNow,
            advancedMode = advancedModeEnabled,
            advancedFeaturesNote = advancedModeEnabled 
                ? "Advanced features are enabled" 
                : "Advanced features are disabled. Enable Advanced Mode in Settings > General to access expert features.",
            systemProfile = profile != null ? new
            {
                tier = profile.Tier.ToString(),
                cpu = new
                {
                    physical = profile.PhysicalCores,
                    logical = profile.LogicalCores
                },
                ram = new { gb = profile.RamGB },
                gpu = profile.Gpu != null ? new
                {
                    vendor = profile.Gpu.Vendor,
                    model = profile.Gpu.Model,
                    vramGB = profile.Gpu.VramGB,
                    series = profile.Gpu.Series
                } : null,
                enableNVENC = profile.EnableNVENC,
                enableSD = profile.EnableSD,
                offlineOnly = profile.OfflineOnly
            } : null,
            profileError = profileError?.Message,
            environment = new
            {
                os = Environment.OSVersion.ToString(),
                platform = Environment.OSVersion.Platform.ToString(),
                is64BitOS = Environment.Is64BitOperatingSystem,
                is64BitProcess = Environment.Is64BitProcess,
                dotnetVersion = Environment.Version.ToString(),
                workingDirectory = Environment.CurrentDirectory
            },
            logsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "logs")
        };

        return diagnostics;
    }
}
