using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Service for generating text embeddings for RAG
/// Supports local and provider-backed embedding generation with batch processing and rate limit handling
/// </summary>
public class EmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly EmbeddingConfig _config;
    
    // Rate limiting state
    private DateTime _lastRequestTime = DateTime.MinValue;
    private int _consecutiveErrors;
    private readonly TimeSpan _minRequestInterval = TimeSpan.FromMilliseconds(100);
    private readonly int _maxRetries = 3;
    private readonly int _batchSize = 100; // OpenAI supports up to 2048 inputs

    public EmbeddingService(
        ILogger<EmbeddingService> logger,
        IHttpClientFactory httpClientFactory,
        EmbeddingConfig config)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }

    /// <summary>
    /// Generate embeddings for a list of text chunks with batch processing
    /// </summary>
    public async Task<List<float[]>> GenerateEmbeddingsAsync(
        List<string> texts,
        CancellationToken ct = default)
    {
        if (texts.Count == 0)
        {
            return new List<float[]>();
        }

        _logger.LogInformation("Generating embeddings for {Count} texts using {Provider}",
            texts.Count, _config.Provider);

        try
        {
            return _config.Provider switch
            {
                EmbeddingProvider.Local => await GenerateLocalEmbeddingsAsync(texts, ct).ConfigureAwait(false),
                EmbeddingProvider.OpenAI => await GenerateOpenAIEmbeddingsBatchAsync(texts, ct).ConfigureAwait(false),
                EmbeddingProvider.Ollama => await GenerateOllamaEmbeddingsBatchAsync(texts, ct).ConfigureAwait(false),
                _ => GenerateSimpleEmbeddings(texts)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings, falling back to simple embeddings");
            return GenerateSimpleEmbeddings(texts);
        }
    }

    /// <summary>
    /// Generate a single embedding for a text
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken ct = default)
    {
        var embeddings = await GenerateEmbeddingsAsync(new List<string> { text }, ct).ConfigureAwait(false);
        return embeddings.FirstOrDefault() ?? Array.Empty<float>();
    }

    private async Task<List<float[]>> GenerateLocalEmbeddingsAsync(
        List<string> texts,
        CancellationToken ct)
    {
        _logger.LogDebug("Using local simple embedding generation");
        return await Task.FromResult(GenerateSimpleEmbeddings(texts)).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate OpenAI embeddings with batch processing and rate limit handling
    /// </summary>
    private async Task<List<float[]>> GenerateOpenAIEmbeddingsBatchAsync(
        List<string> texts,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            _logger.LogWarning("OpenAI API key not configured, falling back to simple embeddings");
            return GenerateSimpleEmbeddings(texts);
        }

        var allEmbeddings = new List<float[]>();

        // Process in batches
        for (int i = 0; i < texts.Count; i += _batchSize)
        {
            var batch = texts.Skip(i).Take(_batchSize).ToList();
            var batchEmbeddings = await GenerateOpenAIEmbeddingsWithRetryAsync(batch, ct).ConfigureAwait(false);
            allEmbeddings.AddRange(batchEmbeddings);

            // Log progress for large batches
            if (texts.Count > _batchSize)
            {
                _logger.LogInformation("Processed {Processed}/{Total} embeddings", 
                    Math.Min(i + _batchSize, texts.Count), texts.Count);
            }
        }

        return allEmbeddings;
    }

    private async Task<List<float[]>> GenerateOpenAIEmbeddingsWithRetryAsync(
        List<string> texts,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                // Rate limiting - wait if needed
                await ApplyRateLimitingAsync(ct).ConfigureAwait(false);

                var request = new
                {
                    input = texts,
                    model = _config.ModelName ?? "text-embedding-ada-002"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/embeddings",
                    content,
                    ct).ConfigureAwait(false);

                // Handle rate limiting
                if ((int)response.StatusCode == 429)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                    _logger.LogWarning("Rate limited by OpenAI. Waiting {Seconds}s before retry (attempt {Attempt}/{MaxRetries})", 
                        retryAfter.TotalSeconds, attempt + 1, _maxRetries);
                    
                    await Task.Delay(retryAfter, ct).ConfigureAwait(false);
                    _consecutiveErrors++;
                    continue;
                }

                response.EnsureSuccessStatusCode();
                _consecutiveErrors = 0;

                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseBody);

                if (result?.Data == null || result.Data.Count == 0)
                {
                    _logger.LogWarning("No embeddings returned from OpenAI");
                    return GenerateSimpleEmbeddings(texts);
                }

                return result.Data.Select(d => d.Embedding).ToList();
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                _consecutiveErrors++;
                var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning(ex, "OpenAI request failed (attempt {Attempt}/{MaxRetries}). Retrying in {Seconds}s", 
                    attempt + 1, _maxRetries, backoffDelay.TotalSeconds);
                await Task.Delay(backoffDelay, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI embeddings API");
                return GenerateSimpleEmbeddings(texts);
            }
        }

        _logger.LogWarning("All retries exhausted for OpenAI embeddings, falling back to simple embeddings");
        return GenerateSimpleEmbeddings(texts);
    }

    /// <summary>
    /// Generate Ollama embeddings with batch processing
    /// </summary>
    private async Task<List<float[]>> GenerateOllamaEmbeddingsBatchAsync(
        List<string> texts,
        CancellationToken ct)
    {
        var baseUrl = _config.BaseUrl ?? "http://localhost:11434";
        var modelName = _config.ModelName ?? "nomic-embed-text";

        try
        {
            var embeddings = new List<float[]>();

            foreach (var text in texts)
            {
                var embedding = await GenerateOllamaEmbeddingWithRetryAsync(text, baseUrl, modelName, ct).ConfigureAwait(false);
                embeddings.Add(embedding);
            }

            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama embeddings API");
            return GenerateSimpleEmbeddings(texts);
        }
    }

    private async Task<float[]> GenerateOllamaEmbeddingWithRetryAsync(
        string text,
        string baseUrl,
        string modelName,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var request = new
                {
                    model = modelName,
                    prompt = text
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{baseUrl}/api/embeddings",
                    content,
                    ct).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseBody);

                if (result?.Embedding != null)
                {
                    return result.Embedding;
                }
                
                _logger.LogWarning("No embedding returned from Ollama for text");
                return GenerateSimpleEmbedding(text);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Ollama request failed (attempt {Attempt}/{MaxRetries}). Retrying in {Seconds}s", 
                    attempt + 1, _maxRetries, backoffDelay.TotalSeconds);
                await Task.Delay(backoffDelay, ct).ConfigureAwait(false);
            }
        }

        return GenerateSimpleEmbedding(text);
    }

    private async Task ApplyRateLimitingAsync(CancellationToken ct)
    {
        var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
        
        // Apply exponential backoff based on consecutive errors
        var requiredInterval = _minRequestInterval;
        if (_consecutiveErrors > 0)
        {
            requiredInterval = TimeSpan.FromMilliseconds(
                _minRequestInterval.TotalMilliseconds * Math.Pow(2, _consecutiveErrors));
        }

        if (timeSinceLastRequest < requiredInterval)
        {
            var delay = requiredInterval - timeSinceLastRequest;
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        _lastRequestTime = DateTime.UtcNow;
    }

    private List<float[]> GenerateSimpleEmbeddings(List<string> texts)
    {
        return texts.Select(GenerateSimpleEmbedding).ToList();
    }

    private float[] GenerateSimpleEmbedding(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[384];
        }

        var words = text.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' },
                StringSplitOptions.RemoveEmptyEntries);

        var embedding = new float[384];
        var random = new Random(text.GetHashCode());

        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        var wordSet = new HashSet<string>(words);
        for (int i = 0; i < Math.Min(wordSet.Count, embedding.Length); i++)
        {
            var word = wordSet.ElementAt(i);
            var wordHash = Math.Abs(word.GetHashCode() % embedding.Length);
            embedding[wordHash] += 0.5f;
        }

        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
        }

        return embedding;
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Embeddings must have the same length");
        }

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private class OpenAIEmbeddingResponse
    {
        public List<OpenAIEmbeddingData> Data { get; set; } = new();
    }

    private class OpenAIEmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    private class OllamaEmbeddingResponse
    {
        public float[]? Embedding { get; set; }
    }
}

/// <summary>
/// Configuration for embedding generation
/// </summary>
public record EmbeddingConfig
{
    public EmbeddingProvider Provider { get; init; } = EmbeddingProvider.Local;
    public string? ApiKey { get; init; }
    public string? BaseUrl { get; init; }
    public string? ModelName { get; init; }
    public int DimensionSize { get; init; } = 384;
}

/// <summary>
/// Supported embedding providers
/// </summary>
public enum EmbeddingProvider
{
    Local,
    OpenAI,
    Ollama
}
