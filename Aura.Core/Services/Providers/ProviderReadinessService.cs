using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Validates that required provider categories (LLM, TTS, Images, etc.) have at least one
/// configured and reachable provider before a generation pipeline starts.
/// </summary>
public interface IProviderReadinessService
{
    /// <summary>
    /// Validate that all required provider categories have at least one healthy provider.
    /// </summary>
    Task<ProviderReadinessResult> ValidateRequiredProvidersAsync(CancellationToken ct = default);
}

/// <inheritdoc />
public class ProviderReadinessService : IProviderReadinessService
{
    private static readonly IReadOnlyList<ProviderCategoryRequirement> DefaultRequirements = new[]
    {
        new ProviderCategoryRequirement(
            "LLM",
            new[]
            {
                "OpenAI",
                "Anthropic",
                "Gemini",
                "AzureOpenAI",
                "Ollama",
                "RuleBased"
            }),
        new ProviderCategoryRequirement(
            "TTS",
            new[]
            {
                "ElevenLabs",
                "PlayHT",
                "Piper",
                "Mimic3",
                "WindowsTTS"
            }),
        new ProviderCategoryRequirement(
            "Images",
            new[]
            {
                "StableDiffusion",
                "Pexels",
                "Pixabay",
                "Unsplash",
                "PlaceholderImages"
            })
    };

    private readonly ILogger<ProviderReadinessService> _logger;
    private readonly ProviderConnectionValidationService _validationService;
    private readonly IReadOnlyList<ProviderCategoryRequirement> _requirements;

    public ProviderReadinessService(
        ILogger<ProviderReadinessService> logger,
        ProviderConnectionValidationService validationService,
        IEnumerable<ProviderCategoryRequirement>? requirements = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        var requirementList = (requirements ?? DefaultRequirements)?.ToList();

        if (requirementList == null || requirementList.Count == 0)
        {
            requirementList = DefaultRequirements.ToList();
        }

        _requirements = requirementList;
    }

    public async Task<ProviderReadinessResult> ValidateRequiredProvidersAsync(CancellationToken ct = default)
    {
        var result = new ProviderReadinessResult();

        foreach (var requirement in _requirements)
        {
            var categoryStatus = await ValidateCategoryAsync(requirement, ct).ConfigureAwait(false);
            result.CategoryStatuses.Add(categoryStatus);

            if (!categoryStatus.Ready)
            {
                _logger.LogWarning(
                    "Provider readiness check failed for category {Category}: {Message}",
                    categoryStatus.Category,
                    categoryStatus.Message ?? "Unknown error");
            }
            else
            {
                _logger.LogDebug(
                    "Provider readiness check passed for category {Category} using provider {Provider}",
                    categoryStatus.Category,
                    categoryStatus.Provider);
            }
        }

        return result;
    }

    private async Task<ProviderCategoryStatus> ValidateCategoryAsync(
        ProviderCategoryRequirement requirement,
        CancellationToken ct)
    {
        var candidates = new List<ProviderCandidateStatus>();

        foreach (var providerName in requirement.ProviderNames)
        {
            var validation = await _validationService.ValidateProviderAsync(providerName, ct).ConfigureAwait(false);

            var candidateStatus = new ProviderCandidateStatus(
                providerName,
                validation.Configured,
                validation.Reachable,
                validation.ErrorCode,
                validation.ErrorMessage,
                validation.HowToFix?.ToArray() ?? Array.Empty<string>());

            candidates.Add(candidateStatus);

            if (validation.Configured && validation.Reachable)
            {
                return new ProviderCategoryStatus(
                    requirement.Category,
                    true,
                    providerName,
                    null,
                    $"{providerName} is ready",
                    Array.Empty<string>(),
                    candidates);
            }
        }

        // No provider succeeded - surface the last failure (most recent) for context.
        var lastFailure = candidates.LastOrDefault();
        var message = lastFailure is null
            ? $"No providers defined for required category {requirement.Category}"
            : $"All {requirement.Category} providers failed. Last attempt {lastFailure.Provider}: {lastFailure.Message ?? "Not reachable"}";

        var suggestions = lastFailure?.Suggestions ?? Array.Empty<string>();

        return new ProviderCategoryStatus(
            requirement.Category,
            false,
            null,
            lastFailure?.ErrorCode,
            message,
            suggestions,
            candidates);
    }
}

/// <summary>
/// Required provider category definition.
/// </summary>
public record ProviderCategoryRequirement(string Category, IReadOnlyList<string> ProviderNames);

/// <summary>
/// Aggregated provider readiness result.
/// </summary>
public class ProviderReadinessResult
{
    public List<ProviderCategoryStatus> CategoryStatuses { get; } = new();

    public bool IsReady => CategoryStatuses.All(status => status.Ready);

    public IReadOnlyList<string> Issues =>
        CategoryStatuses
            .Where(status => !status.Ready)
            .Select(status =>
                status.Message ??
                $"No available providers for category {status.Category}. Configure at least one provider in Settings.")
            .ToList();
}

/// <summary>
/// Status summary for a single provider category.
/// </summary>
public record ProviderCategoryStatus(
    string Category,
    bool Ready,
    string? Provider,
    string? ErrorCode,
    string? Message,
    IReadOnlyList<string> Suggestions,
    IReadOnlyList<ProviderCandidateStatus> Candidates);

/// <summary>
/// Status for each provider candidate evaluated inside a category.
/// </summary>
public record ProviderCandidateStatus(
    string Provider,
    bool Configured,
    bool Reachable,
    string? ErrorCode,
    string? Message,
    IReadOnlyList<string> Suggestions);

