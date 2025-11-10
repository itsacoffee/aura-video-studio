using Aura.Core.Models;
using Aura.Core.Models.Jobs;
using Aura.Core.Models.Video;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test VideoJob instances with sensible defaults
/// </summary>
public class VideoJobBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _projectId = Guid.NewGuid().ToString();
    private JobStatus _status = JobStatus.Pending;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _startedAt;
    private DateTime? _completedAt;
    private string? _error;
    private double _progress;
    private VideoGenerationSpec? _spec;
    private Dictionary<string, object> _metadata = new();

    public VideoJobBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public VideoJobBuilder WithProjectId(string projectId)
    {
        _projectId = projectId;
        return this;
    }

    public VideoJobBuilder WithStatus(JobStatus status)
    {
        _status = status;
        return this;
    }

    public VideoJobBuilder InProgress(double progress = 0.5)
    {
        _status = JobStatus.Running;
        _startedAt = DateTime.UtcNow.AddMinutes(-5);
        _progress = progress;
        return this;
    }

    public VideoJobBuilder Completed()
    {
        _status = JobStatus.Completed;
        _startedAt = DateTime.UtcNow.AddMinutes(-10);
        _completedAt = DateTime.UtcNow;
        _progress = 1.0;
        return this;
    }

    public VideoJobBuilder Failed(string error = "Test error")
    {
        _status = JobStatus.Failed;
        _startedAt = DateTime.UtcNow.AddMinutes(-5);
        _completedAt = DateTime.UtcNow;
        _error = error;
        return this;
    }

    public VideoJobBuilder WithProgress(double progress)
    {
        _progress = progress;
        return this;
    }

    public VideoJobBuilder WithSpec(VideoGenerationSpec spec)
    {
        _spec = spec;
        return this;
    }

    public VideoJobBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public VideoJob Build()
    {
        return new VideoJob
        {
            Id = _id,
            ProjectId = _projectId,
            Status = _status,
            CreatedAt = _createdAt,
            StartedAt = _startedAt,
            CompletedAt = _completedAt,
            Error = _error,
            Progress = _progress,
            Spec = _spec ?? new VideoGenerationSpec { Title = "Test Video" },
            Metadata = _metadata
        };
    }
}
