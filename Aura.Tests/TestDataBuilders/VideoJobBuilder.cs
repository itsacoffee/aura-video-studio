using Aura.Core.Models;
using Aura.Core.Models.Jobs;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test VideoGenerationJob instances with sensible defaults
/// </summary>
public class VideoJobBuilder
{
    private string _jobId = Guid.NewGuid().ToString();
    private string _correlationId = Guid.NewGuid().ToString();
    private Brief? _brief;
    private PlanSpec? _planSpec;
    private VoiceSpec? _voiceSpec;
    private RenderSpec? _renderSpec;
    private SystemProfile? _systemProfile;
    private JobStatus _status = JobStatus.Pending;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _startedAt;
    private DateTime? _completedAt;
    private string? _outputPath;
    private string? _errorMessage;
    private int _retryCount;
    private int _maxRetries = 3;
    private Dictionary<string, object> _metadata = new();
    private List<JobProgressUpdate> _progressUpdates = new();

    public VideoJobBuilder WithJobId(string jobId)
    {
        _jobId = jobId;
        return this;
    }

    public VideoJobBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    public VideoJobBuilder WithBrief(Brief brief)
    {
        _brief = brief;
        return this;
    }

    public VideoJobBuilder WithPlanSpec(PlanSpec planSpec)
    {
        _planSpec = planSpec;
        return this;
    }

    public VideoJobBuilder WithVoiceSpec(VoiceSpec voiceSpec)
    {
        _voiceSpec = voiceSpec;
        return this;
    }

    public VideoJobBuilder WithRenderSpec(RenderSpec renderSpec)
    {
        _renderSpec = renderSpec;
        return this;
    }

    public VideoJobBuilder WithSystemProfile(SystemProfile systemProfile)
    {
        _systemProfile = systemProfile;
        return this;
    }

    public VideoJobBuilder WithStatus(JobStatus status)
    {
        _status = status;
        return this;
    }

    public VideoJobBuilder InProgress()
    {
        _status = JobStatus.Running;
        _startedAt = DateTime.UtcNow.AddMinutes(-5);
        return this;
    }

    public VideoJobBuilder Completed(string outputPath = "/output/test-video.mp4")
    {
        _status = JobStatus.Completed;
        _startedAt = DateTime.UtcNow.AddMinutes(-10);
        _completedAt = DateTime.UtcNow;
        _outputPath = outputPath;
        return this;
    }

    public VideoJobBuilder Failed(string errorMessage = "Test error")
    {
        _status = JobStatus.Failed;
        _startedAt = DateTime.UtcNow.AddMinutes(-5);
        _completedAt = DateTime.UtcNow;
        _errorMessage = errorMessage;
        return this;
    }

    public VideoJobBuilder WithOutputPath(string outputPath)
    {
        _outputPath = outputPath;
        return this;
    }

    public VideoJobBuilder WithRetryCount(int retryCount)
    {
        _retryCount = retryCount;
        return this;
    }

    public VideoJobBuilder WithMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    public VideoJobBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public VideoJobBuilder WithProgressUpdate(JobProgressUpdate update)
    {
        _progressUpdates.Add(update);
        return this;
    }

    public VideoGenerationJob Build()
    {
        return new VideoGenerationJob
        {
            JobId = _jobId,
            CorrelationId = _correlationId,
            Brief = _brief ?? new Brief(
                Topic: "Test Video Topic",
                Audience: "General Audience",
                Goal: "Test Goal",
                Tone: "Professional",
                Language: "English",
                Aspect: Aspect.Widescreen16x9
            ),
            PlanSpec = _planSpec ?? new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(2),
                Pacing: Pacing.Medium,
                Density: Density.Balanced,
                Style: "Documentary"
            ),
            VoiceSpec = _voiceSpec ?? new VoiceSpec(
                VoiceName: "default",
                Rate: 1.0,
                Pitch: 1.0,
                Pause: PauseStyle.Normal
            ),
            RenderSpec = _renderSpec ?? new RenderSpec(
                Res: new Resolution(1920, 1080),
                Container: "mp4",
                VideoBitrateK: 2500,
                AudioBitrateK: 128,
                Fps: 30,
                Codec: "H264"
            ),
            SystemProfile = _systemProfile ?? new SystemProfile 
            { 
                Tier = HardwareTier.B,
                LogicalCores = 8,
                PhysicalCores = 4,
                RamGB = 16
            },
            Status = _status,
            CreatedAt = _createdAt,
            StartedAt = _startedAt,
            CompletedAt = _completedAt,
            OutputPath = _outputPath,
            ErrorMessage = _errorMessage,
            RetryCount = _retryCount,
            MaxRetries = _maxRetries,
            Metadata = _metadata,
            ProgressUpdates = _progressUpdates
        };
    }
}
