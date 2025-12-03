using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class PipelineSummaryTelemetryTests : IDisposable
{
    private readonly string _testArtifactsPath;

    public PipelineSummaryTelemetryTests()
    {
        _testArtifactsPath = Path.Combine(Path.GetTempPath(), "aura-pipeline-telemetry-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testArtifactsPath);
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
    public void PipelineSummaryTelemetry_SerializesToJson_MatchesSchema()
    {
        var summary = new PipelineSummaryTelemetry
        {
            PipelineId = "pipeline-123",
            CorrelationId = "corr-456",
            ProjectId = "proj-789",
            Topic = "Test Video About AI",
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            Success = true,
            TotalInputTokens = 1000,
            TotalOutputTokens = 2000,
            TotalCost = 0.05m,
            SceneCount = 5,
            VideoDurationSeconds = 120.5,
            TtsCharacters = 3000,
            ImagesGenerated = 5
        };

        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Contains("\"version\": \"1.0\"", json);
        Assert.Contains("\"pipeline_id\": \"pipeline-123\"", json);
        Assert.Contains("\"correlation_id\": \"corr-456\"", json);
        Assert.Contains("\"topic\": \"Test Video About AI\"", json);
        Assert.Contains("\"success\": true", json);
        Assert.Contains("\"total_input_tokens\": 1000", json);
        Assert.Contains("\"total_output_tokens\": 2000", json);
        Assert.Contains("\"total_tokens\": 3000", json);
        Assert.Contains("\"scene_count\": 5", json);

        var deserialized = JsonSerializer.Deserialize<PipelineSummaryTelemetry>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(summary.PipelineId, deserialized.PipelineId);
        Assert.Equal(summary.TotalTokens, deserialized.TotalTokens);
    }

    [Fact]
    public void PipelineSummaryTelemetry_CalculatesTotalDuration_Correctly()
    {
        var startedAt = DateTime.UtcNow.AddMinutes(-5);
        var completedAt = DateTime.UtcNow;

        var summary = new PipelineSummaryTelemetry
        {
            PipelineId = "test",
            CorrelationId = "test",
            Topic = "Test",
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Success = true
        };

        // Duration should be approximately 5 minutes (300000 ms)
        Assert.True(summary.TotalDurationMs >= 299000 && summary.TotalDurationMs <= 301000);
    }

    [Fact]
    public void PipelineSummaryTelemetry_CalculatesTotalTokens_Correctly()
    {
        var summary = new PipelineSummaryTelemetry
        {
            PipelineId = "test",
            CorrelationId = "test",
            Topic = "Test",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Success = true,
            TotalInputTokens = 500,
            TotalOutputTokens = 1500
        };

        Assert.Equal(2000, summary.TotalTokens);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksTokenUsage_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test Video";
        collector.RecordTokenUsage("Script", "OpenAI", 100, 200, 0.01m);
        collector.RecordTokenUsage("Script", "OpenAI", 50, 100, 0.005m);
        collector.RecordTokenUsage("Tts", "ElevenLabs", 0, 0, 0.02m);

        var summary = collector.Complete(success: true);

        Assert.Equal(150, summary.TotalInputTokens);
        Assert.Equal(300, summary.TotalOutputTokens);
        Assert.Equal(450, summary.TotalTokens);
        Assert.Equal(0.035m, summary.TotalCost);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksCostByStage_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test";
        collector.RecordTokenUsage("Script", "OpenAI", 100, 200, 0.01m);
        collector.RecordTokenUsage("Plan", "OpenAI", 50, 100, 0.005m);
        collector.RecordTokenUsage("Script", "OpenAI", 100, 200, 0.015m);

        var summary = collector.Complete(success: true);

        Assert.Equal(2, summary.CostByStage.Count);
        Assert.Equal(0.025m, summary.CostByStage["Script"]);
        Assert.Equal(0.005m, summary.CostByStage["Plan"]);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksCostByProvider_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test";
        collector.RecordTokenUsage("Script", "OpenAI", 100, 200, 0.01m);
        collector.RecordTokenUsage("Tts", "ElevenLabs", 0, 0, 0.02m);
        collector.RecordTokenUsage("Script", "Anthropic", 50, 100, 0.015m);

        var summary = collector.Complete(success: true);

        Assert.Equal(3, summary.CostByProvider.Count);
        Assert.Equal(0.01m, summary.CostByProvider["OpenAI"]);
        Assert.Equal(0.02m, summary.CostByProvider["ElevenLabs"]);
        Assert.Equal(0.015m, summary.CostByProvider["Anthropic"]);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksStageTimings_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test";
        collector.RecordStageTiming("Script", TimeSpan.FromSeconds(5));
        collector.RecordStageTiming("Tts", TimeSpan.FromSeconds(10));
        collector.RecordStageTiming("Render", TimeSpan.FromSeconds(30));

        var summary = collector.Complete(success: true);

        Assert.Equal(3, summary.StageTimingsMs.Count);
        Assert.Equal(5000, summary.StageTimingsMs["Script"]);
        Assert.Equal(10000, summary.StageTimingsMs["Tts"]);
        Assert.Equal(30000, summary.StageTimingsMs["Render"]);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksCacheMetrics_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test";
        collector.RecordCacheHit(0.01m);
        collector.RecordCacheHit(0.02m);
        collector.RecordCacheMiss();
        collector.RecordCacheMiss();
        collector.RecordCacheMiss();

        var summary = collector.Complete(success: true);

        Assert.Equal(2, summary.CacheHits);
        Assert.Equal(3, summary.CacheMisses);
        Assert.Equal(0.03m, summary.CostSavedByCache);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksRetries_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test";
        collector.RecordRetry("OpenAI");
        collector.RecordRetry("OpenAI");
        collector.RecordRetry("ElevenLabs");

        var summary = collector.Complete(success: true);

        Assert.Equal(2, summary.RetryCountByProvider.Count);
        Assert.Equal(2, summary.RetryCountByProvider["OpenAI"]);
        Assert.Equal(1, summary.RetryCountByProvider["ElevenLabs"]);
    }

    [Fact]
    public void PipelineTelemetryCollector_TracksProviderOperations_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test";
        collector.RecordTokenUsage("Script", "OpenAI", 100, 200, 0.01m);
        collector.RecordTokenUsage("Script", "OpenAI", 50, 100, 0.005m);
        collector.RecordProviderOperation("StableDiffusion");
        collector.RecordProviderOperation("StableDiffusion");

        var summary = collector.Complete(success: true);

        Assert.Equal(2, summary.OperationsByProvider.Count);
        Assert.Equal(2, summary.OperationsByProvider["OpenAI"]);
        Assert.Equal(2, summary.OperationsByProvider["StableDiffusion"]);
    }

    [Fact]
    public void PipelineTelemetryCollector_CapturesOutputMetrics_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Test Video About AI";
        collector.ProjectId = "proj-123";
        collector.SceneCount = 5;
        collector.VideoDurationSeconds = 180.5;
        collector.TtsCharacters = 5000;
        collector.ImagesGenerated = 5;
        collector.StrategyType = "HighQuality";
        collector.VisualApproach = "AI-Generated";
        collector.MaxConcurrency = 4;
        collector.QualityScore = 85.5;

        var summary = collector.Complete(success: true);

        Assert.Equal("Test Video About AI", summary.Topic);
        Assert.Equal("proj-123", summary.ProjectId);
        Assert.Equal(5, summary.SceneCount);
        Assert.Equal(180.5, summary.VideoDurationSeconds);
        Assert.Equal(5000, summary.TtsCharacters);
        Assert.Equal(5, summary.ImagesGenerated);
        Assert.Equal("HighQuality", summary.StrategyType);
        Assert.Equal("AI-Generated", summary.VisualApproach);
        Assert.Equal(4, summary.MaxConcurrency);
        Assert.Equal(85.5, summary.QualityScore);
    }

    [Fact]
    public void PipelineTelemetryCollector_CapturesFailure_Correctly()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation",
            _testArtifactsPath);

        collector.Topic = "Failed Video";
        collector.RecordTokenUsage("Script", "OpenAI", 100, 50, 0.005m);

        var summary = collector.Complete(success: false, errorMessage: "Script generation failed: timeout");

        Assert.False(summary.Success);
        Assert.Equal("Script generation failed: timeout", summary.ErrorMessage);
        Assert.Equal(100, summary.TotalInputTokens);
        Assert.Equal(50, summary.TotalOutputTokens);
        Assert.Equal(0.005m, summary.TotalCost);
    }

    [Fact]
    public void PipelineTelemetryCollector_PersistsAndLoadsSummary_Correctly()
    {
        string pipelineId;

        // Create and complete a collector
        {
            var collector = new PipelineTelemetryCollector(
                NullLogger<PipelineTelemetryCollector>.Instance,
                "test-correlation",
                _testArtifactsPath);

            collector.Topic = "Persisted Video";
            collector.SceneCount = 3;
            collector.RecordTokenUsage("Script", "OpenAI", 100, 200, 0.01m);

            var summary = collector.Complete(success: true);
            pipelineId = summary.PipelineId;
        }

        // Load and verify
        var loaded = PipelineTelemetryCollector.LoadSummary(_testArtifactsPath, pipelineId);

        Assert.NotNull(loaded);
        Assert.Equal(pipelineId, loaded.PipelineId);
        Assert.Equal("Persisted Video", loaded.Topic);
        Assert.Equal(3, loaded.SceneCount);
        Assert.Equal(100, loaded.TotalInputTokens);
        Assert.Equal(200, loaded.TotalOutputTokens);
        Assert.Equal(0.01m, loaded.TotalCost);
        Assert.True(loaded.Success);
    }

    [Fact]
    public void PipelineTelemetryCollector_ListsRecentSummaries_Correctly()
    {
        // Create multiple summaries
        for (int i = 0; i < 5; i++)
        {
            var collector = new PipelineTelemetryCollector(
                NullLogger<PipelineTelemetryCollector>.Instance,
                $"test-correlation-{i}",
                _testArtifactsPath);

            collector.Topic = $"Video {i}";
            collector.SceneCount = i + 1;
            collector.Complete(success: true);
        }

        // List all
        var all = PipelineTelemetryCollector.ListRecentSummaries(_testArtifactsPath, limit: 10);
        Assert.Equal(5, all.Count);

        // List with limit
        var limited = PipelineTelemetryCollector.ListRecentSummaries(_testArtifactsPath, limit: 3);
        Assert.Equal(3, limited.Count);
    }

    [Fact]
    public void PipelineTelemetryCollector_LoadSummary_ReturnsNull_WhenNotFound()
    {
        var loaded = PipelineTelemetryCollector.LoadSummary(_testArtifactsPath, "non-existent-id");
        Assert.Null(loaded);
    }

    [Fact]
    public void PipelineTelemetryCollector_IsThreadSafe()
    {
        var collector = new PipelineTelemetryCollector(
            NullLogger<PipelineTelemetryCollector>.Instance,
            "test-correlation");

        collector.Topic = "Thread Safety Test";

        var tasks = new List<System.Threading.Tasks.Task>();

        // Simulate concurrent recording from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                collector.RecordTokenUsage($"Stage{index % 5}", $"Provider{index % 3}", 10, 20, 0.001m);
                collector.RecordCacheHit(0.0005m);
                collector.RecordCacheMiss();
                collector.RecordRetry($"Provider{index % 3}");
            }));
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        var summary = collector.Complete(success: true);

        // Verify totals are correct
        Assert.Equal(1000, summary.TotalInputTokens); // 100 * 10
        Assert.Equal(2000, summary.TotalOutputTokens); // 100 * 20
        Assert.Equal(0.1m, summary.TotalCost); // 100 * 0.001
        Assert.Equal(100, summary.CacheHits);
        Assert.Equal(100, summary.CacheMisses);
        Assert.Equal(0.05m, summary.CostSavedByCache); // 100 * 0.0005
    }
}
