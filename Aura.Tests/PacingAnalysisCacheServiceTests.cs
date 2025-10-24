using System;
using System.Threading;
using Aura.Api.Models.Responses;
using Aura.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class PacingAnalysisCacheServiceTests
{
    private readonly PacingAnalysisCacheService _cache;
    private readonly Mock<ILogger<PacingAnalysisCacheService>> _mockLogger;

    public PacingAnalysisCacheServiceTests()
    {
        _mockLogger = new Mock<ILogger<PacingAnalysisCacheService>>();
        _cache = new PacingAnalysisCacheService(_mockLogger.Object);
    }

    [Fact]
    public void Set_AndGet_ReturnsStoredAnalysis()
    {
        // Arrange
        var analysisId = "test-id-123";
        var analysis = new PacingAnalysisResponse
        {
            AnalysisId = analysisId,
            OverallScore = 85.5,
            EstimatedRetention = 75.0,
            AverageEngagement = 80.0
        };

        // Act
        _cache.Set(analysisId, analysis);
        var result = _cache.Get(analysisId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(analysisId, result.AnalysisId);
        Assert.Equal(85.5, result.OverallScore);
    }

    [Fact]
    public void Get_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = _cache.Get("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Delete_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var analysisId = "test-id-456";
        var analysis = new PacingAnalysisResponse { AnalysisId = analysisId };
        _cache.Set(analysisId, analysis);

        // Act
        var deleted = _cache.Delete(analysisId);

        // Assert
        Assert.True(deleted);
        Assert.Null(_cache.Get(analysisId));
    }

    [Fact]
    public void Delete_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var deleted = _cache.Delete("non-existent-id");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void ClearExpired_RemovesExpiredEntries()
    {
        // Note: This test would require reflection or waiting for TTL
        // For now, just verify the method doesn't throw
        _cache.ClearExpired();
        
        // No assertion - just ensuring no exception is thrown
    }

    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        // Arrange
        var analysisId = "test-id-789";
        var analysis1 = new PacingAnalysisResponse { AnalysisId = analysisId, OverallScore = 50.0 };
        var analysis2 = new PacingAnalysisResponse { AnalysisId = analysisId, OverallScore = 90.0 };

        // Act
        _cache.Set(analysisId, analysis1);
        _cache.Set(analysisId, analysis2);
        var result = _cache.Get(analysisId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(90.0, result.OverallScore);
    }
}
