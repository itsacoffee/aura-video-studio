using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Validates LLM provider configuration and availability
/// Performs connectivity tests and API key validation
/// </summary>
public class LlmProviderValidator
{
    private readonly ILogger<LlmProviderValidator> _logger;
    private readonly IKeyStore _keyStore;

    public LlmProviderValidator(
        ILogger<LlmProviderValidator> logger,
        IKeyStore keyStore)
    {
        _logger = logger;
        _keyStore = keyStore;
    }

    /// <summary>
    /// Validates all provider configurations on startup
    /// Returns dictionary of provider names and their validation status
    /// </summary>
    public Dictionary<string, ProviderValidationResult> ValidateAllProviders()
    {
        var results = new Dictionary<string, ProviderValidationResult>();

        _logger.LogInformation("Starting LLM provider configuration validation");

        // Validate RuleBased (always available)
        results["RuleBased"] = new ProviderValidationResult
        {
            ProviderName = "RuleBased",
            IsConfigured = true,
            IsAvailable = true,
            ValidationMessage = "Offline provider - always available"
        };

        // Load API keys
        Dictionary<string, string> apiKeys;
        try
        {
            _keyStore.Reload();
            apiKeys = _keyStore.GetAllKeys();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load API keys during validation");
            apiKeys = new Dictionary<string, string>();
        }

        // Validate OpenAI
        results["OpenAI"] = ValidateOpenAi(apiKeys);

        // Validate Azure OpenAI
        results["Azure"] = ValidateAzureOpenAi(apiKeys);

        // Validate Gemini
        results["Gemini"] = ValidateGemini(apiKeys);

        // Validate Anthropic
        results["Anthropic"] = ValidateAnthropic(apiKeys);

        // Validate Ollama (connectivity check would be async, mark as configured if settings exist)
        results["Ollama"] = ValidateOllama();

        // Log summary
        var configured = results.Count(r => r.Value.IsConfigured);
        var available = results.Count(r => r.Value.IsAvailable);

        _logger.LogInformation(
            "Provider validation complete: {Configured}/{Total} configured, {Available}/{Total} available",
            configured, results.Count, available, results.Count);

        foreach (var result in results.Values)
        {
            if (result.IsAvailable)
            {
                _logger.LogInformation("✓ {Provider}: {Message}", result.ProviderName, result.ValidationMessage);
            }
            else
            {
                _logger.LogWarning("✗ {Provider}: {Message}", result.ProviderName, result.ValidationMessage);
            }
        }

        return results;
    }

    /// <summary>
    /// Tests if a provider is available at runtime
    /// Performs actual connectivity check with minimal API call
    /// </summary>
    public async Task<bool> TestProviderAvailability(
        ILlmProvider provider,
        string providerName,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Testing availability for {Provider}", providerName);

            // Use CompleteAsync with minimal prompt to test connectivity
            var testPrompt = "test";
            var result = await provider.CompleteAsync(testPrompt, ct);

            var isAvailable = !string.IsNullOrWhiteSpace(result);

            _logger.LogInformation(
                "{Provider} availability test: {Status}",
                providerName,
                isAvailable ? "SUCCESS" : "FAILED (empty response)");

            return isAvailable;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{Provider} availability test cancelled", providerName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Provider} availability test failed", providerName);
            return false;
        }
    }

    private ProviderValidationResult ValidateOpenAi(Dictionary<string, string> apiKeys)
    {
        if (!apiKeys.TryGetValue("openai", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new ProviderValidationResult
            {
                ProviderName = "OpenAI",
                IsConfigured = false,
                IsAvailable = false,
                ValidationMessage = "API key not configured"
            };
        }

        // Validate key format
        if (!apiKey.StartsWith("sk-", StringComparison.Ordinal) || apiKey.Length < 40)
        {
            return new ProviderValidationResult
            {
                ProviderName = "OpenAI",
                IsConfigured = true,
                IsAvailable = false,
                ValidationMessage = "API key format appears invalid (should start with 'sk-' and be at least 40 characters)"
            };
        }

        return new ProviderValidationResult
        {
            ProviderName = "OpenAI",
            IsConfigured = true,
            IsAvailable = true,
            ValidationMessage = "API key configured and format validated"
        };
    }

    private ProviderValidationResult ValidateAzureOpenAi(Dictionary<string, string> apiKeys)
    {
        var hasKey = apiKeys.TryGetValue("azure_openai_key", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey);
        var hasEndpoint = apiKeys.TryGetValue("azure_openai_endpoint", out var endpoint) && !string.IsNullOrWhiteSpace(endpoint);

        if (!hasKey && !hasEndpoint)
        {
            return new ProviderValidationResult
            {
                ProviderName = "Azure",
                IsConfigured = false,
                IsAvailable = false,
                ValidationMessage = "API key and endpoint not configured"
            };
        }

        if (!hasKey)
        {
            return new ProviderValidationResult
            {
                ProviderName = "Azure",
                IsConfigured = false,
                IsAvailable = false,
                ValidationMessage = "API key not configured"
            };
        }

        if (!hasEndpoint)
        {
            return new ProviderValidationResult
            {
                ProviderName = "Azure",
                IsConfigured = false,
                IsAvailable = false,
                ValidationMessage = "Endpoint not configured"
            };
        }

        // Validate endpoint format
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return new ProviderValidationResult
            {
                ProviderName = "Azure",
                IsConfigured = true,
                IsAvailable = false,
                ValidationMessage = "Endpoint URL format is invalid"
            };
        }

        return new ProviderValidationResult
        {
            ProviderName = "Azure",
            IsConfigured = true,
            IsAvailable = true,
            ValidationMessage = "API key and endpoint configured"
        };
    }

    private ProviderValidationResult ValidateGemini(Dictionary<string, string> apiKeys)
    {
        if (!apiKeys.TryGetValue("gemini", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new ProviderValidationResult
            {
                ProviderName = "Gemini",
                IsConfigured = false,
                IsAvailable = false,
                ValidationMessage = "API key not configured"
            };
        }

        // Gemini keys should be reasonably long
        if (apiKey.Length < 20)
        {
            return new ProviderValidationResult
            {
                ProviderName = "Gemini",
                IsConfigured = true,
                IsAvailable = false,
                ValidationMessage = "API key appears too short to be valid"
            };
        }

        return new ProviderValidationResult
        {
            ProviderName = "Gemini",
            IsConfigured = true,
            IsAvailable = true,
            ValidationMessage = "API key configured"
        };
    }

    private ProviderValidationResult ValidateAnthropic(Dictionary<string, string> apiKeys)
    {
        if (!apiKeys.TryGetValue("anthropic", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new ProviderValidationResult
            {
                ProviderName = "Anthropic",
                IsConfigured = false,
                IsAvailable = false,
                ValidationMessage = "API key not configured"
            };
        }

        // Anthropic keys typically start with "sk-ant-"
        if (!apiKey.StartsWith("sk-ant-", StringComparison.Ordinal))
        {
            return new ProviderValidationResult
            {
                ProviderName = "Anthropic",
                IsConfigured = true,
                IsAvailable = false,
                ValidationMessage = "API key format appears invalid (should start with 'sk-ant-')"
            };
        }

        return new ProviderValidationResult
        {
            ProviderName = "Anthropic",
            IsConfigured = true,
            IsAvailable = true,
            ValidationMessage = "API key configured and format validated"
        };
    }

    private ProviderValidationResult ValidateOllama()
    {
        return new ProviderValidationResult
        {
            ProviderName = "Ollama",
            IsConfigured = true,
            IsAvailable = true,
            ValidationMessage = "Local provider - availability checked at runtime"
        };
    }
}

/// <summary>
/// Result of provider configuration validation
/// </summary>
public class ProviderValidationResult
{
    public required string ProviderName { get; init; }
    public bool IsConfigured { get; init; }
    public bool IsAvailable { get; init; }
    public required string ValidationMessage { get; init; }
}
