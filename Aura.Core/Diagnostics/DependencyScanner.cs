using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Diagnostics;

/// <summary>
/// Progress event for dependency scanning
/// </summary>
public class ScanProgress
{
    public string Event { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public DependencyIssue? Issue { get; set; }
}

/// <summary>
/// Orchestrates comprehensive dependency and system scanning
/// </summary>
public class DependencyScanner
{
    private readonly ILogger<DependencyScanner> _logger;
    private readonly FfmpegLocator? _ffmpegLocator;
    private readonly ProviderSettings _providerSettings;
    private readonly IKeyStore _keyStore;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly string _workspacePath;
    
    public DependencyScanner(
        ILogger<DependencyScanner> logger,
        ProviderSettings providerSettings,
        IKeyStore keyStore,
        FfmpegLocator? ffmpegLocator = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _providerSettings = providerSettings;
        _keyStore = keyStore;
        _httpClientFactory = httpClientFactory;
        
        _workspacePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura");
    }
    
    /// <summary>
    /// Perform comprehensive dependency scan
    /// </summary>
    public async Task<DependencyScanResult> ScanAsync(
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("Starting dependency scan, CorrelationId: {CorrelationId}", correlationId);
        
        var result = new DependencyScanResult
        {
            ScanTime = startTime,
            Success = true,
            CorrelationId = correlationId
        };
        
        try
        {
            ReportProgress(progress, "started", "Starting system scan", 0);
            
            // Step 1: Collect system information
            ReportProgress(progress, "step", "Collecting system information", 10);
            result.SystemInfo = await CollectSystemInfoAsync(ct);
            
            // Step 2: Check FFmpeg
            ReportProgress(progress, "step", "Checking FFmpeg installation", 25);
            await CheckFfmpegAsync(result.Issues, ct);
            
            // Step 3: Check disk space
            ReportProgress(progress, "step", "Checking disk space", 40);
            CheckDiskSpace(result.Issues);
            
            // Step 4: Check write permissions
            ReportProgress(progress, "step", "Checking write permissions", 55);
            CheckWritePermissions(result.Issues);
            
            // Step 5: Check network connectivity
            ReportProgress(progress, "step", "Checking network connectivity", 70);
            await CheckNetworkConnectivityAsync(result.Issues, ct);
            
            // Step 6: Check provider availability
            ReportProgress(progress, "step", "Checking provider availability", 85);
            await CheckProviderAvailabilityAsync(result.Issues, ct);
            
            ReportProgress(progress, "completed", "Scan completed", 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dependency scan");
            result.Success = false;
            result.Issues.Add(new DependencyIssue
            {
                Id = "scan-error",
                Category = IssueCategory.System,
                Severity = IssueSeverity.Error,
                Title = "Scan Error",
                Description = $"An error occurred during the dependency scan: {ex.Message}",
                Remediation = "Please try again. If the problem persists, contact support."
            });
        }
        finally
        {
            result.Duration = DateTime.UtcNow - startTime;
            
            // Report each issue as a separate progress event
            foreach (var issue in result.Issues)
            {
                ReportProgress(progress, "issue", issue.Title, null, issue);
            }
        }
        
        return result;
    }
    
    private async Task<SystemInfo> CollectSystemInfoAsync(CancellationToken ct)
    {
        var sysInfo = new SystemInfo
        {
            Platform = GetPlatformName(),
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            OsVersion = RuntimeInformation.OSDescription,
            CpuCores = Environment.ProcessorCount,
            TotalMemoryMb = GetTotalMemoryMb()
        };
        
        // Attempt to get GPU info (best effort)
        try
        {
            sysInfo.Gpu = await GetGpuInfoAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect GPU information");
        }
        
        return sysInfo;
    }
    
    private async Task CheckFfmpegAsync(List<DependencyIssue> issues, CancellationToken ct)
    {
        if (_ffmpegLocator == null)
        {
            issues.Add(new DependencyIssue
            {
                Id = "ffmpeg-locator-unavailable",
                Category = IssueCategory.FFmpeg,
                Severity = IssueSeverity.Warning,
                Title = "FFmpeg Locator Unavailable",
                Description = "FFmpeg locator service is not available for automatic detection.",
                Remediation = "Install FFmpeg manually and configure it in Settings."
            });
            return;
        }
        
        try
        {
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct);
            
            if (!result.Found || string.IsNullOrEmpty(result.FfmpegPath))
            {
                issues.Add(new DependencyIssue
                {
                    Id = "ffmpeg-missing",
                    Category = IssueCategory.FFmpeg,
                    Severity = IssueSeverity.Error,
                    Title = "FFmpeg Not Found",
                    Description = "FFmpeg is required for video rendering but was not found on your system.",
                    Remediation = "Install FFmpeg using the 'Install Managed FFmpeg' button or attach an existing installation.",
                    ActionId = "install-ffmpeg",
                    DocsUrl = "https://docs.aura.studio/dependencies/ffmpeg",
                    Metadata = new Dictionary<string, object>
                    {
                        ["reason"] = result.Reason ?? "Not found in PATH or common locations"
                    }
                });
            }
            else
            {
                // Check FFmpeg version
                var version = result.VersionString;
                if (!string.IsNullOrEmpty(version))
                {
                    // Version check - FFmpeg 4.0 or higher required
                    if (!IsVersionSufficient(version, 4, 0))
                    {
                        issues.Add(new DependencyIssue
                        {
                            Id = "ffmpeg-version-old",
                            Category = IssueCategory.FFmpeg,
                            Severity = IssueSeverity.Warning,
                            Title = "FFmpeg Version Outdated",
                            Description = $"FFmpeg version {version} detected. Version 4.0 or higher is recommended.",
                            Remediation = "Update FFmpeg to the latest version for best compatibility.",
                            ActionId = "update-ffmpeg",
                            DocsUrl = "https://docs.aura.studio/dependencies/ffmpeg",
                            Metadata = new Dictionary<string, object>
                            {
                                ["currentVersion"] = version,
                                ["path"] = result.FfmpegPath
                            }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg");
            issues.Add(new DependencyIssue
            {
                Id = "ffmpeg-check-error",
                Category = IssueCategory.FFmpeg,
                Severity = IssueSeverity.Error,
                Title = "FFmpeg Check Failed",
                Description = $"Failed to check FFmpeg: {ex.Message}",
                Remediation = "Ensure FFmpeg is properly installed and try again."
            });
        }
    }
    
    private void CheckDiskSpace(List<DependencyIssue> issues)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(_workspacePath) ?? "C:\\");
            var availableGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            
            if (availableGb < 5.0)
            {
                issues.Add(new DependencyIssue
                {
                    Id = "disk-space-critical",
                    Category = IssueCategory.Storage,
                    Severity = IssueSeverity.Error,
                    Title = "Low Disk Space",
                    Description = $"Only {availableGb:F1} GB available on {drive.Name}. At least 5 GB is required for video rendering.",
                    Remediation = "Free up disk space or change the workspace location in Settings.",
                    RelatedSettingKey = "workspacePath",
                    Metadata = new Dictionary<string, object>
                    {
                        ["availableGb"] = availableGb,
                        ["drive"] = drive.Name
                    }
                });
            }
            else if (availableGb < 10.0)
            {
                issues.Add(new DependencyIssue
                {
                    Id = "disk-space-low",
                    Category = IssueCategory.Storage,
                    Severity = IssueSeverity.Warning,
                    Title = "Low Disk Space Warning",
                    Description = $"Only {availableGb:F1} GB available on {drive.Name}. Consider freeing up space.",
                    Remediation = "At least 10 GB free space is recommended for optimal performance.",
                    RelatedSettingKey = "workspacePath"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disk space");
        }
    }
    
    private void CheckWritePermissions(List<DependencyIssue> issues)
    {
        try
        {
            Directory.CreateDirectory(_workspacePath);
            var testFile = Path.Combine(_workspacePath, $"test-{Guid.NewGuid()}.tmp");
            
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (UnauthorizedAccessException)
        {
            issues.Add(new DependencyIssue
            {
                Id = "workspace-no-write",
                Category = IssueCategory.Storage,
                Severity = IssueSeverity.Error,
                Title = "No Write Permission",
                Description = $"Cannot write to workspace directory: {_workspacePath}",
                Remediation = "Grant write permissions or choose a different workspace location in Settings.",
                RelatedSettingKey = "workspacePath"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check write permissions");
        }
    }
    
    private async Task CheckNetworkConnectivityAsync(List<DependencyIssue> issues, CancellationToken ct)
    {
        // Only check if httpClientFactory is available
        if (_httpClientFactory == null)
        {
            return;
        }
        
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        
        // Check basic internet connectivity
        try
        {
            var response = await httpClient.GetAsync("https://www.google.com", ct);
            if (!response.IsSuccessStatusCode)
            {
                issues.Add(new DependencyIssue
                {
                    Id = "network-internet-unavailable",
                    Category = IssueCategory.Network,
                    Severity = IssueSeverity.Warning,
                    Title = "Internet Connectivity Issue",
                    Description = "Unable to reach the internet. Cloud providers will not be available.",
                    Remediation = "Check your internet connection or use local-only providers."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Internet connectivity check failed");
            issues.Add(new DependencyIssue
            {
                Id = "network-internet-error",
                Category = IssueCategory.Network,
                Severity = IssueSeverity.Warning,
                Title = "Internet Connectivity Unknown",
                Description = "Could not verify internet connectivity. Cloud providers may not work.",
                Remediation = "Check your internet connection or use local-only providers."
            });
        }
    }
    
    private async Task CheckProviderAvailabilityAsync(List<DependencyIssue> issues, CancellationToken ct)
    {
        // Check if OpenAI key exists
        var openAiKey = _keyStore.GetKey("openai");
        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            await CheckProviderEndpointAsync("OpenAI", "https://api.openai.com/v1/models", openAiKey, issues, ct);
        }
        
        // Check Ollama availability
        await CheckOllamaAvailabilityAsync(issues, ct);
    }
    
    private async Task CheckProviderEndpointAsync(
        string providerName,
        string endpoint,
        string apiKey,
        List<DependencyIssue> issues,
        CancellationToken ct)
    {
        if (_httpClientFactory == null)
        {
            return;
        }
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await httpClient.GetAsync(endpoint, ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                issues.Add(new DependencyIssue
                {
                    Id = $"provider-{providerName}-invalid-key",
                    Category = IssueCategory.Provider,
                    Severity = IssueSeverity.Error,
                    Title = $"{providerName} API Key Invalid",
                    Description = $"The configured {providerName} API key appears to be invalid or expired.",
                    Remediation = $"Update your {providerName} API key in Settings > API Keys.",
                    RelatedSettingKey = "apiKeys"
                });
            }
            else if (!response.IsSuccessStatusCode)
            {
                issues.Add(new DependencyIssue
                {
                    Id = $"provider-{providerName}-unreachable",
                    Category = IssueCategory.Provider,
                    Severity = IssueSeverity.Warning,
                    Title = $"{providerName} Service Unreachable",
                    Description = $"{providerName} API returned status {response.StatusCode}.",
                    Remediation = $"Check {providerName} service status or try again later."
                });
            }
        }
        catch (TaskCanceledException)
        {
            issues.Add(new DependencyIssue
            {
                Id = $"provider-{providerName}-timeout",
                Category = IssueCategory.Provider,
                Severity = IssueSeverity.Warning,
                Title = $"{providerName} Connection Timeout",
                Description = $"Connection to {providerName} timed out.",
                Remediation = "Check your internet connection and try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check {Provider} endpoint", providerName);
        }
    }
    
    private async Task CheckOllamaAvailabilityAsync(List<DependencyIssue> issues, CancellationToken ct)
    {
        if (_httpClientFactory == null)
        {
            return;
        }
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await httpClient.GetAsync("http://127.0.0.1:11434/api/tags", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                issues.Add(new DependencyIssue
                {
                    Id = "provider-ollama-not-running",
                    Category = IssueCategory.Provider,
                    Severity = IssueSeverity.Error,
                    Title = "Ollama Not Running",
                    Description = "Ollama is selected as LLM provider but is not running.",
                    Remediation = "Start Ollama service or switch to a different LLM provider.",
                    RelatedSettingKey = "llmProvider",
                    DocsUrl = "https://docs.aura.studio/providers/ollama"
                });
            }
        }
        catch (Exception)
        {
            issues.Add(new DependencyIssue
            {
                Id = "provider-ollama-not-available",
                Category = IssueCategory.Provider,
                Severity = IssueSeverity.Error,
                Title = "Ollama Not Available",
                Description = "Cannot connect to Ollama service at http://127.0.0.1:11434.",
                Remediation = "Install and start Ollama, or switch to a different LLM provider.",
                RelatedSettingKey = "llmProvider",
                ActionId = "install-ollama",
                DocsUrl = "https://docs.aura.studio/providers/ollama"
            });
        }
    }
    
    private async Task<GpuInfo?> GetGpuInfoAsync(CancellationToken ct)
    {
        // This would require a more sophisticated GPU detection mechanism
        // For now, return null and let hardware probe handle this
        await Task.CompletedTask;
        return null;
    }
    
    private string GetPlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        return "Unknown";
    }
    
    private long GetTotalMemoryMb()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            return gcInfo.TotalAvailableMemoryBytes / (1024 * 1024);
        }
        catch
        {
            return 0;
        }
    }
    
    private bool IsVersionSufficient(string versionString, int majorRequired, int minorRequired)
    {
        try
        {
            // Extract version number from string like "ffmpeg version 4.4.2-essentials_build-www.gyan.dev"
            var parts = versionString.Split(' ');
            if (parts.Length >= 3)
            {
                var versionPart = parts[2];
                var versionNumbers = versionPart.Split('-')[0].Split('.');
                
                if (versionNumbers.Length >= 2 &&
                    int.TryParse(versionNumbers[0], out var major) &&
                    int.TryParse(versionNumbers[1], out var minor))
                {
                    return major > majorRequired || (major == majorRequired && minor >= minorRequired);
                }
            }
        }
        catch
        {
            // Parsing failed, assume sufficient
        }
        
        return true;
    }
    
    private void ReportProgress(
        IProgress<ScanProgress>? progress,
        string eventType,
        string message,
        int? percentComplete,
        DependencyIssue? issue = null)
    {
        progress?.Report(new ScanProgress
        {
            Event = eventType,
            Message = message,
            PercentComplete = percentComplete ?? 0,
            Issue = issue
        });
    }
}
