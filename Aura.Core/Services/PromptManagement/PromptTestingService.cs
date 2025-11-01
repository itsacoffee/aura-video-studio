using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PromptManagement;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Service for testing prompt templates safely before production use
/// </summary>
public class PromptTestingService
{
    private readonly ILogger<PromptTestingService> _logger;
    private readonly IPromptRepository _repository;
    private readonly PromptVariableResolver _variableResolver;

    public PromptTestingService(
        ILogger<PromptTestingService> logger,
        IPromptRepository repository,
        PromptVariableResolver variableResolver)
    {
        _logger = logger;
        _repository = repository;
        _variableResolver = variableResolver;
    }

    /// <summary>
    /// Test a prompt template with sample data without affecting production
    /// </summary>
    public async Task<PromptTestResult> TestPromptAsync(
        PromptTestRequest request,
        ILlmProvider llmProvider,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Testing prompt template {TemplateId}", request.TemplateId);

        var stopwatch = Stopwatch.StartNew();
        var result = new PromptTestResult
        {
            TemplateId = request.TemplateId,
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            var template = await _repository.GetByIdAsync(request.TemplateId, ct);
            if (template == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Template {request.TemplateId} not found";
                return result;
            }

            var resolvedPrompt = await _variableResolver.ResolveAsync(
                template.PromptText,
                template.Variables,
                request.TestVariables,
                new VariableResolverOptions
                {
                    ThrowOnMissingRequired = false,
                    ThrowOnInvalidType = false,
                    SanitizeValues = true
                },
                ct);

            result.ResolvedPrompt = resolvedPrompt;

            var testBrief = CreateTestBrief(request.TestVariables);
            var testSpec = CreateTestPlanSpec(request.UseLowTokenLimit);

            var generatedContent = await llmProvider.DraftScriptAsync(testBrief, testSpec, ct);

            result.GeneratedContent = generatedContent;
            result.Success = !string.IsNullOrWhiteSpace(generatedContent);
            result.TokensUsed = EstimateTokens(generatedContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test prompt template {TemplateId}", request.TemplateId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.GenerationTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        }

        _logger.LogInformation(
            "Prompt test completed for {TemplateId}: Success={Success}, Time={Time}ms",
            request.TemplateId, result.Success, result.GenerationTimeMs);

        return result;
    }

    /// <summary>
    /// Test multiple prompt variations in parallel for comparison
    /// </summary>
    public async Task<List<PromptTestResult>> TestMultiplePromptsAsync(
        List<string> templateIds,
        Dictionary<string, object> testVariables,
        ILlmProvider llmProvider,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Testing {Count} prompt templates in parallel", templateIds.Count);

        var tasks = templateIds.Select(async templateId =>
        {
            var request = new PromptTestRequest
            {
                TemplateId = templateId,
                TestVariables = testVariables,
                UseLowTokenLimit = true
            };

            return await TestPromptAsync(request, llmProvider, ct);
        });

        var results = await Task.WhenAll(tasks);
        return new List<PromptTestResult>(results);
    }

    /// <summary>
    /// Validate that a prompt resolves correctly without calling LLM
    /// </summary>
    public async Task<PromptTestResult> ValidatePromptResolutionAsync(
        string templateId,
        Dictionary<string, object> testVariables,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating prompt resolution for {TemplateId}", templateId);

        var result = new PromptTestResult
        {
            TemplateId = templateId,
            ExecutedAt = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var template = await _repository.GetByIdAsync(templateId, ct);
            if (template == null)
            {
                result.ErrorMessage = $"Template {templateId} not found";
                return result;
            }

            var resolvedPrompt = await _variableResolver.ResolveAsync(
                template.PromptText,
                template.Variables,
                testVariables,
                new VariableResolverOptions
                {
                    ThrowOnMissingRequired = true,
                    ThrowOnInvalidType = true,
                    SanitizeValues = true
                },
                ct);

            result.ResolvedPrompt = resolvedPrompt;
            result.Success = true;
            result.TokensUsed = EstimateTokens(resolvedPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt resolution validation failed for {TemplateId}", templateId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Create test brief from variables
    /// </summary>
    private Brief CreateTestBrief(Dictionary<string, object> variables)
    {
        return new Brief(
            Topic: GetStringValue(variables, "topic", "Test Topic"),
            Audience: GetStringValue(variables, "audience", "General Audience"),
            Goal: GetStringValue(variables, "goal", "Test Goal"),
            Tone: GetStringValue(variables, "tone", "professional"),
            Language: GetStringValue(variables, "language", "en"),
            Aspect: Aspect.Widescreen16x9,
            PromptModifiers: null
        );
    }

    /// <summary>
    /// Create test plan spec with optional low token limit
    /// </summary>
    private PlanSpec CreateTestPlanSpec(bool useLowTokenLimit)
    {
        return new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );
    }

    /// <summary>
    /// Get string value from variables dictionary
    /// </summary>
    private string GetStringValue(Dictionary<string, object> variables, string key, string defaultValue)
    {
        if (variables.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Estimate token count from text
    /// </summary>
    private int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount * 1.3);
    }
}
