using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Aura.Core.Orchestrator.Stages;
using Aura.Core.Models;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Orchestrator;

public class BriefStageTests
{
    private readonly Mock<ILogger<BriefStage>> _mockLogger;
    private readonly Mock<PreGenerationValidator> _mockValidator;

    public BriefStageTests()
    {
        _mockLogger = new Mock<ILogger<BriefStage>>();
        _mockValidator = new Mock<PreGenerationValidator>(
            Mock.Of<ILogger<PreGenerationValidator>>(),
            Mock.Of<Core.Dependencies.IFfmpegLocator>(),
            Mock.Of<Core.Dependencies.FFmpegResolver>(MockBehavior.Loose),
            Mock.Of<Core.Hardware.IHardwareDetector>(),
            Mock.Of<Core.Services.Providers.IProviderReadinessService>(),
            null);
    }

    [Fact]
    public async Task ExecuteAsync_ValidBrief_Succeeds()
    {
        // Arrange
        _mockValidator
            .Setup(v => v.ValidateSystemReadyAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(true, new List<string>()));

        var stage = new BriefStage(_mockLogger.Object, _mockValidator.Object);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Brief", result.StageName);
        
        var output = context.GetStageOutput<BriefStageOutput>("Brief");
        Assert.NotNull(output);
        Assert.Equal(context.Brief, output.ValidatedBrief);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidBrief_Fails()
    {
        // Arrange
        _mockValidator
            .Setup(v => v.ValidateSystemReadyAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(false, new List<string> { "Invalid topic", "Missing dependencies" }));

        var stage = new BriefStage(_mockLogger.Object, _mockValidator.Object);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Exception);
        Assert.IsType<Core.Errors.ValidationException>(result.Exception);
    }

    [Fact]
    public void StageName_ReturnsCorrectValue()
    {
        // Arrange
        var stage = new BriefStage(_mockLogger.Object, _mockValidator.Object);

        // Assert
        Assert.Equal("Brief", stage.StageName);
        Assert.Equal("Brief Validation", stage.DisplayName);
    }

    [Fact]
    public void ProgressWeight_ReturnsCorrectValue()
    {
        // Arrange
        var stage = new BriefStage(_mockLogger.Object, _mockValidator.Object);

        // Assert
        Assert.Equal(5, stage.ProgressWeight);
    }

    private PipelineContext CreateTestContext()
    {
        return new PipelineContext(
            correlationId: Guid.NewGuid().ToString(),
            brief: new Brief("Test topic", "Test audience", "Test goal", "Professional", "English", Aspect.Widescreen16x9, null),
            planSpec: new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "informative"),
            voiceSpec: new VoiceSpec("test-voice", 1.0, 0.0, PauseStyle.Natural),
            renderSpec: new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192, 30, "H264", 75, true),
            systemProfile: new SystemProfile { Tier = HardwareTier.B, LogicalCores = 8, PhysicalCores = 4, RamGB = 16 }
        );
    }
}
