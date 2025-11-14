using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aura.Core.Models.Projects;

namespace Aura.Core.Services.Projects;

/// <summary>
/// Auto-save service configuration
/// </summary>
public class AutoSaveConfiguration
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300; // 5 minutes
    public int MaxBackupsPerProject { get; set; } = 10;
    public bool CreateBackupOnCrash { get; set; } = true;
}

/// <summary>
/// Background service for auto-saving projects
/// </summary>
public class ProjectAutoSaveService : BackgroundService
{
    private readonly ILogger<ProjectAutoSaveService> _logger;
    private readonly IProjectFileService _projectFileService;
    private readonly AutoSaveConfiguration _config;
    private readonly ConcurrentDictionary<Guid, ProjectAutoSaveState> _projectStates = new();

    private class ProjectAutoSaveState
    {
        public AuraProjectFile Project { get; set; } = null!;
        public DateTime LastSaved { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsDirty { get; set; }
        public int AutoSaveCount { get; set; }
    }

    public ProjectAutoSaveService(
        ILogger<ProjectAutoSaveService> logger,
        IProjectFileService projectFileService,
        AutoSaveConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectFileService = projectFileService ?? throw new ArgumentNullException(nameof(projectFileService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Project auto-save service started (interval: {Interval}s)", _config.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_config.Enabled)
                {
                    await PerformAutoSaveAsync(stoppingToken).ConfigureAwait(false);
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.IntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-save cycle");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Project auto-save service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Project auto-save service stopping, performing final save...");

        // Perform final save before stopping
        if (_config.CreateBackupOnCrash)
        {
            await PerformAutoSaveAsync(cancellationToken, isFinalSave: true).ConfigureAwait(false);
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PerformAutoSaveAsync(CancellationToken ct, bool isFinalSave = false)
    {
        var savedCount = 0;
        var errorCount = 0;

        foreach (var kvp in _projectStates)
        {
            try
            {
                var projectId = kvp.Key;
                var state = kvp.Value;

                // Check if project needs saving
                if (!state.IsDirty && !isFinalSave)
                {
                    continue;
                }

                // Check if enough time has passed since last save
                var timeSinceLastSave = DateTime.UtcNow - state.LastSaved;
                if (timeSinceLastSave.TotalSeconds < _config.IntervalSeconds && !isFinalSave)
                {
                    continue;
                }

                // Save project
                await _projectFileService.SaveProjectAsync(state.Project, ct).ConfigureAwait(false);
                
                state.LastSaved = DateTime.UtcNow;
                state.IsDirty = false;
                state.AutoSaveCount++;

                savedCount++;

                _logger.LogDebug("Auto-saved project {ProjectId} - {ProjectName} (save #{Count})",
                    projectId, state.Project.Name, state.AutoSaveCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-save project {ProjectId}", kvp.Key);
                errorCount++;
            }
        }

        if (savedCount > 0 || errorCount > 0)
        {
            _logger.LogInformation("Auto-save cycle completed: {Saved} saved, {Errors} errors", savedCount, errorCount);
        }
    }

    /// <summary>
    /// Register a project for auto-save tracking
    /// </summary>
    public void RegisterProject(AuraProjectFile project)
    {
        if (!project.AutoSaveEnabled)
        {
            _logger.LogDebug("Auto-save disabled for project {ProjectId}", project.Id);
            return;
        }

        var state = new ProjectAutoSaveState
        {
            Project = project,
            LastSaved = project.LastSavedAt,
            LastModified = project.ModifiedAt,
            IsDirty = false,
            AutoSaveCount = 0
        };

        _projectStates[project.Id] = state;
        _logger.LogInformation("Registered project {ProjectId} for auto-save", project.Id);
    }

    /// <summary>
    /// Unregister a project from auto-save tracking
    /// </summary>
    public void UnregisterProject(Guid projectId)
    {
        if (_projectStates.TryRemove(projectId, out _))
        {
            _logger.LogInformation("Unregistered project {ProjectId} from auto-save", projectId);
        }
    }

    /// <summary>
    /// Mark a project as modified (dirty)
    /// </summary>
    public void MarkProjectModified(Guid projectId, AuraProjectFile project)
    {
        if (_projectStates.TryGetValue(projectId, out var state))
        {
            state.Project = project;
            state.LastModified = DateTime.UtcNow;
            state.IsDirty = true;
            
            _logger.LogDebug("Marked project {ProjectId} as modified", projectId);
        }
        else
        {
            // Auto-register if not already registered
            RegisterProject(project);
            if (_projectStates.TryGetValue(projectId, out var newState))
            {
                newState.IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Force immediate save of a specific project
    /// </summary>
    public async Task ForceSaveProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        if (_projectStates.TryGetValue(projectId, out var state))
        {
            try
            {
                await _projectFileService.SaveProjectAsync(state.Project, ct).ConfigureAwait(false);
                
                state.LastSaved = DateTime.UtcNow;
                state.IsDirty = false;
                state.AutoSaveCount++;

                _logger.LogInformation("Force-saved project {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to force-save project {ProjectId}", projectId);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Cannot force-save unregistered project {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// Get auto-save statistics for a project
    /// </summary>
    public AutoSaveStatistics? GetProjectStatistics(Guid projectId)
    {
        if (_projectStates.TryGetValue(projectId, out var state))
        {
            return new AutoSaveStatistics
            {
                ProjectId = projectId,
                ProjectName = state.Project.Name,
                LastSaved = state.LastSaved,
                LastModified = state.LastModified,
                IsDirty = state.IsDirty,
                AutoSaveCount = state.AutoSaveCount,
                TimeSinceLastSave = DateTime.UtcNow - state.LastSaved,
                IsRegistered = true
            };
        }

        return null;
    }

    /// <summary>
    /// Get auto-save statistics for all registered projects
    /// </summary>
    public AutoSaveStatistics[] GetAllStatistics()
    {
        var stats = new AutoSaveStatistics[_projectStates.Count];
        int index = 0;

        foreach (var kvp in _projectStates)
        {
            var state = kvp.Value;
            stats[index++] = new AutoSaveStatistics
            {
                ProjectId = kvp.Key,
                ProjectName = state.Project.Name,
                LastSaved = state.LastSaved,
                LastModified = state.LastModified,
                IsDirty = state.IsDirty,
                AutoSaveCount = state.AutoSaveCount,
                TimeSinceLastSave = DateTime.UtcNow - state.LastSaved,
                IsRegistered = true
            };
        }

        return stats;
    }
}

/// <summary>
/// Auto-save statistics for a project
/// </summary>
public class AutoSaveStatistics
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime LastSaved { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDirty { get; set; }
    public int AutoSaveCount { get; set; }
    public TimeSpan TimeSinceLastSave { get; set; }
    public bool IsRegistered { get; set; }
}
