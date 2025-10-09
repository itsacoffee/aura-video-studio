using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Fallback provider that generates simple slideshow frames or solid color backgrounds.
/// Always available as ultimate fallback when no other visual providers work.
/// </summary>
public class SlideshowProvider : IImageProvider
{
    private readonly ILogger<SlideshowProvider> _logger;

    public SlideshowProvider(ILogger<SlideshowProvider> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Generating slideshow/solid background for scene {Scene}: {Heading}", 
            scene.Index, scene.Heading);

        // Generate a simple slide reference - actual slide generation would be done during render
        var assets = new List<Asset>
        {
            new Asset(
                Kind: "slide",
                PathOrUrl: $"slide_{scene.Index}.png",
                License: "Generated",
                Attribution: $"Generated slide for: {scene.Heading}"
            )
        };

        return Task.FromResult<IReadOnlyList<Asset>>(assets);
    }
}
