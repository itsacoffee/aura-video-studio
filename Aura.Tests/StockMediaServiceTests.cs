using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Aura.Core.Services.StockMedia;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class StockMediaServiceTests
{
    #region PerceptualHashService Tests

    [Fact]
    public void PerceptualHashService_Should_Generate_Consistent_Hash()
    {
        // Arrange
        var service = new PerceptualHashService();
        var url = "https://example.com/image.jpg";
        var width = 1920;
        var height = 1080;

        // Act
        var hash1 = service.GenerateHash(url, width, height);
        var hash2 = service.GenerateHash(url, width, height);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
        Assert.Equal(16, hash1.Length);
    }

    [Fact]
    public void PerceptualHashService_Should_Detect_Duplicates()
    {
        // Arrange
        var service = new PerceptualHashService();
        var hash = service.GenerateHash("https://example.com/image.jpg", 1920, 1080);

        // Act
        var isDuplicate = service.IsDuplicate(hash, hash);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public void PerceptualHashService_Should_Calculate_Similarity()
    {
        // Arrange
        var service = new PerceptualHashService();
        var hash1 = "ABCDEF1234567890";
        var hash2 = "ABCDEF1234567890";

        // Act
        var similarity = service.CalculateSimilarity(hash1, hash2);

        // Assert
        Assert.Equal(1.0, similarity);
    }

    #endregion

    #region ContentSafetyFilterService Tests

    [Fact]
    public async Task ContentSafetyFilterService_Should_Allow_Safe_Content()
    {
        // Arrange
        var service = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        // Act
        var isSafe = await service.IsContentSafeAsync("Photo by John Doe on Pexels", CancellationToken.None);

        // Assert
        Assert.True(isSafe);
    }

    [Fact]
    public void ContentSafetyFilterService_Should_Sanitize_Query()
    {
        // Arrange
        var service = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        // Act
        var sanitized = service.SanitizeQuery("beautiful sunset with violence");

        // Assert
        Assert.Contains("beautiful", sanitized);
        Assert.Contains("sunset", sanitized);
        Assert.DoesNotContain("violence", sanitized);
    }

    [Fact]
    public void ContentSafetyFilterService_Should_Validate_Query()
    {
        // Arrange
        var service = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        // Act
        var (isValid, reason) = service.ValidateQuery("beautiful nature landscape");

        // Assert
        Assert.True(isValid);
        Assert.Null(reason);
    }

    [Fact]
    public void ContentSafetyFilterService_Should_Reject_Empty_Query()
    {
        // Arrange
        var service = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        // Act
        var (isValid, reason) = service.ValidateQuery("");

        // Assert
        Assert.False(isValid);
        Assert.NotNull(reason);
    }

    #endregion

    #region UnifiedStockMediaService Tests

    [Fact(Skip = "Needs refactoring for list mutation")]
    public async Task UnifiedStockMediaService_Should_Search_Multiple_Providers()
    {
        // Arrange
        var mockProvider1 = new Mock<IEnhancedStockProvider>();
        mockProvider1.Setup(p => p.ProviderName).Returns(StockMediaProvider.Pexels);
        mockProvider1.Setup(p => p.SupportsVideo).Returns(true);
        mockProvider1.Setup(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMediaResult>
            {
                new StockMediaResult
                {
                    Id = "1",
                    Type = StockMediaType.Image,
                    Provider = StockMediaProvider.Pexels,
                    FullSizeUrl = "https://example.com/1.jpg",
                    Width = 1920,
                    Height = 1080,
                    RelevanceScore = 0.8,
                    Licensing = new Aura.Core.Models.Assets.AssetLicensingInfo
                    {
                        LicenseType = "Pexels License",
                        CommercialUseAllowed = true,
                        AttributionRequired = false,
                        SourcePlatform = "Pexels"
                    }
                }
            });
        mockProvider1.Setup(p => p.GetRateLimitStatus()).Returns(new RateLimitStatus
        {
            Provider = StockMediaProvider.Pexels,
            RequestsRemaining = 200,
            RequestsLimit = 200,
            IsLimited = false
        });

        var mockProvider2 = new Mock<IEnhancedStockProvider>();
        mockProvider2.Setup(p => p.ProviderName).Returns(StockMediaProvider.Unsplash);
        mockProvider2.Setup(p => p.SupportsVideo).Returns(false);
        mockProvider2.Setup(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMediaResult>
            {
                new StockMediaResult
                {
                    Id = "2",
                    Type = StockMediaType.Image,
                    Provider = StockMediaProvider.Unsplash,
                    FullSizeUrl = "https://example.com/2.jpg",
                    Width = 1920,
                    Height = 1080,
                    RelevanceScore = 0.9,
                    Licensing = new Aura.Core.Models.Assets.AssetLicensingInfo
                    {
                        LicenseType = "Unsplash License",
                        CommercialUseAllowed = true,
                        AttributionRequired = true,
                        SourcePlatform = "Unsplash"
                    }
                }
            });
        mockProvider2.Setup(p => p.GetRateLimitStatus()).Returns(new RateLimitStatus
        {
            Provider = StockMediaProvider.Unsplash,
            RequestsRemaining = 50,
            RequestsLimit = 50,
            IsLimited = false
        });

        var providers = new[] { mockProvider1.Object, mockProvider2.Object };
        var hashService = new PerceptualHashService();
        var safetyService = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        var service = new UnifiedStockMediaService(
            NullLogger<UnifiedStockMediaService>.Instance,
            providers,
            hashService,
            safetyService);

        var request = new StockMediaSearchRequest
        {
            Query = "nature",
            Type = StockMediaType.Image,
            Count = 10,
            Providers = new List<StockMediaProvider>(),
            SafeSearchEnabled = false
        };

        // Act
        var response = await service.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, response.Results.Count);
        Assert.Equal(2, response.TotalResults);
        Assert.Contains(response.ResultsByProvider, kvp => kvp.Key == StockMediaProvider.Pexels);
        Assert.Contains(response.ResultsByProvider, kvp => kvp.Key == StockMediaProvider.Unsplash);
    }

    [Fact]
    public async Task UnifiedStockMediaService_Should_Deduplicate_Results()
    {
        // Arrange
        var mockProvider = new Mock<IEnhancedStockProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns(StockMediaProvider.Pexels);
        mockProvider.Setup(p => p.SupportsVideo).Returns(true);
        mockProvider.Setup(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMediaResult>
            {
                new StockMediaResult
                {
                    Id = "1",
                    Type = StockMediaType.Image,
                    Provider = StockMediaProvider.Pexels,
                    FullSizeUrl = "https://example.com/image.jpg",
                    Width = 1920,
                    Height = 1080,
                    PerceptualHash = "ABCD1234"
                },
                new StockMediaResult
                {
                    Id = "2",
                    Type = StockMediaType.Image,
                    Provider = StockMediaProvider.Pexels,
                    FullSizeUrl = "https://example.com/image.jpg",
                    Width = 1920,
                    Height = 1080,
                    PerceptualHash = "ABCD1234"
                }
            });
        mockProvider.Setup(p => p.GetRateLimitStatus()).Returns(new RateLimitStatus
        {
            Provider = StockMediaProvider.Pexels,
            RequestsRemaining = 200,
            RequestsLimit = 200,
            IsLimited = false
        });

        var providers = new[] { mockProvider.Object };
        var hashService = new PerceptualHashService();
        var safetyService = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        var service = new UnifiedStockMediaService(
            NullLogger<UnifiedStockMediaService>.Instance,
            providers,
            hashService,
            safetyService);

        var request = new StockMediaSearchRequest
        {
            Query = "nature",
            Type = StockMediaType.Image,
            Count = 10
        };

        // Act
        var response = await service.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(response.Results);
    }

    [Fact]
    public void UnifiedStockMediaService_Should_Return_RateLimitStatus()
    {
        // Arrange
        var mockProvider = new Mock<IEnhancedStockProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns(StockMediaProvider.Pexels);
        mockProvider.Setup(p => p.GetRateLimitStatus()).Returns(new RateLimitStatus
        {
            Provider = StockMediaProvider.Pexels,
            RequestsRemaining = 100,
            RequestsLimit = 200,
            IsLimited = false
        });

        var providers = new[] { mockProvider.Object };
        var hashService = new PerceptualHashService();
        var safetyService = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        var service = new UnifiedStockMediaService(
            NullLogger<UnifiedStockMediaService>.Instance,
            providers,
            hashService,
            safetyService);

        // Act
        var status = service.GetRateLimitStatus();

        // Assert
        Assert.Single(status);
        Assert.Contains(StockMediaProvider.Pexels, status.Keys);
        Assert.Equal(100, status[StockMediaProvider.Pexels].RequestsRemaining);
    }

    #endregion
}
