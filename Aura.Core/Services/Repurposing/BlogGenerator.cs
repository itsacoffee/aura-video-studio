using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Repurposing;

/// <summary>
/// Interface for generating blog posts from video content
/// </summary>
public interface IBlogGenerator
{
    /// <summary>
    /// Generate a blog post from the plan
    /// </summary>
    Task<GeneratedBlogPost> GenerateAsync(
        BlogPostPlan plan,
        CancellationToken ct = default);
}

/// <summary>
/// Generates blog posts from video scripts
/// </summary>
public partial class BlogGenerator : IBlogGenerator
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<BlogGenerator> _logger;

    public BlogGenerator(
        ILlmProvider llmProvider,
        ILogger<BlogGenerator> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GeneratedBlogPost> GenerateAsync(
        BlogPostPlan plan,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating blog post: {Title}", plan.Title);

        // Build the outline for the LLM
        var sectionsOutline = string.Join("\n\n", plan.Sections.Select(s =>
            $"## {s.Header}\n{s.Content}\nKey Points:\n{string.Join("\n", s.KeyPoints.Select(p => $"- {p}"))}"));

        var prompt = $@"Write a complete blog post based on this outline. 

Title: {plan.Title}
Meta Description: {plan.MetaDescription}

Introduction:
{plan.Introduction}

Sections:
{sectionsOutline}

Conclusion:
{plan.Conclusion}

Call to Action:
{plan.CallToAction}

Write in a conversational, engaging tone. Include:
1. A compelling introduction hook
2. Well-structured paragraphs
3. Bullet points for key takeaways
4. A strong conclusion
5. Natural transitions between sections

Output as Markdown format only.";

        var markdownContent = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);

        // Convert to other formats
        var htmlContent = ConvertMarkdownToHtml(markdownContent);
        var plainTextContent = ConvertMarkdownToPlainText(markdownContent);
        var wordCount = CountWords(plainTextContent);

        return new GeneratedBlogPost(
            Id: Guid.NewGuid().ToString(),
            Title: plan.Title,
            HtmlContent: htmlContent,
            MarkdownContent: markdownContent,
            PlainTextContent: plainTextContent,
            Metadata: new BlogPostMetadata(
                MetaDescription: plan.MetaDescription,
                Tags: plan.Tags,
                WordCount: wordCount,
                EstimatedReadTime: Math.Max(1, wordCount / 200),
                GeneratedAt: DateTime.UtcNow));
    }

    private static string ConvertMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = markdown;

        // Convert headers
        html = Header1Regex().Replace(html, "<h1>$1</h1>");
        html = Header2Regex().Replace(html, "<h2>$1</h2>");
        html = Header3Regex().Replace(html, "<h3>$1</h3>");
        html = Header4Regex().Replace(html, "<h4>$1</h4>");

        // Convert bold
        html = BoldDoubleAsteriskRegex().Replace(html, "<strong>$1</strong>");
        html = BoldDoubleUnderscoreRegex().Replace(html, "<strong>$1</strong>");

        // Convert italic
        html = ItalicSingleAsteriskRegex().Replace(html, "<em>$1</em>");
        html = ItalicSingleUnderscoreRegex().Replace(html, "<em>$1</em>");

        // Convert links
        html = LinkRegex().Replace(html, "<a href=\"$2\">$1</a>");

        // Convert unordered lists
        html = UnorderedListItemRegex().Replace(html, "<li>$1</li>");

        // Convert ordered lists
        html = OrderedListItemRegex().Replace(html, "<li>$1</li>");

        // Convert code blocks
        html = CodeBlockRegex().Replace(html, "<pre><code>$1</code></pre>");

        // Convert inline code
        html = InlineCodeRegex().Replace(html, "<code>$1</code>");

        // Convert blockquotes
        html = BlockquoteRegex().Replace(html, "<blockquote>$1</blockquote>");

        // Convert paragraphs (double newlines)
        html = ParagraphRegex().Replace(html, "</p>\n\n<p>");
        html = $"<p>{html}</p>";

        // Clean up empty paragraphs
        html = EmptyParagraphRegex().Replace(html, "");

        return html;
    }

    private static string ConvertMarkdownToPlainText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var text = markdown;

        // Remove bold formatting
        text = BoldDoubleAsteriskRegex().Replace(text, "$1");
        text = BoldDoubleUnderscoreRegex().Replace(text, "$1");

        // Remove italic formatting
        text = ItalicSingleAsteriskRegex().Replace(text, "$1");
        text = ItalicSingleUnderscoreRegex().Replace(text, "$1");

        // Remove headers
        text = HeaderCleanupRegex().Replace(text, "");

        // Remove links but keep text
        text = LinkRegex().Replace(text, "$1");

        // Remove list markers
        text = ListMarkerRegex().Replace(text, "");

        // Remove code block markers
        text = CodeBlockMarkerRegex().Replace(text, "");

        // Remove inline code backticks
        text = InlineCodeRegex().Replace(text, "$1");

        // Remove blockquote markers
        text = BlockquoteMarkerRegex().Replace(text, "");

        return text.Trim();
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Regex patterns using source generators for performance
    [GeneratedRegex(@"^# (.+)$", RegexOptions.Multiline)]
    private static partial Regex Header1Regex();

    [GeneratedRegex(@"^## (.+)$", RegexOptions.Multiline)]
    private static partial Regex Header2Regex();

    [GeneratedRegex(@"^### (.+)$", RegexOptions.Multiline)]
    private static partial Regex Header3Regex();

    [GeneratedRegex(@"^#### (.+)$", RegexOptions.Multiline)]
    private static partial Regex Header4Regex();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldDoubleAsteriskRegex();

    [GeneratedRegex(@"__(.+?)__")]
    private static partial Regex BoldDoubleUnderscoreRegex();

    [GeneratedRegex(@"\*(.+?)\*")]
    private static partial Regex ItalicSingleAsteriskRegex();

    [GeneratedRegex(@"_(.+?)_")]
    private static partial Regex ItalicSingleUnderscoreRegex();

    [GeneratedRegex(@"\[(.+?)\]\((.+?)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"^[-*+]\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex UnorderedListItemRegex();

    [GeneratedRegex(@"^\d+\.\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex OrderedListItemRegex();

    [GeneratedRegex(@"```[\s\S]*?\n([\s\S]*?)```", RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^>\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"\n\n+")]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex(@"<p>\s*</p>")]
    private static partial Regex EmptyParagraphRegex();

    [GeneratedRegex(@"^#{1,6}\s*", RegexOptions.Multiline)]
    private static partial Regex HeaderCleanupRegex();

    [GeneratedRegex(@"^[-*+]\s*", RegexOptions.Multiline)]
    private static partial Regex ListMarkerRegex();

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Multiline)]
    private static partial Regex CodeBlockMarkerRegex();

    [GeneratedRegex(@"^>\s*", RegexOptions.Multiline)]
    private static partial Regex BlockquoteMarkerRegex();
}
