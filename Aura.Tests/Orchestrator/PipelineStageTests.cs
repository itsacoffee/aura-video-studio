using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Orchestrator;

public class PipelineStageTests
{
    private readonly Mock<ILogger<TestPipelineStage>> _mockLogger;

    public PipelineStageTests()
    {
        _mockLogger = new Mock<ILogger<TestPipelineStage>>();
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulStage_ReturnsSuccess()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("TestStage", result.StageName);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_FailedStage_ReturnsFailure()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object, shouldFail: true);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("TestStage", result.StageName);
        Assert.NotNull(result.Exception);
        Assert.Equal("Test failure", result.Exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_StageWithRetry_RetriesOnFailure()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object, failCount: 2);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, stage.ExecutionCount); // Should succeed on second attempt
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsRetries_ReturnsFailure()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object, failCount: 10);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(4, stage.ExecutionCount); // Initial + 3 retries
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_ThrowsOperationCancelledException()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object, delayMs: 5000);
        var context = CreateTestContext();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ExecuteAsync(context, null, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgress_CallsProgressCallback()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object);
        var context = CreateTestContext();
        var progressReports = new System.Collections.Generic.List<StageProgress>();
        var progress = new Progress<StageProgress>(p => progressReports.Add(p));

        // Act
        await stage.ExecuteAsync(context, progress);

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Percentage == 0);
        Assert.Contains(progressReports, p => p.Percentage == 100);
    }

    [Fact]
    public async Task ExecuteAsync_StoresStageOutput_AvailableInContext()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object);
        var context = CreateTestContext();

        // Act
        await stage.ExecuteAsync(context);

        // Assert
        var output = context.GetStageOutput<string>("TestStage");
        Assert.NotNull(output);
        Assert.Equal("Test output", output);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsMetrics_AvailableInContext()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object);
        var context = CreateTestContext();

        // Act
        await stage.ExecuteAsync(context);

        // Assert
        var metrics = context.GetStageMetrics("TestStage");
        Assert.NotNull(metrics);
        Assert.Equal("TestStage", metrics.StageName);
        Assert.True(metrics.Duration > TimeSpan.Zero);
        Assert.Equal(1, metrics.ItemsProcessed);
    }

    [Fact]
    public async Task ExecuteAsync_ResumableStage_SkipsIfAlreadyCompleted()
    {
        // Arrange
        var stage = new TestPipelineStage(_mockLogger.Object);
        var context = CreateTestContext();
        
        // Pre-populate output to simulate completed stage
        context.SetStageOutput("TestStage", "Existing output");

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.Resumed);
        Assert.Equal(0, stage.ExecutionCount); // Should not execute
    }

    private PipelineContext CreateTestContext()
    {
        return new PipelineContext(
            correlationId: Guid.NewGuid().ToString(),
            brief: new Brief("Test topic", "Test audience", "Test goal", "Professional", "English", Aspect.Widescreen16x9, null),
            planSpec: new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "informative"),
            voiceSpec: new VoiceSpec("test-voice", 1.0, 0.0, PauseStyle.Natural),
            renderSpec: new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192, 30, "H264", 75, true),
            systemProfile: new SystemProfile { Tier = HardwareTier.B, LogicalCores = 8, PhysicalCores = 4, RamGB = 16 }
        );
    }

    // Test implementation of PipelineStage
    private class TestPipelineStage : PipelineStage
    {
        private readonly bool _shouldFail;
        private readonly int _failCount;
        private readonly int _delayMs;
        public int ExecutionCount { get; private set; }

        public TestPipelineStage(
            ILogger<TestPipelineStage> logger,
            bool shouldFail = false,
            int failCount = 0,
            int delayMs = 0) : base(logger)
        {
            _shouldFail = shouldFail;
            _failCount = failCount;
            _delayMs = delayMs;
        }

        public override string StageName => "TestStage";
        public override string DisplayName => "Test Stage";
        public override int ProgressWeight => 20;

        protected override async Task ExecuteStageAsync(
            PipelineContext context,
            IProgress<StageProgress>? progress,
            CancellationToken ct)
        {
            ExecutionCount++;

            ReportProgress(progress, 0, "Starting test stage");

            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, ct);
            }

            if (_shouldFail || ExecutionCount <= _failCount)
            {
                throw new InvalidOperationException("Test failure");
            }

            ReportProgress(progress, 50, "Processing...");
            await Task.Delay(10, ct);

            ReportProgress(progress, 100, "Test stage completed");

            context.SetStageOutput(StageName, "Test output");
        }
    }
}
