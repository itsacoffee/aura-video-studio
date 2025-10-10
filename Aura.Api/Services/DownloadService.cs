using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Service for managing downloads and installations of dependencies
/// Wraps DependencyManager to provide a service layer for API endpoints
/// </summary>
public class DownloadService
{
    private readonly ILogger<DownloadService> _logger;
    private readonly DependencyManager _dependencyManager;

    public DownloadService(
        ILogger<DownloadService> logger,
        DependencyManager dependencyManager)
    {
        _logger = logger;
        _dependencyManager = dependencyManager;
    }

    /// <summary>
    /// Load the dependency manifest
    /// </summary>
    public async Task<DependencyManifest> GetManifestAsync()
    {
        _logger.LogInformation("Loading dependency manifest");
        return await _dependencyManager.LoadManifestAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Check if a component is installed
    /// </summary>
    public async Task<bool> IsComponentInstalledAsync(string componentName)
    {
        _logger.LogInformation("Checking installation status for {Component}", componentName);
        return await _dependencyManager.IsComponentInstalledAsync(componentName).ConfigureAwait(false);
    }

    /// <summary>
    /// Download and install a component
    /// </summary>
    public async Task InstallComponentAsync(string componentName, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Installing component: {Component}", componentName);
        await _dependencyManager.DownloadComponentAsync(componentName, progress, ct).ConfigureAwait(false);
        _logger.LogInformation("Component {Component} installed successfully", componentName);
    }

    /// <summary>
    /// Verify component integrity
    /// </summary>
    public async Task<ComponentVerificationResult> VerifyComponentAsync(string componentName)
    {
        _logger.LogInformation("Verifying component: {Component}", componentName);
        return await _dependencyManager.VerifyComponentAsync(componentName).ConfigureAwait(false);
    }

    /// <summary>
    /// Repair a corrupted component
    /// </summary>
    public async Task RepairComponentAsync(string componentName, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Repairing component: {Component}", componentName);
        await _dependencyManager.RepairComponentAsync(componentName, progress, ct).ConfigureAwait(false);
        _logger.LogInformation("Component {Component} repaired successfully", componentName);
    }

    /// <summary>
    /// Remove a component
    /// </summary>
    public async Task RemoveComponentAsync(string componentName)
    {
        _logger.LogInformation("Removing component: {Component}", componentName);
        await _dependencyManager.RemoveComponentAsync(componentName).ConfigureAwait(false);
        _logger.LogInformation("Component {Component} removed successfully", componentName);
    }

    /// <summary>
    /// Get the installation directory for a component
    /// </summary>
    public string GetComponentDirectory(string componentName)
    {
        _logger.LogDebug("Getting directory for component: {Component}", componentName);
        return _dependencyManager.GetComponentDirectory(componentName);
    }

    /// <summary>
    /// Get manual installation instructions for offline mode
    /// </summary>
    public ManualInstallInstructions GetManualInstructions(string componentName)
    {
        _logger.LogInformation("Getting manual installation instructions for {Component}", componentName);
        return _dependencyManager.GetManualInstallInstructions(componentName);
    }
}
