using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Agents.Telemetry;

/// <summary>
/// Record of a single agent invocation
/// </summary>
public class AgentInvocationRecord
{
    public string AgentName { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public double? DurationMs { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Record of an iteration decision
/// </summary>
public class IterationRecord
{
    public int IterationNumber { get; set; }
    public bool Approved { get; set; }
    public string? Feedback { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Performance report for agent execution
/// </summary>
public class AgentPerformanceReport
{
    public int TotalIterations { get; set; }
    public int ApprovedOnIteration { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public Dictionary<string, TimeSpan> TimePerAgent { get; set; } = new();
    public List<IterationRecord> IterationHistory { get; set; } = new();
    public int InvocationCount { get; set; }
}

/// <summary>
/// Telemetry tracking for agent performance in local execution
/// </summary>
public class AgentTelemetry
{
    private readonly ILogger<AgentTelemetry> _logger;
    private readonly List<AgentInvocationRecord> _invocations = new();
    private readonly List<IterationRecord> _iterations = new();

    public AgentTelemetry(ILogger<AgentTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start tracking an agent invocation
    /// </summary>
    public IDisposable TrackInvocation(string agentName, string messageType)
    {
        _logger.LogInformation(
            "[Agent] {AgentName} starting: {MessageType}",
            agentName,
            messageType);

        var record = new AgentInvocationRecord
        {
            AgentName = agentName,
            MessageType = messageType,
            StartTime = DateTime.UtcNow
        };

        _invocations.Add(record);
        var sw = Stopwatch.StartNew();

        return new InvocationTracker(this, record, sw);
    }

    /// <summary>
    /// Record an iteration decision from the Critic
    /// </summary>
    public void RecordIteration(int iterationNumber, bool approved, string? feedback = null)
    {
        _logger.LogInformation(
            "[Agent] Iteration {Iteration}: {Status}",
            iterationNumber,
            approved ? "APPROVED" : "NEEDS REVISION");

        if (!string.IsNullOrEmpty(feedback))
        {
            _logger.LogInformation("[Agent] Critic feedback: {Feedback}", feedback);
        }

        _iterations.Add(new IterationRecord
        {
            IterationNumber = iterationNumber,
            Approved = approved,
            Feedback = feedback,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get a summary report of the agent execution
    /// </summary>
    public AgentPerformanceReport GetReport()
    {
        var totalTime = _invocations
            .Where(i => i.DurationMs.HasValue)
            .Sum(i => i.DurationMs!.Value);

        var timeByAgent = _invocations
            .Where(i => i.DurationMs.HasValue)
            .GroupBy(i => i.AgentName)
            .ToDictionary(
                g => g.Key,
                g => TimeSpan.FromMilliseconds(g.Sum(i => i.DurationMs!.Value))
            );

        var approvedIteration = _iterations.FirstOrDefault(i => i.Approved)?.IterationNumber ?? 0;

        return new AgentPerformanceReport
        {
            TotalIterations = _iterations.Count,
            ApprovedOnIteration = approvedIteration,
            TotalProcessingTime = TimeSpan.FromMilliseconds(totalTime),
            TimePerAgent = timeByAgent,
            IterationHistory = _iterations.ToList(),
            InvocationCount = _invocations.Count
        };
    }

    /// <summary>
    /// Log a summary to the console/logs
    /// </summary>
    public void LogSummary()
    {
        var report = GetReport();

        _logger.LogInformation("=== Agent Performance Summary ===");
        _logger.LogInformation("Total Iterations: {Count}", report.TotalIterations);
        _logger.LogInformation("Approved on Iteration: {Iteration}", report.ApprovedOnIteration);
        _logger.LogInformation("Total Time: {Time:F2}s", report.TotalProcessingTime.TotalSeconds);

        foreach (var (agent, time) in report.TimePerAgent)
        {
            _logger.LogInformation("  {Agent}: {Time:F2}s", agent, time.TotalSeconds);
        }

        _logger.LogInformation("================================");
    }

    private void CompleteInvocation(AgentInvocationRecord record, Stopwatch sw, bool success)
    {
        record.DurationMs = sw.Elapsed.TotalMilliseconds;
        record.Success = success;

        _logger.LogInformation(
            "[Agent] {AgentName} completed in {Duration:F2}s: {Status}",
            record.AgentName,
            sw.Elapsed.TotalSeconds,
            success ? "SUCCESS" : "FAILED");
    }

    private class InvocationTracker : IDisposable
    {
        private readonly AgentTelemetry _telemetry;
        private readonly AgentInvocationRecord _record;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public InvocationTracker(AgentTelemetry telemetry, AgentInvocationRecord record, Stopwatch stopwatch)
        {
            _telemetry = telemetry;
            _record = record;
            _stopwatch = stopwatch;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _telemetry.CompleteInvocation(_record, _stopwatch, true);
                _disposed = true;
            }
        }
    }
}

