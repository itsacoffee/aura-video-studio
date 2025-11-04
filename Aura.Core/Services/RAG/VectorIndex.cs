using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.RAG;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// File-based vector index for storing and retrieving document chunks
/// </summary>
public class VectorIndex
{
    private readonly ILogger<VectorIndex> _logger;
    private readonly string _indexPath;
    private readonly Dictionary<string, List<DocumentChunk>> _index;
    private readonly object _lock = new();

    public VectorIndex(ILogger<VectorIndex> logger, string indexPath)
    {
        _logger = logger;
        _indexPath = indexPath;
        _index = new Dictionary<string, List<DocumentChunk>>();

        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath) ?? ".");
        LoadIndex();
    }

    /// <summary>
    /// Add chunks to the index
    /// </summary>
    public Task AddChunksAsync(List<DocumentChunk> chunks, CancellationToken ct = default)
    {
        if (chunks.Count == 0)
            return Task.CompletedTask;

        lock (_lock)
        {
            var documentId = chunks.First().DocumentId;

            if (!_index.ContainsKey(documentId))
            {
                _index[documentId] = new List<DocumentChunk>();
            }

            _index[documentId].AddRange(chunks);

            _logger.LogInformation("Added {Count} chunks for document {DocumentId}",
                chunks.Count, documentId);
        }

        return SaveIndexAsync(ct);
    }

    /// <summary>
    /// Remove a document from the index
    /// </summary>
    public Task RemoveDocumentAsync(string documentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_index.Remove(documentId))
            {
                _logger.LogInformation("Removed document {DocumentId} from index", documentId);
            }
        }

        return SaveIndexAsync(ct);
    }

    /// <summary>
    /// Search for similar chunks using cosine similarity
    /// </summary>
    public Task<RetrievalResult> SearchAsync(
        float[] queryEmbedding,
        string query,
        int topK = 5,
        float minimumScore = 0.5f,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var scoredChunks = new List<ScoredChunk>();

        lock (_lock)
        {
            foreach (var documentChunks in _index.Values)
            {
                foreach (var chunk in documentChunks)
                {
                    if (chunk.Embedding.Length == 0)
                    {
                        _logger.LogWarning("Chunk {ChunkId} has no embedding", chunk.Id);
                        continue;
                    }

                    var score = EmbeddingService.CosineSimilarity(queryEmbedding, chunk.Embedding);

                    if (score >= minimumScore)
                    {
                        scoredChunks.Add(new ScoredChunk
                        {
                            Chunk = chunk,
                            Score = score
                        });
                    }
                }
            }
        }

        var topChunks = scoredChunks
            .OrderByDescending(sc => sc.Score)
            .Take(topK)
            .ToList();

        var retrievalTime = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Retrieved {Count} chunks for query in {Duration}ms (total scored: {TotalScored})",
            topChunks.Count, retrievalTime.TotalMilliseconds, scoredChunks.Count);

        return Task.FromResult(new RetrievalResult
        {
            Chunks = topChunks,
            TotalDocuments = _index.Count,
            RetrievalTime = retrievalTime,
            Query = query
        });
    }

    /// <summary>
    /// Get all chunks for a specific document
    /// </summary>
    public Task<List<DocumentChunk>> GetDocumentChunksAsync(
        string documentId,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(
                _index.TryGetValue(documentId, out var chunks)
                    ? new List<DocumentChunk>(chunks)
                    : new List<DocumentChunk>());
        }
    }

    /// <summary>
    /// Get statistics about the index
    /// </summary>
    public Task<IndexStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var totalChunks = _index.Values.Sum(chunks => chunks.Count);
            var documentsByFormat = new Dictionary<string, int>();

            foreach (var chunks in _index.Values)
            {
                if (chunks.Count > 0)
                {
                    var source = chunks.First().Metadata.Source;
                    var extension = Path.GetExtension(source).ToLowerInvariant();

                    if (!documentsByFormat.ContainsKey(extension))
                    {
                        documentsByFormat[extension] = 0;
                    }

                    documentsByFormat[extension]++;
                }
            }

            var stats = new IndexStatistics
            {
                TotalDocuments = _index.Count,
                TotalChunks = totalChunks,
                TotalSizeBytes = CalculateIndexSize(),
                LastUpdated = DateTime.UtcNow,
                DocumentsByFormat = documentsByFormat
            };

            return Task.FromResult(stats);
        }
    }

    /// <summary>
    /// Clear all documents from the index
    /// </summary>
    public Task ClearAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            _index.Clear();
            _logger.LogInformation("Cleared all documents from index");
        }

        return SaveIndexAsync(ct);
    }

    private void LoadIndex()
    {
        try
        {
            if (!File.Exists(_indexPath))
            {
                _logger.LogInformation("No existing index found at {Path}, starting with empty index", _indexPath);
                return;
            }

            var json = File.ReadAllText(_indexPath);
            var loadedIndex = JsonSerializer.Deserialize<Dictionary<string, List<DocumentChunk>>>(json);

            if (loadedIndex != null)
            {
                lock (_lock)
                {
                    foreach (var kvp in loadedIndex)
                    {
                        _index[kvp.Key] = kvp.Value;
                    }
                }

                _logger.LogInformation("Loaded index from {Path} with {DocumentCount} documents",
                    _indexPath, _index.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading index from {Path}, starting with empty index", _indexPath);
        }
    }

    private async Task SaveIndexAsync(CancellationToken ct)
    {
        try
        {
            Dictionary<string, List<DocumentChunk>> indexCopy;

            lock (_lock)
            {
                indexCopy = new Dictionary<string, List<DocumentChunk>>(_index);
            }

            var json = JsonSerializer.Serialize(indexCopy, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_indexPath, json, ct);

            _logger.LogDebug("Saved index to {Path}", _indexPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving index to {Path}", _indexPath);
        }
    }

    private long CalculateIndexSize()
    {
        try
        {
            if (File.Exists(_indexPath))
            {
                return new FileInfo(_indexPath).Length;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating index size");
        }

        return 0;
    }
}
