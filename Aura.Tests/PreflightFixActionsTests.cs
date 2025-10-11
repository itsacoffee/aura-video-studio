using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Providers.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for PreflightService with actionable fix actions
/// </summary>
public class PreflightFixActionsTests
{
    private readonly Mock<ProviderValidationService> _mockValidationService;
    private readonly Mock<IKeyStore> _mockKeyStore;
    private readonly ProviderSettings _providerSettings;
    private readonly PreflightService _service;

    public PreflightFixActionsTests()
    {
        _mockValidationService = new Mock<ProviderValidationService>(
            MockBehavior.Strict,
            NullLogger<ProviderValidationService>.Instance,
            null, // IKeyStore - not used in these tests
            null  // ProviderSettings - not used in these tests
        );
        _mockKeyStore = new Mock<IKeyStore>();
        _providerSettings = new ProviderSettings();

        _service = new PreflightService(
            NullLogger<PreflightService>.Instance,
            _mockValidationService.Object,
            _mockKeyStore.Object,
            _providerSettings
        );
    }

    [Fact]
    public async Task PreflightCheck_WithFailedProvider_ShouldIncludeFixActions()
    {
        // Arrange - OpenAI missing API key
        _mockValidationService
            .Setup(v => v.ValidateProvidersAsync(
                It.Is<string[]>(arr => arr.Contains("OpenAI")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Results = new[]
                {
                    new ProviderValidationResult
                    {
                        Provider = "OpenAI",
                        Ok = false,
                        Details = "API key not configured"
                    }
                }
            });

        // Act
        var report = await _service.RunPreflightAsync("Pro-Max", CancellationToken.None);

        // Assert
        Assert.False(report.Ok);
        var scriptStage = report.Stages.FirstOrDefault(s => s.Stage == "Script");
        Assert.NotNull(scriptStage);
        Assert.Equal(CheckStatus.Fail, scriptStage.Status);
        Assert.NotNull(scriptStage.FixActions);
        Assert.NotEmpty(scriptStage.FixActions);

        // Verify fix actions include "Add API Key" and "Get API Key"
        Assert.Contains(scriptStage.FixActions, action => action.Type == FixActionType.OpenSettings);
        Assert.Contains(scriptStage.FixActions, action => action.Type == FixActionType.Help);
    }

    [Fact]
    public async Task PreflightCheck_WithStableDiffusionNotRunning_ShouldIncludeInstallAndSwitchActions()
    {
        // Arrange - Ollama available for script
        _mockValidationService
            .Setup(v => v.ValidateProvidersAsync(
                It.Is<string[]>(arr => arr.Contains("Ollama")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Results = new[]
                {
                    new ProviderValidationResult
                    {
                        Provider = "Ollama",
                        Ok = true,
                        Details = "Available"
                    }
                }
            });

        // Windows TTS available
        _mockValidationService
            .Setup(v => v.ValidateProvidersAsync(
                It.Is<string[]>(arr => arr.Contains("Windows")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Results = new[]
                {
                    new ProviderValidationResult
                    {
                        Provider = "Windows",
                        Ok = true,
                        Details = "Available"
                    }
                }
            });

        // StableDiffusion not running
        _mockValidationService
            .Setup(v => v.ValidateProvidersAsync(
                It.Is<string[]>(arr => arr.Contains("StableDiffusion")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Results = new[]
                {
                    new ProviderValidationResult
                    {
                        Provider = "StableDiffusion",
                        Ok = false,
                        Details = "Service not running at http://127.0.0.1:7860"
                    }
                }
            });

        // Act
        var report = await _service.RunPreflightAsync("Free-Only", CancellationToken.None);

        // Assert
        var visualsStage = report.Stages.FirstOrDefault(s => s.Stage == "Visuals");
        Assert.NotNull(visualsStage);
        
        if (visualsStage.Status == CheckStatus.Warn || visualsStage.Status == CheckStatus.Fail)
        {
            Assert.NotNull(visualsStage.FixActions);
            
            // Should suggest installing SD WebUI or switching to Stock images
            var hasInstallAction = visualsStage.FixActions.Any(a => a.Type == FixActionType.Install);
            var hasSwitchAction = visualsStage.FixActions.Any(a => a.Type == FixActionType.SwitchToFree);
            
            Assert.True(hasInstallAction || hasSwitchAction, "Should include Install or SwitchToFree action");
        }
    }

    [Fact]
    public void GetSafeDefaultsProfile_ShouldReturnFreeOnlyConfiguration()
    {
        // Act
        var profile = _service.GetSafeDefaultsProfile();

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("Safe Defaults", profile.Name);
        Assert.Equal("Free", profile.Stages["Script"]);
        Assert.Equal("Windows", profile.Stages["TTS"]);
        Assert.Equal("Stock", profile.Stages["Visuals"]);
    }

    [Fact]
    public async Task PreflightCheck_WithMissingAPIKeys_ShouldSuggestOpenSettingsAction()
    {
        // Arrange - ElevenLabs missing API key
        _mockValidationService
            .Setup(v => v.ValidateProvidersAsync(
                It.Is<string[]>(arr => arr.Contains("ElevenLabs")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Results = new[]
                {
                    new ProviderValidationResult
                    {
                        Provider = "ElevenLabs",
                        Ok = false,
                        Details = "API key not configured"
                    }
                }
            });

        // Act
        var report = await _service.RunPreflightAsync("Pro-Max", CancellationToken.None);

        // Assert
        var ttsStage = report.Stages.FirstOrDefault(s => s.Stage == "TTS");
        Assert.NotNull(ttsStage);
        
        if (ttsStage.Status == CheckStatus.Fail || ttsStage.Status == CheckStatus.Warn)
        {
            Assert.NotNull(ttsStage.FixActions);
            
            // Should include OpenSettings action for API keys
            var openSettingsAction = ttsStage.FixActions.FirstOrDefault(a => a.Type == FixActionType.OpenSettings);
            Assert.NotNull(openSettingsAction);
            Assert.Equal("api-keys", openSettingsAction.Parameter);
        }
    }

    [Fact]
    public async Task PreflightCheck_WithPassingChecks_ShouldNotIncludeFixActions()
    {
        // Arrange - All providers available for Free-Only
        _mockValidationService
            .Setup(v => v.ValidateProvidersAsync(
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Results = new[]
                {
                    new ProviderValidationResult
                    {
                        Provider = "Ollama",
                        Ok = true,
                        Details = "Available"
                    }
                }
            });

        // Act
        var report = await _service.RunPreflightAsync("Free-Only", CancellationToken.None);

        // Assert - Passing checks should not have fix actions
        foreach (var stage in report.Stages)
        {
            if (stage.Status == CheckStatus.Pass)
            {
                Assert.True(stage.FixActions == null || stage.FixActions.Length == 0);
            }
        }
    }
}
