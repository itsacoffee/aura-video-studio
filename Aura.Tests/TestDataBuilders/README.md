# Test Data Builders

This directory contains builder classes for creating test data with sensible defaults and fluent APIs.

## Purpose

Test data builders provide:
- **Consistent test data** across all tests
- **Readable test code** with fluent interfaces
- **Easy customization** while maintaining defaults
- **Type safety** at compile time

## Available Builders

### VideoJobBuilder
Create test video jobs with various states:

```csharp
var job = new VideoJobBuilder()
    .WithProjectId("project-123")
    .InProgress(0.5)
    .Build();

var completedJob = new VideoJobBuilder()
    .Completed()
    .Build();

var failedJob = new VideoJobBuilder()
    .Failed("FFmpeg error")
    .Build();
```

### ProjectBuilder
Create test projects:

```csharp
var project = new ProjectBuilder()
    .WithName("My Video Project")
    .WithOwnerId("user-123")
    .WithTag("tutorial")
    .Build();

var archivedProject = new ProjectBuilder()
    .Archived()
    .Build();
```

### TimelineBuilder
Create timelines with tracks and clips:

```csharp
var timeline = new TimelineBuilder()
    .WithDuration(120.0)
    .WithDefaultVideoTrack()
    .WithDefaultAudioTrack()
    .Build();

var customTimeline = new TimelineBuilder()
    .WithTrack(new TrackBuilder()
        .WithType(TrackType.Video)
        .WithClip(new ClipBuilder()
            .AtTime(0.0)
            .WithDuration(5.0)
            .Build())
        .Build())
    .Build();
```

### AssetBuilder
Create test assets:

```csharp
var videoAsset = new AssetBuilder()
    .AsVideo()
    .WithDuration(30.0)
    .WithTag("stock-footage")
    .Build();

var audioAsset = new AssetBuilder()
    .AsAudio()
    .WithName("background-music.mp3")
    .Build();
```

### ApiKeyBuilder
Create API key configurations:

```csharp
var validKey = new ApiKeyBuilder()
    .ForProvider("openai")
    .Valid()
    .Build();

var invalidKey = new ApiKeyBuilder()
    .ForProvider("elevenlabs")
    .Invalid("Insufficient credits")
    .Build();
```

## Best Practices

1. **Use builders in all tests** - Don't create models manually
2. **Start with defaults** - Only override what matters for your test
3. **Chain methods** - Use fluent interface for readability
4. **Create helper methods** - For common test scenarios
5. **Keep builders simple** - One builder per domain model

## Example Test

```csharp
[Fact]
public async Task ProcessJob_WithValidJob_CompletesSuccessfully()
{
    // Arrange
    var job = new VideoJobBuilder()
        .WithProjectId("test-project")
        .WithSpec(new VideoGenerationSpec { Title = "Test" })
        .Build();

    var mockProcessor = new Mock<IVideoProcessor>();
    
    // Act
    await _service.ProcessJob(job);
    
    // Assert
    Assert.Equal(JobStatus.Completed, job.Status);
}
```

## Adding New Builders

When adding a new builder:

1. Create a new file in this directory
2. Follow the naming convention: `{ModelName}Builder.cs`
3. Implement fluent interface with `With*` methods
4. Provide sensible defaults in constructor/fields
5. Add common scenario methods (e.g., `Completed()`, `Failed()`)
6. Update this README with examples
