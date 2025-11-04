using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Adapters;
using Aura.Core.Configuration;
using Aura.Core.Services.ModelSelection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for ModelSelectionService verifying precedence rules and user control
/// </summary>
public class ModelSelectionServiceTests
{
    private readonly ModelSelectionService _service;
    private readonly Mock<ModelCatalog> _mockCatalog;
    private readonly ModelSelectionStore _store;

    public ModelSelectionServiceTests()
    {
        var mockProviderSettings = new Mock<ProviderSettings>(
            NullLogger<ProviderSettings>.Instance);
        
        _store = new ModelSelectionStore(
            NullLogger<ModelSelectionStore>.Instance,
            mockProviderSettings.Object);

        _mockCatalog = new Mock<ModelCatalog>(
            NullLogger<ModelCatalog>.Instance,
            Mock.Of<System.Net.Http.IHttpClientFactory>());

        _service = new ModelSelectionService(
            NullLogger<ModelSelectionService>.Instance,
            _mockCatalog.Object,
            _store);
    }

    [Fact]
    public async Task TestModelPriority_RunOverridePinnedWins()
    {
        // Arrange: Set up a stage-pinned model
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Stage, true, "test", "test", default);

        // Mock catalog to return models
        var runModel = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o",
            MaxTokens = 128000,
            ContextWindow = 128000
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4o"))
            .Returns((runModel, "Found model"));

        // Act: Resolve with run-override pinned
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", "gpt-4o", true, "job123", default);

        // Assert: Run override (pinned) should win over stage-pinned
        Assert.Equal("gpt-4o", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.RunOverridePinned, result.Source);
        Assert.True(result.IsPinned);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public async Task TestModelPriority_StagePinnedWins()
    {
        // Arrange: Set up stage-pinned and project models
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Stage, true, "test", "test", default);
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-3.5-turbo", ModelSelectionScope.Project, false, "test", "test", default);

        var model = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns((model, "Found model"));

        // Act: Resolve without run override
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, null, default);

        // Assert: Stage-pinned should win over project
        Assert.Equal("gpt-4", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.StagePinned, result.Source);
        Assert.True(result.IsPinned);
    }

    [Fact]
    public async Task TestModelPriority_ProjectOverrideWins()
    {
        // Arrange: Set up project and global models
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Project, false, "test", "test", default);
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-3.5-turbo", ModelSelectionScope.Global, false, "test", "test", default);

        var model = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns((model, "Found model"));

        // Act
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, null, default);

        // Assert: Project override should win over global
        Assert.Equal("gpt-4", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.ProjectOverride, result.Source);
        Assert.False(result.IsPinned);
    }

    [Fact]
    public async Task TestModelPriority_FallbackOnlyWhenAllowed()
    {
        // Arrange: No selections configured, auto-fallback disabled (default)
        var fallbackModel = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o-mini",
            MaxTokens = 128000,
            ContextWindow = 128000
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", null))
            .Returns((fallbackModel, "Using default"));

        // Act: Try to resolve without any selections
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, null, default);

        // Assert: Should be blocked because auto-fallback is disabled
        Assert.True(result.IsBlocked);
        Assert.Contains("automatic fallback is disabled", result.BlockReason);
        Assert.Null(result.SelectedModelId);
    }

    [Fact]
    public async Task TestModelPriority_FallbackWhenAllowedInSettings()
    {
        // Arrange: Enable auto-fallback
        await _store.SetAutoFallbackSettingAsync(true, default);

        var fallbackModel = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o-mini",
            MaxTokens = 128000,
            ContextWindow = 128000
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", null))
            .Returns((fallbackModel, "Using default"));

        // Act
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, null, default);

        // Assert: Should use fallback when allowed
        Assert.False(result.IsBlocked);
        Assert.Equal("gpt-4o-mini", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.AutomaticFallback, result.Source);
        Assert.True(result.RequiresUserNotification);
    }

    [Fact]
    public async Task EndToEnd_UsePinnedModel_BlocksWhenUnavailable()
    {
        // Arrange: Set up a pinned model
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Stage, true, "test", "test", default);

        // Mock catalog to return null (model unavailable)
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Model not found"));
        
        _mockCatalog.Setup(c => c.GetAllModels("OpenAI"))
            .Returns(new System.Collections.Generic.List<ModelRegistry.ModelInfo>
            {
                new ModelRegistry.ModelInfo
                {
                    Provider = "OpenAI",
                    ModelId = "gpt-4o",
                    MaxTokens = 128000,
                    ContextWindow = 128000
                }
            });

        // Act: Try to resolve with unavailable pinned model
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, null, default);

        // Assert: Should be blocked and provide alternatives
        Assert.True(result.IsBlocked);
        Assert.Contains("unavailable", result.BlockReason);
        Assert.NotEmpty(result.RecommendedAlternatives);
        Assert.Null(result.SelectedModelId);
    }

    [Fact]
    public async Task SetModelSelection_ValidatesModelExists()
    {
        // Arrange: Mock catalog to return null (model not found)
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "nonexistent"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Not found"));

        // Act
        var result = await _service.SetModelSelectionAsync(
            "OpenAI", "script", "nonexistent", ModelSelectionScope.Global, false, "test", "test", default);

        // Assert: Should fail validation
        Assert.False(result.Applied);
        Assert.Contains("not found", result.Reason);
    }

    [Fact]
    public async Task SetModelSelection_WarnsAboutDeprecation()
    {
        // Arrange: Mock deprecated model
        var deprecatedModel = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-3.5-turbo",
            MaxTokens = 4096,
            ContextWindow = 16384,
            DeprecationDate = DateTime.UtcNow.AddDays(-1),
            ReplacementModel = "gpt-4o-mini"
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-3.5-turbo"))
            .Returns((deprecatedModel, "Found model"));

        // Act
        var result = await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-3.5-turbo", ModelSelectionScope.Global, false, "test", "test", default);

        // Assert: Should succeed but include deprecation warning
        Assert.True(result.Applied);
        Assert.NotNull(result.DeprecationWarning);
        Assert.Contains("deprecated", result.DeprecationWarning);
        Assert.Contains("gpt-4o-mini", result.DeprecationWarning);
    }

    [Fact]
    public async Task ClearSelections_RemovesMatchingSelections()
    {
        // Arrange: Add multiple selections
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Global, false, "test", "test", default);
        await _service.SetModelSelectionAsync(
            "OpenAI", "visual", "gpt-4", ModelSelectionScope.Global, false, "test", "test", default);
        await _service.SetModelSelectionAsync(
            "Anthropic", "script", "claude-3-opus", ModelSelectionScope.Global, false, "test", "test", default);

        // Act: Clear only OpenAI selections
        await _service.ClearSelectionsAsync("OpenAI", null, null, default);

        // Get remaining selections
        var state = await _service.GetAllSelectionsAsync(default);

        // Assert: Only Anthropic selection should remain
        Assert.Single(state.GlobalDefaults);
        Assert.Equal("Anthropic", state.GlobalDefaults[0].Provider);
    }
}
