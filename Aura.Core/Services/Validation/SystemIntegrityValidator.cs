using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Validation;

/// <summary>
/// Validates that all system components are properly wired up
/// </summary>
public class SystemIntegrityValidator
{
    private readonly ILogger<SystemIntegrityValidator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SystemIntegrityValidator(
        ILogger<SystemIntegrityValidator> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Validates system integrity
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting system integrity validation");

        var result = new ValidationResult
        {
            IsValid = true,
            ValidationName = "System Integrity"
        };

        // Check critical services
        await ValidateServiceAsync<IntelligentContentAdvisor>(result, "IntelligentContentAdvisor", required: false).ConfigureAwait(false);
        await ValidateEnumerableServiceAsync<ILlmProvider>(result, "ILlmProvider", required: true).ConfigureAwait(false);
        await ValidateEnumerableServiceAsync<ITtsProvider>(result, "ITtsProvider", required: false).ConfigureAwait(false);

        // Validate EnhancedPromptTemplates
        ValidateEnhancedPromptTemplates(result);

        result.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "System integrity validation complete: {IsValid} ({SuccessCount}/{TotalCount} checks passed)",
            result.IsValid,
            result.SuccessCount,
            result.TotalChecks);

        return result;
    }

    private async Task ValidateServiceAsync<T>(ValidationResult result, string serviceName, bool required) where T : class
    {
        result.TotalChecks++;

        try
        {
            var service = _serviceProvider.GetService(typeof(T)) as T;
            
            if (service == null)
            {
                if (required)
                {
                    result.Errors.Add($"Required service {serviceName} is not registered");
                    result.IsValid = false;
                }
                else
                {
                    result.Warnings.Add($"Optional service {serviceName} is not registered");
                }
            }
            else
            {
                result.SuccessCount++;
                result.Details.Add($"{serviceName} is properly registered");
                _logger.LogDebug("{Service} validation passed", serviceName);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating {serviceName}: {ex.Message}");
            result.IsValid = false;
            _logger.LogError(ex, "Error validating {Service}", serviceName);
        }
    }

    private async Task ValidateEnumerableServiceAsync<T>(ValidationResult result, string serviceName, bool required) where T : class
    {
        result.TotalChecks++;

        try
        {
            var services = _serviceProvider.GetService(typeof(IEnumerable<T>)) as IEnumerable<T>;
            
            if (services == null || !services.Any())
            {
                if (required)
                {
                    result.Errors.Add($"No {serviceName} implementations are registered");
                    result.IsValid = false;
                }
                else
                {
                    result.Warnings.Add($"No {serviceName} implementations are registered");
                }
            }
            else
            {
                var count = services.Count();
                result.SuccessCount++;
                result.Details.Add($"{count} {serviceName} implementation(s) registered");
                _logger.LogDebug("{Count} {Service} implementations found", count, serviceName);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating {serviceName}: {ex.Message}");
            result.IsValid = false;
            _logger.LogError(ex, "Error validating {Service}", serviceName);
        }
    }

    private void ValidateEnhancedPromptTemplates(ValidationResult result)
    {
        result.TotalChecks++;

        try
        {
            // Test that prompt templates can be generated
            var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
            var qualityPrompt = EnhancedPromptTemplates.GetSystemPromptForQualityValidation();
            var visualPrompt = EnhancedPromptTemplates.GetSystemPromptForVisualSelection();

            if (string.IsNullOrWhiteSpace(systemPrompt) ||
                string.IsNullOrWhiteSpace(qualityPrompt) ||
                string.IsNullOrWhiteSpace(visualPrompt))
            {
                result.Errors.Add("EnhancedPromptTemplates returned empty prompts");
                result.IsValid = false;
            }
            else
            {
                result.SuccessCount++;
                result.Details.Add("EnhancedPromptTemplates is functional");
                _logger.LogDebug("EnhancedPromptTemplates validation passed");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating EnhancedPromptTemplates: {ex.Message}");
            result.IsValid = false;
            _logger.LogError(ex, "Error validating EnhancedPromptTemplates");
        }
    }
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ValidationName { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Details { get; set; } = new();
    public int TotalChecks { get; set; }
    public int SuccessCount { get; set; }
    public DateTime CompletedAt { get; set; }
}
