using Aura.Core.Services.FFmpeg;
using Xunit;

namespace Aura.Tests;

public class FFmpegCommandValidatorTests
{
    [Fact]
    public void ValidateArguments_AllowsValidCommand()
    {
        // Arrange
        var args = "-i input.mp4 -c:v libx264 -c:a aac output.mp4";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateArguments_RejectsDangerousProtocols()
    {
        // Arrange
        var args = "-i file:///etc/passwd -c:v copy output.mp4";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateArguments_RejectsShellOperators()
    {
        // Arrange
        var args = "-i input.mp4 | cat";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateArguments_RejectsPipeProtocol()
    {
        // Arrange
        var args = "-i pipe:0 -c:v copy output.mp4";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateArguments_RejectsUnwhitelistedOptions()
    {
        // Arrange
        var args = "-i input.mp4 -dangerous_option value output.mp4";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateArguments_AllowsFilters()
    {
        // Arrange
        var args = "-i input.mp4 -vf scale=1920:1080 output.mp4";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateArguments_RejectsDangerousFilterValues()
    {
        // Arrange
        var args = "-i input.mp4 -vf movie=/etc/passwd output.mp4";

        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SanitizeDrawText_EscapesColons()
    {
        // Arrange
        var text = "Title: My Video";

        // Act
        var result = FFmpegCommandValidator.SanitizeDrawText(text);

        // Assert
        Assert.Contains("\\:", result);
    }

    [Fact]
    public void SanitizeDrawText_EscapesQuotes()
    {
        // Arrange
        var text = "He said 'Hello'";

        // Act
        var result = FFmpegCommandValidator.SanitizeDrawText(text);

        // Assert
        Assert.Contains("\\'", result);
    }

    [Fact]
    public void SanitizeDrawText_RemovesControlCharacters()
    {
        // Arrange
        var text = "Hello\x00World\x01Test";

        // Act
        var result = FFmpegCommandValidator.SanitizeDrawText(text);

        // Assert
        Assert.DoesNotContain('\x00', result);
        Assert.DoesNotContain('\x01', result);
        Assert.Equal("HelloWorldTest", result);
    }

    [Fact]
    public void EscapeFilePath_WrapsPathsWithSpaces()
    {
        // Arrange
        var path = "my video file.mp4";

        // Act
        var result = FFmpegCommandValidator.EscapeFilePath(path);

        // Assert
        Assert.StartsWith("\"", result);
        Assert.EndsWith("\"", result);
    }

    [Fact]
    public void EscapeFilePath_DoesNotWrapSimplePaths()
    {
        // Arrange
        var path = "input.mp4";

        // Act
        var result = FFmpegCommandValidator.EscapeFilePath(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Theory]
    [InlineData("-c:v libx264", true)]
    [InlineData("-vf scale=1920:1080", true)]
    [InlineData("-dangerous", false)]
    [InlineData("-i file:///etc/passwd", false)]
    public void ValidateArguments_HandlesVariousInputs(string args, bool expected)
    {
        // Act
        var result = FFmpegCommandValidator.ValidateArguments(args);

        // Assert
        Assert.Equal(expected, result);
    }
}
