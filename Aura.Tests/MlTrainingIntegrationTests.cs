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
/// Integration tests for ML training end-to-end workflows
/// </summary>
public class MlTrainingIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<AnnotationStorageService> _annotationLogger;
    private readonly ILogger<ModelManager> _modelManagerLogger;
    private readonly ILogger<MlTrainingWorker> _workerLogger;
    private readonly ILogger<ModelTrainingService> _trainingLogger;

    public MlTrainingIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"aura-integration-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        
        var loggerFactory = new LoggerFactory();
        _annotationLogger = loggerFactory.CreateLogger<AnnotationStorageService>();
        _modelManagerLogger = loggerFactory.CreateLogger<ModelManager>();
        _workerLogger = loggerFactory.CreateLogger<MlTrainingWorker>();
        _trainingLogger = loggerFactory.CreateLogger<ModelTrainingService>();
    }

    [Fact]
    public async Task EndToEnd_TrainingWorkflow_WithSmallDataset_Success()
    {
        var annotationDir = Path.Combine(_tempDirectory, "annotations");
        var modelDir = Path.Combine(_tempDirectory, "models");
        var trainingDir = Path.Combine(_tempDirectory, "training");
        
        Directory.CreateDirectory(annotationDir);
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(trainingDir);

        var annotationStorage = new AnnotationStorageService(_annotationLogger, annotationDir);
        var trainingService = new ModelTrainingService(_trainingLogger, trainingDir);
        var modelManager = new ModelManager(_modelManagerLogger, modelDir);
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);

        var userId = "integration-test-user";
        var annotations = new List<AnnotationRecord>
        {
            new("frame001.jpg", 0.9, DateTime.UtcNow),
            new("frame002.jpg", 0.7, DateTime.UtcNow),
            new("frame003.jpg", 0.5, DateTime.UtcNow),
            new("frame004.jpg", 0.8, DateTime.UtcNow),
            new("frame005.jpg", 0.6, DateTime.UtcNow)
        };

        await annotationStorage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);

        var jobId = await worker.SubmitJobAsync(userId, cancellationToken: CancellationToken.None);
        
        Assert.NotNull(jobId);

        TrainingJob? finalStatus = null;
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(200);
            var status = worker.GetJobStatus(jobId);
            
            if (status?.State == TrainingJobState.Completed || 
                status?.State == TrainingJobState.Failed)
            {
                finalStatus = status;
                break;
            }
        }

        Assert.NotNull(finalStatus);
        Assert.Equal(TrainingJobState.Completed, finalStatus.State);
        Assert.Equal(100.0, finalStatus.Progress);
        Assert.NotNull(finalStatus.ModelPath);
        Assert.True(File.Exists(finalStatus.ModelPath));
        Assert.NotNull(finalStatus.Metrics);
        Assert.Equal(5, finalStatus.Metrics.Samples);
    }

    [Fact]
    public async Task EndToEnd_CancellationWorkflow_Success()
    {
        var annotationDir = Path.Combine(_tempDirectory, "annotations-cancel");
        var modelDir = Path.Combine(_tempDirectory, "models-cancel");
        var trainingDir = Path.Combine(_tempDirectory, "training-cancel");
        
        Directory.CreateDirectory(annotationDir);
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(trainingDir);

        var annotationStorage = new AnnotationStorageService(_annotationLogger, annotationDir);
        var trainingService = new ModelTrainingService(_trainingLogger, trainingDir);
        var modelManager = new ModelManager(_modelManagerLogger, modelDir);
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);

        var userId = "cancel-test-user";
        var annotations = new List<AnnotationRecord>
        {
            new("frame001.jpg", 0.9, DateTime.UtcNow)
        };

        await annotationStorage.StoreAnnotationsAsync(userId, annotations, CancellationToken.None);

        var jobId = await worker.SubmitJobAsync(userId, cancellationToken: CancellationToken.None);

        await Task.Delay(100);
        
        var cancelled = worker.CancelJob(jobId);
        Assert.True(cancelled);

        await Task.Delay(500);

        var finalStatus = worker.GetJobStatus(jobId);
        Assert.NotNull(finalStatus);
        Assert.Equal(TrainingJobState.Cancelled, finalStatus.State);
    }

    [Fact]
    public async Task EndToEnd_InsufficientData_Fails()
    {
        var annotationDir = Path.Combine(_tempDirectory, "annotations-insufficient");
        var modelDir = Path.Combine(_tempDirectory, "models-insufficient");
        var trainingDir = Path.Combine(_tempDirectory, "training-insufficient");
        
        Directory.CreateDirectory(annotationDir);
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(trainingDir);

        var annotationStorage = new AnnotationStorageService(_annotationLogger, annotationDir);
        var trainingService = new ModelTrainingService(_trainingLogger, trainingDir);
        var modelManager = new ModelManager(_modelManagerLogger, modelDir);
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);

        var userId = "insufficient-user";

        var jobId = await worker.SubmitJobAsync(userId, cancellationToken: CancellationToken.None);

        await Task.Delay(500);

        var finalStatus = worker.GetJobStatus(jobId);
        Assert.NotNull(finalStatus);
        Assert.Equal(TrainingJobState.Failed, finalStatus.State);
        Assert.NotNull(finalStatus.Error);
        Assert.Contains("No annotations", finalStatus.Error);
    }

    [Fact]
    public async Task EndToEnd_ModelDeploymentAndRevert_Success()
    {
        var modelDir = Path.Combine(_tempDirectory, "models-deploy");
        Directory.CreateDirectory(modelDir);

        var modelManager = new ModelManager(_modelManagerLogger, modelDir);

        var defaultModelPath = Path.Combine(modelDir, "frame-importance-model-default.zip");
        await File.WriteAllTextAsync(defaultModelPath, "default model v1");

        var activePath = await modelManager.GetActiveModelPathAsync(CancellationToken.None);
        Assert.Equal(defaultModelPath, activePath);

        var newModelPath = Path.Combine(_tempDirectory, "new-trained-model.zip");
        await File.WriteAllTextAsync(newModelPath, "trained model v1");

        var deployed = await modelManager.DeployModelAsync(newModelPath, CancellationToken.None);
        Assert.True(deployed);

        var activeAfterDeploy = await modelManager.GetActiveModelPathAsync(CancellationToken.None);
        Assert.NotEqual(defaultModelPath, activeAfterDeploy);
        Assert.Contains("frame-importance-model.zip", activeAfterDeploy);

        var reverted = await modelManager.RevertToDefaultAsync(CancellationToken.None);
        Assert.True(reverted);

        var activeAfterRevert = await modelManager.GetActiveModelPathAsync(CancellationToken.None);
        Assert.Equal(defaultModelPath, activeAfterRevert);
    }

    [Fact]
    public async Task EndToEnd_MultipleJobsSequential_Success()
    {
        var annotationDir = Path.Combine(_tempDirectory, "annotations-multi");
        var modelDir = Path.Combine(_tempDirectory, "models-multi");
        var trainingDir = Path.Combine(_tempDirectory, "training-multi");
        
        Directory.CreateDirectory(annotationDir);
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(trainingDir);

        var annotationStorage = new AnnotationStorageService(_annotationLogger, annotationDir);
        var trainingService = new ModelTrainingService(_trainingLogger, trainingDir);
        var modelManager = new ModelManager(_modelManagerLogger, modelDir);
        var worker = new MlTrainingWorker(_workerLogger, trainingService, modelManager, annotationStorage);

        var userId1 = "multi-user-1";
        var userId2 = "multi-user-2";
        
        var annotations1 = new List<AnnotationRecord>
        {
            new("user1-frame1.jpg", 0.8, DateTime.UtcNow),
            new("user1-frame2.jpg", 0.6, DateTime.UtcNow)
        };
        
        var annotations2 = new List<AnnotationRecord>
        {
            new("user2-frame1.jpg", 0.7, DateTime.UtcNow),
            new("user2-frame2.jpg", 0.9, DateTime.UtcNow)
        };

        await annotationStorage.StoreAnnotationsAsync(userId1, annotations1, CancellationToken.None);
        await annotationStorage.StoreAnnotationsAsync(userId2, annotations2, CancellationToken.None);

        var jobId1 = await worker.SubmitJobAsync(userId1, cancellationToken: CancellationToken.None);
        var jobId2 = await worker.SubmitJobAsync(userId2, cancellationToken: CancellationToken.None);

        await Task.Delay(3000);

        var status1 = worker.GetJobStatus(jobId1);
        var status2 = worker.GetJobStatus(jobId2);

        Assert.NotNull(status1);
        Assert.NotNull(status2);
        Assert.NotEqual(jobId1, jobId2);
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
