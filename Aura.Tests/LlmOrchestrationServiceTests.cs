using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Orchestration;
using Aura.Core.AI.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class LlmOrchestrationServiceTests
{
    private readonly LlmOrchestrationService _service;
    private readonly Mock<ILogger<LlmOrchestrationService>> _mockLogger;
    private readonly Mock<ILogger<SchemaValidator>> _mockValidatorLogger;

    public LlmOrchestrationServiceTests()
    {
        _mockLogger = new Mock<ILogger<LlmOrchestrationService>>();
        _mockValidatorLogger = new Mock<ILogger<SchemaValidator>>();
        var validator = new SchemaValidator(_mockValidatorLogger.Object);
        _service = new LlmOrchestrationService(_mockLogger.Object, validator);
    }

    [Fact]
    public void Constructor_InitializesStepRegistry()
    {
        var steps = _service.GetAllSteps();
        
        Assert.NotEmpty(steps);
        Assert.Contains("brief_to_plan", steps.Keys);
        Assert.Contains("plan_to_scenes", steps.Keys);
        Assert.Contains("scenes_to_voice", steps.Keys);
        Assert.Contains("scenes_to_visuals", steps.Keys);
        Assert.Contains("voice_to_ssml", steps.Keys);
        Assert.Contains("assets_to_timeline", steps.Keys);
    }

    [Fact]
    public void RegisterStep_AddsStepToRegistry()
    {
        _service.RegisterStep("custom_step", "InputType", "OutputType");
        
        var step = _service.GetStepInfo("custom_step");
        
        Assert.NotNull(step);
        Assert.Equal("custom_step", step.StepId);
        Assert.Equal("InputType", step.InputSchema);
        Assert.Equal("OutputType", step.OutputSchema);
    }

    [Fact]
    public async Task ExecuteStepAsync_ValidOutput_ReturnsSuccess()
    {
        var validJson = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure with clear beginning, middle, and end"",
            ""keyMessages"": [""Message 1"", ""Message 2"", ""Message 3""],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(validJson);

        var result = await _service.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            LlmInvoker,
            new OrchestrationConfig(),
            "TestProvider",
            "test-model"
        );

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.SceneCount);
        Assert.Equal(1, result.AttemptsUsed);
        Assert.NotNull(result.Data.Metadata);
        Assert.Equal("TestProvider", result.Data.Metadata.Provider);
        Assert.Equal("test-model", result.Data.Metadata.Model);
    }

    [Fact]
    public async Task ExecuteStepAsync_InvalidOutput_WithAutoRepair_Retries()
    {
        var attempt = 0;
        var invalidJson = @"{
            ""outline"": ""Short"",
            ""sceneCount"": 100,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""invalid"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Short"",
            ""keyMessages"": [],
            ""schema_version"": ""1.0""
        }";

        var validJson = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure with clear beginning, middle, and end"",
            ""keyMessages"": [""Message 1""],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct)
        {
            attempt++;
            return attempt == 1 ? Task.FromResult(invalidJson) : Task.FromResult(validJson);
        }

        var result = await _service.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            LlmInvoker,
            new OrchestrationConfig(MaxRetries: 3, EnableAutoRepair: true)
        );

        Assert.True(result.Success);
        Assert.Equal(2, result.AttemptsUsed);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task ExecuteStepAsync_InvalidOutput_WithoutAutoRepair_FailsImmediately()
    {
        var invalidJson = @"{
            ""outline"": ""Short"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure"",
            ""keyMessages"": [""Message""],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(invalidJson);

        var result = await _service.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            LlmInvoker,
            new OrchestrationConfig(EnableAutoRepair: false)
        );

        Assert.False(result.Success);
        Assert.Equal(1, result.AttemptsUsed);
        Assert.NotEmpty(result.ValidationErrors);
    }

    [Fact]
    public async Task ExecuteStepAsync_MaxRetriesExceeded_ReturnsFailure()
    {
        var invalidJson = @"{
            ""outline"": ""Short"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure"",
            ""keyMessages"": [""Message""],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(invalidJson);

        var result = await _service.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            LlmInvoker,
            new OrchestrationConfig(MaxRetries: 2, EnableAutoRepair: true)
        );

        Assert.False(result.Success);
        Assert.Equal(2, result.AttemptsUsed);
        Assert.NotEmpty(result.ValidationErrors);
    }

    [Fact]
    public async Task ExecuteStepAsync_EmptyOutput_RetriesWithAutoRepair()
    {
        var attempt = 0;
        var validJson = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure with clear beginning, middle, and end"",
            ""keyMessages"": [""Message 1""],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct)
        {
            attempt++;
            return attempt == 1 ? Task.FromResult(string.Empty) : Task.FromResult(validJson);
        }

        var result = await _service.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            LlmInvoker,
            new OrchestrationConfig(MaxRetries: 3, EnableAutoRepair: true)
        );

        Assert.True(result.Success);
        Assert.Equal(2, result.AttemptsUsed);
    }

    [Fact]
    public async Task ExecuteStepAsync_Timeout_RetriesOnTimeout()
    {
        var attempt = 0;
        var validJson = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure with clear beginning, middle, and end"",
            ""keyMessages"": [""Message 1""],
            ""schema_version"": ""1.0""
        }";

        async Task<string> LlmInvoker(string? prompt, CancellationToken ct)
        {
            attempt++;
            if (attempt == 1)
            {
                await Task.Delay(200, ct);
                throw new OperationCanceledException();
            }
            return validJson;
        }

        var result = await _service.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            LlmInvoker,
            new OrchestrationConfig(MaxRetries: 3, Timeout: TimeSpan.FromMilliseconds(100))
        );

        Assert.True(result.Success);
        Assert.Equal(2, result.AttemptsUsed);
    }

    [Fact]
    public async Task ExecuteStepRawAsync_ValidOutput_ReturnsOutput()
    {
        var expectedOutput = "test output";
        
        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(expectedOutput);

        var result = await _service.ExecuteStepRawAsync(
            "custom_step",
            LlmInvoker
        );

        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public async Task ExecuteStepRawAsync_EmptyOutput_ThrowsAfterRetries()
    {
        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.ExecuteStepRawAsync(
                "custom_step",
                LlmInvoker,
                new OrchestrationConfig(MaxRetries: 2)
            );
        });
    }

    [Fact]
    public async Task ExecuteStepAsync_WithSceneBreakdown_ValidatesCorrectly()
    {
        var validJson = @"{
            ""scenes"": [
                {
                    ""index"": 0,
                    ""heading"": ""Introduction"",
                    ""script"": ""Welcome to this comprehensive video"",
                    ""durationSeconds"": 10.0,
                    ""purpose"": ""Hook the viewer and introduce the topic"",
                    ""transitionType"": ""fade""
                }
            ],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(validJson);

        var result = await _service.ExecuteStepAsync<SceneBreakdownSchema>(
            "plan_to_scenes",
            LlmInvoker
        );

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Scenes);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithVoiceStyle_ValidatesCorrectly()
    {
        var validJson = @"{
            ""voiceCharacteristics"": {
                ""rate"": 1.0,
                ""pitch"": 1.0,
                ""volume"": 0.8
            },
            ""pacingGuidelines"": {
                ""defaultPauseMs"": 300,
                ""sentencePauseMs"": 500,
                ""paragraphPauseMs"": 1000
            },
            ""emotionalTone"": ""enthusiastic"",
            ""emphasis"": [],
            ""schema_version"": ""1.0""
        }";

        Task<string> LlmInvoker(string? prompt, CancellationToken ct) => Task.FromResult(validJson);

        var result = await _service.ExecuteStepAsync<VoiceStyleSchema>(
            "scenes_to_voice",
            LlmInvoker
        );

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("enthusiastic", result.Data.EmotionalTone);
    }

    [Fact]
    public void GetStepInfo_NonExistentStep_ReturnsNull()
    {
        var step = _service.GetStepInfo("nonexistent_step");
        
        Assert.Null(step);
    }

    [Fact]
    public void GetAllSteps_ReturnsAllRegisteredSteps()
    {
        var steps = _service.GetAllSteps();
        
        Assert.NotNull(steps);
        Assert.True(steps.Count >= 6);
    }
}
