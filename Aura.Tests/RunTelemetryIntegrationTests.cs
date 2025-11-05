using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for RunTelemetry v1 emission across pipeline stages
/// </summary>
public class RunTelemetryIntegrationTests : IDisposable
{
    private readonly string _testArtifactsPath;
    private readonly string _testJobsPath;
    
    public RunTelemetryIntegrationTests()
    {
        _testArtifactsPath = Path.Combine(Path.GetTempPath(), "aura-telemetry-integration-tests", Guid.NewGuid().ToString());
        _testJobsPath = Path.Combine(_testArtifactsPath, "jobs");
        Directory.CreateDirectory(_testJobsPath);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testArtifactsPath))
        {
            try
            {
                Directory.Delete(_testArtifactsPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        GC.SuppressFinalize(this);
    }
    
    [Fact]
    public void RunTelemetryCollector_IntegratesWithJobRunner_PersistsTelemetry()
    {
        // Arrange
        var telemetryCollector = new RunTelemetryCollector(
            NullLogger<RunTelemetryCollector>.Instance,
            _testJobsPath);
        
        var jobId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        
        // Act - Start collection
        telemetryCollector.StartCollection(jobId, correlationId);
        
        // Simulate script generation telemetry
        var scriptRecord = TelemetryBuilder.Start(jobId, correlationId, RunStage.Script)
            .WithModel("gpt-4", "OpenAI")
            .WithTokens(500, 1500)
            .WithCost(0.045m)
            .WithStatus(ResultStatus.Ok, message: "Script generated successfully")
            .Build();
        telemetryCollector.Record(scriptRecord);
        
        // Simulate TTS telemetry
        var ttsRecord = TelemetryBuilder.Start(jobId, correlationId, RunStage.Tts)
            .WithModel("en-US-AriaNeural", "Windows SAPI")
            .WithStatus(ResultStatus.Ok, message: "Narration synthesized successfully")
            .Build();
        telemetryCollector.Record(ttsRecord);
        
        // Simulate render telemetry
        var renderRecord = TelemetryBuilder.Start(jobId, correlationId, RunStage.Render)
            .WithModel("FFmpeg", "VideoComposer")
            .WithStatus(ResultStatus.Ok, message: "Video rendered successfully")
            .Build();
        telemetryCollector.Record(renderRecord);
        
        // Act - End collection
        var telemetryPath = telemetryCollector.EndCollection();
        
        // Assert
        Assert.NotNull(telemetryPath);
        Assert.True(File.Exists(telemetryPath));
        
        // Load and verify telemetry
        var loaded = telemetryCollector.LoadTelemetry(jobId);
        Assert.NotNull(loaded);
        Assert.Equal(jobId, loaded.JobId);
        Assert.Equal(correlationId, loaded.CorrelationId);
        Assert.Equal(3, loaded.Records.Count);
        
        // Verify script record
        var scriptLoaded = loaded.Records.First(r => r.Stage == RunStage.Script);
        Assert.Equal("gpt-4", scriptLoaded.ModelId);
        Assert.Equal("OpenAI", scriptLoaded.Provider);
        Assert.Equal(500, scriptLoaded.TokensIn);
        Assert.Equal(1500, scriptLoaded.TokensOut);
        Assert.Equal(0.045m, scriptLoaded.CostEstimate);
        Assert.Equal(ResultStatus.Ok, scriptLoaded.ResultStatus);
        
        // Verify TTS record
        var ttsLoaded = loaded.Records.First(r => r.Stage == RunStage.Tts);
        Assert.Equal("en-US-AriaNeural", ttsLoaded.ModelId);
        Assert.Equal("Windows SAPI", ttsLoaded.Provider);
        Assert.Equal(ResultStatus.Ok, ttsLoaded.ResultStatus);
        
        // Verify render record
        var renderLoaded = loaded.Records.First(r => r.Stage == RunStage.Render);
        Assert.Equal("FFmpeg", renderLoaded.ModelId);
        Assert.Equal("VideoComposer", renderLoaded.Provider);
        Assert.Equal(ResultStatus.Ok, renderLoaded.ResultStatus);
        
        // Verify summary
        Assert.NotNull(loaded.Summary);
        Assert.Equal(3, loaded.Summary.TotalOperations);
        Assert.Equal(3, loaded.Summary.SuccessfulOperations);
        Assert.Equal(0, loaded.Summary.FailedOperations);
        Assert.Equal(0.045m, loaded.Summary.TotalCost);
    }
    
    [Fact]
    public void RunTelemetryCollector_HandlesMultipleStagesWithRetries()
    {
        // Arrange
        var telemetryCollector = new RunTelemetryCollector(
            NullLogger<RunTelemetryCollector>.Instance,
            _testJobsPath);
        
        var jobId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        
        // Act
        telemetryCollector.StartCollection(jobId, correlationId);
        
        // Simulate script generation with retries
        var scriptRecord = TelemetryBuilder.Start(jobId, correlationId, RunStage.Script)
            .WithModel("gpt-4", "OpenAI")
            .WithTokens(500, 1500)
            .WithCost(0.045m)
            .WithRetries(2)
            .WithStatus(ResultStatus.Ok, message: "Script generated after 2 retries")
            .Build();
        telemetryCollector.Record(scriptRecord);
        
        // Simulate TTS failure
        var ttsRecord = TelemetryBuilder.Start(jobId, correlationId, RunStage.Tts)
            .WithModel("ElevenLabs", "ElevenLabs")
            .WithRetries(3)
            .WithStatus(ResultStatus.Error, errorCode: "TTS_API_ERROR", message: "TTS API failed after 3 retries")
            .Build();
        telemetryCollector.Record(ttsRecord);
        
        var telemetryPath = telemetryCollector.EndCollection();
        
        // Assert
        var loaded = telemetryCollector.LoadTelemetry(jobId);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Records.Count);
        
        var scriptLoaded = loaded.Records.First(r => r.Stage == RunStage.Script);
        Assert.Equal(2, scriptLoaded.Retries);
        Assert.Equal(ResultStatus.Ok, scriptLoaded.ResultStatus);
        
        var ttsLoaded = loaded.Records.First(r => r.Stage == RunStage.Tts);
        Assert.Equal(3, ttsLoaded.Retries);
        Assert.Equal(ResultStatus.Error, ttsLoaded.ResultStatus);
        Assert.Equal("TTS_API_ERROR", ttsLoaded.ErrorCode);
        
        // Verify summary reflects failures
        Assert.NotNull(loaded.Summary);
        Assert.Equal(2, loaded.Summary.TotalOperations);
        Assert.Equal(1, loaded.Summary.SuccessfulOperations);
        Assert.Equal(1, loaded.Summary.FailedOperations);
        Assert.Equal(5, loaded.Summary.TotalRetries);
    }
}
