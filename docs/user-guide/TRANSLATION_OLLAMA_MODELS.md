# Ollama Model Recommendations for Translation

This guide helps you select the best Ollama model for translation tasks in Aura Video Studio.

## Best Models for Translation

Based on testing with the Aura Video Studio localization feature:

### Tier 1: Excellent (Recommended)

| Model | Description |
|-------|-------------|
| **llama3.1 (8B)** | Best overall balance of quality and speed. Produces clean output with minimal artifacts. |
| **mistral (7B)** | Very clean output, minimal artifacts. Excellent for European languages. |
| **gemma2 (9B)** | High accuracy, good with cultural nuances. Great for nuanced translations. |

### Tier 2: Good

| Model | Description |
|-------|-------------|
| **llama3.2 (3B)** | Fast, acceptable quality for simple translations. Good for quick iterations. |
| **phi3 (3.8B)** | Compact, decent for technical content. Lower memory requirements. |

### Tier 3: Fair (Use with Caution)

| Model | Description |
|-------|-------------|
| **llama2** | Older model, tends to generate verbose output with JSON artifacts. |
| **codellama** | Optimized for code, not ideal for natural language translation. |

## Installation

Install your preferred model using Ollama:

```bash
# Install recommended model
ollama pull llama3.1

# Verify installation
ollama list

# Run the model
ollama run llama3.1
```

## Model Parameters for Translation

When configuring Ollama models for translation in Aura, consider these parameters:

```json
{
  "temperature": 0.3,
  "top_p": 0.9,
  "repeat_penalty": 1.1,
  "num_ctx": 4096
}
```

### Parameter Guidelines

| Parameter | Recommended Value | Purpose |
|-----------|------------------|---------|
| `temperature` | 0.3 | Lower values produce more consistent, literal translations |
| `top_p` | 0.9 | Nucleus sampling for balanced output variety |
| `repeat_penalty` | 1.1 | Reduces repetition in longer translations |
| `num_ctx` | 4096 | Context window for handling longer texts |

## Troubleshooting by Model

### llama2 generates structured JSON output

**Problem**: The model returns JSON objects like `{"title": "...", "translation": "..."}` instead of plain translated text.

**Solution**: 
- Upgrade to llama3.1 or mistral for cleaner output
- The automatic cleanup in Aura handles this, but Tier 1 models rarely produce such artifacts

### Model returns "Translation:" prefix

**Problem**: Output begins with prefixes like "Translation:", "Here is the translation:", etc.

**Solution**:
- Automatic cleanup is applied by Aura Video Studio
- For best results, consider switching to a Tier 1 model
- Check the translation quality metrics in the result to monitor this issue

### Translation takes longer than 30 seconds

**Problem**: Translations are slow, especially for longer texts.

**Solutions**:
1. Use a smaller model (llama3.2 3B) for faster processing
2. Upgrade hardware (more RAM, faster CPU/GPU)
3. Enable GPU acceleration if using an NVIDIA GPU
4. Reduce batch size for very long texts

### Model produces unusually long output

**Problem**: Translation is 3x or more the length of the source text.

**Causes**:
- Model generating explanations or tutorials instead of translations
- Verbose output patterns in older models

**Solutions**:
- Upgrade to Tier 1 models (llama3.1, mistral, gemma2)
- Check the `LengthRatio` in translation metrics
- Report unusual patterns in the quality issues

## Performance Benchmarks

Approximate performance on consumer hardware (16GB RAM, no GPU acceleration):

| Model | Speed (tokens/sec) | Quality Grade | Artifacts? | Memory Usage |
|-------|-------------------|---------------|------------|--------------|
| llama3.1 8B | 25-40 | Excellent | Rare | ~8GB |
| mistral 7B | 30-45 | Excellent | Rare | ~6GB |
| gemma2 9B | 20-35 | Excellent | Rare | ~9GB |
| llama3.2 3B | 50-70 | Good | Occasional | ~4GB |
| phi3 3.8B | 45-60 | Good | Occasional | ~4GB |
| llama2 7B | 25-40 | Fair | Common | ~6GB |

### With GPU Acceleration (NVIDIA RTX)

| Model | Speed (tokens/sec) | Notes |
|-------|-------------------|-------|
| llama3.1 8B | 80-120 | Recommended for production |
| mistral 7B | 90-130 | Fastest Tier 1 model |
| gemma2 9B | 70-100 | Best quality per token |

## Quality Metrics Explained

Aura Video Studio tracks translation quality metrics automatically:

### TranslationQualityGrade

| Grade | Description |
|-------|-------------|
| **Excellent** | Clean output, appropriate length, fast processing |
| **Good** | Minor issues that were cleaned up automatically |
| **Fair** | Multiple issues detected, may need manual review |
| **Poor** | Significant problems, output may need regeneration |

### Key Metrics

| Metric | Description | Ideal Range |
|--------|-------------|-------------|
| `LengthRatio` | Translation length / Source length | 0.5 - 2.5 |
| `HasStructuredArtifacts` | JSON/XML detected in output | false |
| `HasUnwantedPrefixes` | "Translation:" etc. in output | false |
| `TranslationTimeSeconds` | Time to complete | < 30s |

## Recommended Workflow

1. **Start with llama3.1** for general translation tasks
2. **Monitor quality metrics** in the translation results
3. **Switch to mistral** if you notice frequent artifacts
4. **Use llama3.2** when speed is critical and text is simple
5. **Avoid llama2** for production translations

## Getting Help

If you experience issues with translation quality:

1. Check the translation metrics in the API response
2. Review the quality issues list for specific problems
3. Try a different Tier 1 model
4. Report persistent issues on GitHub

## Related Documentation

- [Translation User Guide](./TRANSLATION_USER_GUIDE.md)
- [Provider Integration Guide](./PROVIDER_INTEGRATION_GUIDE.md)
- [Local Providers Setup](./LOCAL_PROVIDERS_SETUP.md)
