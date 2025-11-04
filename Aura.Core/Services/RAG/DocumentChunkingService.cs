using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.RAG;
using Aura.Core.Models.Content;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Service for chunking documents into smaller pieces for embedding and retrieval
/// </summary>
public class DocumentChunkingService
{
    private readonly ILogger<DocumentChunkingService> _logger;
    private static readonly Regex SentenceRegex = new(@"(?<=[.!?])\s+", RegexOptions.Compiled);
    private static readonly Regex ParagraphRegex = new(@"\n\s*\n", RegexOptions.Compiled);

    public DocumentChunkingService(ILogger<DocumentChunkingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Chunks a document import result into smaller pieces for embedding
    /// </summary>
    public Task<List<DocumentChunk>> ChunkDocumentAsync(
        DocumentImportResult document,
        ChunkingConfig config,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Chunking document: {FileName} using {Strategy} strategy",
            document.Metadata.OriginalFileName, config.Strategy);

        var chunks = config.Strategy switch
        {
            ChunkingStrategy.Fixed => ChunkByFixedSize(document, config),
            ChunkingStrategy.Semantic => ChunkBySemantic(document, config),
            ChunkingStrategy.Sentence => ChunkBySentence(document, config),
            ChunkingStrategy.Paragraph => ChunkByParagraph(document, config),
            _ => ChunkByFixedSize(document, config)
        };

        _logger.LogInformation("Created {ChunkCount} chunks from document", chunks.Count);
        return Task.FromResult(chunks);
    }

    private List<DocumentChunk> ChunkByFixedSize(DocumentImportResult document, ChunkingConfig config)
    {
        var chunks = new List<DocumentChunk>();
        var content = document.RawContent;
        var documentId = GenerateDocumentId(document);

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Document has no content to chunk");
            return chunks;
        }

        var words = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var chunkIndex = 0;
        var position = 0;

        for (int i = 0; i < words.Length; i += config.MaxChunkSize - config.OverlapSize)
        {
            var chunkWords = words.Skip(i).Take(config.MaxChunkSize).ToList();
            if (chunkWords.Count == 0) break;

            var chunkContent = string.Join(" ", chunkWords);
            var startPos = position;
            var endPos = startPos + chunkContent.Length;

            chunks.Add(new DocumentChunk
            {
                DocumentId = documentId,
                Content = chunkContent,
                ChunkIndex = chunkIndex++,
                StartPosition = startPos,
                EndPosition = endPos,
                Metadata = CreateChunkMetadata(document, null, null)
            });

            position = endPos;
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkBySemantic(DocumentImportResult document, ChunkingConfig config)
    {
        var chunks = new List<DocumentChunk>();
        var documentId = GenerateDocumentId(document);

        if (document.Structure.Sections.Count == 0)
        {
            _logger.LogDebug("No sections found, falling back to fixed-size chunking");
            return ChunkByFixedSize(document, config);
        }

        var chunkIndex = 0;
        var position = 0;

        foreach (var section in document.Structure.Sections)
        {
            var sectionChunks = ChunkSection(
                section,
                documentId,
                config,
                ref chunkIndex,
                ref position);

            chunks.AddRange(sectionChunks);
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkSection(
        DocumentSection section,
        string documentId,
        ChunkingConfig config,
        ref int chunkIndex,
        ref int position)
    {
        var chunks = new List<DocumentChunk>();
        var content = section.Content;

        if (string.IsNullOrWhiteSpace(content))
            return chunks;

        var sentences = SentenceRegex.Split(content);
        var currentChunk = new StringBuilder();
        var chunkWordCount = 0;
        var chunkStartPos = position;

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrEmpty(trimmedSentence))
                continue;

            var sentenceWords = trimmedSentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = sentenceWords.Length;

            if (chunkWordCount + wordCount > config.MaxChunkSize && currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex++,
                    StartPosition = chunkStartPos,
                    EndPosition = position,
                    Metadata = CreateChunkMetadata(null, section.Heading, null)
                });

                currentChunk.Clear();
                chunkWordCount = 0;
                chunkStartPos = position;
            }

            if (currentChunk.Length > 0)
                currentChunk.Append(' ');

            currentChunk.Append(trimmedSentence);
            chunkWordCount += wordCount;
            position += trimmedSentence.Length + 1;
        }

        if (currentChunk.Length > 0)
        {
            var chunkContent = currentChunk.ToString().Trim();
            chunks.Add(new DocumentChunk
            {
                DocumentId = documentId,
                Content = chunkContent,
                ChunkIndex = chunkIndex++,
                StartPosition = chunkStartPos,
                EndPosition = position,
                Metadata = CreateChunkMetadata(null, section.Heading, null)
            });
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkBySentence(DocumentImportResult document, ChunkingConfig config)
    {
        var chunks = new List<DocumentChunk>();
        var documentId = GenerateDocumentId(document);
        var content = document.RawContent;

        if (string.IsNullOrWhiteSpace(content))
            return chunks;

        var sentences = SentenceRegex.Split(content);
        var chunkIndex = 0;
        var position = 0;
        var currentChunk = new StringBuilder();
        var chunkWordCount = 0;
        var chunkStartPos = 0;

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var words = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = words.Length;

            if (chunkWordCount + wordCount > config.MaxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = currentChunk.ToString().Trim(),
                    ChunkIndex = chunkIndex++,
                    StartPosition = chunkStartPos,
                    EndPosition = position,
                    Metadata = CreateChunkMetadata(document, null, null)
                });

                currentChunk.Clear();
                chunkWordCount = 0;
                chunkStartPos = position;
            }

            if (currentChunk.Length > 0)
                currentChunk.Append(' ');

            currentChunk.Append(trimmed);
            chunkWordCount += wordCount;
            position += trimmed.Length + 1;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                DocumentId = documentId,
                Content = currentChunk.ToString().Trim(),
                ChunkIndex = chunkIndex++,
                StartPosition = chunkStartPos,
                EndPosition = position,
                Metadata = CreateChunkMetadata(document, null, null)
            });
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkByParagraph(DocumentImportResult document, ChunkingConfig config)
    {
        var chunks = new List<DocumentChunk>();
        var documentId = GenerateDocumentId(document);
        var content = document.RawContent;

        if (string.IsNullOrWhiteSpace(content))
            return chunks;

        var paragraphs = ParagraphRegex.Split(content);
        var chunkIndex = 0;
        var position = 0;

        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            var words = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = words.Length;

            if (wordCount > config.MaxChunkSize)
            {
                var subChunks = ChunkLargeParagraph(
                    trimmed,
                    documentId,
                    config,
                    ref chunkIndex,
                    ref position,
                    document);
                chunks.AddRange(subChunks);
            }
            else
            {
                var startPos = position;
                var endPos = startPos + trimmed.Length;

                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = trimmed,
                    ChunkIndex = chunkIndex++,
                    StartPosition = startPos,
                    EndPosition = endPos,
                    Metadata = CreateChunkMetadata(document, null, null)
                });

                position = endPos;
            }
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkLargeParagraph(
        string paragraph,
        string documentId,
        ChunkingConfig config,
        ref int chunkIndex,
        ref int position,
        DocumentImportResult document)
    {
        var chunks = new List<DocumentChunk>();
        var sentences = SentenceRegex.Split(paragraph);
        var currentChunk = new StringBuilder();
        var chunkWordCount = 0;
        var chunkStartPos = position;

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var words = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = words.Length;

            if (chunkWordCount + wordCount > config.MaxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = currentChunk.ToString().Trim(),
                    ChunkIndex = chunkIndex++,
                    StartPosition = chunkStartPos,
                    EndPosition = position,
                    Metadata = CreateChunkMetadata(document, null, null)
                });

                currentChunk.Clear();
                chunkWordCount = 0;
                chunkStartPos = position;
            }

            if (currentChunk.Length > 0)
                currentChunk.Append(' ');

            currentChunk.Append(trimmed);
            chunkWordCount += wordCount;
            position += trimmed.Length + 1;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                DocumentId = documentId,
                Content = currentChunk.ToString().Trim(),
                ChunkIndex = chunkIndex++,
                StartPosition = chunkStartPos,
                EndPosition = position,
                Metadata = CreateChunkMetadata(document, null, null)
            });
        }

        return chunks;
    }

    private ChunkMetadata CreateChunkMetadata(
        DocumentImportResult? document,
        string? section,
        int? pageNumber)
    {
        var metadata = new ChunkMetadata
        {
            Section = section,
            PageNumber = pageNumber
        };

        if (document != null)
        {
            metadata = metadata with
            {
                Source = document.Metadata.OriginalFileName,
                Title = document.Metadata.Title,
                WordCount = CountWords(document.RawContent)
            };
        }

        return metadata;
    }

    private string GenerateDocumentId(DocumentImportResult document)
    {
        return $"{document.Metadata.Format}_{document.Metadata.OriginalFileName}_{document.Metadata.ImportedAt:yyyyMMddHHmmss}";
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
