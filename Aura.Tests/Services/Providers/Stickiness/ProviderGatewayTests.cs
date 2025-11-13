using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Providers.Stickiness;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Services.Providers.Stickiness;

/// <summary>
/// Tests for ProviderGateway ensuring patience-centric behavior and provider locking
/// </summary>
public class ProviderGatewayTests
{
    private readonly ITestOutputHelper _output;

    public ProviderGatewayTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LockProvider_ValidParameters_CreatesAndStoresLock()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);

        // Act
        var lock_ = gateway.LockProvider(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation");

        // Assert
        Assert.NotNull(lock_);
        Assert.Equal("Ollama", lock_.ProviderName);
        Assert.Equal("local_llm", lock_.ProviderType);
        
        var retrievedLock = gateway.GetProviderLock("job-123");
        Assert.NotNull(retrievedLock);
        Assert.Equal(lock_.JobId, retrievedLock.JobId);
    }

    [Fact]
    public void LockProvider_DuplicateJobId_ThrowsInvalidOperationException()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            gateway.LockProvider("job-123", "OpenAI", "cloud_llm", "corr-789"));
    }

    [Fact]
    public void ValidateProviderRequest_MatchingProvider_ReturnsTrue()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456", true, "script-generation");

        // Act
        var result = gateway.ValidateProviderRequest("job-123", "Ollama", "script-generation", out var error);

        // Assert
        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateProviderRequest_DifferentProvider_ReturnsFalse()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456", true, "script-generation");

        // Act
        var result = gateway.ValidateProviderRequest("job-123", "OpenAI", "script-generation", out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("does not match", error);
        Assert.Contains("Ollama", error);
    }

    [Fact]
    public void ValidateProviderRequest_NoLock_ReturnsFalse()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);

        // Act
        var result = gateway.ValidateProviderRequest("job-999", "Ollama", "script-generation", out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("No provider lock found", error);
    }

    [Fact]
    public async Task ExecuteWithPatienceAsync_ValidProvider_ExecutesSuccessfully()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456", true, "script-generation");

        var strategy = new NoHeartbeatStrategy();
        var executionCount = 0;

        // Act
        var result = await gateway.ExecuteWithPatienceAsync(
            "job-123",
            "Ollama",
            "local_llm",
            "script-generation",
            "corr-456",
            strategy,
            async (ct) =>
            {
                executionCount++;
                await Task.Delay(100, ct);
                return "success";
            });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task ExecuteWithPatienceAsync_WrongProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456", true, "script-generation");

        var strategy = new NoHeartbeatStrategy();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            gateway.ExecuteWithPatienceAsync(
                "job-123",
                "OpenAI", // Wrong provider
                "cloud_llm",
                "script-generation",
                "corr-456",
                strategy,
                async (ct) =>
                {
                    await Task.Delay(100, ct);
                    return "success";
                }));
    }

    [Fact]
    public void RecordFallbackDecision_ValidDecision_StoresInHistory()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456", true);

        var decision = FallbackDecision.CreateUserRequested(
            "job-123",
            "Ollama",
            "OpenAI",
            5000,
            "corr-456",
            new[] { "script-generation" });

        // Act
        gateway.RecordFallbackDecision(decision);

        // Assert
        var history = gateway.GetFallbackHistory("job-123");
        Assert.Single(history);
        Assert.Equal(decision.FromProvider, history[0].FromProvider);
        Assert.Equal(decision.ToProvider, history[0].ToProvider);
        Assert.Equal(FallbackReasonCode.USER_REQUEST, history[0].ReasonCode);
    }

    [Fact]
    public void RecordFallbackDecision_UnlocksProvider()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        var lock_ = gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456", true);
        Assert.False(lock_.IsUnlocked);

        var decision = FallbackDecision.CreateUserRequested(
            "job-123",
            "Ollama",
            "OpenAI",
            5000,
            "corr-456");

        // Act
        gateway.RecordFallbackDecision(decision);

        // Assert
        Assert.True(lock_.IsUnlocked);
    }

    [Fact]
    public void ReleaseProviderLock_ExistingLock_RemovesAndReturnsTrue()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-123", "Ollama", "local_llm", "corr-456");

        // Act
        var result = gateway.ReleaseProviderLock("job-123");

        // Assert
        Assert.True(result);
        Assert.Null(gateway.GetProviderLock("job-123"));
    }

    [Fact]
    public void ReleaseProviderLock_NonExistentLock_ReturnsFalse()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);

        // Act
        var result = gateway.ReleaseProviderLock("job-999");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetStatistics_MultipleOperations_ReturnsCorrectCounts()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);
        
        gateway.LockProvider("job-1", "Ollama", "local_llm", "corr-1", true);
        gateway.LockProvider("job-2", "OpenAI", "cloud_llm", "corr-2", true);

        var decision1 = FallbackDecision.CreateUserRequested("job-1", "Ollama", "OpenAI", 5000, "corr-1");
        var decision2 = FallbackDecision.CreateAfterFatalError("job-2", "OpenAI", "Ollama", 3000, "corr-2", "API Error", true);

        gateway.RecordFallbackDecision(decision1);
        gateway.RecordFallbackDecision(decision2);

        // Act
        var stats = gateway.GetStatistics();

        // Assert
        Assert.Equal(2, stats.ActiveLocks);
        Assert.Equal(2, stats.TotalFallbackDecisions);
        Assert.Equal(1, stats.UserRequestedFallbacks);
        Assert.Equal(1, stats.ErrorTriggeredFallbacks);
    }

    [Fact]
    public void GetFallbackHistory_NoHistory_ReturnsEmpty()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var gateway = new ProviderGateway(NullLogger<ProviderGateway>.Instance, detector);

        // Act
        var history = gateway.GetFallbackHistory("job-999");

        // Assert
        Assert.Empty(history);
    }
}
