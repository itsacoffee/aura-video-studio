using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Performance;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class LlmOperationContextTests
{
    private readonly ILogger<LlmOperationContext> _logger;
    private readonly ILogger<LatencyManagementService> _latencyLogger;
    private readonly ILogger<LatencyTelemetry> _telemetryLogger;
    private readonly LatencyTelemetry _telemetry;
    private readonly LatencyManagementService _latencyService;
    private readonly LlmOperationContext _context;

    public LlmOperationContextTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _logger = loggerFactory.CreateLogger<LlmOperationContext>();
        _latencyLogger = loggerFactory.CreateLogger<LatencyManagementService>();
        _telemetryLogger = loggerFactory.CreateLogger<LatencyTelemetry>();
        _telemetry = new LatencyTelemetry(_telemetryLogger);
        
        var policy = new LlmTimeoutPolicy
        {
            ScriptGenerationTimeoutSeconds = 10
        };
        
        _latencyService = new LatencyManagementService(_latencyLogger, _telemetry, policy);
        _context = new LlmOperationContext(_logger, _latencyService, _telemetry);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = "Generated script content";

        // Act
        var result = await _context.ExecuteAsync(
            "OpenAI",
            "ScriptGeneration",
            async ct =>
            {
                await Task.Delay(100, ct);
                return expectedResult;
            },
            500);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_RecordsMetrics()
    {
        // Arrange & Act
        await _context.ExecuteAsync(
            "OpenAI",
            "ScriptGeneration",
            async ct =>
            {
                await Task.Delay(100, ct);
                return "result";
            },
            500);

        var summary = _latencyService.GetPerformanceSummary("OpenAI", "ScriptGeneration");

        // Assert
        Assert.Equal(1, summary.DataPointCount);
        Assert.True(summary.AverageResponseTimeMs >= 100);
        Assert.Equal(1.0, summary.SuccessRate);
    }

    [Fact]
    public async Task ExecuteAsync_OperationTimeout_ThrowsTimeoutException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await _context.ExecuteAsync(
                "OpenAI",
                "ScriptGeneration",
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);
                    return "result";
                },
                500);
        });
    }

    [Fact]
    public async Task ExecuteAsync_OperationFails_RecordsFailureMetrics()
    {
        // Arrange & Act
        try
        {
            await _context.ExecuteAsync<string>(
                "OpenAI",
                "ScriptGeneration",
                ct => throw new InvalidOperationException("Test error"),
                500);
        }
        catch
        {
            // Expected
        }

        var summary = _latencyService.GetPerformanceSummary("OpenAI", "ScriptGeneration");

        // Assert
        Assert.Equal(1, summary.DataPointCount);
        Assert.Equal(0.0, summary.SuccessRate);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _context.ExecuteAsync(
                "OpenAI",
                "ScriptGeneration",
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    return "result";
                },
                500,
                cancellationToken: cts.Token);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var progressReports = new System.Collections.Concurrent.ConcurrentBag<LlmOperationProgress>();
        var progress = new Progress<LlmOperationProgress>(p => progressReports.Add(p));

        // Act
        await _context.ExecuteAsync(
            "OpenAI",
            "ScriptGeneration",
            async ct =>
            {
                await Task.Delay(1000, ct);
                return "result";
            },
            500,
            progress);

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Stage == "ScriptGeneration");
    }

    [Fact]
    public async Task ExecuteAsync_SlowOperation_ReportsWarning()
    {
        // Arrange
        var progressReports = new System.Collections.Concurrent.ConcurrentBag<LlmOperationProgress>();
        var progress = new Progress<LlmOperationProgress>(p => progressReports.Add(p));

        var policy = new LlmTimeoutPolicy
        {
            ScriptGenerationTimeoutSeconds = 10,
            WarningThresholdPercentage = 0.3
        };
        
        var latencyService = new LatencyManagementService(_latencyLogger, _telemetry, policy);
        var context = new LlmOperationContext(_logger, latencyService, _telemetry);

        // Act
        await context.ExecuteAsync(
            "OpenAI",
            "ScriptGeneration",
            async ct =>
            {
                await Task.Delay(4000, ct);
                return "result";
            },
            500,
            progress);

        // Assert
        Assert.Contains(progressReports, p => p.IsWarning);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleOperations_TracksIndependently()
    {
        // Arrange & Act
        await _context.ExecuteAsync("OpenAI", "ScriptGeneration", async ct =>
        {
            await Task.Delay(100, ct);
            return "script";
        }, 500);

        await _context.ExecuteAsync("OpenAI", "VisualPrompt", async ct =>
        {
            await Task.Delay(200, ct);
            return "prompt";
        }, 300);

        var scriptSummary = _latencyService.GetPerformanceSummary("OpenAI", "ScriptGeneration");
        var promptSummary = _latencyService.GetPerformanceSummary("OpenAI", "VisualPrompt");

        // Assert
        Assert.Equal(1, scriptSummary.DataPointCount);
        Assert.Equal(1, promptSummary.DataPointCount);
        Assert.NotEqual(scriptSummary.AverageResponseTimeMs, promptSummary.AverageResponseTimeMs);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutDoesNotAffectResult_WhenOperationCompletes()
    {
        // Arrange
        var expectedResult = "Generated content";

        // Act
        var result = await _context.ExecuteAsync(
            "OpenAI",
            "ScriptGeneration",
            async ct =>
            {
                await Task.Delay(500, ct);
                return expectedResult;
            },
            500);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
