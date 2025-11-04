using System;
using System.Text.Json;
using Aura.Core.AI.Orchestration;
using Aura.Core.AI.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class OrchestrationSchemaTests
{
    private readonly SchemaValidator _validator;

    public OrchestrationSchemaTests()
    {
        var loggerMock = new Mock<ILogger<SchemaValidator>>();
        _validator = new SchemaValidator(loggerMock.Object);
    }

    [Fact]
    public void PlanSchema_ValidData_PassesValidation()
    {
        var json = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure with clear beginning, middle, and end"",
            ""keyMessages"": [""Message 1"", ""Message 2"", ""Message 3""],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<PlanSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Equal(5, data.SceneCount);
        Assert.Equal("moderate", data.TargetPacing);
    }

    [Fact]
    public void PlanSchema_InvalidSceneCount_FailsValidation()
    {
        var json = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 100,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure"",
            ""keyMessages"": [""Message 1""],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<PlanSchema>(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("SceneCount"));
    }

    [Fact]
    public void PlanSchema_InvalidPacing_FailsValidation()
    {
        var json = @"{
            ""outline"": ""This is a comprehensive outline for the video that exceeds fifty characters"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""invalid"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure"",
            ""keyMessages"": [""Message 1""],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<PlanSchema>(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("TargetPacing"));
    }

    [Fact]
    public void SceneBreakdownSchema_ValidData_PassesValidation()
    {
        var json = @"{
            ""scenes"": [
                {
                    ""index"": 0,
                    ""heading"": ""Introduction"",
                    ""script"": ""Welcome to this video about amazing topics"",
                    ""durationSeconds"": 10.0,
                    ""purpose"": ""Hook the viewer and introduce the topic"",
                    ""transitionType"": ""fade""
                },
                {
                    ""index"": 1,
                    ""heading"": ""Main Point"",
                    ""script"": ""Here is the main point we want to discuss"",
                    ""durationSeconds"": 15.0,
                    ""purpose"": ""Explain the core concept clearly""
                }
            ],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<SceneBreakdownSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Equal(2, data.Scenes.Length);
    }

    [Fact]
    public void SceneBreakdownSchema_EmptyScenes_FailsValidation()
    {
        var json = @"{
            ""scenes"": [],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<SceneBreakdownSchema>(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("at least one scene"));
    }

    [Fact]
    public void VoiceStyleSchema_ValidData_PassesValidation()
    {
        var json = @"{
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
            ""emphasis"": [
                { ""text"": ""important"", ""strength"": ""strong"" }
            ],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<VoiceStyleSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Equal(1.0, data.VoiceCharacteristics.Rate);
    }

    [Fact]
    public void VoiceStyleSchema_InvalidRate_FailsValidation()
    {
        var json = @"{
            ""voiceCharacteristics"": {
                ""rate"": 3.0,
                ""pitch"": 1.0,
                ""volume"": 0.8
            },
            ""pacingGuidelines"": {
                ""defaultPauseMs"": 300,
                ""sentencePauseMs"": 500,
                ""paragraphPauseMs"": 1000
            },
            ""emotionalTone"": ""neutral"",
            ""emphasis"": [],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<VoiceStyleSchema>(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Rate"));
    }

    [Fact]
    public void SSMLSpecSchema_ValidData_PassesValidation()
    {
        var json = @"{
            ""ssmlSegments"": [
                {
                    ""sceneIndex"": 0,
                    ""text"": ""Hello world"",
                    ""ssml"": ""<speak>Hello world</speak>"",
                    ""estimatedDurationMs"": 2000
                }
            ],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<SSMLSpecSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Single(data.SSMLSegments);
    }

    [Fact]
    public void VisualPromptSpecSchema_ValidData_PassesValidation()
    {
        var json = @"{
            ""visualPrompts"": [
                {
                    ""sceneIndex"": 0,
                    ""prompt"": ""A beautiful landscape with mountains and a sunset"",
                    ""styleKeywords"": [""cinematic"", ""dramatic"", ""vibrant""],
                    ""aspectRatio"": ""16:9""
                }
            ],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<VisualPromptSpecSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Single(data.VisualPrompts);
    }

    [Fact]
    public void VisualPromptSpecSchema_ShortPrompt_FailsValidation()
    {
        var json = @"{
            ""visualPrompts"": [
                {
                    ""sceneIndex"": 0,
                    ""prompt"": ""Short"",
                    ""styleKeywords"": [""cinematic""]
                }
            ],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<VisualPromptSpecSchema>(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("at least 20 characters"));
    }

    [Fact]
    public void RenderTimelineSchema_ValidData_PassesValidation()
    {
        var json = @"{
            ""timelineSegments"": [
                {
                    ""sceneIndex"": 0,
                    ""startTimeSeconds"": 0.0,
                    ""durationSeconds"": 10.0,
                    ""audioPath"": ""/path/to/audio.wav"",
                    ""visualPath"": ""/path/to/visual.png""
                }
            ],
            ""totalDurationSeconds"": 10.0,
            ""transitionPlan"": [],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<RenderTimelineSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Single(data.TimelineSegments);
    }

    [Fact]
    public void RenderTimelineSchema_WithTransitions_PassesValidation()
    {
        var json = @"{
            ""timelineSegments"": [
                {
                    ""sceneIndex"": 0,
                    ""startTimeSeconds"": 0.0,
                    ""durationSeconds"": 10.0,
                    ""audioPath"": ""/path/to/audio1.wav"",
                    ""visualPath"": ""/path/to/visual1.png""
                },
                {
                    ""sceneIndex"": 1,
                    ""startTimeSeconds"": 10.0,
                    ""durationSeconds"": 10.0,
                    ""audioPath"": ""/path/to/audio2.wav"",
                    ""visualPath"": ""/path/to/visual2.png""
                }
            ],
            ""totalDurationSeconds"": 20.0,
            ""transitionPlan"": [
                {
                    ""fromSceneIndex"": 0,
                    ""toSceneIndex"": 1,
                    ""transitionType"": ""fade"",
                    ""durationMs"": 500
                }
            ],
            ""schema_version"": ""1.0""
        }";

        var (result, data) = _validator.ValidateAndDeserialize<RenderTimelineSchema>(json);

        Assert.True(result.IsValid, string.Join("; ", result.ValidationErrors));
        Assert.NotNull(data);
        Assert.Equal(2, data.TimelineSegments.Length);
        Assert.Single(data.TransitionPlan);
    }

    [Fact]
    public void AllSchemas_HaveSchemaVersion()
    {
        var planSchema = new PlanSchema();
        var sceneSchema = new SceneBreakdownSchema();
        var voiceSchema = new VoiceStyleSchema();
        var ssmlSchema = new SSMLSpecSchema();
        var visualSchema = new VisualPromptSpecSchema();
        var timelineSchema = new RenderTimelineSchema();

        Assert.Equal("1.0", planSchema.SchemaVersion);
        Assert.Equal("1.0", sceneSchema.SchemaVersion);
        Assert.Equal("1.0", voiceSchema.SchemaVersion);
        Assert.Equal("1.0", ssmlSchema.SchemaVersion);
        Assert.Equal("1.0", visualSchema.SchemaVersion);
        Assert.Equal("1.0", timelineSchema.SchemaVersion);
    }

    [Fact]
    public void ArtifactMetadata_SerializesCorrectly()
    {
        var metadata = new ArtifactMetadata(
            "OpenAI",
            "gpt-4",
            0.7,
            DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(metadata);
        var deserialized = JsonSerializer.Deserialize<ArtifactMetadata>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("OpenAI", deserialized.Provider);
        Assert.Equal("gpt-4", deserialized.Model);
        Assert.Equal(0.7, deserialized.Temperature);
    }
}
