using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that performs periodic autosave of active projects
/// </summary>
public class ProjectAutosaveService : BackgroundService
{
    private readonly ProjectVersionService _versionService;
    private readonly ILogger<ProjectAutosaveService> _logger;
    private readonly ConcurrentDictionary<Guid, DateTime> _activeProjects = new();
    private readonly TimeSpan _autosaveInterval;
    private readonly TimeSpan _minTimeBetweenSaves;

    public ProjectAutosaveService(
        ProjectVersionService versionService,
        ILogger<ProjectAutosaveService> logger)
    {
        _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _autosaveInterval = TimeSpan.FromMinutes(5);
        _minTimeBetweenSaves = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// Register a project as active for autosave
    /// </summary>
    public void RegisterProject(Guid projectId)
    {
        _activeProjects[projectId] = DateTime.UtcNow;
        _logger.LogDebug("Registered project {ProjectId} for autosave", projectId);
    }

    /// <summary>
    /// Unregister a project from autosave
    /// </summary>
    public void UnregisterProject(Guid projectId)
    {
        _activeProjects.TryRemove(projectId, out _);
        _logger.LogDebug("Unregistered project {ProjectId} from autosave", projectId);
    }

    /// <summary>
    /// Manually trigger an autosave for a project
    /// </summary>
    public async Task<bool> TriggerAutosaveAsync(Guid projectId, CancellationToken ct = default)
    {
        if (!_activeProjects.ContainsKey(projectId))
        {
            _logger.LogWarning("Cannot autosave unregistered project {ProjectId}", projectId);
            return false;
        }

        try
        {
            await _versionService.CreateAutosaveAsync(projectId, ct).ConfigureAwait(false);
            _activeProjects[projectId] = DateTime.UtcNow;
            _logger.LogDebug("Manual autosave triggered for project {ProjectId}", projectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform manual autosave for project {ProjectId}", projectId);
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Project autosave service started with interval {Interval}",
            _autosaveInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_autosaveInterval, stoppingToken).ConfigureAwait(false);

                if (_activeProjects.IsEmpty)
                {
                    continue;
                }

                _logger.LogDebug("Running autosave check for {Count} active projects", _activeProjects.Count);

                foreach (var kvp in _activeProjects)
                {
                    var projectId = kvp.Key;
                    var lastSave = kvp.Value;

                    if (DateTime.UtcNow - lastSave < _minTimeBetweenSaves)
                    {
                        _logger.LogDebug("Skipping autosave for project {ProjectId} - too soon since last save",
                            projectId);
                        continue;
                    }

                    try
                    {
                        await _versionService.CreateAutosaveAsync(projectId, stoppingToken).ConfigureAwait(false);
                        _activeProjects[projectId] = DateTime.UtcNow;
                        _logger.LogDebug("Autosaved project {ProjectId}", projectId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to autosave project {ProjectId}", projectId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Autosave service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in autosave service");
            }
        }

        _logger.LogInformation("Project autosave service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing final autosave before shutdown");

        foreach (var projectId in _activeProjects.Keys)
        {
            try
            {
                await _versionService.CreateAutosaveAsync(projectId, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Final autosave completed for project {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed final autosave for project {ProjectId}", projectId);
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
