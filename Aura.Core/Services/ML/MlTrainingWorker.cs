using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.ML;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ML;

/// <summary>
/// Background worker for ML training jobs
/// Manages a queue of training jobs and executes them with progress reporting
/// </summary>
public class MlTrainingWorker
{
    private readonly ILogger<MlTrainingWorker> _logger;
    private readonly ModelTrainingService _trainingService;
    private readonly ModelManager _modelManager;
    private readonly AnnotationStorageService _annotationStorage;
    private readonly ConcurrentDictionary<string, TrainingJob> _jobs = new();
    private readonly SemaphoreSlim _jobSemaphore = new(1, 1);

    public MlTrainingWorker(
        ILogger<MlTrainingWorker> logger,
        ModelTrainingService trainingService,
        ModelManager modelManager,
        AnnotationStorageService annotationStorage)
    {
        _logger = logger;
        _trainingService = trainingService;
        _modelManager = modelManager;
        _annotationStorage = annotationStorage;
    }

    /// <summary>
    /// Submit a new training job
    /// </summary>
    public async Task<string> SubmitJobAsync(
        string userId, 
        string? modelName = null,
        Dictionary<string, string>? pipelineConfig = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var jobId = Guid.NewGuid().ToString();
        var job = new TrainingJob(
            JobId: jobId,
            UserId: userId,
            State: TrainingJobState.Queued,
            Progress: 0.0,
            CreatedAt: DateTime.UtcNow,
            ModelName: modelName ?? "frame-importance",
            PipelineConfig: pipelineConfig ?? new Dictionary<string, string>());

        _jobs[jobId] = job;
        _logger.LogInformation("Training job {JobId} submitted for user {UserId}", jobId, userId);

        _ = Task.Run(async () => await ExecuteJobAsync(jobId, cancellationToken).ConfigureAwait(false), cancellationToken);

        return jobId;
    }

    /// <summary>
    /// Get the status of a training job
    /// </summary>
    public TrainingJob? GetJobStatus(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    /// <summary>
    /// Cancel a running training job
    /// </summary>
    public bool CancelJob(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        if (job.State != TrainingJobState.Running && job.State != TrainingJobState.Queued)
        {
            _logger.LogWarning("Cannot cancel job {JobId} in state {State}", jobId, job.State);
            return false;
        }

        job.CancellationTokenSource.Cancel();
        _logger.LogInformation("Training job {JobId} cancellation requested", jobId);
        return true;
    }

    /// <summary>
    /// Execute a training job
    /// </summary>
    private async Task ExecuteJobAsync(string jobId, CancellationToken globalCancellationToken)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        await _jobSemaphore.WaitAsync(globalCancellationToken).ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Starting execution of training job {JobId}", jobId);
            UpdateJobState(jobId, TrainingJobState.Running, 0.0);

            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                globalCancellationToken, 
                job.CancellationTokenSource.Token).Token;

            var annotations = await _annotationStorage.GetAnnotationsAsync(job.UserId, cancellationToken).ConfigureAwait(false);

            if (annotations.Count == 0)
            {
                throw new InvalidOperationException("No annotations found for training");
            }

            _logger.LogInformation("Job {JobId}: Loaded {Count} annotations", jobId, annotations.Count);
            UpdateJobProgress(jobId, 10.0);

            var frameAnnotations = annotations.Select(a => 
                new Aura.Core.Models.FrameAnalysis.FrameAnnotation(a.FramePath, a.Rating)).ToList();

            UpdateJobProgress(jobId, 20.0);

            var trainingResult = await _trainingService.TrainFrameImportanceModelAsync(
                frameAnnotations, 
                cancellationToken).ConfigureAwait(false);

            if (!trainingResult.Success)
            {
                throw new InvalidOperationException(trainingResult.ErrorMessage ?? "Training failed");
            }

            UpdateJobProgress(jobId, 80.0);
            _logger.LogInformation("Job {JobId}: Training completed, deploying model", jobId);

            if (trainingResult.ModelPath != null)
            {
                var deployed = await _modelManager.DeployModelAsync(trainingResult.ModelPath, cancellationToken).ConfigureAwait(false);
                
                if (!deployed)
                {
                    throw new InvalidOperationException("Failed to deploy trained model");
                }
            }

            UpdateJobProgress(jobId, 100.0);

            var metrics = new TrainingMetrics(
                Loss: 0.0,
                Samples: trainingResult.TrainingSamples,
                Duration: trainingResult.TrainingDuration,
                AdditionalMetrics: null);

            UpdateJobCompletion(jobId, TrainingJobState.Completed, metrics, trainingResult.ModelPath);
            _logger.LogInformation("Training job {JobId} completed successfully", jobId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Training job {JobId} was cancelled", jobId);
            UpdateJobState(jobId, TrainingJobState.Cancelled, job.Progress, error: "Job was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Training job {JobId} failed", jobId);
            UpdateJobState(jobId, TrainingJobState.Failed, job.Progress, error: ex.Message);
        }
        finally
        {
            _jobSemaphore.Release();
        }
    }

    private void UpdateJobState(string jobId, TrainingJobState state, double progress, string? error = null)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            _jobs[jobId] = job with 
            { 
                State = state, 
                Progress = progress, 
                Error = error,
                CompletedAt = state == TrainingJobState.Completed || 
                              state == TrainingJobState.Failed || 
                              state == TrainingJobState.Cancelled 
                    ? DateTime.UtcNow 
                    : null
            };
        }
    }

    private void UpdateJobProgress(string jobId, double progress)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            _jobs[jobId] = job with { Progress = progress };
        }
    }

    private void UpdateJobCompletion(
        string jobId, 
        TrainingJobState state, 
        TrainingMetrics metrics, 
        string? modelPath)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            _jobs[jobId] = job with 
            { 
                State = state, 
                Progress = 100.0,
                Metrics = metrics,
                ModelPath = modelPath,
                CompletedAt = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Represents a training job
/// </summary>
public record TrainingJob(
    string JobId,
    string UserId,
    TrainingJobState State,
    double Progress,
    DateTime CreatedAt,
    string ModelName,
    Dictionary<string, string> PipelineConfig,
    TrainingMetrics? Metrics = null,
    string? ModelPath = null,
    string? Error = null,
    DateTime? CompletedAt = null)
{
    public CancellationTokenSource CancellationTokenSource { get; } = new();
}

/// <summary>
/// Training job state
/// </summary>
public enum TrainingJobState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Training metrics
/// </summary>
public record TrainingMetrics(
    double Loss,
    int Samples,
    TimeSpan Duration,
    Dictionary<string, double>? AdditionalMetrics = null);
