using System;
using System.Threading.Tasks;
using Aura.Core.Models.Licensing;
using Aura.Core.Services.Licensing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class LicensingManifestServiceTests
{
    private readonly Mock<ILogger<LicensingService>> _loggerMock;
    private readonly LicensingService _service;

    public LicensingManifestServiceTests()
    {
        _loggerMock = new Mock<ILogger<LicensingService>>();
        _service = new LicensingService(_loggerMock.Object);
    }

    [Fact]
    public async Task GenerateManifestAsync_CreatesBasicManifest()
    {
        var projectId = "test-project-123";

        var manifest = await _service.GenerateManifestAsync(projectId);

        Assert.NotNull(manifest);
        Assert.Equal(projectId, manifest.ProjectId);
        Assert.NotNull(manifest.Assets);
        Assert.NotNull(manifest.Warnings);
        Assert.NotNull(manifest.MissingLicensingInfo);
        Assert.NotNull(manifest.Summary);
    }

    [Fact]
    public async Task ExportManifestAsync_Json_ReturnsValidJson()
    {
        var projectId = "test-project-json";
        var manifest = await _service.GenerateManifestAsync(projectId);

        var exported = await _service.ExportManifestAsync(manifest, LicensingExportFormat.Json);

        Assert.NotNull(exported);
        Assert.Contains(projectId, exported);
        Assert.Contains("projectId", exported);
    }

    [Fact]
    public async Task ExportManifestAsync_Csv_ReturnsValidCsv()
    {
        var projectId = "test-project-csv";
        var manifest = await _service.GenerateManifestAsync(projectId);

        var exported = await _service.ExportManifestAsync(manifest, LicensingExportFormat.Csv);

        Assert.NotNull(exported);
        Assert.Contains("Asset ID", exported);
        Assert.Contains("Type", exported);
    }

    [Fact]
    public async Task ExportManifestAsync_Html_ReturnsValidHtml()
    {
        var projectId = "test-project-html";
        var manifest = await _service.GenerateManifestAsync(projectId);

        var exported = await _service.ExportManifestAsync(manifest, LicensingExportFormat.Html);

        Assert.NotNull(exported);
        Assert.Contains("<!DOCTYPE html>", exported);
        Assert.Contains("<table>", exported);
    }

    [Fact]
    public async Task ExportManifestAsync_Text_ReturnsValidText()
    {
        var projectId = "test-project-text";
        var manifest = await _service.GenerateManifestAsync(projectId);

        var exported = await _service.ExportManifestAsync(manifest, LicensingExportFormat.Text);

        Assert.NotNull(exported);
        Assert.Contains("LICENSING INFORMATION", exported);
        Assert.Contains("SUMMARY", exported);
    }

    [Fact]
    public void ValidateManifest_NullManifest_ReturnsFalse()
    {
        var isValid = _service.ValidateManifest(null!);

        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateManifest_ValidManifest_ReturnsTrue()
    {
        var projectId = "test-project-validate";
        var manifest = await _service.GenerateManifestAsync(projectId);

        var isValid = _service.ValidateManifest(manifest);

        Assert.True(isValid);
    }

    [Fact]
    public async Task RecordSignOffAsync_RecordsSuccessfully()
    {
        var projectId = "test-project-signoff";
        var signOff = new LicensingSignOff
        {
            ProjectId = projectId,
            AcknowledgedCommercialRestrictions = true,
            AcknowledgedAttributionRequirements = true,
            AcknowledgedWarnings = true,
            SignedOffAt = DateTime.UtcNow,
            Notes = "Test sign-off"
        };

        await _service.RecordSignOffAsync(signOff);
    }

    [Fact]
    public async Task GenerateManifestAsync_CachesManifest()
    {
        var projectId = "test-project-cache";

        var manifest1 = await _service.GenerateManifestAsync(projectId);
        var manifest2 = await _service.GenerateManifestAsync(projectId);

        Assert.Same(manifest1, manifest2);
    }

    [Fact]
    public void GenerateManifestFromTimeline_WithEmptyTimeline_CreatesEmptyManifest()
    {
        var projectId = "test-project-timeline";
        var timeline = new Aura.Core.Models.Timeline.EditableTimeline
        {
            Scenes = new System.Collections.Generic.List<Aura.Core.Models.Timeline.TimelineScene>()
        };

        var manifest = _service.GenerateManifestFromTimeline(projectId, timeline);

        Assert.NotNull(manifest);
        Assert.Equal(projectId, manifest.ProjectId);
        Assert.Empty(manifest.Assets);
    }

    [Fact]
    public void GenerateManifestFromTimeline_WithScenes_CreatesManifestWithAssets()
    {
        var projectId = "test-project-timeline-scenes";
        var timeline = new Aura.Core.Models.Timeline.EditableTimeline
        {
            Scenes = new System.Collections.Generic.List<Aura.Core.Models.Timeline.TimelineScene>
            {
                new Aura.Core.Models.Timeline.TimelineScene(
                    Index: 0,
                    Heading: "Scene 1",
                    Script: "Test script",
                    Start: TimeSpan.Zero,
                    Duration: TimeSpan.FromSeconds(5),
                    NarrationAudioPath: "/path/to/audio.wav"
                )
            }
        };

        var manifest = _service.GenerateManifestFromTimeline(projectId, timeline);

        Assert.NotNull(manifest);
        Assert.Equal(projectId, manifest.ProjectId);
        Assert.NotEmpty(manifest.Assets);
        Assert.True(manifest.Assets.Count >= 2);
    }
}
