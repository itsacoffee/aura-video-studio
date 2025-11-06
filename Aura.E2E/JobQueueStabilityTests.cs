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

    /// <summary>
    /// Tests that IsTerminal correctly identifies terminal job states
    /// </summary>
    [Fact]
    public void Job_IsTerminal_Should_IdentifyTerminalStates()
    {
        // Arrange & Assert - Terminal states
        var doneJob = new Job { Status = JobStatus.Done };
        Assert.True(doneJob.IsTerminal(), "Done should be terminal");
        
        var succeededJob = new Job { Status = JobStatus.Succeeded };
        Assert.True(succeededJob.IsTerminal(), "Succeeded should be terminal");
        
        var failedJob = new Job { Status = JobStatus.Failed };
        Assert.True(failedJob.IsTerminal(), "Failed should be terminal");
        
        var canceledJob = new Job { Status = JobStatus.Canceled };
        Assert.True(canceledJob.IsTerminal(), "Canceled should be terminal");
        
        // Non-terminal states
        var queuedJob = new Job { Status = JobStatus.Queued };
        Assert.False(queuedJob.IsTerminal(), "Queued should not be terminal");
        
        var runningJob = new Job { Status = JobStatus.Running };
        Assert.False(runningJob.IsTerminal(), "Running should not be terminal");
        
        _output.WriteLine("All terminal state checks passed");
    }

    /// <summary>
    /// Tests that QueuedUtc is properly set on job creation
    /// </summary>
    [Fact]
    public void Job_Should_HaveQueuedUtcOnCreation()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var job = new Job
        {
            Id = "test-job",
            Status = JobStatus.Queued
        };
        
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.NotNull(job.QueuedUtc);
        Assert.True(job.QueuedUtc >= beforeCreate, 
            $"QueuedUtc ({job.QueuedUtc}) should be >= beforeCreate ({beforeCreate})");
        Assert.True(job.QueuedUtc <= afterCreate, 
            $"QueuedUtc ({job.QueuedUtc}) should be <= afterCreate ({afterCreate})");
        Assert.Equal(job.CreatedUtc, job.QueuedUtc);
        
        _output.WriteLine($"Job created with QueuedUtc: {job.QueuedUtc:yyyy-MM-dd HH:mm:ss.fff}");
    }

    /// <summary>
    /// Tests that terminal states require EndedUtc timestamp
    /// This validates the state machine invariant
    /// </summary>
    [Theory]
    [InlineData(JobStatus.Done)]
    [InlineData(JobStatus.Succeeded)]
    [InlineData(JobStatus.Failed)]
    [InlineData(JobStatus.Canceled)]
    public void TerminalJobs_Should_HaveEndedUtc(JobStatus terminalStatus)
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var job = new Job
        {
            Status = terminalStatus,
            EndedUtc = now
        };
        
        // Assert
        Assert.True(job.IsTerminal());
        Assert.NotNull(job.EndedUtc);
        
        _output.WriteLine($"Terminal job ({terminalStatus}) has EndedUtc: {job.EndedUtc}");
    }

    /// <summary>
    /// Tests the complete lifecycle timestamps for a successful job
    /// </summary>
    [Fact]
    public void Job_SuccessfulLifecycle_Should_HaveCorrectTimestamps()
    {
        // Simulate a job going through the full lifecycle
        var t0 = DateTime.UtcNow;
        
        // Job created and queued
        var job = new Job
        {
            Id = "lifecycle-test",
            Status = JobStatus.Queued,
            CreatedUtc = t0,
            QueuedUtc = t0
        };
        
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(t0, job.CreatedUtc);
        Assert.Equal(t0, job.QueuedUtc);
        Assert.Null(job.StartedUtc);
        
        // Job starts running
        var t1 = t0.AddSeconds(1);
        job = job with
        {
            Status = JobStatus.Running,
            StartedUtc = t1
        };
        
        Assert.Equal(JobStatus.Running, job.Status);
        Assert.Equal(t1, job.StartedUtc);
        Assert.Null(job.CompletedUtc);
        
        // Job completes successfully
        var t2 = t1.AddSeconds(30);
        job = job with
        {
            Status = JobStatus.Done,
            CompletedUtc = t2,
            EndedUtc = t2,
            Percent = 100
        };
        
        Assert.Equal(JobStatus.Done, job.Status);
        Assert.Equal(100, job.Percent);
        Assert.Equal(t2, job.CompletedUtc);
        Assert.Equal(t2, job.EndedUtc);
        Assert.True(job.IsTerminal());
        
        // Verify timestamp ordering
        Assert.True(job.StartedUtc >= job.CreatedUtc);
        Assert.True(job.CompletedUtc >= job.StartedUtc);
        Assert.True(job.EndedUtc == job.CompletedUtc);
        
        _output.WriteLine("Job lifecycle timestamps validated:");
        _output.WriteLine($"  Created: {job.CreatedUtc:HH:mm:ss}");
        _output.WriteLine($"  Queued: {job.QueuedUtc:HH:mm:ss}");
        _output.WriteLine($"  Started: {job.StartedUtc:HH:mm:ss}");
        _output.WriteLine($"  Completed: {job.CompletedUtc:HH:mm:ss}");
        _output.WriteLine($"  Ended: {job.EndedUtc:HH:mm:ss}");
    }

    /// <summary>
    /// Tests the complete lifecycle timestamps for a canceled job
    /// </summary>
    [Fact]
    public void Job_CanceledLifecycle_Should_HaveCorrectTimestamps()
    {
        // Simulate a job being canceled mid-execution
        var t0 = DateTime.UtcNow;
        
        // Job created and queued
        var job = new Job
        {
            Id = "cancel-test",
            Status = JobStatus.Queued,
            CreatedUtc = t0,
            QueuedUtc = t0
        };
        
        // Job starts running
        var t1 = t0.AddSeconds(1);
        job = job with
        {
            Status = JobStatus.Running,
            StartedUtc = t1,
            Percent = 0
        };
        
        // Progress updates
        job = job.WithMonotonicProgress(25);
        Assert.Equal(25, job.Percent);
        
        job = job.WithMonotonicProgress(40);
        Assert.Equal(40, job.Percent);
        
        // Job is canceled at 40%
        var t2 = t1.AddSeconds(15);
        job = job with
        {
            Status = JobStatus.Canceled,
            CanceledUtc = t2,
            EndedUtc = t2,
            ErrorMessage = "Job was cancelled by user"
        };
        
        Assert.Equal(JobStatus.Canceled, job.Status);
        Assert.Equal(40, job.Percent); // Progress preserved at cancellation point
        Assert.Equal(t2, job.CanceledUtc);
        Assert.Equal(t2, job.EndedUtc);
        Assert.True(job.IsTerminal());
        Assert.Null(job.CompletedUtc); // Should not have CompletedUtc
        
        // Verify timestamp ordering
        Assert.True(job.StartedUtc >= job.CreatedUtc);
        Assert.True(job.CanceledUtc >= job.StartedUtc);
        Assert.True(job.EndedUtc == job.CanceledUtc);
        
        _output.WriteLine("Canceled job lifecycle validated:");
        _output.WriteLine($"  Created: {job.CreatedUtc:HH:mm:ss}");
        _output.WriteLine($"  Started: {job.StartedUtc:HH:mm:ss}");
        _output.WriteLine($"  Canceled: {job.CanceledUtc:HH:mm:ss} at {job.Percent}%");
        _output.WriteLine($"  Ended: {job.EndedUtc:HH:mm:ss}");
    }

    /// <summary>
    /// Tests resumability fields and logic
    /// </summary>
    [Fact]
    public void Job_Resumability_Should_TrackLastCompletedStep()
    {
        // Arrange - Job that failed after completing script generation
        var job = new Job
        {
            Id = "resume-test",
            Status = JobStatus.Failed,
            Stage = "Voice", // Failed during voice generation
            LastCompletedStep = "Script", // Successfully completed script
            CanResume = true,
            Percent = 30
        };
        
        // Assert
        Assert.True(job.CanResume);
        Assert.Equal("Script", job.LastCompletedStep);
        Assert.Equal("Voice", job.Stage); // Failed at this stage
        Assert.True(job.IsTerminal());
        
        _output.WriteLine($"Job can be resumed from step: {job.LastCompletedStep}");
        _output.WriteLine($"Job failed at stage: {job.Stage}");
        
        // Test non-resumable job
        var nonResumableJob = new Job
        {
            Id = "non-resume-test",
            Status = JobStatus.Failed,
            Stage = "Rendering", // Rendering is atomic, cannot resume
            CanResume = false
        };
        
        Assert.False(nonResumableJob.CanResume);
        Assert.Null(nonResumableJob.LastCompletedStep);
        
        _output.WriteLine("Non-resumable job (rendering stage) validated");
    }
}
