using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.VideoEffects;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.VideoEffects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.VideoEffects;

public class VideoEffectServiceTests
{
    private readonly Mock<IFFmpegExecutor> _mockExecutor;
    private readonly Mock<ILogger<VideoEffectService>> _mockLogger;
    private readonly string _tempPresetsDir;
    private readonly VideoEffectService _service;

    public VideoEffectServiceTests()
    {
        _mockExecutor = new Mock<IFFmpegExecutor>();
        _mockLogger = new Mock<ILogger<VideoEffectService>>();
        _tempPresetsDir = Path.Combine(Path.GetTempPath(), "aura_test_presets_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPresetsDir);
        
        _service = new VideoEffectService(_mockExecutor.Object, _mockLogger.Object, _tempPresetsDir);
    }

    [Fact]
    public async Task GetPresetsAsync_ReturnsBuiltInPresets()
    {
        // Act
        var presets = await _service.GetPresetsAsync();

        // Assert
        Assert.NotEmpty(presets);
        Assert.Contains(presets, p => p.IsBuiltIn);
    }

    [Fact]
    public async Task GetPresetsAsync_FiltersByCategory()
    {
        // Act
        var cinematicPresets = await _service.GetPresetsAsync(EffectCategory.Cinematic);

        // Assert
        Assert.All(cinematicPresets, p => Assert.Equal(EffectCategory.Cinematic, p.Category));
    }

    [Fact]
    public async Task SavePresetAsync_CreatesPresetFile()
    {
        // Arrange
        var preset = new EffectPreset
        {
            Name = "Test Preset",
            Description = "Test description",
            Category = EffectCategory.Custom,
            Effects = new List<VideoEffect>
            {
                new ColorCorrectionEffect
                {
                    Name = "Test Effect",
                    Brightness = 0.2,
                    Contrast = 0.1,
                    Duration = 1.0
                }
            }
        };

        // Act
        var saved = await _service.SavePresetAsync(preset);

        // Assert
        Assert.False(saved.IsBuiltIn);
        Assert.True(File.Exists(Path.Combine(_tempPresetsDir, $"{saved.Id}.json")));
    }

    [Fact]
    public async Task GetPresetByIdAsync_ReturnsCorrectPreset()
    {
        // Arrange
        var preset = new EffectPreset
        {
            Name = "Test Preset",
            Description = "Test description",
            Category = EffectCategory.Custom,
            Effects = new List<VideoEffect>()
        };
        await _service.SavePresetAsync(preset);

        // Act
        var retrieved = await _service.GetPresetByIdAsync(preset.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(preset.Name, retrieved.Name);
    }

    [Fact]
    public async Task DeletePresetAsync_RemovesCustomPreset()
    {
        // Arrange
        var preset = new EffectPreset
        {
            Name = "Test Preset",
            Description = "Test description",
            Category = EffectCategory.Custom,
            Effects = new List<VideoEffect>()
        };
        await _service.SavePresetAsync(preset);

        // Act
        var deleted = await _service.DeletePresetAsync(preset.Id);

        // Assert
        Assert.True(deleted);
        Assert.False(File.Exists(Path.Combine(_tempPresetsDir, $"{preset.Id}.json")));
    }

    [Fact]
    public async Task DeletePresetAsync_CannotDeleteBuiltInPreset()
    {
        // Act
        var deleted = await _service.DeletePresetAsync("cinematic");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void ValidateEffect_ValidEffect_ReturnsTrue()
    {
        // Arrange
        var effect = new ColorCorrectionEffect
        {
            Name = "Test",
            StartTime = 0,
            Duration = 5.0,
            Intensity = 0.8
        };

        // Act
        var isValid = _service.ValidateEffect(effect, out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateEffect_InvalidDuration_ReturnsFalse()
    {
        // Arrange
        var effect = new ColorCorrectionEffect
        {
            Name = "Test",
            StartTime = 0,
            Duration = -1.0,
            Intensity = 0.8
        };

        // Act
        var isValid = _service.ValidateEffect(effect, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public void BuildEffectFilterComplex_SingleEffect_ReturnsFilter()
    {
        // Arrange
        var effects = new List<VideoEffect>
        {
            new ColorCorrectionEffect
            {
                Name = "Color Grade",
                Brightness = 0.2,
                Contrast = 0.1,
                Duration = 1.0
            }
        };

        // Act
        var filter = _service.BuildEffectFilterComplex(effects);

        // Assert
        Assert.NotEmpty(filter);
        Assert.NotEqual("copy", filter);
    }

    [Fact]
    public void BuildEffectFilterComplex_MultipleEffects_CombinesFilters()
    {
        // Arrange
        var effects = new List<VideoEffect>
        {
            new ColorCorrectionEffect
            {
                Name = "Color Grade",
                Brightness = 0.2,
                Duration = 1.0
            },
            new BlurEffect
            {
                Name = "Blur",
                Strength = 5.0,
                Duration = 1.0
            }
        };

        // Act
        var filter = _service.BuildEffectFilterComplex(effects);

        // Assert
        Assert.Contains(",", filter);
    }

    [Fact]
    public void BuildEffectFilterComplex_DisabledEffect_SkipsEffect()
    {
        // Arrange
        var effects = new List<VideoEffect>
        {
            new ColorCorrectionEffect
            {
                Name = "Color Grade",
                Brightness = 0.2,
                Duration = 1.0,
                Enabled = false
            }
        };

        // Act
        var filter = _service.BuildEffectFilterComplex(effects);

        // Assert
        Assert.Equal("copy", filter);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPresetsDir))
        {
            Directory.Delete(_tempPresetsDir, true);
        }
    }
}
