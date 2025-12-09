namespace Aura.Core.Models.RAG;

/// <summary>
/// Status indicating why RAG context may be empty or limited
/// </summary>
public enum RagContextStatus
{
    /// <summary>Successfully retrieved context</summary>
    Success = 0,

    /// <summary>RAG is disabled in configuration</summary>
    Disabled = 1,

    /// <summary>Vector index is not available</summary>
    IndexUnavailable = 2,

    /// <summary>No documents have been indexed</summary>
    NoDocuments = 3,

    /// <summary>Embedding generation failed</summary>
    EmbeddingFailed = 4,

    /// <summary>No relevant chunks found above minimum score</summary>
    NoRelevantChunks = 5
}
