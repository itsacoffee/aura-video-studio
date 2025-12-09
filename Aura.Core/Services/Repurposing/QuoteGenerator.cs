using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Repurposing;

/// <summary>
/// Interface for generating quote cards from video content
/// </summary>
public interface IQuoteGenerator
{
    /// <summary>
    /// Generate a quote card based on the plan
    /// </summary>
    Task<GeneratedQuote> GenerateAsync(
        QuotePlan plan,
        CancellationToken ct = default);
}

/// <summary>
/// Generates quote cards from video quotes
/// </summary>
public class QuoteGenerator : IQuoteGenerator
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<QuoteGenerator> _logger;

    public QuoteGenerator(
        ILlmProvider llmProvider,
        ILogger<QuoteGenerator> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GeneratedQuote> GenerateAsync(
        QuotePlan plan,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating quote card for: {Quote}",
            plan.Quote.Length > 50 ? plan.Quote[..50] + "..." : plan.Quote);

        var outputDir = Path.Combine(
            Path.GetTempPath(), "AuraVideoStudio", "Quotes", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        var style = DetermineStyle(plan);
        var outputPath = Path.Combine(outputDir, "quote_card.png");

        // Generate the quote card image
        await GenerateQuoteCardAsync(plan.Quote, style, outputPath, ct).ConfigureAwait(false);

        // Generate caption suggestion
        var suggestedCaption = await GenerateCaptionAsync(plan, ct).ConfigureAwait(false);

        return new GeneratedQuote(
            Id: Guid.NewGuid().ToString(),
            Quote: plan.Quote,
            ImagePath: outputPath,
            Style: style,
            Metadata: new QuoteMetadata(
                Context: plan.Context,
                Emotion: plan.Emotion,
                Shareability: plan.Shareability,
                SuggestedCaption: suggestedCaption));
    }

    private static QuoteCardStyle DetermineStyle(QuotePlan plan)
    {
        // Determine colors based on emotion and color scheme
        var (primaryColor, textColor) = plan.ColorScheme.ToLowerInvariant() switch
        {
            "warm" => ("#FF6B35", "#FFFFFF"),
            "cool" => ("#3498DB", "#FFFFFF"),
            "neutral" => ("#34495E", "#FFFFFF"),
            "bold" => ("#E74C3C", "#FFFFFF"),
            _ => ("#2C3E50", "#FFFFFF")
        };

        // Adjust based on emotion
        (primaryColor, textColor) = plan.Emotion.ToLowerInvariant() switch
        {
            "inspiring" => ("#8E44AD", "#FFFFFF"),
            "motivational" => ("#F39C12", "#FFFFFF"),
            "thoughtful" => ("#1ABC9C", "#FFFFFF"),
            "surprising" => ("#E74C3C", "#FFFFFF"),
            "controversial" => ("#C0392B", "#FFFFFF"),
            _ => (primaryColor, textColor)
        };

        // Determine font size based on quote length
        var fontSize = plan.Quote.Length switch
        {
            < 50 => 48,
            < 100 => 36,
            < 150 => 28,
            _ => 24
        };

        return new QuoteCardStyle(
            BackgroundType: plan.SuggestedBackground,
            PrimaryColor: primaryColor,
            TextColor: textColor,
            FontFamily: "Arial",
            FontSize: fontSize);
    }

    private static async Task GenerateQuoteCardAsync(
        string quote,
        QuoteCardStyle style,
        string outputPath,
        CancellationToken ct)
    {
        // Generate a simple SVG-based quote card
        var svg = GenerateQuoteSvg(quote, style);
        
        // Write SVG to file (in production, this would be converted to PNG)
        var svgPath = Path.ChangeExtension(outputPath, ".svg");
        await File.WriteAllTextAsync(svgPath, svg, ct).ConfigureAwait(false);

        // For now, create a placeholder PNG file
        // In production, use a library like SkiaSharp to render the SVG to PNG
        await File.WriteAllTextAsync(outputPath, $"Quote card placeholder for: {quote}", ct).ConfigureAwait(false);
    }

    private static string GenerateQuoteSvg(string quote, QuoteCardStyle style)
    {
        var escapedQuote = System.Security.SecurityElement.Escape(quote);
        
        // Calculate text positioning
        var width = 1080;
        var height = 1080;
        var padding = 80;
        var textWidth = width - (padding * 2);

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg width=""{width}"" height=""{height}"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <linearGradient id=""bg"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" style=""stop-color:{style.PrimaryColor};stop-opacity:1"" />
      <stop offset=""100%"" style=""stop-color:{AdjustColorBrightness(style.PrimaryColor, -0.2)};stop-opacity:1"" />
    </linearGradient>
  </defs>
  
  <rect width=""100%"" height=""100%"" fill=""url(#bg)""/>
  
  <text x=""50%"" y=""50%"" text-anchor=""middle"" dominant-baseline=""middle""
        font-family=""{style.FontFamily}, sans-serif"" font-size=""{style.FontSize}""
        fill=""{style.TextColor}"" font-weight=""bold"">
    <tspan x=""50%"" dy=""-1.2em"">&quot;</tspan>
    <tspan x=""50%"" dy=""1.4em"">{TruncateForSvg(escapedQuote, 60)}</tspan>
    <tspan x=""50%"" dy=""1.4em"">{TruncateForSvg(escapedQuote.Length > 60 ? escapedQuote[60..] : "", 60)}</tspan>
    <tspan x=""50%"" dy=""1.2em"">&quot;</tspan>
  </text>
</svg>";
    }

    private static string AdjustColorBrightness(string hexColor, double factor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith('#') || hexColor.Length != 7)
        {
            return hexColor;
        }

        try
        {
            var r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
            var g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
            var b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

            r = Math.Clamp((int)(r * (1 + factor)), 0, 255);
            g = Math.Clamp((int)(g * (1 + factor)), 0, 255);
            b = Math.Clamp((int)(b * (1 + factor)), 0, 255);

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return hexColor;
        }
    }

    private static string TruncateForSvg(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)] + "...";
    }

    private async Task<string> GenerateCaptionAsync(QuotePlan plan, CancellationToken ct)
    {
        var prompt = $@"Generate a short, engaging social media caption for this quote.

Quote: ""{plan.Quote}""
Context: {plan.Context}
Emotion: {plan.Emotion}

The caption should:
1. Be under 100 characters
2. Encourage engagement
3. Match the emotional tone

Respond with just the caption text, no JSON or formatting.";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            return response.Trim().Trim('"');
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate caption via LLM, using default");
            return $"💬 {plan.Quote[..Math.Min(50, plan.Quote.Length)]}...";
        }
    }
}
