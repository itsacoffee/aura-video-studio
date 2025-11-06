using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Validates OpenAI API connectivity by attempting a minimal token echo completion
/// </summary>
public class OpenAiValidator : IProviderValidator
{
    private readonly ILogger<OpenAiValidator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "OpenAI";

    public OpenAiValidator(ILogger<OpenAiValidator> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "API key not configured",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            // Make a minimal 1-token echo completion to test the API key
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                },
                max_tokens = 1
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(request, cts.Token);

            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OpenAI validation successful");
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
                _logger.LogWarning("OpenAI validation failed: Invalid API key");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Invalid API key",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenAI validation failed: HTTP {StatusCode}", response.StatusCode);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"HTTP {response.StatusCode}: {GetErrorMessage(errorContent)}",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("OpenAI validation timed out");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = "Request timed out",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "OpenAI validation failed: Network error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Network error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "OpenAI validation failed: Unexpected error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }

    private string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length <= 8)
        {
            return "***";
        }
        return key.Substring(0, 8) + "...";
    }

    private string GetErrorMessage(string errorContent)
    {
        try
        {
            var doc = JsonDocument.Parse(errorContent);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Unknown error";
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return "API error";
    }
}
