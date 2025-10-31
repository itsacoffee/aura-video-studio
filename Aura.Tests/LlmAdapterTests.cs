using System;
using System.Net;
using System.Net.Http;
using Aura.Core.AI.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class OpenAiAdapterTests
{
    private readonly OpenAiAdapter _adapter;

    public OpenAiAdapterTests()
    {
        var logger = NullLogger<OpenAiAdapter>.Instance;
        _adapter = new OpenAiAdapter(logger, "gpt-4o-mini");
    }

    [Fact]
    public void Adapter_Should_UseCorrectProviderName()
    {
        Assert.Equal("OpenAI", _adapter.ProviderName);
    }

    [Fact]
    public void Capabilities_Should_IncludeCorrectFeatures()
    {
        var capabilities = _adapter.Capabilities;
        
        Assert.True(capabilities.SupportsJsonMode);
        Assert.True(capabilities.SupportsStreaming);
        Assert.True(capabilities.SupportsFunctionCalling);
        Assert.True(capabilities.MaxTokenLimit > 0);
    }

    [Fact]
    public void CalculateParameters_Creative_Should_UseHigherTemperature()
    {
        var parameters = _adapter.CalculateParameters(LlmOperationType.Creative, 500);
        
        Assert.Equal(0.7, parameters.Temperature);
        Assert.NotNull(parameters.TopP);
        Assert.True(parameters.TopP > 0.8);
    }

    [Fact]
    public void CalculateParameters_Analytical_Should_UseLowerTemperature()
    {
        var parameters = _adapter.CalculateParameters(LlmOperationType.Analytical, 500);
        
        Assert.Equal(0.3, parameters.Temperature);
        Assert.NotNull(parameters.TopP);
        Assert.True(parameters.TopP <= 0.8);
    }

    [Fact]
    public void TruncatePrompt_Should_NotTruncateShortPrompt()
    {
        var prompt = "This is a short prompt.";
        var (truncated, wasTruncated) = _adapter.TruncatePrompt(prompt, 1000);
        
        Assert.Equal(prompt, truncated);
        Assert.False(wasTruncated);
    }

    [Fact]
    public void TruncatePrompt_Should_TruncateLongPrompt()
    {
        var prompt = new string('x', 10000);
        var (truncated, wasTruncated) = _adapter.TruncatePrompt(prompt, 100);
        
        Assert.True(truncated.Length < prompt.Length);
        Assert.True(wasTruncated);
        Assert.Contains("truncated", truncated, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateResponse_Should_ReturnFalse_ForEmptyResponse()
    {
        var isValid = _adapter.ValidateResponse("", LlmOperationType.Creative);
        
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateResponse_Should_ReturnTrue_ForValidResponse()
    {
        var isValid = _adapter.ValidateResponse("This is a valid response.", LlmOperationType.Creative);
        
        Assert.True(isValid);
    }

    [Fact]
    public void HandleError_RateLimit_Should_SuggestRetry()
    {
        var error = new HttpRequestException("Rate limit", null, HttpStatusCode.TooManyRequests);
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.True(strategy.ShouldRetry);
        Assert.NotNull(strategy.RetryDelay);
        Assert.True(strategy.RetryDelay.Value.TotalSeconds > 0);
    }

    [Fact]
    public void HandleError_Unauthorized_Should_NotRetry()
    {
        var error = new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.False(strategy.ShouldRetry);
        Assert.True(strategy.ShouldFallback);
        Assert.True(strategy.IsPermanentFailure);
    }

    [Fact]
    public void OptimizeSystemPrompt_Should_EnhancePrompt()
    {
        var original = "Help users create videos.";
        var optimized = _adapter.OptimizeSystemPrompt(original);
        
        Assert.NotEqual(original, optimized);
        Assert.Contains(original, optimized);
    }
}

public class AnthropicAdapterTests
{
    private readonly AnthropicAdapter _adapter;

    public AnthropicAdapterTests()
    {
        var logger = NullLogger<AnthropicAdapter>.Instance;
        _adapter = new AnthropicAdapter(logger);
    }

    [Fact]
    public void Adapter_Should_UseCorrectProviderName()
    {
        Assert.Equal("Anthropic", _adapter.ProviderName);
    }

    [Fact]
    public void Capabilities_Should_IncludeConstitutionalAI()
    {
        var capabilities = _adapter.Capabilities;
        
        Assert.Contains("constitutional_ai", capabilities.SpecialFeatures);
    }

    [Fact]
    public void CalculateParameters_Should_IncludeStopSequences()
    {
        var parameters = _adapter.CalculateParameters(LlmOperationType.Creative, 500);
        
        Assert.NotNull(parameters.StopSequences);
        Assert.Contains("\n\nHuman:", parameters.StopSequences);
        Assert.Contains("\n\nAssistant:", parameters.StopSequences);
    }

    [Fact]
    public void HandleError_Overloaded_Should_UseLongerBackoff()
    {
        var error = new HttpRequestException("Overloaded", null, (HttpStatusCode)529);
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.True(strategy.ShouldRetry);
        Assert.NotNull(strategy.RetryDelay);
        Assert.True(strategy.RetryDelay.Value.TotalSeconds >= 2);
    }

    [Fact]
    public void OptimizeSystemPrompt_Should_AddConstitutionalPrinciples()
    {
        var original = "You are a helpful assistant.";
        var optimized = _adapter.OptimizeSystemPrompt(original);
        
        Assert.Contains("Constitutional AI", optimized);
    }
}

public class GeminiAdapterTests
{
    private readonly GeminiAdapter _adapter;

    public GeminiAdapterTests()
    {
        var logger = NullLogger<GeminiAdapter>.Instance;
        _adapter = new GeminiAdapter(logger);
    }

    [Fact]
    public void Adapter_Should_UseCorrectProviderName()
    {
        Assert.Equal("Gemini", _adapter.ProviderName);
    }

    [Fact]
    public void Capabilities_Should_IncludeMultimodal()
    {
        var capabilities = _adapter.Capabilities;
        
        Assert.Contains("multimodal", capabilities.SpecialFeatures);
        Assert.Contains("safety_settings", capabilities.SpecialFeatures);
    }

    [Fact]
    public void CalculateParameters_Creative_Should_UseHighestTemperature()
    {
        var parameters = _adapter.CalculateParameters(LlmOperationType.Creative, 500);
        
        Assert.Equal(0.9, parameters.Temperature);
        Assert.NotNull(parameters.TopK);
        Assert.Equal(40, parameters.TopK.Value);
    }

    [Fact]
    public void HandleError_SafetyBlock_Should_ModifyPrompt()
    {
        var error = new Exception("Content blocked by safety filters");
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.True(strategy.ShouldRetry);
        Assert.NotNull(strategy.ModifiedPrompt);
    }
}

public class AzureOpenAiAdapterTests
{
    private readonly AzureOpenAiAdapter _adapter;

    public AzureOpenAiAdapterTests()
    {
        var logger = NullLogger<AzureOpenAiAdapter>.Instance;
        _adapter = new AzureOpenAiAdapter(logger, fallbackRegions: new[] { "eastus2", "westus" });
    }

    [Fact]
    public void Adapter_Should_UseCorrectProviderName()
    {
        Assert.Equal("Azure OpenAI", _adapter.ProviderName);
    }

    [Fact]
    public void Capabilities_Should_IncludeRegionalFailover()
    {
        var capabilities = _adapter.Capabilities;
        
        Assert.Contains("regional_failover", capabilities.SpecialFeatures);
    }

    [Fact]
    public void HandleError_NotFound_Should_BePermanentFailure()
    {
        var error = new HttpRequestException("Not found", null, HttpStatusCode.NotFound);
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.False(strategy.ShouldRetry);
        Assert.True(strategy.IsPermanentFailure);
    }
}

public class OllamaAdapterTests
{
    private readonly OllamaAdapter _adapter;

    public OllamaAdapterTests()
    {
        var logger = NullLogger<OllamaAdapter>.Instance;
        _adapter = new OllamaAdapter(logger, "llama3.1");
    }

    [Fact]
    public void Adapter_Should_UseCorrectProviderName()
    {
        Assert.Equal("Ollama", _adapter.ProviderName);
    }

    [Fact]
    public void Capabilities_Should_IncludeLocalExecution()
    {
        var capabilities = _adapter.Capabilities;
        
        Assert.Contains("local_execution", capabilities.SpecialFeatures);
        Assert.Contains("privacy_focused", capabilities.SpecialFeatures);
    }

    [Fact]
    public void HandleError_ConnectionRefused_Should_NotRetry()
    {
        var socketEx = new System.Net.Sockets.SocketException();
        var error = new HttpRequestException("Connection refused", socketEx);
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.False(strategy.ShouldRetry);
        Assert.True(strategy.ShouldFallback);
    }

    [Fact]
    public void HandleError_ModelNotFound_Should_SuggestPull()
    {
        var error = new HttpRequestException("Not found", null, HttpStatusCode.NotFound);
        var strategy = _adapter.HandleError(error, attemptNumber: 1);
        
        Assert.Contains("ollama pull", strategy.UserMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculateParameters_Should_IncludeKeepAlive()
    {
        var parameters = _adapter.CalculateParameters(LlmOperationType.Creative, 500);
        
        Assert.NotNull(parameters.ProviderSpecificParams);
    }
}

public class ModelRegistryTests
{
    [Fact]
    public void FindModel_Should_FindKnownModel()
    {
        var modelInfo = ModelRegistry.FindModel("OpenAI", "gpt-4o-mini");
        
        Assert.NotNull(modelInfo);
        Assert.Equal("gpt-4o-mini", modelInfo.ModelId);
        Assert.True(modelInfo.MaxTokens > 0);
    }

    [Fact]
    public void FindModel_Should_FindModelByAlias()
    {
        var modelInfo = ModelRegistry.FindModel("OpenAI", "gpt-4o-latest");
        
        Assert.NotNull(modelInfo);
        Assert.Equal("gpt-4o", modelInfo.ModelId);
    }

    [Fact]
    public void FindModel_Should_DetectOllamaPattern()
    {
        var modelInfo = ModelRegistry.FindModel("Ollama", "llama3.1:latest");
        
        Assert.NotNull(modelInfo);
        Assert.Equal("Ollama", modelInfo.Provider);
        Assert.True(modelInfo.MaxTokens > 0);
    }

    [Fact]
    public void FindModel_Should_ReturnNull_ForUnknownProvider()
    {
        var modelInfo = ModelRegistry.FindModel("UnknownProvider", "unknown-model");
        
        Assert.Null(modelInfo);
    }

    [Fact]
    public void GetDefaultModel_Should_ReturnValidModel()
    {
        var defaultModel = ModelRegistry.GetDefaultModel("OpenAI");
        
        Assert.NotNull(defaultModel);
        Assert.NotEmpty(defaultModel);
    }

    [Fact]
    public void EstimateCapabilities_Should_EstimateFromModelName()
    {
        var (maxTokens, contextWindow) = ModelRegistry.EstimateCapabilities("gpt-4o-super-new");
        
        Assert.True(maxTokens > 0);
        Assert.True(contextWindow > 0);
    }

    [Fact]
    public void GetModelsForProvider_Should_ReturnMultipleModels()
    {
        var models = ModelRegistry.GetModelsForProvider("OpenAI");
        
        Assert.NotEmpty(models);
        Assert.All(models, m => Assert.Equal("OpenAI", m.Provider));
    }
}
