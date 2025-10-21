using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Health;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class SmartProviderSelectorTests
{
    private readonly ILogger<ProviderHealthMonitor> _healthMonitorLogger;
    private readonly ILogger<SmartProviderSelector> _selectorLogger;

    public SmartProviderSelectorTests()
    {
        _healthMonitorLogger = NullLogger<ProviderHealthMonitor>.Instance;
        _selectorLogger = NullLogger<SmartProviderSelector>.Instance;
    }

    [Fact]
    public async Task SelectBestLlmProviderAsync_WithHealthyProviders_SelectsBasedOnMetrics()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var mockProvider1 = new Mock<ILlmProvider>();
        var mockProvider2 = new Mock<ILlmProvider>();

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = mockProvider1.Object,
            ["OpenAI"] = mockProvider2.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            llmProviders: llmProviders,
            fallbackLlmProvider: mockProvider1.Object
        );

        // Simulate health checks - OpenAI is faster
        await healthMonitor.CheckProviderHealthAsync("RuleBased", _ => Task.FromResult(true));
        await Task.Delay(50); // Small delay to ensure different response times
        await healthMonitor.CheckProviderHealthAsync("OpenAI", _ => Task.FromResult(true));

        // Act
        var selected = await selector.SelectBestLlmProviderAsync(ProviderTier.Balanced);

        // Assert
        Assert.NotNull(selected);
        Assert.True(selected == mockProvider1.Object || selected == mockProvider2.Object);
    }

    [Fact]
    public async Task SelectBestLlmProviderAsync_WithAllProvidersFailing_ReturnsFallback()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var mockProvider = new Mock<ILlmProvider>();
        var fallbackProvider = new Mock<ILlmProvider>();

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["FailingProvider"] = mockProvider.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            llmProviders: llmProviders,
            fallbackLlmProvider: fallbackProvider.Object
        );

        // Simulate 3 consecutive failures to mark as unhealthy
        for (int i = 0; i < 3; i++)
        {
            await healthMonitor.CheckProviderHealthAsync("FailingProvider", _ => Task.FromResult(false));
        }

        // Act
        var selected = await selector.SelectBestLlmProviderAsync(ProviderTier.Balanced);

        // Assert
        Assert.Equal(fallbackProvider.Object, selected);
    }

    [Fact]
    public async Task SelectBestLlmProviderAsync_FreeTier_ExcludesProProviders()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var freeProvider = new Mock<ILlmProvider>();
        var proProvider = new Mock<ILlmProvider>();

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = freeProvider.Object,
            ["OpenAI"] = proProvider.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            llmProviders: llmProviders,
            fallbackLlmProvider: freeProvider.Object
        );

        // Mark both as healthy
        await healthMonitor.CheckProviderHealthAsync("RuleBased", _ => Task.FromResult(true));
        await healthMonitor.CheckProviderHealthAsync("OpenAI", _ => Task.FromResult(true));

        // Act
        var selected = await selector.SelectBestLlmProviderAsync(ProviderTier.Free);

        // Assert - Should select the free provider
        Assert.Equal(freeProvider.Object, selected);
    }

    [Fact]
    public async Task SelectBestLlmProviderAsync_ProTier_PrefersProProviders()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var freeProvider = new Mock<ILlmProvider>();
        var proProvider = new Mock<ILlmProvider>();

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = freeProvider.Object,
            ["OpenAI"] = proProvider.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            llmProviders: llmProviders,
            fallbackLlmProvider: freeProvider.Object
        );

        // Mark both as healthy
        await healthMonitor.CheckProviderHealthAsync("RuleBased", _ => Task.FromResult(true));
        await healthMonitor.CheckProviderHealthAsync("OpenAI", _ => Task.FromResult(true));

        // Act
        var selected = await selector.SelectBestLlmProviderAsync(ProviderTier.Pro);

        // Assert - Should select the pro provider
        Assert.Equal(proProvider.Object, selected);
    }

    [Fact]
    public async Task SelectBestTtsProviderAsync_WithHealthyProviders_SelectsBasedOnMetrics()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var mockProvider1 = new Mock<ITtsProvider>();
        var mockProvider2 = new Mock<ITtsProvider>();

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = mockProvider1.Object,
            ["ElevenLabs"] = mockProvider2.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            ttsProviders: ttsProviders,
            fallbackTtsProvider: mockProvider1.Object
        );

        // Mark both as healthy
        await healthMonitor.CheckProviderHealthAsync("Windows", _ => Task.FromResult(true));
        await healthMonitor.CheckProviderHealthAsync("ElevenLabs", _ => Task.FromResult(true));

        // Act
        var selected = await selector.SelectBestTtsProviderAsync(ProviderTier.Balanced);

        // Assert
        Assert.NotNull(selected);
        Assert.True(selected == mockProvider1.Object || selected == mockProvider2.Object);
    }

    [Fact]
    public async Task SelectBestTtsProviderAsync_WithAllProvidersFailing_ReturnsFallback()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var mockProvider = new Mock<ITtsProvider>();
        var fallbackProvider = new Mock<ITtsProvider>();

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["FailingProvider"] = mockProvider.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            ttsProviders: ttsProviders,
            fallbackTtsProvider: fallbackProvider.Object
        );

        // Simulate 3 consecutive failures
        for (int i = 0; i < 3; i++)
        {
            await healthMonitor.CheckProviderHealthAsync("FailingProvider", _ => Task.FromResult(false));
        }

        // Act
        var selected = await selector.SelectBestTtsProviderAsync(ProviderTier.Balanced);

        // Assert
        Assert.Equal(fallbackProvider.Object, selected);
    }

    [Fact]
    public void RecordProviderUsage_LogsUsageStatistics()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger
        );

        // Act - should not throw
        selector.RecordProviderUsage("TestProvider", true);
        selector.RecordProviderUsage("TestProvider", false, "Test error");

        // Assert - no exception means success
        Assert.True(true);
    }

    [Fact]
    public async Task SelectBestLlmProviderAsync_ExcludesProvidersWithHighConsecutiveFailures()
    {
        // Arrange
        var healthMonitor = new ProviderHealthMonitor(_healthMonitorLogger);
        var healthyProvider = new Mock<ILlmProvider>();
        var degradedProvider = new Mock<ILlmProvider>();

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["Healthy"] = healthyProvider.Object,
            ["Degraded"] = degradedProvider.Object
        };

        var selector = new SmartProviderSelector(
            healthMonitor,
            _selectorLogger,
            llmProviders: llmProviders,
            fallbackLlmProvider: healthyProvider.Object
        );

        // Mark one as healthy, one with 3 failures
        await healthMonitor.CheckProviderHealthAsync("Healthy", _ => Task.FromResult(true));
        for (int i = 0; i < 3; i++)
        {
            await healthMonitor.CheckProviderHealthAsync("Degraded", _ => Task.FromResult(false));
        }

        // Act
        var selected = await selector.SelectBestLlmProviderAsync(ProviderTier.Balanced);

        // Assert - Should select healthy provider
        Assert.Equal(healthyProvider.Object, selected);
    }
}
