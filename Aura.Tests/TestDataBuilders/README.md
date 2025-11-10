# Test Data Builders

Comprehensive test data builders for creating test fixtures with fluent APIs.

## Overview

Test data builders provide a clean, readable way to create test data with sensible defaults and easy customization.

## Available Builders

### VideoJobBuilder

Creates video generation jobs for testing.

```csharp
var job = new VideoJobBuilder()
    .WithId(Guid.NewGuid())
    .WithTitle("Test Video")
    .WithStatus(JobStatus.Processing)
    .WithProgress(50)
    .Build();
```

### ProjectBuilder

Creates project instances for testing.

```csharp
var project = new ProjectBuilder()
    .WithName("Test Project")
    .WithDescription("A test project")
    .WithCreatedDate(DateTime.UtcNow)
    .Build();
```

### TimelineBuilder

Creates timeline structures for testing.

```csharp
var timeline = new TimelineBuilder()
    .WithDuration(60.0)
    .AddTrack(new TrackBuilder().WithType(TrackType.Video).Build())
    .AddTrack(new TrackBuilder().WithType(TrackType.Audio).Build())
    .Build();
```

### AssetBuilder

Creates asset instances for testing.

```csharp
var asset = new AssetBuilder()
    .WithType(AssetType.Video)
    .WithPath("/path/to/asset.mp4")
    .WithDuration(30.0)
    .Build();
```

### ApiKeyBuilder

Creates API key configurations for testing.

```csharp
var apiKey = new ApiKeyBuilder()
    .WithProvider("OpenAI")
    .WithKey("test-key-123")
    .WithIsValid(true)
    .Build();
```

## Usage Patterns

### Basic Usage

```csharp
[Fact]
public void Should_Process_Video_Job()
{
    // Arrange
    var job = new VideoJobBuilder()
        .WithDefaults()
        .Build();
    
    // Act
    var result = _service.ProcessJob(job);
    
    // Assert
    result.Should().NotBeNull();
}
```

### Customization

```csharp
[Fact]
public void Should_Handle_Failed_Job()
{
    // Arrange
    var job = new VideoJobBuilder()
        .WithStatus(JobStatus.Failed)
        .WithErrorMessage("Test error")
        .Build();
    
    // Act & Assert
    var exception = Assert.Throws<JobException>(() => _service.RetryJob(job));
    exception.Message.Should().Contain("Test error");
}
```

### Chaining Builders

```csharp
[Fact]
public void Should_Create_Complete_Project()
{
    // Arrange
    var project = new ProjectBuilder()
        .WithName("My Project")
        .WithAsset(new AssetBuilder().WithType(AssetType.Video).Build())
        .WithAsset(new AssetBuilder().WithType(AssetType.Audio).Build())
        .WithTimeline(new TimelineBuilder().WithDuration(60).Build())
        .Build();
    
    // Act
    var result = _repository.Save(project);
    
    // Assert
    result.Should().NotBeNull();
    result.Assets.Should().HaveCount(2);
}
```

### Test Data Variants

```csharp
public class VideoJobTestData
{
    public static VideoJob PendingJob => new VideoJobBuilder()
        .WithStatus(JobStatus.Pending)
        .Build();
    
    public static VideoJob ProcessingJob => new VideoJobBuilder()
        .WithStatus(JobStatus.Processing)
        .WithProgress(50)
        .Build();
    
    public static VideoJob CompletedJob => new VideoJobBuilder()
        .WithStatus(JobStatus.Completed)
        .WithProgress(100)
        .WithOutputPath("/output/video.mp4")
        .Build();
    
    public static VideoJob FailedJob => new VideoJobBuilder()
        .WithStatus(JobStatus.Failed)
        .WithErrorMessage("Processing failed")
        .Build();
}
```

## Best Practices

### 1. Use Sensible Defaults

```csharp
public class VideoJobBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _title = "Default Test Video";
    private JobStatus _status = JobStatus.Pending;
    private int _progress = 0;
    
    // ... builder methods
}
```

### 2. Provide Fluent Interface

```csharp
public VideoJobBuilder WithTitle(string title)
{
    _title = title;
    return this; // Return this for chaining
}
```

### 3. Keep Builders Simple

```csharp
// ❌ Don't include business logic
public VideoJobBuilder WithValidatedTitle(string title)
{
    if (string.IsNullOrEmpty(title))
        throw new ArgumentException("Title required");
    _title = title;
    return this;
}

// ✅ Just set the value
public VideoJobBuilder WithTitle(string title)
{
    _title = title;
    return this;
}
```

### 4. Support Random Data

```csharp
public VideoJobBuilder WithRandomData()
{
    _id = Guid.NewGuid();
    _title = $"Test Video {Random.Shared.Next(1000)}";
    _duration = Random.Shared.Next(10, 300);
    return this;
}
```

### 5. Create Helper Methods

```csharp
public static class VideoJobBuilderExtensions
{
    public static VideoJobBuilder AsCompleted(this VideoJobBuilder builder)
    {
        return builder
            .WithStatus(JobStatus.Completed)
            .WithProgress(100)
            .WithCompletedDate(DateTime.UtcNow);
    }
    
    public static VideoJobBuilder AsFailed(this VideoJobBuilder builder, string error)
    {
        return builder
            .WithStatus(JobStatus.Failed)
            .WithErrorMessage(error);
    }
}
```

## Testing the Builders

Builders themselves should have simple tests:

```csharp
public class VideoJobBuilderTests
{
    [Fact]
    public void Should_Build_With_Defaults()
    {
        var job = new VideoJobBuilder().Build();
        
        job.Should().NotBeNull();
        job.Id.Should().NotBeEmpty();
        job.Status.Should().Be(JobStatus.Pending);
    }
    
    [Fact]
    public void Should_Allow_Customization()
    {
        var job = new VideoJobBuilder()
            .WithTitle("Custom Title")
            .Build();
        
        job.Title.Should().Be("Custom Title");
    }
}
```

## Integration with xUnit

### Theory Data

```csharp
public class VideoJobStatusTests
{
    public static TheoryData<VideoJob, bool> JobValidationCases => new()
    {
        { new VideoJobBuilder().WithStatus(JobStatus.Pending).Build(), true },
        { new VideoJobBuilder().WithStatus(JobStatus.Processing).Build(), true },
        { new VideoJobBuilder().WithStatus(JobStatus.Completed).Build(), false },
        { new VideoJobBuilder().WithStatus(JobStatus.Failed).Build(), false }
    };
    
    [Theory]
    [MemberData(nameof(JobValidationCases))]
    public void Should_Validate_Job_Status(VideoJob job, bool canStart)
    {
        var result = _validator.CanStartJob(job);
        result.Should().Be(canStart);
    }
}
```

### ClassData

```csharp
public class FailedJobsData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new VideoJobBuilder().WithErrorMessage("Timeout").Build() };
        yield return new object[] { new VideoJobBuilder().WithErrorMessage("Invalid input").Build() };
        yield return new object[] { new VideoJobBuilder().WithErrorMessage("Service unavailable").Build() };
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(FailedJobsData))]
public void Should_Handle_Failed_Jobs(VideoJob job)
{
    var result = _service.RetryJob(job);
    result.Should().NotBeNull();
}
```

## Performance Considerations

### 1. Lazy Initialization

```csharp
public class VideoJobBuilder
{
    private Lazy<List<Asset>> _assets = new(() => new List<Asset>());
    
    public VideoJobBuilder AddAsset(Asset asset)
    {
        _assets.Value.Add(asset);
        return this;
    }
}
```

### 2. Object Pooling (for very hot paths)

```csharp
public class VideoJobBuilderPool
{
    private static readonly ObjectPool<VideoJobBuilder> _pool = 
        ObjectPool.Create(new DefaultPooledObjectPolicy<VideoJobBuilder>());
    
    public static VideoJobBuilder Get() => _pool.Get();
    public static void Return(VideoJobBuilder builder) => _pool.Return(builder);
}
```

## Troubleshooting

### Builder Not Chainable

```csharp
// ❌ Forgot to return this
public VideoJobBuilder WithTitle(string title)
{
    _title = title;
}

// ✅ Always return this
public VideoJobBuilder WithTitle(string title)
{
    _title = title;
    return this;
}
```

### Immutable Objects

```csharp
// If your model is immutable
public class VideoJobBuilder
{
    private readonly Dictionary<string, object> _props = new();
    
    public VideoJobBuilder WithTitle(string title)
    {
        _props["title"] = title;
        return this;
    }
    
    public VideoJob Build()
    {
        return new VideoJob(
            title: _props["title"] as string ?? "Default",
            // ... other props
        );
    }
}
```

## References

- [Test Data Builders Pattern](https://www.martinfowler.com/bliki/ObjectMother.html)
- [Fluent Interface Design](https://martinfowler.com/bliki/FluentInterface.html)
- [xUnit Best Practices](https://xunit.net/docs/getting-started/netcore/cmdline)
