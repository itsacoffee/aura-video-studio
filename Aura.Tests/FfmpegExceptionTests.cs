using System;
using System.Linq;
using Aura.Core.Errors;
using Xunit;

namespace Aura.Tests;

public class FfmpegExceptionTests
{
    [Fact]
    public void NotFound_Should_CreateCorrectException()
    {
        // Act
        var exception = FfmpegException.NotFound("/path/to/ffmpeg", "test-correlation-id");

        // Assert
        Assert.Equal(FfmpegErrorCategory.NotFound, exception.Category);
        Assert.Contains("/path/to/ffmpeg", exception.Message);
        Assert.Equal("test-correlation-id", exception.CorrelationId);
        Assert.Contains("E302", exception.ErrorCode);
        Assert.NotEmpty(exception.SuggestedActions);
        Assert.Contains(exception.SuggestedActions, a => a.Contains("Download Center"));
    }

    [Fact]
    public void FromProcessFailure_Should_CategorizeEncoderNotFound()
    {
        // Arrange
        var stderr = "Error: Encoder 'libx265' not found";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr, "job-123", "corr-456");

        // Assert
        Assert.Equal(FfmpegErrorCategory.EncoderNotFound, exception.Category);
        Assert.Contains("encoder", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, exception.ExitCode);
        Assert.Equal("job-123", exception.JobId);
        Assert.Equal("corr-456", exception.CorrelationId);
        Assert.Contains(exception.SuggestedActions, a => a.Contains("encoder"));
    }

    [Fact]
    public void FromProcessFailure_Should_CategorizePermissionDenied()
    {
        // Arrange
        var stderr = "Error: Permission denied when trying to write output.mp4";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.PermissionDenied, exception.Category);
        Assert.Contains("permission", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exception.SuggestedActions, a => a.Contains("permissions"));
    }

    [Fact]
    public void FromProcessFailure_Should_CategorizeInvalidInput()
    {
        // Arrange
        var stderr = "Error: Invalid data found when processing input";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.InvalidInput, exception.Category);
        Assert.Contains("input", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exception.SuggestedActions, a => a.Contains("input"));
    }

    [Fact]
    public void FromProcessFailure_Should_CategorizeCrashed()
    {
        // Arrange - negative exit codes often indicate crashes
        var stderr = "Segmentation fault";

        // Act
        var exception = FfmpegException.FromProcessFailure(-11, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.Crashed, exception.Category);
        Assert.Contains("crashed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exception.SuggestedActions, a => a.Contains("corrupted") || a.Contains("dependencies"));
    }

    [Fact]
    public void FromProcessFailure_Should_CategorizeCorrupted()
    {
        // Arrange
        var stderr = "Error: Corrupted header detected in input.wav";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.Corrupted, exception.Category);
        Assert.Contains("corrupted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromProcessFailure_Should_TruncateLongStderr()
    {
        // Arrange - create stderr larger than 64KB
        var longStderr = new string('x', 100 * 1024); // 100KB

        // Act
        var exception = FfmpegException.FromProcessFailure(1, longStderr);

        // Assert
        Assert.NotNull(exception.Stderr);
        Assert.True(exception.Stderr.Length <= 64 * 1024 + 100); // 64KB + some overhead for truncation marker
        Assert.Contains("truncated", exception.Stderr);
    }

    [Fact]
    public void FromProcessFailure_Should_HandleNullStderr()
    {
        // Act
        var exception = FfmpegException.FromProcessFailure(1, null);

        // Assert
        Assert.Equal(FfmpegErrorCategory.ProcessFailed, exception.Category);
        Assert.Null(exception.Stderr);
        Assert.NotEmpty(exception.SuggestedActions);
    }

    [Fact]
    public void FromProcessFailure_Should_HandleEmptyStderr()
    {
        // Act
        var exception = FfmpegException.FromProcessFailure(1, string.Empty);

        // Assert
        Assert.Equal(FfmpegErrorCategory.ProcessFailed, exception.Category);
        Assert.NotNull(exception.SuggestedActions);
    }

    [Fact]
    public void ErrorCode_Should_IncludeExitCode()
    {
        // Act
        var exception = FfmpegException.FromProcessFailure(42, "Some error");

        // Assert
        Assert.Contains("42", exception.ErrorCode);
    }

    [Fact]
    public void ErrorCode_Should_IncludeCategoryCode()
    {
        // Arrange & Act
        var notFoundEx = FfmpegException.NotFound();
        var processEx = FfmpegException.FromProcessFailure(1, "encoder not found");

        // Assert
        Assert.Contains("E302", notFoundEx.ErrorCode); // NotFound
        Assert.Contains("E305", processEx.ErrorCode); // EncoderNotFound
    }

    [Fact]
    public void ParseErrorPatterns_Should_ExtractErrorMessages()
    {
        // Arrange
        var stderr = @"
[error] Could not open file
Error: Invalid argument
Some other output
Error writing header: Permission denied
";

        // Act
        var patterns = FfmpegException.ParseErrorPatterns(stderr);

        // Assert
        Assert.NotEmpty(patterns);
        Assert.True(patterns.Count >= 1);
    }

    [Fact]
    public void ParseErrorPatterns_Should_HandleNullStderr()
    {
        // Act
        var patterns = FfmpegException.ParseErrorPatterns(null);

        // Assert
        Assert.Empty(patterns);
    }

    [Fact]
    public void SuggestedActions_Should_IncludeDiskSpaceWhenRelevant()
    {
        // Arrange
        var stderr = "Error: No space left on disk";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Contains(exception.SuggestedActions, a => a.Contains("disk space"));
    }

    [Fact]
    public void SuggestedActions_Should_IncludeMemoryWhenRelevant()
    {
        // Arrange
        var stderr = "Error: malloc failed - out of memory";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Contains(exception.SuggestedActions, a => a.Contains("memory"));
    }

    [Fact]
    public void SuggestedActions_Should_IncludeGpuWhenRelevant()
    {
        // Arrange
        var stderr = "Error: CUDA error - no suitable GPU found";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Contains(exception.SuggestedActions, a => a.Contains("GPU") || a.Contains("driver"));
    }

    [Fact]
    public void Constructor_Should_AcceptInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FfmpegException(
            "Test message",
            FfmpegErrorCategory.Unknown,
            innerException: innerException);

        // Assert
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_Should_AcceptCustomSuggestedActions()
    {
        // Arrange
        var customActions = new[] { "Action 1", "Action 2" };

        // Act
        var exception = new FfmpegException(
            "Test message",
            FfmpegErrorCategory.Unknown,
            suggestedActions: customActions);

        // Assert
        Assert.Equal(customActions, exception.SuggestedActions);
    }

    [Fact]
    public void FromProcessFailure_Should_HandleWindowsAccessDenied()
    {
        // Arrange
        var stderr = "Error: Access is denied";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.PermissionDenied, exception.Category);
    }

    [Fact]
    public void FromProcessFailure_Should_HandleMoovAtomError()
    {
        // Arrange
        var stderr = "Error: moov atom not found";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.InvalidInput, exception.Category);
    }

    [Fact]
    public void FromProcessFailure_Should_HandleUnknownEncoderError()
    {
        // Arrange
        var stderr = "Error: Unknown encoder 'xyz'";

        // Act
        var exception = FfmpegException.FromProcessFailure(1, stderr);

        // Assert
        Assert.Equal(FfmpegErrorCategory.EncoderNotFound, exception.Category);
    }
}
