# PR-001: Unified Streaming Interface for Real-Time LLM Responses

**Priority**: P0 (Critical - Foundation for user experience)  
**Complexity**: High  
**Estimated Effort**: 5-7 days  
**Dependencies**: None  
**Can Run in Parallel With**: PR-002, PR-003, PR-004

---

## Objective

Implement comprehensive streaming support across all LLM providers (OpenAI, Anthropic, Gemini, Azure OpenAI) to enable real-time token-by-token generation with progress feedback.

**Current State**: Only `OllamaLlmProvider` has streaming via `GenerateStreamingAsync`. Other providers lack streaming despite API support, causing poor UX for long script generation (2-5 minutes).

**Target State**: All providers support streaming with unified interface, real-time UI updates, and ability to cancel mid-generation.

---

## Success Criteria

- [ ] All 5 LLM providers (OpenAI, Anthropic, Gemini, Azure, Ollama) implement streaming  
- [ ] Unified `ILlmProvider` interface with streaming methods  
- [ ] Frontend displays real-time token generation with cancel capability  
- [ ] SSE endpoints in API for browser streaming  
- [ ] Zero regressions in existing non-streaming flows  
- [ ] Performance: <100ms first token latency, >20 tokens/sec throughput  
- [ ] All tests pass with >85% coverage

---

## Core Architecture

### 1. Interface Changes

**File**: `Aura.Core/Providers/IProviders.cs`

```csharp
public interface ILlmProvider
{
    // Existing non-streaming methods remain unchanged
    Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct);
    
    // NEW: Streaming methods
    IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
        Brief brief, PlanSpec spec, CancellationToken ct);
    
    IAsyncEnumerable<LlmStreamChunk> CompleteStreamAsync(
        string prompt, CancellationToken ct);
    
    // NEW: Capability detection
    bool SupportsStreaming { get; }
}
```

### 2. Unified Streaming Models

**File**: `Aura.Core/Models/Streaming/LlmStreamingModels.cs`

```csharp
namespace Aura.Core.Models.Streaming;

public record LlmStreamChunk
{
    public required string ProviderName { get; init; }
    public required string Content { get; init; }
    public string? AccumulatedContent { get; init; }
    public int TokenIndex { get; init; }
    public bool IsFinal { get; init; }
    public LlmStreamMetadata? Metadata { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record LlmStreamMetadata
{
    public int? TotalTokens { get; init; }
    public long? TotalLatencyMs { get; init; }
    public decimal? EstimatedCost { get; init; }
    public double? TokensPerSecond { get; init; }
    public string? ModelName { get; init; }
}
```

---

## Implementation Strategy

### Provider Pattern (Apply to All Providers)

**Reference Implementation Pattern** (apply to OpenAI, Anthropic, Gemini, Azure):

```csharp
public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
    Brief brief, PlanSpec spec,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // 1. Build prompt
    var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
    var userPrompt = _promptCustomizationService.BuildCustomizedPrompt(brief, spec, brief.PromptModifiers);
    
    // 2. Create streaming request with stream=true parameter
    var requestBody = BuildStreamingRequest(systemPrompt, userPrompt);
    
    // 3. Send request with ResponseHeadersRead
    var response = await _httpClient.SendAsync(
        CreateRequest(requestBody), 
        HttpCompletionOption.ResponseHeadersRead, 
        ct);
    
    // 4. Parse SSE stream
    var stream = await response.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);
    var accumulated = new StringBuilder();
    var tokenCount = 0;
    var startTime = DateTime.UtcNow;
    
    while (!reader.EndOfStream && !ct.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        if (!line?.StartsWith("data: ")) continue;
        
        var data = line.Substring(6).Trim();
        if (data == "[DONE]") // or check provider-specific finish condition
        {
            yield return CreateFinalChunk(accumulated.ToString(), tokenCount, startTime);
            break;
        }
        
        var contentChunk = ParseProviderChunk(data); // Provider-specific parsing
        if (!string.IsNullOrEmpty(contentChunk))
        {
            accumulated.Append(contentChunk);
            tokenCount++;
            
            yield return new LlmStreamChunk
            {
                ProviderName = GetProviderName(),
                Content = contentChunk,
                AccumulatedContent = accumulated.ToString(),
                TokenIndex = tokenCount,
                IsFinal = false
            };
        }
    }
}

public bool SupportsStreaming => true;
```

**Provider-Specific Details**:

| Provider | Endpoint | Stream Parameter | Event Format | Finish Signal |
|----------|----------|------------------|--------------|---------------|
| OpenAI | `/v1/chat/completions` | `stream: true` | `data: {...}` then `data: [DONE]` | `[DONE]` |
| Anthropic | `/v1/messages` | `stream: true` | `event: content_block_delta` | `event: message_stop` |
| Gemini | `:streamGenerateContent?alt=sse` | URL parameter | `data: {...}` | `finishReason: "STOP"` |
| Azure OpenAI | Same as OpenAI | Same as OpenAI | Same as OpenAI | Same as OpenAI |

### RuleBased Provider (Simulated Streaming)

```csharp
public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
    Brief brief, PlanSpec spec,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var script = await DraftScriptAsync(brief, spec, ct);
    var words = script.Split(' ');
    var accumulated = new StringBuilder();
    
    for (int i = 0; i < words.Length; i += 5) // Chunk by 5 words
    {
        var chunk = string.Join(" ", words.Skip(i).Take(5)) + " ";
        accumulated.Append(chunk);
        
        yield return new LlmStreamChunk
        {
            ProviderName = "RuleBased",
            Content = chunk,
            AccumulatedContent = accumulated.ToString(),
            TokenIndex = i / 5,
            IsFinal = i + 5 >= words.Length
        };
        
        await Task.Delay(50, ct); // Simulate streaming delay
    }
}

public bool SupportsStreaming => false; // Simulated only
```

---

## API Server-Sent Events Endpoint

**File**: `Aura.Api/Controllers/ScriptsController.cs`

```csharp
[HttpPost("generate-stream")]
[Produces("text/event-stream")]
public async Task GenerateScriptStream(
    [FromBody] ScriptGenerationRequest request,
    CancellationToken ct)
{
    Response.Headers.Add("Content-Type", "text/event-stream");
    Response.Headers.Add("Cache-Control", "no-cache");
    Response.Headers.Add("Connection", "keep-alive");
    
    var brief = MapToBrief(request);
    var spec = MapToPlanSpec(request);
    var provider = _llmProviderFactory.GetProvider(request.ProviderName ?? "OpenAI");
    
    if (!provider.SupportsStreaming)
    {
        await WriteError("Provider does not support streaming");
        return;
    }
    
    try
    {
        await foreach (var chunk in provider.DraftScriptStreamAsync(brief, spec, ct))
        {
            var json = JsonSerializer.Serialize(new
            {
                provider = chunk.ProviderName,
                content = chunk.Content,
                accumulated = chunk.AccumulatedContent,
                tokenIndex = chunk.TokenIndex,
                isFinal = chunk.IsFinal,
                metadata = chunk.Metadata
            });
            
            await Response.WriteAsync($"event: chunk\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
            
            if (chunk.IsFinal)
            {
                await Response.WriteAsync("event: done\ndata: {}\n\n", ct);
                break;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Streaming error");
        await WriteError(ex.Message);
    }
}
```

---

## Frontend Integration

**File**: `Aura.Web/src/components/streaming/StreamingScriptDisplay.tsx`

**Key Implementation Points**:

1. **Use EventSource or fetch with ReadableStream** for SSE consumption  
2. **Display real-time content** with auto-scroll  
3. **Show progress indicators**: token count, tokens/sec, estimated cost  
4. **Implement cancellation** via AbortController  
5. **Handle reconnection** on network failures  
6. **Parse SSE events**: `chunk`, `done`, `error`

**Minimal Example**:

```typescript
const [content, setContent] = useState('');
const [isStreaming, setIsStreaming] = useState(true);
const abortController = useRef(new AbortController());

useEffect(() => {
  const streamScript = async () => {
    const response = await fetch('/api/scripts/generate-stream', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestBody),
      signal: abortController.current.signal
    });
    
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n\n');
      buffer = lines.pop() || '';
      
      for (const line of lines) {
        if (line.includes('event: chunk')) {
          const dataMatch = line.match(/data: (.+)/);
          if (dataMatch) {
            const chunk = JSON.parse(dataMatch[1]);
            setContent(chunk.accumulated);
          }
        }
      }
    }
    setIsStreaming(false);
  };  
  
  streamScript();
  return () => abortController.current.abort();
}, []);

const handleCancel = () => abortController.current.abort();
```

**Full component**: See reference implementation in `src/components/streaming/`

---

## Testing Requirements

### Unit Tests (`Aura.Tests/Streaming/`)

```csharp
[Fact]
public async Task StreamingGeneration_ShouldProduceIncrementalContent()
{
    var provider = CreateTestProvider();
    var chunks = await provider.DraftScriptStreamAsync(brief, spec).ToListAsync();
    
    Assert.NotEmpty(chunks);
    Assert.True(chunks.Last().IsFinal);
    Assert.NotNull(chunks.Last().Metadata);
}

[Fact]
public async Task Streaming_CancellationToken_ShouldStopStream()
{
    var cts = new CancellationTokenSource();
    var receivedChunks = 0;
    
    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
        await foreach (var chunk in provider.DraftScriptStreamAsync(brief, spec, cts.Token))
        {
            if (++receivedChunks == 5) cts.Cancel();
        }
    });
}
```

### Integration Tests (`Aura.E2E/Streaming/`)

```csharp
[Fact]
public async Task SSE_Endpoint_ShouldStreamScriptGeneration()
{
    var response = await _client.PostAsync("/api/scripts/generate-stream", content);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("text/event-stream", response.Content.Headers.ContentType.MediaType);
    
    var chunkCount = 0;
    await foreach (var line in ReadLinesAsync(response.Content))
    {
        if (line.StartsWith("event: chunk")) chunkCount++;
    }
    
    Assert.True(chunkCount > 0);
}
}
```

---

## Configuration

**File**: `Aura.Api/appsettings.json`

```json
{
  "Streaming": {
    "Enabled": true,
    "MaxConcurrentStreams": 10,
    "StreamTimeoutSeconds": 300,
    "BufferSize": 4096
  }
}
```

---

## Implementation Checklist

### Phase 1: Core Infrastructure (Days 1-2)
- [ ] Create `LlmStreamChunk` and `LlmStreamMetadata` models
- [ ] Update `ILlmProvider` interface with streaming methods
- [ ] Implement OpenAI streaming (reference implementation)
- [ ] Add unit tests for OpenAI streaming

### Phase 2: Provider Rollout (Days 3-4)
- [ ] Implement Anthropic streaming
- [ ] Implement Gemini streaming
- [ ] Implement Azure OpenAI streaming
- [ ] Add RuleBased simulated streaming
- [ ] Unit tests for all providers

### Phase 3: API & Frontend (Days 5-6)
- [ ] Create SSE endpoint in ScriptsController
- [ ] Build StreamingScriptDisplay React component
- [ ] Implement cancellation and error handling
- [ ] Integration tests for SSE endpoint

### Phase 4: Polish & Deploy (Day 7)
- [ ] Performance testing and optimization
- [ ] Documentation updates
- [ ] Feature flag configuration
- [ ] Deployment and monitoring setup

---

## Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| First Token Latency | <500ms (p95) | Time to first chunk |
| Throughput | >20 tokens/sec (p50) | Average streaming speed |
| Concurrent Streams | 10+ users | Load testing |
| Memory per Stream | <100MB | Resource profiling |
| Error Rate | <1% | Telemetry |

---

## Rollback Plan

1. Set `Streaming.Enabled = false` in appsettings.json
2. Deploy config change (no code redeployment needed)
3. All requests fall back to non-streaming mode
4. Investigate and fix issues
5. Re-enable after validation

---

## Success Metrics

- **Adoption**: >70% of users enable streaming within 1 week
- **Performance**: 80% reduction in perceived generation time
- **Satisfaction**: >4.5/5 user rating
- **Stability**: <0.5% error rate
- **Cost**: No increase in provider API costs

---

## Documentation

Create `docs/features/STREAMING_GUIDE.md` with:
- Overview of streaming feature
- Supported providers list
- UI usage instructions
- Benefits and technical details
- Troubleshooting guide

---

**This PR delivers a foundational enhancement that dramatically improves UX and enables future real-time features.**
