using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Validates ElevenLabs API connectivity by listing voices
/// </summary>
public class ElevenLabsValidator : IProviderValidator
{
    private readonly ILogger<ElevenLabsValidator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "ElevenLabs";

    public ElevenLabsValidator(ILogger<ElevenLabsValidator> logger, HttpClient httpClient)
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
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation failed: API key not configured", correlationId);
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

            // Validate API key format (32 hex characters)
            if (apiKey.Length != 32 || !System.Text.RegularExpressions.Regex.IsMatch(apiKey, "^[a-fA-F0-9]{32}$"))
            {
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation failed: Invalid API key format (expected 32 hex characters, got {Length} characters)", 
                    correlationId, apiKey.Length);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key format invalid (expected 32 hexadecimal characters)",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            _logger.LogInformation("[{CorrelationId}] Validating ElevenLabs API key (key ending: ...{KeySuffix})", 
                correlationId, apiKey.Substring(Math.Max(0, apiKey.Length - 4)));

            // Try to get user info first (more reliable than voices endpoint)
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/user");
            request.Headers.Add("xi-api-key", apiKey);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Increased timeout from 10s to 30s

            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.SendAsync(request, cts.Token);
            }
            catch (TaskCanceledException) when (cts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Timeout occurred
                sw.Stop();
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation timed out after 30 seconds", correlationId);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Request timed out - ElevenLabs API may be slow or unreachable",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("[{CorrelationId}] ElevenLabs validation successful (response length: {Length} bytes, elapsed: {ElapsedMs}ms)", 
                    correlationId, responseBody.Length, sw.ElapsedMilliseconds);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = true,
                    Details = "Connected successfully",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else if ((int)response.StatusCode == 401)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation failed: HTTP 401 Unauthorized - API key is invalid (error: {Error})", 
                    correlationId, errorBody.Length > 100 ? errorBody.Substring(0, 100) + "..." : errorBody);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key is invalid - please verify you copied it correctly from ElevenLabs settings",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else if ((int)response.StatusCode == 403)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation failed: HTTP 403 Forbidden - API key valid but account has no access (error: {Error})", 
                    correlationId, errorBody.Length > 100 ? errorBody.Substring(0, 100) + "..." : errorBody);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key valid but account has no access - check your ElevenLabs subscription",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else if ((int)response.StatusCode == 429)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation failed: HTTP 429 Too Many Requests - rate limit exceeded (error: {Error})", 
                    correlationId, errorBody.Length > 100 ? errorBody.Substring(0, 100) + "..." : errorBody);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Rate limit exceeded - please wait a moment and try again",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[{CorrelationId}] ElevenLabs validation failed: HTTP {StatusCode} (error: {Error})", 
                    correlationId, response.StatusCode, errorBody.Length > 100 ? errorBody.Substring(0, 100) + "..." : errorBody);
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
            _logger.LogWarning("[{CorrelationId}] ElevenLabs validation cancelled by user", correlationId);
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
            _logger.LogWarning(ex, "[{CorrelationId}] ElevenLabs validation failed: Network error - {Message}", correlationId, ex.Message);
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = "Could not reach ElevenLabs API - check your internet connection",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ElevenLabs validation failed: Unexpected error - {Message}", correlationId, ex.Message);
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Unexpected error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }
}
