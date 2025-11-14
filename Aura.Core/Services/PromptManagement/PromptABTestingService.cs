using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PromptManagement;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Service for A/B testing prompt template variations
/// </summary>
public class PromptABTestingService
{
    private readonly ILogger<PromptABTestingService> _logger;
    private readonly IPromptRepository _repository;
    private readonly PromptTestingService _testingService;

    public PromptABTestingService(
        ILogger<PromptABTestingService> logger,
        IPromptRepository repository,
        PromptTestingService testingService)
    {
        _logger = logger;
        _repository = repository;
        _testingService = testingService;
    }

    /// <summary>
    /// Create a new A/B test
    /// </summary>
    public async Task<PromptABTest> CreateABTestAsync(
        string name,
        string description,
        List<string> templateIds,
        string createdBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating A/B test '{Name}' with {Count} templates by {User}",
            name, templateIds.Count, createdBy);

        if (templateIds.Count < 2)
        {
            throw new ArgumentException("A/B test requires at least 2 template variations");
        }

        if (templateIds.Count > 5)
        {
            throw new ArgumentException("A/B test supports maximum 5 template variations");
        }

        foreach (var templateId in templateIds)
        {
            var template = await _repository.GetByIdAsync(templateId, ct).ConfigureAwait(false);
            if (template == null)
            {
                throw new ArgumentException($"Template {templateId} not found");
            }
        }

        var test = new PromptABTest
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            TemplateIds = templateIds,
            Status = ABTestStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateABTestAsync(test, ct).ConfigureAwait(false);

        _logger.LogInformation("Created A/B test {TestId}: {Name}", test.Id, name);
        return test;
    }

    /// <summary>
    /// Run an A/B test with sample data
    /// </summary>
    public async Task<PromptABTest> RunABTestAsync(
        string testId,
        Dictionary<string, object> testVariables,
        ILlmProvider llmProvider,
        int iterations = 3,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Running A/B test {TestId} with {Iterations} iterations", 
            testId, iterations);

        var test = await _repository.GetABTestAsync(testId, ct).ConfigureAwait(false);
        if (test == null)
        {
            throw new ArgumentException($"A/B test {testId} not found");
        }

        if (test.Status == ABTestStatus.Running)
        {
            throw new InvalidOperationException("Test is already running");
        }

        if (test.Status == ABTestStatus.Completed)
        {
            throw new InvalidOperationException("Test is already completed");
        }

        test.Status = ABTestStatus.Running;
        test.StartedAt = DateTime.UtcNow;
        await _repository.UpdateABTestAsync(test, ct).ConfigureAwait(false);

        try
        {
            var allResults = new List<ABTestResult>();

            for (int i = 0; i < iterations; i++)
            {
                _logger.LogDebug("Running iteration {Iteration} of {Total}", i + 1, iterations);

                var testResults = await _testingService.TestMultiplePromptsAsync(
                    test.TemplateIds,
                    testVariables,
                    llmProvider,
                    ct).ConfigureAwait(false);

                foreach (var testResult in testResults)
                {
                    var template = await _repository.GetByIdAsync(testResult.TemplateId, ct).ConfigureAwait(false);
                    
                    var result = new ABTestResult
                    {
                        TemplateId = testResult.TemplateId,
                        TemplateName = template?.Name ?? "Unknown",
                        QualityScore = CalculateQualityScore(testResult),
                        GenerationTimeMs = testResult.GenerationTimeMs,
                        TokenUsage = testResult.TokensUsed,
                        Success = testResult.Success,
                        ExecutedAt = testResult.ExecutedAt
                    };

                    allResults.Add(result);
                }
            }

            test.Results = allResults;
            test.Status = ABTestStatus.Completed;
            test.CompletedAt = DateTime.UtcNow;

            DetermineWinner(test);

            await _repository.UpdateABTestAsync(test, ct).ConfigureAwait(false);

            _logger.LogInformation("Completed A/B test {TestId}. Winner: {Winner}",
                testId, test.WinningTemplateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A/B test {TestId} failed", testId);
            test.Status = ABTestStatus.Cancelled;
            await _repository.UpdateABTestAsync(test, ct).ConfigureAwait(false);
            throw;
        }

        return test;
    }

    /// <summary>
    /// Get A/B test results
    /// </summary>
    public async Task<PromptABTest?> GetABTestResultsAsync(
        string testId,
        CancellationToken ct = default)
    {
        return await _repository.GetABTestAsync(testId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// List all A/B tests
    /// </summary>
    public async Task<List<PromptABTest>> ListABTestsAsync(
        ABTestStatus? status = null,
        CancellationToken ct = default)
    {
        return await _repository.ListABTestsAsync(status, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancel a running A/B test
    /// </summary>
    public async Task CancelABTestAsync(string testId, CancellationToken ct = default)
    {
        _logger.LogInformation("Cancelling A/B test {TestId}", testId);

        var test = await _repository.GetABTestAsync(testId, ct).ConfigureAwait(false);
        if (test == null)
        {
            throw new ArgumentException($"A/B test {testId} not found");
        }

        if (test.Status != ABTestStatus.Running)
        {
            throw new InvalidOperationException("Only running tests can be cancelled");
        }

        test.Status = ABTestStatus.Cancelled;
        await _repository.UpdateABTestAsync(test, ct).ConfigureAwait(false);

        _logger.LogInformation("Cancelled A/B test {TestId}", testId);
    }

    /// <summary>
    /// Get summary statistics for an A/B test
    /// </summary>
    public async Task<Dictionary<string, ABTestSummary>> GetABTestSummaryAsync(
        string testId,
        CancellationToken ct = default)
    {
        var test = await _repository.GetABTestAsync(testId, ct).ConfigureAwait(false);
        if (test == null)
        {
            throw new ArgumentException($"A/B test {testId} not found");
        }

        var summary = new Dictionary<string, ABTestSummary>();

        foreach (var templateId in test.TemplateIds)
        {
            var results = test.Results.Where(r => r.TemplateId == templateId).ToList();
            
            if (results.Count != 0)
            {
                summary[templateId] = new ABTestSummary
                {
                    TemplateId = templateId,
                    TemplateName = results.First().TemplateName,
                    AverageQualityScore = results.Average(r => r.QualityScore),
                    AverageGenerationTimeMs = results.Average(r => r.GenerationTimeMs),
                    AverageTokenUsage = (int)results.Average(r => r.TokenUsage),
                    SuccessRate = results.Count(r => r.Success) / (double)results.Count,
                    TotalRuns = results.Count
                };
            }
        }

        return summary;
    }

    /// <summary>
    /// Calculate quality score from test result
    /// </summary>
    private double CalculateQualityScore(PromptTestResult result)
    {
        if (!result.Success)
            return 0.0;

        var baseScore = 50.0;

        if (!string.IsNullOrEmpty(result.GeneratedContent))
        {
            var contentLength = result.GeneratedContent.Length;
            if (contentLength > 100 && contentLength < 5000)
                baseScore += 20.0;
            else if (contentLength >= 50)
                baseScore += 10.0;
        }

        if (result.GenerationTimeMs < 3000)
            baseScore += 15.0;
        else if (result.GenerationTimeMs < 5000)
            baseScore += 10.0;
        else if (result.GenerationTimeMs < 10000)
            baseScore += 5.0;

        if (result.TokensUsed > 0 && result.TokensUsed < 1000)
            baseScore += 15.0;
        else if (result.TokensUsed < 2000)
            baseScore += 10.0;

        return Math.Min(baseScore, 100.0);
    }

    /// <summary>
    /// Determine winner based on aggregate scores
    /// </summary>
    private void DetermineWinner(PromptABTest test)
    {
        var scores = new Dictionary<string, double>();

        foreach (var templateId in test.TemplateIds)
        {
            var results = test.Results.Where(r => r.TemplateId == templateId).ToList();
            if (results.Count != 0)
            {
                scores[templateId] = results.Average(r => r.QualityScore);
            }
        }

        if (scores.Count != 0)
        {
            test.WinningTemplateId = scores.OrderByDescending(kvp => kvp.Value).First().Key;
        }
    }
}

/// <summary>
/// Summary statistics for a template in an A/B test
/// </summary>
public class ABTestSummary
{
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public double AverageQualityScore { get; set; }
    public double AverageGenerationTimeMs { get; set; }
    public int AverageTokenUsage { get; set; }
    public double SuccessRate { get; set; }
    public int TotalRuns { get; set; }
}
