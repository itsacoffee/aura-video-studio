using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Errors;
using Xunit;

namespace Aura.Tests.Orchestrator;

/// <summary>
/// Unit tests for VideoGenerationException error handling and stage error propagation.
/// Tests verify that the exception correctly identifies retryable errors and provides
/// appropriate suggested actions for different pipeline stages.
/// </summary>
public class VideoOrchestratorErrorHandlingTests
{
    [Fact]
    public void VideoGenerationException_ContainsStageInfo()
    {
        var ex = new VideoGenerationException("Test error", "Script", new TimeoutException());

        Assert.Equal("Script", ex.Stage);
        Assert.True(ex.IsRetryable);
        Assert.NotNull(ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_HttpError_IsRetryable()
    {
        var httpEx = new HttpRequestException("Connection refused");
        var ex = new VideoGenerationException("Test error", "Script", httpEx);

        Assert.True(ex.IsRetryable);
        Assert.Contains("Ollama", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_CancellationError_NotRetryable()
    {
        var cancelEx = new OperationCanceledException();
        var ex = new VideoGenerationException("Cancelled", "Script", cancelEx);

        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public void VideoGenerationException_TTSStage_HasCorrectSuggestion()
    {
        var httpEx = new HttpRequestException("Connection refused");
        var ex = new VideoGenerationException("Test error", "TTS", httpEx);

        Assert.Contains("TTS service", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_RenderStage_HasCorrectSuggestion()
    {
        var httpEx = new HttpRequestException("Connection refused");
        var ex = new VideoGenerationException("Test error", "Render", httpEx);

        Assert.Contains("FFmpeg", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_ImageStage_HasCorrectSuggestion()
    {
        var httpEx = new HttpRequestException("Connection refused");
        var ex = new VideoGenerationException("Test error", "Image", httpEx);

        Assert.Contains("image provider", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_ImagesStage_HasCorrectSuggestion()
    {
        // Test plural form "Images" stage name
        var httpEx = new HttpRequestException("Connection refused");
        var ex = new VideoGenerationException("Test error", "Images", httpEx);

        Assert.Contains("image provider", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_TimeoutError_IsRetryable()
    {
        var timeoutEx = new TimeoutException("Operation timed out");
        var ex = new VideoGenerationException("Test error", "Script", timeoutEx);

        Assert.True(ex.IsRetryable);
        Assert.Contains("too long", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_IOException_IsRetryable()
    {
        var ioEx = new System.IO.IOException("Disk full");
        var ex = new VideoGenerationException("Test error", "Render", ioEx);

        Assert.True(ex.IsRetryable);
    }

    [Fact]
    public void VideoGenerationException_NoInnerException_NotRetryable()
    {
        var ex = new VideoGenerationException("Test error", "Script", null);

        Assert.False(ex.IsRetryable);
        Assert.Null(ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_UnknownStage_HasGenericSuggestion()
    {
        var httpEx = new HttpRequestException("Connection refused");
        var ex = new VideoGenerationException("Test error", "UnknownStage", httpEx);

        Assert.Contains("required services", ex.SuggestedAction);
    }

    [Fact]
    public void VideoGenerationException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("Inner error message");
        var ex = new VideoGenerationException("Outer error", "Script", inner);

        Assert.Equal(inner, ex.InnerException);
        Assert.Equal("Outer error", ex.Message);
    }

    [Fact]
    public void VideoGenerationException_GenericException_NotRetryable()
    {
        var genericEx = new ArgumentException("Bad argument");
        var ex = new VideoGenerationException("Test error", "Script", genericEx);

        Assert.False(ex.IsRetryable);
        Assert.Null(ex.SuggestedAction);
    }
}
