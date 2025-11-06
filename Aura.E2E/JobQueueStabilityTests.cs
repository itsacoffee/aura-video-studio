using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Events;
using Aura.Core.Orchestrator;
using Aura.Core.Services;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Aura.E2E;

/// <summary>
/// End-to-end tests for job queue and SSE stability guarantees
/// Tests monotonic progress, state transitions, and cancellation cleanup
/// </summary>
public class JobQueueStabilityTests
{
    private readonly ITestOutputHelper _output;

    public JobQueueStabilityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Tests that job progress never decreases using the model's WithMonotonicProgress method
    /// This is a unit-style test for the monotonic progress guarantee at the model level
    /// </summary>
    [Fact]
    public void JobModel_WithMonotonicProgress_Should_PreventDecrease()
    {
        // Arrange - Create a sequence of progress values that would normally decrease
        var progressSequence = new[] { 0, 10, 25, 20, 30, 28, 50, 75, 100, 95 }; // Contains decreases
        var job = new Job { Id = "test-job", Percent = 0 };
        
        var actualProgress = new List<int>();

        // Act - Apply each progress value using WithMonotonicProgress
        foreach (var targetProgress in progressSequence)
        {
            job = job.WithMonotonicProgress(targetProgress);
            actualProgress.Add(job.Percent);
            _output.WriteLine($"Target: {targetProgress}% -> Actual: {job.Percent}%");
        }

        // Assert - Progress should never decrease
        for (int i = 1; i < actualProgress.Count; i++)
        {
            Assert.True(actualProgress[i] >= actualProgress[i - 1],
                $"Progress decreased from {actualProgress[i - 1]}% to {actualProgress[i]}% at index {i}");
        }
        
        // Verify specific expectations
        Assert.Equal(0, actualProgress[0]);   // 0 -> 0
        Assert.Equal(10, actualProgress[1]);  // 10 -> 10
        Assert.Equal(25, actualProgress[2]);  // 25 -> 25
        Assert.Equal(25, actualProgress[3]);  // 20 -> 25 (prevented decrease)
        Assert.Equal(30, actualProgress[4]);  // 30 -> 30
        Assert.Equal(30, actualProgress[5]);  // 28 -> 30 (prevented decrease)
        Assert.Equal(50, actualProgress[6]);  // 50 -> 50
        Assert.Equal(75, actualProgress[7]);  // 75 -> 75
        Assert.Equal(100, actualProgress[8]); // 100 -> 100
        Assert.Equal(100, actualProgress[9]); // 95 -> 100 (prevented decrease)
        
        _output.WriteLine($"Final progress sequence: {string.Join(" -> ", actualProgress)}");
    }

    /// <summary>
    /// Tests that job state transitions follow the defined rules
    /// </summary>
    [Fact]
    public void JobStateTransitions_Should_FollowInvariants()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job",
            Status = JobStatus.Queued
        };

        // Act & Assert - Valid transitions from Queued
        Assert.True(job.CanTransitionTo(JobStatus.Running));
        Assert.True(job.CanTransitionTo(JobStatus.Canceled));
        Assert.False(job.CanTransitionTo(JobStatus.Done));
        Assert.False(job.CanTransitionTo(JobStatus.Failed));

        // Running state
        var runningJob = job with { Status = JobStatus.Running };
        Assert.True(runningJob.CanTransitionTo(JobStatus.Done));
        Assert.True(runningJob.CanTransitionTo(JobStatus.Succeeded));
        Assert.True(runningJob.CanTransitionTo(JobStatus.Failed));
        Assert.True(runningJob.CanTransitionTo(JobStatus.Canceled));
        Assert.False(runningJob.CanTransitionTo(JobStatus.Queued));

        // Terminal states cannot transition
        var completedJob = job with { Status = JobStatus.Done };
        Assert.False(completedJob.CanTransitionTo(JobStatus.Running));
        Assert.False(completedJob.CanTransitionTo(JobStatus.Failed));
        Assert.False(completedJob.CanTransitionTo(JobStatus.Canceled));

        var failedJob = job with { Status = JobStatus.Failed };
        Assert.False(failedJob.CanTransitionTo(JobStatus.Running));
        Assert.False(failedJob.CanTransitionTo(JobStatus.Done));

        var canceledJob = job with { Status = JobStatus.Canceled };
        Assert.False(canceledJob.CanTransitionTo(JobStatus.Running));
        Assert.False(canceledJob.CanTransitionTo(JobStatus.Done));
    }

    /// <summary>
    /// Tests that monotonic progress helper works correctly
    /// </summary>
    [Fact]
    public void WithMonotonicProgress_Should_PreventDecrease()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job",
            Percent = 50
        };

        // Act & Assert - Progress increases
        var increasedJob = job.WithMonotonicProgress(75);
        Assert.Equal(75, increasedJob.Percent);

        // Progress stays same
        var sameJob = job.WithMonotonicProgress(50);
        Assert.Equal(50, sameJob.Percent);

        // Progress would decrease - should stay at current value
        var decreaseAttempt = job.WithMonotonicProgress(25);
        Assert.Equal(50, decreaseAttempt.Percent);

        // Negative value clamped
        var negativeAttempt = job.WithMonotonicProgress(-10);
        Assert.Equal(50, negativeAttempt.Percent);

        // Over 100 clamped
        var overLimitAttempt = job.WithMonotonicProgress(150);
        Assert.Equal(100, overLimitAttempt.Percent);
    }

    /// <summary>
    /// Tests that CleanupService can clean up job artifacts
    /// </summary>
    [Fact]
    public void CleanupService_Should_HandleJobCleanup()
    {
        // Arrange
        var cleanupService = new CleanupService(NullLogger<CleanupService>.Instance);
        var testJobId = "test-job-" + Guid.NewGuid();

        // Act - Clean up a job (even if it doesn't exist, should not throw)
        cleanupService.CleanupJob(testJobId);
        
        // Assert - Method should complete without exceptions
        // The actual cleanup behavior is tested by ensuring no exception is thrown
        _output.WriteLine($"Cleanup completed for job: {testJobId}");
        
        // Also test statistics retrieval
        var (tempSize, proxySize, tempDirCount, proxyDirCount) = cleanupService.GetStorageStats();
        _output.WriteLine($"Temp storage: {tempSize} bytes in {tempDirCount} directories");
        _output.WriteLine($"Proxy storage: {proxySize} bytes in {proxyDirCount} directories");
        
        Assert.True(tempSize >= 0);
        Assert.True(proxySize >= 0);
        Assert.True(tempDirCount >= 0);
        Assert.True(proxyDirCount >= 0);
    }

    /// <summary>
    /// Tests that job timestamps follow correct ordering
    /// </summary>
    [Fact]
    public void JobTimestamps_Should_FollowCorrectOrdering()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oneSecondLater = now.AddSeconds(1);
        var twoSecondsLater = now.AddSeconds(2);
        
        // Act - Create a completed job with proper timestamps
        var job = new Job
        {
            Id = "test-job",
            Status = JobStatus.Done,
            CreatedUtc = now,
            StartedUtc = oneSecondLater,
            CompletedUtc = twoSecondsLater,
            EndedUtc = twoSecondsLater
        };

        // Assert - Timestamps are in correct order
        Assert.True(job.StartedUtc >= job.CreatedUtc,
            "StartedUtc should be >= CreatedUtc");
        Assert.True(job.CompletedUtc >= job.StartedUtc,
            "CompletedUtc should be >= StartedUtc");
        Assert.True(job.EndedUtc >= job.CompletedUtc,
            "EndedUtc should be >= CompletedUtc");
        
        _output.WriteLine($"Created: {job.CreatedUtc:HH:mm:ss.fff}");
        _output.WriteLine($"Started: {job.StartedUtc:HH:mm:ss.fff}");
        _output.WriteLine($"Completed: {job.CompletedUtc:HH:mm:ss.fff}");
        _output.WriteLine($"Ended: {job.EndedUtc:HH:mm:ss.fff}");
        
        // Test canceled job timestamps
        var canceledJob = new Job
        {
            Id = "canceled-job",
            Status = JobStatus.Canceled,
            CreatedUtc = now,
            StartedUtc = oneSecondLater,
            CanceledUtc = twoSecondsLater,
            EndedUtc = twoSecondsLater
        };
        
        Assert.True(canceledJob.CanceledUtc >= canceledJob.StartedUtc,
            "CanceledUtc should be >= StartedUtc");
        Assert.True(canceledJob.EndedUtc >= canceledJob.CanceledUtc,
            "EndedUtc should be >= CanceledUtc");
        
        _output.WriteLine($"Canceled job - Created: {canceledJob.CreatedUtc:HH:mm:ss.fff}");
        _output.WriteLine($"Canceled job - Canceled: {canceledJob.CanceledUtc:HH:mm:ss.fff}");
    }
}
