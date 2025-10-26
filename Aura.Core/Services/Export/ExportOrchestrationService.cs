using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.FFmpeg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Export job information
/// </summary>
public record ExportJob
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string InputFile { get; init; } = string.Empty;
    public string OutputFile { get; init; } = string.Empty;
    public ExportPreset Preset { get; init; } = null!;
    public Models.Export.Platform TargetPlatform { get; init; }
    public ExportJobStatus Status { get; init; } = ExportJobStatus.Queued;
    public double Progress { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// Export job status
/// </summary>
public enum ExportJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Export request
/// </summary>
public class ExportRequest
{
    public required string InputFile { get; init; }
    public required string OutputFile { get; init; }
    public required ExportPreset Preset { get; init; }
    public Models.Export.Platform TargetPlatform { get; init; } = Models.Export.Platform.Generic;
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? Duration { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Export result
/// </summary>
public record ExportResult
{
    public bool Success { get; init; }
    public string OutputFile { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// DTO for export history
/// </summary>
public record ExportHistoryDto
{
    public required string Id { get; init; }
    public required string InputFile { get; init; }
    public required string OutputFile { get; init; }
    public required string PresetName { get; init; }
    public required string Status { get; init; }
    public double Progress { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public long? FileSize { get; init; }
    public double? DurationSeconds { get; init; }
    public string? Platform { get; init; }
    public string? Resolution { get; init; }
    public string? Codec { get; init; }
}

/// <summary>
/// Service for orchestrating video export operations
/// </summary>
public interface IExportOrchestrationService
{
    /// <summary>
    /// Export a video with the specified settings
    /// </summary>
    Task<ExportResult> ExportAsync(
        ExportRequest request,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Queue an export job for background processing
    /// </summary>
    Task<string> QueueExportAsync(ExportRequest request);
    
    /// <summary>
    /// Get status of an export job
    /// </summary>
    Task<ExportJob?> GetJobStatusAsync(string jobId);
    
    /// <summary>
    /// Cancel an export job
    /// </summary>
    Task<bool> CancelJobAsync(string jobId);
    
    /// <summary>
    /// Get all queued and processing jobs
    /// </summary>
    Task<IReadOnlyList<ExportJob>> GetActiveJobsAsync();
    
    /// <summary>
    /// Get export history
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="limit">Maximum number of records to return</param>
    Task<IReadOnlyList<ExportHistoryDto>> GetExportHistoryAsync(string? status = null, int limit = 100);
}

/// <summary>
/// Implementation of export orchestration service
/// </summary>
public class ExportOrchestrationService : IExportOrchestrationService
{
    private readonly IFFmpegService _ffmpegService;
    private readonly IFormatConversionService _formatConversionService;
    private readonly IResolutionService _resolutionService;
    private readonly IBitrateOptimizationService _bitrateOptimizationService;
    private readonly ILogger<ExportOrchestrationService> _logger;
    private readonly AuraDbContext _dbContext;
    private readonly Dictionary<string, ExportJob> _jobs = new();
    private readonly SemaphoreSlim _jobLock = new(1, 1);

    public ExportOrchestrationService(
        IFFmpegService ffmpegService,
        IFormatConversionService formatConversionService,
        IResolutionService resolutionService,
        IBitrateOptimizationService bitrateOptimizationService,
        ILogger<ExportOrchestrationService> logger,
        AuraDbContext dbContext)
    {
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
        _formatConversionService = formatConversionService ?? throw new ArgumentNullException(nameof(formatConversionService));
        _resolutionService = resolutionService ?? throw new ArgumentNullException(nameof(resolutionService));
        _bitrateOptimizationService = bitrateOptimizationService ?? throw new ArgumentNullException(nameof(bitrateOptimizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ExportResult> ExportAsync(
        ExportRequest request,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting export to {OutputFile} using preset {Preset}", 
            request.OutputFile, request.Preset.Name);

        var warnings = new List<string>();
        var startTime = DateTime.UtcNow;

        try
        {
            // Validate input file
            if (!File.Exists(request.InputFile))
            {
                throw new FileNotFoundException("Input file not found", request.InputFile);
            }

            // Validate preset against platform if specified
            if (request.TargetPlatform != Models.Export.Platform.Generic)
            {
                var profile = PlatformExportProfileFactory.GetProfile(request.TargetPlatform);
                var (isValid, errors) = PlatformExportProfileFactory.ValidateExportForPlatform(request.Preset, profile);
                
                if (!isValid)
                {
                    warnings.AddRange(errors);
                    _logger.LogWarning("Export settings may not be optimal for {Platform}: {Errors}", 
                        request.TargetPlatform, string.Join(", ", errors));
                }
            }

            // Get video info
            var videoInfo = await _ffmpegService.GetVideoInfoAsync(request.InputFile, cancellationToken);
            
            // Build FFmpeg command
            var commandBuilder = FFmpegCommandBuilder.FromPreset(
                request.Preset, 
                request.InputFile, 
                request.OutputFile);

            // Apply trimming if requested
            if (request.StartTime.HasValue)
            {
                commandBuilder.SetStartTime(request.StartTime.Value);
            }
            
            if (request.Duration.HasValue)
            {
                commandBuilder.SetDuration(request.Duration.Value);
            }

            // Add metadata
            foreach (var metadata in request.Metadata)
            {
                commandBuilder.AddMetadata(metadata.Key, metadata.Value);
            }

            // Check if resolution conversion is needed
            var needsResize = videoInfo.Width != request.Preset.Resolution.Width ||
                            videoInfo.Height != request.Preset.Resolution.Height;
            
            if (needsResize)
            {
                var scaleMode = _resolutionService.DetermineScaleMode(
                    new Resolution(videoInfo.Width, videoInfo.Height),
                    request.Preset.Resolution,
                    request.Preset.AspectRatio);
                
                commandBuilder.AddScaleFilter(
                    request.Preset.Resolution.Width,
                    request.Preset.Resolution.Height,
                    scaleMode);
            }

            // Execute export
            var command = commandBuilder.Build();
            _logger.LogDebug("FFmpeg command: {Command}", command);

            var totalDuration = request.Duration ?? videoInfo.Duration;
            var result = await _ffmpegService.ExecuteAsync(
                command,
                progress =>
                {
                    if (totalDuration.TotalSeconds > 0)
                    {
                        var percentComplete = (progress.ProcessedDuration.TotalSeconds / totalDuration.TotalSeconds) * 100.0;
                        progressCallback?.Invoke(Math.Min(100, percentComplete));
                    }
                },
                cancellationToken);

            if (!result.Success)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage ?? "Export failed",
                    Duration = DateTime.UtcNow - startTime,
                    Warnings = warnings
                };
            }

            // Get output file info
            var outputFileInfo = new FileInfo(request.OutputFile);
            
            _logger.LogInformation("Export completed successfully. Output size: {Size} bytes", 
                outputFileInfo.Length);

            return new ExportResult
            {
                Success = true,
                OutputFile = request.OutputFile,
                FileSize = outputFileInfo.Length,
                Duration = DateTime.UtcNow - startTime,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            
            return new ExportResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime,
                Warnings = warnings
            };
        }
    }

    public async Task<string> QueueExportAsync(ExportRequest request)
    {
        await _jobLock.WaitAsync();
        try
        {
            var job = new ExportJob
            {
                InputFile = request.InputFile,
                OutputFile = request.OutputFile,
                Preset = request.Preset,
                TargetPlatform = request.TargetPlatform,
                Status = ExportJobStatus.Queued
            };

            _jobs[job.Id] = job;
            
            // Save to database
            var historyEntity = new ExportHistoryEntity
            {
                Id = job.Id,
                InputFile = job.InputFile,
                OutputFile = job.OutputFile,
                PresetName = job.Preset.Name,
                Status = job.Status.ToString(),
                Progress = job.Progress,
                CreatedAt = job.CreatedAt,
                Platform = job.TargetPlatform.ToString(),
                Resolution = $"{job.Preset.Resolution.Width}x{job.Preset.Resolution.Height}",
                Codec = job.Preset.VideoCodec
            };
            
            _dbContext.ExportHistory.Add(historyEntity);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Queued export job {JobId}", job.Id);

            // Start processing in background (in a real implementation, this would use a proper queue/worker)
            _ = ProcessJobAsync(job.Id);

            return job.Id;
        }
        finally
        {
            _jobLock.Release();
        }
    }

    public async Task<ExportJob?> GetJobStatusAsync(string jobId)
    {
        await _jobLock.WaitAsync();
        try
        {
            return _jobs.TryGetValue(jobId, out var job) ? job : null;
        }
        finally
        {
            _jobLock.Release();
        }
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        await _jobLock.WaitAsync();
        try
        {
            if (_jobs.TryGetValue(jobId, out var job) && job.Status == ExportJobStatus.Queued)
            {
                _jobs[jobId] = job with { Status = ExportJobStatus.Cancelled };
                return true;
            }
            return false;
        }
        finally
        {
            _jobLock.Release();
        }
    }

    public async Task<IReadOnlyList<ExportJob>> GetActiveJobsAsync()
    {
        await _jobLock.WaitAsync();
        try
        {
            return _jobs.Values
                .Where(j => j.Status == ExportJobStatus.Queued || j.Status == ExportJobStatus.Processing)
                .OrderBy(j => j.CreatedAt)
                .ToList();
        }
        finally
        {
            _jobLock.Release();
        }
    }

    private async Task ProcessJobAsync(string jobId)
    {
        await _jobLock.WaitAsync();
        ExportJob? job;
        try
        {
            if (!_jobs.TryGetValue(jobId, out job) || job.Status != ExportJobStatus.Queued)
            {
                return;
            }

            _jobs[jobId] = job with 
            { 
                Status = ExportJobStatus.Processing, 
                StartedAt = DateTime.UtcNow 
            };
            job = _jobs[jobId];
            
            // Update database
            var entity = await _dbContext.ExportHistory.FindAsync(jobId);
            if (entity != null)
            {
                entity.Status = ExportJobStatus.Processing.ToString();
                entity.StartedAt = job.StartedAt;
                await _dbContext.SaveChangesAsync();
            }
        }
        finally
        {
            _jobLock.Release();
        }

        try
        {
            var request = new ExportRequest
            {
                InputFile = job.InputFile,
                OutputFile = job.OutputFile,
                Preset = job.Preset,
                TargetPlatform = job.TargetPlatform
            };

            var result = await ExportAsync(
                request,
                progress =>
                {
                    _jobLock.Wait();
                    try
                    {
                        if (_jobs.TryGetValue(jobId, out var currentJob))
                        {
                            _jobs[jobId] = currentJob with { Progress = progress };
                        }
                    }
                    finally
                    {
                        _jobLock.Release();
                    }
                });

            await _jobLock.WaitAsync();
            try
            {
                if (_jobs.TryGetValue(jobId, out var currentJob))
                {
                    _jobs[jobId] = currentJob with
                    {
                        Status = result.Success ? ExportJobStatus.Completed : ExportJobStatus.Failed,
                        CompletedAt = DateTime.UtcNow,
                        ErrorMessage = result.ErrorMessage,
                        Progress = 100
                    };
                    
                    // Update database
                    var entity = await _dbContext.ExportHistory.FindAsync(jobId);
                    if (entity != null)
                    {
                        entity.Status = (result.Success ? ExportJobStatus.Completed : ExportJobStatus.Failed).ToString();
                        entity.CompletedAt = DateTime.UtcNow;
                        entity.ErrorMessage = result.ErrorMessage;
                        entity.Progress = 100;
                        if (result.Success && result.FileSize > 0)
                        {
                            entity.FileSize = result.FileSize;
                            entity.DurationSeconds = result.Duration.TotalSeconds;
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            finally
            {
                _jobLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing export job {JobId}", jobId);
            
            await _jobLock.WaitAsync();
            try
            {
                if (_jobs.TryGetValue(jobId, out var currentJob))
                {
                    _jobs[jobId] = currentJob with
                    {
                        Status = ExportJobStatus.Failed,
                        CompletedAt = DateTime.UtcNow,
                        ErrorMessage = ex.Message
                    };
                    
                    // Update database
                    var entity = await _dbContext.ExportHistory.FindAsync(jobId);
                    if (entity != null)
                    {
                        entity.Status = ExportJobStatus.Failed.ToString();
                        entity.CompletedAt = DateTime.UtcNow;
                        entity.ErrorMessage = ex.Message;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            finally
            {
                _jobLock.Release();
            }
        }
    }

    public async Task<IReadOnlyList<ExportHistoryDto>> GetExportHistoryAsync(string? status = null, int limit = 100)
    {
        var query = _dbContext.ExportHistory.AsQueryable();
        
        // Filter by status if provided
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }
        
        // Order by most recent first and limit results
        var entities = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();
        
        // Map to DTOs
        return entities.Select(e => new ExportHistoryDto
        {
            Id = e.Id,
            InputFile = e.InputFile,
            OutputFile = e.OutputFile,
            PresetName = e.PresetName,
            Status = e.Status,
            Progress = e.Progress,
            CreatedAt = e.CreatedAt,
            StartedAt = e.StartedAt,
            CompletedAt = e.CompletedAt,
            ErrorMessage = e.ErrorMessage,
            FileSize = e.FileSize,
            DurationSeconds = e.DurationSeconds,
            Platform = e.Platform,
            Resolution = e.Resolution,
            Codec = e.Codec
        }).ToList();
    }
}
