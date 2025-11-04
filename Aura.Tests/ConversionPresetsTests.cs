using System;
using System.Linq;
using Aura.Core.Models.Content;
using Aura.Core.Services.Content;
using Xunit;

namespace Aura.Tests;

public class ConversionPresetsTests
{
    [Fact]
    public void GetAllPresets_ReturnsAtLeast5Presets()
    {
        var presets = ConversionPresets.GetAllPresets();

        Assert.NotNull(presets);
        Assert.True(presets.Count >= 5, $"Expected at least 5 presets, got {presets.Count}");
    }

    [Fact]
    public void GetAllPresets_ContainsRequiredPresets()
    {
        var presets = ConversionPresets.GetAllPresets();

        Assert.Contains(presets, p => p.Type == ConversionPreset.Generic);
        Assert.Contains(presets, p => p.Type == ConversionPreset.BlogToYouTube);
        Assert.Contains(presets, p => p.Type == ConversionPreset.TechnicalToExplainer);
        Assert.Contains(presets, p => p.Type == ConversionPreset.AcademicToEducational);
        Assert.Contains(presets, p => p.Type == ConversionPreset.NewsToSegment);
        Assert.Contains(presets, p => p.Type == ConversionPreset.TutorialToHowTo);
    }

    [Fact]
    public void GetPreset_Generic_ReturnsValidPreset()
    {
        var preset = ConversionPresets.GetPreset(ConversionPreset.Generic);

        Assert.NotNull(preset);
        Assert.Equal(ConversionPreset.Generic, preset.Type);
        Assert.Equal("Generic Document", preset.Name);
        Assert.NotNull(preset.DefaultConfig);
        Assert.True(preset.DefaultConfig.TargetDuration.TotalMinutes > 0);
        Assert.True(preset.DefaultConfig.WordsPerMinute > 0);
    }

    [Fact]
    public void GetPreset_BlogToYouTube_HasCorrectConfiguration()
    {
        var preset = ConversionPresets.GetPreset(ConversionPreset.BlogToYouTube);

        Assert.Equal("Blog Post → YouTube Video", preset.Name);
        Assert.True(preset.DefaultConfig.TargetDuration.TotalMinutes >= 5);
        Assert.True(preset.DefaultConfig.EnableAudienceRetargeting);
        Assert.True(preset.DefaultConfig.EnableVisualSuggestions);
        Assert.Contains(".md", preset.BestForFormats);
        Assert.Contains(".html", preset.BestForFormats);
    }

    [Fact]
    public void GetPreset_TechnicalToExplainer_SimplifiesContent()
    {
        var preset = ConversionPresets.GetPreset(ConversionPreset.TechnicalToExplainer);

        Assert.Equal("Technical Doc → Explainer Video", preset.Name);
        Assert.True(preset.DefaultConfig.AggressivenessLevel >= 0.7);
        Assert.Contains("Simplify jargon", preset.RestructuringStrategy);
    }

    [Fact]
    public void GetPreset_AcademicToEducational_PreservesStructure()
    {
        var preset = ConversionPresets.GetPreset(ConversionPreset.AcademicToEducational);

        Assert.Equal("Academic Paper → Educational Video", preset.Name);
        Assert.True(preset.DefaultConfig.PreserveOriginalStructure);
        Assert.True(preset.DefaultConfig.TargetDuration.TotalMinutes >= 8);
    }

    [Fact]
    public void GetPreset_NewsToSegment_IsConcise()
    {
        var preset = ConversionPresets.GetPreset(ConversionPreset.NewsToSegment);

        Assert.Equal("News Article → News Segment", preset.Name);
        Assert.True(preset.DefaultConfig.TargetDuration.TotalMinutes <= 3);
        Assert.True(preset.DefaultConfig.WordsPerMinute >= 160);
        Assert.False(preset.DefaultConfig.AddTransitions);
    }

    [Fact]
    public void GetPreset_TutorialToHowTo_MaintainsSteps()
    {
        var preset = ConversionPresets.GetPreset(ConversionPreset.TutorialToHowTo);

        Assert.Equal("Tutorial Guide → How-To Video", preset.Name);
        Assert.True(preset.DefaultConfig.PreserveOriginalStructure);
        Assert.Contains("numbered steps", preset.RestructuringStrategy);
    }

    [Fact]
    public void SuggestPresetForFormat_Markdown_SuggestsAppropriatePreset()
    {
        var preset = ConversionPresets.SuggestPresetForFormat("article.md", DocFormat.Markdown);

        Assert.NotNull(preset);
        Assert.Contains(".md", preset.BestForFormats);
    }

    [Fact]
    public void SuggestPresetForFormat_Html_SuggestsAppropriatePreset()
    {
        var preset = ConversionPresets.SuggestPresetForFormat("blog.html", DocFormat.Html);

        Assert.NotNull(preset);
        Assert.Contains(".html", preset.BestForFormats);
    }

    [Fact]
    public void SuggestPresetForFormat_UnknownFormat_ReturnsGeneric()
    {
        var preset = ConversionPresets.SuggestPresetForFormat("file.xyz", DocFormat.PlainText);

        Assert.NotNull(preset);
        Assert.Equal(ConversionPreset.Generic, preset.Type);
    }

    [Fact]
    public void CreateCustomPreset_CreatesValidPreset()
    {
        var config = new ConversionConfig
        {
            Preset = ConversionPreset.Custom,
            TargetDuration = TimeSpan.FromMinutes(5),
            WordsPerMinute = 150,
            AggressivenessLevel = 0.7
        };

        var preset = ConversionPresets.CreateCustomPreset(
            "My Custom Preset",
            "A custom conversion strategy",
            config
        );

        Assert.NotNull(preset);
        Assert.Equal(ConversionPreset.Custom, preset.Type);
        Assert.Equal("My Custom Preset", preset.Name);
        Assert.Equal(config, preset.DefaultConfig);
    }

    [Fact]
    public void AllPresets_HaveValidConfigurations()
    {
        var presets = ConversionPresets.GetAllPresets();

        foreach (var preset in presets)
        {
            Assert.NotNull(preset.Name);
            Assert.NotEmpty(preset.Name);
            Assert.NotNull(preset.Description);
            Assert.NotNull(preset.DefaultConfig);
            Assert.True(preset.DefaultConfig.TargetDuration.TotalMinutes > 0);
            Assert.True(preset.DefaultConfig.WordsPerMinute > 0);
            Assert.True(preset.DefaultConfig.AggressivenessLevel >= 0 && preset.DefaultConfig.AggressivenessLevel <= 1);
            Assert.NotNull(preset.RestructuringStrategy);
        }
    }

    [Fact]
    public void AllPresets_HaveUniqueNames()
    {
        var presets = ConversionPresets.GetAllPresets();
        var names = presets.Select(p => p.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }
}
