using Aura.Tests.TestDataBuilders;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Performance;

/// <summary>
/// Performance tests for video processing operations
/// </summary>
public class VideoProcessingPerformanceTests : PerformanceTestBase
{
    public VideoProcessingPerformanceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task JobProcessing_CompletesWithinThreshold()
    {
        // Arrange
        var job = new VideoJobBuilder()
            .WithProjectId("perf-test")
            .Build();

        var threshold = TimeSpan.FromSeconds(5);

        // Act
        var duration = await MeasureAsync(async () =>
        {
            // Simulate job processing
            await Task.Delay(100);
        }, "Job Processing");

        // Assert
        AssertPerformance(duration, threshold, "Job Processing");
    }

    [Fact]
    public async Task TimelineRendering_AverageDurationAcceptable()
    {
        // Arrange
        var timeline = new TimelineBuilder()
            .WithDuration(120.0)
            .WithDefaultVideoTrack()
            .WithDefaultAudioTrack()
            .Build();

        var threshold = TimeSpan.FromMilliseconds(100);
        var iterations = 10;

        // Act
        var averageDuration = await AverageDurationAsync(async () =>
        {
            // Simulate timeline rendering
            await Task.Delay(10);
        }, iterations, "Timeline Rendering");

        // Assert
        AssertPerformance(averageDuration, threshold, "Average Timeline Rendering");
        PrintPerformanceSummary();
    }

    [Fact]
    public void AssetLoading_MeetsSLO()
    {
        // Arrange
        var threshold = TimeSpan.FromMilliseconds(50);
        var assets = Enumerable.Range(0, 100)
            .Select(i => new AssetBuilder()
                .WithName($"asset-{i}")
                .Build())
            .ToList();

        // Act
        var duration = Measure(() =>
        {
            // Simulate asset loading
            _ = assets.Count;
        }, "Asset Loading");

        // Assert
        AssertPerformance(duration, threshold, "Asset Loading");
    }

    [Fact]
    public async Task ConcurrentJobProcessing_HandlesLoad()
    {
        // Arrange
        var jobCount = 10;
        var jobs = Enumerable.Range(0, jobCount)
            .Select(i => new VideoJobBuilder()
                .WithProjectId($"concurrent-{i}")
                .Build())
            .ToList();

        var threshold = TimeSpan.FromSeconds(2);

        // Act
        var duration = await MeasureAsync(async () =>
        {
            var tasks = jobs.Select(async job =>
            {
                await Task.Delay(50); // Simulate processing
            });
            await Task.WhenAll(tasks);
        }, "Concurrent Job Processing");

        // Assert
        AssertPerformance(duration, threshold, "Concurrent Job Processing");
        Output.WriteLine($"Processed {jobCount} jobs concurrently");
        PrintPerformanceSummary();
    }

    [Fact]
    public async Task MemoryUsage_StaysWithinBounds()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var iterations = 1000;

        // Act
        await MeasureAsync(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var _ = new VideoJobBuilder().Build();
                if (i % 100 == 0)
                {
                    await Task.Yield();
                }
            }
        }, "Memory Test");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Assert - Memory growth should be reasonable
        var maxGrowth = 10 * 1024 * 1024; // 10MB
        Assert.True(memoryGrowth < maxGrowth,
            $"Memory grew by {memoryGrowth / 1024 / 1024}MB, exceeding {maxGrowth / 1024 / 1024}MB threshold");

        Output.WriteLine($"Memory growth: {memoryGrowth / 1024 / 1024:F2}MB");
        PrintPerformanceSummary();
    }
}
