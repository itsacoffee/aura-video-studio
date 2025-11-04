using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Adapters;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for ModelCatalog with ModelRegistry interaction
/// </summary>
public class ModelCatalogIntegrationTests
{
    private readonly ModelCatalog _catalog;
    private readonly IHttpClientFactory _httpClientFactory;

    public ModelCatalogIntegrationTests()
    {
        var logger = NullLogger<ModelCatalog>.Instance;
        _httpClientFactory = new TestHttpClientFactory();
        _catalog = new ModelCatalog(logger, _httpClientFactory);
    }

    [Fact]
    public void Integration_StaticRegistry_ProvidesAllExpectedModels()
    {
        // Act
        var openAiModels = _catalog.GetAllModels("OpenAI");
        var anthropicModels = _catalog.GetAllModels("Anthropic");
        var geminiModels = _catalog.GetAllModels("Gemini");
        var azureModels = _catalog.GetAllModels("Azure");

        // Assert
        Assert.NotEmpty(openAiModels);
        Assert.NotEmpty(anthropicModels);
        Assert.NotEmpty(geminiModels);
        Assert.NotEmpty(azureModels);

        Assert.Contains(openAiModels, m => m.ModelId == "gpt-4o");
        Assert.Contains(openAiModels, m => m.ModelId == "gpt-4o-mini");
        Assert.Contains(anthropicModels, m => m.ModelId == "claude-3-5-sonnet-20241022");
        Assert.Contains(geminiModels, m => m.ModelId == "gemini-1.5-pro");
    }

    [Fact]
    public void Integration_FindOrDefault_WorksAcrossMultipleProviders()
    {
        // Test OpenAI
        var (openAiModel, openAiReason) = _catalog.FindOrDefault("OpenAI", "gpt-4o-mini");
        Assert.NotNull(openAiModel);
        Assert.Equal("gpt-4o-mini", openAiModel.ModelId);
        Assert.Contains("Found", openAiReason);

        // Test Anthropic
        var (anthropicModel, anthropicReason) = _catalog.FindOrDefault("Anthropic", "claude-3-5-sonnet");
        Assert.NotNull(anthropicModel);
        Assert.Equal("claude-3-5-sonnet-20241022", anthropicModel.ModelId);
        Assert.Contains("Found", anthropicReason);

        // Test Gemini
        var (geminiModel, geminiReason) = _catalog.FindOrDefault("Gemini", "gemini-1.5-pro-latest");
        Assert.NotNull(geminiModel);
        Assert.Equal("gemini-1.5-pro", geminiModel.ModelId);
        Assert.Contains("Found", geminiReason);
    }

    [Fact]
    public void Integration_FallbackChain_WorksCorrectly()
    {
        // Request unknown model -> should fall back to default
        var (model1, reason1) = _catalog.FindOrDefault("OpenAI", "gpt-5-ultra-mega");
        Assert.NotNull(model1);
        Assert.Equal("gpt-4o-mini", model1.ModelId);
        Assert.Contains("not available", reason1);

        // Request without specifying model -> should use default
        var (model2, reason2) = _catalog.FindOrDefault("Anthropic");
        Assert.NotNull(model2);
        Assert.Equal("claude-3-5-sonnet-20241022", model2.ModelId);
        Assert.Contains("default", reason2);

        // Request for unknown provider -> should return null
        var (model3, reason3) = _catalog.FindOrDefault("UnknownProvider");
        Assert.Null(model3);
        Assert.Contains("No models available", reason3);
    }

    [Fact]
    public void Integration_ModelCapabilities_ConsistentWithRegistry()
    {
        // Get capabilities via catalog
        var (maxTokens1, contextWindow1, _) = _catalog.GetModelCapabilities("OpenAI", "gpt-4o-mini");

        // Get same model from registry
        var model = ModelRegistry.FindModel("OpenAI", "gpt-4o-mini");
        
        // Should match
        Assert.NotNull(model);
        Assert.Equal(model.MaxTokens, maxTokens1);
        Assert.Equal(model.ContextWindow, contextWindow1);
    }

    [Fact]
    public void Integration_AliasResolution_WorksWithStaticRegistry()
    {
        // Test OpenAI alias
        var (model1, _) = _catalog.FindOrDefault("OpenAI", "gpt-4o-mini-latest");
        Assert.NotNull(model1);
        Assert.Equal("gpt-4o-mini", model1.ModelId);

        // Test Anthropic alias
        var (model2, _) = _catalog.FindOrDefault("Anthropic", "claude-3.5-sonnet");
        Assert.NotNull(model2);
        Assert.Equal("claude-3-5-sonnet-20241022", model2.ModelId);

        // Test Gemini alias
        var (model3, _) = _catalog.FindOrDefault("Gemini", "gemini-1.5-flash-latest");
        Assert.NotNull(model3);
        Assert.Equal("gemini-1.5-flash", model3.ModelId);
    }

    [Fact]
    public void Integration_CacheWorks_AcrossMultipleRequests()
    {
        // First request - not from cache
        var (maxTokens1, contextWindow1, fromCache1) = _catalog.GetModelCapabilities("OpenAI", "gpt-4o");
        Assert.False(fromCache1);

        // Second request - should be from cache
        var (maxTokens2, contextWindow2, fromCache2) = _catalog.GetModelCapabilities("OpenAI", "gpt-4o");
        Assert.True(fromCache2);

        // Values should match
        Assert.Equal(maxTokens1, maxTokens2);
        Assert.Equal(contextWindow1, contextWindow2);

        // Different model - not from cache
        var (_, _, fromCache3) = _catalog.GetModelCapabilities("OpenAI", "gpt-4o-mini");
        Assert.False(fromCache3);
    }

    [Fact]
    public void Integration_EstimateCapabilities_ProvidesReasonableDefaults()
    {
        // Test with unknown Ollama model
        var (maxTokens, contextWindow, _) = _catalog.GetModelCapabilities("Ollama", "mysteriousmodel:latest");
        
        // Should return non-zero values
        Assert.True(maxTokens > 0);
        Assert.True(contextWindow > 0);
        
        // Should be reasonable defaults
        Assert.InRange(maxTokens, 2048, 128000);
        Assert.InRange(contextWindow, 2048, 128000);
    }

    [Fact]
    public async Task Integration_PreflightCheck_ValidatesMultipleProvidersAsync()
    {
        // Arrange
        var providersToCheck = new Dictionary<string, string>
        {
            ["OpenAI"] = "gpt-4o-mini",
            ["Anthropic"] = "claude-3-5-sonnet-20241022",
            ["Gemini"] = "gemini-1.5-pro",
            ["InvalidProvider"] = "invalid-model"
        };

        // Act
        var results = await _catalog.PreflightCheckAsync(
            providersToCheck, 
            new Dictionary<string, string>(), 
            null,
            CancellationToken.None);

        // Assert
        Assert.Equal(4, results.Count);
        Assert.True(results["OpenAI"]);
        Assert.True(results["Anthropic"]);
        Assert.True(results["Gemini"]);
        Assert.False(results["InvalidProvider"]);
    }

    [Fact]
    public void Integration_ModelRegistry_StaticHelpers_StillWorkIndependently()
    {
        // Ensure ModelRegistry static methods still work as before
        var openAiDefault = ModelRegistry.GetDefaultModel("OpenAI");
        Assert.Equal("gpt-4o-mini", openAiDefault);

        var anthropicDefault = ModelRegistry.GetDefaultModel("Anthropic");
        Assert.Equal("claude-3-5-sonnet-20241022", anthropicDefault);

        var model = ModelRegistry.FindModel("OpenAI", "gpt-4o");
        Assert.NotNull(model);
        Assert.Equal("OpenAI", model.Provider);

        var (maxTokens, contextWindow) = ModelRegistry.EstimateCapabilities("gpt-4o");
        Assert.True(maxTokens > 0);
        Assert.True(contextWindow > 0);
    }

    [Fact]
    public void Integration_NeedsRefresh_InitiallyTrue()
    {
        // On initial creation, catalog should need refresh
        Assert.True(_catalog.NeedsRefresh());
    }

    [Fact]
    public void Integration_GetAllModels_ReturnsUniqueModels()
    {
        // Get all models for a provider
        var models = _catalog.GetAllModels("OpenAI");
        
        // Should not have duplicates
        var distinctCount = models.DistinctBy(m => m.ModelId).Count();
        Assert.Equal(models.Count, distinctCount);
    }

    /// <summary>
    /// Test HTTP client factory for integration tests
    /// </summary>
    private class TestHttpClientFactory : IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name)
        {
            return new System.Net.Http.HttpClient();
        }
    }
}
