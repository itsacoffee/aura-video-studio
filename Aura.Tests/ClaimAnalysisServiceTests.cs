using System.Collections.Generic;
using Aura.Core.Models.RAG;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for ClaimAnalysisService
/// </summary>
public class ClaimAnalysisServiceTests
{
    private readonly Mock<ILogger<ClaimAnalysisService>> _loggerMock;
    private readonly ClaimAnalysisService _service;

    public ClaimAnalysisServiceTests()
    {
        _loggerMock = new Mock<ILogger<ClaimAnalysisService>>();
        _service = new ClaimAnalysisService(_loggerMock.Object);
    }

    [Fact]
    public void ExtractVerifiableClaims_WithPercentages_ExtractsStatisticClaims()
    {
        // Arrange
        var ragContext = CreateRagContextWithContent(
            "According to the survey, 75% of users prefer dark mode. This has increased significantly.");

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.NotEmpty(claims);
        Assert.Contains(claims, c => c.Statement.Contains("75%"));
        Assert.Contains(claims, c => c.ClaimType == ClaimType.Statistic);
    }

    [Fact]
    public void ExtractVerifiableClaims_WithResearchFindings_ExtractsResearchClaims()
    {
        // Arrange
        var ragContext = CreateRagContextWithContent(
            "Research found that early adopters are more likely to share feedback. The study showed significant correlation.");

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.NotEmpty(claims);
        Assert.Contains(claims, c => c.ClaimType == ClaimType.ResearchFinding);
    }

    [Fact]
    public void ExtractVerifiableClaims_WithNumbersAndScale_ExtractsFactualClaims()
    {
        // Arrange
        var ragContext = CreateRagContextWithContent(
            "The market reached 5 billion dollars in 2023. Companies are investing 2 million in AI research.");

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.NotEmpty(claims);
        Assert.Contains(claims, c => c.Statement.Contains("billion") || c.Statement.Contains("million"));
    }

    [Fact]
    public void ExtractVerifiableClaims_WithQuantitativeChanges_ExtractsFactualClaims()
    {
        // Arrange
        var ragContext = CreateRagContextWithContent(
            "Revenue increased by 30% year over year. User engagement declined by 15% in the last quarter.");

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.NotEmpty(claims);
        Assert.True(claims.Count >= 2, "Should extract at least two claims");
    }

    [Fact]
    public void ExtractVerifiableClaims_WithNoFactualContent_ReturnsEmptyList()
    {
        // Arrange
        var ragContext = CreateRagContextWithContent(
            "Welcome to our documentation. Please read carefully. Have a nice day.");

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.Empty(claims);
    }

    [Fact]
    public void ExtractVerifiableClaims_PreservesCitationNumber()
    {
        // Arrange
        var ragContext = new RagContext
        {
            Query = "test",
            Chunks = new List<ContextChunk>
            {
                new ContextChunk
                {
                    Content = "Studies found that 80% of developers use version control.",
                    Source = "developer-survey.pdf",
                    CitationNumber = 3,
                    RelevanceScore = 0.9f
                }
            },
            FormattedContext = "Test context",
            TotalTokens = 50
        };

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.NotEmpty(claims);
        Assert.All(claims, claim => Assert.Equal(3, claim.CitationNumber));
    }

    [Fact]
    public void ExtractVerifiableClaims_WithMultipleChunks_ExtractsFromAll()
    {
        // Arrange
        var ragContext = new RagContext
        {
            Query = "test",
            Chunks = new List<ContextChunk>
            {
                new ContextChunk
                {
                    Content = "Survey found that 60% agree.",
                    Source = "survey.pdf",
                    CitationNumber = 1,
                    RelevanceScore = 0.9f
                },
                new ContextChunk
                {
                    Content = "Research showed a 25% increase.",
                    Source = "research.pdf",
                    CitationNumber = 2,
                    RelevanceScore = 0.85f
                }
            },
            FormattedContext = "Test context",
            TotalTokens = 100
        };

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.True(claims.Count >= 2, "Should extract claims from both chunks");
        Assert.Contains(claims, c => c.CitationNumber == 1);
        Assert.Contains(claims, c => c.CitationNumber == 2);
    }

    [Fact]
    public void FormatClaimsForPrompt_WithClaims_FormatsCorrectly()
    {
        // Arrange
        var claims = new List<VerifiableClaim>
        {
            new VerifiableClaim
            {
                Statement = "75% of users prefer dark mode",
                CitationNumber = 1,
                ClaimType = ClaimType.Statistic
            },
            new VerifiableClaim
            {
                Statement = "Research found significant improvement",
                CitationNumber = 2,
                ClaimType = ClaimType.ResearchFinding
            }
        };

        // Act
        var result = _service.FormatClaimsForPrompt(claims);

        // Assert
        Assert.Contains("# Pre-Verified Claims You Can Use", result);
        Assert.Contains("75% of users prefer dark mode", result);
        Assert.Contains("[Citation 1]", result);
        Assert.Contains("[Citation 2]", result);
    }

    [Fact]
    public void FormatClaimsForPrompt_WithEmptyClaims_ReturnsEmptyString()
    {
        // Arrange
        var claims = new List<VerifiableClaim>();

        // Act
        var result = _service.FormatClaimsForPrompt(claims);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatClaimsForPrompt_WithNullClaims_ReturnsEmptyString()
    {
        // Act
        var result = _service.FormatClaimsForPrompt(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatClaimsForPrompt_RespectsMaxClaimsLimit()
    {
        // Arrange
        var claims = new List<VerifiableClaim>();
        for (int i = 1; i <= 15; i++)
        {
            claims.Add(new VerifiableClaim
            {
                Statement = $"Claim number {i} with 50% statistic",
                CitationNumber = i,
                ClaimType = ClaimType.Statistic
            });
        }

        // Act
        var result = _service.FormatClaimsForPrompt(claims, maxClaims: 5);

        // Assert
        // Should only contain first 5 claims
        Assert.Contains("[Citation 1]", result);
        Assert.Contains("[Citation 5]", result);
        Assert.DoesNotContain("[Citation 6]", result);
        Assert.DoesNotContain("[Citation 15]", result);
    }

    [Fact]
    public void ExtractVerifiableClaims_WithAttributions_ExtractsGeneralFacts()
    {
        // Arrange
        var ragContext = CreateRagContextWithContent(
            "According to industry experts, the technology is evolving rapidly. According to the report, adoption rates are high.");

        // Act
        var claims = _service.ExtractVerifiableClaims(ragContext);

        // Assert
        Assert.NotEmpty(claims);
        Assert.Contains(claims, c => c.Statement.Contains("According to"));
    }

    private static RagContext CreateRagContextWithContent(string content)
    {
        return new RagContext
        {
            Query = "test",
            Chunks = new List<ContextChunk>
            {
                new ContextChunk
                {
                    Content = content,
                    Source = "test-document.pdf",
                    CitationNumber = 1,
                    RelevanceScore = 0.9f
                }
            },
            FormattedContext = content,
            TotalTokens = content.Length / 4
        };
    }
}
