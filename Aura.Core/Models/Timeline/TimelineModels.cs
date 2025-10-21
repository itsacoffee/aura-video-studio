using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.Models.Timeline;

/// <summary>
/// Asset types that can be added to a timeline scene
/// </summary>
public enum AssetType
{
    Image,
    Video,
    Audio
}

/// <summary>
/// Position and size of an asset as percentages (0-100)
/// </summary>
public record Position(
    double X,
    double Y,
    double Width,
    double Height);

/// <summary>
/// Visual effects configuration for assets
/// </summary>
public record EffectConfig(
    double Brightness = 1.0,
    double Contrast = 1.0,
    double Saturation = 1.0,
    string? Filter = null);

/// <summary>
/// Asset within a timeline scene with positioning and effects
/// </summary>
public record TimelineAsset(
    string Id,
    AssetType Type,
    string FilePath,
    TimeSpan Start,
    TimeSpan Duration,
    Position Position,
    int ZIndex = 0,
    double Opacity = 1.0,
    EffectConfig? Effects = null);

/// <summary>
/// Scene in the timeline with assets and transitions
/// </summary>
public record TimelineScene(
    int Index,
    string Heading,
    string Script,
    TimeSpan Start,
    TimeSpan Duration,
    string? NarrationAudioPath = null,
    List<TimelineAsset>? VisualAssets = null,
    string TransitionType = "None",
    TimeSpan? TransitionDuration = null)
{
    public List<TimelineAsset> VisualAssets { get; init; } = VisualAssets ?? new List<TimelineAsset>();
}

/// <summary>
/// Subtitle track for the timeline
/// </summary>
public record SubtitleTrack(
    bool Enabled = false,
    string? FilePath = null,
    string Position = "Bottom",
    int FontSize = 24,
    string FontColor = "#FFFFFF",
    string BackgroundColor = "#000000",
    double BackgroundOpacity = 0.7);

/// <summary>
/// Editable timeline for the video editor
/// </summary>
public class EditableTimeline
{
    public List<TimelineScene> Scenes { get; set; } = new();
    public string? BackgroundMusicPath { get; set; }
    public SubtitleTrack Subtitles { get; set; } = new();
    
    /// <summary>
    /// Computed total duration based on all scenes
    /// </summary>
    public TimeSpan TotalDuration => 
        Scenes.Count > 0 
            ? Scenes.Max(s => s.Start + s.Duration) 
            : TimeSpan.Zero;

    /// <summary>
    /// Add a scene to the timeline
    /// </summary>
    public void AddScene(TimelineScene scene)
    {
        Scenes.Add(scene);
        RecalculateSceneIndices();
        RecalculateSceneTimings();
    }

    /// <summary>
    /// Remove a scene from the timeline
    /// </summary>
    public void RemoveScene(int index)
    {
        var scene = Scenes.FirstOrDefault(s => s.Index == index);
        if (scene != null)
        {
            Scenes.Remove(scene);
            RecalculateSceneIndices();
            RecalculateSceneTimings();
        }
    }

    /// <summary>
    /// Reorder a scene to a new position
    /// </summary>
    public void ReorderScene(int fromIndex, int toIndex)
    {
        var scene = Scenes.FirstOrDefault(s => s.Index == fromIndex);
        if (scene == null) return;

        Scenes.Remove(scene);
        
        // Adjust toIndex if necessary
        if (toIndex > fromIndex) toIndex--;
        
        // Insert at new position
        if (toIndex >= Scenes.Count)
        {
            Scenes.Add(scene);
        }
        else
        {
            Scenes.Insert(toIndex, scene);
        }
        
        RecalculateSceneIndices();
        RecalculateSceneTimings();
    }

    /// <summary>
    /// Update the duration of a scene
    /// </summary>
    public void UpdateSceneDuration(int index, TimeSpan newDuration)
    {
        var sceneIndex = Scenes.FindIndex(s => s.Index == index);
        if (sceneIndex >= 0)
        {
            var scene = Scenes[sceneIndex];
            Scenes[sceneIndex] = scene with { Duration = newDuration };
            RecalculateSceneTimings();
        }
    }

    /// <summary>
    /// Replace an asset in a scene
    /// </summary>
    public void ReplaceSceneAsset(int sceneIndex, string assetId, TimelineAsset newAsset)
    {
        var scene = Scenes.FirstOrDefault(s => s.Index == sceneIndex);
        if (scene != null)
        {
            var assetIndex = scene.VisualAssets.FindIndex(a => a.Id == assetId);
            if (assetIndex >= 0)
            {
                scene.VisualAssets[assetIndex] = newAsset;
            }
        }
    }

    /// <summary>
    /// Add an asset to a scene
    /// </summary>
    public void AddAssetToScene(int sceneIndex, TimelineAsset asset)
    {
        var scene = Scenes.FirstOrDefault(s => s.Index == sceneIndex);
        if (scene != null)
        {
            scene.VisualAssets.Add(asset);
        }
    }

    /// <summary>
    /// Remove an asset from a scene
    /// </summary>
    public void RemoveAssetFromScene(int sceneIndex, string assetId)
    {
        var scene = Scenes.FirstOrDefault(s => s.Index == sceneIndex);
        if (scene != null)
        {
            scene.VisualAssets.RemoveAll(a => a.Id == assetId);
        }
    }

    /// <summary>
    /// Recalculate scene indices after reordering
    /// </summary>
    private void RecalculateSceneIndices()
    {
        for (int i = 0; i < Scenes.Count; i++)
        {
            Scenes[i] = Scenes[i] with { Index = i };
        }
    }

    /// <summary>
    /// Recalculate scene start times based on durations
    /// </summary>
    private void RecalculateSceneTimings()
    {
        TimeSpan currentTime = TimeSpan.Zero;
        for (int i = 0; i < Scenes.Count; i++)
        {
            Scenes[i] = Scenes[i] with { Start = currentTime };
            currentTime += Scenes[i].Duration;
        }
    }

    /// <summary>
    /// Convert to immutable Timeline record for rendering
    /// </summary>
    public Timeline ToImmutableTimeline()
    {
        return new Timeline(
            Scenes: new List<TimelineScene>(Scenes),
            BackgroundMusicPath: BackgroundMusicPath,
            Subtitles: Subtitles);
    }
}

/// <summary>
/// Immutable timeline record for rendering
/// </summary>
public record Timeline(
    List<TimelineScene> Scenes,
    string? BackgroundMusicPath,
    SubtitleTrack Subtitles)
{
    public TimeSpan TotalDuration => 
        Scenes.Count > 0 
            ? Scenes.Max(s => s.Start + s.Duration) 
            : TimeSpan.Zero;
}
