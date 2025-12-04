using System;
using System.Collections.Generic;
using Aura.Core.Models;
using ProviderTimeline = Aura.Core.Providers.Timeline;

namespace Aura.Core.Services.Repurposing;

/// <summary>
/// Options for video repurposing operations
/// </summary>
public record RepurposingOptions(
    bool GenerateShorts = true,
    bool GenerateBlogPost = true,
    bool GenerateSocialQuotes = true,
    bool GenerateAlternateAspects = true,
    int MaxShortsCount = 3,
    int MaxQuotesCount = 5,
    IReadOnlyList<Aspect>? TargetAspects = null);

/// <summary>
/// Plan for repurposing a video into multiple content formats
/// </summary>
public record RepurposingPlan(
    string SourceVideoId,
    IReadOnlyList<ShortsPlan> Shorts,
    BlogPostPlan? BlogPost,
    IReadOnlyList<QuotePlan> Quotes,
    IReadOnlyList<AspectVariantPlan> AspectVariants,
    RepurposingMetadata Metadata);

/// <summary>
/// Result of executing a repurposing plan
/// </summary>
public record RepurposingResult(
    string SourceVideoId,
    IReadOnlyList<GeneratedShort> Shorts,
    GeneratedBlogPost? BlogPost,
    IReadOnlyList<GeneratedQuote> Quotes,
    IReadOnlyList<GeneratedAspectVariant> AspectVariants,
    RepurposingStats Stats);

/// <summary>
/// Metadata about the repurposing analysis
/// </summary>
public record RepurposingMetadata(
    TimeSpan SourceDuration,
    int SceneCount,
    DateTime AnalyzedAt);

/// <summary>
/// Statistics about the repurposing operation
/// </summary>
public record RepurposingStats(
    int ShortsGenerated,
    int QuotesGenerated,
    int AspectVariantsGenerated,
    bool BlogPostGenerated,
    TimeSpan TotalProcessingTime);

/// <summary>
/// Progress update for repurposing operations
/// </summary>
public record RepurposingProgress(
    string Stage,
    int PercentComplete,
    string CurrentItem,
    string Message);

/// <summary>
/// Plan for extracting a short-form video
/// </summary>
public record ShortsPlan(
    string Title,
    int StartSceneIndex,
    int EndSceneIndex,
    string HookText,
    TimeSpan EstimatedDuration,
    double ViralPotential,
    string Platform,
    string Reasoning,
    ProviderTimeline SourceTimeline,
    string SourceVideoPath);

/// <summary>
/// Generated short-form video output
/// </summary>
public record GeneratedShort(
    string Id,
    string Title,
    string OutputPath,
    TimeSpan Duration,
    Aspect Aspect,
    string Platform,
    string ThumbnailPath,
    ShortMetadata Metadata);

/// <summary>
/// Metadata for a generated short
/// </summary>
public record ShortMetadata(
    string HookText,
    double ViralPotential,
    string SuggestedCaption,
    string[] SuggestedHashtags);

/// <summary>
/// Plan for generating a blog post from video content
/// </summary>
public record BlogPostPlan(
    string Title,
    string MetaDescription,
    string Introduction,
    IReadOnlyList<BlogSection> Sections,
    string Conclusion,
    string CallToAction,
    IReadOnlyList<string> Tags,
    int EstimatedReadTime,
    ProviderTimeline SourceTimeline);

/// <summary>
/// Section within a blog post
/// </summary>
public record BlogSection(
    string Header,
    string Content,
    IReadOnlyList<string> KeyPoints);

/// <summary>
/// Generated blog post output
/// </summary>
public record GeneratedBlogPost(
    string Id,
    string Title,
    string HtmlContent,
    string MarkdownContent,
    string PlainTextContent,
    BlogPostMetadata Metadata);

/// <summary>
/// Metadata for a generated blog post
/// </summary>
public record BlogPostMetadata(
    string MetaDescription,
    IReadOnlyList<string> Tags,
    int WordCount,
    int EstimatedReadTime,
    DateTime GeneratedAt);

/// <summary>
/// Plan for generating a quote card
/// </summary>
public record QuotePlan(
    string Quote,
    string Context,
    string Emotion,
    string SuggestedBackground,
    string ColorScheme,
    double Shareability);

/// <summary>
/// Generated quote card output
/// </summary>
public record GeneratedQuote(
    string Id,
    string Quote,
    string ImagePath,
    QuoteCardStyle Style,
    QuoteMetadata Metadata);

/// <summary>
/// Visual style for a quote card
/// </summary>
public record QuoteCardStyle(
    string BackgroundType,
    string PrimaryColor,
    string TextColor,
    string FontFamily,
    int FontSize);

/// <summary>
/// Metadata for a generated quote
/// </summary>
public record QuoteMetadata(
    string Context,
    string Emotion,
    double Shareability,
    string SuggestedCaption);

/// <summary>
/// Plan for generating an aspect ratio variant
/// </summary>
public record AspectVariantPlan(
    string SourceVideoPath,
    Aspect SourceAspect,
    Aspect TargetAspect,
    CropStrategy CropStrategy,
    ProviderTimeline? SourceTimeline);

/// <summary>
/// Generated aspect ratio variant output
/// </summary>
public record GeneratedAspectVariant(
    string Id,
    string OutputPath,
    Aspect Aspect,
    int Width,
    int Height,
    TimeSpan Duration);

/// <summary>
/// Strategy for cropping video during aspect ratio conversion
/// </summary>
public enum CropStrategy
{
    /// <summary>
    /// Center crop without smart detection
    /// </summary>
    CenterCrop,

    /// <summary>
    /// Smart center detection for subjects
    /// </summary>
    SmartCenter,

    /// <summary>
    /// Letterbox or pillarbox with padding
    /// </summary>
    Letterbox,

    /// <summary>
    /// Fill by stretching (may distort)
    /// </summary>
    Stretch
}
