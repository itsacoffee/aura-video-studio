using Aura.Core.Services.Providers.Stickiness;
using Aura.Tests.TestUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aura.Tests.Providers.Stickiness;

public class ProviderProfileLockServiceTests : IDisposable
{
    private readonly Mock<ILogger<ProviderProfileLockService>> _loggerMock;
    private readonly ProviderSettingsTestContext _settingsContext;
    private readonly ProviderProfileLockService _service;

    public ProviderProfileLockServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProviderProfileLockService>>();
        _settingsContext = new ProviderSettingsTestContext();
        _service = new ProviderProfileLockService(_loggerMock.Object, _settingsContext.Settings);
    }

    [Fact]
    public async Task SetProfileLockAsync_CreatesSessionLock()
    {
        // Arrange
        var jobId = "test-job-123";
        var providerName = "Ollama";
        var providerType = "local_llm";

        // Act
        var lock_ = await _service.SetProfileLockAsync(
            jobId,
            providerName,
            providerType,
            isEnabled: true,
            offlineModeEnabled: true,
            isSessionLevel: true,
            ct: CancellationToken.None);

        // Assert
        Assert.NotNull(lock_);
        Assert.Equal(jobId, lock_.JobId);
        Assert.Equal(providerName, lock_.ProviderName);
        Assert.Equal(providerType, lock_.ProviderType);
        Assert.True(lock_.IsEnabled);
        Assert.True(lock_.OfflineModeEnabled);
    }

    [Fact]
    public async Task GetProfileLock_ReturnsSessionLockOverProjectLock()
    {
        // Arrange
        var jobId = "test-job-123";

        await _service.SetProfileLockAsync(
            jobId,
            "OpenAI",
            "cloud_llm",
            isEnabled: true,
            isSessionLevel: false,
            ct: CancellationToken.None);

        await _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            isSessionLevel: true,
            ct: CancellationToken.None);

        // Act
        var lock_ = _service.GetProfileLock(jobId);

        // Assert
        Assert.NotNull(lock_);
        Assert.Equal("Ollama", lock_.ProviderName);
    }

    [Fact]
    public void ValidateProviderRequest_AllowsMatchingProvider()
    {
        // Arrange
        var jobId = "test-job-123";
        _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            ct: CancellationToken.None).Wait();

        // Act
        var isValid = _service.ValidateProviderRequest(
            jobId,
            "Ollama",
            "script_generation",
            providerRequiresNetwork: false,
            out var error);

        // Assert
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateProviderRequest_BlocksNonMatchingProvider()
    {
        // Arrange
        var jobId = "test-job-123";
        _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            ct: CancellationToken.None).Wait();

        // Act
        var isValid = _service.ValidateProviderRequest(
            jobId,
            "OpenAI",
            "script_generation",
            providerRequiresNetwork: true,
            out var error);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("does not match locked provider", error);
    }

    [Fact]
    public void ValidateProviderRequest_BlocksNetworkProviderInOfflineMode()
    {
        // Arrange
        var jobId = "test-job-123";
        _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            offlineModeEnabled: true,
            ct: CancellationToken.None).Wait();

        // Act
        var isValid = _service.ValidateProviderRequest(
            jobId,
            "Ollama",
            "script_generation",
            providerRequiresNetwork: true, // Simulate network requirement
            out var error);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("offline mode", error.ToLower());
    }

    [Fact]
    public void ValidateProviderRequest_AllowsOfflineProviderInOfflineMode()
    {
        // Arrange
        var jobId = "test-job-123";
        _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            offlineModeEnabled: true,
            ct: CancellationToken.None).Wait();

        // Act
        var isValid = _service.ValidateProviderRequest(
            jobId,
            "Ollama",
            "script_generation",
            providerRequiresNetwork: false,
            out var error);

        // Assert
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void UnlockProfileLock_DisablesLock()
    {
        // Arrange
        var jobId = "test-job-123";
        _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            ct: CancellationToken.None).Wait();

        // Act
        var unlocked = _service.UnlockProfileLock(jobId, isSessionLevel: true);
        var lock_ = _service.GetProfileLock(jobId);

        // Assert
        Assert.True(unlocked);
        Assert.NotNull(lock_);
        Assert.False(lock_.IsEnabled);
    }

    [Fact]
    public void RemoveProfileLock_RemovesLockCompletely()
    {
        // Arrange
        var jobId = "test-job-123";
        _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            ct: CancellationToken.None).Wait();

        // Act
        var removed = _service.RemoveProfileLock(jobId, isSessionLevel: true);
        var lock_ = _service.GetProfileLock(jobId);

        // Assert
        Assert.True(removed);
        Assert.Null(lock_);
    }

    [Theory]
    [InlineData("Ollama", true)]
    [InlineData("RuleBased", true)]
    [InlineData("Windows", true)]
    [InlineData("Piper", true)]
    [InlineData("Mimic3", true)]
    [InlineData("LocalSD", true)]
    [InlineData("Stock", true)]
    [InlineData("OpenAI", false)]
    [InlineData("ElevenLabs", false)]
    [InlineData("Anthropic", false)]
    public void IsOfflineCompatible_CorrectlyIdentifiesProviders(string providerName, bool expectedCompatible)
    {
        // Act
        var isCompatible = _service.IsOfflineCompatible(providerName, out var message);

        // Assert
        Assert.Equal(expectedCompatible, isCompatible);
        if (!expectedCompatible)
        {
            Assert.NotNull(message);
            Assert.Contains("requires network access", message);
        }
    }

    [Fact]
    public void GetStatistics_ReturnsAccurateStats()
    {
        // Arrange
        _service.SetProfileLockAsync("job1", "Ollama", "local_llm", true, true, isSessionLevel: true).Wait();
        _service.SetProfileLockAsync("job2", "OpenAI", "cloud_llm", true, false, isSessionLevel: true).Wait();
        _service.SetProfileLockAsync("job3", "Piper", "tts", false, false, isSessionLevel: false).Wait();

        // Act
        var stats = _service.GetStatistics();

        // Assert
        Assert.Equal(2, stats.TotalSessionLocks);
        Assert.Equal(1, stats.TotalProjectLocks);
        Assert.Equal(2, stats.EnabledSessionLocks);
        Assert.Equal(0, stats.EnabledProjectLocks);
        Assert.Equal(1, stats.OfflineModeLocksCount);
    }

    [Fact]
    public async Task SessionLockPersistence_SurvivesServiceRestart()
    {
        // Arrange
        var jobId = "persistent-job";
        await _service.SetProfileLockAsync(
            jobId,
            "Ollama",
            "local_llm",
            isEnabled: true,
            offlineModeEnabled: true,
            isSessionLevel: true);

        // Simulate service restart by creating new instance sharing same settings
        var newService = new ProviderProfileLockService(_loggerMock.Object, _settingsContext.Settings);

        // Act
        var lock_ = newService.GetProfileLock(jobId);

        // Assert
        Assert.NotNull(lock_);
        Assert.Equal("Ollama", lock_.ProviderName);
        Assert.True(lock_.IsEnabled);
        Assert.True(lock_.OfflineModeEnabled);
    }

    public void Dispose()
    {
        _settingsContext.Dispose();
    }
}
