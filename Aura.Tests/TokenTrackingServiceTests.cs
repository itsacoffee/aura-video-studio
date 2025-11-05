using System;
using System.Linq;
using Aura.Core.Configuration;
using Aura.Core.Models.CostTracking;
using Aura.Core.Services.CostTracking;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class TokenTrackingServiceTests
{
    private readonly Mock<ILogger<TokenTrackingService>> _loggerMock;
    private readonly Mock<ILogger<EnhancedCostTrackingService>> _costLoggerMock;
    private readonly Mock<ProviderSettings> _settingsMock;
    private readonly EnhancedCostTrackingService _costTrackingService;
    private readonly TokenTrackingService _service;

    public TokenTrackingServiceTests()
    {
        _loggerMock = new Mock<ILogger<TokenTrackingService>>();
        _costLoggerMock = new Mock<ILogger<EnhancedCostTrackingService>>();
        _settingsMock = new Mock<ProviderSettings>();
        
        _settingsMock.Setup(s => s.GetAuraDataDirectory()).Returns(System.IO.Path.GetTempPath());
        
        _costTrackingService = new EnhancedCostTrackingService(_costLoggerMock.Object, _settingsMock.Object);
        _service = new TokenTrackingService(_loggerMock.Object, _settingsMock.Object, _costTrackingService);
    }

    [Fact]
    public void RecordTokenUsage_ValidMetrics_RecordsSuccessfully()
    {
        var metrics = new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "ScriptGeneration",
            InputTokens = 500,
            OutputTokens = 1000,
            ResponseTimeMs = 5000,
            RetryCount = 0,
            CacheHit = false,
            EstimatedCost = 0.09m,
            JobId = "test-job-1",
            Success = true
        };

        _service.RecordTokenUsage(metrics);

        var jobMetrics = _service.GetJobMetrics("test-job-1");
        Assert.Single(jobMetrics);
        Assert.Equal(metrics.ProviderName, jobMetrics[0].ProviderName);
        Assert.Equal(metrics.InputTokens, jobMetrics[0].InputTokens);
        Assert.Equal(metrics.OutputTokens, jobMetrics[0].OutputTokens);
    }

    [Fact]
    public void GetJobStatistics_SingleOperation_CalculatesCorrectly()
    {
        var metrics = new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "ScriptGeneration",
            InputTokens = 500,
            OutputTokens = 1000,
            ResponseTimeMs = 5000,
            RetryCount = 0,
            CacheHit = false,
            EstimatedCost = 0.09m,
            JobId = "test-job-2",
            Success = true
        };

        _service.RecordTokenUsage(metrics);

        var stats = _service.GetJobStatistics("test-job-2");

        Assert.Equal(500, stats.TotalInputTokens);
        Assert.Equal(1000, stats.TotalOutputTokens);
        Assert.Equal(1500, stats.TotalTokens);
        Assert.Equal(1, stats.OperationCount);
        Assert.Equal(0, stats.CacheHits);
        Assert.Equal(0, stats.CacheHitRate);
        Assert.Equal(0.09m, stats.TotalCost);
    }

    [Fact]
    public void GetJobStatistics_MultipleOperations_AggregatesCorrectly()
    {
        var jobId = "test-job-3";

        _service.RecordTokenUsage(new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "ScriptGeneration",
            InputTokens = 500,
            OutputTokens = 1000,
            ResponseTimeMs = 5000,
            RetryCount = 0,
            CacheHit = false,
            EstimatedCost = 0.09m,
            JobId = jobId,
            Success = true
        });

        _service.RecordTokenUsage(new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "VisualPrompts",
            InputTokens = 300,
            OutputTokens = 500,
            ResponseTimeMs = 3000,
            RetryCount = 0,
            CacheHit = true,
            EstimatedCost = 0.048m,
            JobId = jobId,
            Success = true
        });

        var stats = _service.GetJobStatistics(jobId);

        Assert.Equal(800, stats.TotalInputTokens);
        Assert.Equal(1500, stats.TotalOutputTokens);
        Assert.Equal(2300, stats.TotalTokens);
        Assert.Equal(2, stats.OperationCount);
        Assert.Equal(1, stats.CacheHits);
        Assert.Equal(50.0, stats.CacheHitRate);
        Assert.Equal(0.138m, stats.TotalCost);
        Assert.Equal(0.048m, stats.CostSavedByCache);
    }

    [Fact]
    public void GetJobStatistics_WithFailedOperations_IgnoresFailures()
    {
        var jobId = "test-job-4";

        _service.RecordTokenUsage(new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "ScriptGeneration",
            InputTokens = 500,
            OutputTokens = 1000,
            ResponseTimeMs = 5000,
            RetryCount = 0,
            CacheHit = false,
            EstimatedCost = 0.09m,
            JobId = jobId,
            Success = true
        });

        _service.RecordTokenUsage(new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "VisualPrompts",
            InputTokens = 300,
            OutputTokens = 0,
            ResponseTimeMs = 1000,
            RetryCount = 2,
            CacheHit = false,
            EstimatedCost = 0.0m,
            JobId = jobId,
            Success = false,
            ErrorMessage = "API timeout"
        });

        var stats = _service.GetJobStatistics(jobId);

        Assert.Equal(500, stats.TotalInputTokens);
        Assert.Equal(1000, stats.TotalOutputTokens);
        Assert.Equal(1, stats.OperationCount);
    }

    [Fact]
    public void GenerateOptimizationSuggestions_LowCacheHitRate_SuggestsCaching()
    {
        var jobId = "test-job-5";

        for (int i = 0; i < 10; i++)
        {
            _service.RecordTokenUsage(new TokenUsageMetrics
            {
                ProviderName = "OpenAI",
                ModelName = "gpt-4",
                OperationType = "ScriptGeneration",
                InputTokens = 500,
                OutputTokens = 1000,
                ResponseTimeMs = 5000,
                RetryCount = 0,
                CacheHit = i == 0,
                EstimatedCost = 0.09m,
                JobId = jobId,
                Success = true
            });
        }

        var suggestions = _service.GenerateOptimizationSuggestions(jobId);

        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Category == OptimizationCategory.Caching);
    }

    [Fact]
    public void GenerateOptimizationSuggestions_HighTokenUsage_SuggestsPromptOptimization()
    {
        var jobId = "test-job-6";

        for (int i = 0; i < 5; i++)
        {
            _service.RecordTokenUsage(new TokenUsageMetrics
            {
                ProviderName = "OpenAI",
                ModelName = "gpt-4",
                OperationType = "ScriptGeneration",
                InputTokens = 2000,
                OutputTokens = 2000,
                ResponseTimeMs = 10000,
                RetryCount = 0,
                CacheHit = false,
                EstimatedCost = 0.24m,
                JobId = jobId,
                Success = true
            });
        }

        var suggestions = _service.GenerateOptimizationSuggestions(jobId);

        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Category == OptimizationCategory.PromptOptimization);
    }

    [Fact]
    public void GetStatisticsByOperation_GroupsCorrectly()
    {
        var startDate = DateTime.UtcNow.AddHours(-1);
        var endDate = DateTime.UtcNow;

        _service.RecordTokenUsage(new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "ScriptGeneration",
            InputTokens = 500,
            OutputTokens = 1000,
            ResponseTimeMs = 5000,
            RetryCount = 0,
            CacheHit = false,
            EstimatedCost = 0.09m,
            JobId = "job1",
            Success = true
        });

        _service.RecordTokenUsage(new TokenUsageMetrics
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "VisualPrompts",
            InputTokens = 300,
            OutputTokens = 500,
            ResponseTimeMs = 3000,
            RetryCount = 0,
            CacheHit = false,
            EstimatedCost = 0.048m,
            JobId = "job1",
            Success = true
        });

        var statsByOp = _service.GetStatisticsByOperation(startDate, endDate);

        Assert.Equal(2, statsByOp.Count);
        Assert.True(statsByOp.ContainsKey("ScriptGeneration"));
        Assert.True(statsByOp.ContainsKey("VisualPrompts"));
        Assert.Equal(1500, statsByOp["ScriptGeneration"].TotalTokens);
        Assert.Equal(800, statsByOp["VisualPrompts"].TotalTokens);
    }
}
