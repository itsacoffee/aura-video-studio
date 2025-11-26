using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Agents.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.AI.Agents.Telemetry;

public class AgentTelemetryTests
{
    [Fact]
    public void TrackInvocation_RecordsAgentExecution()
    {
        // Arrange
        var logger = new Mock<ILogger<AgentTelemetry>>();
        var telemetry = new AgentTelemetry(logger.Object);

        // Act
        using (var tracker = telemetry.TrackInvocation("Screenwriter", "GenerateScript"))
        {
            Thread.Sleep(100); // Simulate work
        }

        // Assert
        var report = telemetry.GetReport();
        Assert.Equal(1, report.InvocationCount);
        Assert.True(report.TimePerAgent.ContainsKey("Screenwriter"));
        Assert.True(report.TimePerAgent["Screenwriter"].TotalMilliseconds >= 100);
    }

    [Fact]
    public void RecordIteration_TracksApprovalStatus()
    {
        // Arrange
        var logger = new Mock<ILogger<AgentTelemetry>>();
        var telemetry = new AgentTelemetry(logger.Object);

        // Act
        telemetry.RecordIteration(1, false, "Needs work");
        telemetry.RecordIteration(2, true, null);

        // Assert
        var report = telemetry.GetReport();
        Assert.Equal(2, report.TotalIterations);
        Assert.Equal(2, report.ApprovedOnIteration);
        Assert.Equal(2, report.IterationHistory.Count);
        Assert.False(report.IterationHistory[0].Approved);
        Assert.True(report.IterationHistory[1].Approved);
    }

    [Fact]
    public void GetReport_AggregatesTimeByAgent()
    {
        // Arrange
        var logger = new Mock<ILogger<AgentTelemetry>>();
        var telemetry = new AgentTelemetry(logger.Object);

        // Act
        using (telemetry.TrackInvocation("Screenwriter", "GenerateScript"))
        {
            Thread.Sleep(50);
        }
        using (telemetry.TrackInvocation("Screenwriter", "ReviseScript"))
        {
            Thread.Sleep(50);
        }
        using (telemetry.TrackInvocation("VisualDirector", "GeneratePrompts"))
        {
            Thread.Sleep(30);
        }

        // Assert
        var report = telemetry.GetReport();
        Assert.Equal(3, report.InvocationCount);
        Assert.True(report.TimePerAgent["Screenwriter"].TotalMilliseconds >= 100);
        Assert.True(report.TimePerAgent["VisualDirector"].TotalMilliseconds >= 30);
    }

    [Fact]
    public void LogSummary_OutputsStructuredInformation()
    {
        // Arrange
        var logger = new Mock<ILogger<AgentTelemetry>>();
        var telemetry = new AgentTelemetry(logger.Object);

        telemetry.RecordIteration(1, false, "Feedback");
        telemetry.RecordIteration(2, true);

        // Act
        telemetry.LogSummary();

        // Assert - Verify logger was called with summary information
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent Performance Summary")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

