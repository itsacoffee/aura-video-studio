using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service for validating OpenAI API keys with live network verification
/// Implements patience-centric validation with extended timeouts, proxy support, and comprehensive error categorization
/// </summary>
public class OpenAIKeyValidationService
{
    private readonly ILogger<OpenAIKeyValidationService> _logger;
    private readonly HttpClient _httpClient;
    
    private const int TotalTimeoutSeconds = 90;
    private const int MaxRetryAttempts = 2;
    private const int InitialRetryDelayMs = 1000;
    private const string DefaultBaseUrl = "https://api.openai.com";

    public OpenAIKeyValidationService(
        ILogger<OpenAIKeyValidationService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Validate OpenAI API key format (preliminary check)
    /// </summary>
    public (bool IsValid, string Message) ValidateKeyFormat(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API key is required");
        }

        // OpenAI keys can start with:
        // - "sk-" for regular keys
        // - "sk-proj-" for project-scoped keys
        // - "sk-live-" for live environment keys
        // Must be at least 20 characters long
        var hasValidPrefix = apiKey.StartsWith("sk-", StringComparison.Ordinal) ||
                            apiKey.StartsWith("sk-proj-", StringComparison.Ordinal) ||
                            apiKey.StartsWith("sk-live-", StringComparison.Ordinal);
        
        if (!hasValidPrefix || apiKey.Length < 20)
        {
            return (false, "Invalid OpenAI API key format. Must start with 'sk-', 'sk-proj-', or 'sk-live-' and be at least 20 characters.");
        }

        return (true, "Format looks correct; verifying with OpenAI…");
    }

    /// <summary>
    /// Validate OpenAI API key with live network verification
    /// Uses GET /v1/models endpoint for low-cost validation without token usage
    /// Implements retry logic for transient failures with extended timeout
    /// </summary>
    public async Task<OpenAIValidationResult> ValidateKeyAsync(
        string apiKey,
        string? baseUrl = null,
        string? organizationId = null,
        string? projectId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var maskedKey = MaskApiKey(apiKey);
        
        _logger.LogInformation(
            "Validating OpenAI API key (masked: {MaskedKey}), CorrelationId: {CorrelationId}, Timeout: {Timeout}s",
            maskedKey,
            correlationId ?? "none",
            TotalTimeoutSeconds);

        // First check format
        var (formatValid, formatMessage) = ValidateKeyFormat(apiKey);
        if (!formatValid)
        {
            return new OpenAIValidationResult
            {
                IsValid = false,
                Status = "Invalid",
                Message = formatMessage,
                FormatValid = false,
                NetworkCheckPassed = false,
                ResponseTimeMs = 0,
                DiagnosticInfo = "Format validation failed"
            };
        }

        // Check for offline mode (basic connectivity check)
        if (!await IsNetworkAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("Network appears to be offline, Key: {MaskedKey}", maskedKey);
            return new OpenAIValidationResult
            {
                IsValid = false,
                Status = "Offline",
                Message = "No internet connection detected. You can continue and validation will be deferred until online.",
                FormatValid = true,
                NetworkCheckPassed = false,
                ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                ErrorType = "Offline",
                DiagnosticInfo = "Network connectivity check failed"
            };
        }

        // Perform live validation with retry logic
        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl;
        var requestUri = $"{effectiveBaseUrl.TrimEnd('/')}/v1/models";

        for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var delay = InitialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogInformation(
                        "Retry attempt {Attempt}/{MaxAttempts} after {Delay}ms, Key: {MaskedKey}",
                        attempt,
                        MaxRetryAttempts,
                        delay,
                        maskedKey);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogDebug(
                    "Sending OpenAI validation request (attempt {Attempt}/{Total}) to {RequestUri} with key {MaskedKey}",
                    attempt + 1,
                    MaxRetryAttempts + 1,
                    requestUri,
                    maskedKey);

                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Add optional headers for sk-proj keys
                if (!string.IsNullOrWhiteSpace(organizationId))
                {
                    request.Headers.Add("OpenAI-Organization", organizationId);
                }
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    request.Headers.Add("OpenAI-Project", projectId);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(TotalTimeoutSeconds));

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("OpenAI validation cancelled by user, Key: {MaskedKey}", maskedKey);
                        return new OpenAIValidationResult
                        {
                            IsValid = false,
                            Status = "Cancelled",
                            Message = "Validation cancelled by user",
                            FormatValid = true,
                            NetworkCheckPassed = false,
                            ResponseTimeMs = (long)elapsed,
                            ErrorType = "Cancelled",
                            DiagnosticInfo = $"Cancelled after {elapsed:F0}ms"
                        };
                    }

                    if (attempt < MaxRetryAttempts)
                    {
                        _logger.LogWarning(
                            "OpenAI validation timeout on attempt {Attempt}, will retry, Key: {MaskedKey}",
                            attempt + 1,
                            maskedKey);
                        continue;
                    }

                    _logger.LogWarning(
                        "OpenAI validation timeout after {Timeout}s and {Attempts} attempts, Key: {MaskedKey}",
                        TotalTimeoutSeconds,
                        attempt + 1,
                        maskedKey);
                    
                    return new OpenAIValidationResult
                    {
                        IsValid = false,
                        Status = "Timeout",
                        Message = $"Request timed out after {TotalTimeoutSeconds} seconds. You can continue anyway, and the key will be validated on first use.",
                        FormatValid = true,
                        NetworkCheckPassed = false,
                        HttpStatusCode = null,
                        ErrorType = "Timeout",
                        ResponseTimeMs = (long)elapsed,
                        DiagnosticInfo = $"Timeout after {attempt + 1} attempts, total {elapsed:F0}ms"
                    };
                }
                catch (HttpRequestException ex)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    var diagnosticInfo = CategorizeNetworkError(ex);
                    
                    if (attempt < MaxRetryAttempts && IsRetriableNetworkError(ex))
                    {
                        _logger.LogWarning(
                            ex,
                            "Retriable network error on attempt {Attempt}, will retry, Key: {MaskedKey}",
                            attempt + 1,
                            maskedKey);
                        continue;
                    }

                    _logger.LogWarning(
                        ex,
                        "Network error during OpenAI validation after {Attempts} attempts, Key: {MaskedKey}",
                        attempt + 1,
                        maskedKey);
                    
                    var (errorType, userMessage) = GetNetworkErrorDetails(ex);
                    
                    return new OpenAIValidationResult
                    {
                        IsValid = false,
                        Status = "NetworkError",
                        Message = userMessage,
                        FormatValid = true,
                        NetworkCheckPassed = false,
                        HttpStatusCode = null,
                        ErrorType = errorType,
                        ResponseTimeMs = (long)elapsed,
                        DiagnosticInfo = diagnosticInfo
                    };
                }

                var elapsed2 = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var statusCode = (int)response.StatusCode;

                // Success case
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "OpenAI API key validated successfully in {ElapsedMs}ms after {Attempts} attempts, Key: {MaskedKey}",
                        elapsed2,
                        attempt + 1,
                        maskedKey);
                    
                    return new OpenAIValidationResult
                    {
                        IsValid = true,
                        Status = "Valid",
                        Message = "API key is valid and verified with OpenAI.",
                        FormatValid = true,
                        NetworkCheckPassed = true,
                        HttpStatusCode = statusCode,
                        ResponseTimeMs = (long)elapsed2,
                        DiagnosticInfo = $"Validated successfully after {attempt + 1} attempts"
                    };
                }

                // Read error response for detailed messages
                string errorBody = string.Empty;
                string errorMessage = string.Empty;
                
                try
                {
                    errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    
                    _logger.LogDebug(
                        "OpenAI API response body: {ResponseBody}, Status: {StatusCode}, Key: {MaskedKey}",
                        errorBody,
                        response.StatusCode,
                        maskedKey);
                    
                    using var jsonDoc = JsonDocument.Parse(errorBody);
                    if (jsonDoc.RootElement.TryGetProperty("error", out var errorObj))
                    {
                        if (errorObj.TryGetProperty("message", out var msgProp))
                        {
                            errorMessage = msgProp.GetString() ?? string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse error response from OpenAI, Raw body: {ErrorBody}", errorBody);
                }

                // Handle specific error codes
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var message = !string.IsNullOrEmpty(errorMessage)
                        ? errorMessage
                        : "Invalid API key. Please check the value and try again.";

                    _logger.LogWarning(
                        "OpenAI API key validation failed: Unauthorized, Key: {MaskedKey}, Error: {ErrorMessage}",
                        maskedKey,
                        errorMessage);

                    return new OpenAIValidationResult
                    {
                        IsValid = false,
                        Status = "Invalid",
                        Message = message,
                        FormatValid = true,
                        NetworkCheckPassed = true,
                        HttpStatusCode = statusCode,
                        ErrorType = "Unauthorized",
                        ResponseTimeMs = (long)elapsed2,
                        DiagnosticInfo = $"HTTP 401 after {attempt + 1} attempts"
                    };
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    var message = !string.IsNullOrEmpty(errorMessage)
                        ? errorMessage
                        : "Access denied. Check organization/project permissions or billing.";

                    _logger.LogWarning(
                        "OpenAI API key validation failed: Forbidden, Key: {MaskedKey}, Error: {ErrorMessage}",
                        maskedKey,
                        errorMessage);

                    return new OpenAIValidationResult
                    {
                        IsValid = false,
                        Status = "PermissionDenied",
                        Message = message,
                        FormatValid = true,
                        NetworkCheckPassed = true,
                        HttpStatusCode = statusCode,
                        ErrorType = "Forbidden",
                        ResponseTimeMs = (long)elapsed2,
                        DiagnosticInfo = $"HTTP 403 after {attempt + 1} attempts"
                    };
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var message = !string.IsNullOrEmpty(errorMessage)
                        ? errorMessage
                        : "Rate limited. Your key is valid, but you've hit a limit. You can continue and try again later.";

                    _logger.LogInformation(
                        "OpenAI API key validation rate limited (key valid): Key: {MaskedKey}",
                        maskedKey);

                    return new OpenAIValidationResult
                    {
                        IsValid = true,
                        Status = "RateLimited",
                        Message = message,
                        FormatValid = true,
                        NetworkCheckPassed = true,
                        HttpStatusCode = statusCode,
                        ErrorType = "RateLimited",
                        ResponseTimeMs = (long)elapsed2,
                        DiagnosticInfo = $"HTTP 429 rate limit after {attempt + 1} attempts"
                    };
                }

                if ((int)response.StatusCode >= 500)
                {
                    if (attempt < MaxRetryAttempts)
                    {
                        _logger.LogWarning(
                            "OpenAI service error on attempt {Attempt}, will retry, Status: {StatusCode}, Key: {MaskedKey}",
                            attempt + 1,
                            response.StatusCode,
                            maskedKey);
                        continue;
                    }

                    var message = !string.IsNullOrEmpty(errorMessage)
                        ? $"OpenAI service issue: {errorMessage}. You can continue and try again later."
                        : "OpenAI service issue. Your key may be valid; you can continue anyway.";

                    _logger.LogWarning(
                        "OpenAI service error after {Attempts} attempts: {StatusCode}, Key: {MaskedKey}",
                        attempt + 1,
                        response.StatusCode,
                        maskedKey);

                    return new OpenAIValidationResult
                    {
                        IsValid = false,
                        Status = "ServiceIssue",
                        Message = message,
                        FormatValid = true,
                        NetworkCheckPassed = true,
                        HttpStatusCode = statusCode,
                        ErrorType = "ServiceError",
                        ResponseTimeMs = (long)elapsed2,
                        DiagnosticInfo = $"HTTP {statusCode} after {attempt + 1} attempts"
                    };
                }

                // Other HTTP errors - no retry for client errors
                _logger.LogWarning(
                    "OpenAI API returned unexpected status: {StatusCode}, Key: {MaskedKey}",
                    response.StatusCode,
                    maskedKey);

                return new OpenAIValidationResult
                {
                    IsValid = false,
                    Status = "Error",
                    Message = $"OpenAI API returned error: {response.StatusCode}. {errorMessage}",
                    FormatValid = true,
                    NetworkCheckPassed = true,
                    HttpStatusCode = statusCode,
                    ErrorType = response.StatusCode.ToString(),
                    ResponseTimeMs = (long)elapsed2,
                    DiagnosticInfo = $"HTTP {statusCode} after {attempt + 1} attempts"
                };
            }
            catch (Exception ex)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogError(
                    ex,
                    "Unexpected error during OpenAI key validation, Key: {MaskedKey}",
                    maskedKey);

                return new OpenAIValidationResult
                {
                    IsValid = false,
                    Status = "Error",
                    Message = "An unexpected error occurred while validating the API key. You can continue anyway.",
                    FormatValid = true,
                    NetworkCheckPassed = false,
                    ErrorType = "UnexpectedError",
                    ResponseTimeMs = (long)elapsed,
                    DiagnosticInfo = $"Exception: {ex.GetType().Name} - {ex.Message}"
                };
            }
        }

        // Should never reach here due to continue/return in loop, but add fallback
        var totalElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        return new OpenAIValidationResult
        {
            IsValid = false,
            Status = "Error",
            Message = "Validation failed after all retry attempts",
            FormatValid = true,
            NetworkCheckPassed = false,
            ErrorType = "MaxRetriesExceeded",
            ResponseTimeMs = (long)totalElapsed,
            DiagnosticInfo = $"All {MaxRetryAttempts + 1} attempts failed"
        };
    }

    /// <summary>
    /// Check if network is available with a basic connectivity test
    /// </summary>
    private async Task<bool> IsNetworkAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            var request = new HttpRequestMessage(HttpMethod.Head, "https://www.google.com");
            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Categorize network error for diagnostic purposes
    /// </summary>
    private string CategorizeNetworkError(HttpRequestException ex)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;
        
        if (message.Contains("ssl") || message.Contains("tls") || innerMessage.Contains("ssl") || innerMessage.Contains("tls"))
        {
            return "TLS/SSL error - certificate validation failed or TLS handshake issue";
        }
        if (message.Contains("dns") || message.Contains("nodename") || innerMessage.Contains("dns"))
        {
            return "DNS resolution error - unable to resolve hostname";
        }
        if (message.Contains("proxy") || innerMessage.Contains("proxy"))
        {
            return "Proxy error - check proxy configuration";
        }
        if (message.Contains("timeout") || innerMessage.Contains("timeout"))
        {
            return "Connection timeout - network may be slow or unreachable";
        }
        if (message.Contains("refused") || innerMessage.Contains("refused"))
        {
            return "Connection refused - target server rejected connection";
        }
        if (message.Contains("unreachable") || innerMessage.Contains("unreachable"))
        {
            return "Network unreachable - check internet connection";
        }
        
        return $"Network error: {ex.Message}";
    }

    /// <summary>
    /// Determine if a network error is retriable
    /// </summary>
    private bool IsRetriableNetworkError(HttpRequestException ex)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;
        
        // Retriable: timeouts, temporary connection issues
        if (message.Contains("timeout") || innerMessage.Contains("timeout"))
        {
            return true;
        }
        if (message.Contains("temporarily") || innerMessage.Contains("temporarily"))
        {
            return true;
        }
        
        // Not retriable: DNS, TLS, authentication issues
        if (message.Contains("dns") || message.Contains("ssl") || message.Contains("tls"))
        {
            return false;
        }
        
        // Default to retriable for unknown network errors
        return true;
    }

    /// <summary>
    /// Get user-friendly error message and error type from HttpRequestException
    /// </summary>
    private (string ErrorType, string UserMessage) GetNetworkErrorDetails(HttpRequestException ex)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;
        
        if (message.Contains("ssl") || message.Contains("tls") || innerMessage.Contains("ssl") || innerMessage.Contains("tls"))
        {
            return ("TLS_Error", "TLS/SSL connection error. This may be caused by proxy settings or certificate issues. You can continue anyway.");
        }
        if (message.Contains("dns") || message.Contains("nodename") || innerMessage.Contains("dns"))
        {
            return ("DNS_Error", "Unable to resolve api.openai.com. Check your internet connection or DNS settings. You can continue anyway.");
        }
        if (message.Contains("proxy") || innerMessage.Contains("proxy"))
        {
            return ("Proxy_Error", "Proxy connection error. Check your proxy settings (HTTP_PROXY environment variable or Windows proxy settings). You can continue anyway.");
        }
        if (message.Contains("timeout") || innerMessage.Contains("timeout"))
        {
            return ("Connection_Timeout", "Connection timed out. Your network may be slow. You can continue anyway, and the key will be validated on first use.");
        }
        if (message.Contains("refused") || innerMessage.Contains("refused"))
        {
            return ("Connection_Refused", "Connection refused. Check your firewall or network settings. You can continue anyway.");
        }
        if (message.Contains("unreachable") || innerMessage.Contains("unreachable"))
        {
            return ("Network_Unreachable", "Network unreachable. Check your internet connection. You can continue in offline mode.");
        }
        
        return ("Network_Error", "Network error while contacting OpenAI. Please check your internet connection. You can continue anyway.");
    }

    /// <summary>
    /// Get available models for the validated API key
    /// </summary>
    public async Task<OpenAIModelsResult> GetAvailableModelsAsync(
        string apiKey,
        string? baseUrl = null,
        string? organizationId = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var maskedKey = MaskApiKey(apiKey);
        
        _logger.LogInformation(
            "Fetching available models for OpenAI API key (masked: {MaskedKey})",
            maskedKey);

        try
        {
            var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl;
            var requestUri = $"{effectiveBaseUrl.TrimEnd('/')}/v1/models";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            if (!string.IsNullOrWhiteSpace(organizationId))
            {
                request.Headers.Add("OpenAI-Organization", organizationId);
            }
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                request.Headers.Add("OpenAI-Project", projectId);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TotalTimeoutSeconds));

            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch models: HTTP {StatusCode}, Key: {MaskedKey}",
                    response.StatusCode,
                    maskedKey);
                
                return new OpenAIModelsResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to fetch models: HTTP {response.StatusCode}"
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var models = new List<string>();
            
            if (jsonDoc.RootElement.TryGetProperty("data", out var dataArray))
            {
                foreach (var model in dataArray.EnumerateArray())
                {
                    if (model.TryGetProperty("id", out var idProp))
                    {
                        var modelId = idProp.GetString();
                        if (!string.IsNullOrEmpty(modelId))
                        {
                            models.Add(modelId);
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Fetched {Count} models successfully for key {MaskedKey}",
                models.Count,
                maskedKey);

            return new OpenAIModelsResult
            {
                Success = true,
                Models = models
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models for key {MaskedKey}", maskedKey);
            return new OpenAIModelsResult
            {
                Success = false,
                ErrorMessage = $"Error fetching models: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Test script generation with a simple prompt to verify the API key works for completions
    /// </summary>
    public async Task<OpenAITestGenerationResult> TestScriptGenerationAsync(
        string apiKey,
        string model = "gpt-4o-mini",
        string? baseUrl = null,
        string? organizationId = null,
        string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var maskedKey = MaskApiKey(apiKey);
        
        _logger.LogInformation(
            "Testing script generation with model {Model} for key {MaskedKey}",
            model,
            maskedKey);

        var startTime = DateTime.UtcNow;

        try
        {
            var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl;
            var requestUri = $"{effectiveBaseUrl.TrimEnd('/')}/v1/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = "Say hello" }
                },
                max_tokens = 10
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            if (!string.IsNullOrWhiteSpace(organizationId))
            {
                request.Headers.Add("OpenAI-Organization", organizationId);
            }
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                request.Headers.Add("OpenAI-Project", projectId);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TotalTimeoutSeconds));

            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                
                _logger.LogWarning(
                    "Script generation test failed: HTTP {StatusCode}, Key: {MaskedKey}",
                    response.StatusCode,
                    maskedKey);

                return new OpenAITestGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Generation test failed: HTTP {response.StatusCode}",
                    ResponseTimeMs = (long)elapsed
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            
            using var jsonDoc = JsonDocument.Parse(responseBody);
            string? generatedText = null;
            
            if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    generatedText = contentProp.GetString();
                }
            }

            _logger.LogInformation(
                "Script generation test successful in {ElapsedMs}ms, Key: {MaskedKey}",
                elapsed,
                maskedKey);

            return new OpenAITestGenerationResult
            {
                Success = true,
                GeneratedText = generatedText ?? string.Empty,
                Model = model,
                ResponseTimeMs = (long)elapsed
            };
        }
        catch (TaskCanceledException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "Script generation test timed out for key {MaskedKey}", maskedKey);
            
            return new OpenAITestGenerationResult
            {
                Success = false,
                ErrorMessage = "Request timed out",
                ResponseTimeMs = (long)elapsed
            };
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error testing script generation for key {MaskedKey}", maskedKey);
            
            return new OpenAITestGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error: {ex.Message}",
                ResponseTimeMs = (long)elapsed
            };
        }
    }

    /// <summary>
    /// Mask API key for logging (show first 6 and last 4 characters)
    /// </summary>
    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "[empty]";
        }

        if (apiKey.Length <= 10)
        {
            return new string('*', apiKey.Length);
        }

        var prefix = apiKey.Substring(0, Math.Min(6, apiKey.Length));
        var suffix = apiKey.Length > 4 ? apiKey.Substring(apiKey.Length - 4) : string.Empty;
        var maskedMiddle = new string('*', Math.Max(0, apiKey.Length - 10));

        return $"{prefix}{maskedMiddle}{suffix}";
    }
}

/// <summary>
/// Result of OpenAI API key validation
/// </summary>
public class OpenAIValidationResult
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool FormatValid { get; set; }
    public bool NetworkCheckPassed { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorType { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? DiagnosticInfo { get; set; }
}

/// <summary>
/// Result of fetching available models
/// </summary>
public class OpenAIModelsResult
{
    public bool Success { get; set; }
    public List<string> Models { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of testing script generation
/// </summary>
public class OpenAITestGenerationResult
{
    public bool Success { get; set; }
    public string? GeneratedText { get; set; }
    public string? Model { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}
