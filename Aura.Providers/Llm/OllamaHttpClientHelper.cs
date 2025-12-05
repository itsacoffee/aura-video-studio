using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// Utility class for configuring HttpClient timeouts for Ollama providers.
/// Ensures HttpClient timeout is always properly synchronized with provider timeouts.
/// </summary>
internal static class OllamaHttpClientHelper
{
    /// <summary>
    /// The buffer (in seconds) to add on top of the provider timeout for HttpClient.
    /// This gives extra margin for network latency, connection setup, etc.
    /// </summary>
    public const int TimeoutBufferSeconds = 300; // 5 minutes

    /// <summary>
    /// Ensures the HttpClient timeout is properly configured for Ollama's long-running requests.
    /// Handles shared HttpClient instances safely by creating a new instance if modification fails.
    /// </summary>
    /// <param name="httpClient">The HttpClient to configure</param>
    /// <param name="timeoutSeconds">The provider timeout in seconds</param>
    /// <param name="logger">Logger for diagnostic messages</param>
    /// <returns>A properly configured HttpClient (may be the same instance or a new one)</returns>
    public static HttpClient EnsureProperTimeout(
        HttpClient httpClient,
        int timeoutSeconds,
        ILogger logger)
    {
        var requiredTimeout = TimeSpan.FromSeconds(timeoutSeconds + TimeoutBufferSeconds);

        // If HttpClient has infinite timeout, it's already configured for long-running requests
        if (httpClient.Timeout == Timeout.InfiniteTimeSpan)
        {
            return httpClient;
        }

        // If HttpClient timeout is already sufficient, no changes needed
        if (httpClient.Timeout >= requiredTimeout)
        {
            return httpClient;
        }

        // Log warning that HttpClient timeout needs to be increased
        // This indicates the HttpClient should be configured properly in DI registration
        logger.LogWarning(
            "HttpClient timeout ({HttpClientTimeout}s) is shorter than required ({RequiredTimeout}s). " +
            "Adjusting timeout. For best results, configure HttpClient timeout in DI registration.",
            httpClient.Timeout.TotalSeconds, requiredTimeout.TotalSeconds);

        // Try to modify the existing HttpClient's timeout
        try
        {
            httpClient.Timeout = requiredTimeout;
            logger.LogInformation(
                "Configured HttpClient timeout to {Timeout}s for Ollama provider",
                requiredTimeout.TotalSeconds);
            return httpClient;
        }
        catch (InvalidOperationException)
        {
            // HttpClient is already in use (e.g., shared instance), create a new one
            logger.LogWarning(
                "HttpClient already in use, creating new instance with timeout {Timeout}s",
                requiredTimeout.TotalSeconds);

            var newClient = new HttpClient
            {
                Timeout = requiredTimeout,
                BaseAddress = httpClient.BaseAddress
            };

            // Copy default request headers from the original client
            foreach (var header in httpClient.DefaultRequestHeaders)
            {
                newClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    header.Key, header.Value);
            }

            return newClient;
        }
    }
}
