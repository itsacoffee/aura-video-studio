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
    /// Configuration loading is handled in Program.cs to avoid duplication.
    /// </summary>
    public static IServiceCollection AddRouterServices(this IServiceCollection services)
    {
        services.AddSingleton<IRouterProviderFactory, RouterProviderFactory>();
        services.AddSingleton<ILlmRouterService, LlmRouterService>();

        return services;
    }
}
