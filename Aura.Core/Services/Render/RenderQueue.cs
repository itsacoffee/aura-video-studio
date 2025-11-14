using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.Editor;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Status of a queue item
/// </summary>
public enum QueueItemStatus
{
    Queued,
    Rendering,
    Complete,
    Failed,
    Cancelled
}

/// <summary>
/// Priority level for queue items
/// </summary>
public enum QueuePriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

/// <summary>
/// Item in the render queue
/// </summary>
public record RenderQueueItem(
    string Id,
    string JobId,
    string TimelineJson,
    ExportPreset Preset,
    string OutputPath,
    QueuePriority Priority,
    QueueItemStatus Status,
    int Progress,
    DateTime CreatedAt,
    DateTime? StartedAt = null,
    DateTime? CompletedAt = null,
    TimeSpan? RenderTime = null,
    long? FileSizeBytes = null,
    string? ErrorMessage = null,
    int RetryCount = 0);

/// <summary>
/// Queue statistics
/// </summary>
public record QueueStatistics(
    int TotalItems,
    int QueuedItems,
    int RenderingItems,
    int CompleteItems,
    int FailedItems,
    TimeSpan EstimatedTotalTime);

/// <summary>
/// Manages render queue for batch exports
/// </summary>
public class RenderQueue
{
    private readonly ILogger<RenderQueue> _logger;
    private readonly TimelineRenderer _timelineRenderer;
    private readonly string _queuePersistencePath;
    private readonly ConcurrentDictionary<string, RenderQueueItem> _queue;
    private readonly SemaphoreSlim _processingLock;
    private bool _isPaused;
    private CancellationTokenSource? _currentRenderCancellation;
    private Task? _processingTask;
    private CancellationTokenSource? _processingCancellation;

    public RenderQueue(ILogger<RenderQueue> logger, TimelineRenderer timelineRenderer, string persistenceDirectory)
    {
        _logger = logger;
        _timelineRenderer = timelineRenderer;
        _queuePersistencePath = Path.Combine(persistenceDirectory, "render_queue.json");
        _queue = new ConcurrentDictionary<string, RenderQueueItem>();
        _processingLock = new SemaphoreSlim(1, 1);
        _isPaused = false;

        // Load persisted queue
        LoadQueueFromDisk();
        
        // Start background processing
        StartProcessing();
    }

    /// <summary>
    /// Adds an item to the render queue
    /// </summary>
    public async Task<string> AddToQueueAsync(
        EditableTimeline timeline,
        ExportPreset preset,
        string outputPath,
        string jobId,
        QueuePriority priority = QueuePriority.Normal)
    {
        var id = $"queue_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        
        var timelineJson = JsonSerializer.Serialize(timeline);
        
        var item = new RenderQueueItem(
            Id: id,
            JobId: jobId,
            TimelineJson: timelineJson,
            Preset: preset,
            OutputPath: outputPath,
            Priority: priority,
            Status: QueueItemStatus.Queued,
            Progress: 0,
            CreatedAt: DateTime.UtcNow
        );

        if (_queue.TryAdd(id, item))
        {
            _logger.LogInformation(
                "Added item {Id} to render queue (Priority: {Priority}, Preset: {Preset})",
                id, priority, preset.Name
            );

            await PersistQueueAsync().ConfigureAwait(false);
            return id;
        }

        throw new InvalidOperationException($"Failed to add item {id} to queue");
    }

    /// <summary>
    /// Removes an item from the queue
    /// </summary>
    public async Task<bool> RemoveFromQueueAsync(string id)
    {
        if (_queue.TryGetValue(id, out var item))
        {
            if (item.Status == QueueItemStatus.Rendering)
            {
                // Cancel ongoing render
                _currentRenderCancellation?.Cancel();
            }

            if (_queue.TryRemove(id, out _))
            {
                _logger.LogInformation("Removed item {Id} from queue", id);
                await PersistQueueAsync().ConfigureAwait(false);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Updates queue item status
    /// </summary>
    public async Task UpdateQueueItemAsync(string id, Action<RenderQueueItem> updateAction)
    {
        if (_queue.TryGetValue(id, out var item))
        {
            var updatedItem = item;
            updateAction(updatedItem);
            
            _queue[id] = updatedItem;
            await PersistQueueAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets all queue items
    /// </summary>
    public List<RenderQueueItem> GetAllItems()
    {
        return _queue.Values
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets queue statistics
    /// </summary>
    public QueueStatistics GetStatistics()
    {
        var items = _queue.Values.ToList();
        
        return new QueueStatistics(
            TotalItems: items.Count,
            QueuedItems: items.Count(i => i.Status == QueueItemStatus.Queued),
            RenderingItems: items.Count(i => i.Status == QueueItemStatus.Rendering),
            CompleteItems: items.Count(i => i.Status == QueueItemStatus.Complete),
            FailedItems: items.Count(i => i.Status == QueueItemStatus.Failed),
            EstimatedTotalTime: EstimateTotalQueueTime(items)
        );
    }

    /// <summary>
    /// Pauses queue processing
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
        _logger.LogInformation("Render queue paused");
    }

    /// <summary>
    /// Resumes queue processing
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        _logger.LogInformation("Render queue resumed");
    }

    /// <summary>
    /// Clears completed items from queue
    /// </summary>
    public async Task ClearCompletedAsync()
    {
        var completedIds = _queue.Values
            .Where(i => i.Status == QueueItemStatus.Complete)
            .Select(i => i.Id)
            .ToList();

        foreach (var id in completedIds)
        {
            _queue.TryRemove(id, out _);
        }

        _logger.LogInformation("Cleared {Count} completed items from queue", completedIds.Count);
        await PersistQueueAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Retries a failed item
    /// </summary>
    public async Task<bool> RetryItemAsync(string id)
    {
        if (_queue.TryGetValue(id, out var item) && item.Status == QueueItemStatus.Failed)
        {
            var retriedItem = item with
            {
                Status = QueueItemStatus.Queued,
                Progress = 0,
                ErrorMessage = null,
                RetryCount = item.RetryCount + 1
            };

            _queue[id] = retriedItem;
            _logger.LogInformation("Retrying item {Id} (Attempt {Retry})", id, retriedItem.RetryCount + 1);
            await PersistQueueAsync().ConfigureAwait(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Starts background processing
    /// </summary>
    private void StartProcessing()
    {
        _processingCancellation = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessingLoopAsync(_processingCancellation.Token));
    }

    /// <summary>
    /// Stops background processing
    /// </summary>
    public async Task StopProcessingAsync()
    {
        _processingCancellation?.Cancel();
        if (_processingTask != null)
        {
            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }
    }

    /// <summary>
    /// Background worker that processes queue items
    /// </summary>
    private async Task ProcessingLoopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Render queue processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_isPaused)
                {
                    await ProcessNextItemAsync(stoppingToken).ConfigureAwait(false);
                }

                // Wait before checking for next item
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in render queue processor");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Render queue processor stopped");
    }

    private async Task ProcessNextItemAsync(CancellationToken stoppingToken)
    {
        // Check if already processing
        if (!await _processingLock.WaitAsync(0, stoppingToken).ConfigureAwait(false))
        {
            return; // Already processing an item
        }

        try
        {
            // Get next queued item (highest priority first)
            var nextItem = _queue.Values
                .Where(i => i.Status == QueueItemStatus.Queued)
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.CreatedAt)
                .FirstOrDefault();

            if (nextItem == null)
            {
                return; // No items to process
            }

            // Mark as rendering
            var renderingItem = nextItem with
            {
                Status = QueueItemStatus.Rendering,
                StartedAt = DateTime.UtcNow
            };
            _queue[nextItem.Id] = renderingItem;
            await PersistQueueAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Starting render for item {Id} (Preset: {Preset})",
                nextItem.Id, nextItem.Preset.Name
            );

            // Create cancellation token for this render
            _currentRenderCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                var startTime = DateTime.UtcNow;

                // Deserialize timeline
                var timeline = JsonSerializer.Deserialize<EditableTimeline>(nextItem.TimelineJson);
                if (timeline == null)
                {
                    throw new InvalidOperationException("Failed to deserialize timeline");
                }

                // Render the timeline using TimelineRenderer
                var renderSpec = ConvertPresetToRenderSpec(nextItem.Preset);
                
                var renderProgress = new Progress<int>(percent =>
                {
                    var progressItem = renderingItem with { Progress = percent };
                    _queue[nextItem.Id] = progressItem;
                });

                await _timelineRenderer.GenerateFinalAsync(
                    timeline,
                    renderSpec,
                    nextItem.OutputPath,
                    renderProgress,
                    _currentRenderCancellation.Token).ConfigureAwait(false);

                var renderTime = DateTime.UtcNow - startTime;

                // Get file size if output exists
                long? fileSize = null;
                if (File.Exists(nextItem.OutputPath))
                {
                    fileSize = new FileInfo(nextItem.OutputPath).Length;
                }

                // Mark as complete
                var completedItem = renderingItem with
                {
                    Status = QueueItemStatus.Complete,
                    Progress = 100,
                    CompletedAt = DateTime.UtcNow,
                    RenderTime = renderTime,
                    FileSizeBytes = fileSize
                };
                _queue[nextItem.Id] = completedItem;

                _logger.LogInformation(
                    "Completed render for item {Id} in {Time:F1}s (Size: {Size} MB)",
                    nextItem.Id, renderTime.TotalSeconds, fileSize.HasValue ? fileSize.Value / 1024.0 / 1024.0 : 0
                );
            }
            catch (OperationCanceledException)
            {
                // Mark as cancelled
                var cancelledItem = renderingItem with
                {
                    Status = QueueItemStatus.Cancelled,
                    CompletedAt = DateTime.UtcNow
                };
                _queue[nextItem.Id] = cancelledItem;
                _logger.LogWarning("Render cancelled for item {Id}", nextItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Render failed for item {Id}", nextItem.Id);

                // Retry logic
                if (renderingItem.RetryCount < 2)
                {
                    // Retry with exponential backoff
                    var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, renderingItem.RetryCount));
                    await Task.Delay(retryDelay, stoppingToken).ConfigureAwait(false);

                    var retryItem = renderingItem with
                    {
                        Status = QueueItemStatus.Queued,
                        Progress = 0,
                        RetryCount = renderingItem.RetryCount + 1
                    };
                    _queue[nextItem.Id] = retryItem;
                    _logger.LogInformation(
                        "Retrying item {Id} in {Delay}s (Attempt {Retry})",
                        nextItem.Id, retryDelay.TotalSeconds, retryItem.RetryCount
                    );
                }
                else
                {
                    // Mark as failed
                    var failedItem = renderingItem with
                    {
                        Status = QueueItemStatus.Failed,
                        CompletedAt = DateTime.UtcNow,
                        ErrorMessage = ex.Message
                    };
                    _queue[nextItem.Id] = failedItem;
                }
            }
            finally
            {
                _currentRenderCancellation?.Dispose();
                _currentRenderCancellation = null;
                await PersistQueueAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private TimeSpan EstimateTotalQueueTime(List<RenderQueueItem> items)
    {
        // Use average render time from completed items
        var completedItems = items.Where(i => i.Status == QueueItemStatus.Complete && i.RenderTime.HasValue).ToList();
        
        if (completedItems.Count == 0)
        {
            // No data, estimate 5 minutes per item
            return TimeSpan.FromMinutes(items.Count(i => i.Status == QueueItemStatus.Queued) * 5);
        }

        var avgRenderTime = TimeSpan.FromSeconds(completedItems.Average(i => i.RenderTime!.Value.TotalSeconds));
        var queuedCount = items.Count(i => i.Status == QueueItemStatus.Queued);
        
        return TimeSpan.FromSeconds(avgRenderTime.TotalSeconds * queuedCount);
    }

    private async Task PersistQueueAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_queuePersistencePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var items = _queue.Values.ToList();
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_queuePersistencePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist render queue");
        }
    }

    private void LoadQueueFromDisk()
    {
        try
        {
            if (!File.Exists(_queuePersistencePath))
            {
                return;
            }

            var json = File.ReadAllText(_queuePersistencePath);
            var items = JsonSerializer.Deserialize<List<RenderQueueItem>>(json);

            if (items != null)
            {
                foreach (var item in items)
                {
                    // Reset rendering items to queued on startup
                    if (item.Status == QueueItemStatus.Rendering)
                    {
                        var resetItem = item with { Status = QueueItemStatus.Queued, Progress = 0 };
                        _queue.TryAdd(item.Id, resetItem);
                    }
                    else
                    {
                        _queue.TryAdd(item.Id, item);
                    }
                }

                _logger.LogInformation("Loaded {Count} items from persisted queue", items.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted render queue");
        }
    }

    /// <summary>
    /// Converts ExportPreset to RenderSpec
    /// </summary>
    private Models.RenderSpec ConvertPresetToRenderSpec(ExportPreset preset)
    {
        return new Models.RenderSpec(
            Res: new Models.Resolution(preset.Resolution.Width, preset.Resolution.Height),
            Container: preset.Container,
            VideoBitrateK: preset.VideoBitrate / 1000,
            AudioBitrateK: preset.AudioBitrate / 1000,
            Fps: preset.FrameRate,
            Codec: "H264",
            QualityLevel: 75
        );
    }
}
