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
    private readonly Mock<ILogger<ContentAdaptationEngine>> _mockLogger;
    private readonly Mock<ILogger<VocabularyLevelAdjuster>> _mockVocabLogger;
    private readonly Mock<ILogger<ExamplePersonalizationService>> _mockExampleLogger;
    private readonly Mock<ILogger<PacingAdaptationService>> _mockPacingLogger;
    private readonly Mock<ILogger<ToneAndFormalityOptimizer>> _mockToneLogger;
    private readonly Mock<ILogger<CognitiveLoadBalancer>> _mockLoadLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly ContentAdaptationEngine _engine;

    public ContentAdaptationEngineTests()
    {
        _mockLogger = new Mock<ILogger<ContentAdaptationEngine>>();
        _mockVocabLogger = new Mock<ILogger<VocabularyLevelAdjuster>>();
        _mockExampleLogger = new Mock<ILogger<ExamplePersonalizationService>>();
        _mockPacingLogger = new Mock<ILogger<PacingAdaptationService>>();
        _mockToneLogger = new Mock<ILogger<ToneAndFormalityOptimizer>>();
        _mockLoadLogger = new Mock<ILogger<CognitiveLoadBalancer>>();
        _mockLlmProvider = new Mock<ILlmProvider>();

        var vocabAdjuster = new VocabularyLevelAdjuster(_mockVocabLogger.Object, _mockLlmProvider.Object);
        var examplePersonalizer = new ExamplePersonalizationService(_mockExampleLogger.Object, _mockLlmProvider.Object);
        var pacingAdapter = new PacingAdaptationService(_mockPacingLogger.Object, _mockLlmProvider.Object);
        var toneOptimizer = new ToneAndFormalityOptimizer(_mockToneLogger.Object, _mockLlmProvider.Object);
        var loadBalancer = new CognitiveLoadBalancer(_mockLoadLogger.Object, _mockLlmProvider.Object);

        _engine = new ContentAdaptationEngine(
            _mockLogger.Object,
            _mockLlmProvider.Object,
            vocabAdjuster,
            examplePersonalizer,
            pacingAdapter,
            toneOptimizer,
            loadBalancer);
    }

    [Fact]
    public async Task AdaptContentAsync_ValidRequest_ReturnsAdaptationResult()
    {
        // Arrange
        var content = "This is a test video script about machine learning algorithms.";
        var profile = new AudienceProfile
        {
            EducationLevel = EducationLevel.Undergraduate,
            ExpertiseLevel = ExpertiseLevel.Intermediate,
            AgeRange = AgeRange.Adult25to34
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Adapted content for the target audience.");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AdaptedContent);
        Assert.NotNull(result.OriginalContent);
        Assert.Equal(content, result.OriginalContent);
        Assert.NotEmpty(result.Changes);
        Assert.NotNull(result.OriginalMetrics);
        Assert.NotNull(result.AdaptedMetrics);
        Assert.True(result.QualityScore >= 0 && result.QualityScore <= 100);
    }

    [Fact]
    public async Task AdaptContentAsync_HighSchoolAudience_AdjustsVocabularyAppropriately()
    {
        // Arrange
        var content = "Complex algorithms utilize sophisticated mathematical constructs.";
        var profile = new AudienceProfile
        {
            EducationLevel = EducationLevel.HighSchool,
            ExpertiseLevel = ExpertiseLevel.Novice,
            PrefersTechnicalLanguage = false
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile,
                Aggressiveness = AdaptationAggressiveness.Aggressive
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Simple programs use basic math concepts.");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Changes, c => c.Type == AdaptationChangeType.VocabularySimplification);
    }

    [Fact]
    public async Task AdaptContentAsync_ExpertAudience_IncreasesTechnicalComplexity()
    {
        // Arrange
        var content = "We use computers to solve problems.";
        var profile = new AudienceProfile
        {
            EducationLevel = EducationLevel.Expert,
            ExpertiseLevel = ExpertiseLevel.Expert,
            PrefersTechnicalLanguage = true,
            ProfessionalDomain = "Computer Science"
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("We leverage computational systems to address algorithmic challenges.");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AdaptedMetrics!.VocabularyComplexity >= result.OriginalMetrics!.VocabularyComplexity);
    }

    [Fact]
    public async Task AdaptContentAsync_WithPersonalization_CustomizesExamples()
    {
        // Arrange
        var content = "Understanding data structures is important.";
        var profile = new AudienceProfile
        {
            ProfessionalDomain = "Healthcare",
            GeographicRegion = "North America"
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile,
                PersonalizeExamples = true
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Understanding patient records management is important.");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Changes, c => c.Type == AdaptationChangeType.ExamplePersonalization);
    }

    [Fact]
    public async Task AdaptContentAsync_BeginnerAudience_IncreasesExplanationLength()
    {
        // Arrange
        var content = "AI uses neural networks.";
        var profile = new AudienceProfile
        {
            ExpertiseLevel = ExpertiseLevel.Novice,
            CognitiveLoadCapacity = 40
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile,
                AdjustPacing = true
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Artificial Intelligence, which is like teaching computers to think, uses something called neural networks. Neural networks are systems that work similarly to how our brains work.");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AdaptedContent.Length > result.OriginalContent.Length);
        Assert.Contains(result.Changes, c => c.Type == AdaptationChangeType.PacingAdjustment);
    }

    [Fact]
    public async Task AdaptContentAsync_CasualFormality_AdjustsTone()
    {
        // Arrange
        var content = "One must carefully consider the implications.";
        var profile = new AudienceProfile
        {
            FormalityLevel = FormalityLevel.Casual,
            EnergyLevel = EnergyLevel.High,
            AgeRange = AgeRange.YoungAdult18to24
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile,
                AdjustTone = true
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("You've gotta think about what this means!");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Changes, c => c.Type == AdaptationChangeType.ToneAdjustment);
    }

    [Fact]
    public async Task AdaptContentAsync_CalculatesReadabilityMetrics()
    {
        // Arrange
        var content = "The quick brown fox jumps over the lazy dog. This sentence has simple words.";
        var profile = new AudienceProfile();

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result.OriginalMetrics);
        Assert.True(result.OriginalMetrics.FleschKincaidGrade >= 0);
        Assert.True(result.OriginalMetrics.SmogIndex >= 0);
        Assert.True(result.OriginalMetrics.AverageSentenceLength > 0);
        Assert.True(result.OriginalMetrics.CognitiveLoad >= 0 && result.OriginalMetrics.CognitiveLoad <= 100);
    }

    [Fact]
    public async Task AdaptContentAsync_CompletesWithinTimeLimit()
    {
        // Arrange
        var content = "Short test content.";
        var profile = new AudienceProfile();
        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig { AudienceProfile = profile }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AdaptationTime.TotalSeconds < 15, "Adaptation should complete quickly for short content");
    }

    [Fact]
    public async Task AdaptContentAsync_WithCognitiveLoadBalancing_AdjustsComplexity()
    {
        // Arrange
        var content = "Complex abstract theoretical frameworks.";
        var profile = new AudienceProfile
        {
            CognitiveLoadCapacity = 30,
            LearningStyle = LearningStyle.Visual
        };

        var request = new AdaptationRequest
        {
            Content = content,
            Config = new AdaptationConfig
            {
                AudienceProfile = profile,
                BalanceCognitiveLoad = true
            }
        };

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Simple ideas broken into steps.");

        // Act
        var result = await _engine.AdaptContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Changes, c => c.Type == AdaptationChangeType.ComplexityReduction);
    }
}
