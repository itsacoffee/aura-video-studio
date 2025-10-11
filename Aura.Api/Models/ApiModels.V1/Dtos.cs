using ApiV1 = Aura.Api.Models.ApiModels.V1;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// V1 API DTOs - request and response models for API endpoints
/// </summary>

public record PlanRequest(
    double TargetDurationMinutes, 
    ApiV1.Pacing Pacing, 
    ApiV1.Density Density, 
    string Style);

public record ScriptRequest(
    string Topic, 
    string Audience, 
    string Goal, 
    string Tone, 
    string Language, 
    ApiV1.Aspect Aspect, 
    double TargetDurationMinutes, 
    ApiV1.Pacing Pacing, 
    ApiV1.Density Density, 
    string Style, 
    string? ProviderTier,
    ProviderSelectionDto? ProviderSelection);

public record TtsRequest(
    List<LineDto> Lines, 
    string VoiceName, 
    double Rate, 
    double Pitch, 
    ApiV1.PauseStyle PauseStyle);

public record LineDto(
    int SceneIndex, 
    string Text, 
    double StartSeconds, 
    double DurationSeconds);

public record ComposeRequest(string TimelineJson);

public record RenderRequest(string TimelineJson, string PresetName, RenderSettingsDto? Settings);

public record RenderSettingsDto(
    int Width, 
    int Height, 
    int Fps, 
    string Codec, 
    string Container, 
    int QualityLevel, 
    int VideoBitrateK, 
    int AudioBitrateK, 
    bool EnableSceneCut);

public record RenderJobDto(
    string Id, 
    string Status, 
    float Progress, 
    string? OutputPath, 
    DateTime CreatedAt,
    int? EstimatedTimeRemaining = null,
    string? Error = null);

public record ApplyProfileRequest(string ProfileName);

public record ApiKeysRequest(
    string? OpenAiKey, 
    string? ElevenLabsKey, 
    string? PexelsKey, 
    string? StabilityAiKey);

public record ProviderPathsRequest(
    string? StableDiffusionUrl, 
    string? OllamaUrl, 
    string? FfmpegPath, 
    string? FfprobePath, 
    string? OutputDirectory);

public record ProviderTestRequest(string? Url, string? Path);

public record CaptionsRequest(
    List<LineDto> Lines, 
    string Format = "SRT", 
    string? OutputPath = null);

public record ValidateProvidersRequest(string[]? Providers);

public record RecommendationsRequestDto(
    string Topic,
    string? Audience,
    string? Goal,
    string? Tone,
    string? Language,
    ApiV1.Aspect? Aspect,
    double TargetDurationMinutes,
    ApiV1.Pacing? Pacing,
    ApiV1.Density? Density,
    string? Style,
    string? AudiencePersona,
    ConstraintsDto? Constraints);

public record ConstraintsDto(
    int? MaxSceneCount,
    int? MinSceneCount,
    double? MaxBRollPercentage,
    int? MaxReadingLevel);

public record AssetSearchRequest(
    string Provider, 
    string Query, 
    int Count, 
    string? ApiKey = null, 
    string? LocalDirectory = null);

public record AssetGenerateRequest(
    string Prompt, 
    int? SceneIndex = null,
    string? Model = null, 
    int? Steps = null, 
    double? CfgScale = null, 
    int? Seed = null, 
    int? Width = null, 
    int? Height = null, 
    string? Style = null, 
    string? SamplerName = null, 
    ApiV1.Aspect? Aspect = null,
    string[]? Keywords = null,
    string? StableDiffusionUrl = null,
    bool BypassHardwareChecks = false);
