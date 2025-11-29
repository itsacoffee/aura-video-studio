using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.StockMedia;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.Visual;

/// <summary>
/// Unit tests for intelligent Pexels scene matching services.
/// </summary>
public class PexelsSceneMatchingServiceTests
{
    private readonly Mock<ILogger<VisualKeywordExtractor>> _keywordExtractorLoggerMock;
    private readonly Mock<ILogger<PexelsSceneMatchingService>> _matchingServiceLoggerMock;
    private readonly VisualKeywordExtractor _keywordExtractor;
    private readonly PexelsSceneMatchingService _matchingService;

    public PexelsSceneMatchingServiceTests()
    {
        _keywordExtractorLoggerMock = new Mock<ILogger<VisualKeywordExtractor>>();
        _matchingServiceLoggerMock = new Mock<ILogger<PexelsSceneMatchingService>>();
        _keywordExtractor = new VisualKeywordExtractor(_keywordExtractorLoggerMock.Object);
        _matchingService = new PexelsSceneMatchingService(
            _matchingServiceLoggerMock.Object,
            _keywordExtractor,
            PexelsMatchingConfig.Default);
    }

    #region VisualKeywordExtractor Tests

    [Fact]
    public void ExtractKeywords_FromHeading_ReturnsRelevantKeywords()
    {
        // Arrange
        var heading = "The Future of Artificial Intelligence in Healthcare";
        
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(heading, null, maxKeywords: 5);
        
        // Assert
        Assert.NotEmpty(keywords);
        Assert.Contains("healthcare", keywords, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("artificial", keywords, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractKeywords_FiltersStopWords()
    {
        // Arrange
        var heading = "The quick brown fox jumps over the lazy dog";
        
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(heading, null, maxKeywords: 10);
        
        // Assert
        Assert.DoesNotContain("the", keywords, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("over", keywords, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractKeywords_BoostsVisualTerms()
    {
        // Arrange
        var heading = "Modern landscape photography techniques";
        
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(heading, null, maxKeywords: 3, visualTermBoost: 2.0);
        
        // Assert
        // Visual terms like "landscape" and "modern" should be prioritized
        Assert.NotEmpty(keywords);
        Assert.True(
            keywords.Any(k => k.Equals("landscape", StringComparison.OrdinalIgnoreCase) ||
                              k.Equals("modern", StringComparison.OrdinalIgnoreCase)),
            "Should include visual terms like 'landscape' or 'modern'");
    }

    [Fact]
    public void ExtractKeywords_FromNarration_ConsidersFrequency()
    {
        // Arrange
        var narration = "Technology is changing healthcare. Healthcare innovation is driven by technology. Technology enables better healthcare outcomes.";
        
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(null, narration, maxKeywords: 3);
        
        // Assert
        Assert.NotEmpty(keywords);
        // "technology" and "healthcare" appear multiple times, should be prioritized
        Assert.Contains("technology", keywords, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("healthcare", keywords, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractKeywords_CombinesHeadingAndNarration()
    {
        // Arrange
        var heading = "Digital Transformation";
        var narration = "Companies are adopting cloud computing and artificial intelligence to modernize operations.";
        
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(heading, narration, maxKeywords: 5);
        
        // Assert
        Assert.NotEmpty(keywords);
        // Should include words from both heading and narration
        Assert.True(keywords.Count >= 3, "Should extract at least 3 keywords from combined text");
    }

    [Fact]
    public void ExtractKeywords_HandlesEmptyInput()
    {
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(null, null, maxKeywords: 5);
        
        // Assert
        Assert.Empty(keywords);
    }

    [Fact]
    public void ExtractKeywords_RespectsMaxKeywordsLimit()
    {
        // Arrange
        var heading = "Introduction to Machine Learning Deep Learning Neural Networks Computer Vision Natural Language Processing";
        
        // Act
        var keywords = _keywordExtractor.ExtractKeywords(heading, null, maxKeywords: 3);
        
        // Assert
        Assert.True(keywords.Count <= 3, "Should not exceed maxKeywords limit");
    }

    [Fact]
    public void BuildSearchQuery_CombinesKeywordsAndStyle()
    {
        // Arrange
        var keywords = new[] { "technology", "healthcare", "innovation" };
        var style = "professional";
        
        // Act
        var query = _keywordExtractor.BuildSearchQuery(keywords, heading: null, style: style);
        
        // Assert
        Assert.NotEmpty(query);
        Assert.Contains("professional", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildSearchQuery_IncludesHeadingKeyword()
    {
        // Arrange
        var keywords = new[] { "technology", "business" };
        var heading = "AI in Healthcare";
        
        // Act
        var query = _keywordExtractor.BuildSearchQuery(keywords, heading: heading, style: null);
        
        // Assert
        Assert.NotEmpty(query);
        Assert.True(
            query.Contains("healthcare", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("technology", StringComparison.OrdinalIgnoreCase),
            "Query should include heading or keyword terms");
    }

    [Fact]
    public void BuildSearchQuery_LimitsQueryLength()
    {
        // Arrange
        var keywords = new[] { "one", "two", "three", "four", "five", "six", "seven" };
        
        // Act
        var query = _keywordExtractor.BuildSearchQuery(keywords, heading: "extra", style: "style", maxKeywords: 3);
        
        // Assert
        var wordCount = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(wordCount <= 4, "Query should be limited to maxKeywords plus potential style/heading");
    }

    [Theory]
    [InlineData(16, 9, "landscape")]
    [InlineData(9, 16, "portrait")]
    [InlineData(1, 1, "square")]
    public void GetOrientationFromAspect_ReturnsCorrectOrientation(int width, int height, string expected)
    {
        // Act
        var result = VisualKeywordExtractor.GetOrientationFromAspect(width, height);
        
        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region PexelsSceneMatchingService Tests

    [Fact]
    public void ScoreResults_AssignsScoresBasedOnKeywordMatches()
    {
        // Arrange
        var results = new List<StockMediaResult>
        {
            CreateStockMediaResult("1", new Dictionary<string, string>
            {
                ["photographer"] = "healthcare technology expert",
                ["alt"] = "medical technology image"
            }),
            CreateStockMediaResult("2", new Dictionary<string, string>
            {
                ["photographer"] = "nature photographer",
                ["alt"] = "landscape sunset"
            })
        };
        var keywords = new[] { "healthcare", "technology", "medical" };
        
        // Act
        var scored = _matchingService.ScoreResults(results, keywords, "Healthcare Technology", null);
        
        // Assert
        Assert.Equal(2, scored.Count);
        // First result should score higher due to keyword matches
        Assert.True(scored[0].RelevanceScore.Score >= scored[1].RelevanceScore.Score);
        Assert.True(scored[0].RelevanceScore.MatchedKeywords.Count > 0);
    }

    [Fact]
    public void ScoreResults_MeetsThreshold_WhenScoreSufficient()
    {
        // Arrange
        var results = new List<StockMediaResult>
        {
            CreateStockMediaResult("1", new Dictionary<string, string>
            {
                ["photographer"] = "business professional meeting corporate"
            })
        };
        var keywords = new[] { "business", "professional", "corporate" };
        
        // Act
        var scored = _matchingService.ScoreResults(results, keywords, "Business Meeting", "professional");
        
        // Assert
        Assert.Single(scored);
        Assert.True(scored[0].RelevanceScore.MeetsThreshold, "Score should meet default threshold of 60");
    }

    [Fact]
    public void ScoreResults_GeneratesReasoning()
    {
        // Arrange
        var results = new List<StockMediaResult>
        {
            CreateStockMediaResult("1", new Dictionary<string, string>
            {
                ["photographer"] = "technology innovation"
            })
        };
        var keywords = new[] { "technology" };
        
        // Act
        var scored = _matchingService.ScoreResults(results, keywords, "Tech Innovation", null);
        
        // Assert
        Assert.Single(scored);
        Assert.NotEmpty(scored[0].RelevanceScore.Reasoning);
        Assert.Contains("Keyword match", scored[0].RelevanceScore.Reasoning);
    }

    [Fact]
    public async Task FindMatchingImagesAsync_ReturnsFilteredResults()
    {
        // Arrange
        var scene = new Scene(
            Index: 0,
            Heading: "AI in Healthcare",
            Script: "Artificial intelligence is revolutionizing healthcare delivery.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(10));

        var mockResults = new List<StockMediaResult>
        {
            CreateStockMediaResult("1", new Dictionary<string, string>
            {
                ["photographer"] = "healthcare ai technology"
            }),
            CreateStockMediaResult("2", new Dictionary<string, string>
            {
                ["photographer"] = "unrelated content"
            })
        };

        Task<List<StockMediaResult>> SearchFunc(StockMediaSearchRequest req, CancellationToken ct)
        {
            return Task.FromResult(mockResults);
        }
        
        // Act
        var results = await _matchingService.FindMatchingImagesAsync(
            scene,
            Aspect.Widescreen16x9,
            "professional",
            SearchFunc);
        
        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.RelevanceScore.MeetsThreshold));
    }

    [Fact]
    public async Task FindMatchingImagesAsync_UsesFallback_WhenNoResults()
    {
        // Arrange
        var scene = new Scene(
            Index: 0,
            Heading: "Very Obscure Topic",
            Script: "Content that won't match anything.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));

        var callCount = 0;
        Task<List<StockMediaResult>> SearchFunc(StockMediaSearchRequest req, CancellationToken ct)
        {
            callCount++;
            // Return empty on first call (intelligent search), return results on second (fallback)
            if (callCount == 1)
            {
                return Task.FromResult(new List<StockMediaResult>());
            }
            return Task.FromResult(new List<StockMediaResult>
            {
                CreateStockMediaResult("fallback-1", new Dictionary<string, string>())
            });
        }
        
        // Act
        var results = await _matchingService.FindMatchingImagesAsync(
            scene,
            Aspect.Widescreen16x9,
            null,
            SearchFunc);
        
        // Assert
        Assert.Equal(2, callCount); // Should have called twice: intelligent + fallback
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task FindMatchingImagesAsync_AppliesOrientationFilter()
    {
        // Arrange
        var scene = new Scene(
            Index: 0,
            Heading: "Portrait Video Content",
            Script: "Content for vertical video.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));

        StockMediaSearchRequest? capturedRequest = null;
        Task<List<StockMediaResult>> SearchFunc(StockMediaSearchRequest req, CancellationToken ct)
        {
            capturedRequest = req;
            return Task.FromResult(new List<StockMediaResult>
            {
                CreateStockMediaResult("1", new Dictionary<string, string>())
            });
        }
        
        // Act
        await _matchingService.FindMatchingImagesAsync(
            scene,
            Aspect.Vertical9x16,
            null,
            SearchFunc);
        
        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("portrait", capturedRequest!.Orientation);
    }

    [Fact]
    public async Task FindMatchingImagesAsync_DisabledSemanticMatching_UsesBasicSearch()
    {
        // Arrange
        var config = PexelsMatchingConfig.Minimal;
        var service = new PexelsSceneMatchingService(
            _matchingServiceLoggerMock.Object,
            _keywordExtractor,
            config);

        var scene = new Scene(
            Index: 0,
            Heading: "Test Scene",
            Script: "Test content.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));

        StockMediaSearchRequest? capturedRequest = null;
        Task<List<StockMediaResult>> SearchFunc(StockMediaSearchRequest req, CancellationToken ct)
        {
            capturedRequest = req;
            return Task.FromResult(new List<StockMediaResult>
            {
                CreateStockMediaResult("1", new Dictionary<string, string>())
            });
        }
        
        // Act
        var results = await service.FindMatchingImagesAsync(
            scene,
            Aspect.Widescreen16x9,
            null,
            SearchFunc);
        
        // Assert
        Assert.NotNull(capturedRequest);
        // Should use the heading directly without intelligent query building
        Assert.Equal("Test Scene", capturedRequest!.Query);
        Assert.NotEmpty(results);
    }

    #endregion

    #region Helper Methods

    private static StockMediaResult CreateStockMediaResult(string id, Dictionary<string, string> metadata)
    {
        return new StockMediaResult
        {
            Id = id,
            Type = StockMediaType.Image,
            Provider = StockMediaProvider.Pexels,
            ThumbnailUrl = $"https://example.com/thumb/{id}.jpg",
            PreviewUrl = $"https://example.com/preview/{id}.jpg",
            FullSizeUrl = $"https://example.com/full/{id}.jpg",
            Width = 1920,
            Height = 1080,
            Licensing = new Aura.Core.Models.Assets.AssetLicensingInfo
            {
                LicenseType = "Pexels License",
                CommercialUseAllowed = true,
                AttributionRequired = false,
                SourcePlatform = "Pexels"
            },
            Metadata = metadata
        };
    }

    #endregion
}
