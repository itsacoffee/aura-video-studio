using System.Collections.Generic;

namespace Aura.Core.AI.Pacing.ContentTemplates;

/// <summary>
/// Library of content templates for different video formats.
/// </summary>
public static class TemplateLibrary
{
    /// <summary>
    /// Gets all available content templates.
    /// </summary>
    public static IReadOnlyList<ContentTemplate> GetAllTemplates()
    {
        return new List<ContentTemplate>
        {
            GetExplainerTemplate(),
            GetTutorialTemplate(),
            GetVlogTemplate(),
            GetReviewTemplate(),
            GetEducationalTemplate(),
            GetEntertainmentTemplate()
        };
    }

    /// <summary>
    /// Gets template by video format.
    /// </summary>
    public static ContentTemplate GetTemplate(VideoFormat format)
    {
        return format switch
        {
            VideoFormat.Explainer => GetExplainerTemplate(),
            VideoFormat.Tutorial => GetTutorialTemplate(),
            VideoFormat.Vlog => GetVlogTemplate(),
            VideoFormat.Review => GetReviewTemplate(),
            VideoFormat.Educational => GetEducationalTemplate(),
            VideoFormat.Entertainment => GetEntertainmentTemplate(),
            _ => GetDefaultTemplate()
        };
    }

    private static ContentTemplate GetExplainerTemplate()
    {
        return new ContentTemplate(
            "Explainer Video",
            "Clear, concise explanations with visual support. Optimized for educational content that explains concepts or processes.",
            VideoFormat.Explainer,
            new PacingParameters(
                MinSceneDuration: 8,
                MaxSceneDuration: 25,
                AverageSceneDuration: 15,
                TransitionDensity: 0.6,
                HookDuration: 10,
                MusicSyncEnabled: true
            )
        );
    }

    private static ContentTemplate GetTutorialTemplate()
    {
        return new ContentTemplate(
            "Tutorial Video",
            "Step-by-step instructional content. Allows longer scenes for detailed explanations.",
            VideoFormat.Tutorial,
            new PacingParameters(
                MinSceneDuration: 15,
                MaxSceneDuration: 40,
                AverageSceneDuration: 25,
                TransitionDensity: 0.4,
                HookDuration: 12,
                MusicSyncEnabled: false
            )
        );
    }

    private static ContentTemplate GetVlogTemplate()
    {
        return new ContentTemplate(
            "Vlog",
            "Personal, narrative-driven content. Fast-paced with frequent transitions.",
            VideoFormat.Vlog,
            new PacingParameters(
                MinSceneDuration: 5,
                MaxSceneDuration: 20,
                AverageSceneDuration: 12,
                TransitionDensity: 0.8,
                HookDuration: 8,
                MusicSyncEnabled: true
            )
        );
    }

    private static ContentTemplate GetReviewTemplate()
    {
        return new ContentTemplate(
            "Review Video",
            "Product or service evaluation. Balanced pacing with clear sections.",
            VideoFormat.Review,
            new PacingParameters(
                MinSceneDuration: 10,
                MaxSceneDuration: 30,
                AverageSceneDuration: 18,
                TransitionDensity: 0.5,
                HookDuration: 10,
                MusicSyncEnabled: true
            )
        );
    }

    private static ContentTemplate GetEducationalTemplate()
    {
        return new ContentTemplate(
            "Educational Content",
            "In-depth learning material. Slower pacing for complex topics.",
            VideoFormat.Educational,
            new PacingParameters(
                MinSceneDuration: 20,
                MaxSceneDuration: 60,
                AverageSceneDuration: 35,
                TransitionDensity: 0.3,
                HookDuration: 15,
                MusicSyncEnabled: false
            )
        );
    }

    private static ContentTemplate GetEntertainmentTemplate()
    {
        return new ContentTemplate(
            "Entertainment",
            "Engaging, fast-paced content. Optimized for maximum viewer retention.",
            VideoFormat.Entertainment,
            new PacingParameters(
                MinSceneDuration: 3,
                MaxSceneDuration: 15,
                AverageSceneDuration: 8,
                TransitionDensity: 0.9,
                HookDuration: 5,
                MusicSyncEnabled: true
            )
        );
    }

    private static ContentTemplate GetDefaultTemplate()
    {
        return new ContentTemplate(
            "General Content",
            "Default pacing template for general-purpose videos.",
            VideoFormat.Explainer,
            new PacingParameters(
                MinSceneDuration: 10,
                MaxSceneDuration: 30,
                AverageSceneDuration: 18,
                TransitionDensity: 0.5,
                HookDuration: 10,
                MusicSyncEnabled: true
            )
        );
    }
}
