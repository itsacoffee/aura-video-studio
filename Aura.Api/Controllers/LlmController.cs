using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for LLM operations including testing, health checks, and connectivity verification
/// </summary>
[ApiController]
[Route("api/llm")]
[Produces("application/json")]
public class LlmController : ControllerBase
{
    private readonly ILogger<LlmController> _logger;
    private readonly ILlmProvider _llmProvider;

    public LlmController(
        ILogger<LlmController> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Test LLM connectivity with a simple prompt
    /// Returns response time, model info, and a simple generated response
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(LlmTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<LlmTestResponse>> TestLlmConnectivity(
        [FromBody] LlmTestRequest? request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation(
            "Testing LLM connectivity, CorrelationId: {CorrelationId}",
            correlationId);

        // Default test prompt if none provided
        var testPrompt = request?.Prompt ?? "Say 'Hello, I am working correctly!' in exactly those words.";
        var timeoutSeconds = request?.TimeoutSeconds ?? 30;

        try
        {
            // Get provider capabilities for model info
            var capabilities = _llmProvider.GetCapabilities();
            
            _logger.LogInformation(
                "Testing LLM provider: {Provider}, CorrelationId: {CorrelationId}",
                capabilities.ProviderName,
                correlationId);

            // Create a timeout for the test
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Measure response time
            var stopwatch = Stopwatch.StartNew();
            
            string response;
            try
            {
                response = await _llmProvider.CompleteAsync(testPrompt, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "LLM test timed out after {Timeout}s, CorrelationId: {CorrelationId}",
                    timeoutSeconds, correlationId);

                return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
                {
                    Type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#LLM_TIMEOUT",
                    Title = "LLM Test Timeout",
                    Status = StatusCodes.Status504GatewayTimeout,
                    Detail = $"LLM test timed out after {timeoutSeconds} seconds. The provider may be slow or unresponsive.",
                    Extensions =
                    {
                        ["correlationId"] = correlationId,
                        ["errorCode"] = "LLM_TIMEOUT",
                        ["timeoutSeconds"] = timeoutSeconds,
                        ["providerName"] = capabilities.ProviderName
                    }
                });
            }

            stopwatch.Stop();
            var responseTimeMs = stopwatch.ElapsedMilliseconds;

            // Estimate token usage (rough approximation)
            var inputTokens = EstimateTokens(testPrompt);
            var outputTokens = EstimateTokens(response);

            _logger.LogInformation(
                "LLM test completed: Provider={Provider}, ResponseTime={ResponseTime}ms, " +
                "InputTokens={InputTokens}, OutputTokens={OutputTokens}, CorrelationId: {CorrelationId}",
                capabilities.ProviderName,
                responseTimeMs,
                inputTokens,
                outputTokens,
                correlationId);

            return Ok(new LlmTestResponse(
                Success: true,
                ProviderName: capabilities.ProviderName,
                // Note: ModelName uses ProviderName as the ProviderCapabilities interface doesn't expose model info.
                // This would require extending the ILlmProvider interface to include model metadata.
                ModelName: capabilities.ProviderName,
                ResponseTimeMs: responseTimeMs,
                GeneratedText: response,
                InputTokens: inputTokens,
                OutputTokens: outputTokens,
                TotalTokens: inputTokens + outputTokens,
                IsLocalModel: capabilities.IsLocalModel,
                SupportsStreaming: capabilities.SupportsStreaming,
                Message: "LLM connectivity test successful",
                CorrelationId: correlationId
            ));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "LLM test was cancelled, CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(499, new ProblemDetails
            {
                Title = "Request Cancelled",
                Status = 499,
                Detail = "The LLM test request was cancelled.",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LLM test failed, CorrelationId: {CorrelationId}",
                correlationId);

            // Try to get provider info even if the test failed
            string providerName = "Unknown";
            try
            {
                providerName = _llmProvider.GetCapabilities().ProviderName;
            }
            catch
            {
                // Ignore - we'll use "Unknown"
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#LLM_UNAVAILABLE",
                Title = "LLM Service Unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = $"Failed to connect to LLM provider: {ex.Message}",
                Extensions =
                {
                    ["correlationId"] = correlationId,
                    ["errorCode"] = "LLM_UNAVAILABLE",
                    ["providerName"] = providerName,
                    ["suggestion"] = GetSuggestionForError(ex)
                }
            });
        }
    }

    /// <summary>
    /// Get LLM provider health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(LlmHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<LlmHealthResponse> GetLlmHealth()
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var capabilities = _llmProvider.GetCapabilities();

            return Ok(new LlmHealthResponse(
                IsAvailable: true,
                ProviderName: capabilities.ProviderName,
                // Note: DefaultModel uses ProviderName as the ProviderCapabilities interface doesn't expose model info.
                DefaultModel: capabilities.ProviderName,
                IsLocalModel: capabilities.IsLocalModel,
                SupportsStreaming: capabilities.SupportsStreaming,
                SupportsTranslation: capabilities.SupportsTranslation,
                MaxContextLength: capabilities.MaxContextLength,
                KnownLimitations: capabilities.KnownLimitations,
                ErrorMessage: null,
                CorrelationId: correlationId
            ));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to get LLM health status, CorrelationId: {CorrelationId}",
                correlationId);

            return Ok(new LlmHealthResponse(
                IsAvailable: false,
                ProviderName: "Unknown",
                DefaultModel: null,
                IsLocalModel: false,
                SupportsStreaming: false,
                SupportsTranslation: false,
                MaxContextLength: null,
                KnownLimitations: null,
                ErrorMessage: ex.Message,
                CorrelationId: correlationId
            ));
        }
    }

    /// <summary>
    /// Get available LLM models from the current provider
    /// </summary>
    [HttpGet("models")]
    [ProducesResponseType(typeof(LlmModelsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<LlmModelsResponse> GetAvailableModels()
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var capabilities = _llmProvider.GetCapabilities();

            // Note: Using provider name as model name since ProviderCapabilities doesn't expose model info.
            // This would require extending the ILlmProvider interface to include model discovery.
            var models = new List<string> { capabilities.ProviderName };

            return Ok(new LlmModelsResponse(
                ProviderName: capabilities.ProviderName,
                Models: models,
                DefaultModel: capabilities.ProviderName,
                IsLocalProvider: capabilities.IsLocalModel,
                CorrelationId: correlationId
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get available LLM models, CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#LLM_MODELS_ERROR",
                Title = "Failed to Get LLM Models",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = $"Could not retrieve available models: {ex.Message}",
                Extensions =
                {
                    ["correlationId"] = correlationId,
                    ["errorCode"] = "LLM_MODELS_ERROR"
                }
            });
        }
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // Rough approximation: ~4 characters per token for English text
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    private static string GetSuggestionForError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();

        if (message.Contains("ollama") || message.Contains("connection refused") || message.Contains("localhost"))
        {
            return "Ensure Ollama is running: 'ollama serve' and verify models are installed: 'ollama list'";
        }

        if (message.Contains("api key") || message.Contains("unauthorized") || message.Contains("401"))
        {
            return "Check your API key in Settings. Ensure it is valid and has not expired.";
        }

        if (message.Contains("rate limit") || message.Contains("429"))
        {
            return "You have hit the rate limit. Wait a few minutes and try again.";
        }

        if (message.Contains("timeout") || message.Contains("timed out"))
        {
            return "The request timed out. Try again or check your network connection.";
        }

        return "Check your LLM provider configuration in Settings.";
    }
}
