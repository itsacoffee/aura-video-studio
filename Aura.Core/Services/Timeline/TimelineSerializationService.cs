using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Core.Models;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Timeline;

/// <summary>
/// Centralizes conversion and serialization logic between provider timelines and
/// editable timelines used by the UI/editor layers.
/// </summary>
public class TimelineSerializationService
{
    private readonly ILogger<TimelineSerializationService> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public TimelineSerializationService(ILogger<TimelineSerializationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Shared serializer options for timelines.
    /// </summary>
    public JsonSerializerOptions SerializerOptions => _serializerOptions;

    /// <summary>
    /// Serialize an editable timeline to JSON using normalized options.
    /// </summary>
    public string Serialize(EditableTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        return JsonSerializer.Serialize(timeline, _serializerOptions);
    }

    /// <summary>
    /// Deserialize JSON into an editable timeline. Returns an empty timeline on failure.
    /// </summary>
    public EditableTimeline Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new EditableTimeline();
        }

        try
        {
            return JsonSerializer.Deserialize<EditableTimeline>(json, _serializerOptions) ?? new EditableTimeline();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize timeline JSON. Returning empty timeline.");
            return new EditableTimeline();
        }
    }

    /// <summary>
    /// Convert a provider pipeline timeline to an editable timeline used by the UI/editor.
    /// </summary>
    public EditableTimeline CreateEditableTimeline(Core.Providers.Timeline providerTimeline)
    {
        ArgumentNullException.ThrowIfNull(providerTimeline);

        var editableTimeline = new EditableTimeline
        {
            BackgroundMusicPath = string.IsNullOrWhiteSpace(providerTimeline.MusicPath)
                ? null
                : providerTimeline.MusicPath,
            Subtitles = string.IsNullOrWhiteSpace(providerTimeline.SubtitlesPath)
                ? new SubtitleTrack()
                : new SubtitleTrack(
                    Enabled: true,
                    FilePath: providerTimeline.SubtitlesPath)
        };

        foreach (var scene in providerTimeline.Scenes)
        {
            var assets = providerTimeline.SceneAssets.TryGetValue(scene.Index, out var sceneAssets) && sceneAssets != null
                ? sceneAssets.Select((asset, assetIndex) => ConvertAsset(asset, scene, assetIndex)).ToList()
                : new List<TimelineAsset>();

            editableTimeline.Scenes.Add(new TimelineScene(
                Index: scene.Index,
                Heading: string.IsNullOrWhiteSpace(scene.Heading)
                    ? $"Scene {scene.Index + 1}"
                    : scene.Heading,
                Script: scene.Script,
                Start: scene.Start,
                Duration: scene.Duration,
                NarrationAudioPath: providerTimeline.NarrationPath,
                VisualAssets: assets));
        }

        return editableTimeline;
    }

    private static TimelineAsset ConvertAsset(Asset asset, Scene scene, int order)
    {
        var assetType = asset.Kind?.ToLowerInvariant() switch
        {
            "video" => AssetType.Video,
            "audio" => AssetType.Audio,
            _ => AssetType.Image
        };

        var assetId = $"{assetType.ToString().ToLowerInvariant()}_{scene.Index}_{order}_{Guid.NewGuid():N}";

        return new TimelineAsset(
            Id: assetId,
            Type: assetType,
            FilePath: asset.PathOrUrl,
            Start: scene.Start,
            Duration: scene.Duration,
            Position: new Position(0, 0, 100, 100),
            ZIndex: order);
    }
}

