using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Parser for HTML documents (.html, .htm)
/// </summary>
public class HtmlParser : IDocumentParser
{
    private readonly ILogger<HtmlParser> _logger;

    public DocumentFormat SupportedFormat => DocumentFormat.Html;
    public string[] SupportedExtensions => new[] { ".html", ".htm" };

    public HtmlParser(ILogger<HtmlParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<DocumentImportResult> ParseAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Parsing HTML document: {FileName}", fileName);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var htmlContent = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

            var plainText = StripHtmlTags(htmlContent);
            var metadata = ExtractMetadata(plainText, fileName, stream.Length, htmlContent);
            var structure = AnalyzeStructure(htmlContent, plainText);
            
            stopwatch.Stop();

            return new DocumentImportResult
            {
                Success = true,
                Metadata = metadata,
                Structure = structure,
                RawContent = plainText,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML document: {FileName}", fileName);
            stopwatch.Stop();
            
            return new DocumentImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse HTML: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    private string StripHtmlTags(string html)
    {
        var text = html;
        
        text = Regex.Replace(text, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = Regex.Replace(text, @"&nbsp;", " ");
        text = Regex.Replace(text, @"&lt;", "<");
        text = Regex.Replace(text, @"&gt;", ">");
        text = Regex.Replace(text, @"&amp;", "&");
        text = Regex.Replace(text, @"&quot;", "\"");
        text = Regex.Replace(text, @"\s+", " ");
        
        return text.Trim();
    }

    private DocumentMetadata ExtractMetadata(string content, string fileName, long fileSize, string html)
    {
        var wordCount = CountWords(content);
        
        var titleMatch = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var title = titleMatch.Success ? StripHtmlTags(titleMatch.Groups[1].Value) : null;

        var authorMatch = Regex.Match(html, @"<meta\s+name=[""']author[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        var author = authorMatch.Success ? authorMatch.Groups[1].Value : null;
        
        return new DocumentMetadata
        {
            OriginalFileName = fileName,
            Format = DocumentFormat.Html,
            FileSizeBytes = fileSize,
            ImportedAt = DateTime.UtcNow,
            WordCount = wordCount,
            CharacterCount = content.Length,
            DetectedLanguage = "en",
            Title = title,
            Author = author
        };
    }

    private DocumentStructure AnalyzeStructure(string html, string plainText)
    {
        var sections = ParseSections(html);
        var maxLevel = sections.Any() ? sections.Max(s => s.Level) : 0;
        
        var complexity = AnalyzeComplexity(plainText);
        var tone = AnalyzeTone(plainText);
        var keyConcepts = ExtractKeyConcepts(plainText);

        return new DocumentStructure
        {
            Sections = sections,
            HeadingLevels = maxLevel,
            KeyConcepts = keyConcepts,
            Complexity = complexity,
            Tone = tone
        };
    }

    private List<DocumentSection> ParseSections(string html)
    {
        var sections = new List<DocumentSection>();
        
        var headingMatches = Regex.Matches(html, @"<h([1-6])[^>]*>(.*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        for (var i = 0; i < headingMatches.Count; i++)
        {
            var match = headingMatches[i];
            var level = int.Parse(match.Groups[1].Value);
            var heading = StripHtmlTags(match.Groups[2].Value);
            
            var startPos = match.Index + match.Length;
            var endPos = i < headingMatches.Count - 1 ? headingMatches[i + 1].Index : html.Length;
            var sectionHtml = html[startPos..endPos];
            var content = StripHtmlTags(sectionHtml);
            
            var wordCount = CountWords(content);
            var duration = TimeSpan.FromSeconds(wordCount / 2.5);

            sections.Add(new DocumentSection
            {
                Level = level,
                Heading = heading,
                Content = content,
                WordCount = wordCount,
                EstimatedSpeechDuration = duration,
                Examples = ExtractExamples(content),
                VisualOpportunities = ExtractVisualOpportunitiesFromHtml(sectionHtml)
            });
        }

        if (sections.Count == 0)
        {
            var plainText = StripHtmlTags(html);
            var wordCount = CountWords(plainText);
            sections.Add(new DocumentSection
            {
                Level = 1,
                Heading = "Content",
                Content = plainText,
                WordCount = wordCount,
                EstimatedSpeechDuration = TimeSpan.FromSeconds(wordCount / 2.5)
            });
        }

        return sections;
    }

    private DocumentComplexity AnalyzeComplexity(string content)
    {
        var sentences = Regex.Split(content, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        var avgSentenceLength = sentences.Count > 0 ? words.Length / sentences.Count : 0;
        var complexWords = words.Count(w => CountSyllables(w) >= 3);
        var readingLevel = CalculateFleschKincaid(words.Length, sentences.Count, CountTotalSyllables(words));

        return new DocumentComplexity
        {
            ReadingLevel = readingLevel,
            TechnicalDensity = 0.5,
            AbstractionLevel = 0.5,
            AverageSentenceLength = avgSentenceLength,
            ComplexWordCount = complexWords,
            ComplexityDescription = GetComplexityDescription(readingLevel)
        };
    }

    private DocumentTone AnalyzeTone(string content)
    {
        return new DocumentTone
        {
            PrimaryTone = "Professional",
            FormalityLevel = 0.6,
            WritingStyle = "Web Article",
            ToneIndicators = new List<string>()
        };
    }

    private List<string> ExtractKeyConcepts(string content)
    {
        var words = content.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var wordFrequency = new Dictionary<string, int>();

        foreach (var word in words)
        {
            var cleaned = word.ToLowerInvariant().Trim();
            if (cleaned.Length > 4 && !IsCommonWord(cleaned))
            {
                wordFrequency[cleaned] = wordFrequency.GetValueOrDefault(cleaned) + 1;
            }
        }

        return wordFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private List<string> ExtractExamples(string content)
    {
        var examples = new List<string>();
        var patterns = new[] { "for example", "e.g.", "such as", "for instance" };
        
        foreach (var pattern in patterns)
        {
            var index = content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = Math.Max(0, index - 20);
                var length = Math.Min(100, content.Length - start);
                examples.Add(content.Substring(start, length).Trim());
            }
        }
        
        return examples.Take(5).ToList();
    }

    private List<string> ExtractVisualOpportunitiesFromHtml(string html)
    {
        var opportunities = new List<string>();
        
        var imgMatches = Regex.Matches(html, @"<img[^>]+alt=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        foreach (Match match in imgMatches)
        {
            opportunities.Add($"Image: {match.Groups[1].Value}");
        }

        return opportunities;
    }

    private int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private int CountSyllables(string word)
    {
        word = word.ToLowerInvariant();
        var vowels = new[] { 'a', 'e', 'i', 'o', 'u', 'y' };
        var syllableCount = 0;
        var previousWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = vowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                syllableCount++;
            }
            previousWasVowel = isVowel;
        }

        if (word.EndsWith("e"))
        {
            syllableCount--;
        }

        return Math.Max(1, syllableCount);
    }

    private int CountTotalSyllables(string[] words)
    {
        return words.Sum(CountSyllables);
    }

    private double CalculateFleschKincaid(int wordCount, int sentenceCount, int syllableCount)
    {
        if (sentenceCount == 0 || wordCount == 0) return 0;
        
        var asl = (double)wordCount / sentenceCount;
        var asw = (double)syllableCount / wordCount;
        
        return 0.39 * asl + 11.8 * asw - 15.59;
    }

    private string GetComplexityDescription(double gradeLevel)
    {
        return gradeLevel switch
        {
            < 6 => "Elementary",
            < 9 => "Middle School",
            < 13 => "High School",
            < 16 => "College",
            _ => "Graduate"
        };
    }

    private bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string> { "the", "and", "that", "this", "with", "from", "have", "been", "were", "will", "would", "could", "should" };
        return commonWords.Contains(word);
    }
}
