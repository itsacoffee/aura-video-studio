using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Ken Burns motion types for scene animation.
/// Controls how the camera virtually moves across a still image.
/// </summary>
public enum KenBurnsMotion
{
    /// <summary>No motion applied</summary>
    None,
    /// <summary>Zoom into the image center</summary>
    ZoomIn,
    /// <summary>Zoom out from the image center</summary>
    ZoomOut,
    /// <summary>Pan from right to left</summary>
    PanLeft,
    /// <summary>Pan from left to right</summary>
    PanRight,
    /// <summary>Pan from bottom to top</summary>
    PanUp,
    /// <summary>Pan from top to bottom</summary>
    PanDown,
    /// <summary>Diagonal movement from top-left to bottom-right</summary>
    DiagonalTopLeftToBottomRight,
    /// <summary>Diagonal movement from bottom-right to top-left</summary>
    DiagonalBottomRightToTopLeft,
    /// <summary>Track detected subject across the frame</summary>
    SubjectTracking
}

/// <summary>
/// Video transition types between scenes for AI Director.
/// Extended set compared to base TransitionType in PacingModels.
/// </summary>
public enum DirectorTransitionType
{
    /// <summary>Direct cut with no transition effect</summary>
    Cut,
    /// <summary>Fade to/from black</summary>
    Fade,
    /// <summary>Cross dissolve between scenes</summary>
    CrossDissolve,
    /// <summary>Wipe transition</summary>
    Wipe,
    /// <summary>Zoom transition effect</summary>
    Zoom,
    /// <summary>Slide one scene over another</summary>
    Slide,
    /// <summary>Push one scene off screen</summary>
    Push,
    /// <summary>No transition (used for internal processing)</summary>
    None
}

/// <summary>
/// Director preset styles that define overall video aesthetics.
/// Each preset applies different motion, transition, and pacing decisions.
/// </summary>
public enum DirectorPreset
{
    /// <summary>Steady, informative style with minimal motion and clean cuts</summary>
    Documentary,
    /// <summary>Fast-paced, dynamic style with quick cuts and high energy</summary>
    TikTokEnergy,
    /// <summary>Slow, dramatic style with emotional transitions and epic reveals</summary>
    Cinematic,
    /// <summary>Clean, professional style with subtle motion and polished feel</summary>
    Corporate,
    /// <summary>Clear, focused style with emphasis on key points for comprehension</summary>
    Educational,
    /// <summary>Narrative-driven style with emotion-matched pacing</summary>
    Storytelling,
    /// <summary>Manual control over all director settings</summary>
    Custom
}

/// <summary>
/// Emotional state identified for a scene.
/// </summary>
public record SceneEmotion(
    double Intensity,
    string PrimaryEmotion,
    bool IsKeyPoint,
    string FocusPoint);

/// <summary>
/// Result of emotional arc analysis across all scenes.
/// </summary>
public record EmotionalArcResult(
    IReadOnlyList<SceneEmotion> SceneEmotions,
    string Summary,
    string OverallTone);

/// <summary>
/// Direction decisions for a single scene.
/// </summary>
public record SceneDirection(
    int SceneIndex,
    KenBurnsMotion Motion,
    DirectorTransitionType InTransition,
    DirectorTransitionType OutTransition,
    double EmotionalIntensity,
    string VisualFocus,
    TimeSpan SuggestedDuration,
    double KenBurnsIntensity = 0.1);

/// <summary>
/// Complete director decisions for a video.
/// </summary>
public record DirectorDecisions(
    IReadOnlyList<SceneDirection> SceneDirections,
    string OverallStyle,
    string EmotionalArc);
