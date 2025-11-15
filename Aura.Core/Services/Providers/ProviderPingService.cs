using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Provider ping result with explicit diagnostic information
/// </summary>
public record CoreProviderPingResult
{
    public bool Attempted { get; init; }
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public string? HttpStatus { get; init; }
    public string? Endpoint { get; init; }
    public long? ResponseTimeMs { get; init; }
}

/// <summary>
/// Service for explicit provider connectivity testing (ping)
/// </summary>
public class ProviderPingService
{
    private readonly ILogger<ProviderPingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private const int TimeoutSeconds = 5;

    public ProviderPingService(
        ILogger<ProviderPingService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Ping OpenAI provider with actual network call
    /// </summary>
    public async Task<CoreProviderPingResult> PingOpenAIAsync(
        string? apiKey,
        string? baseUrl,
        CancellationToken cancellationToken = default)
    {
        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com" : baseUrl.TrimEnd('/');
        var endpoint = $"{effectiveBaseUrl}/v1/models";

        _logger.LogInformation("Pinging OpenAI at {Endpoint}", endpoint);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured");
            return new CoreProviderPingResult
            {
                Attempted = false,
                Success = false,
                ErrorCode = "ProviderNotConfigured",
                Message = "OpenAI is not configured. Add your API key first.",
                Endpoint = endpoint
            };
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "OpenAI ping completed: Status={StatusCode}, Time={ResponseTime}ms",
                response.StatusCode,
                responseTime);

            if (response.IsSuccessStatusCode)
            {
                return new CoreProviderPingResult
                {
                    Attempted = true,
                    Success = true,
                    Message = "OpenAI is reachable and your key works.",
                    HttpStatus = ((int)response.StatusCode).ToString(),
                    Endpoint = endpoint,
                    ResponseTimeMs = responseTime
                };
            }

            var errorCode = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ProviderKeyInvalid",
                HttpStatusCode.TooManyRequests => "ProviderRateLimited",
                _ when (int)response.StatusCode >= 500 => "ProviderServerError",
                _ => "ProviderConfigError"
            };

            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => 
                    "The API key is invalid or lacks permissions. Verify it and ensure it has correct permissions.",
                HttpStatusCode.TooManyRequests => 
                    "Rate limit exceeded. Please wait a moment and try again.",
                _ when (int)response.StatusCode >= 500 => 
                    "OpenAI server error. This is a temporary issue on OpenAI's side.",
                _ => 
                    $"OpenAI returned an error (HTTP {(int)response.StatusCode})."
            };

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                HttpStatus = ((int)response.StatusCode).ToString(),
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
        catch (HttpRequestException ex)
        {
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "Network error pinging OpenAI");

            var errorCode = "ProviderNetworkError";
            var message = "Cannot reach OpenAI. Check your internet connection, proxy, or VPN settings.";

            if (ex.InnerException?.Message.Contains("DNS", StringComparison.OrdinalIgnoreCase) == true)
            {
                message = "DNS resolution failed. Check your internet connection and DNS settings.";
            }
            else if (ex.InnerException?.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) == true ||
                     ex.InnerException?.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase) == true)
            {
                message = "Secure connection failed. Check your system date/time and firewall settings.";
            }

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
        catch (TaskCanceledException ex)
        {
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "Timeout pinging OpenAI");

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = "ProviderNetworkError",
                Message = $"Connection timed out after {TimeoutSeconds} seconds. Check your internet connection.",
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
        catch (Exception ex)
        {
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Unexpected error pinging OpenAI");

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = "ProviderConfigError",
                Message = $"Unexpected error: {ex.Message}",
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
    }

    /// <summary>
    /// Ping Anthropic provider with actual network call
    /// </summary>
    public async Task<CoreProviderPingResult> PingAnthropicAsync(
        string? apiKey,
        string? baseUrl,
        CancellationToken cancellationToken = default)
    {
        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.anthropic.com" : baseUrl.TrimEnd('/');
        var endpoint = $"{effectiveBaseUrl}/v1/messages";

        _logger.LogInformation("Pinging Anthropic at {Endpoint}", endpoint);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new CoreProviderPingResult
            {
                Attempted = false,
                Success = false,
                ErrorCode = "ProviderNotConfigured",
                Message = "Anthropic is not configured. Add your API key first.",
                Endpoint = endpoint
            };
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(
                """{"model":"claude-3-haiku-20240307","max_tokens":1,"messages":[{"role":"user","content":"Hi"}]}""",
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Anthropic ping completed: Status={StatusCode}, Time={ResponseTime}ms",
                response.StatusCode,
                responseTime);

            if (response.IsSuccessStatusCode)
            {
                return new CoreProviderPingResult
                {
                    Attempted = true,
                    Success = true,
                    Message = "Anthropic is reachable and your key works.",
                    HttpStatus = ((int)response.StatusCode).ToString(),
                    Endpoint = endpoint,
                    ResponseTimeMs = responseTime
                };
            }

            var errorCode = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ProviderKeyInvalid",
                HttpStatusCode.TooManyRequests => "ProviderRateLimited",
                _ when (int)response.StatusCode >= 500 => "ProviderServerError",
                _ => "ProviderConfigError"
            };

            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => 
                    "The API key is invalid. Verify your Anthropic API key.",
                HttpStatusCode.TooManyRequests => 
                    "Rate limit exceeded. Please wait a moment and try again.",
                _ when (int)response.StatusCode >= 500 => 
                    "Anthropic server error. This is a temporary issue on Anthropic's side.",
                _ => 
                    $"Anthropic returned an error (HTTP {(int)response.StatusCode})."
            };

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                HttpStatus = ((int)response.StatusCode).ToString(),
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
        catch (HttpRequestException ex)
        {
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "Network error pinging Anthropic");

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = "ProviderNetworkError",
                Message = "Cannot reach Anthropic. Check your internet connection.",
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
        catch (TaskCanceledException ex)
        {
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "Timeout pinging Anthropic");

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = "ProviderNetworkError",
                Message = $"Connection timed out after {TimeoutSeconds} seconds.",
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
        catch (Exception ex)
        {
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Unexpected error pinging Anthropic");

            return new CoreProviderPingResult
            {
                Attempted = true,
                Success = false,
                ErrorCode = "ProviderConfigError",
                Message = $"Unexpected error: {ex.Message}",
                Endpoint = endpoint,
                ResponseTimeMs = responseTime
            };
        }
    }
}
