using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for offline provider recommendations based on hardware
/// </summary>
public class OfflineProviderRecommendationsTests
{
    [Fact]
    public async Task GetMachineRecommendations_HighRAM_RecommendsMimic3()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 16,
                VramGB = 0,
                LogicalCores = 8,
                Tier = "B",
                Gpu = null
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.Equal(16, recommendations.HardwareSummary.RamGB);
        Assert.Contains("Mimic3", recommendations.TtsRecommendation.Primary);
        Assert.Contains("quality", recommendations.TtsRecommendation.Rationale.ToLowerInvariant());
    }

    [Fact]
    public async Task GetMachineRecommendations_LowRAM_RecommendsPiper()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 8,
                VramGB = 0,
                LogicalCores = 4,
                Tier = "C",
                Gpu = null
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.Equal(8, recommendations.HardwareSummary.RamGB);
        Assert.Contains("Piper", recommendations.TtsRecommendation.Primary);
        Assert.Contains("minimal resource", recommendations.TtsRecommendation.Rationale.ToLowerInvariant());
    }

    [Fact]
    public async Task GetMachineRecommendations_HighRAMAndVRAM_RecommendsGPUAcceleratedOllama()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 16,
                VramGB = 8,
                LogicalCores = 8,
                Tier = "A",
                Gpu = new GpuInfo("NVIDIA", "RTX 3070", 8, "30")
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.Equal(8, recommendations.HardwareSummary.VramGB);
        Assert.True(recommendations.HardwareSummary.HasNvidiaGpu);
        Assert.Contains("llama3.1:8b", recommendations.LlmRecommendation.Primary.ToLowerInvariant());
        Assert.Contains("GPU", recommendations.LlmRecommendation.Rationale);
    }

    [Fact]
    public async Task GetMachineRecommendations_NvidiaGPU8GBVRAM_RecommendsStableDiffusion()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 16,
                VramGB = 8,
                LogicalCores = 8,
                Tier = "A",
                Gpu = new GpuInfo("NVIDIA", "RTX 3070", 8, "30")
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.Contains("Stable Diffusion", recommendations.ImageRecommendation.Primary);
        Assert.Contains("VRAM", recommendations.ImageRecommendation.Rationale);
    }

    [Fact]
    public async Task GetMachineRecommendations_NoGPU_RecommendsStockImages()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 16,
                VramGB = 0,
                LogicalCores = 8,
                Tier = "C",
                Gpu = null
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.Contains("Stock Images", recommendations.ImageRecommendation.Primary);
        Assert.Contains("Insufficient GPU", recommendations.ImageRecommendation.Rationale);
    }

    [Fact]
    public async Task GetMachineRecommendations_IncludesQuickStartSteps()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 16,
                VramGB = 8,
                LogicalCores = 8,
                Tier = "A",
                Gpu = new GpuInfo("NVIDIA", "RTX 3070", 8, "30")
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations.QuickStartSteps);
        Assert.Contains(recommendations.QuickStartSteps, step => step.Contains("Install"));
        Assert.Contains(recommendations.QuickStartSteps, step => step.Contains("preflight"));
    }

    [Fact]
    public async Task GetMachineRecommendations_IncludesCapabilitiesAssessment()
    {
        // Arrange
        var mockHardwareDetector = new Mock<IHardwareDetector>();
        mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                RamGB = 16,
                VramGB = 8,
                LogicalCores = 8,
                Tier = "A",
                Gpu = new GpuInfo("NVIDIA", "RTX 3070", 8, "30")
            });

        var service = new OfflineProviderAvailabilityService(
            NullLogger<OfflineProviderAvailabilityService>.Instance,
            new System.Net.Http.HttpClient(),
            new Configuration.ProviderSettings(),
            mockHardwareDetector.Object
        );

        // Act
        var recommendations = await service.GetMachineRecommendationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations.OverallCapabilities);
        Assert.Contains(recommendations.OverallCapabilities, cap => cap.Contains("TTS"));
    }
}
