using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Ollama;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Services;

public class StreamingOrchestratorTests
{
    private readonly StreamingOrchestrator _orchestrator;
    private readonly ILogger<StreamingOrchestrator> _logger;

    public StreamingOrchestratorTests()
    {
        _logger = new LoggerFactory().CreateLogger<StreamingOrchestrator>();
        _orchestrator = new StreamingOrchestrator(_logger);
    }

    [Fact]
    public async Task StreamScriptGenerationAsync_ProcessesChunks_ReturnsCompleteScript()
    {
        var chunks = new List<OllamaStreamResponse>
        {
            new() { Model = "llama3.1", Response = "Hello", Done = false },
            new() { Model = "llama3.1", Response = " world", Done = false },
            new() { Model = "llama3.1", Response = "!", Done = false },
            new()
            {
                Model = "llama3.1",
                Response = "",
                Done = true,
                TotalDuration = 1_000_000_000,
                EvalCount = 3,
                EvalDuration = 500_000_000
            }
        };

        var streamSource = CreateAsyncEnumerable(chunks);
        var events = new List<StreamingScriptEvent>();

        await foreach (var evt in _orchestrator.StreamScriptGenerationAsync(streamSource, "test topic", CancellationToken.None))
        {
            events.Add(evt);
        }

        Assert.Equal(4, events.Count);
        Assert.Equal("Hello", events[0].Content);
        Assert.Equal("Hello world!", events[2].CumulativeContent);
        Assert.True(events[3].IsComplete);
        Assert.NotNull(events[3].Metrics);
        Assert.Equal(3, events[3].Metrics!.EvalCount);
    }

    [Fact]
    public async Task StreamScriptGenerationAsync_CalculatesTokensPerSecond_Correctly()
    {
        var chunks = new List<OllamaStreamResponse>
        {
            new() { Model = "test", Response = "token", Done = false },
            new()
            {
                Model = "test",
                Response = "",
                Done = true,
                EvalCount = 100,
                EvalDuration = 1_000_000_000
            }
        };

        var streamSource = CreateAsyncEnumerable(chunks);
        var events = new List<StreamingScriptEvent>();

        await foreach (var evt in _orchestrator.StreamScriptGenerationAsync(streamSource, "test", CancellationToken.None))
        {
            events.Add(evt);
        }

        var completeEvent = events.Last();
        Assert.True(completeEvent.IsComplete);
        Assert.NotNull(completeEvent.Metrics);
        Assert.Equal(100.0, completeEvent.Metrics!.TokensPerSecond);
    }

    [Fact]
    public void FormatAsServerSentEvent_FormatsCorrectly()
    {
        var evt = new StreamingScriptEvent
        {
            EventType = "chunk",
            Content = "test",
            CumulativeContent = "test",
            TokenCount = 1,
            ProgressPercentage = 50.0,
            Model = "llama3.1",
            IsComplete = false
        };

        var formatted = _orchestrator.FormatAsServerSentEvent(evt);

        Assert.Contains("event: progress", formatted);
        Assert.Contains("data: {", formatted);
        Assert.Contains("\"EventType\":\"chunk\"", formatted);
        Assert.EndsWith("\n\n", formatted);
    }

    [Fact]
    public void FormatAsServerSentEvent_FormatsCompleteEvent()
    {
        var evt = new StreamingScriptEvent
        {
            EventType = "complete",
            Content = "final",
            CumulativeContent = "final",
            TokenCount = 10,
            ProgressPercentage = 100.0,
            Model = "test",
            IsComplete = true,
            Metrics = new GenerationMetrics
            {
                TotalDurationMs = 1000,
                EvalCount = 10,
                TokensPerSecond = 10.0
            }
        };

        var formatted = _orchestrator.FormatAsServerSentEvent(evt);

        Assert.Contains("event: complete", formatted);
        Assert.Contains("\"IsComplete\":true", formatted);
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
