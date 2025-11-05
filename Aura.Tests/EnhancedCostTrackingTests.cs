using System;
using Aura.Core.Configuration;
using Aura.Core.Models.CostTracking;
using Aura.Core.Services.CostTracking;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class EnhancedCostTrackingTests
{
    private readonly Mock<ILogger<EnhancedCostTrackingService>> _loggerMock;
    private readonly Mock<ProviderSettings> _settingsMock;
    private readonly EnhancedCostTrackingService _service;

    public EnhancedCostTrackingTests()
    {
        _loggerMock = new Mock<ILogger<EnhancedCostTrackingService>>();
        _settingsMock = new Mock<ProviderSettings>();
        _settingsMock.Setup(s => s.GetAuraDataDirectory()).Returns(System.IO.Path.GetTempPath());
        
        _service = new EnhancedCostTrackingService(_loggerMock.Object, _settingsMock.Object);
    }

    [Fact]
    public void EstimateLlmCost_OpenAI_CalculatesCorrectly()
    {
        var cost = _service.EstimateLlmCost("OpenAI", 1000, 2000);
        
        var expectedCost = (1000 / 1000m * 0.03m) + (2000 / 1000m * 0.06m);
        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void EstimateTtsCost_ElevenLabs_CalculatesCorrectly()
    {
        var cost = _service.EstimateTtsCost("ElevenLabs", 1000);
        
        var expectedCost = 1000 / 1000m * 0.30m;
        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void EstimateLlmCost_FreeProvider_ReturnsZero()
    {
        var cost = _service.EstimateLlmCost("Ollama", 1000, 2000);
        
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void CheckBudget_WithinLimit_ReturnsSuccess()
    {
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false
        };
        
        _service.UpdateConfiguration(config);
        
        var result = _service.CheckBudget("OpenAI", 5m);
        
        Assert.True(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
    }

    [Fact]
    public void CheckBudget_ExceedsLimit_WithHardLimit_ReturnsBlock()
    {
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 10m,
            HardBudgetLimit = true
        };
        
        _service.UpdateConfiguration(config);
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 8m
        });
        
        var result = _service.CheckBudget("OpenAI", 5m);
        
        Assert.False(result.IsWithinBudget);
        Assert.True(result.ShouldBlock);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void CheckBudget_ExceedsLimit_WithoutHardLimit_ReturnsWarning()
    {
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 10m,
            HardBudgetLimit = false
        };
        
        _service.UpdateConfiguration(config);
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 8m
        });
        
        var result = _service.CheckBudget("OpenAI", 5m);
        
        Assert.False(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void CheckBudget_PerProviderLimit_EnforcesCorrectly()
    {
        var config = new CostTrackingConfiguration
        {
            ProviderBudgets = new()
            {
                ["OpenAI"] = 5m
            },
            HardBudgetLimit = true
        };
        
        _service.UpdateConfiguration(config);
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 4m
        });
        
        var result = _service.CheckBudget("OpenAI", 2m);
        
        Assert.False(result.IsWithinBudget);
        Assert.True(result.ShouldBlock);
    }

    [Fact]
    public void CheckBudget_ApproachingLimit_ReturnsWarning()
    {
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false
        };
        
        _service.UpdateConfiguration(config);
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 85m
        });
        
        var result = _service.CheckBudget("OpenAI", 10m);
        
        Assert.True(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void GetSpendingByProvider_GroupsCorrectly()
    {
        var startDate = DateTime.UtcNow.AddHours(-1);
        var endDate = DateTime.UtcNow;
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 5m
        });
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.VisualPrompts,
            Cost = 3m
        });
        
        _service.LogCost(new CostLog
        {
            ProviderName = "ElevenLabs",
            Feature = CostFeatureType.TextToSpeech,
            Cost = 2m
        });
        
        var spending = _service.GetSpendingByProvider(startDate, endDate);
        
        Assert.Equal(2, spending.Count);
        Assert.Equal(8m, spending["OpenAI"]);
        Assert.Equal(2m, spending["ElevenLabs"]);
    }

    [Fact]
    public void GetSpendingByFeature_GroupsCorrectly()
    {
        var startDate = DateTime.UtcNow.AddHours(-1);
        var endDate = DateTime.UtcNow;
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 5m
        });
        
        _service.LogCost(new CostLog
        {
            ProviderName = "OpenAI",
            Feature = CostFeatureType.ScriptGeneration,
            Cost = 3m
        });
        
        _service.LogCost(new CostLog
        {
            ProviderName = "ElevenLabs",
            Feature = CostFeatureType.TextToSpeech,
            Cost = 2m
        });
        
        var spending = _service.GetSpendingByFeature(startDate, endDate);
        
        Assert.Equal(2, spending.Count);
        Assert.Equal(8m, spending[CostFeatureType.ScriptGeneration]);
        Assert.Equal(2m, spending[CostFeatureType.TextToSpeech]);
    }

    [Fact]
    public void UpdateProviderPricing_UpdatesCorrectly()
    {
        var newPricing = new ProviderPricing
        {
            ProviderName = "TestProvider",
            ProviderType = ProviderType.LLM,
            CostPer1KInputTokens = 0.01m,
            CostPer1KOutputTokens = 0.02m,
            IsManualOverride = true
        };
        
        _service.UpdateProviderPricing(newPricing);
        
        var retrieved = _service.GetProviderPricing("TestProvider");
        
        Assert.NotNull(retrieved);
        Assert.Equal(0.01m, retrieved.CostPer1KInputTokens);
        Assert.Equal(0.02m, retrieved.CostPer1KOutputTokens);
        Assert.True(retrieved.IsManualOverride);
    }
}
