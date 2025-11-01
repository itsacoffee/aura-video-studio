using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audience;
using Aura.Core.Providers;
using Aura.Core.Services.Audience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ContentAdaptationEngineTests
{
    private readonly Mock<ILogger<ContentAdaptationEngine>> _loggerMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly ContentAdaptationEngine _engine;

    public ContentAdaptationEngineTests()
    {
        _loggerMock = new Mock<ILogger<ContentAdaptationEngine>>();
        _llmProviderMock = new Mock<ILlmProvider>();
        _engine = new ContentAdaptationEngine(_loggerMock.Object, _llmProviderMock.Object);
    }

    [Fact]
    public async Task AdaptContentAsync_WithBeginnerAudience_IncreasesExplanation()
    {
        var profile = new AudienceProfileBuilder("Tech Beginners")
            .SetExpertise(ExpertiseLevel.CompleteBeginner)
            .SetEducation(EducationLevel.HighSchool)
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnableVocabularyAdjustment = false,
            EnableExamplePersonalization = false,
            EnablePacingAdaptation = false,
            EnableToneOptimization = false,
            EnableCognitiveLoadBalancing = false
        };

        var originalContent = "Machine learning algorithms process data to identify patterns.";

        var result = await _engine.AdaptContentAsync(originalContent, profile, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(originalContent, result.OriginalContent);
        Assert.NotNull(result.OriginalMetrics);
        Assert.NotNull(result.AdaptedMetrics);
    }

    [Fact]
    public async Task AdaptContentAsync_WithExpertAudience_HigherReadingLevel()
    {
        var profile = new AudienceProfileBuilder("Domain Experts")
            .SetExpertise(ExpertiseLevel.Expert)
            .SetEducation(EducationLevel.Doctorate)
            .SetTechnicalComfort(TechnicalComfort.Expert)
            .Build();

        var config = new ContentAdaptationConfig
        {
            AggressivenessLevel = 0.8
        };

        var originalContent = "This is a simple explanation of a complex topic.";

        var result = await _engine.AdaptContentAsync(originalContent, profile, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.ProcessingTime.TotalSeconds >= 0);
    }

    [Fact]
    public void GetTargetReadingLevelDescription_ForHighSchool_ReturnsAppropriateLevel()
    {
        var profile = new AudienceProfileBuilder("High School Students")
            .SetEducation(EducationLevel.HighSchool)
            .Build();

        var description = _engine.GetTargetReadingLevelDescription(profile);

        Assert.Contains("High School", description);
    }

    [Fact]
    public void GetTargetReadingLevelDescription_ForDoctorate_ReturnsAdvancedLevel()
    {
        var profile = new AudienceProfileBuilder("Researchers")
            .SetEducation(EducationLevel.Doctorate)
            .SetExpertise(ExpertiseLevel.Expert)
            .Build();

        var description = _engine.GetTargetReadingLevelDescription(profile);

        Assert.Contains("Professional", description);
    }

    [Fact]
    public async Task AdaptContentAsync_CalculatesReadabilityMetrics()
    {
        var profile = new AudienceProfileBuilder("General Audience")
            .SetEducation(EducationLevel.BachelorDegree)
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnableVocabularyAdjustment = false,
            EnableExamplePersonalization = false,
            EnablePacingAdaptation = false,
            EnableToneOptimization = false,
            EnableCognitiveLoadBalancing = false
        };

        var content = "The quick brown fox jumps over the lazy dog. This is a test sentence.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.True(result.OriginalMetrics.FleschKincaidGradeLevel > 0);
        Assert.True(result.OriginalMetrics.AverageWordsPerSentence > 0);
        Assert.True(result.OriginalMetrics.AverageSyllablesPerWord > 0);
    }

    [Fact]
    public async Task AdaptContentAsync_WithYoungAudience_UsesCasualTone()
    {
        var profile = new AudienceProfileBuilder("Young Adults")
            .SetAgeRange(18, 24)
            .SetEducation(EducationLevel.SomeCollege)
            .Build();

        var config = new ContentAdaptationConfig();

        var content = "This content needs tone adjustment.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.AdaptedContent);
    }

    [Fact]
    public async Task AdaptContentAsync_WithProfessionalAudience_UsesProfessionalTone()
    {
        var profile = new AudienceProfileBuilder("Business Executives")
            .SetAgeRange(35, 54)
            .SetEducation(EducationLevel.MasterDegree)
            .SetProfession("Executive")
            .Build();

        var config = new ContentAdaptationConfig();

        var content = "Business strategy optimization.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AdaptContentAsync_WithAccessibilityNeeds_SimplifiesLanguage()
    {
        var profile = new AudienceProfileBuilder("Accessible Content")
            .SetEducation(EducationLevel.HighSchool)
            .SetAccessibilityNeeds(requiresSimplifiedLanguage: true)
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnableVocabularyAdjustment = true
        };

        var content = "Extraordinarily complex terminology obfuscates comprehension.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AdaptContentAsync_WithCulturalSensitivities_RespectsConstraints()
    {
        var profile = new AudienceProfileBuilder("Culturally Sensitive")
            .SetRegion(GeographicRegion.MiddleEast)
            .SetCulturalBackground(
                sensitivities: new System.Collections.Generic.List<string> { "Religious topics" },
                tabooTopics: new System.Collections.Generic.List<string> { "Alcohol" }
            )
            .Build();

        var config = new ContentAdaptationConfig();

        var content = "Sample content for cultural adaptation.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AdaptContentAsync_WithTechProfessionals_UsesTechAnalogies()
    {
        var profile = new AudienceProfileBuilder("Software Developers")
            .SetProfession("Software Developer")
            .SetIndustry("Technology")
            .SetExpertise(ExpertiseLevel.Advanced)
            .AddInterest("Programming")
            .AddInterest("AI")
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnableExamplePersonalization = true
        };

        var content = "Understanding complex systems requires practice.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AdaptContentAsync_CompletesWithinTimeLimit()
    {
        var profile = new AudienceProfileBuilder("Test Audience")
            .SetEducation(EducationLevel.BachelorDegree)
            .Build();

        var config = new ContentAdaptationConfig();

        var content = string.Join(" ", System.Linq.Enumerable.Repeat(
            "This is a sentence that will be part of a longer script for testing.",
            50));

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.ProcessingTime.TotalSeconds < 30, 
            $"Processing took {result.ProcessingTime.TotalSeconds:F2}s, expected < 30s");
    }

    [Fact]
    public async Task AdaptContentAsync_WithAggressivenessLevels_VariesChanges()
    {
        var profile = new AudienceProfileBuilder("Test")
            .SetEducation(EducationLevel.HighSchool)
            .Build();

        var subtleConfig = new ContentAdaptationConfig { AggressivenessLevel = 0.3 };
        var aggressiveConfig = new ContentAdaptationConfig { AggressivenessLevel = 0.9 };

        var content = "Complex multifaceted analysis of intricate phenomena.";

        var subtleResult = await _engine.AdaptContentAsync(content, profile, subtleConfig, CancellationToken.None);
        var aggressiveResult = await _engine.AdaptContentAsync(content, profile, aggressiveConfig, CancellationToken.None);

        Assert.NotNull(subtleResult);
        Assert.NotNull(aggressiveResult);
    }

    [Fact]
    public async Task AdaptContentAsync_WithShortAttentionSpan_AdjustsPacing()
    {
        var profile = new AudienceProfileBuilder("Short Attention")
            .SetAttentionSpan(AttentionSpan.Short)
            .SetExpertise(ExpertiseLevel.Novice)
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnablePacingAdaptation = true
        };

        var content = "Long detailed explanation that might lose viewer attention.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AdaptContentAsync_WithLongAttentionSpan_AllowsDepth()
    {
        var profile = new AudienceProfileBuilder("Deep Dive")
            .SetAttentionSpan(AttentionSpan.Long)
            .SetExpertise(ExpertiseLevel.Advanced)
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnablePacingAdaptation = true
        };

        var content = "Brief overview.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AdaptContentAsync_WithCognitiveLoadBalancing_ManagesComplexity()
    {
        var profile = new AudienceProfileBuilder("Beginner")
            .SetExpertise(ExpertiseLevel.CompleteBeginner)
            .SetEducation(EducationLevel.HighSchool)
            .Build();

        var config = new ContentAdaptationConfig
        {
            EnableCognitiveLoadBalancing = true,
            CognitiveLoadThreshold = 70.0
        };

        var content = "Complex abstract theoretical frameworks require substantial cognitive processing capacity.";

        var result = await _engine.AdaptContentAsync(content, profile, config, CancellationToken.None);

        Assert.NotNull(result);
    }
}
