using Aura.Core.AI.Agents;
using Aura.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Extensions;

/// <summary>
/// Extension methods for registering agent services
/// </summary>
public static class AgentServiceExtensions
{
    /// <summary>
    /// Registers all agent-related services in the dependency injection container
    /// </summary>
    public static IServiceCollection AddAgentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<AgenticModeOptions>(
            configuration.GetSection("AgenticMode"));

        // Register individual agents as scoped (one per request)
        services.AddScoped<ScreenwriterAgent>(sp =>
        {
            var llmProvider = sp.GetRequiredService<Aura.Core.Providers.ILlmProvider>();
            var logger = sp.GetRequiredService<ILogger<ScreenwriterAgent>>();
            return new ScreenwriterAgent(llmProvider, logger);
        });

        services.AddScoped<VisualDirectorAgent>(sp =>
        {
            var llmProvider = sp.GetRequiredService<Aura.Core.Providers.ILlmProvider>();
            var logger = sp.GetRequiredService<ILogger<VisualDirectorAgent>>();
            return new VisualDirectorAgent(llmProvider, logger);
        });

        services.AddScoped<CriticAgent>(sp =>
        {
            var llmProvider = sp.GetRequiredService<Aura.Core.Providers.ILlmProvider>();
            var validator = sp.GetRequiredService<Aura.Core.AI.Validation.ScriptSchemaValidator>();
            var logger = sp.GetRequiredService<ILogger<CriticAgent>>();
            return new CriticAgent(llmProvider, validator, logger);
        });

        // Register orchestrator
        services.AddScoped<AgentOrchestrator>(sp =>
        {
            var screenwriter = sp.GetRequiredService<ScreenwriterAgent>();
            var visualDirector = sp.GetRequiredService<VisualDirectorAgent>();
            var critic = sp.GetRequiredService<CriticAgent>();
            var logger = sp.GetRequiredService<ILogger<AgentOrchestrator>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var promptRepository = sp.GetService<Aura.Core.Data.Repositories.IVisualPromptRepository>(); // Optional
            return new AgentOrchestrator(screenwriter, visualDirector, critic, logger, loggerFactory, promptRepository);
        });

        return services;
    }
}

