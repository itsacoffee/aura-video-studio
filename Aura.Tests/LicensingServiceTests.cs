using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Services.AudioIntelligence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class LicensingServiceTests
{
    private readonly LicensingService _service;

    public LicensingServiceTests()
    {
        _service = new LicensingService(NullLogger<LicensingService>.Instance);
    }

    [Fact]
    public void TrackAssetUsage_Should_AddAssetToJob()
    {
        // Arrange
        var jobId = "test-job-123";
        var asset = CreateTestMusicAsset("track1", true, false);

        // Act
        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Assert - Verify by getting summary
        var summary = _service.GetLicensingSummaryAsync(jobId).Result;
        Assert.Single(summary.UsedAssets);
        Assert.Equal(asset.AssetId, summary.UsedAssets[0].Asset.AssetId);
    }

    [Fact]
    public async Task GetLicensingSummaryAsync_Should_ReturnEmptyForNewJob()
    {
        // Arrange
        var jobId = "new-job-456";

        // Act
        var summary = await _service.GetLicensingSummaryAsync(jobId);

        // Assert
        Assert.Empty(summary.UsedAssets);
        Assert.True(summary.AllCommercialUseAllowed);
        Assert.Empty(summary.RequiredAttributions);
    }

    [Fact]
    public async Task GetLicensingSummaryAsync_Should_IdentifyCommercialRestrictions()
    {
        // Arrange
        var jobId = "job-commercial-test";
        var commercialAsset = CreateTestMusicAsset("commercial", true, false);
        var nonCommercialAsset = CreateTestMusicAsset("non-commercial", false, true);

        _service.TrackAssetUsage(jobId, commercialAsset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);
        _service.TrackAssetUsage(jobId, nonCommercialAsset, 1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), true);

        // Act
        var summary = await _service.GetLicensingSummaryAsync(jobId);

        // Assert
        Assert.False(summary.AllCommercialUseAllowed);
        Assert.Equal(2, summary.UsedAssets.Count);
    }

    [Fact]
    public async Task GetLicensingSummaryAsync_Should_CollectRequiredAttributions()
    {
        // Arrange
        var jobId = "job-attribution-test";
        var asset1 = CreateTestMusicAsset("track1", true, true, "Track 1 by Artist A");
        var asset2 = CreateTestMusicAsset("track2", true, true, "Track 2 by Artist B");
        var asset3 = CreateTestMusicAsset("track3", true, false); // No attribution required

        _service.TrackAssetUsage(jobId, asset1, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);
        _service.TrackAssetUsage(jobId, asset2, 1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), true);
        _service.TrackAssetUsage(jobId, asset3, 2, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), true);

        // Act
        var summary = await _service.GetLicensingSummaryAsync(jobId);

        // Assert
        Assert.Equal(2, summary.RequiredAttributions.Count);
        Assert.Contains("Track 1 by Artist A", summary.RequiredAttributions);
        Assert.Contains("Track 2 by Artist B", summary.RequiredAttributions);
    }

    [Fact]
    public async Task GetLicensingSummaryAsync_Should_OnlyIncludeSelectedAssets()
    {
        // Arrange
        var jobId = "job-selection-test";
        var selectedAsset = CreateTestMusicAsset("selected", true, false);
        var unselectedAsset = CreateTestMusicAsset("unselected", true, false);

        _service.TrackAssetUsage(jobId, selectedAsset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);
        _service.TrackAssetUsage(jobId, unselectedAsset, 1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), false);

        // Act
        var summary = await _service.GetLicensingSummaryAsync(jobId);

        // Assert
        Assert.Single(summary.UsedAssets);
        Assert.Equal(selectedAsset.AssetId, summary.UsedAssets[0].Asset.AssetId);
    }

    [Fact]
    public async Task ExportToCsvAsync_Should_GenerateValidCsv()
    {
        // Arrange
        var jobId = "job-csv-export";
        var asset = CreateTestMusicAsset("track1", true, true, "Track 1 Attribution");

        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Act
        var csv = await _service.ExportToCsvAsync(jobId, false);

        // Assert
        Assert.NotEmpty(csv);
        Assert.Contains("Asset ID,Title,Artist", csv);
        Assert.Contains("track1", csv);
        Assert.Contains("Test Track", csv);
        Assert.Contains("True", csv);
    }

    [Fact]
    public async Task ExportToJsonAsync_Should_GenerateValidJson()
    {
        // Arrange
        var jobId = "job-json-export";
        var asset = CreateTestMusicAsset("track1", true, false);

        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Act
        var json = await _service.ExportToJsonAsync(jobId, false);

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("\"jobId\"", json);
        Assert.Contains("\"assets\"", json);
        Assert.Contains("track1", json);
    }

    [Fact]
    public async Task ExportToTextAsync_Should_GenerateReadableText()
    {
        // Arrange
        var jobId = "job-text-export";
        var asset = CreateTestMusicAsset("track1", true, true, "Track 1 by Artist");

        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Act
        var text = await _service.ExportToTextAsync(jobId);

        // Assert
        Assert.NotEmpty(text);
        Assert.Contains("AUDIO LICENSING INFORMATION", text);
        Assert.Contains("REQUIRED ATTRIBUTIONS:", text);
        Assert.Contains("Track 1 by Artist", text);
        Assert.Contains("Scene 0:", text);
    }

    [Fact]
    public async Task ExportToHtmlAsync_Should_GenerateValidHtml()
    {
        // Arrange
        var jobId = "job-html-export";
        var asset = CreateTestMusicAsset("track1", false, true);

        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Act
        var html = await _service.ExportToHtmlAsync(jobId);

        // Assert
        Assert.NotEmpty(html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("Audio Licensing", html);
        Assert.Contains("WARNING", html); // Should warn about non-commercial asset
    }

    [Fact]
    public async Task ValidateForCommercialUseAsync_Should_PassWhenAllAssetsAllowCommercial()
    {
        // Arrange
        var jobId = "job-validation-pass";
        var asset1 = CreateTestMusicAsset("track1", true, false);
        var asset2 = CreateTestMusicAsset("track2", true, false);

        _service.TrackAssetUsage(jobId, asset1, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);
        _service.TrackAssetUsage(jobId, asset2, 1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), true);

        // Act
        var (isValid, issues) = await _service.ValidateForCommercialUseAsync(jobId);

        // Assert
        Assert.True(isValid);
        Assert.Empty(issues.Where(i => i.Contains("do not allow commercial use")));
    }

    [Fact]
    public async Task ValidateForCommercialUseAsync_Should_FailWhenAssetProhibitsCommercial()
    {
        // Arrange
        var jobId = "job-validation-fail";
        var commercialAsset = CreateTestMusicAsset("commercial", true, false);
        var nonCommercialAsset = CreateTestMusicAsset("non-commercial", false, true);

        _service.TrackAssetUsage(jobId, commercialAsset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);
        _service.TrackAssetUsage(jobId, nonCommercialAsset, 1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), true);

        // Act
        var (isValid, issues) = await _service.ValidateForCommercialUseAsync(jobId);

        // Assert
        Assert.False(isValid);
        Assert.Contains(issues, i => i.Contains("do not allow commercial use"));
        Assert.Contains(issues, i => i.Contains("non-commercial"));
    }

    [Fact]
    public async Task ValidateForCommercialUseAsync_Should_IdentifyAttributionRequirements()
    {
        // Arrange
        var jobId = "job-attribution-validation";
        var asset = CreateTestMusicAsset("track1", true, true, "Attribution required");

        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Act
        var (isValid, issues) = await _service.ValidateForCommercialUseAsync(jobId);

        // Assert
        Assert.True(isValid); // Attribution doesn't prevent commercial use
        Assert.Contains(issues, i => i.Contains("Attribution is required"));
    }

    [Fact]
    public void ClearJobData_Should_RemoveAllJobData()
    {
        // Arrange
        var jobId = "job-to-clear";
        var asset = CreateTestMusicAsset("track1", true, false);

        _service.TrackAssetUsage(jobId, asset, 0, TimeSpan.Zero, TimeSpan.FromSeconds(10), true);

        // Act
        _service.ClearJobData(jobId);

        // Assert
        var summary = _service.GetLicensingSummaryAsync(jobId).Result;
        Assert.Empty(summary.UsedAssets);
    }

    private MusicAsset CreateTestMusicAsset(
        string assetId,
        bool commercialUseAllowed,
        bool attributionRequired,
        string? attributionText = null)
    {
        return new MusicAsset(
            AssetId: assetId,
            Title: "Test Track",
            Artist: "Test Artist",
            Album: null,
            FilePath: "/test/path.mp3",
            PreviewUrl: "https://example.com/preview",
            Duration: TimeSpan.FromMinutes(3),
            LicenseType: commercialUseAllowed
                ? (attributionRequired ? LicenseType.CreativeCommonsBY : LicenseType.PublicDomain)
                : LicenseType.CreativeCommonsBYNC,
            LicenseUrl: "https://creativecommons.org/licenses/by/4.0/",
            CommercialUseAllowed: commercialUseAllowed,
            AttributionRequired: attributionRequired,
            AttributionText: attributionText,
            SourcePlatform: "Test Platform",
            CreatorProfileUrl: "https://example.com/creator",
            Genre: MusicGenre.Corporate,
            Mood: MusicMood.Neutral,
            Energy: EnergyLevel.Medium,
            BPM: 120,
            Tags: new List<string> { "test", "music" },
            Metadata: null
        );
    }
}
