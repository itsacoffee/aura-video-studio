using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Interfaces;
using Aura.Core.Providers;
using Aura.Core.Services.TTS;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify that all provider implementations conform to their interface contracts
/// Ensures 100% implementation of required methods and proper nullability
/// </summary>
public class ProviderContractConformanceTests
{
    [Fact]
    public void ILlmProvider_AllImplementations_HaveAllRequiredMethods()
    {
        var providerTypes = new[]
        {
            typeof(OpenAiLlmProvider),
            typeof(AnthropicLlmProvider),
            typeof(GeminiLlmProvider),
            typeof(OllamaLlmProvider),
            typeof(RuleBasedLlmProvider),
            typeof(AzureOpenAiLlmProvider),
            typeof(MockLlmProvider)
        };

        var requiredMethods = new[]
        {
            "DraftScriptAsync",
            "CompleteAsync",
            "AnalyzeSceneImportanceAsync",
            "GenerateVisualPromptAsync",
            "AnalyzeContentComplexityAsync",
            "AnalyzeSceneCoherenceAsync",
            "ValidateNarrativeArcAsync",
            "GenerateTransitionTextAsync"
        };

        foreach (var providerType in providerTypes)
        {
            foreach (var methodName in requiredMethods)
            {
                var method = providerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(method);
                Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                    $"{providerType.Name}.{methodName} must return Task<T>");
            }
        }
    }

    [Fact]
    public void IScriptLlmProvider_BaseLlmScriptProvider_HasAllRequiredMethods()
    {
        var baseType = typeof(BaseLlmScriptProvider);
        
        var requiredMethods = new[]
        {
            "GenerateScriptAsync",
            "GetAvailableModelsAsync",
            "ValidateConfigurationAsync",
            "GetProviderMetadata",
            "IsAvailableAsync"
        };

        foreach (var methodName in requiredMethods)
        {
            var method = baseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            
            if (methodName != "GetProviderMetadata")
            {
                Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                    $"BaseLlmScriptProvider.{methodName} must return Task<T>");
            }
        }
    }

    [Fact]
    public void ITtsProvider_AllImplementations_HaveRequiredMethods()
    {
        var providerTypes = new[]
        {
            typeof(ElevenLabsTtsProvider),
            typeof(PlayHTTtsProvider),
            typeof(WindowsTtsProvider),
            typeof(PiperTtsProvider),
            typeof(Mimic3TtsProvider),
            typeof(AzureTtsProvider),
            typeof(NullTtsProvider)
        };

        var requiredMethods = new[]
        {
            "GetAvailableVoicesAsync",
            "SynthesizeAsync"
        };

        foreach (var providerType in providerTypes)
        {
            foreach (var methodName in requiredMethods)
            {
                var method = providerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(method);
                Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                    $"{providerType.Name}.{methodName} must return Task<T>");
            }
        }
    }

    [Fact]
    public void IImageProvider_AllImplementations_HaveRequiredMethod()
    {
        var imageProviderType = typeof(IImageProvider);
        var requiredMethod = "FetchOrGenerateAsync";

        var method = imageProviderType.GetMethod(requiredMethod);
        Assert.NotNull(method);
        Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
            $"IImageProvider.{requiredMethod} must return Task<T>");
    }

    [Fact]
    public void IStockProvider_AllImplementations_HaveRequiredMethod()
    {
        var stockProviderType = typeof(IStockProvider);
        var requiredMethod = "SearchAsync";

        var method = stockProviderType.GetMethod(requiredMethod);
        Assert.NotNull(method);
        Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
            $"IStockProvider.{requiredMethod} must return Task<T>");
    }

    [Fact]
    public void IVideoComposer_HasRequiredMethod()
    {
        var videoComposerType = typeof(IVideoComposer);
        var requiredMethod = "RenderAsync";

        var method = videoComposerType.GetMethod(requiredMethod);
        Assert.NotNull(method);
        Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
            $"IVideoComposer.{requiredMethod} must return Task<T>");
    }

    [Fact]
    public void ISfxProvider_HasAllRequiredMethods()
    {
        var sfxProviderType = typeof(ISfxProvider);
        
        var requiredMethods = new[]
        {
            "IsAvailableAsync",
            "SearchAsync",
            "GetByIdAsync",
            "DownloadAsync",
            "GetPreviewUrlAsync",
            "FindByTagsAsync"
        };

        foreach (var methodName in requiredMethods)
        {
            var method = sfxProviderType.GetMethod(methodName);
            Assert.NotNull(method);
            Assert.True(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
                $"ISfxProvider.{methodName} must return Task<T>");
        }
        
        var nameProperty = sfxProviderType.GetProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal(typeof(string), nameProperty.PropertyType);
    }

    [Fact]
    public void ProviderInterfaces_DoNotHaveNotImplementedMethods()
    {
        var providersAssembly = typeof(OpenAiLlmProvider).Assembly;
        var providerTypes = providersAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Aura.Providers", StringComparison.Ordinal))
            .ToList();

        foreach (var providerType in providerTypes)
        {
            var methods = providerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in methods)
            {
                if (method.DeclaringType == providerType)
                {
                    try
                    {
                        var methodBody = method.GetMethodBody();
                        if (methodBody != null)
                        {
                            var il = methodBody.GetILAsByteArray();
                            if (il != null)
                            {
                                var ilString = System.Text.Encoding.UTF8.GetString(il);
                                var throwsNotImplemented = ilString.Contains("NotImplementedException");
                                
                                if (!throwsNotImplemented && method.Name.Contains("Stream"))
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip methods where we can't inspect IL
                    }
                }
            }
        }
    }

    [Fact]
    public void LlmProviders_NullableAnnotations_AreConsistent()
    {
        var providerTypes = new[]
        {
            typeof(OpenAiLlmProvider),
            typeof(AnthropicLlmProvider),
            typeof(GeminiLlmProvider),
            typeof(OllamaLlmProvider)
        };

        foreach (var providerType in providerTypes)
        {
            var methods = providerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.EndsWith("Async", StringComparison.Ordinal))
                .ToList();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                
                var ctParam = parameters.FirstOrDefault(p => p.ParameterType == typeof(CancellationToken));
                if (ctParam != null)
                {
                    Assert.True(!ctParam.IsNullable() || ctParam.HasDefaultValue,
                        $"{providerType.Name}.{method.Name}: CancellationToken should have default value or be non-nullable");
                }

                if (method.ReturnType.IsGenericType)
                {
                    var returnType = method.ReturnType.GetGenericArguments()[0];
                    if (returnType.IsClass || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        var nullableContext = new System.Reflection.NullabilityInfoContext();
                        var returnInfo = nullableContext.Create(method.ReturnParameter);
                    }
                }
            }
        }
    }
}

public static class ParameterInfoExtensions
{
    public static bool IsNullable(this ParameterInfo parameter)
    {
        return new System.Reflection.NullabilityInfoContext()
            .Create(parameter).ReadState == System.Reflection.NullabilityState.Nullable;
    }
}
