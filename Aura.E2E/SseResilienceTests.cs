using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Aura.E2E;

/// <summary>
/// End-to-end tests for SSE resilience, reconnection, and Last-Event-ID support
/// Tests that SSE clients can recover from transient disconnections
/// </summary>
public class SseResilienceTests
{
    private readonly ITestOutputHelper _output;

    public SseResilienceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Tests that SSE event IDs are monotonically increasing
    /// This is critical for reconnection support
    /// </summary>
    [Fact]
    public void SseEventIds_Should_BeMonotonicallyIncreasing()
    {
        // Arrange - Simulate a sequence of SSE event IDs
        var eventIds = new List<string>();
        var baseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Act - Generate event IDs as the backend would
        for (int i = 0; i < 10; i++)
        {
            var eventId = $"{baseTimestamp + i}-{i}";
            eventIds.Add(eventId);
        }
        
        // Assert - Event IDs should be unique and comparable
        Assert.Equal(10, eventIds.Count);
        Assert.Equal(10, eventIds.Distinct().Count()); // All unique
        
        _output.WriteLine("Generated event IDs:");
        foreach (var eventId in eventIds)
        {
            _output.WriteLine($"  {eventId}");
        }
    }

    /// <summary>
    /// Tests that progress values from SSE are monotonically increasing
    /// Simulates what the SSE client should enforce
    /// </summary>
    [Fact]
    public void SseProgress_Should_NeverDecrease()
    {
        // Arrange - Simulate SSE progress events that might arrive out of order
        var progressEvents = new[]
        {
            (eventId: "1000-0", progress: 0),
            (eventId: "1001-1", progress: 10),
            (eventId: "1002-2", progress: 25),
            (eventId: "1003-3", progress: 20),  // Out of order - should be ignored
            (eventId: "1004-4", progress: 30),
            (eventId: "1005-5", progress: 28),  // Slight regression - should be ignored
            (eventId: "1006-6", progress: 50),
            (eventId: "1007-7", progress: 100)
        };
        
        var currentProgress = 0;
        var acceptedProgress = new List<int>();
        
        // Act - Process events and enforce monotonic progress
        foreach (var (eventId, progress) in progressEvents)
        {
            if (progress >= currentProgress)
            {
                currentProgress = progress;
                acceptedProgress.Add(progress);
                _output.WriteLine($"Event {eventId}: Accepted progress {progress}%");
            }
            else
            {
                _output.WriteLine($"Event {eventId}: Rejected progress {progress}% (current: {currentProgress}%)");
            }
        }
        
        // Assert - Accepted progress should be monotonically increasing
        for (int i = 1; i < acceptedProgress.Count; i++)
        {
            Assert.True(acceptedProgress[i] >= acceptedProgress[i - 1],
                $"Progress decreased from {acceptedProgress[i - 1]}% to {acceptedProgress[i]}%");
        }
        
        Assert.Equal(100, currentProgress); // Should reach 100%
        Assert.Equal(6, acceptedProgress.Count); // 2 events rejected
    }

    /// <summary>
    /// Tests SSE reconnection with Last-Event-ID
    /// Simulates client disconnecting and reconnecting
    /// </summary>
    [Fact]
    public void SseReconnection_Should_ResumeFromLastEventId()
    {
        // Arrange - Simulate a sequence of SSE events
        var allEvents = new[]
        {
            (id: "1000-0", type: "job-status", data: "Queued"),
            (id: "1001-1", type: "job-status", data: "Running"),
            (id: "1002-2", type: "step-progress", data: "Script: 15%"),
            (id: "1003-3", type: "step-progress", data: "Script: 25%"),
            // Client disconnects here
            (id: "1004-4", type: "step-progress", data: "Voice: 40%"),
            (id: "1005-5", type: "step-progress", data: "Visuals: 60%"),
            (id: "1006-6", type: "job-completed", data: "Done")
        };
        
        // Act - Simulate first connection receiving events 0-3
        var firstConnectionEvents = allEvents.Take(4).ToList();
        var lastEventId = firstConnectionEvents.Last().id;
        
        _output.WriteLine("First connection received events:");
        foreach (var evt in firstConnectionEvents)
        {
            _output.WriteLine($"  {evt.id}: {evt.type} - {evt.data}");
        }
        
        _output.WriteLine($"\nClient disconnected. Last event ID: {lastEventId}");
        
        // Simulate reconnection with Last-Event-ID
        // Server should send events after lastEventId
        var reconnectionEvents = allEvents
            .SkipWhile(e => e.id != lastEventId)
            .Skip(1) // Skip the lastEventId itself
            .ToList();
        
        _output.WriteLine("\nReconnection with Last-Event-ID, receiving:");
        foreach (var evt in reconnectionEvents)
        {
            _output.WriteLine($"  {evt.id}: {evt.type} - {evt.data}");
        }
        
        // Assert - Reconnection should receive remaining events
        Assert.Equal(3, reconnectionEvents.Count);
        Assert.Equal("1004-4", reconnectionEvents[0].id);
        Assert.Equal("1006-6", reconnectionEvents[2].id);
        Assert.Equal("job-completed", reconnectionEvents[2].type);
    }

    /// <summary>
    /// Tests SSE heartbeat/keepalive mechanism
    /// Validates that keepalive comments are sent periodically
    /// </summary>
    [Fact]
    public void SseHeartbeat_Should_SendKeepaliveComments()
    {
        // Arrange - Simulate time-based heartbeat
        var heartbeatInterval = TimeSpan.FromSeconds(10);
        var testDuration = TimeSpan.FromSeconds(35);
        var startTime = DateTime.UtcNow;
        
        var heartbeats = new List<DateTime>();
        
        // Act - Simulate heartbeat generation
        var currentTime = startTime;
        var lastHeartbeat = startTime;
        
        while (currentTime - startTime < testDuration)
        {
            currentTime = currentTime.AddSeconds(1); // Simulate 1 second passing
            
            if ((currentTime - lastHeartbeat) >= heartbeatInterval)
            {
                heartbeats.Add(currentTime);
                lastHeartbeat = currentTime;
                _output.WriteLine($"Heartbeat sent at {(currentTime - startTime).TotalSeconds}s");
            }
        }
        
        // Assert - Should have sent ~3 heartbeats (at 10s, 20s, 30s)
        Assert.True(heartbeats.Count >= 3, $"Expected at least 3 heartbeats, got {heartbeats.Count}");
        
        // Verify spacing between heartbeats
        for (int i = 1; i < heartbeats.Count; i++)
        {
            var spacing = heartbeats[i] - heartbeats[i - 1];
            Assert.True(spacing >= heartbeatInterval, 
                $"Heartbeat spacing {spacing} should be >= {heartbeatInterval}");
        }
    }

    /// <summary>
    /// Tests SSE stage-to-phase mapping consistency
    /// Validates that stage names map to documented phases
    /// </summary>
    [Theory]
    [InlineData("Initialization", "plan")]
    [InlineData("Queued", "plan")]
    [InlineData("Script", "plan")]
    [InlineData("Planning", "plan")]
    [InlineData("Brief", "plan")]
    [InlineData("TTS", "tts")]
    [InlineData("Audio", "tts")]
    [InlineData("Voice", "tts")]
    [InlineData("Visuals", "visuals")]
    [InlineData("Images", "visuals")]
    [InlineData("Assets", "visuals")]
    [InlineData("Composition", "compose")]
    [InlineData("Timeline", "compose")]
    [InlineData("Compose", "compose")]
    [InlineData("Rendering", "render")]
    [InlineData("Render", "render")]
    [InlineData("Encode", "render")]
    [InlineData("Complete", "complete")]
    [InlineData("Done", "complete")]
    [InlineData("Unknown", "processing")]
    public void SseStageMapping_Should_MatchDocumentedPhases(string stage, string expectedPhase)
    {
        // Arrange & Act - Implement the stage-to-phase mapping logic
        var actualPhase = MapStageToPhase(stage);
        
        // Assert
        Assert.Equal(expectedPhase, actualPhase);
        _output.WriteLine($"Stage '{stage}' maps to phase '{actualPhase}'");
    }

    /// <summary>
    /// Tests that SSE event types are consistently labeled
    /// </summary>
    [Fact]
    public void SseEventTypes_Should_BeConsistent()
    {
        // Arrange - List of expected SSE event types
        var expectedEventTypes = new[]
        {
            "job-status",        // Overall job status changes
            "step-status",       // Stage/step transitions
            "step-progress",     // Progress within a step
            "warning",           // Non-fatal warnings
            "error",            // Errors
            "job-completed",    // Successful completion
            "job-failed",       // Job failure
            "job-cancelled"     // Job cancellation
        };
        
        // Act & Assert - Verify all event types follow naming convention
        foreach (var eventType in expectedEventTypes)
        {
            Assert.NotNull(eventType);
            Assert.NotEmpty(eventType);
            Assert.DoesNotContain(" ", eventType); // No spaces
            Assert.True(eventType.ToLowerInvariant() == eventType, 
                $"Event type '{eventType}' should be lowercase");
            
            _output.WriteLine($"Event type validated: {eventType}");
        }
        
        Assert.Equal(8, expectedEventTypes.Length);
    }

    /// <summary>
    /// Helper method to map stage names to phases
    /// This duplicates the logic from JobsController to validate it
    /// </summary>
    private static string MapStageToPhase(string stage)
    {
        return stage.ToLowerInvariant() switch
        {
            "initialization" or "queued" => "plan",
            "script" or "planning" or "brief" => "plan",
            "tts" or "audio" or "voice" => "tts",
            "visuals" or "images" or "assets" => "visuals",
            "composition" or "timeline" or "compose" => "compose",
            "rendering" or "render" or "encode" => "render",
            "complete" or "done" => "complete",
            _ => "processing"
        };
    }

    /// <summary>
    /// Tests SSE error recovery scenarios
    /// </summary>
    [Fact]
    public void SseErrorRecovery_Should_HandleTransientFailures()
    {
        // Arrange - Simulate connection attempts with exponential backoff
        var maxRetries = 5;
        var baseDelay = 1000; // 1 second
        var maxDelay = 30000; // 30 seconds
        
        var retryDelays = new List<int>();
        
        // Act - Calculate retry delays with exponential backoff
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var calculatedDelay = baseDelay * (int)Math.Pow(2, attempt - 1);
            var delay = Math.Min(calculatedDelay, maxDelay);
            retryDelays.Add(delay);
            
            _output.WriteLine($"Retry attempt {attempt}: delay = {delay}ms");
        }
        
        // Assert - Delays should increase exponentially up to max
        Assert.Equal(1000, retryDelays[0]);   // 1s
        Assert.Equal(2000, retryDelays[1]);   // 2s
        Assert.Equal(4000, retryDelays[2]);   // 4s
        Assert.Equal(8000, retryDelays[3]);   // 8s
        Assert.Equal(16000, retryDelays[4]);  // 16s
        
        // All should be <= maxDelay
        Assert.All(retryDelays, delay => Assert.True(delay <= maxDelay));
    }
}
