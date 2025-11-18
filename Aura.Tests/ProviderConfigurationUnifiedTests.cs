using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for unified provider configuration endpoints
/// Ensures GET /api/providers/config, POST /api/providers/config, and POST /api/providers/config/secrets work correctly
/// </summary>
[Collection("ProviderConfigurationUnifiedTests")]
public class ProviderConfigurationUnifiedTests : IDisposable
{
    private readonly Mock<ILogger<ProviderConfigurationController>> _mockLogger;
    private readonly Mock<ILogger<SecureStorageService>> _mockSecureLogger;
    private readonly ProviderSettings _providerSettings;
    private readonly ISecureStorageService _secureStorage;
    private readonly ProviderConfigurationController _controller;
    private readonly string _testDir;

    public ProviderConfigurationUnifiedTests()
    {
        _mockLogger = new Mock<ILogger<ProviderConfigurationController>>();
        _mockSecureLogger = new Mock<ILogger<SecureStorageService>>();
        
        // Create temporary test directory
        _testDir = Path.Combine(Path.GetTempPath(), "AuraProviderConfigTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);

        var providerLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _providerSettings = CreateTestProviderSettings(
            providerLoggerFactory.CreateLogger<ProviderSettings>(),
            _testDir);

        // Create real secure storage service
        _secureStorage = new SecureStorageService(_mockSecureLogger.Object);

        _controller = new ProviderConfigurationController(
            _mockLogger.Object,
            _providerSettings,
            _secureStorage);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    /// <summary>
    /// Create test ProviderSettings with a custom data path
    /// </summary>
    private static ProviderSettings CreateTestProviderSettings(ILogger<ProviderSettings> logger, string testDir)
    {
        // Set environment variable to point to test directory
        Environment.SetEnvironmentVariable("AURA_DATA_PATH", testDir);
        
        var settings = new ProviderSettings(logger);
        
        // Clear environment variable after initialization
        Environment.SetEnvironmentVariable("AURA_DATA_PATH", null);
        
        return settings;
    }

    [Fact]
    public async Task GetConfig_ReturnsDefaultConfiguration()
    {
        // Act
        var result = await Task.Run(() => _controller.GetConfig());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var config = Assert.IsType<ProviderConfigurationDto>(okResult.Value);
        
        Assert.NotNull(config.OpenAi);
        Assert.NotNull(config.Ollama);
        Assert.NotNull(config.StableDiffusion);
        Assert.NotNull(config.Anthropic);
        Assert.NotNull(config.Gemini);
        Assert.NotNull(config.ElevenLabs);
        
        // Verify secrets are not returned
        Assert.Null(config.OpenAi.ApiKey);
        Assert.Null(config.Anthropic.ApiKey);
        Assert.Null(config.Gemini.ApiKey);
        Assert.Null(config.ElevenLabs.ApiKey);
    }

    [Fact]
    public async Task GetConfig_ReturnsConfiguredEndpoints()
    {
        // Arrange
        _providerSettings.SetOpenAiEndpoint("https://custom.openai.com/v1");
        _providerSettings.SetOllamaUrl("http://192.168.1.100:11434");
        _providerSettings.SetStableDiffusionUrl("http://192.168.1.101:7860");

        // Act
        var result = await Task.Run(() => _controller.GetConfig());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var config = Assert.IsType<ProviderConfigurationDto>(okResult.Value);
        
        Assert.Equal("https://custom.openai.com/v1", config.OpenAi.Endpoint);
        Assert.Equal("http://192.168.1.100:11434", config.Ollama.Url);
        Assert.Equal("http://192.168.1.101:7860", config.StableDiffusion.Url);
    }

    [Fact]
    public async Task UpdateConfig_UpdatesOpenAiEndpoint()
    {
        // Arrange
        var updateDto = new ProviderConfigurationUpdateDto
        {
            OpenAi = new OpenAiConfigDto
            {
                Endpoint = "https://my-proxy.com/v1"
            }
        };

        // Act
        var result = await _controller.UpdateConfig(updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify the setting was persisted
        _providerSettings.Reload();
        Assert.Equal("https://my-proxy.com/v1", _providerSettings.GetOpenAiEndpoint());
    }

    [Fact]
    public async Task UpdateConfig_UpdatesOllamaUrlAndModel()
    {
        // Arrange
        var updateDto = new ProviderConfigurationUpdateDto
        {
            Ollama = new OllamaConfigDto
            {
                Url = "http://localhost:12345",
                Model = "llama3.1:70b"
            }
        };

        // Act
        var result = await _controller.UpdateConfig(updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify the settings were persisted
        _providerSettings.Reload();
        Assert.Equal("http://localhost:12345", _providerSettings.GetOllamaUrl());
        Assert.Equal("llama3.1:70b", _providerSettings.GetOllamaModel());
    }

    [Fact]
    public async Task UpdateConfig_UpdatesStableDiffusionUrl()
    {
        // Arrange
        var updateDto = new ProviderConfigurationUpdateDto
        {
            StableDiffusion = new StableDiffusionConfigDto
            {
                Url = "http://10.0.0.5:7860"
            }
        };

        // Act
        var result = await _controller.UpdateConfig(updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify the setting was persisted
        _providerSettings.Reload();
        Assert.Equal("http://10.0.0.5:7860", _providerSettings.GetStableDiffusionUrl());
    }

    [Fact]
    public async Task UpdateConfig_WithNullDto_ReturnsBadRequest()
    {
        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        var result = await _controller.UpdateConfig(null, CancellationToken.None);
#pragma warning restore CS8625

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problemDetails.Status);
    }

    [Fact]
    public async Task UpdateSecrets_UpdatesOpenAiApiKey()
    {
        // Arrange
        var secretsDto = new ProviderSecretsUpdateDto
        {
            OpenAiApiKey = "sk-test123456789"
        };

        // Act
        var result = await _controller.UpdateSecrets(secretsDto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify the secret was persisted
        _providerSettings.Reload();
        Assert.Equal("sk-test123456789", _providerSettings.GetOpenAiApiKey());
    }

    [Fact]
    public async Task UpdateSecrets_UpdatesMultipleApiKeys()
    {
        // Arrange
        var secretsDto = new ProviderSecretsUpdateDto
        {
            OpenAiApiKey = "sk-openai-key",
            AnthropicApiKey = "sk-anthropic-key",
            GeminiApiKey = "gemini-api-key",
            ElevenLabsApiKey = "elevenlabs-key"
        };

        // Act
        var result = await _controller.UpdateSecrets(secretsDto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify all secrets were persisted
        _providerSettings.Reload();
        Assert.Equal("sk-openai-key", _providerSettings.GetOpenAiApiKey());
        Assert.Equal("sk-anthropic-key", _providerSettings.GetAnthropicKey());
        Assert.Equal("gemini-api-key", _providerSettings.GetGeminiApiKey());
        Assert.Equal("elevenlabs-key", _providerSettings.GetElevenLabsApiKey());
    }

    [Fact]
    public async Task UpdateSecrets_WithEmptyStrings_IgnoresFields()
    {
        // Arrange - set an initial key
        _providerSettings.SetOpenAiKey("initial-key");
        
        var secretsDto = new ProviderSecretsUpdateDto
        {
            OpenAiApiKey = "", // Empty string should be ignored
            AnthropicApiKey = "new-anthropic-key"
        };

        // Act
        var result = await _controller.UpdateSecrets(secretsDto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify only non-empty keys were updated
        _providerSettings.Reload();
        Assert.Equal("initial-key", _providerSettings.GetOpenAiApiKey()); // Should remain unchanged
        Assert.Equal("new-anthropic-key", _providerSettings.GetAnthropicKey());
    }

    [Fact]
    public async Task UpdateSecrets_WithNullDto_ReturnsBadRequest()
    {
        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        var result = await _controller.UpdateSecrets(null, CancellationToken.None);
#pragma warning restore CS8625

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problemDetails.Status);
    }

    [Fact]
    public async Task UpdateConfig_DoesNotExposeSecretsInSubsequentGet()
    {
        // Arrange - set a secret
        var secretsDto = new ProviderSecretsUpdateDto
        {
            OpenAiApiKey = "sk-secret-key"
        };
        await _controller.UpdateSecrets(secretsDto, CancellationToken.None);

        // Act - get configuration
        var result = await Task.Run(() => _controller.GetConfig());

        // Assert - verify secret is not exposed
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var config = Assert.IsType<ProviderConfigurationDto>(okResult.Value);
        Assert.Null(config.OpenAi.ApiKey);
        
        // But verify it's still stored
        _providerSettings.Reload();
        Assert.Equal("sk-secret-key", _providerSettings.GetOpenAiApiKey());
    }
}
