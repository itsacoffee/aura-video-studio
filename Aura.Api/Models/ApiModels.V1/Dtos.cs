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
    ScriptRefinementConfigDto? RefinementConfig = null,
    string? AudienceProfileId = null);

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

/// <summary>
/// Request to get provider recommendations for an operation type
/// </summary>
public record ProviderRecommendationRequest(
    string OperationType,
    int EstimatedInputTokens = 1000);

/// <summary>
/// Recommendation for which LLM provider to use
/// </summary>
public record ProviderRecommendationDto(
    string ProviderName,
    string Reasoning,
    int QualityScore,
    decimal EstimatedCost,
    double ExpectedLatencySeconds,
    bool IsAvailable,
    string HealthStatus,
    int Confidence);

/// <summary>
/// Health metrics for a provider
/// </summary>
public record ProviderHealthDto(
    string ProviderName,
    double SuccessRatePercent,
    double AverageLatencySeconds,
    int TotalRequests,
    int ConsecutiveFailures,
    string Status);

/// <summary>
/// User preferences for provider selection
/// </summary>
public record ProviderPreferencesDto(
    // Master toggle and assistance level
    bool EnableRecommendations,
    string AssistanceLevel,
    
    // Feature toggles (all OFF by default)
    bool EnableHealthMonitoring,
    bool EnableCostTracking,
    bool EnableLearning,
    bool EnableProfiles,
    bool EnableAutoFallback,
    
    // Manual configuration (always available)
    string? GlobalDefault,
    bool AlwaysUseDefault,
    Dictionary<string, string>? PerOperationOverrides,
    string ActiveProfile,
    List<string>? ExcludedProviders,
    string? PinnedProvider,
    
    // Fallback and budget settings
    Dictionary<string, List<string>>? FallbackChains,
    decimal? MonthlyBudgetLimit,
    Dictionary<string, decimal>? PerProviderBudgetLimits,
    bool HardBudgetLimit);

/// <summary>
/// Cost estimate for an LLM operation
/// </summary>
public record CostEstimateDto(
    string ProviderName,
    string OperationType,
    int EstimatedInputTokens,
    int EstimatedOutputTokens,
    int TotalTokens,
    decimal EstimatedCostUsd,
    decimal CostPer1KTokens);

/// <summary>
/// Budget check result
/// </summary>
public record BudgetCheckDto(
    bool IsWithinBudget,
    bool ShouldBlock,
    List<string> Warnings,
    decimal CurrentMonthlyCost,
    decimal EstimatedNewTotal);

/// <summary>
/// Cost tracking summary for current month
/// </summary>
public record CostTrackingSummaryDto(
    decimal TotalMonthlyCost,
    Dictionary<string, decimal> CostByProvider,
    Dictionary<string, decimal> CostByOperation);

/// <summary>
/// Request to test provider connection
/// </summary>
public record TestProviderConnectionRequest(
    string ProviderName,
    string? ApiKey = null);

// ============================================================================
// AUDIENCE PROFILE DTOs
// ============================================================================

/// <summary>
/// Request to create or update an audience profile
/// </summary>
public record AudienceProfileDto(
    string? Id,
    string Name,
    string? Description,
    AgeRangeDto? AgeRange,
    string? EducationLevel,
    string? Profession,
    string? Industry,
    string? ExpertiseLevel,
    string? IncomeBracket,
    string? GeographicRegion,
    LanguageFluencyDto? LanguageFluency,
    List<string>? Interests,
    List<string>? PainPoints,
    List<string>? Motivations,
    CulturalBackgroundDto? CulturalBackground,
    string? PreferredLearningStyle,
    AttentionSpanDto? AttentionSpan,
    string? TechnicalComfort,
    AccessibilityNeedsDto? AccessibilityNeeds,
    bool IsTemplate,
    List<string>? Tags,
    int Version,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    bool IsFavorite = false,
    string? FolderPath = null,
    int UsageCount = 0,
    DateTime? LastUsedAt = null);

/// <summary>
/// Age range specification
/// </summary>
public record AgeRangeDto(
    int MinAge,
    int MaxAge,
    string DisplayName,
    string ContentRating);

/// <summary>
/// Language fluency specification
/// </summary>
public record LanguageFluencyDto(
    string Language,
    string Level);

/// <summary>
/// Cultural background and sensitivities
/// </summary>
public record CulturalBackgroundDto(
    List<string>? Sensitivities,
    List<string>? TabooTopics,
    string PreferredCommunicationStyle);

/// <summary>
/// Attention span preferences
/// </summary>
public record AttentionSpanDto(
    double PreferredDurationMinutes,
    string DisplayName);

/// <summary>
/// Accessibility requirements
/// </summary>
public record AccessibilityNeedsDto(
    bool RequiresCaptions,
    bool RequiresAudioDescriptions,
    bool RequiresHighContrast,
    bool RequiresSimplifiedLanguage,
    bool RequiresLargeText);

/// <summary>
/// Request to create a new audience profile
/// </summary>
public record CreateAudienceProfileRequest(
    AudienceProfileDto Profile);

/// <summary>
/// Request to update an existing audience profile
/// </summary>
public record UpdateAudienceProfileRequest(
    AudienceProfileDto Profile);

/// <summary>
/// Response containing an audience profile
/// </summary>
public record AudienceProfileResponse(
    AudienceProfileDto Profile,
    ValidationResultDto? Validation);

/// <summary>
/// Response containing a list of audience profiles
/// </summary>
public record AudienceProfileListResponse(
    List<AudienceProfileDto> Profiles,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// Validation result for audience profile
/// </summary>
public record ValidationResultDto(
    bool IsValid,
    List<ValidationIssueDto> Errors,
    List<ValidationIssueDto> Warnings,
    List<ValidationIssueDto> Infos);

/// <summary>
/// Individual validation issue
/// </summary>
public record ValidationIssueDto(
    string Severity,
    string Field,
    string Message,
    string? SuggestedFix);

/// <summary>
/// Request to analyze script and infer audience
/// </summary>
public record AnalyzeAudienceRequest(
    string ScriptText);

/// <summary>
/// Response from audience analysis
/// </summary>
public record AnalyzeAudienceResponse(
    AudienceProfileDto InferredProfile,
    double ConfidenceScore,
    List<string> ReasoningFactors);

/// <summary>
/// Request to move profile to a folder
/// </summary>
public record MoveToFolderRequest(
    string? FolderPath);

/// <summary>
/// Response containing list of folders
/// </summary>
public record FolderListResponse(
    List<string> Folders);

/// <summary>
/// Request to export profile to JSON
/// </summary>
public record ExportProfileResponse(
    string Json);

/// <summary>
/// Request to import profile from JSON
/// </summary>
public record ImportProfileRequest(
    string Json);

/// <summary>
/// Request to get profile recommendations based on topic and goal
/// </summary>
public record RecommendProfilesRequest(
    string Topic,
    string? Goal = null,
    int? MaxResults = 5);

/// <summary>
/// Request to adapt content for an audience
/// </summary>
public record AdaptContentRequest(
    string Content,
    string AudienceProfileId,
    ContentAdaptationConfigDto? Config = null);

/// <summary>
/// Configuration for content adaptation
/// </summary>
public record ContentAdaptationConfigDto(
    double AggressivenessLevel = 0.6,
    bool EnableVocabularyAdjustment = true,
    bool EnableExamplePersonalization = true,
    bool EnablePacingAdaptation = true,
    bool EnableToneOptimization = true,
    bool EnableCognitiveLoadBalancing = true,
    double CognitiveLoadThreshold = 75.0,
    int ExamplesPerConcept = 3);

/// <summary>
/// Result of content adaptation
/// </summary>
public record ContentAdaptationResultDto(
    string OriginalContent,
    string AdaptedContent,
    ReadabilityMetricsDto OriginalMetrics,
    ReadabilityMetricsDto AdaptedMetrics,
    List<AdaptationChangeDto> Changes,
    double OverallRelevanceScore,
    double ProcessingTimeSeconds);

/// <summary>
/// Readability metrics
/// </summary>
public record ReadabilityMetricsDto(
    double FleschKincaidGradeLevel,
    double SmogScore,
    double AverageWordsPerSentence,
    double AverageSyllablesPerWord,
    double ComplexWordPercentage,
    double TechnicalTermDensity,
    double OverallComplexity);

/// <summary>
/// Individual adaptation change
/// </summary>
public record AdaptationChangeDto(
    string Category,
    string Description,
    string OriginalText,
    string AdaptedText,
    string Reasoning,
    int Position);

/// <summary>
/// Request for adaptation preview/comparison
/// </summary>
public record AdaptationPreviewRequest(
    string Content,
    string AudienceProfileId,
    ContentAdaptationConfigDto? Config = null);

/// <summary>
/// Adaptation comparison report
/// </summary>
public record AdaptationComparisonReportDto(
    string OriginalContent,
    string AdaptedContent,
    double ProcessingTimeSeconds,
    double OverallRelevanceScore,
    List<ComparisonSectionDto> Sections,
    MetricsComparisonDto MetricsComparison,
    Dictionary<string, int> ChangesByCategory,
    string Summary);

/// <summary>
/// Comparison section
/// </summary>
public record ComparisonSectionDto(
    string OriginalText,
    string AdaptedText,
    List<AdaptationChangeDto> Changes,
    List<TextHighlightDto> HighlightedDifferences);

/// <summary>
/// Text highlight
/// </summary>
public record TextHighlightDto(
    string OriginalText,
    string AdaptedText,
    int Position,
    string Type);

/// <summary>
/// Metrics comparison
/// </summary>
public record MetricsComparisonDto(
    MetricChangeDto FleschKincaidChange,
    MetricChangeDto SmogChange,
    MetricChangeDto ComplexityChange,
    MetricChangeDto WordsPerSentenceChange);

/// <summary>
/// Individual metric change
/// </summary>
public record MetricChangeDto(
    string Name,
    double OriginalValue,
    double AdaptedValue,
    string Direction,
    double PercentageChange);

/// <summary>
/// Reading level response
/// </summary>
public record ReadingLevelResponse(
    string ProfileId,
    string ProfileName,
    string ReadingLevelDescription);

// Document Import and Conversion DTOs

/// <summary>
/// Response after importing a document
/// </summary>
public record DocumentImportResponse(
    bool Success,
    DocumentMetadataDto? Metadata,
    DocumentStructureDto? Structure,
    InferredAudienceDto? InferredAudience,
    List<string> Warnings,
    string? ErrorMessage,
    double ProcessingTimeSeconds);

/// <summary>
/// Document metadata
/// </summary>
public record DocumentMetadataDto(
    string OriginalFileName,
    string Format,
    long FileSizeBytes,
    DateTime ImportedAt,
    int WordCount,
    int CharacterCount,
    string? DetectedLanguage,
    string? Title,
    string? Author);

/// <summary>
/// Document structure information
/// </summary>
public record DocumentStructureDto(
    List<DocumentSectionDto> Sections,
    int HeadingLevels,
    List<string> KeyConcepts,
    DocumentComplexityDto Complexity,
    DocumentToneDto Tone);

/// <summary>
/// A section within a document
/// </summary>
public record DocumentSectionDto(
    int Level,
    string Heading,
    string Content,
    int WordCount,
    double EstimatedSpeechDurationSeconds);

/// <summary>
/// Document complexity metrics
/// </summary>
public record DocumentComplexityDto(
    double ReadingLevel,
    double TechnicalDensity,
    double AbstractionLevel,
    int AverageSentenceLength,
    int ComplexWordCount,
    string ComplexityDescription);

/// <summary>
/// Detected tone of document
/// </summary>
public record DocumentToneDto(
    string PrimaryTone,
    double FormalityLevel,
    string WritingStyle);

/// <summary>
/// Inferred audience characteristics
/// </summary>
public record InferredAudienceDto(
    string EducationLevel,
    string ExpertiseLevel,
    List<string> PossibleProfessions,
    string AgeRange,
    double ConfidenceScore,
    string Reasoning);

/// <summary>
/// Request to convert document to script
/// </summary>
public record ConvertDocumentRequest(
    DocumentImportResponse ImportResult,
    ConversionConfigDto Config);

/// <summary>
/// Configuration for document conversion
/// </summary>
public record ConversionConfigDto(
    string Preset,
    double TargetDurationMinutes,
    int WordsPerMinute,
    bool EnableAudienceRetargeting,
    bool EnableVisualSuggestions,
    bool PreserveOriginalStructure,
    bool AddTransitions,
    double AggressivenessLevel,
    string? TargetAudienceProfileId);

/// <summary>
/// Result of document to script conversion
/// </summary>
public record ConversionResultDto(
    bool Success,
    List<SceneDto> Scenes,
    BriefDto SuggestedBrief,
    List<ConversionChangeDto> Changes,
    ConversionMetricsDto Metrics,
    List<SectionConversionDto> SectionConversions,
    string? ErrorMessage,
    double ProcessingTimeSeconds);

/// <summary>
/// Scene information
/// </summary>
public record SceneDto(
    int Index,
    string Heading,
    string Script,
    double StartSeconds,
    double DurationSeconds);

/// <summary>
/// Brief configuration
/// </summary>
public record BriefDto(
    string Topic,
    string? Audience,
    string? Goal,
    string Tone,
    string Language,
    string Aspect);

/// <summary>
/// A change made during conversion
/// </summary>
public record ConversionChangeDto(
    string Category,
    string Description,
    string Justification,
    int SectionIndex,
    double ImpactLevel);

/// <summary>
/// Metrics about the conversion
/// </summary>
public record ConversionMetricsDto(
    int OriginalWordCount,
    int ConvertedWordCount,
    double CompressionRatio,
    int SectionsCreated,
    int TransitionsAdded,
    int VisualSuggestionsGenerated,
    double OverallConfidenceScore);

/// <summary>
/// Conversion details for a single section
/// </summary>
public record SectionConversionDto(
    int SectionIndex,
    string OriginalHeading,
    string ConvertedHeading,
    string OriginalContent,
    string ConvertedContent,
    double ConfidenceScore,
    bool RequiresManualReview,
    List<string> ChangeHighlights,
    string Reasoning);

/// <summary>
/// Preset definition
/// </summary>
public record PresetDefinitionDto(
    string Type,
    string Name,
    string Description,
    ConversionConfigDto DefaultConfig,
    List<string> BestForFormats,
    string RestructuringStrategy);

/// <summary>
/// Translation request
/// </summary>
public record TranslateScriptRequest(
    string SourceLanguage,
    string TargetLanguage,
    string? SourceText,
    List<ScriptLineDto>? ScriptLines,
    CulturalContextDto? CulturalContext,
    TranslationOptionsDto? Options,
    Dictionary<string, string>? Glossary,
    string? AudienceProfileId);

/// <summary>
/// Script line for translation
/// </summary>
public record ScriptLineDto(
    string Text,
    double StartSeconds,
    double DurationSeconds);

/// <summary>
/// Cultural context for translation
/// </summary>
public record CulturalContextDto(
    string TargetRegion,
    string TargetFormality,
    string PreferredStyle,
    List<string> Sensitivities,
    List<string> TabooTopics,
    string ContentRating);

/// <summary>
/// Translation options
/// </summary>
public record TranslationOptionsDto(
    string Mode = "Localized",
    bool EnableBackTranslation = true,
    bool EnableQualityScoring = true,
    bool AdjustTimings = true,
    double MaxTimingVariance = 0.15,
    bool PreserveNames = true,
    bool PreserveBrands = true,
    bool AdaptMeasurements = true);

/// <summary>
/// Translation result
/// </summary>
public record TranslationResultDto(
    string SourceLanguage,
    string TargetLanguage,
    string SourceText,
    string TranslatedText,
    List<TranslatedScriptLineDto> TranslatedLines,
    TranslationQualityDto Quality,
    List<CulturalAdaptationDto> CulturalAdaptations,
    TimingAdjustmentDto TimingAdjustment,
    List<VisualLocalizationRecommendationDto> VisualRecommendations,
    double TranslationTimeSeconds);

/// <summary>
/// Translated script line
/// </summary>
public record TranslatedScriptLineDto(
    int SceneIndex,
    string SourceText,
    string TranslatedText,
    double OriginalStartSeconds,
    double OriginalDurationSeconds,
    double AdjustedStartSeconds,
    double AdjustedDurationSeconds,
    double TimingVariance,
    List<string> AdaptationNotes);

/// <summary>
/// Translation quality metrics
/// </summary>
public record TranslationQualityDto(
    double OverallScore,
    double FluencyScore,
    double AccuracyScore,
    double CulturalAppropriatenessScore,
    double TerminologyConsistencyScore,
    double BackTranslationScore,
    string? BackTranslatedText,
    List<QualityIssueDto> Issues);

/// <summary>
/// Quality issue
/// </summary>
public record QualityIssueDto(
    string Severity,
    string Category,
    string Description,
    string? Suggestion,
    int? LineNumber);

/// <summary>
/// Cultural adaptation
/// </summary>
public record CulturalAdaptationDto(
    string Category,
    string SourcePhrase,
    string AdaptedPhrase,
    string Reasoning,
    int? LineNumber);

/// <summary>
/// Timing adjustment information
/// </summary>
public record TimingAdjustmentDto(
    double OriginalTotalDuration,
    double AdjustedTotalDuration,
    double ExpansionFactor,
    bool RequiresCompression,
    List<string> CompressionSuggestions,
    List<TimingWarningDto> Warnings);

/// <summary>
/// Timing warning
/// </summary>
public record TimingWarningDto(
    string Severity,
    string Message,
    int? LineNumber);

/// <summary>
/// Visual localization recommendation
/// </summary>
public record VisualLocalizationRecommendationDto(
    string ElementType,
    string Description,
    string Recommendation,
    string Priority,
    int? SceneIndex);

/// <summary>
/// Batch translation request
/// </summary>
public record BatchTranslateRequest(
    string SourceLanguage,
    List<string> TargetLanguages,
    string? SourceText,
    List<ScriptLineDto>? ScriptLines,
    CulturalContextDto? CulturalContext,
    TranslationOptionsDto? Options,
    Dictionary<string, string>? Glossary);

/// <summary>
/// Batch translation result
/// </summary>
public record BatchTranslationResultDto(
    string SourceLanguage,
    Dictionary<string, TranslationResultDto> Translations,
    List<string> SuccessfulLanguages,
    List<string> FailedLanguages,
    double TotalTimeSeconds);

/// <summary>
/// Cultural analysis request
/// </summary>
public record CulturalAnalysisRequest(
    string TargetLanguage,
    string TargetRegion,
    string Content,
    string? AudienceProfileId);

/// <summary>
/// Cultural analysis result
/// </summary>
public record CulturalAnalysisResultDto(
    string TargetLanguage,
    string TargetRegion,
    double CulturalSensitivityScore,
    List<CulturalIssueDto> Issues,
    List<CulturalRecommendationDto> Recommendations);

/// <summary>
/// Cultural issue
/// </summary>
public record CulturalIssueDto(
    string Severity,
    string Category,
    string Issue,
    string Context,
    string? Suggestion);

/// <summary>
/// Cultural recommendation
/// </summary>
public record CulturalRecommendationDto(
    string Category,
    string Recommendation,
    string Reasoning,
    string Priority);

/// <summary>
/// Language info
/// </summary>
public record LanguageInfoDto(
    string Code,
    string Name,
    string NativeName,
    string Region,
    bool IsRightToLeft,
    string DefaultFormality,
    double TypicalExpansionFactor);

/// <summary>
/// Glossary entry
/// </summary>
public record GlossaryEntryDto(
    string Id,
    string Term,
    Dictionary<string, string> Translations,
    string? Context,
    string? Industry);

/// <summary>
/// Project glossary
/// </summary>
public record ProjectGlossaryDto(
    string Id,
    string Name,
    string? Description,
    List<GlossaryEntryDto> Entries,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Create glossary request
/// </summary>
public record CreateGlossaryRequest(
    string Name,
    string? Description);

/// <summary>
/// Add glossary entry request
/// </summary>
public record AddGlossaryEntryRequest(
    string Term,
    Dictionary<string, string> Translations,
    string? Context,
    string? Industry);

// User Preferences and Customization DTOs

/// <summary>
/// Custom audience profile DTO
/// </summary>
public record CustomAudienceProfileDto(
    string Id,
    string Name,
    string? BaseProfileId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsCustom,
    int MinAge,
    int MaxAge,
    string EducationLevel,
    string? EducationLevelDescription,
    List<string> CulturalSensitivities,
    List<string> TopicsToAvoid,
    List<string> TopicsToEmphasize,
    int VocabularyLevel,
    string SentenceStructurePreference,
    int ReadingLevel,
    int ViolenceThreshold,
    int ProfanityThreshold,
    int SexualContentThreshold,
    int ControversialTopicsThreshold,
    string HumorStyle,
    int SarcasmLevel,
    List<string> JokeTypes,
    List<string> CulturalHumorPreferences,
    int FormalityLevel,
    int AttentionSpanSeconds,
    string PacingPreference,
    int InformationDensity,
    int TechnicalDepthTolerance,
    int JargonAcceptability,
    List<string> FamiliarTechnicalTerms,
    string EmotionalTone,
    int EmotionalIntensity,
    int CtaAggressiveness,
    string CtaStyle,
    string? BrandVoiceGuidelines,
    List<string> BrandToneKeywords,
    string? BrandPersonality,
    string? Description,
    List<string> Tags,
    bool IsFavorite,
    int UsageCount,
    DateTime? LastUsedAt);

/// <summary>
/// Content filtering policy DTO
/// </summary>
public record ContentFilteringPolicyDto(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool FilteringEnabled,
    bool AllowOverrideAll,
    string ProfanityFilter,
    List<string> CustomBannedWords,
    List<string> CustomAllowedWords,
    int ViolenceThreshold,
    bool BlockGraphicContent,
    int SexualContentThreshold,
    bool BlockExplicitContent,
    List<string> BannedTopics,
    List<string> AllowedControversialTopics,
    string PoliticalContent,
    string? PoliticalContentGuidelines,
    string ReligiousContent,
    string? ReligiousContentGuidelines,
    string SubstanceReferences,
    bool BlockHateSpeech,
    List<string> HateSpeechExceptions,
    string CopyrightPolicy,
    List<string> BlockedConcepts,
    List<string> AllowedConcepts,
    List<string> BlockedPeople,
    List<string> AllowedPeople,
    List<string> BlockedBrands,
    List<string> AllowedBrands,
    string? Description,
    bool IsDefault,
    int UsageCount,
    DateTime? LastUsedAt);

/// <summary>
/// AI behavior settings DTO
/// </summary>
public record AIBehaviorSettingsDto(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    LLMStageParametersDto ScriptGeneration,
    LLMStageParametersDto SceneDescription,
    LLMStageParametersDto ContentOptimization,
    LLMStageParametersDto Translation,
    LLMStageParametersDto QualityAnalysis,
    double CreativityVsAdherence,
    bool EnableChainOfThought,
    bool ShowPromptsBeforeSending,
    string? Description,
    bool IsDefault,
    int UsageCount,
    DateTime? LastUsedAt);

/// <summary>
/// LLM stage parameters DTO
/// </summary>
public record LLMStageParametersDto(
    string StageName,
    double Temperature,
    double TopP,
    double FrequencyPenalty,
    double PresencePenalty,
    int MaxTokens,
    string? CustomSystemPrompt,
    string? PreferredModel,
    double StrictnessLevel);

/// <summary>
/// Custom prompt template DTO
/// </summary>
public record CustomPromptTemplateDto(
    string Id,
    string Name,
    string Stage,
    string TemplateText,
    List<string> Variables,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? VariantGroup,
    int SuccessCount,
    int TotalUses,
    double SuccessRate,
    string? Description,
    List<string> Tags,
    bool IsFavorite);

/// <summary>
/// Custom quality thresholds DTO
/// </summary>
public record CustomQualityThresholdsDto(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool SkipValidation,
    int MinScriptWordCount,
    int MaxScriptWordCount,
    int AcceptableGrammarErrors,
    List<string> RequiredKeywords,
    List<string> ExcludedKeywords,
    int MinImageResolutionWidth,
    int MinImageResolutionHeight,
    double MinImageClarityScore,
    bool AllowLowQualityImages,
    int MinAudioBitrate,
    double MinAudioClarity,
    double MaxBackgroundNoise,
    bool RequireStereo,
    double MinSubtitleAccuracy,
    bool RequireSubtitles,
    Dictionary<string, double> CustomMetricThresholds,
    string? Description,
    bool IsDefault,
    int UsageCount,
    DateTime? LastUsedAt);

/// <summary>
/// Custom visual style DTO
/// </summary>
public record CustomVisualStyleDto(
    string Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> ColorPalette,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    int VisualComplexity,
    string ArtisticStyle,
    string CompositionPreference,
    string LightingPreference,
    List<string> PreferredCameraAngles,
    string TransitionStyle,
    int TransitionDurationMs,
    List<string> ReferenceImagePaths,
    string? Description,
    List<string> Tags,
    bool IsFavorite,
    int UsageCount);

/// <summary>
/// Import/Export request for user preferences
/// </summary>
public record ImportPreferencesRequest(string JsonData);

/// <summary>
/// Response for export operation
/// </summary>
public record ExportPreferencesResponse(
    string JsonData,
    DateTime ExportDate,
    string Version);

