using Aura.Core.Services.Repurposing;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering AI repurposing services.
/// </summary>
public static class RepurposingServicesExtensions
{
    /// <summary>
    /// Registers AI repurposing services including video repurposing,
    /// shorts extraction, blog generation, quote generation, and aspect conversion.
    /// </summary>
    public static IServiceCollection AddRepurposingServices(this IServiceCollection services)
    {
        // Register shorts extractor
        services.AddScoped<IShortsExtractor, ShortsExtractor>();

        // Register blog generator
        services.AddScoped<IBlogGenerator, BlogGenerator>();

        // Register quote generator
        services.AddScoped<IQuoteGenerator, QuoteGenerator>();

        // Register aspect converter
        services.AddScoped<IAspectConverter, AspectConverter>();

        // Register main repurposing service
        services.AddScoped<IRepurposingService, RepurposingService>();

        return services;
    }
}
