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
/// Parser for Markdown documents (.md)
/// </summary>
public class MarkdownParser : IDocumentParser
{
    private readonly ILogger<MarkdownParser> _logger;
    private const double DefaultWordsPerSecond = 2.5; // 150 words per minute

    public DocFormat SupportedFormat => DocFormat.Markdown;
    public string[] SupportedExtensions => new[] { ".md", ".markdown" };

    public MarkdownParser(ILogger<MarkdownParser> logger)
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
            _logger.LogInformation("Parsing Markdown document: {FileName}", fileName);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

            var metadata = ExtractMetadata(content, fileName, stream.Length);
            var structure = AnalyzeStructure(content);
            
            stopwatch.Stop();

            return new DocumentImportResult
            {
                Success = true,
                Metadata = metadata,
                Structure = structure,
                RawContent = content,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Markdown document: {FileName}", fileName);
            stopwatch.Stop();
            
            return new DocumentImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse Markdown: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    private DocumentMetadata ExtractMetadata(string content, string fileName, long fileSize)
    {
        var plainText = StripMarkdown(content);
        var wordCount = CountWords(plainText);
        
        var metadata = new DocumentMetadata
        {
            OriginalFileName = fileName,
            Format = DocFormat.Markdown,
            FileSizeBytes = fileSize,
            ImportedAt = DateTime.UtcNow,
            WordCount = wordCount,
            CharacterCount = plainText.Length,
            DetectedLanguage = "en"
        };

        var frontMatter = ExtractFrontMatter(content);
        if (frontMatter != null)
        {
            metadata = metadata with
            {
                Title = frontMatter.GetValueOrDefault("title"),
                Author = frontMatter.GetValueOrDefault("author"),
                CustomMetadata = frontMatter
            };
        }

        return metadata;
    }

    private DocumentStructure AnalyzeStructure(string content)
    {
        var sections = ParseSections(content);
        var maxLevel = sections.Any() ? sections.Max(s => s.Level) : 0;
        
        var plainText = StripMarkdown(content);
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

    private Dictionary<string, string>? ExtractFrontMatter(string content)
    {
        var frontMatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---", RegexOptions.Singleline);
        if (!frontMatterMatch.Success)
        {
            return null;
        }

        var frontMatter = new Dictionary<string, string>();
        var lines = frontMatterMatch.Groups[1].Value.Split('\n');
        
        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim().Trim('"', '\'');
                frontMatter[key] = value;
            }
        }

        return frontMatter;
    }

    private List<DocumentSection> ParseSections(string content)
    {
        var sections = new List<DocumentSection>();
        var lines = content.Split('\n');
        
        DocumentSection? currentSection = null;
        var currentContent = new StringBuilder();
        var sectionStack = new Stack<DocumentSection>();

        foreach (var line in lines)
        {
            var headingMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            
            if (headingMatch.Success)
            {
                if (currentSection != null && currentContent.Length > 0)
                {
                    currentSection = FinalizeSection(currentSection, currentContent.ToString());
                    
                    while (sectionStack.Count > 0 && sectionStack.Peek().Level >= currentSection.Level)
                    {
                        var completed = sectionStack.Pop();
                        if (!sectionStack.Any())
                        {
                            sections.Add(completed);
                        }
                        else
                        {
                            sectionStack.Peek().Subsections.Add(completed);
                        }
                    }
                    
                    sectionStack.Push(currentSection);
                }

                var level = headingMatch.Groups[1].Value.Length;
                var heading = headingMatch.Groups[2].Value.Trim();
                
                currentSection = new DocumentSection
                {
                    Level = level,
                    Heading = heading,
                    Subsections = new List<DocumentSection>()
                };
                currentContent.Clear();
            }
            else if (currentSection != null)
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentSection != null && currentContent.Length > 0)
        {
            currentSection = FinalizeSection(currentSection, currentContent.ToString());
            sectionStack.Push(currentSection);
        }

        while (sectionStack.Any())
        {
            var completed = sectionStack.Pop();
            if (!sectionStack.Any())
            {
                sections.Add(completed);
            }
            else
            {
                sectionStack.Peek().Subsections.Add(completed);
            }
        }

        return sections;
    }

    private DocumentSection FinalizeSection(DocumentSection section, string content)
    {
        var plainText = StripMarkdown(content);
        var wordCount = CountWords(plainText);
        var duration = TimeSpan.FromSeconds(wordCount / DefaultWordsPerSecond);

        return section with
        {
            Content = plainText,
            WordCount = wordCount,
            EstimatedSpeechDuration = duration,
            Examples = ExtractExamples(plainText),
            VisualOpportunities = ExtractVisualOpportunities(content)
        };
    }

    private string StripMarkdown(string markdown)
    {
        var text = markdown;
        
        text = Regex.Replace(text, @"^---\s*\n.*?\n---\s*\n", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"!\[.*?\]\(.*?\)", "");
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        text = Regex.Replace(text, @"`{1,3}[^`]+`{1,3}", "");
        text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\*\*([^\*]+)\*\*", "$1");
        text = Regex.Replace(text, @"\*([^\*]+)\*", "$1");
        text = Regex.Replace(text, @"__([^_]+)__", "$1");
        text = Regex.Replace(text, @"_([^_]+)_", "$1");
        text = Regex.Replace(text, @"~~([^~]+)~~", "$1");
        text = Regex.Replace(text, @"^[-*+]\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\d+\.\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^>\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        
        return text.Trim();
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
            TechnicalDensity = CalculateTechnicalDensity(content),
            AbstractionLevel = 0.5,
            AverageSentenceLength = avgSentenceLength,
            ComplexWordCount = complexWords,
            ComplexityDescription = GetComplexityDescription(readingLevel)
        };
    }

    private DocumentTone AnalyzeTone(string content)
    {
        var formalityScore = CalculateFormalityScore(content);
        
        return new DocumentTone
        {
            PrimaryTone = DeterminePrimaryTone(content),
            FormalityLevel = formalityScore,
            WritingStyle = formalityScore > 0.6 ? "Formal" : "Conversational",
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

    private List<string> ExtractVisualOpportunities(string markdown)
    {
        var opportunities = new List<string>();
        
        var imageMatches = Regex.Matches(markdown, @"!\[(.*?)\]\((.*?)\)");
        foreach (Match match in imageMatches)
        {
            var altText = match.Groups[1].Value;
            opportunities.Add($"Image referenced: {altText}");
        }

        var codeBlocks = Regex.Matches(markdown, @"```(\w+)?");
        if (codeBlocks.Count > 0)
        {
            opportunities.Add($"Code blocks found ({codeBlocks.Count}) - consider syntax highlighting visuals");
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

    private double CalculateTechnicalDensity(string content)
    {
        var technicalTerms = new[] { "algorithm", "system", "process", "method", "analysis", "implementation", "framework" };
        var foundCount = technicalTerms.Count(term => content.Contains(term, StringComparison.OrdinalIgnoreCase));
        return foundCount / (double)technicalTerms.Length;
    }

    private double CalculateFormalityScore(string content)
    {
        var formalMarkers = new[] { "therefore", "however", "furthermore", "consequently", "moreover" };
        var informalMarkers = new[] { "gonna", "wanna", "yeah", "stuff", "things", "cool" };
        
        var formalCount = formalMarkers.Count(m => content.Contains(m, StringComparison.OrdinalIgnoreCase));
        var informalCount = informalMarkers.Count(m => content.Contains(m, StringComparison.OrdinalIgnoreCase));
        
        var total = formalCount + informalCount;
        return total == 0 ? 0.5 : formalCount / (double)total;
    }

    private string DeterminePrimaryTone(string content)
    {
        var lowerContent = content.ToLowerInvariant();
        
        if (lowerContent.Contains("research") || lowerContent.Contains("study"))
            return "Academic";
        if (lowerContent.Contains("you") || lowerContent.Contains("your"))
            return "Conversational";
        
        return "Professional";
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
