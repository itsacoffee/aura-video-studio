using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware that extends request timeout for endpoints that involve LLM operations.
/// These operations (especially with local Ollama models) can take several minutes to complete.
/// </summary>
public class LlmRequestTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LlmRequestTimeoutMiddleware> _logger;

    /// <summary>
    /// Endpoints that may involve long-running LLM operations
    /// </summary>
    private static readonly string[] LongRunningEndpoints = new[]
    {
        "/api/ideation/brainstorm",
        "/api/ideation/expand-brief",
        "/api/ideation/research",
        "/api/ideation/storyboard",
        "/api/ideation/refine",
        "/api/ideation/gap-analysis",
        "/api/ideation/questions",
        "/api/ideation/idea-to-brief",
        "/api/ideation/enhance-topic",
        "/api/ideation/analyze-prompt-quality",
        "/api/script",
        "/api/jobs"
    };

    private readonly TimeSpan _llmTimeout = TimeSpan.FromMinutes(15);

    public LlmRequestTimeoutMiddleware(RequestDelegate next, ILogger<LlmRequestTimeoutMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        if (LongRunningEndpoints.Any(e => path.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            // Create a new CancellationTokenSource with extended timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            cts.CancelAfter(_llmTimeout);

            // Store the original token for later comparison
            var originalToken = context.RequestAborted;

            // Replace the RequestAborted token with our extended one
            // Note: We need to use HttpContext.Features to properly override the token
            var originalCancellationFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature>();
            context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature>(
                new ExtendedTimeoutLifetimeFeature(cts.Token));

            _logger.LogDebug("Extended timeout to {Timeout} for LLM endpoint: {Path}",
                _llmTimeout, path);

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !originalToken.IsCancellationRequested)
            {
                _logger.LogWarning("LLM request timed out after {Timeout}: {Path}", _llmTimeout, path);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 504; // Gateway Timeout
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Request timed out. The AI model took too long to respond.",
                        suggestions = new[]
                        {
                            "Try a simpler topic",
                            "If using Ollama, ensure you have a fast model loaded",
                            "Check that your GPU is being utilized properly",
                            "Consider using a smaller model for faster responses"
                        }
                    }).ConfigureAwait(false);
                }
            }
            finally
            {
                // Restore original feature if needed
                if (originalCancellationFeature != null)
                {
                    context.Features.Set(originalCancellationFeature);
                }
            }
        }
        else
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Custom lifetime feature that provides the extended timeout token
    /// </summary>
    private sealed class ExtendedTimeoutLifetimeFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature
    {
        public ExtendedTimeoutLifetimeFeature(CancellationToken token)
        {
            RequestAborted = token;
        }

        public CancellationToken RequestAborted { get; set; }

        public void Abort()
        {
            // No-op since we're using a linked token
        }
    }
}
