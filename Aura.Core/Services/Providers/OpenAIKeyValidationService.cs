using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service for validating OpenAI API keys with live network verification
/// </summary>
public class OpenAIKeyValidationService
{
    private readonly ILogger<OpenAIKeyValidationService> _logger;
    private readonly HttpClient _httpClient;
    
    private const int ConnectTimeoutSeconds = 5;
    private const int TotalTimeoutSeconds = 10;
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
        if (!(apiKey.StartsWith("sk-", StringComparison.Ordinal) && apiKey.Length >= 20))
        {
            return (false, "Invalid OpenAI API key format. Must start with 'sk-' or 'sk-proj-' and be at least 20 characters.");
        }

        return (true, "Format looks correct; verifying with OpenAI…");
    }

    /// <summary>
    /// Validate OpenAI API key with live network verification
    /// Uses GET /v1/models endpoint for low-cost validation without token usage
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
            "Validating OpenAI API key (masked: {MaskedKey}), CorrelationId: {CorrelationId}",
            maskedKey,
            correlationId ?? "none");

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
                ResponseTimeMs = 0
            };
        }

        // Perform live validation
        try
        {
            var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl;
            var requestUri = $"{effectiveBaseUrl.TrimEnd('/')}/v1/models";

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
                response = await _httpClient.SendAsync(request, cts.Token);
            }
            catch (TaskCanceledException ex)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("OpenAI validation cancelled by user, Key: {MaskedKey}", maskedKey);
                    return new OpenAIValidationResult
                    {
                        IsValid = false,
                        Status = "Cancelled",
                        Message = "Validation cancelled",
                        FormatValid = true,
                        NetworkCheckPassed = false,
                        ResponseTimeMs = (long)elapsed
                    };
                }

                _logger.LogWarning(
                    "OpenAI validation timeout after {Timeout}s, Key: {MaskedKey}",
                    TotalTimeoutSeconds,
                    maskedKey);
                
                return new OpenAIValidationResult
                {
                    IsValid = false,
                    Status = "Timeout",
                    Message = $"Request timed out after {TotalTimeoutSeconds} seconds. Please check your internet connection.",
                    FormatValid = true,
                    NetworkCheckPassed = false,
                    HttpStatusCode = null,
                    ErrorType = "Timeout",
                    ResponseTimeMs = (long)elapsed
                };
            }
            catch (HttpRequestException ex)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogWarning(
                    ex,
                    "Network error during OpenAI validation, Key: {MaskedKey}",
                    maskedKey);
                
                return new OpenAIValidationResult
                {
                    IsValid = false,
                    Status = "NetworkError",
                    Message = "Network error while contacting OpenAI. Please check your internet connection and try again.",
                    FormatValid = true,
                    NetworkCheckPassed = false,
                    HttpStatusCode = null,
                    ErrorType = "NetworkError",
                    ResponseTimeMs = (long)elapsed
                };
            }

            var elapsed2 = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = (int)response.StatusCode;

            // Success case
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "OpenAI API key validated successfully in {ElapsedMs}ms, Key: {MaskedKey}",
                    elapsed2,
                    maskedKey);
                
                return new OpenAIValidationResult
                {
                    IsValid = true,
                    Status = "Valid",
                    Message = "API key is valid and verified with OpenAI.",
                    FormatValid = true,
                    NetworkCheckPassed = true,
                    HttpStatusCode = statusCode,
                    ResponseTimeMs = (long)elapsed2
                };
            }

            // Read error response for detailed messages
            string errorBody = string.Empty;
            string errorMessage = string.Empty;
            
            try
            {
                errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                
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
                _logger.LogWarning(ex, "Failed to parse error response from OpenAI");
            }

            // Handle specific error codes
            if (response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                var message = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "Invalid API key or authentication failed."
                    : "API key is valid but lacks required permissions or project scope.";

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    message = errorMessage;
                }

                _logger.LogWarning(
                    "OpenAI API key validation failed: {StatusCode}, Key: {MaskedKey}, Error: {ErrorMessage}",
                    response.StatusCode,
                    maskedKey,
                    errorMessage);

                return new OpenAIValidationResult
                {
                    IsValid = false,
                    Status = response.StatusCode == HttpStatusCode.Unauthorized ? "Unauthorized" : "Forbidden",
                    Message = message,
                    FormatValid = true,
                    NetworkCheckPassed = true,
                    HttpStatusCode = statusCode,
                    ErrorType = response.StatusCode.ToString(),
                    ResponseTimeMs = (long)elapsed2
                };
            }

            // Other HTTP errors
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
                ResponseTimeMs = (long)elapsed2
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
                Message = "An unexpected error occurred while validating the API key.",
                FormatValid = true,
                NetworkCheckPassed = false,
                ErrorType = "UnexpectedError",
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
}
