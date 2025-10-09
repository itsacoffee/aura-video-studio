using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Stock image provider that scans a local folder for images.
/// Used for CC0 packs and user-provided local assets.
/// </summary>
public class LocalStockProvider : IStockProvider
{
    private readonly ILogger<LocalStockProvider> _logger;
    private readonly string _baseDirectory;
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

    public LocalStockProvider(
        ILogger<LocalStockProvider> logger,
        string baseDirectory)
    {
        _logger = logger;
        _baseDirectory = baseDirectory;

        if (!Directory.Exists(_baseDirectory))
        {
            _logger.LogWarning("Local stock directory does not exist: {Directory}", _baseDirectory);
        }
    }

    public Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct)
    {
        if (!Directory.Exists(_baseDirectory))
        {
            _logger.LogWarning("Local stock directory not found: {Directory}", _baseDirectory);
            return Task.FromResult<IReadOnlyList<Asset>>(Array.Empty<Asset>());
        }

        _logger.LogInformation("Searching local directory for images: {Query} (count: {Count})", query, count);

        try
        {
            var assets = new List<Asset>();
            var files = Directory.GetFiles(_baseDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            // Simple keyword matching - if query is in filename or parent directory
            var queryKeywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchedFiles = files.Where(f =>
            {
                var fullPath = f.ToLowerInvariant();
                return queryKeywords.Any(keyword => fullPath.Contains(keyword));
            }).ToList();

            // If no matches, just return random files from the collection
            if (matchedFiles.Count == 0)
            {
                matchedFiles = files.OrderBy(_ => Guid.NewGuid()).ToList();
            }

            foreach (var file in matchedFiles.Take(count))
            {
                assets.Add(new Asset(
                    Kind: "image",
                    PathOrUrl: file,
                    License: "Local/CC0",
                    Attribution: $"Local file: {Path.GetFileName(file)}"
                ));
            }

            _logger.LogInformation("Found {Count} local images", assets.Count);
            return Task.FromResult<IReadOnlyList<Asset>>(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching local directory for {Query}", query);
            return Task.FromResult<IReadOnlyList<Asset>>(Array.Empty<Asset>());
        }
    }
}
