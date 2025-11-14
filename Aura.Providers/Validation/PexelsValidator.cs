using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Validates Pexels API connectivity by checking collections endpoint
/// </summary>
public class PexelsValidator : IProviderValidator
{
    private readonly ILogger<PexelsValidator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Pexels";

    public PexelsValidator(ILogger<PexelsValidator> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("[{CorrelationId}] Pexels validation failed: API key not configured", correlationId);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key not configured",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            // Trim whitespace from API key (common copy-paste issue)
            apiKey = apiKey.Trim();

            // Basic format validation
            if (apiKey.Length < 20)
            {
                _logger.LogWarning("[{CorrelationId}] Pexels validation failed: API key too short (expected at least 20 characters, got {Length} characters)", 
                    correlationId, apiKey.Length);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key format invalid (should be at least 20 characters)",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            _logger.LogInformation("[{CorrelationId}] Validating Pexels API key (key ending: ...{KeySuffix})", 
                correlationId, apiKey.Substring(Math.Max(0, apiKey.Length - 4)));

            // Test the API key by fetching a simple curated photos endpoint
            // This endpoint requires authentication and has minimal quota impact
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pexels.com/v1/curated?per_page=1");
            // Pexels uses the API key directly in the Authorization header (not Bearer)
            request.Headers.Add("Authorization", apiKey);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (cts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Timeout occurred
                sw.Stop();
                _logger.LogWarning("[{CorrelationId}] Pexels validation timed out after 15 seconds", correlationId);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Request timed out - Pexels API may be slow or unreachable",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[{CorrelationId}] Pexels validation successful (response length: {Length} bytes, elapsed: {ElapsedMs}ms)", 
                    correlationId, responseBody.Length, sw.ElapsedMilliseconds);
                
                // Extract rate limit info from headers if available
                var rateLimitRemaining = response.Headers.Contains("X-Ratelimit-Remaining") 
                    ? response.Headers.GetValues("X-Ratelimit-Remaining").FirstOrDefault() 
                    : "unknown";
                
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = true,
                    Details = $"Connected successfully (rate limit remaining: {rateLimitRemaining})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else if ((int)response.StatusCode == 401)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[{CorrelationId}] Pexels validation failed: HTTP 401 Unauthorized - API key is invalid (error: {Error})", 
                    correlationId, TruncateErrorBody(errorBody));
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key is invalid - please verify you copied it correctly from Pexels API page",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else if ((int)response.StatusCode == 403)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[{CorrelationId}] Pexels validation failed: HTTP 403 Forbidden - API key valid but account has no access (error: {Error})", 
                    correlationId, TruncateErrorBody(errorBody));
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key valid but account has no access - check your Pexels account status",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else if ((int)response.StatusCode == 429)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[{CorrelationId}] Pexels validation failed: HTTP 429 Too Many Requests - rate limit exceeded (error: {Error})", 
                    correlationId, TruncateErrorBody(errorBody));
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Rate limit exceeded - Pexels free tier allows 200 requests/hour",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[{CorrelationId}] Pexels validation failed: HTTP {StatusCode} (error: {Error})", 
                    correlationId, response.StatusCode, TruncateErrorBody(errorBody));
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"Unexpected error: HTTP {response.StatusCode} - {response.ReasonPhrase}",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("[{CorrelationId}] Pexels validation cancelled by user", correlationId);
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = "Validation cancelled",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[{CorrelationId}] Pexels validation failed: Network error - {Message}", correlationId, ex.Message);
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = "Could not reach Pexels API - check your internet connection",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[{CorrelationId}] Pexels validation failed: Unexpected error - {Message}", correlationId, ex.Message);
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Unexpected error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Truncate error body to 100 characters for logging
    /// </summary>
    private static string TruncateErrorBody(string errorBody)
    {
        if (string.IsNullOrEmpty(errorBody))
        {
            return string.Empty;
        }
        
        return errorBody.Length > 100 ? string.Concat(errorBody.AsSpan(0, 100), "...") : errorBody;
    }
}
