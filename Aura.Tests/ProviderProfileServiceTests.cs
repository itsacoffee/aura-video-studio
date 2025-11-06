using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aura.Tests;

public class ProviderProfileServiceTests
{
    private readonly Mock<ILogger<ProviderProfileService>> _loggerMock;
    private readonly ProviderSettings _providerSettings;
    private readonly Mock<IKeyStore> _keyStoreMock;
    private readonly HttpClient _httpClient;
    private readonly ProviderProfileService _service;

    public ProviderProfileServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProviderProfileService>>();
        var settingsLogger = new Mock<ILogger<ProviderSettings>>();
        _providerSettings = new ProviderSettings(settingsLogger.Object);
        _keyStoreMock = new Mock<IKeyStore>();
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });
        
        _httpClient = new HttpClient(handlerMock.Object);
        
        _service = new ProviderProfileService(
            _loggerMock.Object,
            _providerSettings,
            _keyStoreMock.Object,
            _httpClient);
    }

    [Fact]
    public async Task GetAllProfilesAsync_ReturnsBuiltInProfiles()
    {
        var profiles = await _service.GetAllProfilesAsync();
        
        Assert.NotEmpty(profiles);
        Assert.Contains(profiles, p => p.Id == "free-only");
        Assert.Contains(profiles, p => p.Id == "balanced-mix");
        Assert.Contains(profiles, p => p.Id == "pro-max");
    }

    [Fact]
    public async Task ValidateProfileAsync_MissingKeys_ReturnsInvalid()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>());

        var result = await _service.ValidateProfileAsync("pro-max");
        
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.MissingKeys);
        Assert.Contains("openai", result.MissingKeys);
    }

    [Fact]
    public async Task ValidateProfileAsync_AllKeysPresent_ChecksKeys()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>
            {
                ["openai"] = "sk-test",
                ["elevenlabs"] = "test-key",
                ["stabilityai"] = "test-key"
            });

        var result = await _service.ValidateProfileAsync("pro-max");
        
        Assert.Empty(result.MissingKeys);
        if (!result.IsValid)
        {
            Assert.True(result.Errors.Any(e => e.Contains("FFmpeg") || e.Contains("engine")));
        }
    }

    [Fact]
    public async Task GetRecommendedProfileAsync_NoKeys_ReturnsFreeOnly()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>());

        var profile = await _service.GetRecommendedProfileAsync();
        
        Assert.Equal("free-only", profile.Id);
        Assert.Equal(ProfileTier.FreeOnly, profile.Tier);
    }

    [Fact]
    public async Task GetRecommendedProfileAsync_HasOpenAI_ReturnsBalancedMix()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>
            {
                ["openai"] = "sk-test"
            });

        var profile = await _service.GetRecommendedProfileAsync();
        
        Assert.Equal("balanced-mix", profile.Id);
        Assert.Equal(ProfileTier.BalancedMix, profile.Tier);
    }

    [Fact]
    public async Task GetRecommendedProfileAsync_AllPremiumKeys_ReturnsProMax()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>
            {
                ["openai"] = "sk-test",
                ["elevenlabs"] = "test-key",
                ["stabilityai"] = "test-key"
            });

        var profile = await _service.GetRecommendedProfileAsync();
        
        Assert.Equal("pro-max", profile.Id);
        Assert.Equal(ProfileTier.ProMax, profile.Tier);
    }

    [Fact]
    public async Task SetActiveProfileAsync_ValidProfile_ReturnsTrue()
    {
        var result = await _service.SetActiveProfileAsync("free-only");
        
        Assert.True(result);
    }

    [Fact]
    public async Task SetActiveProfileAsync_InvalidProfile_ReturnsFalse()
    {
        var result = await _service.SetActiveProfileAsync("nonexistent");
        
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateProfileAsync_FreeOnlyProfile_ChecksEngines()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>());

        var result = await _service.ValidateProfileAsync("free-only");
        
        Assert.NotNull(result);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateProfileAsync_ProMaxWithKeys_ChecksEngines()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>
            {
                ["openai"] = "sk-test",
                ["elevenlabs"] = "test-key",
                ["stabilityai"] = "test-key"
            });

        var result = await _service.ValidateProfileAsync("pro-max");
        
        Assert.NotNull(result);
        if (!result.IsValid)
        {
            Assert.True(result.Errors.Any(e => e.Contains("FFmpeg") || e.Contains("engine")));
        }
    }

    [Fact]
    public async Task ValidateProfileAsync_BalancedMixMissingKey_ReturnsErrors()
    {
        _keyStoreMock.Setup(x => x.GetAllKeys())
            .Returns(new Dictionary<string, string>());

        var result = await _service.ValidateProfileAsync("balanced-mix");
        
        Assert.False(result.IsValid);
        Assert.Contains("openai", result.MissingKeys);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateProfileAsync_InvalidProfileId_ReturnsError()
    {
        var result = await _service.ValidateProfileAsync("invalid-profile");
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }
}
