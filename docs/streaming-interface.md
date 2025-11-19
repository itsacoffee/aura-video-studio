# Unified Streaming Interface for LLM Providers

This document describes the unified streaming interface implementation for real-time script generation across all LLM providers (OpenAI, Anthropic, Gemini, Azure OpenAI, Ollama, RuleBased).

## Overview

The unified streaming interface provides token-by-token script generation with real-time metrics, cost tracking, and automatic provider fallback. This feature was implemented in PR #416 (backend) and this PR (frontend integration).

## Architecture

### Backend (`Aura.Api`, `Aura.Providers`)

**Endpoint**: `POST /api/scripts/generate/stream`

**Request Format**:
```json
{
  "topic": "AI video creation",
  "audience": "Content creators",
  "targetDurationSeconds": 60,
  "preferredProvider": "Anthropic"
}
```

**SSE Event Types**:

1. **`init`** - Provider characteristics (sent first)
```json
{
  "eventType": "init",
  "providerName": "Anthropic",
  "isLocal": false,
  "expectedFirstTokenMs": 400,
  "expectedTokensPerSec": 15,
  "costPer1KTokens": 0.015,
  "supportsStreaming": true
}
```

2. **`chunk`** - Token-by-token content (sent during generation)
```json
{
  "eventType": "chunk",
  "content": "Hello ",
  "accumulatedContent": "Hello ",
  "tokenIndex": 1
}
```

3. **`complete`** - Final metrics (sent on completion)
```json
{
  "eventType": "complete",
  "content": "",
  "accumulatedContent": "Complete script text...",
  "tokenCount": 150,
  "metadata": {
    "totalTokens": 150,
    "estimatedCost": 0.00225,
    "tokensPerSecond": 12.5,
    "isLocalModel": false,
    "modelName": "claude-3-opus",
    "timeToFirstTokenMs": 385,
    "totalDurationMs": 12000,
    "finishReason": "stop"
  }
}
```

4. **`error`** - Error information (sent on failure)
```json
{
  "eventType": "error",
  "errorMessage": "API key not configured",
  "correlationId": "xyz789"
}
```

**Provider Fallback Chain**:
1. Requested provider (if specified)
2. Ollama (local, free)
3. RuleBased (always available, template-based)

### Frontend (`Aura.Web`)

**Service Layer**: `src/services/api/ollamaService.ts`

**Key Functions**:
- `streamGeneration()` - Main streaming function using fetch API
- `streamGenerationWithEventSource()` - Alternative using EventSource API (deprecated)

**Components**:

1. **`LlmProviderSelector`** - Provider selection with characteristics
   - Shows cost per 1K tokens
   - Shows expected latency
   - Shows local/cloud indicator
   - Supports "Auto" mode with fallback

2. **`StreamingMetrics`** - Real-time and final metrics display
   - Real-time: Progress bar, token count, provider info
   - Final: Cost, tokens/sec, latency, duration, model name

3. **`StreamingScriptDemo`** - Complete demo page (dev tools only)
   - Full integration showcase
   - Available at `/streaming-demo`

## Provider Support

| Provider | Cost/1K | Latency | Local | Status |
|----------|---------|---------|-------|--------|
| RuleBased | Free | ~100ms | ✅ | ✅ Always Available |
| Ollama | Free | ~2s | ✅ | ✅ Requires Installation |
| OpenAI GPT-4 | $0.001 | ~300ms | ❌ | ✅ Requires API Key |
| Anthropic Claude | $0.015 | ~400ms | ❌ | ✅ Requires API Key |
| Google Gemini | $0.0005 | ~500ms | ❌ | ✅ Requires API Key |
| Azure OpenAI | $0.01 | ~300ms | ❌ | ✅ Requires Credentials |

## Usage Examples

### Basic Streaming with Auto Provider

```typescript
import { streamGeneration } from '@/services/api/ollamaService';

await streamGeneration(
  {
    topic: 'AI in video production',
    targetDurationSeconds: 60,
  },
  (event) => {
    switch (event.eventType) {
      case 'init':
        console.log('Using provider:', event.providerName);
        break;
      case 'chunk':
        console.log('Token:', event.content);
        break;
      case 'complete':
        console.log('Done! Cost:', event.metadata.estimatedCost);
        break;
      case 'error':
        console.error('Error:', event.errorMessage);
        break;
    }
  },
  abortSignal
);
```

### With Specific Provider

```typescript
await streamGeneration(
  {
    topic: 'AI in video production',
    targetDurationSeconds: 60,
    preferredProvider: 'Anthropic',  // Request specific provider
  },
  handleEvent,
  abortSignal
);
```

### Using Components

```tsx
import { LlmProviderSelector } from '@/components/Streaming/LlmProviderSelector';
import { StreamingMetrics } from '@/components/Streaming/StreamingMetrics';

function MyComponent() {
  const [provider, setProvider] = useState<string | undefined>();
  const [initEvent, setInitEvent] = useState<StreamInitEvent>();
  const [completeEvent, setCompleteEvent] = useState<StreamCompleteEvent>();
  const [isStreaming, setIsStreaming] = useState(false);

  return (
    <>
      <LlmProviderSelector
        value={provider}
        onChange={setProvider}
        showAutoOption={true}
      />
      
      <StreamingMetrics
        initEvent={initEvent}
        completeEvent={completeEvent}
        isStreaming={isStreaming}
      />
    </>
  );
}
```

## Testing

### Manual Testing

1. Start the application with dev tools enabled
2. Navigate to `/streaming-demo`
3. Test scenarios:
   - Auto mode (fallback chain)
   - Each individual provider
   - Cancellation
   - Error handling
   - Cost calculation accuracy

### Automated Testing

**Unit Tests** (To be added):
- `LlmProviderSelector.test.tsx` - Component behavior
- `StreamingMetrics.test.tsx` - Metrics display
- `ollamaService.test.ts` - SSE parsing

**Integration Tests** (To be added):
- Streaming with mock providers
- Fallback chain behavior
- Error handling scenarios

## Configuration

### Backend Provider Registration

Providers are registered as keyed services in `Program.cs`:

```csharp
builder.Services.AddKeyedScoped<ILlmProvider, AnthropicLlmProvider>("Anthropic");
builder.Services.AddKeyedScoped<ILlmProvider, GeminiLlmProvider>("Gemini");
builder.Services.AddKeyedScoped<ILlmProvider, AzureOpenAiLlmProvider>("Azure");
builder.Services.AddKeyedScoped<ILlmProvider, OpenAiLlmProvider>("OpenAI");
builder.Services.AddKeyedScoped<ILlmProvider, OllamaLlmProvider>("Ollama");
builder.Services.AddKeyedScoped<ILlmProvider, RuleBasedLlmProvider>("RuleBased");
```

### Frontend Provider Configuration

Provider metadata in `src/state/providers.ts`:

```typescript
export const ScriptProviders = [
  {
    value: 'Anthropic',
    label: 'Anthropic Claude (Pro)',
    description: 'Cloud AI (Claude), high quality, requires API key',
    cost: 0.015,
    expectedLatency: 400,
    isLocal: false,
  },
  // ... other providers
];
```

## Cost Tracking

Cost is calculated per provider based on token count:

- **OpenAI**: $0.001 per 1K tokens
- **Anthropic**: $0.015 per 1K tokens
- **Gemini**: $0.0005 per 1K tokens
- **Azure**: $0.01 per 1K tokens (variable by deployment)
- **Ollama**: Free (local)
- **RuleBased**: Free (template-based)

Cost is displayed in real-time during generation and as a final metric.

## Performance Metrics

All providers report:
- **Time to First Token**: Latency until first token arrives
- **Tokens per Second**: Generation speed
- **Total Duration**: Complete generation time
- **Finish Reason**: Why generation stopped (stop, length, error)

## Error Handling

### Client-Side Errors
- Network failures: Automatic retry with exponential backoff (circuit breaker)
- Abort/cancellation: Graceful cleanup
- Parsing errors: Logged to console, streaming continues

### Server-Side Errors
- Provider unavailable: Automatic fallback to next provider
- API errors: Returned in `error` event with correlation ID
- Timeout: Configurable per provider

## Future Enhancements

### Planned Features
- [ ] Cost tracking history view
- [ ] Provider health monitoring dashboard
- [ ] Integration into CreateWizard main workflow
- [ ] Streaming for other operations (image prompts, refinement)
- [ ] Batch streaming for multiple scripts
- [ ] Real-time collaboration with shared streams

### Potential Optimizations
- [ ] Connection pooling for SSE
- [ ] Compression for large scripts
- [ ] Client-side caching of partial results
- [ ] WebSocket alternative for bidirectional communication

## Troubleshooting

### Stream Not Starting
1. Check provider API key configuration
2. Verify provider is running (Ollama, Stable Diffusion)
3. Check network connectivity
4. Review browser console for errors
5. Check backend logs for detailed errors

### Incorrect Cost Calculation
1. Verify provider cost rates in `providers.ts`
2. Check token counting accuracy
3. Compare with provider's official pricing

### Performance Issues
1. Check network latency
2. Verify provider API rate limits
3. Monitor system resources (CPU, RAM)
4. Consider using local provider (Ollama)

## References

- **PR #416**: Backend unified streaming implementation
- **SSE Specification**: [Server-Sent Events Spec](https://html.spec.whatwg.org/multipage/server-sent-events.html)
- **Provider APIs**:
  - [OpenAI Streaming](https://platform.openai.com/docs/api-reference/streaming)
  - [Anthropic Streaming](https://docs.anthropic.com/claude/reference/streaming)
  - [Google Gemini Streaming](https://ai.google.dev/tutorials/streaming)
  - [Azure OpenAI Streaming](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/streaming)

## Contributing

When adding new providers:
1. Implement `ILlmProvider` interface
2. Add streaming support via `DraftScriptStreamAsync`
3. Register as keyed service in `Program.cs`
4. Add provider metadata to `ScriptProviders` in frontend
5. Update this documentation
6. Add tests for new provider
