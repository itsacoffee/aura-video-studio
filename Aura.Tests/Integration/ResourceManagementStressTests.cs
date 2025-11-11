using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Stress tests for resource management under heavy load
/// Tests memory, process, and file handle management during concurrent operations
/// </summary>
public class ResourceManagementStressTests
{
    [Fact]
    public async Task ProcessManager_ConcurrentProcesses_Should_CleanupCorrectly()
    {
        // Arrange
        var processManager = new ProcessManager(NullLogger<ProcessManager>.Instance);
        var processIds = new List<int>();
        
        try
        {
            // Act - Register multiple processes
            for (int i = 0; i < 10; i++)
            {
                var processId = 1000 + i; // Mock process IDs
                processManager.RegisterProcess(processId, $"job-{i}");
                processIds.Add(processId);
            }
            
            // Verify all tracked
            Assert.Equal(10, processManager.GetProcessCount());
            
            // Unregister half of them
            for (int i = 0; i < 5; i++)
            {
                processManager.UnregisterProcess(processIds[i]);
            }
            
            // Verify correct count
            Assert.Equal(5, processManager.GetProcessCount());
        }
        finally
        {
            // Cleanup
            processManager.Dispose();
            
            // Verify all cleaned up
            Assert.Equal(0, processManager.GetProcessCount());
        }
        
        await Task.CompletedTask;
    }
    
    [Fact]
    public async Task MemoryMonitor_MultipleJobs_Should_HandleConcurrentTracking()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobCount = 20;
        var jobs = new List<string>();
        
        // Act - Start monitoring for multiple jobs concurrently
        var startTasks = new List<Task>();
        for (int i = 0; i < jobCount; i++)
        {
            var jobId = $"job-{i}";
            jobs.Add(jobId);
            startTasks.Add(Task.Run(() => monitor.StartMonitoring(jobId)));
        }
        
        await Task.WhenAll(startTasks);
        
        // Simulate work with memory allocations
        var allocations = new List<byte[]>();
        for (int i = 0; i < 10; i++)
        {
            allocations.Add(new byte[1024 * 1024]); // 1MB each
            await Task.Delay(10);
        }
        
        // Stop monitoring for all jobs
        var stopTasks = new List<Task<MemoryStatistics>>();
        foreach (var jobId in jobs)
        {
            stopTasks.Add(Task.Run(() => monitor.StopMonitoring(jobId)));
        }
        
        var allStats = await Task.WhenAll(stopTasks);
        
        // Assert - All jobs should have valid statistics
        Assert.Equal(jobCount, allStats.Length);
        foreach (var stats in allStats)
        {
            Assert.True(stats.StartMemoryMb >= 0);
            Assert.True(stats.EndMemoryMb >= 0);
            Assert.True(stats.DurationSeconds >= 0);
        }
        
        // Keep allocations alive
        GC.KeepAlive(allocations);
    }
    
    [Fact]
    public async Task MemoryPressure_UnderHeavyLoad_Should_TriggerGarbageCollection()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(
            NullLogger<MemoryPressureMonitor>.Instance,
            memoryPressureThresholdMb: 50); // Low threshold
        
        var jobId = Guid.NewGuid().ToString();
        monitor.StartMonitoring(jobId);
        
        // Act - Allocate memory to simulate heavy load
        var allocations = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            allocations.Add(new byte[1024 * 1024]); // 1MB each
            
            // Update peak memory and check for pressure
            monitor.UpdatePeakMemory(jobId);
            
            // Force collection if needed
            if (i % 10 == 0)
            {
                monitor.ForceCollectionIfNeeded();
            }
            
            await Task.Delay(5);
        }
        
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert - Should have recorded GC activity
        var totalCollections = stats.Gen0Collections + stats.Gen1Collections + stats.Gen2Collections;
        Assert.True(totalCollections > 0, "Should have performed garbage collection under load");
        
        // Clear allocations
        allocations.Clear();
        GC.Collect();
    }
    
    [Fact]
    public async Task ResourceCleanup_SequentialJobs_Should_NotLeakMemory()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var iterations = 5;
        var initialMemory = monitor.GetCurrentMemoryUsage();
        
        // Act - Run multiple job cycles
        for (int i = 0; i < iterations; i++)
        {
            var jobId = $"job-{i}";
            monitor.StartMonitoring(jobId);
            
            // Simulate job work
            var temp = new byte[10 * 1024 * 1024]; // 10MB
            await Task.Delay(50);
            monitor.UpdatePeakMemory(jobId);
            
            var stats = monitor.StopMonitoring(jobId);
            Assert.True(stats.DurationSeconds > 0);
            
            // Force cleanup
            temp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        
        // Assert - Memory should return to reasonable levels
        var finalMemory = monitor.GetCurrentMemoryUsage();
        var memoryGrowthMb = (finalMemory - initialMemory) / (1024.0 * 1024.0);
        
        // Allow some growth but not excessive (less than 50MB growth for 5 iterations)
        Assert.True(memoryGrowthMb < 50, 
            $"Memory grew by {memoryGrowthMb:F1}MB, possible leak detected");
    }
    
    [Fact]
    public async Task ConcurrentJobExecution_Should_NotCauseResourceContention()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var concurrentJobs = 10;
        var tasks = new List<Task>();
        
        // Act - Execute multiple jobs concurrently
        for (int i = 0; i < concurrentJobs; i++)
        {
            var jobId = $"concurrent-job-{i}";
            tasks.Add(Task.Run(async () =>
            {
                monitor.StartMonitoring(jobId);
                
                // Simulate job work
                var allocation = new byte[5 * 1024 * 1024]; // 5MB
                await Task.Delay(100);
                monitor.UpdatePeakMemory(jobId);
                
                var stats = monitor.StopMonitoring(jobId);
                Assert.True(stats.DurationSeconds > 0);
                Assert.True(stats.PeakMemoryMb > 0);
                
                GC.KeepAlive(allocation);
            }));
        }
        
        // Wait for all to complete
        await Task.WhenAll(tasks);
        
        // Assert - All jobs should complete without deadlock or errors
        Assert.Equal(concurrentJobs, tasks.Count);
        Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
    }
    
    [Fact]
    public void ProcessManager_Disposal_Should_CleanupAllProcesses()
    {
        // Arrange
        var processManager = new ProcessManager(NullLogger<ProcessManager>.Instance);
        
        // Register several processes
        for (int i = 0; i < 20; i++)
        {
            processManager.RegisterProcess(2000 + i, $"job-{i}");
        }
        
        Assert.Equal(20, processManager.GetProcessCount());
        
        // Act - Dispose should cleanup all
        processManager.Dispose();
        
        // Assert
        Assert.Equal(0, processManager.GetProcessCount());
    }
    
    [Fact]
    public async Task MemoryPressure_RecoveryAfterPeak_Should_ReleaseMemory()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        monitor.StartMonitoring(jobId);
        var initialMemory = monitor.GetCurrentMemoryUsage();
        
        // Act - Create memory pressure
        var largeAllocation = new byte[100 * 1024 * 1024]; // 100MB
        monitor.UpdatePeakMemory(jobId);
        var peakMemory = monitor.GetCurrentMemoryUsage();
        
        // Release and force collection
        largeAllocation = null;
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        
        await Task.Delay(100); // Allow GC to complete
        
        var recoveredMemory = monitor.GetCurrentMemoryUsage();
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert - Memory should be significantly lower than peak
        var peakMb = peakMemory / (1024.0 * 1024.0);
        var recoveredMb = recoveredMemory / (1024.0 * 1024.0);
        var freedMb = peakMb - recoveredMb;
        
        Assert.True(freedMb > 50, 
            $"Should have freed significant memory (freed {freedMb:F1}MB)");
    }
}
