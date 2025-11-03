using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.UserPreferences;
using Aura.Core.Services.UserPreferences;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class AIBehaviorSettingsTests
{
    private readonly UserPreferencesService _service;
    private readonly string _testDirectory;

    public AIBehaviorSettingsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AuraTests_{Guid.NewGuid()}");
        var logger = new Mock<ILogger<UserPreferencesService>>();
        _service = new UserPreferencesService(logger.Object, _testDirectory);
    }

    [Fact]
    public async Task SaveAIBehaviorSettings_ShouldCreateNewSetting()
    {
        // Arrange
        var settings = CreateTestSettings("Test Settings 1");

        // Act
        var saved = await _service.SaveAIBehaviorSettingsAsync(settings, CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(settings.Name, saved.Name);
        Assert.Equal(settings.CreativityVsAdherence, saved.CreativityVsAdherence);
        Assert.Equal(settings.EnableChainOfThought, saved.EnableChainOfThought);
    }

    [Fact]
    public async Task GetAIBehaviorSettings_ShouldReturnAllSettings()
    {
        // Arrange
        var settings1 = CreateTestSettings("Test Settings 1");
        var settings2 = CreateTestSettings("Test Settings 2");
        
        await _service.SaveAIBehaviorSettingsAsync(settings1, CancellationToken.None);
        await _service.SaveAIBehaviorSettingsAsync(settings2, CancellationToken.None);

        // Act
        var allSettings = await _service.GetAIBehaviorSettingsAsync(CancellationToken.None);

        // Assert
        Assert.NotEmpty(allSettings);
        Assert.Contains(allSettings, s => s.Name == "Test Settings 1");
        Assert.Contains(allSettings, s => s.Name == "Test Settings 2");
    }

    [Fact]
    public async Task GetAIBehaviorSetting_ShouldReturnSpecificSetting()
    {
        // Arrange
        var settings = CreateTestSettings("Test Settings");
        await _service.SaveAIBehaviorSettingsAsync(settings, CancellationToken.None);

        // Act
        var retrieved = await _service.GetAIBehaviorSettingAsync(settings.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(settings.Id, retrieved.Id);
        Assert.Equal(settings.Name, retrieved.Name);
    }

    [Fact]
    public async Task UpdateAIBehaviorSettings_ShouldUpdateExisting()
    {
        // Arrange
        var settings = CreateTestSettings("Original Name");
        await _service.SaveAIBehaviorSettingsAsync(settings, CancellationToken.None);

        settings.Name = "Updated Name";
        settings.CreativityVsAdherence = 0.8;

        // Act
        var updated = await _service.SaveAIBehaviorSettingsAsync(settings, CancellationToken.None);

        // Assert
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(0.8, updated.CreativityVsAdherence);
    }

    [Fact]
    public async Task DeleteAIBehaviorSettings_ShouldRemoveSetting()
    {
        // Arrange
        var settings = CreateTestSettings("To Delete");
        await _service.SaveAIBehaviorSettingsAsync(settings, CancellationToken.None);

        // Act
        var deleted = await _service.DeleteAIBehaviorSettingsAsync(settings.Id, CancellationToken.None);

        // Assert
        Assert.True(deleted);
        
        var retrieved = await _service.GetAIBehaviorSettingAsync(settings.Id, CancellationToken.None);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteNonExistentSettings_ShouldReturnFalse()
    {
        // Act
        var deleted = await _service.DeleteAIBehaviorSettingsAsync("non-existent-id", CancellationToken.None);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void AIBehaviorSettings_DefaultValues_ShouldBeValid()
    {
        // Act
        var settings = new AIBehaviorSettings();

        // Assert
        Assert.NotNull(settings.Id);
        Assert.NotEqual(string.Empty, settings.Id);
        Assert.Equal(0.5, settings.CreativityVsAdherence);
        Assert.False(settings.EnableChainOfThought);
        Assert.False(settings.ShowPromptsBeforeSending);
        Assert.NotNull(settings.ScriptGeneration);
        Assert.NotNull(settings.SceneDescription);
        Assert.NotNull(settings.ContentOptimization);
        Assert.NotNull(settings.Translation);
        Assert.NotNull(settings.QualityAnalysis);
    }

    [Fact]
    public void LLMStageParameters_DefaultValues_ShouldBeValid()
    {
        // Act
        var parameters = new LLMStageParameters();

        // Assert
        Assert.Equal(0.7, parameters.Temperature);
        Assert.Equal(0.9, parameters.TopP);
        Assert.Equal(0.0, parameters.FrequencyPenalty);
        Assert.Equal(0.0, parameters.PresencePenalty);
        Assert.Equal(2000, parameters.MaxTokens);
        Assert.Equal(0.5, parameters.StrictnessLevel);
    }

    [Fact]
    public async Task AIBehaviorSettings_WithCustomStageParameters_ShouldPersist()
    {
        // Arrange
        var settings = CreateTestSettings("Custom Parameters");
        settings.ScriptGeneration.Temperature = 0.9;
        settings.ScriptGeneration.MaxTokens = 3000;
        settings.ScriptGeneration.CustomSystemPrompt = "Custom prompt";

        // Act
        await _service.SaveAIBehaviorSettingsAsync(settings, CancellationToken.None);
        var retrieved = await _service.GetAIBehaviorSettingAsync(settings.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(0.9, retrieved.ScriptGeneration.Temperature);
        Assert.Equal(3000, retrieved.ScriptGeneration.MaxTokens);
        Assert.Equal("Custom prompt", retrieved.ScriptGeneration.CustomSystemPrompt);
    }

    private AIBehaviorSettings CreateTestSettings(string name)
    {
        return new AIBehaviorSettings
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = "Test description",
            CreativityVsAdherence = 0.5,
            EnableChainOfThought = false,
            ShowPromptsBeforeSending = false,
            ScriptGeneration = new LLMStageParameters
            {
                StageName = "ScriptGeneration",
                Temperature = 0.7,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 2000,
                StrictnessLevel = 0.5
            },
            SceneDescription = new LLMStageParameters
            {
                StageName = "SceneDescription",
                Temperature = 0.7,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 1500,
                StrictnessLevel = 0.5
            },
            ContentOptimization = new LLMStageParameters
            {
                StageName = "ContentOptimization",
                Temperature = 0.5,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 2000,
                StrictnessLevel = 0.7
            },
            Translation = new LLMStageParameters
            {
                StageName = "Translation",
                Temperature = 0.3,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 2000,
                StrictnessLevel = 0.8
            },
            QualityAnalysis = new LLMStageParameters
            {
                StageName = "QualityAnalysis",
                Temperature = 0.2,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 1000,
                StrictnessLevel = 0.9
            },
            IsDefault = false,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
