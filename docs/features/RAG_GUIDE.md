# Retrieval-Augmented Generation (RAG) Guide

## Overview

The RAG system in Aura Video Studio allows you to ground LLM outputs in your own documents and references, reducing hallucinations and increasing relevance. Upload documents (PDFs, Word files, etc.), and the system will automatically chunk, embed, and index them for intelligent retrieval during content generation.

## Features

- **Document Ingestion**: Upload and index documents from various formats (PDF, DOCX, TXT, Markdown, HTML, JSON)
- **Intelligent Chunking**: Multiple strategies (Semantic, Fixed, Sentence, Paragraph) to preserve context
- **Vector Search**: Cosine similarity-based retrieval of relevant content
- **Citation Support**: Automatic source attribution in generated content
- **Multiple Embedding Providers**: Local (default), OpenAI, or Ollama
- **Persistent Index**: File-based vector storage that persists across sessions

## Quick Start

### 1. Upload a Document

```bash
curl -X POST http://localhost:5005/api/rag/ingest \
  -F "file=@your-document.pdf" \
  -F "strategy=Semantic" \
  -F "maxChunkSize=512"
```

**Response:**
```json
{
  "success": true,
  "documentId": "Pdf_your-document_20240101120000",
  "chunksCreated": 45,
  "processingTimeMs": 2341.5,
  "warnings": []
}
```

### 2. Search for Relevant Content

```bash
curl -X POST http://localhost:5005/api/rag/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What are the key features of machine learning?",
    "topK": 5,
    "minimumScore": 0.5,
    "includeCitations": true
  }'
```

**Response:**
```json
{
  "query": "What are the key features of machine learning?",
  "formattedContext": "# Reference Material\n\n## Reference 1\nSource: ml-guide.pdf - Section: Introduction - Page: 3 [Citation 1]\n\nMachine learning is a subset of artificial intelligence...",
  "chunks": [
    {
      "content": "Machine learning is a subset of artificial intelligence...",
      "source": "ml-guide.pdf",
      "section": "Introduction",
      "pageNumber": 3,
      "relevanceScore": 0.92,
      "citationNumber": 1
    }
  ],
  "citations": [
    {
      "number": 1,
      "source": "ml-guide.pdf",
      "title": "Machine Learning Guide",
      "section": "Introduction",
      "pageNumber": 3
    }
  ],
  "totalTokens": 543
}
```

### 3. Get Index Statistics

```bash
curl http://localhost:5005/api/rag/statistics
```

**Response:**
```json
{
  "totalDocuments": 5,
  "totalChunks": 234,
  "totalSizeBytes": 1048576,
  "lastUpdated": "2024-01-01T12:00:00Z",
  "documentsByFormat": {
    ".pdf": 3,
    ".docx": 2
  }
}
```

## Chunking Strategies

### Semantic Chunking (Recommended)

Preserves document structure by respecting section boundaries and keeping related sentences together.

**Best for**: Structured documents with headings and sections (PDFs, Word docs, Markdown)

**Configuration:**
```json
{
  "strategy": "Semantic",
  "maxChunkSize": 512,
  "overlapSize": 50,
  "preserveSentences": true
}
```

### Fixed Chunking

Splits text into fixed-size chunks with optional overlap.

**Best for**: Plain text, unstructured content, or when consistent chunk sizes are required

**Configuration:**
```json
{
  "strategy": "Fixed",
  "maxChunkSize": 512,
  "overlapSize": 50
}
```

### Sentence Chunking

Keeps complete sentences together, never breaking mid-sentence.

**Best for**: Content where sentence boundaries are important

**Configuration:**
```json
{
  "strategy": "Sentence",
  "maxChunkSize": 512,
  "preserveSentences": true
}
```

### Paragraph Chunking

Preserves paragraph boundaries.

**Best for**: Articles, blog posts, or content where paragraph structure matters

**Configuration:**
```json
{
  "strategy": "Paragraph",
  "maxChunkSize": 512
}
```

## Embedding Providers

### Local Embedding (Default)

Simple hash-based embeddings that work offline.

**Pros**: No API key required, works offline, fast
**Cons**: Lower accuracy than neural embeddings

**Configuration:**
```json
{
  "RAG": {
    "Embedding": {
      "Provider": "Local",
      "DimensionSize": 384
    }
  }
}
```

### OpenAI Embeddings

High-quality neural embeddings using OpenAI's models.

**Pros**: Excellent semantic understanding, best accuracy
**Cons**: Requires API key, costs money, needs internet

**Configuration:**
```json
{
  "RAG": {
    "Embedding": {
      "Provider": "OpenAI",
      "ApiKey": "sk-...",
      "ModelName": "text-embedding-ada-002",
      "DimensionSize": 1536
    }
  }
}
```

### Ollama Embeddings

Local neural embeddings using Ollama.

**Pros**: Good accuracy, no API costs, private
**Cons**: Requires Ollama installation and model download

**Configuration:**
```json
{
  "RAG": {
    "Embedding": {
      "Provider": "Ollama",
      "BaseUrl": "http://localhost:11434",
      "ModelName": "nomic-embed-text",
      "DimensionSize": 768
    }
  }
}
```

**Setup:**
```bash
# Install Ollama
# Download embedding model
ollama pull nomic-embed-text
```

## RAG-Enhanced Content Generation

### Enable RAG for Script Generation

When RAG is enabled, the system will automatically retrieve relevant context from your indexed documents and include it in the LLM prompt.

**Manual Context Building:**
```bash
# 1. Search for relevant context
CONTEXT=$(curl -X POST http://localhost:5005/api/rag/search \
  -H "Content-Type: application/json" \
  -d '{"query": "machine learning basics", "topK": 3}')

# 2. Use context in your script generation
curl -X POST http://localhost:5005/api/scripts/generate \
  -H "Content-Type: application/json" \
  -d '{
    "brief": "Create a video about machine learning",
    "context": "'$CONTEXT'"
  }'
```

## Best Practices

### Document Preparation

1. **Clean Format**: Remove headers, footers, and navigation elements
2. **Clear Structure**: Use headings and sections for better semantic chunking
3. **Reasonable Size**: Aim for 1,000-10,000 words per document
4. **Single Topic**: Each document should focus on one topic

### Chunking Configuration

- **General Documents**: Use Semantic chunking with 512 tokens
- **Technical Docs**: Use 768 tokens to preserve code examples
- **Short Articles**: Use 256-384 tokens for more granular retrieval
- **Overlap**: Use 10-20% overlap (50-100 tokens) for context continuity

### Search Configuration

- **Precision**: Use `topK=3-5` with `minimumScore=0.7` for highly relevant results
- **Recall**: Use `topK=10-15` with `minimumScore=0.5` for broader coverage
- **Balanced**: Use `topK=5` with `minimumScore=0.6` (default)

### Performance Tips

1. **Batch Uploads**: Upload multiple documents at once rather than one at a time
2. **Index Size**: Keep index under 1000 documents for best performance
3. **Chunk Size**: Larger chunks (768-1024) reduce index size but may miss details
4. **Provider**: Use OpenAI or Ollama embeddings for production use cases

## API Reference

### POST /api/rag/ingest

Upload and index a document.

**Request:**
- `file`: Document file (multipart/form-data)
- `strategy`: Chunking strategy (query param, optional, default: Semantic)
- `maxChunkSize`: Maximum chunk size in tokens (query param, optional, default: 512)

**Response:** `IndexingResultDto`

### POST /api/rag/search

Search for relevant document chunks.

**Request:** `SearchRequest`
```json
{
  "query": "string",
  "topK": 5,
  "minimumScore": 0.5,
  "maxContextTokens": 2000,
  "includeCitations": true
}
```

**Response:** `RagContextDto`

### GET /api/rag/statistics

Get statistics about the indexed documents.

**Response:** `IndexStatisticsDto`

### DELETE /api/rag/documents/{documentId}

Remove a specific document from the index.

**Response:** 204 No Content

### DELETE /api/rag/clear

Clear all documents from the index.

**Response:** 204 No Content

## Troubleshooting

### No Results Returned

- **Check Index**: Verify documents are indexed with `/api/rag/statistics`
- **Lower Threshold**: Reduce `minimumScore` to 0.3-0.4
- **Rephrase Query**: Try different search terms
- **Check Embeddings**: Ensure embedding service is working

### Low Relevance Scores

- **Better Embeddings**: Switch from Local to OpenAI or Ollama
- **Better Chunks**: Use Semantic chunking for structured documents
- **Query Quality**: Make queries more specific and detailed

### Slow Search

- **Reduce topK**: Search fewer chunks (3-5 instead of 10+)
- **Smaller Index**: Remove unused documents
- **Better Hardware**: Consider more RAM for large indices

### Memory Issues

- **Smaller Chunks**: Reduce `maxChunkSize` to 256-384
- **Fewer Documents**: Split large projects into multiple indices
- **Clear Old Data**: Remove documents you no longer need

## Examples

### Example 1: Research Paper RAG

```bash
# Upload research paper
curl -X POST http://localhost:5005/api/rag/ingest \
  -F "file=@research-paper.pdf" \
  -F "strategy=Semantic" \
  -F "maxChunkSize=768"

# Search for methodology
curl -X POST http://localhost:5005/api/rag/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "research methodology and experimental setup",
    "topK": 5,
    "includeCitations": true
  }'
```

### Example 2: Product Documentation

```bash
# Upload multiple docs
for doc in docs/*.pdf; do
  curl -X POST http://localhost:5005/api/rag/ingest \
    -F "file=@$doc" \
    -F "strategy=Semantic"
done

# Search for feature info
curl -X POST http://localhost:5005/api/rag/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "authentication and security features",
    "topK": 10,
    "minimumScore": 0.6
  }'
```

### Example 3: Content Repurposing

```bash
# Upload existing blog posts
curl -X POST http://localhost:5005/api/rag/ingest \
  -F "file=@blog-archive.md" \
  -F "strategy=Paragraph"

# Find relevant content for new video
curl -X POST http://localhost:5005/api/rag/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "beginner-friendly explanation of concepts",
    "topK": 8,
    "maxContextTokens": 3000
  }'
```

## Integration with Video Generation

The RAG system is designed to work seamlessly with Aura's video generation pipeline:

1. **Document Upload**: Index your reference materials
2. **Content Planning**: RAG retrieves relevant context during brief analysis
3. **Script Generation**: LLM uses retrieved context to write grounded scripts
4. **Citation Display**: Citations appear in the generated script
5. **Video Creation**: Proceed with TTS, visuals, and rendering as usual

## Further Reading

- **DOCUMENT_IMPORT_GUIDE.md**: Document import and conversion system
- **LLM_IMPLEMENTATION_GUIDE.md**: LLM integration and prompt engineering
- **CONTENT_ADAPTATION_GUIDE.md**: Content adaptation and audience targeting

## Support

For issues, feature requests, or questions:
- GitHub Issues: https://github.com/Saiyan9001/aura-video-studio/issues
- Documentation: Check other guides in the repository
