using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using SkiaSharp;

// Alias to disambiguate Asset types
using CoreAsset = Aura.Core.Models.Asset;

namespace Aura.Providers.Images;

/// <summary>
/// Configuration for placeholder color generation.
/// </summary>
public record PlaceholderColorConfig
{
    /// <summary>
    /// Custom color palette to use. If empty, uses default palette.
    /// Colors should be in hex format (e.g., "#2C3E50").
    /// </summary>
    public List<string> ColorPalette { get; init; } = new();

    /// <summary>
    /// Width of generated placeholder images.
    /// </summary>
    public int Width { get; init; } = 1920;

    /// <summary>
    /// Height of generated placeholder images.
    /// </summary>
    public int Height { get; init; } = 1080;

    /// <summary>
    /// Whether to include text overlay on placeholders.
    /// </summary>
    public bool ShowText { get; init; } = true;

    /// <summary>
    /// Font size for text overlay.
    /// </summary>
    public int FontSize { get; init; } = 72;

    /// <summary>
    /// Whether to add a gradient effect to placeholders.
    /// </summary>
    public bool UseGradient { get; init; } = true;
}

/// <summary>
/// Generates solid color placeholder frames as a fallback when no stock providers are available.
/// This service is always available and requires no external dependencies.
/// Implements IStockProvider for integration with the provider system.
/// </summary>
public class PlaceholderColorGenerator : IStockProvider
{
    // Text display constants
    private const int MaxDisplayTextLength = 50;
    private const int TruncatedTextLength = 47;
    private const int TextMargin = 200;
    private const int MinFontSize = 20;
    private const int FontSizeDecrementStep = 4;
    
    private readonly ILogger<PlaceholderColorGenerator> _logger;
    private readonly string _outputDirectory;
    private readonly PlaceholderColorConfig _config;

    private static readonly SKColor[] DefaultColorPalette =
    {
        SKColor.Parse("#2C3E50"), // Dark blue-gray
        SKColor.Parse("#E74C3C"), // Red
        SKColor.Parse("#3498DB"), // Blue
        SKColor.Parse("#2ECC71"), // Green
        SKColor.Parse("#F39C12"), // Orange
        SKColor.Parse("#9B59B6"), // Purple
        SKColor.Parse("#1ABC9C"), // Turquoise
        SKColor.Parse("#34495E"), // Midnight blue
        SKColor.Parse("#16A085"), // Green Sea
        SKColor.Parse("#27AE60"), // Nephritis
        SKColor.Parse("#2980B9"), // Belize Hole
        SKColor.Parse("#8E44AD"), // Wisteria
        SKColor.Parse("#C0392B"), // Pomegranate
        SKColor.Parse("#D35400"), // Pumpkin
        SKColor.Parse("#7F8C8D"), // Asbestos
        SKColor.Parse("#2C3E50"), // Wet Asphalt
    };

    private readonly SKColor[] _colorPalette;

    public PlaceholderColorGenerator(
        ILogger<PlaceholderColorGenerator> logger,
        string? outputDirectory = null,
        PlaceholderColorConfig? config = null)
    {
        _logger = logger;
        _config = config ?? new PlaceholderColorConfig();
        _outputDirectory = outputDirectory ?? Path.Combine(Path.GetTempPath(), "aura-placeholders");

        if (_config.ColorPalette.Count > 0)
        {
            var customColors = new List<SKColor>();
            foreach (var hex in _config.ColorPalette)
            {
                try
                {
                    customColors.Add(SKColor.Parse(hex));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid color hex: {Color}, using default", hex);
                }
            }
            _colorPalette = customColors.Count > 0 ? customColors.ToArray() : DefaultColorPalette;
        }
        else
        {
            _colorPalette = DefaultColorPalette;
        }

        EnsureOutputDirectory();
    }

    private void EnsureOutputDirectory()
    {
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
            _logger.LogInformation("Created placeholder output directory: {Directory}", _outputDirectory);
        }
    }

    /// <summary>
    /// Searches and generates placeholder color frame assets.
    /// Implements IStockProvider interface.
    /// </summary>
    public Task<IReadOnlyList<CoreAsset>> SearchAsync(string query, int count, CancellationToken ct)
    {
        return GeneratePlaceholdersAsync(query, count, ct);
    }

    /// <summary>
    /// Generates placeholder color frame assets.
    /// </summary>
    /// <param name="query">Text to display on placeholders (optional)</param>
    /// <param name="count">Number of placeholders to generate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of generated placeholder assets</returns>
    public Task<IReadOnlyList<CoreAsset>> GeneratePlaceholdersAsync(
        string query,
        int count,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating {Count} placeholder color frames for query: {Query}",
            count, query);

        var assets = new List<CoreAsset>();

        try
        {
            for (int i = 0; i < count && !ct.IsCancellationRequested; i++)
            {
                var color = _colorPalette[i % _colorPalette.Length];
                var imagePath = GeneratePlaceholderImage(query, i, color);

                assets.Add(new CoreAsset(
                    Kind: "image",
                    PathOrUrl: imagePath,
                    License: "Generated (Placeholder)",
                    Attribution: "Aura Video Studio - Placeholder Generator"
                ));
            }

            _logger.LogInformation("Generated {Count} placeholder frames", assets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating placeholder frames");
        }

        return Task.FromResult<IReadOnlyList<CoreAsset>>(assets);
    }

    /// <summary>
    /// Generates a single placeholder image with the specified color.
    /// </summary>
    public string GeneratePlaceholderImage(string text, int index, SKColor? backgroundColor = null)
    {
        var width = _config.Width;
        var height = _config.Height;

        var fileName = $"placeholder_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid():N}.png";
        var outputPath = Path.Combine(_outputDirectory, fileName);

        var bgColor = backgroundColor ?? _colorPalette[index % _colorPalette.Length];

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        if (_config.UseGradient)
        {
            DrawGradientBackground(canvas, width, height, bgColor);
        }
        else
        {
            canvas.Clear(bgColor);
        }

        if (_config.ShowText && !string.IsNullOrWhiteSpace(text))
        {
            DrawTextOverlay(canvas, text, width, height, bgColor);
        }

        DrawPlaceholderIcon(canvas, width, height, GetContrastColor(bgColor));

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        _logger.LogDebug("Generated placeholder image: {Path}", outputPath);
        return outputPath;
    }

    private void DrawGradientBackground(SKCanvas canvas, int width, int height, SKColor baseColor)
    {
        var lighterColor = LightenColor(baseColor, 0.15f);
        var darkerColor = DarkenColor(baseColor, 0.15f);

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width, height),
            new[] { lighterColor, baseColor, darkerColor },
            new[] { 0f, 0.5f, 1f },
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true
        };

        canvas.DrawRect(0, 0, width, height, paint);
    }

    private void DrawTextOverlay(SKCanvas canvas, string text, int width, int height, SKColor bgColor)
    {
        var textColor = GetContrastColor(bgColor);
        var displayText = text.Length > MaxDisplayTextLength 
            ? string.Concat(text.AsSpan(0, TruncatedTextLength), "...") 
            : text;

        var fontSize = (float)_config.FontSize;

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
        paint.MeasureText(displayText, ref textBounds);

        while (textBounds.Width > width - TextMargin && fontSize > MinFontSize)
        {
            fontSize -= FontSizeDecrementStep;
            paint.TextSize = fontSize;
            paint.MeasureText(displayText, ref textBounds);
        }

        var x = width / 2f;
        var y = (height - textBounds.Height) / 2f - textBounds.Top;

        using (var shadowPaint = paint.Clone())
        {
            shadowPaint.Color = SKColors.Black.WithAlpha(80);
            canvas.DrawText(displayText, x + 3, y + 3, shadowPaint);
        }

        canvas.DrawText(displayText, x, y, paint);
    }

    private void DrawPlaceholderIcon(SKCanvas canvas, int width, int height, SKColor color)
    {
        var iconSize = 80f;
        var centerX = width / 2f;
        var centerY = height * 0.75f;

        using var paint = new SKPaint
        {
            Color = color.WithAlpha(100),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4
        };

        var rect = new SKRect(
            centerX - iconSize / 2,
            centerY - iconSize / 2,
            centerX + iconSize / 2,
            centerY + iconSize / 2
        );

        canvas.DrawRoundRect(rect, 8, 8, paint);

        paint.Style = SKPaintStyle.Fill;
        canvas.DrawCircle(centerX, centerY, iconSize / 4, paint);
    }

    private static SKColor GetContrastColor(SKColor backgroundColor)
    {
        var luminance = (0.299 * backgroundColor.Red +
                        0.587 * backgroundColor.Green +
                        0.114 * backgroundColor.Blue) / 255;

        return luminance > 0.5 ? SKColors.Black : SKColors.White;
    }

    private static SKColor LightenColor(SKColor color, float amount)
    {
        var r = Math.Min(255, (int)(color.Red + 255 * amount));
        var g = Math.Min(255, (int)(color.Green + 255 * amount));
        var b = Math.Min(255, (int)(color.Blue + 255 * amount));
        return new SKColor((byte)r, (byte)g, (byte)b, color.Alpha);
    }

    private static SKColor DarkenColor(SKColor color, float amount)
    {
        var r = Math.Max(0, (int)(color.Red - 255 * amount));
        var g = Math.Max(0, (int)(color.Green - 255 * amount));
        var b = Math.Max(0, (int)(color.Blue - 255 * amount));
        return new SKColor((byte)r, (byte)g, (byte)b, color.Alpha);
    }

    /// <summary>
    /// Cleans up old placeholder files to prevent disk space issues.
    /// </summary>
    /// <param name="maxAgeHours">Maximum age of files to keep in hours</param>
    public void CleanupOldPlaceholders(int maxAgeHours = 24)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-maxAgeHours);
            var files = Directory.GetFiles(_outputDirectory, "placeholder_*.png");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoff)
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted old placeholder: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up old placeholders");
        }
    }
}
