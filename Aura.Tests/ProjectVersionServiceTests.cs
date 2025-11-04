using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ProjectVersionServiceTests
{
    private readonly Mock<ProjectVersionRepository> _versionRepositoryMock;
    private readonly Mock<ProjectStateRepository> _projectRepositoryMock;
    private readonly Mock<ILogger<ProjectVersionService>> _loggerMock;
    private readonly ProjectVersionService _service;

    public ProjectVersionServiceTests()
    {
        _versionRepositoryMock = new Mock<ProjectVersionRepository>(
            Mock.Of<AuraDbContext>(),
            Mock.Of<ILogger<ProjectVersionRepository>>());
        _projectRepositoryMock = new Mock<ProjectStateRepository>(
            Mock.Of<AuraDbContext>(),
            Mock.Of<ILogger<ProjectStateRepository>>());
        _loggerMock = new Mock<ILogger<ProjectVersionService>>();

        _service = new ProjectVersionService(
            _versionRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateManualSnapshotAsync_WithValidProject_CreatesVersion()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity
        {
            Id = projectId,
            Title = "Test Project",
            BriefJson = "{\"topic\":\"test\"}",
            PlanSpecJson = "{\"duration\":120}",
            VoiceSpecJson = "{\"voice\":\"test\"}",
            RenderSpecJson = "{\"width\":1920}"
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _versionRepositoryMock
            .Setup(x => x.StoreContentBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("hash123");

        _versionRepositoryMock
            .Setup(x => x.CreateVersionAsync(It.IsAny<ProjectVersionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectVersionEntity v, CancellationToken ct) => v);

        var versionId = await _service.CreateManualSnapshotAsync(
            projectId,
            "Test Snapshot",
            "Test Description",
            "user123",
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, versionId);
        _versionRepositoryMock.Verify(
            x => x.CreateVersionAsync(It.IsAny<ProjectVersionEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateManualSnapshotAsync_WithNonExistentProject_ThrowsException()
    {
        var projectId = Guid.NewGuid();

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectStateEntity?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateManualSnapshotAsync(projectId, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAutosaveAsync_WithValidProject_CreatesAutosaveVersion()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity
        {
            Id = projectId,
            Title = "Test Project",
            BriefJson = "{\"topic\":\"test\"}"
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _versionRepositoryMock
            .Setup(x => x.StoreContentBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("hash123");

        _versionRepositoryMock
            .Setup(x => x.CreateVersionAsync(It.IsAny<ProjectVersionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectVersionEntity v, CancellationToken ct) =>
            {
                Assert.Equal("Autosave", v.VersionType);
                return v;
            });

        var versionId = await _service.CreateAutosaveAsync(projectId, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, versionId);
    }

    [Fact]
    public async Task CreateRestorePointAsync_WithTrigger_CreatesRestorePointVersion()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity
        {
            Id = projectId,
            Title = "Test Project",
            BriefJson = "{\"topic\":\"test\"}"
        };

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _versionRepositoryMock
            .Setup(x => x.StoreContentBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("hash123");

        _versionRepositoryMock
            .Setup(x => x.CreateVersionAsync(It.IsAny<ProjectVersionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectVersionEntity v, CancellationToken ct) =>
            {
                Assert.Equal("RestorePoint", v.VersionType);
                Assert.Equal("PreScriptRegeneration", v.Trigger);
                return v;
            });

        var versionId = await _service.CreateRestorePointAsync(
            projectId,
            "PreScriptRegeneration",
            null,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, versionId);
    }

    [Fact]
    public async Task RestoreVersionAsync_WithValidVersion_RestoresProjectState()
    {
        var projectId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var version = new ProjectVersionEntity
        {
            Id = versionId,
            ProjectId = projectId,
            VersionNumber = 5,
            BriefJson = "{\"topic\":\"restored\"}",
            PlanSpecJson = "{\"duration\":180}",
            VoiceSpecJson = "{\"voice\":\"restored\"}",
            RenderSpecJson = "{\"width\":1920}"
        };

        var project = new ProjectStateEntity
        {
            Id = projectId,
            Title = "Test Project"
        };

        _versionRepositoryMock
            .Setup(x => x.GetVersionByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        _projectRepositoryMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.RestoreVersionAsync(projectId, versionId, CancellationToken.None);

        _projectRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<ProjectStateEntity>(p =>
                p.BriefJson == version.BriefJson &&
                p.PlanSpecJson == version.PlanSpecJson &&
                p.VoiceSpecJson == version.VoiceSpecJson &&
                p.RenderSpecJson == version.RenderSpecJson
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RestoreVersionAsync_WithNonExistentVersion_ThrowsException()
    {
        var projectId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        _versionRepositoryMock
            .Setup(x => x.GetVersionByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectVersionEntity?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RestoreVersionAsync(projectId, versionId, CancellationToken.None));
    }

    [Fact]
    public async Task CompareVersionsAsync_WithTwoVersions_ReturnsComparison()
    {
        var version1Id = Guid.NewGuid();
        var version2Id = Guid.NewGuid();

        var version1 = new ProjectVersionEntity
        {
            Id = version1Id,
            VersionNumber = 1,
            BriefHash = "hash1",
            PlanHash = "hash1",
            VoiceHash = "hash1",
            RenderHash = "hash1",
            TimelineHash = "hash1",
            BriefJson = "{\"topic\":\"v1\"}",
            PlanSpecJson = "{\"duration\":120}"
        };

        var version2 = new ProjectVersionEntity
        {
            Id = version2Id,
            VersionNumber = 2,
            BriefHash = "hash2",
            PlanHash = "hash1",
            VoiceHash = "hash1",
            RenderHash = "hash2",
            TimelineHash = null,
            BriefJson = "{\"topic\":\"v2\"}",
            PlanSpecJson = "{\"duration\":120}"
        };

        _versionRepositoryMock
            .Setup(x => x.GetVersionByIdAsync(version1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version1);

        _versionRepositoryMock
            .Setup(x => x.GetVersionByIdAsync(version2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version2);

        var result = await _service.CompareVersionsAsync(version1Id, version2Id, CancellationToken.None);

        Assert.Equal(version1Id, result.Version1Id);
        Assert.Equal(version2Id, result.Version2Id);
        Assert.True(result.BriefChanged);
        Assert.False(result.PlanChanged);
        Assert.False(result.VoiceChanged);
        Assert.True(result.RenderChanged);
        Assert.True(result.TimelineChanged);
    }
}
