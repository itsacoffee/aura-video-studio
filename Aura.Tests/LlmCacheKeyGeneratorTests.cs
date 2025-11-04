using System;
using System.Collections.Generic;
using Aura.Core.AI.Cache;
using Xunit;

namespace Aura.Tests;

public class LlmCacheKeyGeneratorTests
{
    [Fact]
    public void GenerateKey_SameInputs_ProducesSameKey()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", "system", "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", "system", "user prompt", 0.2, 1000);
        
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentProviders_ProducesDifferentKeys()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "Anthropic", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentModels_ProducesDifferentKeys()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-3.5-turbo", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentOperationTypes_ProducesDifferentKeys()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "OutlineTransform", null, "user prompt", 0.2, 1000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentUserPrompts_ProducesDifferentKeys()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt 1", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt 2", 0.2, 1000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentTemperatures_ProducesDifferentKeys()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.3, 1000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_DifferentMaxTokens_ProducesDifferentKeys()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 2000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_WithSystemPrompt_IncludesInKey()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", "system prompt", "user prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_WithAdditionalParams_IncludesInKey()
    {
        var params1 = new Dictionary<string, object>
        {
            ["topP"] = 0.9,
            ["frequencyPenalty"] = 0.5
        };
        
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000, params1);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000, null);
        
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_AdditionalParamsSorted_ProducesSameKey()
    {
        var params1 = new Dictionary<string, object>
        {
            ["topP"] = 0.9,
            ["frequencyPenalty"] = 0.5
        };
        
        var params2 = new Dictionary<string, object>
        {
            ["frequencyPenalty"] = 0.5,
            ["topP"] = 0.9
        };
        
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000, params1);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000, params2);
        
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateKey_CaseInsensitive_ProducesSameKey()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "User Prompt", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateKey_WhitespaceNormalized_ProducesSameKey()
    {
        var key1 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "  user prompt  ", 0.2, 1000);
        
        var key2 = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "user prompt", 0.2, 1000);
        
        Assert.Equal(key1, key2);
    }

    [Theory]
    [InlineData("planscaffold", true)]
    [InlineData("PlanScaffold", true)]
    [InlineData("PLANSCAFFOLD", true)]
    [InlineData("outlinetransform", true)]
    [InlineData("sceneanalysis", true)]
    [InlineData("contentcomplexity", true)]
    [InlineData("visualprompt", true)]
    [InlineData("creativestory", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsCacheable_ReturnsExpectedResult(string? operationType, bool expected)
    {
        var result = LlmCacheKeyGenerator.IsCacheable(operationType!);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.1, true)]
    [InlineData(0.2, true)]
    [InlineData(0.3, true)]
    [InlineData(0.31, false)]
    [InlineData(0.5, false)]
    [InlineData(0.7, false)]
    [InlineData(1.0, false)]
    public void IsTemperatureSuitable_ReturnsExpectedResult(double temperature, bool expected)
    {
        var result = LlmCacheKeyGenerator.IsTemperatureSuitable(temperature);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateKey_NullProviderName_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LlmCacheKeyGenerator.GenerateKey(null!, "gpt-4", "PlanScaffold", null, "prompt", 0.2, 1000));
    }

    [Fact]
    public void GenerateKey_NullModelName_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LlmCacheKeyGenerator.GenerateKey("OpenAI", null!, "PlanScaffold", null, "prompt", 0.2, 1000));
    }

    [Fact]
    public void GenerateKey_NullOperationType_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LlmCacheKeyGenerator.GenerateKey("OpenAI", "gpt-4", null!, null, "prompt", 0.2, 1000));
    }

    [Fact]
    public void GenerateKey_NullUserPrompt_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LlmCacheKeyGenerator.GenerateKey("OpenAI", "gpt-4", "PlanScaffold", null, null!, 0.2, 1000));
    }

    [Fact]
    public void GenerateKey_ProducesHexString()
    {
        var key = LlmCacheKeyGenerator.GenerateKey(
            "OpenAI", "gpt-4", "PlanScaffold", null, "prompt", 0.2, 1000);
        
        Assert.Matches("^[a-f0-9]+$", key);
        Assert.Equal(64, key.Length);
    }
}
