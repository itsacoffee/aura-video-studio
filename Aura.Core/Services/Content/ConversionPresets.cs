using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Content;

namespace Aura.Core.Services.Content;

/// <summary>
/// Predefined conversion presets for common document-to-video workflows
/// </summary>
public static class ConversionPresets
{
    private static readonly List<PresetDefinition> _presets = new()
    {
        new PresetDefinition
        {
            Type = ConversionPreset.Generic,
            Name = "Generic Document",
            Description = "Balanced conversion suitable for most documents",
            DefaultConfig = new ConversionConfig
            {
                Preset = ConversionPreset.Generic,
                TargetDuration = TimeSpan.FromMinutes(3),
                WordsPerMinute = 150,
                EnableAudienceRetargeting = true,
                EnableVisualSuggestions = true,
                PreserveOriginalStructure = false,
                AddTransitions = true,
                AggressivenessLevel = 0.6
            },
            BestForFormats = new List<string> { ".txt", ".md", ".html" },
            RestructuringStrategy = "Identify main points and create engaging video flow with hook, body, and conclusion"
        },

        new PresetDefinition
        {
            Type = ConversionPreset.BlogToYouTube,
            Name = "Blog Post → YouTube Video",
            Description = "Converts blog articles into engaging YouTube video scripts",
            DefaultConfig = new ConversionConfig
            {
                Preset = ConversionPreset.BlogToYouTube,
                TargetDuration = TimeSpan.FromMinutes(8),
                WordsPerMinute = 160,
                EnableAudienceRetargeting = true,
                EnableVisualSuggestions = true,
                PreserveOriginalStructure = false,
                AddTransitions = true,
                AggressivenessLevel = 0.7
            },
            BestForFormats = new List<string> { ".md", ".html" },
            RestructuringStrategy = "Strong hook, conversational tone, clear call-to-action, YouTube optimization"
        },

        new PresetDefinition
        {
            Type = ConversionPreset.TechnicalToExplainer,
            Name = "Technical Doc → Explainer Video",
            Description = "Simplifies technical documentation into accessible explainer videos",
            DefaultConfig = new ConversionConfig
            {
                Preset = ConversionPreset.TechnicalToExplainer,
                TargetDuration = TimeSpan.FromMinutes(5),
                WordsPerMinute = 140,
                EnableAudienceRetargeting = true,
                EnableVisualSuggestions = true,
                PreserveOriginalStructure = false,
                AddTransitions = true,
                AggressivenessLevel = 0.8
            },
            BestForFormats = new List<string> { ".md", ".pdf", ".docx" },
            RestructuringStrategy = "Simplify jargon, add analogies, step-by-step explanations, heavy visual support"
        },

        new PresetDefinition
        {
            Type = ConversionPreset.AcademicToEducational,
            Name = "Academic Paper → Educational Video",
            Description = "Transforms academic papers into educational video content",
            DefaultConfig = new ConversionConfig
            {
                Preset = ConversionPreset.AcademicToEducational,
                TargetDuration = TimeSpan.FromMinutes(10),
                WordsPerMinute = 135,
                EnableAudienceRetargeting = true,
                EnableVisualSuggestions = true,
                PreserveOriginalStructure = true,
                AddTransitions = true,
                AggressivenessLevel = 0.7
            },
            BestForFormats = new List<string> { ".pdf", ".docx" },
            RestructuringStrategy = "Maintain structure, simplify abstract concepts, add visual aids, preserve citations as on-screen text"
        },

        new PresetDefinition
        {
            Type = ConversionPreset.NewsToSegment,
            Name = "News Article → News Segment",
            Description = "Converts news articles into broadcast-style news segments",
            DefaultConfig = new ConversionConfig
            {
                Preset = ConversionPreset.NewsToSegment,
                TargetDuration = TimeSpan.FromMinutes(2),
                WordsPerMinute = 170,
                EnableAudienceRetargeting = false,
                EnableVisualSuggestions = true,
                PreserveOriginalStructure = false,
                AddTransitions = false,
                AggressivenessLevel = 0.5
            },
            BestForFormats = new List<string> { ".html", ".txt" },
            RestructuringStrategy = "Inverted pyramid, key facts first, concise delivery, B-roll opportunities"
        },

        new PresetDefinition
        {
            Type = ConversionPreset.TutorialToHowTo,
            Name = "Tutorial Guide → How-To Video",
            Description = "Converts step-by-step tutorials into instructional videos",
            DefaultConfig = new ConversionConfig
            {
                Preset = ConversionPreset.TutorialToHowTo,
                TargetDuration = TimeSpan.FromMinutes(6),
                WordsPerMinute = 145,
                EnableAudienceRetargeting = true,
                EnableVisualSuggestions = true,
                PreserveOriginalStructure = true,
                AddTransitions = true,
                AggressivenessLevel = 0.6
            },
            BestForFormats = new List<string> { ".md", ".html", ".txt" },
            RestructuringStrategy = "Clear numbered steps, prerequisites upfront, tips and warnings as callouts, demo visuals"
        }
    };

    /// <summary>
    /// Gets all available presets
    /// </summary>
    public static IReadOnlyList<PresetDefinition> GetAllPresets()
    {
        return _presets.AsReadOnly();
    }

    /// <summary>
    /// Gets a specific preset by type
    /// </summary>
    public static PresetDefinition GetPreset(ConversionPreset type)
    {
        return _presets.FirstOrDefault(p => p.Type == type) 
               ?? _presets.First(p => p.Type == ConversionPreset.Generic);
    }

    /// <summary>
    /// Suggests the best preset for a given document format
    /// </summary>
    public static PresetDefinition SuggestPresetForFormat(string fileName, DocumentFormat format)
    {
        var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        
        if (format == DocumentFormat.AuraScript || format == DocumentFormat.Json)
        {
            return GetPreset(ConversionPreset.Generic);
        }

        foreach (var preset in _presets.Where(p => p.Type != ConversionPreset.Custom))
        {
            if (preset.BestForFormats.Contains(extension))
            {
                return preset;
            }
        }

        return GetPreset(ConversionPreset.Generic);
    }

    /// <summary>
    /// Creates a custom preset with user-defined configuration
    /// </summary>
    public static PresetDefinition CreateCustomPreset(
        string name, 
        string description, 
        ConversionConfig config)
    {
        return new PresetDefinition
        {
            Type = ConversionPreset.Custom,
            Name = name,
            Description = description,
            DefaultConfig = config,
            BestForFormats = new List<string>(),
            RestructuringStrategy = "User-defined conversion strategy"
        };
    }
}
