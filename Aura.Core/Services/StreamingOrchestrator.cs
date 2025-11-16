using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Models.Ollama;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Orchestrates streaming script generation with real-time progress updates
/// </summary>
public class StreamingOrchestrator
{
    private readonly ILogger<StreamingOrchestrator> _logger;

    public StreamingOrchestrator(ILogger<StreamingOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Stream script generation from Ollama responses with SSE events
    /// </summary>
    public async IAsyncEnumerable<StreamingScriptEvent> StreamScriptGenerationAsync(
        IAsyncEnumerable<OllamaStreamResponse> streamSource,
        string topic,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Starting streaming script generation for topic: {Topic}", topic);

        var scriptBuilder = new StringBuilder();
        var startTime = DateTime.UtcNow;
        var tokenCount = 0;

        await foreach (var chunk in streamSource.WithCancellation(ct).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(chunk.Response))
            {
                scriptBuilder.Append(chunk.Response);
                tokenCount++;

                var progressPercentage = chunk.GetProgressPercentage();
                var tokensPerSecond = chunk.GetTokensPerSecond();

                yield return new StreamingScriptEvent
                {
                    EventType = "chunk",
                    Content = chunk.Response,
                    CumulativeContent = scriptBuilder.ToString(),
                    TokenCount = tokenCount,
                    ProgressPercentage = progressPercentage,
                    TokensPerSecond = tokensPerSecond,
                    Model = chunk.Model,
                    IsComplete = false
                };
            }

            if (chunk.Done)
            {
                var duration = DateTime.UtcNow - startTime;
                var tokensPerSecond = chunk.GetTokensPerSecond();

                _logger.LogInformation(
                    "Streaming generation complete. Tokens: {Tokens}, Duration: {Duration}s, Tokens/sec: {TokensPerSec:F2}",
                    chunk.EvalCount ?? tokenCount,
                    duration.TotalSeconds,
                    tokensPerSecond ?? 0.0);

                yield return new StreamingScriptEvent
                {
                    EventType = "complete",
                    Content = scriptBuilder.ToString(),
                    CumulativeContent = scriptBuilder.ToString(),
                    TokenCount = chunk.EvalCount ?? tokenCount,
                    ProgressPercentage = 100.0,
                    TokensPerSecond = tokensPerSecond,
                    Model = chunk.Model,
                    IsComplete = true,
                    Metrics = new GenerationMetrics
                    {
                        TotalDurationMs = chunk.TotalDuration.HasValue 
                            ? chunk.TotalDuration.Value / 1_000_000.0 
                            : duration.TotalMilliseconds,
                        LoadDurationMs = chunk.LoadDuration.HasValue 
                            ? chunk.LoadDuration.Value / 1_000_000.0 
                            : 0,
                        PromptEvalCount = chunk.PromptEvalCount ?? 0,
                        PromptEvalDurationMs = chunk.PromptEvalDuration.HasValue 
                            ? chunk.PromptEvalDuration.Value / 1_000_000.0 
                            : 0,
                        EvalCount = chunk.EvalCount ?? tokenCount,
                        EvalDurationMs = chunk.EvalDuration.HasValue 
                            ? chunk.EvalDuration.Value / 1_000_000.0 
                            : 0,
                        TokensPerSecond = tokensPerSecond ?? 0.0
                    }
                };
            }
        }
    }

    /// <summary>
    /// Convert streaming events to Server-Sent Events format
    /// </summary>
    public string FormatAsServerSentEvent(StreamingScriptEvent eventData)
    {
        var eventType = eventData.IsComplete ? "complete" : "progress";
        var data = System.Text.Json.JsonSerializer.Serialize(eventData);
        
        return $"event: {eventType}\ndata: {data}\n\n";
    }
}

/// <summary>
/// Represents a streaming script generation event
/// </summary>
public class StreamingScriptEvent
{
    /// <summary>
    /// Event type: "chunk", "complete", "error"
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// New content in this chunk
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Full accumulated content so far
    /// </summary>
    public string CumulativeContent { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens generated
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Tokens generated per second
    /// </summary>
    public double? TokensPerSecond { get; set; }

    /// <summary>
    /// Model name
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Whether generation is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Final generation metrics (only in complete event)
    /// </summary>
    public GenerationMetrics? Metrics { get; set; }

    /// <summary>
    /// Error message if any
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Final generation metrics from Ollama
/// </summary>
public class GenerationMetrics
{
    /// <summary>
    /// Total generation duration in milliseconds
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// Model load duration in milliseconds
    /// </summary>
    public double LoadDurationMs { get; set; }

    /// <summary>
    /// Number of prompt tokens evaluated
    /// </summary>
    public int PromptEvalCount { get; set; }

    /// <summary>
    /// Prompt evaluation duration in milliseconds
    /// </summary>
    public double PromptEvalDurationMs { get; set; }

    /// <summary>
    /// Number of tokens generated
    /// </summary>
    public int EvalCount { get; set; }

    /// <summary>
    /// Token generation duration in milliseconds
    /// </summary>
    public double EvalDurationMs { get; set; }

    /// <summary>
    /// Tokens generated per second
    /// </summary>
    public double TokensPerSecond { get; set; }
}
