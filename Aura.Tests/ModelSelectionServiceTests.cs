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

    [Fact]
    public async Task PrecedenceMatrix_CompleteHierarchy()
    {
        // Arrange: Set up complete hierarchy of selections
        var gptMini = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o-mini",
            MaxTokens = 16384,
            ContextWindow = 128000
        };
        var gpt35 = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-3.5-turbo",
            MaxTokens = 4096,
            ContextWindow = 16384
        };
        var gpt4 = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        var gpt4o = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o",
            MaxTokens = 16384,
            ContextWindow = 128000
        };

        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4o-mini"))
            .Returns((gptMini, "Found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-3.5-turbo"))
            .Returns((gpt35, "Found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns((gpt4, "Found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4o"))
            .Returns((gpt4o, "Found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", null))
            .Returns((gptMini, "Default"));

        // Set up hierarchy: Global -> Project -> Stage
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4o-mini", ModelSelectionScope.Global, false, "test", "Global default", default);
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-3.5-turbo", ModelSelectionScope.Project, false, "test", "Project override", default);
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Stage, true, "test", "Stage pinned", default);

        // Test 1: Run override (pinned) beats everything
        var result1 = await _service.ResolveModelAsync("OpenAI", "script", "gpt-4o", true, "job1", default);
        Assert.Equal("gpt-4o", result1.SelectedModelId);
        Assert.Equal(ModelSelectionSource.RunOverridePinned, result1.Source);
        Assert.True(result1.IsPinned);

        // Test 2: Run override (not pinned) beats stage/project/global
        var result2 = await _service.ResolveModelAsync("OpenAI", "script", "gpt-4o", false, "job2", default);
        Assert.Equal("gpt-4o", result2.SelectedModelId);
        Assert.Equal(ModelSelectionSource.RunOverride, result2.Source);
        Assert.False(result2.IsPinned);

        // Test 3: Stage pinned beats project and global
        var result3 = await _service.ResolveModelAsync("OpenAI", "script", null, false, "job3", default);
        Assert.Equal("gpt-4", result3.SelectedModelId);
        Assert.Equal(ModelSelectionSource.StagePinned, result3.Source);
        Assert.True(result3.IsPinned);

        // Test 4: Clear stage, project override should win
        await _store.ClearSelectionsAsync("OpenAI", "script", ModelSelectionScope.Stage, default);
        var result4 = await _service.ResolveModelAsync("OpenAI", "script", null, false, "job4", default);
        Assert.Equal("gpt-3.5-turbo", result4.SelectedModelId);
        Assert.Equal(ModelSelectionSource.ProjectOverride, result4.Source);

        // Test 5: Clear project, global default should win
        await _store.ClearSelectionsAsync("OpenAI", "script", ModelSelectionScope.Project, default);
        var result5 = await _service.ResolveModelAsync("OpenAI", "script", null, false, "job5", default);
        Assert.Equal("gpt-4o-mini", result5.SelectedModelId);
        Assert.Equal(ModelSelectionSource.GlobalDefault, result5.Source);
    }

    [Fact]
    public async Task AuditLog_RecordsAllResolutions()
    {
        // Arrange: Set up model
        var model = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns((model, "Found"));

        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Global, false, "user", "test", default);

        // Act: Perform multiple resolutions
        await _service.ResolveModelAsync("OpenAI", "script", null, false, "job1", default);
        await _service.ResolveModelAsync("OpenAI", "script", null, false, "job2", default);
        await _service.ResolveModelAsync("OpenAI", "visual", null, false, "job3", default);

        // Get audit log
        var auditLog = await _service.GetAuditLogAsync(null, default);

        // Assert: Should have 3 entries
        Assert.True(auditLog.Count >= 3);
        var lastThree = auditLog.TakeLast(3).ToList();
        
        Assert.Equal("OpenAI", lastThree[0].Provider);
        Assert.Equal("script", lastThree[0].Stage);
        Assert.Equal("job1", lastThree[0].JobId);
        
        Assert.Equal("job2", lastThree[1].JobId);
        Assert.Equal("job3", lastThree[2].JobId);
    }

    [Fact]
    public async Task ExplainChoice_ComparesModels()
    {
        // Arrange: Set up models
        var gpt4 = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        var gpt4o = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o",
            MaxTokens = 16384,
            ContextWindow = 128000
        };

        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns((gpt4, "Found"));
        _mockCatalog.Setup(c => c.GetAllModels("OpenAI"))
            .Returns(new List<ModelRegistry.ModelInfo> { gpt4o, gpt4 });

        // Act: Explain choosing gpt-4 (not the recommended gpt-4o)
        var explanation = await _service.ExplainModelChoiceAsync("OpenAI", "script", "gpt-4", default);

        // Assert
        Assert.Equal("gpt-4", explanation.SelectedModel.ModelId);
        Assert.NotNull(explanation.RecommendedModel);
        Assert.Equal("gpt-4o", explanation.RecommendedModel.ModelId);
        Assert.False(explanation.SelectedIsRecommended);
        Assert.Contains("smaller context window", explanation.Reasoning);
        Assert.NotEmpty(explanation.Tradeoffs);
        Assert.NotEmpty(explanation.Suggestions);
    }

    [Fact]
    public async Task Integration_UnavailableWithBlock_UserAppliedFallback()
    {
        // Arrange: Pin a model that becomes unavailable
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Stage, true, "user", "test", default);

        // Mock model as unavailable
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Not found"));
        _mockCatalog.Setup(c => c.GetAllModels("OpenAI"))
            .Returns(new List<ModelRegistry.ModelInfo>
            {
                new ModelRegistry.ModelInfo
                {
                    Provider = "OpenAI",
                    ModelId = "gpt-4o",
                    MaxTokens = 16384,
                    ContextWindow = 128000
                }
            });

        // Act: Try to resolve - should be blocked
        var result = await _service.ResolveModelAsync("OpenAI", "script", null, false, "job1", default);

        // Assert: Blocked with alternatives
        Assert.True(result.IsBlocked);
        Assert.Contains("unavailable", result.BlockReason);
        Assert.NotEmpty(result.RecommendedAlternatives);

        // User applies fallback manually
        var fallback = result.RecommendedAlternatives.First();
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", fallback))
            .Returns((new ModelRegistry.ModelInfo
            {
                Provider = "OpenAI",
                ModelId = fallback,
                MaxTokens = 16384,
                ContextWindow = 128000
            }, "Found"));

        await _service.SetModelSelectionAsync(
            "OpenAI", "script", fallback, ModelSelectionScope.Stage, true, "user", "Manual fallback", default);

        var result2 = await _service.ResolveModelAsync("OpenAI", "script", null, false, "job2", default);
        Assert.False(result2.IsBlocked);
        Assert.Equal(fallback, result2.SelectedModelId);
        Assert.Equal(ModelSelectionSource.StagePinned, result2.Source);
    }

    [Fact]
    public async Task FallbackReason_TrackedWhenRunOverrideUnavailable()
    {
        // Arrange: Set up a project override
        var projectModel = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns((projectModel, "Found"));
        
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Project, false, "user", "test", default);

        // Mock run override as unavailable
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4o"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Not found"));

        // Act: Try to resolve with unavailable run override
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", "gpt-4o", false, "job1", default);

        // Assert: Should fall back to project and track reason
        Assert.Equal("gpt-4", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.ProjectOverride, result.Source);
        Assert.NotNull(result.FallbackReason);
        Assert.Contains("gpt-4o", result.FallbackReason);
        Assert.Contains("unavailable", result.FallbackReason);
    }

    [Fact]
    public async Task FallbackReason_TrackedInAutomaticFallback()
    {
        // Arrange: Enable auto-fallback, no selections configured
        await _store.SetAutoFallbackSettingAsync(true, default);

        var fallbackModel = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o-mini",
            MaxTokens = 128000,
            ContextWindow = 128000
        };
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", null))
            .Returns((fallbackModel, "Using safe default"));

        // Act: Resolve with no configured selections
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, "job1", default);

        // Assert: Should use automatic fallback with reason
        Assert.Equal("gpt-4o-mini", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.AutomaticFallback, result.Source);
        Assert.NotNull(result.FallbackReason);
        Assert.Contains("No configured model", result.FallbackReason);
    }

    [Fact]
    public async Task AuditLog_IncludesFallbackReasonForJob()
    {
        // Arrange: Set up model that will become unavailable
        var gpt4 = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        };
        var gptMini = new ModelRegistry.ModelInfo
        {
            Provider = "OpenAI",
            ModelId = "gpt-4o-mini",
            MaxTokens = 128000,
            ContextWindow = 128000
        };

        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4o-mini", ModelSelectionScope.Global, false, "user", "test", default);

        // Mock project override as unavailable
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "gpt-4", ModelSelectionScope.Project, false, "user", "test", default);

        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Not found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4o-mini"))
            .Returns((gptMini, "Found"));

        // Act: Resolve - should fall back from project to global
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, "job-with-fallback", default);

        // Assert resolution
        Assert.Equal("gpt-4o-mini", result.SelectedModelId);
        Assert.NotNull(result.FallbackReason);

        // Get audit log for this job
        var auditLog = await _store.GetAuditLogByJobIdAsync("job-with-fallback", default);
        Assert.Single(auditLog);
        
        var auditEntry = auditLog.First();
        Assert.Equal("job-with-fallback", auditEntry.JobId);
        Assert.Equal("gpt-4o-mini", auditEntry.ModelId);
        Assert.NotNull(auditEntry.FallbackReason);
        Assert.Contains("gpt-4", auditEntry.FallbackReason);
    }

    [Fact]
    public async Task PrecedenceMatrix_RunOverridePinnedBlocksEvenWithAutoFallbackEnabled()
    {
        // Arrange: Enable auto-fallback
        await _store.SetAutoFallbackSettingAsync(true, default);

        // Mock run override as unavailable, but have a fallback available
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "gpt-4-unavailable"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Not found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", null))
            .Returns((new ModelRegistry.ModelInfo
            {
                Provider = "OpenAI",
                ModelId = "gpt-4o-mini",
                MaxTokens = 128000,
                ContextWindow = 128000
            }, "Safe fallback"));

        // Act: Try to resolve with pinned run override that's unavailable
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", "gpt-4-unavailable", true, "job1", default);

        // Assert: Should be blocked despite auto-fallback being enabled
        Assert.True(result.IsBlocked);
        Assert.Null(result.SelectedModelId);
        Assert.Contains("Pinned model", result.BlockReason);
        Assert.Contains("unavailable", result.BlockReason);
    }

    [Fact]
    public async Task NoSilentSwaps_AllFallbacksAudited()
    {
        // Arrange: Set up chain of unavailable models
        await _service.SetModelSelectionAsync(
            "OpenAI", "script", "unavailable-global", ModelSelectionScope.Global, false, "user", "test", default);
        
        await _store.SetAutoFallbackSettingAsync(true, default);

        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", "unavailable-global"))
            .Returns(((ModelRegistry.ModelInfo?)null, "Not found"));
        _mockCatalog.Setup(c => c.FindOrDefault("OpenAI", null))
            .Returns((new ModelRegistry.ModelInfo
            {
                Provider = "OpenAI",
                ModelId = "gpt-4o-mini",
                MaxTokens = 128000,
                ContextWindow = 128000
            }, "Catalog fallback"));

        // Act: Resolve - should use automatic fallback
        var result = await _service.ResolveModelAsync(
            "OpenAI", "script", null, false, "job-audit-test", default);

        // Assert: Fallback was used and audited
        Assert.Equal("gpt-4o-mini", result.SelectedModelId);
        Assert.Equal(ModelSelectionSource.AutomaticFallback, result.Source);
        Assert.NotNull(result.FallbackReason);
        Assert.True(result.RequiresUserNotification);

        // Verify audit trail
        var auditLog = await _store.GetAuditLogByJobIdAsync("job-audit-test", default);
        Assert.Single(auditLog);
        Assert.NotNull(auditLog[0].FallbackReason);
        Assert.Equal(ModelSelectionSource.AutomaticFallback.ToString(), auditLog[0].Source);
    }
}
