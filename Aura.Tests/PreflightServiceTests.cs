using Xunit;
using Aura.Api.Services;
using Aura.Providers.Validation;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Tests;

public class PreflightServiceTests
{
    private readonly Mock<IKeyStore> _mockKeyStore;
    private readonly ProviderSettings _providerSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProviderValidationService _validationService;

    public PreflightServiceTests()
    {
        _mockKeyStore = new Mock<IKeyStore>();
        _providerSettings = new ProviderSettings(NullLogger<ProviderSettings>.Instance);
        
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
        _httpClientFactory = mockHttpClientFactory.Object;
        
        var loggerFactory = NullLoggerFactory.Instance;
        _validationService = new ProviderValidationService(
            loggerFactory,
            _mockKeyStore.Object,
            _providerSettings,
            _httpClientFactory);
    }

    private PreflightService CreatePreflightService()
    {
        return new PreflightService(
            NullLogger<PreflightService>.Instance,
            _validationService,
            _mockKeyStore.Object,
            _providerSettings);
    }

    [Fact]
    public async Task RunPreflight_UnknownProfile_ReturnsFailure()
    {
        // Arrange
        var preflightService = CreatePreflightService();

        // Act
        var result = await preflightService.RunPreflightAsync("Invalid-Profile", CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Single(result.Stages);
        Assert.Equal("Profile", result.Stages[0].Stage);
        Assert.Equal(CheckStatus.Fail, result.Stages[0].Status);
        Assert.Contains("Unknown profile", result.Stages[0].Message);
    }

    [Fact]
    public async Task RunPreflight_FreeOnlyProfile_ChecksOllama()
    {
        // Arrange
        var preflightService = CreatePreflightService();

        // Act
        var result = await preflightService.RunPreflightAsync("Free-Only", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Stages.Length); // Script, TTS, Visuals
        
        var scriptStage = result.Stages[0];
        Assert.Equal("Script", scriptStage.Stage);
        Assert.Equal("Ollama", scriptStage.Provider);
        // Will likely fail since Ollama isn't running, but that's expected
    }

    [Fact]
    public async Task RunPreflight_ProMaxProfile_RequiresOpenAI()
    {
        // Arrange
        var preflightService = CreatePreflightService();

        // Act
        var result = await preflightService.RunPreflightAsync("Pro-Max", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Ok); // Should fail because OpenAI and ElevenLabs are not configured
        
        var scriptStage = result.Stages[0];
        Assert.Equal("Script", scriptStage.Stage);
        Assert.Equal(CheckStatus.Fail, scriptStage.Status);
        Assert.Equal("OpenAI", scriptStage.Provider);
        Assert.Contains("API key", scriptStage.Message);
    }

    [Fact]
    public async Task RunPreflight_BalancedMixProfile_ChecksProviders()
    {
        // Arrange
        var preflightService = CreatePreflightService();

        // Act
        var result = await preflightService.RunPreflightAsync("Balanced Mix", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Stages.Length); // Script, TTS, Visuals
        
        var scriptStage = result.Stages[0];
        Assert.Equal("Script", scriptStage.Stage);
        // Either OpenAI or Ollama depending on what's configured
        Assert.True(scriptStage.Provider == "OpenAI" || scriptStage.Provider == "Ollama" || scriptStage.Provider == "None");
    }

    [Fact]
    public async Task RunPreflight_WindowsTTS_AlwaysPasses()
    {
        // Arrange
        var preflightService = CreatePreflightService();

        // Act
        var result = await preflightService.RunPreflightAsync("Free-Only", CancellationToken.None);

        // Assert
        var ttsStage = result.Stages[1];
        Assert.Equal("TTS", ttsStage.Stage);
        Assert.Equal(CheckStatus.Pass, ttsStage.Status);
        Assert.Equal("Windows TTS", ttsStage.Provider);
    }

    [Fact]
    public async Task RunPreflight_VisualsStage_ReturnsValidProvider()
    {
        // Arrange
        var preflightService = CreatePreflightService();

        // Act
        var result = await preflightService.RunPreflightAsync("Balanced Mix", CancellationToken.None);

        // Assert
        var visualsStage = result.Stages[2];
        Assert.Equal("Visuals", visualsStage.Stage);
        // Should be either StableDiffusion or Stock depending on availability
        Assert.True(visualsStage.Provider == "StableDiffusion" || visualsStage.Provider == "Stock");
    }
}
