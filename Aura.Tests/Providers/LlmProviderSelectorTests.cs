using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.Providers;

public class LlmProviderSelectorTests
{
    private readonly ILogger<LlmProviderSelector> _logger;
    private readonly Mock<ProviderSettings> _mockSettings;
    private readonly Mock<LlmProviderFactory> _mockFactory;
    private readonly ServiceCollection _services;

    public LlmProviderSelectorTests()
    {
        _logger = NullLogger<LlmProviderSelector>.Instance;
        _mockSettings = new Mock<ProviderSettings>(MockBehavior.Strict, NullLogger<ProviderSettings>.Instance);
        _mockFactory = new Mock<LlmProviderFactory>(
            MockBehavior.Strict,
            NullLogger<LlmProviderFactory>.Instance,
            Mock.Of<IHttpClientFactory>(),
            _mockSettings.Object,
            Mock.Of<IKeyStore>(),
            Mock.Of<IServiceProvider>());

        _services = new ServiceCollection();
    }

    [Fact]
    public async Task GetProviderAsync_OllamaAvailable_UsesOllama()
    {
        // Arrange
        _mockSettings.Setup(s => s.GetPreferredLlmProvider()).Returns((string?)null);

        var mockOllamaProvider = new Mock<ILlmProvider>();
        var mockOllamaDetection = new Mock<OllamaDetectionService>(
            MockBehavior.Strict,
            NullLogger<OllamaDetectionService>.Instance,
            Mock.Of<System.Net.Http.HttpClient>(),
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            "http://localhost:11434");

        var status = new OllamaStatus(
            IsRunning: true,
            IsInstalled: true,
            Version: "1.0.0",
            BaseUrl: "http://localhost:11434",
            ErrorMessage: null);

        mockOllamaDetection.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        _services.AddKeyedSingleton<ILlmProvider>("Ollama", (sp, key) => mockOllamaProvider.Object);
        _services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) => new Mock<ILlmProvider>().Object);
        _services.AddSingleton(_mockSettings.Object);
        _services.AddSingleton(_mockFactory.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var selector = new LlmProviderSelector(
            serviceProvider,
            _mockSettings.Object,
            _logger,
            mockOllamaDetection.Object,
            _mockFactory.Object);

        // Act
        var result = await selector.GetProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockOllamaProvider.Object, result);
        mockOllamaDetection.Verify(s => s.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProviderAsync_OllamaUnavailable_FallsBackToRuleBased()
    {
        // Arrange
        _mockSettings.Setup(s => s.GetPreferredLlmProvider()).Returns((string?)null);

        var mockOllamaProvider = new Mock<ILlmProvider>();
        var mockRuleBasedProvider = new Mock<ILlmProvider>();
        var mockOllamaDetection = new Mock<OllamaDetectionService>(
            MockBehavior.Strict,
            NullLogger<OllamaDetectionService>.Instance,
            Mock.Of<System.Net.Http.HttpClient>(),
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            "http://localhost:11434");

        var status = new OllamaStatus(
            IsRunning: false,
            IsInstalled: false,
            Version: null,
            BaseUrl: "http://localhost:11434",
            ErrorMessage: "Service not running");

        mockOllamaDetection.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        _services.AddKeyedSingleton<ILlmProvider>("Ollama", (sp, key) => mockOllamaProvider.Object);
        _services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) => mockRuleBasedProvider.Object);
        _services.AddSingleton(_mockSettings.Object);
        _services.AddSingleton(_mockFactory.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var selector = new LlmProviderSelector(
            serviceProvider,
            _mockSettings.Object,
            _logger,
            mockOllamaDetection.Object,
            _mockFactory.Object);

        // Act
        var result = await selector.GetProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockRuleBasedProvider.Object, result);
    }

    [Fact]
    public async Task GetProviderAsync_PreferredProviderConfigured_RespectsPriority()
    {
        // Arrange
        _mockSettings.Setup(s => s.GetPreferredLlmProvider()).Returns("OpenAI");

        var mockOpenAiProvider = new Mock<ILlmProvider>();
        var mockOllamaProvider = new Mock<ILlmProvider>();
        var mockRuleBasedProvider = new Mock<ILlmProvider>();

        _services.AddKeyedSingleton<ILlmProvider>("OpenAI", (sp, key) => mockOpenAiProvider.Object);
        _services.AddKeyedSingleton<ILlmProvider>("Ollama", (sp, key) => mockOllamaProvider.Object);
        _services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) => mockRuleBasedProvider.Object);
        _services.AddSingleton(_mockSettings.Object);
        _services.AddSingleton(_mockFactory.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var selector = new LlmProviderSelector(
            serviceProvider,
            _mockSettings.Object,
            _logger,
            null,
            _mockFactory.Object);

        // Act
        var result = await selector.GetProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockOpenAiProvider.Object, result);
    }

    [Fact]
    public async Task GetProviderAsync_PreferredProviderUnavailable_FallsBackToChain()
    {
        // Arrange
        _mockSettings.Setup(s => s.GetPreferredLlmProvider()).Returns("OpenAI");

        var mockOllamaProvider = new Mock<ILlmProvider>();
        var mockRuleBasedProvider = new Mock<ILlmProvider>();
        var mockOllamaDetection = new Mock<OllamaDetectionService>(
            MockBehavior.Strict,
            NullLogger<OllamaDetectionService>.Instance,
            Mock.Of<System.Net.Http.HttpClient>(),
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            "http://localhost:11434");

        var status = new OllamaStatus(
            IsRunning: true,
            IsInstalled: true,
            Version: "1.0.0",
            BaseUrl: "http://localhost:11434",
            ErrorMessage: null);

        mockOllamaDetection.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // OpenAI not registered (simulating unavailable)
        _services.AddKeyedSingleton<ILlmProvider>("Ollama", (sp, key) => mockOllamaProvider.Object);
        _services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) => mockRuleBasedProvider.Object);
        _services.AddSingleton(_mockSettings.Object);
        _services.AddSingleton(_mockFactory.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var selector = new LlmProviderSelector(
            serviceProvider,
            _mockSettings.Object,
            _logger,
            mockOllamaDetection.Object,
            _mockFactory.Object);

        // Act
        var result = await selector.GetProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockOllamaProvider.Object, result);
    }

    [Fact]
    public async Task GetProviderAsync_AllProvidersUnavailable_ThrowsException()
    {
        // Arrange
        _mockSettings.Setup(s => s.GetPreferredLlmProvider()).Returns((string?)null);

        // No providers registered
        _services.AddSingleton(_mockSettings.Object);
        _services.AddSingleton(_mockFactory.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var selector = new LlmProviderSelector(
            serviceProvider,
            _mockSettings.Object,
            _logger,
            null,
            _mockFactory.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => selector.GetProviderAsync());
    }

    [Fact]
    public async Task GetProviderAsync_RuleBasedAlwaysAvailable_AsFinalFallback()
    {
        // Arrange
        _mockSettings.Setup(s => s.GetPreferredLlmProvider()).Returns((string?)null);

        var mockRuleBasedProvider = new Mock<ILlmProvider>();
        var mockOllamaDetection = new Mock<OllamaDetectionService>(
            MockBehavior.Strict,
            NullLogger<OllamaDetectionService>.Instance,
            Mock.Of<System.Net.Http.HttpClient>(),
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            "http://localhost:11434");

        var status = new OllamaStatus(
            IsRunning: false,
            IsInstalled: false,
            Version: null,
            BaseUrl: "http://localhost:11434",
            ErrorMessage: "Service not running");

        mockOllamaDetection.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Only RuleBased registered
        _services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) => mockRuleBasedProvider.Object);
        _services.AddSingleton(_mockSettings.Object);
        _services.AddSingleton(_mockFactory.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var selector = new LlmProviderSelector(
            serviceProvider,
            _mockSettings.Object,
            _logger,
            mockOllamaDetection.Object,
            _mockFactory.Object);

        // Act
        var result = await selector.GetProviderAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockRuleBasedProvider.Object, result);
    }
}

