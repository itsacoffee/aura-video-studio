using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using Aura.Core.Models.RAG;
using Aura.Core.Services.Content;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Service for ingesting documents into the RAG system
/// </summary>
public class DocumentIngestService
{
    private readonly ILogger<DocumentIngestService> _logger;
    private readonly DocumentImportService _importService;
    private readonly DocumentChunkingService _chunkingService;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorIndex _vectorIndex;

    public DocumentIngestService(
        ILogger<DocumentIngestService> logger,
        DocumentImportService importService,
        DocumentChunkingService chunkingService,
        EmbeddingService embeddingService,
        VectorIndex vectorIndex)
    {
        _logger = logger;
        _importService = importService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorIndex = vectorIndex;
    }

    /// <summary>
    /// Ingest a document file into the RAG system
    /// </summary>
    public async Task<IndexingResult> IngestDocumentAsync(
        Stream fileStream,
        string fileName,
        ChunkingConfig? chunkingConfig = null,
        IProgress<IngestProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var documentId = string.Empty;

        try
        {
            _logger.LogInformation("Starting document ingestion for: {FileName}", fileName);

            progress?.Report(new IngestProgress
            {
                Stage = "Parsing",
                PercentComplete = 0,
                Message = "Parsing document..."
            });

            var importResult = await _importService.ImportDocumentAsync(fileStream, fileName, ct);

            if (!importResult.Success)
            {
                return new IndexingResult
                {
                    Success = false,
                    ErrorMessage = importResult.ErrorMessage ?? "Document import failed"
                };
            }

            documentId = GenerateDocumentId(importResult);

            progress?.Report(new IngestProgress
            {
                Stage = "Chunking",
                PercentComplete = 25,
                Message = "Creating document chunks..."
            });

            var config = chunkingConfig ?? new ChunkingConfig();
            var chunks = await _chunkingService.ChunkDocumentAsync(importResult, config, ct);

            if (chunks.Count == 0)
            {
                return new IndexingResult
                {
                    Success = false,
                    DocumentId = documentId,
                    ErrorMessage = "No chunks created from document"
                };
            }

            progress?.Report(new IngestProgress
            {
                Stage = "Embedding",
                PercentComplete = 50,
                Message = $"Generating embeddings for {chunks.Count} chunks..."
            });

            var chunkTexts = chunks.ConvertAll(c => c.Content);
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, ct);

            if (embeddings.Count != chunks.Count)
            {
                _logger.LogWarning(
                    "Embedding count mismatch: {EmbeddingCount} vs {ChunkCount}",
                    embeddings.Count, chunks.Count);
            }

            for (int i = 0; i < Math.Min(chunks.Count, embeddings.Count); i++)
            {
                chunks[i] = chunks[i] with { Embedding = embeddings[i] };
            }

            progress?.Report(new IngestProgress
            {
                Stage = "Indexing",
                PercentComplete = 75,
                Message = "Adding chunks to index..."
            });

            await _vectorIndex.AddChunksAsync(chunks, ct);

            progress?.Report(new IngestProgress
            {
                Stage = "Complete",
                PercentComplete = 100,
                Message = "Document ingestion complete"
            });

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully ingested document {FileName}: {ChunkCount} chunks in {Duration}ms",
                fileName, chunks.Count, stopwatch.ElapsedMilliseconds);

            return new IndexingResult
            {
                Success = true,
                DocumentId = documentId,
                ChunksCreated = chunks.Count,
                ProcessingTime = stopwatch.Elapsed,
                Warnings = importResult.Warnings
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Document ingestion cancelled for: {FileName}", fileName);
            stopwatch.Stop();

            return new IndexingResult
            {
                Success = false,
                DocumentId = documentId,
                ErrorMessage = "Document ingestion was cancelled",
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting document: {FileName}", fileName);
            stopwatch.Stop();

            return new IndexingResult
            {
                Success = false,
                DocumentId = documentId,
                ErrorMessage = $"Failed to ingest document: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Remove a document from the RAG system
    /// </summary>
    public async Task<bool> RemoveDocumentAsync(string documentId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Removing document from RAG system: {DocumentId}", documentId);
            await _vectorIndex.RemoveDocumentAsync(documentId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document: {DocumentId}", documentId);
            return false;
        }
    }

    /// <summary>
    /// Get statistics about the indexed documents
    /// </summary>
    public Task<IndexStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        return _vectorIndex.GetStatisticsAsync(ct);
    }

    /// <summary>
    /// Clear all documents from the RAG system
    /// </summary>
    public async Task<bool> ClearAllAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Clearing all documents from RAG system");
            await _vectorIndex.ClearAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing RAG system");
            return false;
        }
    }

    private string GenerateDocumentId(DocumentImportResult document)
    {
        return $"{document.Metadata.Format}_{Path.GetFileNameWithoutExtension(document.Metadata.OriginalFileName)}_{document.Metadata.ImportedAt:yyyyMMddHHmmss}";
    }
}

/// <summary>
/// Progress information for document ingestion
/// </summary>
public record IngestProgress
{
    public string Stage { get; init; } = string.Empty;
    public int PercentComplete { get; init; }
    public string Message { get; init; } = string.Empty;
}
