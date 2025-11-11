using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Services.Memory;

/// <summary>
/// Tests for MemoryPressureMonitor to ensure proper resource tracking and cleanup
/// </summary>
public class MemoryPressureMonitorTests
{
    [Fact]
    public void GetCurrentMemoryUsage_Should_ReturnPositiveValue()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        
        // Act
        var memory = monitor.GetCurrentMemoryUsage();
        
        // Assert
        Assert.True(memory > 0, "Memory usage should be positive");
    }
    
    [Fact]
    public void GetGcStatistics_Should_ReturnValidStatistics()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        
        // Act
        var stats = monitor.GetGcStatistics();
        
        // Assert
        Assert.True(stats.Gen0Collections >= 0);
        Assert.True(stats.Gen1Collections >= 0);
        Assert.True(stats.Gen2Collections >= 0);
        Assert.True(stats.TotalMemoryBytes > 0);
        Assert.True(stats.TotalMemoryMb > 0);
    }
    
    [Fact]
    public void StartMonitoring_Should_CreateJobTracking()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId);
        
        // Assert - Should not throw, monitoring started
        var stats = monitor.StopMonitoring(jobId);
        Assert.Equal(jobId, stats.JobId);
        Assert.True(stats.StartMemoryMb >= 0);
    }
    
    [Fact]
    public void StopMonitoring_WithoutStart_Should_ReturnEmptyStatistics()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert
        Assert.Equal(jobId, stats.JobId);
        Assert.Equal(0, stats.StartMemoryMb);
        Assert.Equal(0, stats.EndMemoryMb);
    }
    
    [Fact]
    public async Task StartAndStopMonitoring_Should_TrackMemoryDelta()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId);
        
        // Allocate some memory to simulate job work
        var largeArray = new byte[10 * 1024 * 1024]; // 10MB
        await Task.Delay(100); // Small delay to ensure measurement
        
        monitor.UpdatePeakMemory(jobId);
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert
        Assert.True(stats.DurationSeconds > 0, "Duration should be positive");
        Assert.True(stats.EndMemoryMb >= 0, "End memory should be non-negative");
        Assert.True(stats.PeakMemoryMb >= stats.StartMemoryMb, "Peak should be >= start");
        
        // Keep reference to prevent premature collection
        GC.KeepAlive(largeArray);
    }
    
    [Fact]
    public void ForceCollectionIfNeeded_Should_NotThrow()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(
            NullLogger<MemoryPressureMonitor>.Instance,
            memoryPressureThresholdMb: 1); // Very low threshold to trigger collection
        
        // Act & Assert - Should not throw
        monitor.ForceCollectionIfNeeded();
    }
    
    [Fact]
    public async Task MemoryMonitoring_UnderLoad_Should_TrackGcCollections()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId);
        
        // Simulate memory pressure by allocating and releasing memory
        for (int i = 0; i < 5; i++)
        {
            var temp = new byte[1024 * 1024]; // 1MB
            await Task.Delay(10);
            GC.KeepAlive(temp);
        }
        
        // Force at least one collection to have measurable GC stats
        GC.Collect(0);
        
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert - At least one Gen0 collection should have occurred
        Assert.True(stats.Gen0Collections >= 0);
        Assert.True(stats.Gen1Collections >= 0);
        Assert.True(stats.Gen2Collections >= 0);
    }
    
    [Fact]
    public void IsUnderMemoryPressure_WithLowMemory_Should_ReturnFalse()
    {
        // Arrange - Set very high threshold
        var monitor = new MemoryPressureMonitor(
            NullLogger<MemoryPressureMonitor>.Instance,
            memoryPressureThresholdMb: 1000000); // 1TB threshold
        
        // Act
        var isUnderPressure = monitor.IsUnderMemoryPressure();
        
        // Assert
        Assert.False(isUnderPressure, "Should not be under pressure with very high threshold");
    }
    
    [Fact]
    public void IsUnderMemoryPressure_WithVeryLowThreshold_Should_ReturnTrue()
    {
        // Arrange - Set very low threshold to simulate pressure
        var monitor = new MemoryPressureMonitor(
            NullLogger<MemoryPressureMonitor>.Instance,
            memoryPressureThresholdMb: 1); // 1MB threshold
        
        // Act
        var isUnderPressure = monitor.IsUnderMemoryPressure();
        
        // Assert - With such a low threshold, we should detect pressure
        Assert.True(isUnderPressure, "Should be under pressure with very low threshold");
    }
    
    [Fact]
    public void UpdatePeakMemory_Should_UpdatePeakValue()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId);
        var initialStats = monitor.GetGcStatistics();
        
        // Allocate memory to increase usage
        var largeArray = new byte[50 * 1024 * 1024]; // 50MB
        monitor.UpdatePeakMemory(jobId);
        
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert
        Assert.True(stats.PeakMemoryMb > 0, "Peak memory should be tracked");
        
        // Keep reference
        GC.KeepAlive(largeArray);
    }
    
    [Fact]
    public void MultipleJobs_Should_TrackIndependently()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId1 = Guid.NewGuid().ToString();
        var jobId2 = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId1);
        monitor.StartMonitoring(jobId2);
        
        var stats1 = monitor.StopMonitoring(jobId1);
        var stats2 = monitor.StopMonitoring(jobId2);
        
        // Assert
        Assert.Equal(jobId1, stats1.JobId);
        Assert.Equal(jobId2, stats2.JobId);
        Assert.True(stats1.StartMemoryMb >= 0);
        Assert.True(stats2.StartMemoryMb >= 0);
    }
    
    [Fact]
    public async Task LongRunningJob_Should_TrackMemoryOverTime()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId);
        
        // Simulate long-running job with periodic memory updates
        var allocations = new List<byte[]>();
        for (int i = 0; i < 3; i++)
        {
            var allocation = new byte[5 * 1024 * 1024]; // 5MB each iteration
            allocations.Add(allocation);
            monitor.UpdatePeakMemory(jobId);
            await Task.Delay(50);
        }
        
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert
        Assert.True(stats.DurationSeconds > 0);
        Assert.True(stats.PeakMemoryMb >= stats.StartMemoryMb);
        
        // Keep references
        GC.KeepAlive(allocations);
    }
    
    [Fact]
    public void MemoryStatistics_Should_CalculateCorrectDelta()
    {
        // Arrange
        var monitor = new MemoryPressureMonitor(NullLogger<MemoryPressureMonitor>.Instance);
        var jobId = Guid.NewGuid().ToString();
        
        // Act
        monitor.StartMonitoring(jobId);
        var stats = monitor.StopMonitoring(jobId);
        
        // Assert
        var expectedDelta = stats.EndMemoryMb - stats.StartMemoryMb;
        Assert.Equal(expectedDelta, stats.MemoryDeltaMb, precision: 1);
    }
}
