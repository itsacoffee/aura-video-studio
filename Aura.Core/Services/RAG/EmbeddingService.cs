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
/// Supports local and provider-backed embedding generation
/// </summary>
public class EmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly EmbeddingConfig _config;

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
    /// Generate embeddings for a list of text chunks
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
                EmbeddingProvider.OpenAI => await GenerateOpenAIEmbeddingsAsync(texts, ct).ConfigureAwait(false),
                EmbeddingProvider.Ollama => await GenerateOllamaEmbeddingsAsync(texts, ct).ConfigureAwait(false),
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

    private async Task<List<float[]>> GenerateOpenAIEmbeddingsAsync(
        List<string> texts,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            _logger.LogWarning("OpenAI API key not configured, falling back to simple embeddings");
            return GenerateSimpleEmbeddings(texts);
        }

        try
        {
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

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseBody);

            if (result?.Data == null || result.Data.Count == 0)
            {
                _logger.LogWarning("No embeddings returned from OpenAI");
                return GenerateSimpleEmbeddings(texts);
            }

            return result.Data.Select(d => d.Embedding).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI embeddings API");
            return GenerateSimpleEmbeddings(texts);
        }
    }

    private async Task<List<float[]>> GenerateOllamaEmbeddingsAsync(
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
                    embeddings.Add(result.Embedding);
                }
                else
                {
                    _logger.LogWarning("No embedding returned for text");
                    embeddings.Add(GenerateSimpleEmbedding(text));
                }
            }

            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama embeddings API");
            return GenerateSimpleEmbeddings(texts);
        }
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
