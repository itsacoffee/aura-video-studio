using System;
using System.Collections.Generic;

namespace Aura.Core.Models.RAG;

/// <summary>
/// Represents a chunk of text from a document with metadata for retrieval
/// </summary>
public record DocumentChunk
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string DocumentId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public ChunkMetadata Metadata { get; init; } = new();
    public float[] Embedding { get; init; } = Array.Empty<float>();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Metadata about a document chunk
/// </summary>
public record ChunkMetadata
{
    public string Source { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Section { get; init; }
    public int? PageNumber { get; init; }
    public int WordCount { get; init; }
    public Dictionary<string, string> CustomProperties { get; init; } = new();
}

/// <summary>
/// Configuration for chunking strategy
/// </summary>
public record ChunkingConfig
{
    public ChunkingStrategy Strategy { get; init; } = ChunkingStrategy.Semantic;
    public int MaxChunkSize { get; init; } = 512;
    public int OverlapSize { get; init; } = 50;
    public bool PreserveSentences { get; init; } = true;
}

/// <summary>
/// Chunking strategy type
/// </summary>
public enum ChunkingStrategy
{
    Fixed,
    Semantic,
    Sentence,
    Paragraph
}

/// <summary>
/// Result of a retrieval query
/// </summary>
public record RetrievalResult
{
    public List<ScoredChunk> Chunks { get; init; } = new();
    public int TotalDocuments { get; init; }
    public TimeSpan RetrievalTime { get; init; }
    public string Query { get; init; } = string.Empty;
}

/// <summary>
/// A document chunk with relevance score
/// </summary>
public record ScoredChunk
{
    public DocumentChunk Chunk { get; init; } = new();
    public float Score { get; init; }
    public string? Explanation { get; init; }
}

/// <summary>
/// Context built from retrieved documents
/// </summary>
public record RagContext
{
    public string Query { get; init; } = string.Empty;
    public List<ContextChunk> Chunks { get; init; } = new();
    public string FormattedContext { get; init; } = string.Empty;
    public List<Citation> Citations { get; init; } = new();
    public int TotalTokens { get; init; }
}

/// <summary>
/// A chunk with citation information
/// </summary>
public record ContextChunk
{
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Section { get; init; }
    public int? PageNumber { get; init; }
    public float RelevanceScore { get; init; }
    public int CitationNumber { get; init; }
}

/// <summary>
/// Citation for a source
/// </summary>
public record Citation
{
    public int Number { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Section { get; init; }
    public int? PageNumber { get; init; }
    public string? Url { get; init; }
}

/// <summary>
/// Configuration for RAG retrieval
/// </summary>
public record RagConfig
{
    public bool Enabled { get; init; } = true;
    public int TopK { get; init; } = 5;
    public float MinimumScore { get; init; } = 0.5f;
    public int MaxContextTokens { get; init; } = 2000;
    public bool IncludeCitations { get; init; } = true;
    public bool RerankerEnabled { get; init; } = false;
}

/// <summary>
/// Statistics about the vector index
/// </summary>
public record IndexStatistics
{
    public int TotalDocuments { get; init; }
    public int TotalChunks { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTime LastUpdated { get; init; }
    public Dictionary<string, int> DocumentsByFormat { get; init; } = new();
}

/// <summary>
/// Result of document indexing operation
/// </summary>
public record IndexingResult
{
    public bool Success { get; init; }
    public string DocumentId { get; init; } = string.Empty;
    public int ChunksCreated { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Telemetry for RAG operations
/// </summary>
public record RagTelemetry
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Operation { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public int ChunksRetrieved { get; init; }
    public float AverageScore { get; init; }
    public int TokensUsed { get; init; }
    public bool HitCache { get; init; }
}
