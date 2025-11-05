using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.AI.Orchestration;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests demonstrating telemetry collection in a full video generation workflow
/// </summary>
public class TelemetryIntegrationTests : IDisposable
{
    private readonly string _testArtifactsPath;
    private readonly RunTelemetryCollector _collector;
    private readonly TelemetryIntegration _integration;
    
    public TelemetryIntegrationTests()
    {
        _testArtifactsPath = Path.Combine(Path.GetTempPath(), "aura-telemetry-integration", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testArtifactsPath);
        _collector = new RunTelemetryCollector(NullLogger<RunTelemetryCollector>.Instance, _testArtifactsPath);
        _integration = new TelemetryIntegration(NullLogger<TelemetryIntegration>.Instance, _collector);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testArtifactsPath))
        {
            Directory.Delete(_testArtifactsPath, true);
        }
        GC.SuppressFinalize(this);
    }
    
    [Fact]
    public async Task SimulatedVideoGenerationRun_EmitsCompleteTelemetry()
    {
        var jobId = "test-video-job";
        var correlationId = "corr-integration-test";
        
        _collector.StartCollection(jobId, correlationId);
        
        await SimulateBriefStage(jobId, correlationId);
        await SimulatePlanStage(jobId, correlationId);
        await SimulateScriptStage(jobId, correlationId);
        await SimulateTtsStage(jobId, correlationId, sceneCount: 3);
        await SimulateVisualsStage(jobId, correlationId, sceneCount: 3);
        await SimulateRenderStage(jobId, correlationId);
        await SimulatePostStage(jobId, correlationId);
        
        var filePath = _collector.EndCollection();
        
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
        
        var telemetry = _collector.LoadTelemetry(jobId);
        Assert.NotNull(telemetry);
        Assert.Equal(jobId, telemetry.JobId);
        Assert.Equal(correlationId, telemetry.CorrelationId);
        
        Assert.True(telemetry.Records.Count >= 8);
        
        var summary = telemetry.Summary;
        Assert.NotNull(summary);
        Assert.True(summary.TotalOperations >= 8);
        Assert.True(summary.TotalLatencyMs > 0);
        Assert.True(summary.TotalCost > 0);
        Assert.True(summary.TotalTokensIn > 0);
        Assert.True(summary.TotalTokensOut > 0);
        
        var stagesRecorded = telemetry.Records.Select(r => r.Stage).Distinct().ToList();
        Assert.Contains(RunStage.Brief, stagesRecorded);
        Assert.Contains(RunStage.Plan, stagesRecorded);
        Assert.Contains(RunStage.Script, stagesRecorded);
        Assert.Contains(RunStage.Tts, stagesRecorded);
        Assert.Contains(RunStage.Visuals, stagesRecorded);
        Assert.Contains(RunStage.Render, stagesRecorded);
        Assert.Contains(RunStage.Post, stagesRecorded);
    }
    
    [Fact]
    public void TelemetryIntegration_RecordsLlmOperation_SuccessfullyMapsFields()
    {
        var jobId = "test-llm-job";
        var correlationId = "corr-llm-test";
        
        _collector.StartCollection(jobId, correlationId);
        
        var llmTelemetry = new LlmOperationTelemetry
        {
            SessionId = jobId,
            OperationType = LlmOperationType.Script,
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            TokensIn = 500,
            TokensOut = 1000,
            LatencyMs = 2500,
            Success = true,
            CacheHit = false,
            EstimatedCost = 0.075m,
            StartedAt = DateTime.UtcNow.AddSeconds(-3),
            CompletedAt = DateTime.UtcNow,
            RetryCount = 0
        };
        
        _integration.RecordLlmOperation(jobId, correlationId, RunStage.Script, llmTelemetry);
        
        var filePath = _collector.EndCollection();
        var telemetry = _collector.LoadTelemetry(jobId);
        
        Assert.NotNull(telemetry);
        Assert.Single(telemetry.Records);
        
        var record = telemetry.Records.First();
        Assert.Equal(RunStage.Script, record.Stage);
        Assert.Equal("OpenAI", record.Provider);
        Assert.Equal("gpt-4", record.ModelId);
        Assert.Equal(500, record.TokensIn);
        Assert.Equal(1000, record.TokensOut);
        Assert.Equal(2500, record.LatencyMs);
        Assert.False(record.CacheHit);
        Assert.Equal(0.075m, record.CostEstimate);
        Assert.Equal(ResultStatus.Ok, record.ResultStatus);
    }
    
    [Fact]
    public void TelemetryIntegration_RecordsFailedOperation_CapturesErrorDetails()
    {
        var jobId = "test-error-job";
        var correlationId = "corr-error-test";
        
        _collector.StartCollection(jobId, correlationId);
        
        var llmTelemetry = new LlmOperationTelemetry
        {
            SessionId = jobId,
            OperationType = LlmOperationType.Script,
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            TokensIn = 100,
            TokensOut = 0,
            LatencyMs = 500,
            Success = false,
            ErrorMessage = "API rate limit exceeded",
            StartedAt = DateTime.UtcNow.AddSeconds(-1),
            CompletedAt = DateTime.UtcNow,
            RetryCount = 3
        };
        
        _integration.RecordLlmOperation(jobId, correlationId, RunStage.Script, llmTelemetry);
        
        var filePath = _collector.EndCollection();
        var telemetry = _collector.LoadTelemetry(jobId);
        
        Assert.NotNull(telemetry);
        var record = telemetry.Records.First();
        
        Assert.Equal(ResultStatus.Error, record.ResultStatus);
        Assert.Equal("LLM_ERROR", record.ErrorCode);
        Assert.Contains("rate limit", record.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, record.Retries);
    }
    
    [Fact]
    public void StopwatchExtension_RecordsTelemetry_WithCorrectTiming()
    {
        var jobId = "test-stopwatch-job";
        var correlationId = "corr-stopwatch-test";
        
        _collector.StartCollection(jobId, correlationId);
        
        var stopwatch = Stopwatch.StartNew();
        System.Threading.Thread.Sleep(100);
        stopwatch.Stop();
        
        stopwatch.RecordTelemetry(
            _integration,
            jobId,
            correlationId,
            RunStage.Render,
            ResultStatus.Ok,
            "Render completed successfully");
        
        var filePath = _collector.EndCollection();
        var telemetry = _collector.LoadTelemetry(jobId);
        
        Assert.NotNull(telemetry);
        var record = telemetry.Records.First();
        
        Assert.Equal(RunStage.Render, record.Stage);
        Assert.True(record.LatencyMs >= 100);
        Assert.Equal(ResultStatus.Ok, record.ResultStatus);
        Assert.Equal("Render completed successfully", record.Message);
    }
    
    private async Task SimulateBriefStage(string jobId, string correlationId)
    {
        await Task.Delay(10);
        
        var llmTelemetry = new LlmOperationTelemetry
        {
            SessionId = jobId,
            OperationType = LlmOperationType.Brief,
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            TokensIn = 50,
            TokensOut = 100,
            LatencyMs = 500,
            Success = true,
            CacheHit = false,
            EstimatedCost = 0.0015m,
            StartedAt = DateTime.UtcNow.AddMilliseconds(-500),
            CompletedAt = DateTime.UtcNow
        };
        
        _integration.RecordLlmOperation(jobId, correlationId, RunStage.Brief, llmTelemetry);
    }
    
    private async Task SimulatePlanStage(string jobId, string correlationId)
    {
        await Task.Delay(10);
        
        var llmTelemetry = new LlmOperationTelemetry
        {
            SessionId = jobId,
            OperationType = LlmOperationType.Plan,
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            TokensIn = 200,
            TokensOut = 300,
            LatencyMs = 1200,
            Success = true,
            CacheHit = false,
            EstimatedCost = 0.015m,
            StartedAt = DateTime.UtcNow.AddMilliseconds(-1200),
            CompletedAt = DateTime.UtcNow
        };
        
        _integration.RecordLlmOperation(jobId, correlationId, RunStage.Plan, llmTelemetry);
    }
    
    private async Task SimulateScriptStage(string jobId, string correlationId)
    {
        await Task.Delay(10);
        
        var llmTelemetry = new LlmOperationTelemetry
        {
            SessionId = jobId,
            OperationType = LlmOperationType.Script,
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            TokensIn = 500,
            TokensOut = 1000,
            LatencyMs = 2500,
            Success = true,
            CacheHit = false,
            EstimatedCost = 0.045m,
            StartedAt = DateTime.UtcNow.AddMilliseconds(-2500),
            CompletedAt = DateTime.UtcNow
        };
        
        _integration.RecordLlmOperation(jobId, correlationId, RunStage.Script, llmTelemetry);
    }
    
    private async Task SimulateTtsStage(string jobId, string correlationId, int sceneCount)
    {
        for (int i = 0; i < sceneCount; i++)
        {
            await Task.Delay(5);
            
            var record = TelemetryExtensions.CreateTtsTelemetry(
                jobId,
                correlationId,
                i,
                "ElevenLabs",
                150,
                8.5,
                3000,
                0.045m);
            
            _collector.Record(record);
        }
    }
    
    private async Task SimulateVisualsStage(string jobId, string correlationId, int sceneCount)
    {
        for (int i = 0; i < sceneCount; i++)
        {
            await Task.Delay(5);
            
            _integration.RecordStage(
                jobId,
                correlationId,
                RunStage.Visuals,
                5000,
                ResultStatus.Ok,
                $"Visual generated for scene {i}");
        }
    }
    
    private async Task SimulateRenderStage(string jobId, string correlationId)
    {
        await Task.Delay(20);
        
        _integration.RecordStage(
            jobId,
            correlationId,
            RunStage.Render,
            15000,
            ResultStatus.Ok,
            "Video rendered successfully");
    }
    
    private async Task SimulatePostStage(string jobId, string correlationId)
    {
        await Task.Delay(5);
        
        _integration.RecordStage(
            jobId,
            correlationId,
            RunStage.Post,
            2000,
            ResultStatus.Ok,
            "Post-processing completed");
    }
}
