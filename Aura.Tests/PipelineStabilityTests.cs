using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Services.Generation;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for pipeline stability improvements including retry logic, validation, and cleanup
/// </summary>
public class PipelineStabilityTests
{
    [Fact]
    public async Task RetryWrapper_SucceedsAfterTransientFailure()
    {
        // Arrange
        var logger = new Mock<ILogger<ProviderRetryWrapper>>();
        var wrapper = new ProviderRetryWrapper(logger.Object);
        int attemptCount = 0;

        // Act
        var result = await wrapper.ExecuteWithRetryAsync(
            async ct =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new HttpRequestException("Transient error");
                }
                return "Success";
            },
            "TestOperation",
            CancellationToken.None);

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryWrapper_ThrowsAfterMaxRetries()
    {
        // Arrange
        var logger = new Mock<ILogger<ProviderRetryWrapper>>();
        var wrapper = new ProviderRetryWrapper(logger.Object);
        int attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await wrapper.ExecuteWithRetryAsync<string>(
                ct =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Persistent error");
                },
                "TestOperation",
                CancellationToken.None,
                maxRetries: 2);
        });

        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void TtsOutputValidator_ValidatesFileExistence()
    {
        // Arrange
        var logger = new Mock<ILogger<TtsOutputValidator>>();
        var validator = new TtsOutputValidator(logger.Object);

        // Act
        var result = validator.ValidateAudioFile("nonexistent.wav", TimeSpan.FromSeconds(5));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("not found"));
    }

    [Fact]
    public void ImageOutputValidator_ValidatesEmptyAssets()
    {
        // Arrange
        var logger = new Mock<ILogger<ImageOutputValidator>>();
        var validator = new ImageOutputValidator(logger.Object);
        var assets = new List<Asset>();

        // Act
        var result = validator.ValidateImageAssets(assets, expectedMinCount: 1);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Insufficient assets"));
    }

    [Fact]
    public void LlmOutputValidator_DetectsInvalidScript()
    {
        // Arrange
        var logger = new Mock<ILogger<LlmOutputValidator>>();
        var validator = new LlmOutputValidator(logger.Object);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "casual");

        // Act - script without proper structure
        var result = validator.ValidateScriptContent("This is not a valid script", planSpec);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public void LlmOutputValidator_DetectsPlaceholderText()
    {
        // Arrange
        var logger = new Mock<ILogger<LlmOutputValidator>>();
        var validator = new LlmOutputValidator(logger.Object);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "casual");

        // Act - script with placeholder
        var script = @"# Test Video
## Scene 1
This is a [PLACEHOLDER] scene that needs content.
## Scene 2
Another scene with actual content.";
        
        var result = validator.ValidateScriptContent(script, planSpec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("placeholder"));
    }

    [Fact]
    public void LlmOutputValidator_DetectsAIRefusal()
    {
        // Arrange
        var logger = new Mock<ILogger<LlmOutputValidator>>();
        var validator = new LlmOutputValidator(logger.Object);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "casual");

        // Act - script with AI refusal
        var script = @"I cannot create this content as an AI.";
        
        var result = validator.ValidateScriptContent(script, planSpec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("refusal"));
    }

    [Fact]
    public void ResourceCleanupManager_RegistersAndCleansUpFiles()
    {
        // Arrange
        var logger = new Mock<ILogger<ResourceCleanupManager>>();
        var manager = new ResourceCleanupManager(logger.Object);
        
        // Act
        manager.RegisterTempFile("/tmp/test1.txt");
        manager.RegisterTempFile("/tmp/test2.txt");
        
        // No exception should be thrown
        manager.CleanupAll();
        
        // Assert - verify logger was called
        logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleanup complete")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ScriptValidator_ValidatesValidScript()
    {
        // Arrange
        var validator = new ScriptValidator();
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "casual");
        
        var validScript = @"# My Video
## Scene 1
This is the first scene with some content. It has enough words to be valid.
## Scene 2
This is the second scene with more content. Multiple sentences here.";

        // Act
        var result = validator.Validate(validScript, planSpec);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }
}
