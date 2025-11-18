using Aura.Api.Contracts;
using Xunit;

namespace Aura.Tests.Contracts;

/// <summary>
/// Tests for BackendEndpoints constants to ensure endpoint paths remain stable
/// and consistent across Electron, backend, and frontend layers.
/// </summary>
public class BackendEndpointsTests
{
    [Fact]
    public void HealthLive_Should_HaveExpectedValue()
    {
        // Assert
        Assert.Equal("/health/live", BackendEndpoints.HealthLive);
    }

    [Fact]
    public void HealthReady_Should_HaveExpectedValue()
    {
        // Assert
        Assert.Equal("/health/ready", BackendEndpoints.HealthReady);
    }

    [Fact]
    public void JobsBase_Should_HaveExpectedValue()
    {
        // Assert
        Assert.Equal("/api/jobs", BackendEndpoints.JobsBase);
    }

    [Fact]
    public void JobEventsTemplate_Should_HaveExpectedValue()
    {
        // Assert
        Assert.Equal("/api/jobs/{id}/events", BackendEndpoints.JobEventsTemplate);
    }

    [Fact]
    public void JobEventsTemplate_Should_ContainIdPlaceholder()
    {
        // Assert
        Assert.Contains("{id}", BackendEndpoints.JobEventsTemplate);
    }

    [Fact]
    public void BuildJobEventsPath_Should_SubstituteJobId()
    {
        // Arrange
        const string jobId = "job-12345";

        // Act
        var result = BackendEndpoints.BuildJobEventsPath(jobId);

        // Assert
        Assert.Equal("/api/jobs/job-12345/events", result);
    }

    [Fact]
    public void BuildJobEventsPath_Should_NotContainPlaceholder()
    {
        // Arrange
        const string jobId = "test-job";

        // Act
        var result = BackendEndpoints.BuildJobEventsPath(jobId);

        // Assert
        Assert.DoesNotContain("{id}", result);
    }

    [Fact]
    public void BuildJobEventsPath_Should_ThrowForNullJobId()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => BackendEndpoints.BuildJobEventsPath(null!));
        Assert.Equal("jobId", exception.ParamName);
    }

    [Fact]
    public void BuildJobEventsPath_Should_ThrowForEmptyJobId()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => BackendEndpoints.BuildJobEventsPath(""));
        Assert.Equal("jobId", exception.ParamName);
    }

    [Fact]
    public void BuildJobEventsPath_Should_ThrowForWhitespaceJobId()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => BackendEndpoints.BuildJobEventsPath("   "));
        Assert.Equal("jobId", exception.ParamName);
    }

    [Fact]
    public void BuildJobEventsPath_Should_HandleJobIdWithSpecialCharacters()
    {
        // Arrange
        const string jobId = "job-with-dashes-123";

        // Act
        var result = BackendEndpoints.BuildJobEventsPath(jobId);

        // Assert
        Assert.Equal("/api/jobs/job-with-dashes-123/events", result);
    }

    [Fact]
    public void BuildJobEventsPath_Should_HandleGuidJobId()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();

        // Act
        var result = BackendEndpoints.BuildJobEventsPath(jobId);

        // Assert
        Assert.StartsWith("/api/jobs/", result);
        Assert.EndsWith("/events", result);
        Assert.Contains(jobId, result);
    }

    [Theory]
    [InlineData("simple-job")]
    [InlineData("job123")]
    [InlineData("JOB-ABC-456")]
    [InlineData("job_with_underscores")]
    public void BuildJobEventsPath_Should_HandleVariousJobIdFormats(string jobId)
    {
        // Act
        var result = BackendEndpoints.BuildJobEventsPath(jobId);

        // Assert
        Assert.Equal($"/api/jobs/{jobId}/events", result);
        Assert.DoesNotContain("{id}", result);
    }

    [Fact]
    public void AllEndpoints_Should_StartWithSlash()
    {
        // Assert - all endpoint constants should start with /
        Assert.StartsWith("/", BackendEndpoints.HealthLive);
        Assert.StartsWith("/", BackendEndpoints.HealthReady);
        Assert.StartsWith("/", BackendEndpoints.JobsBase);
        Assert.StartsWith("/", BackendEndpoints.JobEventsTemplate);
    }

    [Fact]
    public void HealthEndpoints_Should_NotContainApiPrefix()
    {
        // Assert - health endpoints are at root level, not under /api
        Assert.DoesNotContain("/api", BackendEndpoints.HealthLive);
        Assert.DoesNotContain("/api", BackendEndpoints.HealthReady);
    }

    [Fact]
    public void JobsEndpoints_Should_ContainApiPrefix()
    {
        // Assert - jobs endpoints are under /api
        Assert.StartsWith("/api", BackendEndpoints.JobsBase);
        Assert.StartsWith("/api", BackendEndpoints.JobEventsTemplate);
    }
}
