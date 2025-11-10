using Aura.Core.Data;
using Aura.Core.Services.Media;
using Aura.Core.Services.Storage;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering media library services.
/// </summary>
public static class MediaServicesExtensions
{
    /// <summary>
    /// Registers all media library related services including storage, processing, and management.
    /// </summary>
    public static IServiceCollection AddMediaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Storage services
        var storageType = configuration["Storage:Type"] ?? "Local";
        
        if (storageType.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IStorageService, AzureBlobStorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, LocalStorageService>();
        }

        // Media repositories
        services.AddScoped<IMediaRepository, MediaRepository>();

        // Media processing services
        services.AddScoped<IThumbnailGenerationService, ThumbnailGenerationService>();
        services.AddScoped<IMediaMetadataService, MediaMetadataService>();

        // Core media service
        services.AddScoped<IMediaService, MediaService>();

        // Media generation integration
        services.AddScoped<IMediaGenerationIntegrationService, MediaGenerationIntegrationService>();

        return services;
    }
}
