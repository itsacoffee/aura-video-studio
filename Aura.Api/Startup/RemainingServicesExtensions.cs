using Aura.Core.Configuration;
using Aura.Core.Providers;
using Aura.Core.Services.Performance;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering remaining application services organized by domain.
/// </summary>
public static class RemainingServicesExtensions
{
    public static IServiceCollection AddHealthServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Health.ProviderHealthMonitor>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Health.ProviderHealthMonitor>>();
            var circuitBreakerSettings = sp.GetRequiredService<CircuitBreakerSettings>();
            return new Aura.Core.Services.Health.ProviderHealthMonitor(logger, circuitBreakerSettings);
        });
        services.AddSingleton<Aura.Core.Services.Health.ProviderHealthService>();
        services.AddSingleton<Aura.Core.Services.Health.SystemHealthChecker>();
        return services;
    }

    public static IServiceCollection AddConversationServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Conversation.ContextPersistence>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Conversation.ContextPersistence>>();
            var baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura");
            return new Aura.Core.Services.Conversation.ContextPersistence(logger, baseDirectory);
        });
        services.AddSingleton<Aura.Core.Services.Conversation.ConversationContextManager>();
        services.AddSingleton<Aura.Core.Services.Conversation.ProjectContextManager>();
        services.AddSingleton<Aura.Core.Services.Conversation.ConversationalLlmService>();
        return services;
    }

    public static IServiceCollection AddPromptServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.AI.PromptCustomizationService>();
        services.AddScoped<Aura.Core.Services.AI.ChainOfThoughtOrchestrator>();

        services.AddSingleton<Aura.Core.Services.PromptManagement.IPromptRepository,
            Aura.Core.Services.PromptManagement.InMemoryPromptRepository>();
        services.AddSingleton<Aura.Core.Services.PromptManagement.PromptVariableResolver>();
        services.AddSingleton<Aura.Core.Services.PromptManagement.PromptValidator>();
        services.AddSingleton<Aura.Core.Services.PromptManagement.PromptAnalyticsService>();
        services.AddSingleton<Aura.Core.Services.PromptManagement.PromptTestingService>();
        services.AddSingleton<Aura.Core.Services.PromptManagement.PromptABTestingService>();
        services.AddSingleton<Aura.Core.Services.PromptManagement.PromptManagementService>();

        services.AddHostedService<Aura.Api.HostedServices.SystemPromptInitializer>();
        return services;
    }

    public static IServiceCollection AddProfileServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Profiles.ProfilePersistence>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Profiles.ProfilePersistence>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var baseDirectory = providerSettings.GetAuraDataDirectory();
            return new Aura.Core.Services.Profiles.ProfilePersistence(logger, baseDirectory);
        });
        services.AddSingleton<Aura.Core.Services.Profiles.ProfileService>();
        services.AddSingleton<Aura.Core.Services.Profiles.ProfileContextProvider>();
        return services;
    }

    public static IServiceCollection AddLearningServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Learning.LearningPersistence>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Learning.LearningPersistence>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var baseDirectory = providerSettings.GetAuraDataDirectory();
            return new Aura.Core.Services.Learning.LearningPersistence(logger, baseDirectory);
        });
        services.AddSingleton<Aura.Core.Services.Learning.DecisionAnalysisEngine>();
        services.AddSingleton<Aura.Core.Services.Learning.PatternRecognitionSystem>();
        services.AddSingleton<Aura.Core.Services.Learning.PreferenceInferenceEngine>();
        services.AddSingleton<Aura.Core.Services.Learning.PredictiveSuggestionRanker>();
        services.AddSingleton<Aura.Core.Services.Learning.LearningService>();
        return services;
    }

    public static IServiceCollection AddIdeationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<Aura.Core.Services.Ideation.TrendingTopicsService>();
        services.AddSingleton<Aura.Core.Services.Ideation.IdeationService>();
        return services;
    }

    public static IServiceCollection AddAudienceServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Audience.AudienceProfileStore>();
        services.AddSingleton<Aura.Core.Services.Audience.AudienceProfileValidator>();
        services.AddSingleton<Aura.Core.Services.Audience.AudienceProfileConverter>();
        services.AddScoped<Aura.Core.Services.Audience.ContentAdaptationEngine>();
        services.AddScoped<Aura.Core.Services.Audience.AdaptationPreviewService>();

        services.AddSingleton<Aura.Core.Services.UserPreferences.UserPreferencesService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.UserPreferences.UserPreferencesService>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var dataDirectory = providerSettings.GetAuraDataDirectory();
            return new Aura.Core.Services.UserPreferences.UserPreferencesService(logger, dataDirectory);
        });
        return services;
    }

    public static IServiceCollection AddContentServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.ContentVerification.FactCheckingService>();
        services.AddSingleton<Aura.Core.Services.ContentVerification.SourceAttributionService>();
        services.AddSingleton<Aura.Core.Services.ContentVerification.ConfidenceAnalysisService>();
        services.AddSingleton<Aura.Core.Services.ContentVerification.MisinformationDetectionService>();
        services.AddSingleton<Aura.Core.Services.ContentVerification.ContentVerificationOrchestrator>();
        services.AddSingleton<Aura.Core.Services.ContentVerification.VerificationPersistence>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.ContentVerification.VerificationPersistence>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var dataDir = providerSettings.GetAuraDataDirectory();
            return new Aura.Core.Services.ContentVerification.VerificationPersistence(logger, dataDir);
        });

        services.AddSingleton<Aura.Core.Services.ContentPlanning.TrendAnalysisService>();
        services.AddSingleton<Aura.Core.Services.ContentPlanning.TopicGenerationService>();
        services.AddSingleton<Aura.Core.Services.ContentPlanning.AudienceAnalysisService>();
        services.AddSingleton<Aura.Core.Services.ContentPlanning.ContentSchedulingService>();

        services.AddSingleton<Aura.Core.Services.ContentSafety.KeywordListManager>();
        services.AddSingleton<Aura.Core.Services.ContentSafety.TopicFilterManager>();
        services.AddSingleton<Aura.Core.Services.ContentSafety.ContentSafetyService>();
        services.AddSingleton<Aura.Core.Services.ContentSafety.SafetyIntegrationService>();
        return services;
    }

    public static IServiceCollection AddAudioServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Audio.WavValidator>();
        services.AddSingleton<Aura.Core.Audio.SilentWavGenerator>();
        services.AddSingleton<Aura.Core.Services.Audio.NarrationOptimizationService>();
        return services;
    }

    public static IServiceCollection AddValidationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind ValidationTimeoutSettings from configuration using IConfiguration.Bind
        var timeoutSettings = new Aura.Core.Validation.ValidationTimeoutSettings();
        configuration.GetSection("Validation").Bind(timeoutSettings);
        services.AddSingleton(timeoutSettings);
        
        services.AddSingleton<Aura.Core.Validation.PreGenerationValidator>();
        services.AddSingleton<Aura.Core.Validation.ScriptValidator>();
        services.AddSingleton<Aura.Core.Validation.TtsOutputValidator>();
        services.AddSingleton<Aura.Core.Validation.ImageOutputValidator>();
        services.AddSingleton<Aura.Core.Validation.LlmOutputValidator>();
        return services;
    }

    public static IServiceCollection AddPerformanceServices(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmTimeoutPolicy>>().Value);
        services.AddSingleton<LatencyTelemetry>();
        services.AddSingleton<LatencyManagementService>();
        services.AddSingleton<LlmOperationContext>();
        return services;
    }

    public static IServiceCollection AddMLServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.ML.Models.AttentionRetentionModel>();
        services.AddSingleton<Aura.Core.ML.Models.FrameImportanceModel>();
        services.AddSingleton<Aura.Core.Services.ML.ModelTrainingService>();
        return services;
    }

    public static IServiceCollection AddPacingServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.PacingServices.ContentComplexityAnalyzer>();
        services.AddSingleton<Aura.Core.Services.PacingServices.SceneImportanceAnalyzer>();
        services.AddSingleton<Aura.Core.Services.PacingServices.AttentionCurvePredictor>();
        services.AddSingleton<Aura.Core.Services.PacingServices.TransitionRecommender>();
        services.AddSingleton<Aura.Core.Services.PacingServices.EmotionalBeatAnalyzer>();
        services.AddSingleton<Aura.Core.Services.PacingServices.SceneRelationshipMapper>();
        services.AddSingleton<Aura.Core.Services.PacingServices.IntelligentPacingOptimizer>();
        services.AddSingleton<Aura.Core.Services.PacingServices.PacingApplicationService>();
        services.AddSingleton<Aura.Api.Services.PacingAnalysisCacheService>();

        services.AddSingleton<Aura.Core.AI.Pacing.RhythmDetector>();
        services.AddSingleton<Aura.Core.AI.Pacing.RetentionOptimizer>();
        services.AddSingleton<Aura.Core.AI.Pacing.PacingAnalyzer>();
        services.AddSingleton<Aura.Core.Services.Analytics.ViewerRetentionPredictor>();
        return services;
    }

    public static IServiceCollection AddPipelineServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Orchestration.PipelineCache>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Orchestration.PipelineCache>>();
            return new Aura.Core.Services.Orchestration.PipelineCache(logger);
        });

        services.AddSingleton<Aura.Core.Services.Orchestration.PipelineHealthCheck>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Orchestration.PipelineHealthCheck>>();
            var llmProvider = sp.GetRequiredService<ILlmProvider>();
            var ttsProvider = sp.GetRequiredService<ITtsProvider>();
            var narrationOptimizer = sp.GetService<Aura.Core.Services.Audio.NarrationOptimizationService>();
            var pacingOptimizer = sp.GetService<Aura.Core.Services.PacingServices.IntelligentPacingOptimizer>();

            return new Aura.Core.Services.Orchestration.PipelineHealthCheck(
                logger: logger,
                llmProvider: llmProvider,
                ttsProvider: ttsProvider,
                contentAdvisor: null,
                narrativeAnalyzer: null,
                pacingOptimizer: pacingOptimizer,
                toneEnforcer: null,
                visualPromptService: null,
                visualAlignmentService: null,
                narrationOptimizer: narrationOptimizer,
                scriptRefinement: null
            );
        });

        services.AddSingleton<Aura.Core.Services.Orchestration.PipelineOrchestrationEngine>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Orchestration.PipelineOrchestrationEngine>>();
            var llmProvider = sp.GetRequiredService<ILlmProvider>();
            var cache = sp.GetRequiredService<Aura.Core.Services.Orchestration.PipelineCache>();
            var healthCheck = sp.GetRequiredService<Aura.Core.Services.Orchestration.PipelineHealthCheck>();
            var ttsProvider = sp.GetRequiredService<ITtsProvider>();
            var narrationOptimizer = sp.GetService<Aura.Core.Services.Audio.NarrationOptimizationService>();
            var pacingOptimizer = sp.GetService<Aura.Core.Services.PacingServices.IntelligentPacingOptimizer>();

            var config = new Aura.Core.Services.Orchestration.PipelineConfiguration
            {
                MaxConcurrentLlmCalls = Math.Max(1, Environment.ProcessorCount / 2),
                EnableCaching = true,
                CacheTtl = TimeSpan.FromHours(1),
                ContinueOnOptionalFailure = true,
                EnableParallelExecution = true
            };

            return new Aura.Core.Services.Orchestration.PipelineOrchestrationEngine(
                logger: logger,
                llmProvider: llmProvider,
                cache: cache,
                healthCheck: healthCheck,
                config: config,
                ttsProvider: ttsProvider,
                contentAdvisor: null,
                narrativeAnalyzer: null,
                pacingOptimizer: pacingOptimizer,
                toneEnforcer: null,
                visualPromptService: null,
                visualAlignmentService: null,
                narrationOptimizer: narrationOptimizer,
                scriptRefinement: null
            );
        });
        return services;
    }

    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.PerformanceAnalytics.AnalyticsPersistence>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.PerformanceAnalytics.AnalyticsPersistence>>();
            var baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura");
            return new Aura.Core.Services.PerformanceAnalytics.AnalyticsPersistence(logger, baseDirectory);
        });
        services.AddSingleton<Aura.Core.Services.PerformanceAnalytics.AnalyticsImporter>();

        return services;
    }

    public static IServiceCollection AddResourceServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Resources.TemporaryFileCleanupService>();
        services.AddSingleton<Aura.Core.Services.Resources.DiskSpaceChecker>();
        return services;
    }

    public static IServiceCollection AddTelemetryServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Api.Telemetry.PerformanceMetrics>();
        return services;
    }
}
