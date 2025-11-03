using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.ML;
using Aura.Core.Services.ML;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for ML training services
/// </summary>
public class MlTrainingTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<AnnotationStorageService> _annotationLogger;
    private readonly ILogger<ModelManager> _modelManagerLogger;
    private readonly ILogger<MlTrainingWorker> _workerLogger;
    private readonly ILogger<ModelTrainingService> _trainingLogger;

    public MlTrainingTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        
        var loggerFactory = new LoggerFactory();
        _annotationLogger = loggerFactory.CreateLogger<AnnotationStorageService>();
        _modelManagerLogger = loggerFactory.CreateLogger<ModelManager>();
        _workerLogger = loggerFactory.CreateLogger<MlTrainingWorker>();
        _trainingLogger = loggerFactory.CreateLogger<ModelTrainingService>();
    }

    [Fact]
    public async Task AnnotationStorage_StoreAndRetrieve_Success()
    {
        var storage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var userId = "test-user";
        
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.8, DateTime.UtcNow),
            new("frame2.jpg", 0.5, DateTime.UtcNow),
            new("frame3.jpg", 0.9, DateTime.UtcNow)
        };

        await storage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);

        var retrieved = await storage.GetAnnotationsAsync(userId, CancellationToken.None);

        Assert.Equal(3, retrieved.Count);
        Assert.Contains(retrieved, a => a.FramePath == "frame1.jpg" && a.Rating == 0.8);
        Assert.Contains(retrieved, a => a.FramePath == "frame2.jpg" && a.Rating == 0.5);
        Assert.Contains(retrieved, a => a.FramePath == "frame3.jpg" && a.Rating == 0.9);
    }

    [Fact]
    public async Task AnnotationStorage_GetStats_ReturnsCorrectStatistics()
    {
        var storage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var userId = "test-user-stats";
        
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.6, DateTime.UtcNow.AddHours(-2)),
            new("frame2.jpg", 0.8, DateTime.UtcNow.AddHours(-1)),
            new("frame3.jpg", 1.0, DateTime.UtcNow)
        };

        await storage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);

        var stats = await storage.GetStatsAsync(userId, CancellationToken.None);

        Assert.Equal(userId, stats.UserId);
        Assert.Equal(3, stats.TotalAnnotations);
        Assert.Equal(0.8, stats.AverageRating, precision: 2);
        Assert.NotNull(stats.OldestAnnotation);
        Assert.NotNull(stats.NewestAnnotation);
    }

    [Fact]
    public async Task AnnotationStorage_InvalidRating_ThrowsException()
    {
        var storage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var userId = "test-user-invalid";
        
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 1.5, DateTime.UtcNow)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None));
    }

    [Fact]
    public async Task AnnotationStorage_EmptyFramePath_ThrowsException()
    {
        var storage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var userId = "test-user-empty";
        
        var annotations = new List<AnnotationRecord>
        {
            new("", 0.5, DateTime.UtcNow)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => 
            storage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None));
    }

    [Fact]
    public async Task AnnotationStorage_ClearAnnotations_RemovesAllData()
    {
        var storage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var userId = "test-user-clear";
        
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.8, DateTime.UtcNow)
        };

        await storage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);
        await storage.ClearAnnotationsAsync(userId, CancellationToken.None);

        var retrieved = await storage.GetAnnotationsAsync(userId, CancellationToken.None);
        Assert.Empty(retrieved);
    }

    [Fact]
    public async Task ModelManager_DeployModel_CreatesBackup()
    {
        var modelDirectory = Path.Combine(_tempDirectory, "models");
        Directory.CreateDirectory(modelDirectory);
        
        var manager = new ModelManager(_modelManagerLogger, modelDirectory);
        
        var existingModelPath = Path.Combine(modelDirectory, "frame-importance-model.zip");
        await File.WriteAllTextAsync(existingModelPath, "existing model data");
        
        var newModelPath = Path.Combine(_tempDirectory, "new-model.zip");
        await File.WriteAllTextAsync(newModelPath, "new model data");

        var deployed = await manager.DeployModelAsync(newModelPath, CancellationToken.None);

        Assert.True(deployed);
        
        var backupPath = existingModelPath + ".backup";
        Assert.True(File.Exists(backupPath));
        
        var backupContent = await File.ReadAllTextAsync(backupPath);
        Assert.Equal("existing model data", backupContent);
        
        var currentContent = await File.ReadAllTextAsync(existingModelPath);
        Assert.Equal("new model data", currentContent);
    }

    [Fact]
    public async Task ModelManager_RevertToDefault_RemovesActiveModel()
    {
        var modelDirectory = Path.Combine(_tempDirectory, "models-revert");
        Directory.CreateDirectory(modelDirectory);
        
        var manager = new ModelManager(_modelManagerLogger, modelDirectory);
        
        var activeModelPath = Path.Combine(modelDirectory, "frame-importance-model.zip");
        await File.WriteAllTextAsync(activeModelPath, "active model");

        var reverted = await manager.RevertToDefaultAsync(CancellationToken.None);

        Assert.True(reverted);
        Assert.False(File.Exists(activeModelPath));
    }

    [Fact]
    public async Task ModelManager_GetActiveModelPath_FallsBackToDefault()
    {
        var modelDirectory = Path.Combine(_tempDirectory, "models-fallback");
        Directory.CreateDirectory(modelDirectory);
        
        var manager = new ModelManager(_modelManagerLogger, modelDirectory);
        
        var defaultModelPath = Path.Combine(modelDirectory, "frame-importance-model-default.zip");
        await File.WriteAllTextAsync(defaultModelPath, "default model");

        var activePath = await manager.GetActiveModelPathAsync(CancellationToken.None);

        Assert.Equal(defaultModelPath, activePath);
    }

    [Fact]
    public async Task MlTrainingWorker_SubmitJob_ReturnsJobId()
    {
        var annotationStorage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var trainingService = new ModelTrainingService(_trainingLogger, Path.Combine(_tempDirectory, "training"));
        var modelManager = new ModelManager(_modelManagerLogger, Path.Combine(_tempDirectory, "models-worker"));
        
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);
        
        var userId = "test-worker-user";
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.7, DateTime.UtcNow)
        };
        await annotationStorage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);

        var jobId = await worker.SubmitJobAsync(userId, cancellationToken: CancellationToken.None);

        Assert.NotNull(jobId);
        Assert.NotEmpty(jobId);
        
        var status = worker.GetJobStatus(jobId);
        Assert.NotNull(status);
        Assert.Equal(jobId, status.JobId);
    }

    [Fact]
    public async Task MlTrainingWorker_GetJobStatus_ReturnsCorrectStatus()
    {
        var annotationStorage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var trainingService = new ModelTrainingService(_trainingLogger, Path.Combine(_tempDirectory, "training-status"));
        var modelManager = new ModelManager(_modelManagerLogger, Path.Combine(_tempDirectory, "models-status"));
        
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);
        
        var userId = "test-status-user";
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.7, DateTime.UtcNow)
        };
        await annotationStorage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);

        var jobId = await worker.SubmitJobAsync(userId, cancellationToken: CancellationToken.None);
        
        await Task.Delay(100);

        var status = worker.GetJobStatus(jobId);
        
        Assert.NotNull(status);
        Assert.Equal(userId, status.UserId);
        Assert.True(status.State == TrainingJobState.Queued || status.State == TrainingJobState.Running);
    }

    [Fact]
    public void MlTrainingWorker_GetJobStatus_NonExistentJob_ReturnsNull()
    {
        var annotationStorage = new AnnotationStorageService(_annotationLogger, _tempDirectory);
        var trainingService = new ModelTrainingService(_trainingLogger, Path.Combine(_tempDirectory, "training-null"));
        var modelManager = new ModelManager(_modelManagerLogger, Path.Combine(_tempDirectory, "models-null"));
        
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);

        var status = worker.GetJobStatus("non-existent-job-id");

        Assert.Null(status);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
