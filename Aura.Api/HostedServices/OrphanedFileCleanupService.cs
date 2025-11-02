using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that cleans up orphaned files and old failed/cancelled projects
/// </summary>
public class OrphanedFileCleanupService : BackgroundService
{
    private readonly ILogger<OrphanedFileCleanupService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);
    private readonly TimeSpan _tempFileAge = TimeSpan.FromHours(24);
    private readonly TimeSpan _failedProjectAge = TimeSpan.FromDays(7);

    public OrphanedFileCleanupService(
        ILogger<OrphanedFileCleanupService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Orphaned file cleanup service starting");

        // Wait 5 minutes after startup before running first cleanup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running scheduled cleanup of orphaned files and old projects");
                await PerformCleanupAsync(stoppingToken);
                _logger.LogInformation("Cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup execution");
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Orphaned file cleanup service stopping");
    }

    private async Task PerformCleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ProjectStateRepository>();

        int filesDeleted = 0;
        int projectsDeleted = 0;
        long bytesFreed = 0;

        // Clean up old failed/cancelled projects
        var oldFailedProjects = await repository.GetOldProjectsByStatusAsync("Failed", _failedProjectAge, ct);
        var oldCancelledProjects = await repository.GetOldProjectsByStatusAsync("Cancelled", _failedProjectAge, ct);
        var oldProjects = oldFailedProjects.Concat(oldCancelledProjects).ToList();

        foreach (var project in oldProjects)
        {
            try
            {
                _logger.LogInformation("Cleaning up old project {ProjectId} (Status: {Status}, Updated: {UpdatedAt})",
                    project.Id, project.Status, project.UpdatedAt);

                // Delete associated files
                foreach (var asset in project.Assets)
                {
                    if (File.Exists(asset.FilePath))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(asset.FilePath);
                            bytesFreed += fileInfo.Length;
                            File.Delete(asset.FilePath);
                            filesDeleted++;
                            _logger.LogDebug("Deleted file: {FilePath}", asset.FilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete file: {FilePath}", asset.FilePath);
                        }
                    }
                }

                // Delete project from database
                await repository.DeleteAsync(project.Id, ct);
                projectsDeleted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up project {ProjectId}", project.Id);
            }
        }

        // Clean up orphaned temporary assets
        var orphanedAssets = await repository.GetOrphanedAssetsAsync(_tempFileAge, ct);
        
        foreach (var asset in orphanedAssets)
        {
            if (File.Exists(asset.FilePath))
            {
                try
                {
                    var fileInfo = new FileInfo(asset.FilePath);
                    bytesFreed += fileInfo.Length;
                    File.Delete(asset.FilePath);
                    filesDeleted++;
                    _logger.LogDebug("Deleted orphaned file: {FilePath}", asset.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete orphaned file: {FilePath}", asset.FilePath);
                }
            }
        }

        var mbFreed = bytesFreed / (1024.0 * 1024.0);
        _logger.LogInformation("Cleanup summary: {ProjectsDeleted} projects removed, {FilesDeleted} files deleted, {MbFreed:F2} MB freed",
            projectsDeleted, filesDeleted, mbFreed);
    }
}
