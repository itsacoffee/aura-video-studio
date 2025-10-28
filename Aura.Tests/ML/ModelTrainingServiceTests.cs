using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.FrameAnalysis;
using Aura.Core.Services.ML;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.ML;

public class ModelTrainingServiceTests
{
    private readonly Mock<ILogger<ModelTrainingService>> _mockLogger;
    private readonly string _tempModelDirectory;
    private readonly ModelTrainingService _service;

    public ModelTrainingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ModelTrainingService>>();
        
        // Use a temporary directory for testing
        _tempModelDirectory = Path.Combine(Path.GetTempPath(), $"aura-test-models-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempModelDirectory);
        
        _service = new ModelTrainingService(_mockLogger.Object, _tempModelDirectory);
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithValidAnnotations_ReturnsSuccess()
    {
        // Arrange
        var annotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("/path/to/frame1.jpg", 0.8),
            new FrameAnnotation("/path/to/frame2.jpg", 0.3),
            new FrameAnnotation("/path/to/frame3.jpg", 0.95)
        };

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(annotations);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.ModelPath);
            Assert.Equal(3, result.TrainingSamples);
            Assert.True(result.TrainingDuration.TotalSeconds > 0);
            Assert.Null(result.ErrorMessage);
            
            // Verify model file was created
            Assert.True(File.Exists(result.ModelPath));
        }
        finally
        {
            // Cleanup
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithEmptyAnnotations_ReturnsFailure()
    {
        // Arrange
        var emptyAnnotations = new List<FrameAnnotation>();

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(emptyAnnotations);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(0, result.TrainingSamples);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithInvalidRating_ReturnsFailure()
    {
        // Arrange
        var invalidAnnotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("/path/to/frame1.jpg", 1.5) // Invalid rating > 1.0
        };

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(invalidAnnotations);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithNegativeRating_ReturnsFailure()
    {
        // Arrange
        var invalidAnnotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("/path/to/frame1.jpg", -0.5) // Invalid negative rating
        };

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(invalidAnnotations);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithEmptyFramePath_ReturnsFailure()
    {
        // Arrange
        var invalidAnnotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("", 0.5) // Empty frame path
        };

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(invalidAnnotations);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_BacksUpExistingModel()
    {
        // Arrange
        var modelPath = Path.Combine(_tempModelDirectory, "frame-importance-model.zip");
        var backupPath = Path.Combine(_tempModelDirectory, "frame-importance-model.zip.backup");
        
        // Create an existing model file
        await File.WriteAllTextAsync(modelPath, "# Existing Model");
        
        var annotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("/path/to/frame1.jpg", 0.7)
        };

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(annotations);

            // Assert
            Assert.True(result.Success);
            Assert.True(File.Exists(backupPath), "Backup file should be created");
            
            var backupContent = await File.ReadAllTextAsync(backupPath);
            Assert.Contains("Existing Model", backupContent);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var annotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("/path/to/frame1.jpg", 0.5)
        };
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _service.TrainFrameImportanceModelAsync(annotations, cts.Token));
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithSpecialCharactersInPath_HandlesCorrectly()
    {
        // Arrange
        var annotations = new List<FrameAnnotation>
        {
            new FrameAnnotation("/path/with spaces/frame1.jpg", 0.6),
            new FrameAnnotation("/path/with,comma/frame2.jpg", 0.8),
            new FrameAnnotation("/path/with\"quotes\"/frame3.jpg", 0.4)
        };

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(annotations);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.TrainingSamples);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    [Fact]
    public async Task TrainFrameImportanceModelAsync_WithLargeDataset_CompletesSuccessfully()
    {
        // Arrange
        var annotations = Enumerable.Range(0, 100)
            .Select(i => new FrameAnnotation($"/path/to/frame{i}.jpg", i / 100.0))
            .ToList();

        try
        {
            // Act
            var result = await _service.TrainFrameImportanceModelAsync(annotations);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(100, result.TrainingSamples);
        }
        finally
        {
            CleanupTempDirectory();
        }
    }

    private void CleanupTempDirectory()
    {
        if (Directory.Exists(_tempModelDirectory))
        {
            try
            {
                Directory.Delete(_tempModelDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
