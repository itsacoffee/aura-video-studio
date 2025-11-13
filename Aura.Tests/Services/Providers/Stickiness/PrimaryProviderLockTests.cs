using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Providers.Stickiness;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Services.Providers.Stickiness;

/// <summary>
/// Tests for PrimaryProviderLock ensuring provider stickiness behavior
/// </summary>
public class PrimaryProviderLockTests
{
    private readonly ITestOutputHelper _output;

    public PrimaryProviderLockTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesLock()
    {
        // Arrange & Act
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation", "refinement");

        // Assert
        Assert.Equal("job-123", lock_.JobId);
        Assert.Equal("Ollama", lock_.ProviderName);
        Assert.Equal("local_llm", lock_.ProviderType);
        Assert.Equal("corr-456", lock_.CorrelationId);
        Assert.True(lock_.IsOverrideable);
        Assert.False(lock_.IsUnlocked);
        Assert.Null(lock_.UnlockedAt);
        Assert.Null(lock_.UnlockReason);
        Assert.Equal(2, lock_.ApplicableStages.Length);
    }

    [Fact]
    public void Constructor_NullJobId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new PrimaryProviderLock(
            null!,
            "Ollama",
            "local_llm",
            "corr-456"));
    }

    [Fact]
    public void Constructor_EmptyProviderName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new PrimaryProviderLock(
            "job-123",
            "",
            "local_llm",
            "corr-456"));
    }

    [Fact]
    public void TryUnlock_OverrideableLock_SuccessfullyUnlocks()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true);

        // Act
        var result = lock_.TryUnlock("USER_REQUEST");

        // Assert
        Assert.True(result);
        Assert.True(lock_.IsUnlocked);
        Assert.NotNull(lock_.UnlockedAt);
        Assert.Equal("USER_REQUEST", lock_.UnlockReason);
        Assert.True(lock_.GetLockDuration() > TimeSpan.Zero);
    }

    [Fact]
    public void TryUnlock_NonOverrideableLock_FailsToUnlock()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: false);

        // Act
        var result = lock_.TryUnlock("USER_REQUEST");

        // Assert
        Assert.False(result);
        Assert.False(lock_.IsUnlocked);
        Assert.Null(lock_.UnlockedAt);
        Assert.Null(lock_.UnlockReason);
    }

    [Fact]
    public void TryUnlock_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true);
        
        lock_.TryUnlock("USER_REQUEST");

        // Act
        var secondUnlockResult = lock_.TryUnlock("ANOTHER_REASON");

        // Assert
        Assert.False(secondUnlockResult);
        Assert.Equal("USER_REQUEST", lock_.UnlockReason); // Original reason preserved
    }

    [Fact]
    public void AppliesToStage_NoStagesSpecified_AlwaysReturnsTrue()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456");

        // Act & Assert
        Assert.True(lock_.AppliesToStage("script-generation"));
        Assert.True(lock_.AppliesToStage("refinement"));
        Assert.True(lock_.AppliesToStage("tts-synthesis"));
        Assert.True(lock_.AppliesToStage("any-stage"));
    }

    [Fact]
    public void AppliesToStage_SpecificStages_ReturnsCorrectly()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation", "refinement");

        // Act & Assert
        Assert.True(lock_.AppliesToStage("script-generation"));
        Assert.True(lock_.AppliesToStage("refinement"));
        Assert.False(lock_.AppliesToStage("tts-synthesis"));
        Assert.False(lock_.AppliesToStage("other-stage"));
    }

    [Fact]
    public void ValidateProvider_MatchingProvider_ReturnsTrue()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation");

        // Act
        var result = lock_.ValidateProvider("Ollama", "script-generation");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateProvider_DifferentProvider_ReturnsFalse()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation");

        // Act
        var result = lock_.ValidateProvider("OpenAI", "script-generation");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateProvider_UnlockedProvider_AllowsAnyProvider()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation");
        
        lock_.TryUnlock("USER_REQUEST");

        // Act
        var result1 = lock_.ValidateProvider("OpenAI", "script-generation");
        var result2 = lock_.ValidateProvider("Anthropic", "script-generation");

        // Assert
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public void ValidateProvider_StageNotApplicable_ReturnsTrue()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation");

        // Act - Different provider but stage not governed by lock
        var result = lock_.ValidateProvider("ElevenLabs", "tts-synthesis");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetLockDuration_ActiveLock_ReturnsPositiveDuration()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456");

        // Act
        Thread.Sleep(50); // Small delay to ensure measurable duration
        var duration = lock_.GetLockDuration();

        // Assert
        Assert.True(duration > TimeSpan.Zero);
        Assert.True(duration.TotalMilliseconds >= 50);
    }

    [Fact]
    public void ToString_ReturnsDescriptiveString()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true,
            "script-generation", "refinement");

        // Act
        var str = lock_.ToString();

        // Assert
        Assert.Contains("Ollama", str);
        Assert.Contains("local_llm", str);
        Assert.Contains("job-123", str);
        Assert.Contains("Active", str);
        Assert.Contains("script-generation, refinement", str);
    }

    [Fact]
    public void ToString_UnlockedProvider_ShowsUnlockedStatus()
    {
        // Arrange
        var lock_ = new PrimaryProviderLock(
            "job-123",
            "Ollama",
            "local_llm",
            "corr-456",
            isOverrideable: true);
        
        lock_.TryUnlock("USER_REQUEST");

        // Act
        var str = lock_.ToString();

        // Assert
        Assert.Contains("Unlocked", str);
        Assert.Contains("Ollama", str);
    }
}
