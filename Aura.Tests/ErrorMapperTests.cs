using Xunit;
using Aura.Core.Errors;
using Aura.Core.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Aura.Tests;

public class ErrorMapperTests
{
    [Fact]
    public void MapException_FileNotFoundException_ReturnsFfmpegNotFound()
    {
        // Arrange
        var ex = new FileNotFoundException("ffmpeg.exe not found");
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", "render");
        
        // Assert
        Assert.Equal("FFmpegNotFound", error.Code);
        Assert.Contains("FFmpeg", error.Message);
        Assert.Contains("Settings", error.Remediation);
    }
    
    [Fact]
    public void MapException_UnauthorizedAccess_ReturnsOutputDirectoryNotWritable()
    {
        // Arrange
        var ex = new UnauthorizedAccessException("Cannot write to output directory");
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", "mux");
        
        // Assert
        Assert.Equal("OutputDirectoryNotWritable", error.Code);
        Assert.Contains("write", error.Message);
        Assert.Contains("output", error.Remediation);
    }
    
    [Fact]
    public void MapException_TimeoutException_ReturnsStepTimeout()
    {
        // Arrange
        var ex = new TimeoutException("Operation timed out");
        var stepName = "narration";
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", stepName);
        
        // Assert
        Assert.Equal($"StepTimeout:{stepName}", error.Code);
        Assert.Contains(stepName, error.Message);
        Assert.Contains("Retry", error.Remediation);
    }
    
    [Fact]
    public void MapException_HttpRequestException_ReturnsTransientNetworkFailure()
    {
        // Arrange
        var ex = new HttpRequestException("Network error");
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", "broll");
        
        // Assert
        Assert.Equal("TransientNetworkFailure", error.Code);
        Assert.Contains("Network", error.Message);
        Assert.Contains("connectivity", error.Remediation);
    }
    
    [Fact]
    public void MapException_InvalidOperationWithGPU_ReturnsRequiresNvidiaGPU()
    {
        // Arrange
        var ex = new InvalidOperationException("CUDA device not found");
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", "render");
        
        // Assert
        Assert.Equal("RequiresNvidiaGPU", error.Code);
        Assert.Contains("GPU", error.Message);
        Assert.Contains("CPU", error.Remediation);
    }
    
    [Fact]
    public void MapException_PlatformNotSupported_ReturnsUnsupportedOS()
    {
        // Arrange
        var ex = new PlatformNotSupportedException("Not supported on Linux");
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", "step");
        
        // Assert
        Assert.StartsWith("UnsupportedOS:", error.Code);
        Assert.Contains("not supported", error.Message);
        Assert.Contains("alternative", error.Remediation);
    }
    
    [Fact]
    public void MissingApiKey_ReturnsCorrectCode()
    {
        // Arrange
        var keyName = "STABLE_KEY";
        
        // Act
        var error = ErrorMapper.MissingApiKey(keyName, "test-correlation");
        
        // Assert
        Assert.Equal($"MissingApiKey:{keyName}", error.Code);
        Assert.Contains(keyName, error.Message);
        Assert.Contains("Settings → Providers", error.Remediation);
    }
    
    [Fact]
    public void InvalidInput_ReturnsCorrectCode()
    {
        // Arrange
        var fieldName = "fps";
        var reason = "must be positive";
        
        // Act
        var error = ErrorMapper.InvalidInput(fieldName, reason, "test-correlation");
        
        // Assert
        Assert.Equal($"InvalidInput:{fieldName}", error.Code);
        Assert.Contains(fieldName, error.Message);
        Assert.Contains(reason, error.Message);
    }
    
    [Fact]
    public void FFmpegFailed_ReturnsCorrectCode()
    {
        // Arrange
        var exitCode = 1;
        var stderr = "Error: invalid input file";
        
        // Act
        var error = ErrorMapper.FFmpegFailed(exitCode, stderr, "test-correlation");
        
        // Assert
        Assert.Equal($"FFmpegFailedExitCode:{exitCode}", error.Code);
        Assert.Contains($"exit code {exitCode}", error.Message);
        Assert.Contains("FFmpeg", error.Remediation);
    }
    
    [Fact]
    public void MapException_UnknownException_ReturnsUnknownError()
    {
        // Arrange
        var ex = new ApplicationException("Some unknown error");
        
        // Act
        var error = ErrorMapper.MapException(ex, "test-correlation", "step");
        
        // Assert
        Assert.Equal("UnknownError", error.Code);
        Assert.Contains("Some unknown error", error.Message);
        Assert.Contains("logs", error.Remediation);
    }
}
