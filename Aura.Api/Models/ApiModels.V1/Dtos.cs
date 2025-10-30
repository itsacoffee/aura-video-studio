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
    ProviderSelectionDto? ProviderSelection,
    PromptModifiersDto? PromptModifiers = null,
    ScriptRefinementConfigDto? RefinementConfig = null);

/// <summary>
/// User customization options for prompt engineering
/// </summary>
public record PromptModifiersDto(
    string? AdditionalInstructions = null,
    string? ExampleStyle = null,
    bool EnableChainOfThought = false,
    string? PromptVersion = null);

/// <summary>
/// Configuration for multi-stage script refinement pipeline
/// </summary>
public record ScriptRefinementConfigDto(
    int MaxRefinementPasses = 2,
    double QualityThreshold = 85.0,
    double MinimumImprovement = 5.0,
    bool EnableAdvisorValidation = true,
    int PassTimeoutMinutes = 2);

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
    string? PixabayKey,
    string? UnsplashKey,
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

// Azure TTS DTOs
public record AzureTtsSynthesizeRequest(
    string Text,
    string VoiceId,
    AzureTtsOptionsDto? Options = null);

public record AzureTtsOptionsDto(
    double? Rate = null,
    double? Pitch = null,
    double? Volume = null,
    string? Style = null,
    double? StyleDegree = null,
    string? Role = null,
    string? AudioEffect = null,
    string? Emphasis = null);

public record AzureVoiceDto(
    string Id,
    string Name,
    string Locale,
    string Gender,
    string VoiceType,
    string[] AvailableStyles,
    string[] AvailableRoles,
    string[] SupportedFeatures,
    string? Description,
    string? LocalName);

public record AzureVoiceCapabilitiesDto(
    string VoiceId,
    string Name,
    string Locale,
    string Gender,
    string VoiceType,
    string[] AvailableStyles,
    string[] AvailableRoles,
    Dictionary<string, string> StyleDescriptions,
    string[] SupportedFeatures);

/// <summary>
/// Request to train the frame importance ML model with user annotations
/// </summary>
public record TrainFrameImportanceRequest(
    List<FrameAnnotationDto> Annotations);

/// <summary>
/// Frame annotation data for ML model training
/// </summary>
public record FrameAnnotationDto(
    string FramePath,
    double Rating);

/// <summary>
/// Response from model training endpoint
/// </summary>
public record TrainFrameImportanceResponse(
    bool Success,
    string? ModelPath,
    int TrainingSamples,
    double TrainingDurationSeconds,
    string? ErrorMessage);

/// <summary>
/// Response for stock provider status
/// </summary>
public record StockProviderDto(
    string Name,
    bool Available,
    bool HasApiKey,
    int? QuotaRemaining,
    int? QuotaLimit,
    string? Error);

/// <summary>
/// Response for listing all stock providers
/// </summary>
public record StockProvidersResponse(
    List<StockProviderDto> Providers);

/// <summary>
/// Response for checking quota status
/// </summary>
public record QuotaStatusResponse(
    string Provider,
    int Remaining,
    int Limit,
    DateTime? ResetTime);

/// <summary>
/// Request for prompt preview with variable substitutions
/// </summary>
public record PromptPreviewRequest(
    string Topic,
    string? Audience,
    string? Goal,
    string Tone,
    string Language,
    ApiV1.Aspect Aspect,
    double TargetDurationMinutes,
    ApiV1.Pacing Pacing,
    ApiV1.Density Density,
    string Style,
    PromptModifiersDto? PromptModifiers = null);

/// <summary>
/// Response containing prompt preview with substitutions
/// </summary>
public record PromptPreviewResponse(
    string SystemPrompt,
    string UserPrompt,
    string FinalPrompt,
    Dictionary<string, string> SubstitutedVariables,
    string PromptVersion,
    int EstimatedTokens);

/// <summary>
/// Few-shot example for a specific video type
/// </summary>
public record FewShotExampleDto(
    string VideoType,
    string ExampleName,
    string Description,
    string SampleBrief,
    string SampleOutput,
    string[] KeyTechniques);

/// <summary>
/// Response containing list of available few-shot examples
/// </summary>
public record ListExamplesResponse(
    List<FewShotExampleDto> Examples,
    List<string> VideoTypes);

/// <summary>
/// Prompt version information
/// </summary>
public record PromptVersionDto(
    string Version,
    string Name,
    string Description,
    bool IsDefault);

/// <summary>
/// Response containing available prompt versions
/// </summary>
public record ListPromptVersionsResponse(
    List<PromptVersionDto> Versions,
    string DefaultVersion);

/// <summary>
/// Chain-of-thought stage result
/// </summary>
public record ChainOfThoughtStageDto(
    string Stage,
    string Content,
    bool RequiresUserReview,
    string? SuggestedEdits);

/// <summary>
/// Request to execute a chain-of-thought stage
/// </summary>
public record ExecuteChainOfThoughtStageRequest(
    string Stage,
    string Topic,
    string? Audience,
    string? Goal,
    string Tone,
    string Language,
    ApiV1.Aspect Aspect,
    double TargetDurationMinutes,
    ApiV1.Pacing Pacing,
    ApiV1.Density Density,
    string Style,
    string? PreviousStageContent = null,
    PromptModifiersDto? PromptModifiers = null);

/// <summary>
/// Script quality metrics for a single iteration
/// </summary>
public record ScriptQualityMetricsDto(
    double NarrativeCoherence,
    double PacingAppropriateness,
    double AudienceAlignment,
    double VisualClarity,
    double EngagementPotential,
    double OverallScore,
    int Iteration,
    DateTime AssessedAt,
    List<string>? Issues = null,
    List<string>? Suggestions = null,
    List<string>? Strengths = null);

/// <summary>
/// Result of multi-stage script refinement
/// </summary>
public record ScriptRefinementResultDto(
    string FinalScript,
    List<ScriptQualityMetricsDto> IterationMetrics,
    int TotalPasses,
    string StopReason,
    double TotalDurationSeconds,
    bool Success,
    string? ErrorMessage = null);

