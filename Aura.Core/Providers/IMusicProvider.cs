using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;

namespace Aura.Core.Providers;

/// <summary>
/// Interface for music providers (stock and generative)
/// </summary>
public interface IMusicProvider
{
    /// <summary>
    /// Provider name for identification
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this provider is available (API key configured, service accessible)
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Search for music tracks matching criteria
    /// </summary>
    Task<SearchResult<MusicAsset>> SearchAsync(
        MusicSearchCriteria criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific music track by ID
    /// </summary>
    Task<MusicAsset?> GetByIdAsync(string assetId, CancellationToken ct = default);

    /// <summary>
    /// Download music file to specified path
    /// </summary>
    Task<string> DownloadAsync(string assetId, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Get preview URL for streaming without download
    /// </summary>
    Task<string?> GetPreviewUrlAsync(string assetId, CancellationToken ct = default);
}

/// <summary>
/// Interface for generative music providers (optional, requires API key)
/// </summary>
public interface IGenerativeMusicProvider : IMusicProvider
{
    /// <summary>
    /// Generate music based on prompt
    /// </summary>
    Task<MusicAsset> GenerateAsync(
        MusicPrompt prompt,
        CancellationToken ct = default);

    /// <summary>
    /// Check generation status for async generation
    /// </summary>
    Task<GenerationStatus> GetGenerationStatusAsync(
        string generationId,
        CancellationToken ct = default);
}

/// <summary>
/// Generation status for async operations
/// </summary>
public record GenerationStatus(
    string GenerationId,
    GenerationState State,
    int ProgressPercent,
    string? ErrorMessage,
    MusicAsset? Result
);

/// <summary>
/// State of music generation
/// </summary>
public enum GenerationState
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}
