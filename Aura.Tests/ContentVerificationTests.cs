using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentVerification;
using Aura.Core.Services.ContentVerification;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ContentVerificationTests
{
    [Fact]
    public async Task FactCheckingService_CheckClaim_ReturnsResult()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FactCheckingService>>();
        var service = new FactCheckingService(mockLogger.Object);
        
        var claim = new Claim(
            ClaimId: "test-claim-1",
            Text: "The Earth orbits the Sun",
            Context: "Solar system facts",
            StartPosition: 0,
            EndPosition: 100,
            Type: ClaimType.Scientific,
            ExtractionConfidence: 0.9
        );

        // Act
        var result = await service.CheckClaimAsync(claim, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(claim.ClaimId, result.ClaimId);
        Assert.Equal(claim.Text, result.Claim);
        Assert.True(result.ConfidenceScore >= 0 && result.ConfidenceScore <= 1);
        Assert.NotNull(result.Evidence);
    }

    [Fact]
    public async Task ConfidenceAnalysisService_AnalyzeConfidence_ReturnsAnalysis()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConfidenceAnalysisService>>();
        var service = new ConfidenceAnalysisService(mockLogger.Object);
        
        var claims = new List<Claim>
        {
            new Claim("claim1", "Test claim 1", "context", 0, 100, ClaimType.Factual, 0.8),
            new Claim("claim2", "Test claim 2", "context", 100, 200, ClaimType.Statistical, 0.9)
        };

        var factChecks = new List<FactCheckResult>
        {
            new FactCheckResult("claim1", "Test claim 1", VerificationStatus.Verified, 0.85, new List<Evidence>(), null, DateTime.UtcNow),
            new FactCheckResult("claim2", "Test claim 2", VerificationStatus.PartiallyVerified, 0.7, new List<Evidence>(), null, DateTime.UtcNow)
        };

        // Act
        var result = await service.AnalyzeConfidenceAsync("content1", claims, factChecks, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("content1", result.ContentId);
        Assert.True(result.OverallConfidence > 0);
        Assert.Equal(2, result.ClaimConfidences.Count);
    }

    [Fact]
    public async Task MisinformationDetectionService_DetectMisinformation_ReturnsDetection()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MisinformationDetectionService>>();
        var service = new MisinformationDetectionService(mockLogger.Object);
        
        var content = "This is always true and everyone knows it.";
        var claims = new List<Claim>
        {
            new Claim("claim1", content, "context", 0, 100, ClaimType.Opinion, 0.7)
        };
        var factChecks = new List<FactCheckResult>();

        // Act
        var result = await service.DetectMisinformationAsync("content1", content, claims, factChecks, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("content1", result.ContentId);
        Assert.NotNull(result.Flags);
        Assert.NotNull(result.Recommendations);
    }

    [Fact]
    public async Task ContentVerificationOrchestrator_VerifyContent_ReturnsResult()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ContentVerificationOrchestrator>>();
        var mockFactCheckingLogger = new Mock<ILogger<FactCheckingService>>();
        var mockSourceLogger = new Mock<ILogger<SourceAttributionService>>();
        var mockConfidenceLogger = new Mock<ILogger<ConfidenceAnalysisService>>();
        var mockMisinfoLogger = new Mock<ILogger<MisinformationDetectionService>>();

        var factCheckingService = new FactCheckingService(mockFactCheckingLogger.Object);
        var sourceService = new SourceAttributionService(mockSourceLogger.Object);
        var confidenceService = new ConfidenceAnalysisService(mockConfidenceLogger.Object);
        var misinfoService = new MisinformationDetectionService(mockMisinfoLogger.Object);

        var orchestrator = new ContentVerificationOrchestrator(
            mockLogger.Object,
            factCheckingService,
            sourceService,
            confidenceService,
            misinfoService
        );

        var request = new VerificationRequest(
            ContentId: "test-content-1",
            Content: "The Earth is round. Water boils at 100 degrees Celsius at sea level.",
            Options: new VerificationOptions()
        );

        // Act
        var result = await orchestrator.VerifyContentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-content-1", result.ContentId);
        Assert.NotNull(result.Claims);
        Assert.NotNull(result.FactChecks);
        Assert.NotNull(result.Warnings);
        Assert.True(result.OverallConfidence >= 0 && result.OverallConfidence <= 1);
    }

    [Fact]
    public async Task SourceAttributionService_GenerateCitations_ReturnsFormattedCitations()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SourceAttributionService>>();
        var service = new SourceAttributionService(mockLogger.Object);

        var sources = new List<SourceAttribution>
        {
            new SourceAttribution(
                SourceId: "source1",
                Name: "Scientific Journal",
                Url: "https://example.com/article",
                Type: SourceType.AcademicJournal,
                CredibilityScore: 0.9,
                PublishedDate: DateTime.UtcNow.AddMonths(-6),
                Author: "Dr. Smith"
            )
        };

        // Act
        var citations = await service.GenerateCitationsAsync(sources, CitationFormat.APA, CancellationToken.None);

        // Assert
        Assert.NotNull(citations);
        Assert.Single(citations);
        Assert.Contains("Dr. Smith", citations[0]);
        Assert.Contains("https://example.com/article", citations[0]);
    }

    [Fact]
    public void SourceAttributionService_DeduplicateSources_RemovesDuplicates()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SourceAttributionService>>();
        var service = new SourceAttributionService(mockLogger.Object);

        var sources = new List<SourceAttribution>
        {
            new SourceAttribution("1", "Source A", "https://example.com/a", SourceType.NewsOrganization, 0.8, null, null),
            new SourceAttribution("2", "Source A", "https://example.com/a", SourceType.NewsOrganization, 0.9, null, null),
            new SourceAttribution("3", "Source B", "https://example.com/b", SourceType.Wikipedia, 0.7, null, null)
        };

        // Act
        var deduplicated = service.DeduplicateSources(sources);

        // Assert
        Assert.Equal(2, deduplicated.Count);
        Assert.Contains(deduplicated, s => s.Name == "Source A" && s.CredibilityScore == 0.9);
        Assert.Contains(deduplicated, s => s.Name == "Source B");
    }

    [Fact]
    public async Task QuickVerify_WithShortContent_ReturnsQuickResult()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ContentVerificationOrchestrator>>();
        var mockFactCheckingLogger = new Mock<ILogger<FactCheckingService>>();
        var mockSourceLogger = new Mock<ILogger<SourceAttributionService>>();
        var mockConfidenceLogger = new Mock<ILogger<ConfidenceAnalysisService>>();
        var mockMisinfoLogger = new Mock<ILogger<MisinformationDetectionService>>();

        var factCheckingService = new FactCheckingService(mockFactCheckingLogger.Object);
        var sourceService = new SourceAttributionService(mockSourceLogger.Object);
        var confidenceService = new ConfidenceAnalysisService(mockConfidenceLogger.Object);
        var misinfoService = new MisinformationDetectionService(mockMisinfoLogger.Object);

        var orchestrator = new ContentVerificationOrchestrator(
            mockLogger.Object,
            factCheckingService,
            sourceService,
            confidenceService,
            misinfoService
        );

        // Act
        var result = await orchestrator.QuickVerifyAsync("The sky is blue. Water is wet.", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ClaimCount >= 0);
        Assert.True(result.AverageConfidence >= 0 && result.AverageConfidence <= 1);
    }
}
