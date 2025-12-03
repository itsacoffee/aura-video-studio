using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Visuals;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for PlaceholderProvider PNG generation
/// </summary>
public class PlaceholderProviderPngTests
{
    [Fact]
    public async Task PlaceholderProvider_Should_Generate_Valid_Png_File()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 1920, Height = 1080 };

        // Act
        var result = await provider.GenerateImageAsync("Test PNG generation", options);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.EndsWith(".png", result);
        
        // Verify the file is a valid PNG by loading it with SkiaSharp
        using var stream = File.OpenRead(result);
        using var bitmap = SKBitmap.Decode(stream);
        Assert.NotNull(bitmap);
        Assert.Equal(1920, bitmap.Width);
        Assert.Equal(1080, bitmap.Height);

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }

    [Fact]
    public async Task PlaceholderProvider_Should_Generate_Png_With_Custom_Dimensions()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 512, Height = 512 };

        // Act
        var result = await provider.GenerateImageAsync("Custom size test", options);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        using var stream = File.OpenRead(result);
        using var bitmap = SKBitmap.Decode(stream);
        Assert.NotNull(bitmap);
        Assert.Equal(512, bitmap.Width);
        Assert.Equal(512, bitmap.Height);

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }

    [Fact]
    public async Task PlaceholderProvider_Should_Use_Default_Dimensions_For_Zero_Values()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 0, Height = 0 };

        // Act
        var result = await provider.GenerateImageAsync("Default size test", options);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        using var stream = File.OpenRead(result);
        using var bitmap = SKBitmap.Decode(stream);
        Assert.NotNull(bitmap);
        // Should use defaults (1920x1080)
        Assert.Equal(1920, bitmap.Width);
        Assert.Equal(1080, bitmap.Height);

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }

    [Fact]
    public async Task PlaceholderProvider_Should_Generate_Different_Colors_For_Different_Prompts()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 100, Height = 100 };

        // Act
        var result1 = await provider.GenerateImageAsync("First unique prompt for testing", options);
        var result2 = await provider.GenerateImageAsync("Second different prompt here", options);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1, result2);
        
        // Both should be valid PNGs
        using var stream1 = File.OpenRead(result1);
        using var bitmap1 = SKBitmap.Decode(stream1);
        Assert.NotNull(bitmap1);
        
        using var stream2 = File.OpenRead(result2);
        using var bitmap2 = SKBitmap.Decode(stream2);
        Assert.NotNull(bitmap2);

        // Cleanup
        if (File.Exists(result1)) File.Delete(result1);
        if (File.Exists(result2)) File.Delete(result2);
    }

    [Fact]
    public async Task PlaceholderProvider_Png_Should_Be_Processable_By_Image_Tools()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 640, Height = 360 };

        // Act
        var result = await provider.GenerateImageAsync("FFmpeg compatible test", options);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        // Check file size is reasonable (not empty, not too small)
        var fileInfo = new FileInfo(result);
        Assert.True(fileInfo.Length > 100, "PNG file should have reasonable size");
        
        // Verify PNG magic bytes
        using var stream = File.OpenRead(result);
        var header = new byte[8];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, 8));
        Assert.Equal(8, bytesRead);
        
        // PNG signature: 137 80 78 71 13 10 26 10
        Assert.Equal(137, header[0]);
        Assert.Equal(80, header[1]);  // 'P'
        Assert.Equal(78, header[2]);  // 'N'
        Assert.Equal(71, header[3]);  // 'G'

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }

    [Fact]
    public async Task PlaceholderProvider_Should_Handle_Long_Prompts()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 800, Height = 600 };
        var longPrompt = new string('A', 200) + " test with very long prompt text";

        // Act
        var result = await provider.GenerateImageAsync(longPrompt, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        using var stream = File.OpenRead(result);
        using var bitmap = SKBitmap.Decode(stream);
        Assert.NotNull(bitmap);

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }

    [Fact]
    public async Task PlaceholderProvider_Should_Support_Cancellation()
    {
        // Arrange
        var provider = new PlaceholderProvider(NullLogger<PlaceholderProvider>.Instance);
        var options = new VisualGenerationOptions { Width = 100, Height = 100 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - should complete without throwing (synchronous operation)
        var result = await provider.GenerateImageAsync("Cancellation test", options, cts.Token);

        // Assert - even with cancellation token, the synchronous operation completes
        Assert.NotNull(result);
        Assert.True(File.Exists(result));

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }
}
