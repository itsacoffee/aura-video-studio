using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Streaming;

/// <summary>
/// Streaming client for Ollama API that supports real-time token-by-token generation
/// </summary>
public class OllamaStreamingClient
{
    private readonly ILogger<OllamaStreamingClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaStreamingClient(
        ILogger<OllamaStreamingClient> logger,
        HttpClient httpClient,
        string baseUrl = "http://127.0.0.1:11434")
    {
        _logger = logger;
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Stream a completion from Ollama with real-time progress updates
    /// </summary>
    /// <param name="model">Model name to use</param>
    /// <param name="prompt">Prompt text</param>
    /// <param name="temperature">Temperature (0.0-1.0)</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Async enumerable of streaming chunks</returns>
    public async IAsyncEnumerable<OllamaStreamingChunk> StreamCompletionAsync(
        string model,
        string prompt,
        double temperature = 0.7,
        int maxTokens = 2048,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Starting streaming completion with model: {Model}", model);

        var requestBody = new
        {
            model = model,
            prompt = prompt,
            stream = true,
            options = new
            {
                temperature = temperature,
                num_predict = maxTokens
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
        {
            Content = content
        };

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            stream = await response.Content.ReadAsStreamAsync(ct);
            reader = new StreamReader(stream);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during streaming from Ollama");
            throw new InvalidOperationException("Failed to connect to Ollama for streaming", ex);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Streaming cancelled by user");
            throw;
        }

        var totalTokens = 0;
        var buffer = new StringBuilder();

        try
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                OllamaStreamingChunk? chunk = null;
                JsonDocument? doc = null;
                try
                {
                    doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("response", out var responseToken))
                    {
                        var token = responseToken.GetString() ?? string.Empty;
                        buffer.Append(token);
                        totalTokens++;

                        var isDone = root.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean();

                        chunk = new OllamaStreamingChunk
                        {
                            Token = token,
                            TotalText = buffer.ToString(),
                            IsComplete = isDone,
                            TokenCount = totalTokens,
                            Model = model
                        };

                        if (isDone)
                        {
                            _logger.LogInformation("Streaming completion finished. Total tokens: {Tokens}", totalTokens);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming JSON line: {Line}", line);
                }
                finally
                {
                    doc?.Dispose();
                }

                if (chunk != null)
                {
                    yield return chunk;
                    if (chunk.IsComplete)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }

    /// <summary>
    /// Stream a chat completion with message history
    /// </summary>
    public async IAsyncEnumerable<OllamaStreamingChunk> StreamChatAsync(
        string model,
        IEnumerable<ChatMessage> messages,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Starting streaming chat with model: {Model}", model);

        var requestBody = new
        {
            model = model,
            messages = messages,
            stream = true,
            options = new
            {
                temperature = temperature
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = content
        };

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            stream = await response.Content.ReadAsStreamAsync(ct);
            reader = new StreamReader(stream);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during streaming chat from Ollama");
            throw new InvalidOperationException("Failed to connect to Ollama for streaming chat", ex);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Streaming chat cancelled by user");
            throw;
        }

        var totalTokens = 0;
        var buffer = new StringBuilder();

        try
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                OllamaStreamingChunk? chunk = null;
                JsonDocument? doc = null;
                try
                {
                    doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("message", out var messageProp) &&
                        messageProp.TryGetProperty("content", out var contentProp))
                    {
                        var token = contentProp.GetString() ?? string.Empty;
                        buffer.Append(token);
                        totalTokens++;

                        var isDone = root.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean();

                        chunk = new OllamaStreamingChunk
                        {
                            Token = token,
                            TotalText = buffer.ToString(),
                            IsComplete = isDone,
                            TokenCount = totalTokens,
                            Model = model
                        };

                        if (isDone)
                        {
                            _logger.LogInformation("Streaming chat finished. Total tokens: {Tokens}", totalTokens);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming JSON line: {Line}", line);
                }
                finally
                {
                    doc?.Dispose();
                }

                if (chunk != null)
                {
                    yield return chunk;
                    if (chunk.IsComplete)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }
}

/// <summary>
/// Represents a chunk of streaming output from Ollama
/// </summary>
public class OllamaStreamingChunk
{
    /// <summary>
    /// The new token received in this chunk
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The complete accumulated text so far
    /// </summary>
    public string TotalText { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Total number of tokens received so far
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// The model being used
    /// </summary>
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Represents a chat message for Ollama chat API
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = "user"; // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
}
