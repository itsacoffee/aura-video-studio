using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.RAG;
using Aura.Core.Services.RAG;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for Retrieval-Augmented Generation (RAG) operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RagController : ControllerBase
{
    private readonly DocumentIngestService _ingestService;
    private readonly RagContextBuilder _contextBuilder;
    private readonly VectorIndex _vectorIndex;

    public RagController(
        DocumentIngestService ingestService,
        RagContextBuilder contextBuilder,
        VectorIndex vectorIndex)
    {
        _ingestService = ingestService;
        _contextBuilder = contextBuilder;
        _vectorIndex = vectorIndex;
    }

    /// <summary>
    /// Upload and index a document for RAG
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IndexingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestDocument(
        IFormFile file,
        [FromQuery] ChunkingStrategy strategy = ChunkingStrategy.Semantic,
        [FromQuery] int maxChunkSize = 512,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Ingesting document: {FileName}", correlationId, file.FileName);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid File",
                    Status = 400,
                    Detail = "No file was uploaded or file is empty",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            var config = new ChunkingConfig
            {
                Strategy = strategy,
                MaxChunkSize = maxChunkSize
            };

            using var stream = file.OpenReadStream();
            var result = await _ingestService.IngestDocumentAsync(stream, file.FileName, config, null, ct);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Document Ingestion Failed",
                    Status = 400,
                    Detail = result.ErrorMessage ?? "Failed to ingest document",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            Log.Information(
                "[{CorrelationId}] Successfully ingested document: {DocumentId}, Chunks: {ChunkCount}",
                correlationId, result.DocumentId, result.ChunksCreated);

            return Ok(new IndexingResultDto
            {
                Success = result.Success,
                DocumentId = result.DocumentId,
                ChunksCreated = result.ChunksCreated,
                ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds,
                Warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error ingesting document", correlationId);

            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = $"An error occurred while ingesting the document: {ex.Message}",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Search for relevant document chunks
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(RagContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromBody] SearchRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] RAG search: {Query}", correlationId, request.Query);

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Query",
                    Status = 400,
                    Detail = "Query cannot be empty",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            var config = new RagConfig
            {
                Enabled = true,
                TopK = request.TopK ?? 5,
                MinimumScore = request.MinimumScore ?? 0.5f,
                MaxContextTokens = request.MaxContextTokens ?? 2000,
                IncludeCitations = request.IncludeCitations ?? true
            };

            var context = await _contextBuilder.BuildContextAsync(request.Query, config, ct);

            return Ok(new RagContextDto
            {
                Query = context.Query,
                FormattedContext = context.FormattedContext,
                Chunks = context.Chunks.ConvertAll(c => new ContextChunkDto
                {
                    Content = c.Content,
                    Source = c.Source,
                    Section = c.Section,
                    PageNumber = c.PageNumber,
                    RelevanceScore = c.RelevanceScore,
                    CitationNumber = c.CitationNumber
                }),
                Citations = context.Citations.ConvertAll(c => new CitationDto
                {
                    Number = c.Number,
                    Source = c.Source,
                    Title = c.Title,
                    Section = c.Section,
                    PageNumber = c.PageNumber
                }),
                TotalTokens = context.TotalTokens
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error performing RAG search", correlationId);

            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = $"An error occurred while searching: {ex.Message}",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Get statistics about indexed documents
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(IndexStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken ct = default)
    {
        try
        {
            var stats = await _ingestService.GetStatisticsAsync(ct);

            return Ok(new IndexStatisticsDto
            {
                TotalDocuments = stats.TotalDocuments,
                TotalChunks = stats.TotalChunks,
                TotalSizeBytes = stats.TotalSizeBytes,
                LastUpdated = stats.LastUpdated,
                DocumentsByFormat = stats.DocumentsByFormat
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting RAG statistics", correlationId);

            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = $"An error occurred while getting statistics: {ex.Message}",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Remove a document from the index
    /// </summary>
    [HttpDelete("documents/{documentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveDocument(string documentId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Removing document: {DocumentId}", correlationId, documentId);

            var success = await _ingestService.RemoveDocumentAsync(documentId, ct);

            if (!success)
            {
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Removal Failed",
                    Status = 500,
                    Detail = "Failed to remove document",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error removing document", correlationId);

            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = $"An error occurred while removing document: {ex.Message}",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Clear all documents from the index
    /// </summary>
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAll(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Clearing all RAG documents", correlationId);

            await _ingestService.ClearAllAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error clearing RAG index", correlationId);

            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = $"An error occurred while clearing index: {ex.Message}",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }
}

public record SearchRequest
{
    public string Query { get; init; } = string.Empty;
    public int? TopK { get; init; }
    public float? MinimumScore { get; init; }
    public int? MaxContextTokens { get; init; }
    public bool? IncludeCitations { get; init; }
}

public record IndexingResultDto
{
    public bool Success { get; init; }
    public string DocumentId { get; init; } = string.Empty;
    public int ChunksCreated { get; init; }
    public double ProcessingTimeMs { get; init; }
    public System.Collections.Generic.List<string> Warnings { get; init; } = new();
}

public record RagContextDto
{
    public string Query { get; init; } = string.Empty;
    public string FormattedContext { get; init; } = string.Empty;
    public System.Collections.Generic.List<ContextChunkDto> Chunks { get; init; } = new();
    public System.Collections.Generic.List<CitationDto> Citations { get; init; } = new();
    public int TotalTokens { get; init; }
}

public record ContextChunkDto
{
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Section { get; init; }
    public int? PageNumber { get; init; }
    public float RelevanceScore { get; init; }
    public int CitationNumber { get; init; }
}

public record CitationDto
{
    public int Number { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Section { get; init; }
    public int? PageNumber { get; init; }
}

public record IndexStatisticsDto
{
    public int TotalDocuments { get; init; }
    public int TotalChunks { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTime LastUpdated { get; init; }
    public System.Collections.Generic.Dictionary<string, int> DocumentsByFormat { get; init; } = new();
}
