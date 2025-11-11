using Aura.Core.Configuration;
using Aura.Core.Resilience;
using Aura.Core.Resilience.ErrorTracking;
using Aura.Core.Resilience.Idempotency;
using Aura.Core.Resilience.Monitoring;
using Aura.Core.Resilience.Saga;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering resilience services
/// </summary>
public static class ResilienceServicesExtensions
{
    /// <summary>
    /// Adds comprehensive resilience services to the DI container
    /// </summary>
    public static IServiceCollection AddResilienceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure circuit breaker settings
        services.Configure<CircuitBreakerSettings>(
            configuration.GetSection("CircuitBreaker") ?? configuration.GetSection("Resilience:CircuitBreaker"));

        // Register core resilience services
        services.AddSingleton<IResiliencePipelineFactory, ResiliencePipelineFactory>();
        services.AddSingleton<CircuitBreakerStateManager>();
        services.AddSingleton<ErrorMetricsCollector>();
        services.AddSingleton<IdempotencyManager>();
        services.AddSingleton<ResilienceHealthMonitor>();
        services.AddSingleton<SagaOrchestrator>();

        return services;
    }

    /// <summary>
    /// Adds resilience-aware HttpClient configurations
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
    {
        var builder = services.AddHttpClient(name, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            configureClient?.Invoke(client);
        });

        // Add standard resilience handler (retry, circuit breaker, timeout)
        builder.AddStandardResilienceHandler();

        return builder;
    }

    /// <summary>
    /// Adds a typed HttpClient with resilience
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient<TClient>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        var clientName = typeof(TClient).Name;
        
        var builder = services.AddHttpClient<TClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            configureClient?.Invoke(client);
        });

        // Add standard resilience handler (retry, circuit breaker, timeout)
        builder.AddStandardResilienceHandler();

        return builder;
    }
}
