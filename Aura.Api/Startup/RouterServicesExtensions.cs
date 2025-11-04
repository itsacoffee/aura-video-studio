using System;
using System.IO;
using System.Text.Json;
using Aura.Core.AI.Routing;
using Aura.Providers.Llm;
using Microsoft.Extensions.Options;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering LLM router services.
/// </summary>
public static class RouterServicesExtensions
{
    /// <summary>
    /// Registers LLM router services including provider factory and routing configuration.
    /// </summary>
    public static IServiceCollection AddRouterServices(this IServiceCollection services)
    {
        services.AddSingleton<IRouterProviderFactory, RouterProviderFactory>();

        services.Configure<RoutingConfiguration>(options =>
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "routing-policies.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<RoutingConfiguration>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config != null)
                    {
                        options.CircuitBreaker = config.CircuitBreaker;
                        options.HealthCheck = config.HealthCheck;
                        options.CostTracking = config.CostTracking;
                        options.EnableFailover = config.EnableFailover;
                        options.EnableCostTracking = config.EnableCostTracking;
                        options.Policies = config.Policies;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load routing configuration from {configPath}: {ex.Message}");
                }
            }
        });

        services.AddSingleton<ILlmRouterService, LlmRouterService>();

        return services;
    }
}
