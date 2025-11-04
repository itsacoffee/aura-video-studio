using Aura.Api.Configuration;
using Aura.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for configuring services in the application's DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all configuration options from appsettings.json to the service collection.
    /// </summary>
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Register all Options classes for configuration sections
        services.Configure<HealthChecksOptions>(configuration.GetSection("HealthChecks"));
        services.Configure<EnginesOptions>(configuration.GetSection("Engines"));
        services.Configure<FFmpegOptions>(configuration.GetSection("FFmpeg"));
        services.Configure<PerformanceOptions>(configuration.GetSection("Performance"));
        services.Configure<LlmTimeoutsOptions>(configuration.GetSection("LlmTimeouts"));
        services.Configure<PromptEngineeringOptions>(configuration.GetSection("PromptEngineering"));
        services.Configure<CircuitBreakerSettings>(configuration.GetSection("CircuitBreaker"));
        services.Configure<ValidationOptions>(configuration.GetSection("Validation"));

        // Register singleton instances from options for convenience
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<CircuitBreakerSettings>>().Value);

        return services;
    }

    /// <summary>
    /// Adds all application services to the DI container.
    /// Uses domain-specific extension methods for organization.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services by domain
        services.AddCoreServices();
        services.AddProviderServices();
        services.AddOrchestratorServices(configuration);
        services.AddHealthServices();
        services.AddConversationServices();
        services.AddPromptServices();
        services.AddProfileServices();
        services.AddLearningServices();
        services.AddIdeationServices();
        services.AddAudienceServices();
        services.AddContentServices();
        services.AddAudioServices();
        services.AddValidationServices();
        services.AddPerformanceServices();
        services.AddMLServices();
        services.AddPacingServices();
        services.AddPipelineServices();
        services.AddAnalyticsServices();
        services.AddResourceServices();
        services.AddTelemetryServices();

        return services;
    }
}
