using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class RunTelemetryTests : IDisposable
{
    private readonly string _testArtifactsPath;
    private readonly RunTelemetryCollector _collector;
    
    public RunTelemetryTests()
    {
        _testArtifactsPath = Path.Combine(Path.GetTempPath(), "aura-telemetry-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testArtifactsPath);
        _collector = new RunTelemetryCollector(NullLogger<RunTelemetryCollector>.Instance, _testArtifactsPath);
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
    public void RunTelemetryRecord_SerializesToJson_MatchesSchema()
    {
        var record = new RunTelemetryRecord
        {
            JobId = "test-job-123",
            CorrelationId = "corr-456",
            ProjectId = "proj-789",
            Stage = RunStage.Script,
            SceneIndex = 0,
            ModelId = "gpt-4",
            Provider = "OpenAI",
            SelectionSource = SelectionSource.Default,
            TokensIn = 100,
            TokensOut = 200,
            CacheHit = false,
            Retries = 0,
            LatencyMs = 1500,
            CostEstimate = 0.01m,
            Currency = "USD",
            PricingVersion = "2024-01",
            ResultStatus = ResultStatus.Ok,
            Message = "Script generated successfully",
            StartedAt = DateTime.UtcNow.AddSeconds(-2),
            EndedAt = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        
        Assert.Contains("\"version\": \"1.0\"", json);
        Assert.Contains("\"job_id\": \"test-job-123\"", json);
        Assert.Contains("\"correlation_id\": \"corr-456\"", json);
        Assert.Contains("\"stage\": \"Script\"", json);
        Assert.Contains("\"model_id\": \"gpt-4\"", json);
        Assert.Contains("\"provider\": \"OpenAI\"", json);
        Assert.Contains("\"tokens_in\": 100", json);
        Assert.Contains("\"tokens_out\": 200", json);
        Assert.Contains("\"latency_ms\": 1500", json);
        Assert.Contains("\"result_status\": \"Ok\"", json);
        
        var deserialized = JsonSerializer.Deserialize<RunTelemetryRecord>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(record.JobId, deserialized.JobId);
        Assert.Equal(record.Stage, deserialized.Stage);
        Assert.Equal(record.TokensIn, deserialized.TokensIn);
    }
    
    [Fact]
    public void TelemetryBuilder_CreatesValidRecord_WithTimingData()
    {
        var builder = TelemetryBuilder.Start("job-123", "corr-456", RunStage.Brief)
            .WithModel("gpt-4", "OpenAI")
            .WithTokens(50, 150)
            .WithCost(0.005m)
            .WithStatus(ResultStatus.Ok, message: "Brief generated");
        
        System.Threading.Thread.Sleep(100);
        
        var record = builder.Build();
        
        Assert.Equal("job-123", record.JobId);
        Assert.Equal("corr-456", record.CorrelationId);
        Assert.Equal(RunStage.Brief, record.Stage);
        Assert.Equal("gpt-4", record.ModelId);
        Assert.Equal("OpenAI", record.Provider);
        Assert.Equal(50, record.TokensIn);
        Assert.Equal(150, record.TokensOut);
        Assert.Equal(0.005m, record.CostEstimate);
        Assert.Equal(ResultStatus.Ok, record.ResultStatus);
        Assert.True(record.LatencyMs >= 100);
        Assert.True((record.EndedAt - record.StartedAt).TotalMilliseconds >= 100);
    }
    
    [Fact]
    public void RunTelemetryCollector_RecordsAndPersistsTelemetry()
    {
        var jobId = "test-job-persist";
        var correlationId = "corr-persist";
        
        _collector.StartCollection(jobId, correlationId);
        
        _collector.Record(new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Script,
            LatencyMs = 1000,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow.AddSeconds(-1),
            EndedAt = DateTime.UtcNow
        });
        
        _collector.Record(new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Tts,
            Provider = "ElevenLabs",
            LatencyMs = 2000,
            CostEstimate = 0.02m,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow.AddSeconds(-2),
            EndedAt = DateTime.UtcNow
        });
        
        var filePath = _collector.EndCollection();
        
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
        
        var loaded = _collector.LoadTelemetry(jobId);
        Assert.NotNull(loaded);
        Assert.Equal(jobId, loaded.JobId);
        Assert.Equal(correlationId, loaded.CorrelationId);
        Assert.Equal(2, loaded.Records.Count);
        
        Assert.NotNull(loaded.Summary);
        Assert.Equal(2, loaded.Summary.TotalOperations);
        Assert.Equal(2, loaded.Summary.SuccessfulOperations);
        Assert.Equal(0, loaded.Summary.FailedOperations);
        Assert.Equal(0.02m, loaded.Summary.TotalCost);
        Assert.Equal(3000, loaded.Summary.TotalLatencyMs);
    }
    
    [Fact]
    public void RunTelemetryCollector_MasksSensitiveData()
    {
        var jobId = "test-job-mask";
        var correlationId = "corr-mask";
        
        _collector.StartCollection(jobId, correlationId);
        
        _collector.Record(new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Script,
            LatencyMs = 100,
            ResultStatus = ResultStatus.Error,
            Message = "API error: sk-1234567890abcdef1234567890abcdef failed",
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["api_key"] = "sk-secret123",
                ["model"] = "gpt-4"
            }
        });
        
        var filePath = _collector.EndCollection();
        var loaded = _collector.LoadTelemetry(jobId);
        
        Assert.NotNull(loaded);
        var record = loaded.Records.First();
        
        Assert.DoesNotContain("sk-1234567890abcdef1234567890abcdef", record.Message);
        Assert.Contains("[REDACTED]", record.Message);
        
        Assert.NotNull(record.Metadata);
        Assert.Equal("[REDACTED]", record.Metadata["api_key"].ToString());
        Assert.Equal("gpt-4", record.Metadata["model"].ToString());
    }
    
    [Fact]
    public void RunTelemetrySummary_CalculatesCorrectAggregates()
    {
        var jobId = "test-job-summary";
        var correlationId = "corr-summary";
        
        _collector.StartCollection(jobId, correlationId);
        
        _collector.Record(new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Script,
            Provider = "OpenAI",
            TokensIn = 100,
            TokensOut = 200,
            CacheHit = true,
            LatencyMs = 1000,
            CostEstimate = 0.01m,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow
        });
        
        _collector.Record(new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Tts,
            Provider = "ElevenLabs",
            CacheHit = false,
            Retries = 2,
            LatencyMs = 3000,
            CostEstimate = 0.03m,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow
        });
        
        _collector.Record(new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Visuals,
            Provider = "StableDiffusion",
            LatencyMs = 5000,
            CostEstimate = 0.05m,
            ResultStatus = ResultStatus.Error,
            ErrorCode = "TIMEOUT",
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow
        });
        
        var filePath = _collector.EndCollection();
        var loaded = _collector.LoadTelemetry(jobId);
        
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Summary);
        
        var summary = loaded.Summary;
        Assert.Equal(3, summary.TotalOperations);
        Assert.Equal(2, summary.SuccessfulOperations);
        Assert.Equal(1, summary.FailedOperations);
        Assert.Equal(0.09m, summary.TotalCost);
        Assert.Equal(9000, summary.TotalLatencyMs);
        Assert.Equal(100, summary.TotalTokensIn);
        Assert.Equal(200, summary.TotalTokensOut);
        Assert.Equal(1, summary.CacheHits);
        Assert.Equal(2, summary.TotalRetries);
        
        Assert.NotNull(summary.CostByStage);
        Assert.Equal(0.01m, summary.CostByStage["script"]);
        Assert.Equal(0.03m, summary.CostByStage["tts"]);
        Assert.Equal(0.05m, summary.CostByStage["visuals"]);
        
        Assert.NotNull(summary.OperationsByProvider);
        Assert.Equal(1, summary.OperationsByProvider["OpenAI"]);
        Assert.Equal(1, summary.OperationsByProvider["ElevenLabs"]);
        Assert.Equal(1, summary.OperationsByProvider["StableDiffusion"]);
    }
    
    [Fact]
    public void TelemetryExtensions_CreateLlmTelemetry_GeneratesValidRecord()
    {
        var record = TelemetryExtensions.CreateLlmTelemetry(
            jobId: "job-123",
            correlationId: "corr-456",
            stage: RunStage.Plan,
            modelId: "gpt-4-turbo",
            provider: "OpenAI",
            tokensIn: 500,
            tokensOut: 1000,
            latencyMs: 2500,
            cost: 0.05m,
            cacheHit: true,
            retries: 1,
            status: ResultStatus.Ok
        );
        
        Assert.Equal("job-123", record.JobId);
        Assert.Equal(RunStage.Plan, record.Stage);
        Assert.Equal("gpt-4-turbo", record.ModelId);
        Assert.Equal("OpenAI", record.Provider);
        Assert.Equal(500, record.TokensIn);
        Assert.Equal(1000, record.TokensOut);
        Assert.Equal(2500, record.LatencyMs);
        Assert.Equal(0.05m, record.CostEstimate);
        Assert.True(record.CacheHit);
        Assert.Equal(1, record.Retries);
        Assert.Equal(ResultStatus.Ok, record.ResultStatus);
    }
    
    [Fact]
    public void TelemetryExtensions_CreateTtsTelemetry_GeneratesValidRecord()
    {
        var record = TelemetryExtensions.CreateTtsTelemetry(
            jobId: "job-123",
            correlationId: "corr-456",
            sceneIndex: 2,
            provider: "ElevenLabs",
            characters: 150,
            durationSeconds: 8.5,
            latencyMs: 3000,
            cost: 0.045m,
            retries: 0,
            status: ResultStatus.Ok
        );
        
        Assert.Equal("job-123", record.JobId);
        Assert.Equal(RunStage.Tts, record.Stage);
        Assert.Equal(2, record.SceneIndex);
        Assert.Equal("ElevenLabs", record.Provider);
        Assert.Equal(3000, record.LatencyMs);
        Assert.Equal(0.045m, record.CostEstimate);
        Assert.Equal(ResultStatus.Ok, record.ResultStatus);
        
        Assert.NotNull(record.Metadata);
        Assert.Equal(150, Convert.ToInt32(record.Metadata["characters"]));
        Assert.Equal(8.5, Convert.ToDouble(record.Metadata["duration_seconds"]));
    }
    
    [Fact]
    public void RunStage_SerializesToLowerCase()
    {
        var stages = new[] 
        { 
            RunStage.Brief, RunStage.Plan, RunStage.Script, 
            RunStage.Ssml, RunStage.Tts, RunStage.Visuals, 
            RunStage.Render, RunStage.Post 
        };
        
        foreach (var stage in stages)
        {
            var json = JsonSerializer.Serialize(stage);
            var stageName = stage.ToString();
            Assert.Contains(stageName, json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
