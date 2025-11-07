using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Visuals;

/// <summary>
/// Placeholder visual provider that always succeeds by generating solid color cards.
/// This is the guaranteed fallback provider.
/// </summary>
public class PlaceholderProvider : BaseVisualProvider
{
    private static readonly Color[] ProfessionalColors = new[]
    {
        Color.FromArgb(41, 128, 185),   // Blue
        Color.FromArgb(39, 174, 96),    // Green
        Color.FromArgb(142, 68, 173),   // Purple
        Color.FromArgb(230, 126, 34),   // Orange
        Color.FromArgb(231, 76, 60),    // Red
        Color.FromArgb(52, 73, 94),     // Dark Blue
        Color.FromArgb(44, 62, 80),     // Navy
        Color.FromArgb(149, 165, 166)   // Gray
    };

    public PlaceholderProvider(ILogger<PlaceholderProvider> logger) : base(logger)
    {
    }

    public override string ProviderName => "Placeholder";

    public override bool RequiresApiKey => false;

    public override Task<string?> GenerateImageAsync(
        string prompt,
        VisualGenerationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("Generating placeholder image for prompt: {Prompt}", prompt);

            var width = options.Width;
            var height = options.Height;
            var color = SelectColorForPrompt(prompt);

            var tempPath = Path.Combine(Path.GetTempPath(), $"placeholder_{Guid.NewGuid()}.png");

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.Clear(color);

            var textColor = GetContrastColor(color);
            using var font = new Font("Arial", 24, FontStyle.Bold);
            using var brush = new SolidBrush(textColor);

            var truncatedPrompt = prompt.Length > 50 ? prompt.Substring(0, 47) + "..." : prompt;
            var textSize = graphics.MeasureString(truncatedPrompt, font);
            var x = (width - textSize.Width) / 2;
            var y = (height - textSize.Height) / 2;

            graphics.DrawString(truncatedPrompt, font, brush, x, y);

            bitmap.Save(tempPath, ImageFormat.Png);

            Logger.LogInformation("Placeholder image generated successfully at: {Path}", tempPath);
            return Task.FromResult<string?>(tempPath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate placeholder image");
            return Task.FromResult<string?>(null);
        }
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

    private static Color SelectColorForPrompt(string prompt)
    {
        var hash = prompt.GetHashCode();
        var index = Math.Abs(hash) % ProfessionalColors.Length;
        return ProfessionalColors[index];
    }

    private static Color GetContrastColor(Color background)
    {
        var brightness = (background.R * 299 + background.G * 587 + background.B * 114) / 1000;
        return brightness > 128 ? Color.Black : Color.White;
    }
}
