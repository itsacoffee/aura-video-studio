using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Aura.Providers.Visuals;

/// <summary>
/// Placeholder visual provider that always succeeds by generating a real PNG image file.
/// This is the guaranteed fallback provider.
/// </summary>
public class PlaceholderProvider : BaseVisualProvider
{
    private static readonly SKColor[] ProfessionalColors = new[]
    {
        new SKColor(41, 128, 185),    // Blue
        new SKColor(39, 174, 96),     // Green
        new SKColor(142, 68, 173),    // Purple
        new SKColor(230, 126, 34),    // Orange
        new SKColor(231, 76, 60),     // Red
        new SKColor(52, 73, 94),      // Dark Blue
        new SKColor(44, 62, 80),      // Navy
        new SKColor(149, 165, 166)    // Gray
    };

    public PlaceholderProvider(ILogger<PlaceholderProvider> logger) : base(logger)
    {
    }

    public override string ProviderName => "Placeholder";

    public override bool RequiresApiKey => false;

    /// <summary>
    /// Generates a real PNG image from a prompt using SkiaSharp.
    /// Creates a solid color background with centered text overlay.
    /// </summary>
    public override Task<string?> GenerateImageAsync(
        string prompt,
        VisualGenerationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("Generating placeholder PNG image for prompt: {Prompt}", prompt);

            var tempPath = Path.Combine(Path.GetTempPath(), $"placeholder_{Guid.NewGuid()}.png");
            
            var backgroundColor = SelectColorForPrompt(prompt);
            var textColor = GetContrastColor(backgroundColor);
            var truncatedPrompt = prompt.Length > 50 ? string.Concat(prompt.AsSpan(0, 47), "...") : prompt;
            
            var width = options.Width > 0 ? options.Width : 1920;
            var height = options.Height > 0 ? options.Height : 1080;

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            
            canvas.Clear(backgroundColor);

            var fontSize = Math.Min(width, height) / 12f;
            using var paint = new SKPaint
            {
                Color = textColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                TextSize = fontSize
            };

            var textBounds = new SKRect();
            paint.MeasureText(truncatedPrompt, ref textBounds);

            while (textBounds.Width > width - 100 && fontSize > 20)
            {
                fontSize -= 5;
                paint.TextSize = fontSize;
                paint.MeasureText(truncatedPrompt, ref textBounds);
            }

            var x = width / 2f;
            var y = (height - textBounds.Height) / 2f - textBounds.Top;

            using (var shadowPaint = paint.Clone())
            {
                shadowPaint.Color = SKColors.Black.WithAlpha(128);
                canvas.DrawText(truncatedPrompt, x + 3, y + 3, shadowPaint);
            }

            canvas.DrawText(truncatedPrompt, x, y, paint);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(tempPath);
            data.SaveTo(stream);

            Logger.LogInformation("Placeholder PNG image generated successfully at: {Path}", tempPath);
            return Task.FromResult<string?>(tempPath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate placeholder PNG image");
            return Task.FromResult<string?>(null);
        }
    }

    private static SKColor GetContrastColor(SKColor backgroundColor)
    {
        var luminance = (0.299 * backgroundColor.Red +
                        0.587 * backgroundColor.Green +
                        0.114 * backgroundColor.Blue) / 255;

        return luminance > 0.5 ? SKColors.Black : SKColors.White;
    }

    public override VisualProviderCapabilities GetProviderCapabilities()
    {
        return new VisualProviderCapabilities
        {
            ProviderName = ProviderName,
            SupportsNegativePrompts = false,
            SupportsBatchGeneration = true,
            SupportsStylePresets = false,
            SupportedAspectRatios = new List<string> { "16:9", "9:16", "1:1", "4:3" },
            SupportedStyles = new List<string> { "solid" },
            MaxWidth = 4096,
            MaxHeight = 4096,
            IsLocal = true,
            IsFree = true,
            CostPerImage = 0m,
            Tier = "Free"
        };
    }

    public override Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    private static SKColor SelectColorForPrompt(string prompt)
    {
        var hash = prompt.GetHashCode();
        var index = Math.Abs(hash) % ProfessionalColors.Length;
        return ProfessionalColors[index];
    }
}
