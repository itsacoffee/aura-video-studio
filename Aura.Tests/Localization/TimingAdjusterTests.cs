using System.Collections.Generic;
using Aura.Core.Models.Localization;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

public class TimingAdjusterTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly TimingAdjuster _adjuster;

    public TimingAdjusterTests()
    {
        _mockLogger = new Mock<ILogger>();
        _adjuster = new TimingAdjuster(_mockLogger.Object);
    }

    [Fact]
    public void AdjustTimings_EmptyLines_ReturnsDefaultAdjustment()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>();

        // Act
        var result = _adjuster.AdjustTimings(lines, 1.0, 0.15);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.OriginalTotalDuration);
        Assert.Equal(0, result.AdjustedTotalDuration);
        Assert.Equal(1.0, result.ExpansionFactor);
    }

    [Fact]
    public void AdjustTimings_NoExpansion_PreservesOriginalDuration()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>
        {
            new()
            {
                SourceText = "Hello world",
                TranslatedText = "Hello world",
                OriginalDurationSeconds = 5.0
            }
        };

        // Act
        var result = _adjuster.AdjustTimings(lines, 1.0, 0.15);

        // Assert
        Assert.Equal(5.0, result.OriginalTotalDuration);
        Assert.Equal(5.0, result.AdjustedTotalDuration);
        Assert.False(result.RequiresCompression);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void AdjustTimings_GermanExpansion_IncreasesDuration()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>
        {
            new()
            {
                SourceText = "Hello",
                TranslatedText = "Guten Tag",
                OriginalDurationSeconds = 5.0
            }
        };

        // Act - German typically 1.3x expansion
        var result = _adjuster.AdjustTimings(lines, 1.3, 0.15);

        // Assert
        Assert.True(result.AdjustedTotalDuration > result.OriginalTotalDuration);
    }

    [Fact]
    public void AdjustTimings_ExceedsMaxVariance_AddsWarning()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>
        {
            new()
            {
                SourceText = "Hi",
                TranslatedText = "This is a much longer translation that significantly expands the original text",
                OriginalDurationSeconds = 2.0
            }
        };

        // Act - Very small max variance to force warning
        var result = _adjuster.AdjustTimings(lines, 1.0, 0.05);

        // Assert
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void AdjustTimings_LargeVariance_RequiresCompression()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>
        {
            new()
            {
                SourceText = "Short",
                TranslatedText = "This is an extremely long translation that goes on and on with many words",
                OriginalDurationSeconds = 2.0
            }
        };

        // Act
        var result = _adjuster.AdjustTimings(lines, 1.0, 0.10);

        // Assert
        Assert.True(result.RequiresCompression);
        Assert.NotEmpty(result.CompressionSuggestions);
    }

    [Fact]
    public void AdjustTimings_MultipleLines_AdjustsStartTimes()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>
        {
            new()
            {
                SourceText = "Line 1",
                TranslatedText = "Line 1 translated",
                OriginalStartSeconds = 0.0,
                OriginalDurationSeconds = 3.0
            },
            new()
            {
                SourceText = "Line 2",
                TranslatedText = "Line 2 translated",
                OriginalStartSeconds = 3.0,
                OriginalDurationSeconds = 4.0
            }
        };

        // Act
        var result = _adjuster.AdjustTimings(lines, 1.2, 0.15);

        // Assert
        Assert.Equal(0.0, lines[0].AdjustedStartSeconds);
        Assert.True(lines[1].AdjustedStartSeconds > 0);
        Assert.True(result.AdjustedTotalDuration >= result.OriginalTotalDuration);
    }

    [Fact]
    public void AdjustTimings_ChineseCompression_DecresesDuration()
    {
        // Arrange
        var lines = new List<TranslatedScriptLine>
        {
            new()
            {
                SourceText = "This is a long English sentence",
                TranslatedText = "这是中文",
                OriginalDurationSeconds = 5.0
            }
        };

        // Act - Chinese typically 0.7x compression
        var result = _adjuster.AdjustTimings(lines, 0.7, 0.15);

        // Assert
        Assert.True(result.AdjustedTotalDuration < result.OriginalTotalDuration);
    }
}
