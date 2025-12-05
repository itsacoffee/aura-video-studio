using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using ProviderTimeline = Aura.Core.Providers.Timeline;

namespace Aura.Core.Services.Repurposing;

/// <summary>
/// Interface for video repurposing service
/// </summary>
public interface IRepurposingService
{
    /// <summary>
    /// Analyze a video for repurposing opportunities
    /// </summary>
    Task<RepurposingPlan> AnalyzeForRepurposingAsync(
        VideoGenerationResult sourceVideo,
        RepurposingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a repurposing plan to generate content variants
    /// </summary>
    Task<RepurposingResult> ExecuteRepurposingAsync(
        RepurposingPlan plan,
        IProgress<RepurposingProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Service for repurposing video content into multiple formats
/// </summary>
public class RepurposingService : IRepurposingService
{
    private readonly ILlmProvider _llmProvider;
    private readonly IShortsExtractor _shortsExtractor;
    private readonly IBlogGenerator _blogGenerator;
    private readonly IQuoteGenerator _quoteGenerator;
    private readonly IAspectConverter _aspectConverter;
    private readonly ILogger<RepurposingService> _logger;

    public RepurposingService(
        ILlmProvider llmProvider,
        IShortsExtractor shortsExtractor,
        IBlogGenerator blogGenerator,
        IQuoteGenerator quoteGenerator,
        IAspectConverter aspectConverter,
        ILogger<RepurposingService> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _shortsExtractor = shortsExtractor ?? throw new ArgumentNullException(nameof(shortsExtractor));
        _blogGenerator = blogGenerator ?? throw new ArgumentNullException(nameof(blogGenerator));
        _quoteGenerator = quoteGenerator ?? throw new ArgumentNullException(nameof(quoteGenerator));
        _aspectConverter = aspectConverter ?? throw new ArgumentNullException(nameof(aspectConverter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RepurposingPlan> AnalyzeForRepurposingAsync(
        VideoGenerationResult sourceVideo,
        RepurposingOptions options,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing video for repurposing opportunities");

        var timeline = sourceVideo.ProviderTimeline
            ?? throw new ArgumentException("Source video must have timeline data", nameof(sourceVideo));

        // Parallel analysis for all repurposing types
        var shortsTask = options.GenerateShorts
            ? IdentifyShortsOpportunitiesAsync(timeline, sourceVideo.OutputPath, options.MaxShortsCount, ct)
            : Task.FromResult(Array.Empty<ShortsPlan>() as IReadOnlyList<ShortsPlan>);

        var blogTask = options.GenerateBlogPost
            ? PlanBlogPostAsync(timeline, ct)
            : Task.FromResult<BlogPostPlan?>(null);

        var quotesTask = options.GenerateSocialQuotes
            ? IdentifyQuoteOpportunitiesAsync(timeline, options.MaxQuotesCount, ct)
            : Task.FromResult(Array.Empty<QuotePlan>() as IReadOnlyList<QuotePlan>);

        await Task.WhenAll(shortsTask, blogTask, quotesTask).ConfigureAwait(false);

        var aspectPlans = options.GenerateAlternateAspects
            ? PlanAspectVariants(sourceVideo, options.TargetAspects)
            : Array.Empty<AspectVariantPlan>();

        var totalDuration = timeline.Scenes.Aggregate(TimeSpan.Zero, (sum, scene) => sum + scene.Duration);

        return new RepurposingPlan(
            SourceVideoId: sourceVideo.CorrelationId ?? Guid.NewGuid().ToString(),
            Shorts: await shortsTask.ConfigureAwait(false),
            BlogPost: await blogTask.ConfigureAwait(false),
            Quotes: await quotesTask.ConfigureAwait(false),
            AspectVariants: aspectPlans,
            Metadata: new RepurposingMetadata(
                SourceDuration: totalDuration,
                SceneCount: timeline.Scenes.Count,
                AnalyzedAt: DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<RepurposingResult> ExecuteRepurposingAsync(
        RepurposingPlan plan,
        IProgress<RepurposingProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Executing repurposing plan for video {VideoId}", plan.SourceVideoId);
        var startTime = DateTime.UtcNow;

        var shorts = new List<GeneratedShort>();
        var quotes = new List<GeneratedQuote>();
        var aspectVariants = new List<GeneratedAspectVariant>();
        GeneratedBlogPost? blogPost = null;

        var totalItems = plan.Shorts.Count + plan.Quotes.Count + plan.AspectVariants.Count +
                        (plan.BlogPost != null ? 1 : 0);
        var processedItems = 0;

        // Generate shorts
        foreach (var shortPlan in plan.Shorts)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new RepurposingProgress(
                "Generating Shorts",
                (int)((double)processedItems / totalItems * 100),
                shortPlan.Title,
                $"Creating short: {shortPlan.Title}"));

            try
            {
                var generatedShort = await _shortsExtractor.ExtractShortAsync(shortPlan, ct).ConfigureAwait(false);
                shorts.Add(generatedShort);
                _logger.LogInformation("Generated short: {Title}", shortPlan.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate short: {Title}", shortPlan.Title);
            }

            processedItems++;
        }

        // Generate blog post
        if (plan.BlogPost != null)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new RepurposingProgress(
                "Generating Blog Post",
                (int)((double)processedItems / totalItems * 100),
                plan.BlogPost.Title,
                "Creating blog post content"));

            try
            {
                blogPost = await _blogGenerator.GenerateAsync(plan.BlogPost, ct).ConfigureAwait(false);
                _logger.LogInformation("Generated blog post: {Title}", plan.BlogPost.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate blog post: {Title}", plan.BlogPost.Title);
            }

            processedItems++;
        }

        // Generate quotes
        foreach (var quotePlan in plan.Quotes)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new RepurposingProgress(
                "Generating Quotes",
                (int)((double)processedItems / totalItems * 100),
                quotePlan.Quote.Length > 50 ? quotePlan.Quote[..50] + "..." : quotePlan.Quote,
                "Creating quote card"));

            try
            {
                var generatedQuote = await _quoteGenerator.GenerateAsync(quotePlan, ct).ConfigureAwait(false);
                quotes.Add(generatedQuote);
                _logger.LogInformation("Generated quote card");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate quote card");
            }

            processedItems++;
        }

        // Generate aspect variants
        foreach (var aspectPlan in plan.AspectVariants)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new RepurposingProgress(
                "Generating Aspect Variants",
                (int)((double)processedItems / totalItems * 100),
                $"{aspectPlan.TargetAspect}",
                $"Converting to {aspectPlan.TargetAspect}"));

            try
            {
                var variant = await _aspectConverter.ConvertAsync(aspectPlan, ct).ConfigureAwait(false);
                aspectVariants.Add(variant);
                _logger.LogInformation("Generated aspect variant: {Aspect}", aspectPlan.TargetAspect);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate aspect variant: {Aspect}", aspectPlan.TargetAspect);
            }

            processedItems++;
        }

        progress?.Report(new RepurposingProgress(
            "Complete",
            100,
            "All content generated",
            "Repurposing complete"));

        var processingTime = DateTime.UtcNow - startTime;

        return new RepurposingResult(
            SourceVideoId: plan.SourceVideoId,
            Shorts: shorts,
            BlogPost: blogPost,
            Quotes: quotes,
            AspectVariants: aspectVariants,
            Stats: new RepurposingStats(
                ShortsGenerated: shorts.Count,
                QuotesGenerated: quotes.Count,
                AspectVariantsGenerated: aspectVariants.Count,
                BlogPostGenerated: blogPost != null,
                TotalProcessingTime: processingTime));
    }

    private async Task<IReadOnlyList<ShortsPlan>> IdentifyShortsOpportunitiesAsync(
        ProviderTimeline timeline,
        string sourceVideoPath,
        int maxCount,
        CancellationToken ct)
    {
        var scriptContent = string.Join("\n\n", timeline.Scenes.Select((s, i) =>
            $"Scene {i + 1} ({s.Duration.TotalSeconds:F1}s): {s.Script}"));

        var prompt = $@"Analyze this video script and identify the {maxCount} best segments for short-form content (15-60 seconds). 

Look for:
1. Strong hooks that grab attention immediately
2. Complete thoughts or mini-stories
3. Key insights or surprising revelations
4. Memorable quotes or powerful statements
5. Emotionally engaging moments

Script:
{scriptContent}

Respond with JSON array only, no additional text:
[
  {{
    ""title"": ""Catchy title for this short"",
    ""startSceneIndex"": 0,
    ""endSceneIndex"": 1,
    ""hookText"": ""Attention-grabbing opening line"",
    ""estimatedDurationSeconds"": 30,
    ""viralPotential"": 0.8,
    ""platform"": ""tiktok"",
    ""reasoning"": ""Why this segment will perform well""
  }}
]";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            return ParseShortPlans(response, timeline, sourceVideoPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to identify shorts opportunities via LLM, returning empty list");
            return Array.Empty<ShortsPlan>();
        }
    }

    private IReadOnlyList<ShortsPlan> ParseShortPlans(string response, ProviderTimeline timeline, string sourceVideoPath)
    {
        try
        {
            // Extract JSON from response (handle markdown code blocks)
            var jsonContent = ExtractJsonFromResponse(response);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty JSON response from LLM for shorts plans");
                return Array.Empty<ShortsPlan>();
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var plans = new List<ShortsPlan>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (!element.TryGetProperty("startSceneIndex", out var startProp) ||
                    !element.TryGetProperty("endSceneIndex", out var endProp))
                {
                    _logger.LogWarning("Missing scene index properties in shorts plan JSON");
                    continue;
                }

                var startIndex = startProp.GetInt32();
                var endIndex = endProp.GetInt32();

                // Validate indices (require at least one scene)
                if (startIndex < 0 || endIndex >= timeline.Scenes.Count || startIndex > endIndex)
                {
                    _logger.LogWarning("Invalid scene indices in shorts plan: {Start}-{End}", startIndex, endIndex);
                    continue;
                }

                // Get estimated duration with null check
                var estimatedDurationSeconds = 30.0; // Default to 30 seconds
                if (element.TryGetProperty("estimatedDurationSeconds", out var durationProp))
                {
                    estimatedDurationSeconds = durationProp.GetDouble();
                }

                var plan = new ShortsPlan(
                    Title: element.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Untitled Short" : "Untitled Short",
                    StartSceneIndex: startIndex,
                    EndSceneIndex: endIndex,
                    HookText: element.TryGetProperty("hookText", out var hookProp) ? hookProp.GetString() ?? string.Empty : string.Empty,
                    EstimatedDuration: TimeSpan.FromSeconds(estimatedDurationSeconds),
                    ViralPotential: element.TryGetProperty("viralPotential", out var vp) ? vp.GetDouble() : 0.5,
                    Platform: element.TryGetProperty("platform", out var platformProp) ? platformProp.GetString() ?? "tiktok" : "tiktok",
                    Reasoning: element.TryGetProperty("reasoning", out var r) ? r.GetString() ?? string.Empty : string.Empty,
                    SourceTimeline: timeline,
                    SourceVideoPath: sourceVideoPath);

                plans.Add(plan);
            }

            return plans;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse shorts plans JSON response");
            return Array.Empty<ShortsPlan>();
        }
    }

    private async Task<BlogPostPlan?> PlanBlogPostAsync(ProviderTimeline timeline, CancellationToken ct)
    {
        var fullScript = string.Join("\n\n", timeline.Scenes.Select(s => s.Script));

        var prompt = $@"Convert this video script into a blog post outline. 

Script:
{fullScript}

Create an outline with:
1. SEO-friendly title
2. Meta description (150-160 chars)
3. Introduction hook
4. Main sections with headers (H2)
5. Key takeaways / bullet points
6. Conclusion with CTA
7. Suggested tags/keywords

Respond with JSON only, no additional text:
{{
  ""title"": ""..."",
  ""metaDescription"": ""..."",
  ""introduction"": ""..."",
  ""sections"": [
    {{ ""header"": ""..."", ""content"": ""..."", ""keyPoints"": [""...""] }}
  ],
  ""conclusion"": ""..."",
  ""callToAction"": ""..."",
  ""tags"": [""...""],
  ""estimatedReadTime"": 5
}}";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            return ParseBlogPostPlan(response, timeline);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to plan blog post via LLM");
            return null;
        }
    }

    private BlogPostPlan? ParseBlogPostPlan(string response, ProviderTimeline timeline)
    {
        try
        {
            var jsonContent = ExtractJsonFromResponse(response);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var sections = new List<BlogSection>();
            if (root.TryGetProperty("sections", out var sectionsElement))
            {
                foreach (var section in sectionsElement.EnumerateArray())
                {
                    var keyPoints = new List<string>();
                    if (section.TryGetProperty("keyPoints", out var kpElement))
                    {
                        foreach (var kp in kpElement.EnumerateArray())
                        {
                            var point = kp.GetString();
                            if (!string.IsNullOrEmpty(point))
                            {
                                keyPoints.Add(point);
                            }
                        }
                    }

                    sections.Add(new BlogSection(
                        Header: section.GetProperty("header").GetString() ?? string.Empty,
                        Content: section.GetProperty("content").GetString() ?? string.Empty,
                        KeyPoints: keyPoints));
                }
            }

            var tags = new List<string>();
            if (root.TryGetProperty("tags", out var tagsElement))
            {
                foreach (var tag in tagsElement.EnumerateArray())
                {
                    var tagStr = tag.GetString();
                    if (!string.IsNullOrEmpty(tagStr))
                    {
                        tags.Add(tagStr);
                    }
                }
            }

            return new BlogPostPlan(
                Title: root.GetProperty("title").GetString() ?? "Untitled Blog Post",
                MetaDescription: root.GetProperty("metaDescription").GetString() ?? string.Empty,
                Introduction: root.GetProperty("introduction").GetString() ?? string.Empty,
                Sections: sections,
                Conclusion: root.GetProperty("conclusion").GetString() ?? string.Empty,
                CallToAction: root.TryGetProperty("callToAction", out var cta) ? cta.GetString() ?? string.Empty : string.Empty,
                Tags: tags,
                EstimatedReadTime: root.TryGetProperty("estimatedReadTime", out var ert) ? ert.GetInt32() : 5,
                SourceTimeline: timeline);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse blog post plan JSON response");
            return null;
        }
    }

    private async Task<IReadOnlyList<QuotePlan>> IdentifyQuoteOpportunitiesAsync(
        ProviderTimeline timeline,
        int maxCount,
        CancellationToken ct)
    {
        var fullScript = string.Join(" ", timeline.Scenes.Select(s => s.Script));

        var prompt = $@"Extract the {maxCount} most shareable quotes from this video script.

Look for:
1. Insightful statements
2. Memorable one-liners
3. Surprising facts or statistics
4. Motivational or inspiring lines
5. Controversial or thought-provoking statements

Script:
{fullScript}

Respond with JSON array only, no additional text:
[
  {{
    ""quote"": ""The exact quote text"",
    ""context"": ""Brief context for the quote"",
    ""emotion"": ""inspiring"",
    ""suggestedBackground"": ""gradient"",
    ""colorScheme"": ""warm"",
    ""shareability"": 0.9
  }}
]";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            return ParseQuotePlans(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to identify quote opportunities via LLM");
            return Array.Empty<QuotePlan>();
        }
    }

    private IReadOnlyList<QuotePlan> ParseQuotePlans(string response)
    {
        try
        {
            var jsonContent = ExtractJsonFromResponse(response);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return Array.Empty<QuotePlan>();
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var plans = new List<QuotePlan>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var quote = element.GetProperty("quote").GetString();
                if (string.IsNullOrWhiteSpace(quote))
                {
                    continue;
                }

                plans.Add(new QuotePlan(
                    Quote: quote,
                    Context: element.TryGetProperty("context", out var ctx) ? ctx.GetString() ?? string.Empty : string.Empty,
                    Emotion: element.TryGetProperty("emotion", out var em) ? em.GetString() ?? "neutral" : "neutral",
                    SuggestedBackground: element.TryGetProperty("suggestedBackground", out var bg) ? bg.GetString() ?? "gradient" : "gradient",
                    ColorScheme: element.TryGetProperty("colorScheme", out var cs) ? cs.GetString() ?? "neutral" : "neutral",
                    Shareability: element.TryGetProperty("shareability", out var sh) ? sh.GetDouble() : 0.5));
            }

            return plans;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse quote plans JSON response");
            return Array.Empty<QuotePlan>();
        }
    }

    private AspectVariantPlan[] PlanAspectVariants(
        VideoGenerationResult source,
        IReadOnlyList<Aspect>? targetAspects)
    {
        // Default to widescreen 16:9 as source aspect
        var sourceAspect = Aspect.Widescreen16x9;

        var targets = targetAspects ?? new[]
        {
            Aspect.Vertical9x16,  // TikTok, Reels, Shorts
            Aspect.Square1x1     // Instagram Feed, Twitter
        };

        return targets
            .Where(a => a != sourceAspect)
            .Select(targetAspect => new AspectVariantPlan(
                SourceVideoPath: source.OutputPath,
                SourceAspect: sourceAspect,
                TargetAspect: targetAspect,
                CropStrategy: DetermineCropStrategy(sourceAspect, targetAspect),
                SourceTimeline: source.ProviderTimeline))
            .ToArray();
    }

    private static CropStrategy DetermineCropStrategy(Aspect source, Aspect target)
    {
        // 16:9 to 9:16 needs smart cropping
        if (source == Aspect.Widescreen16x9 && target == Aspect.Vertical9x16)
        {
            return CropStrategy.SmartCenter;
        }

        // 16:9 to 1:1 can use center crop
        if (source == Aspect.Widescreen16x9 && target == Aspect.Square1x1)
        {
            return CropStrategy.CenterCrop;
        }

        return CropStrategy.CenterCrop;
    }

    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return string.Empty;
        }

        var trimmed = response.Trim();

        // Handle markdown code blocks
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var endIndex = trimmed.IndexOf("```", 7, StringComparison.Ordinal);
            if (endIndex > 0)
            {
                return trimmed.Substring(7, endIndex - 7).Trim();
            }
        }

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var startIndex = trimmed.IndexOf('\n');
            if (startIndex > 0)
            {
                var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                }
            }
        }

        // If already looks like JSON, return as-is
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        return trimmed;
    }
}
