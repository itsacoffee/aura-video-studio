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
/// Validates Ollama connectivity by listing models and testing with a minimal 2-token completion
/// </summary>
public class OllamaValidator : IProviderValidator
{
    private readonly ILogger<OllamaValidator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Ollama";

    public OllamaValidator(ILogger<OllamaValidator> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var baseUrl = configUrl ?? "http://127.0.0.1:11434";

        try
        {
            // Step 1: Check if Ollama is running by listing models
            using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts1.CancelAfter(TimeSpan.FromSeconds(5));

            var listResponse = await _httpClient.GetAsync($"{baseUrl}/api/tags", cts1.Token);

            if (!listResponse.IsSuccessStatusCode)
            {
                sw.Stop();
                _logger.LogWarning("Ollama validation failed: GET /api/tags returned {StatusCode}", listResponse.StatusCode);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"Ollama not responding (HTTP {listResponse.StatusCode})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            var listJson = await listResponse.Content.ReadAsStringAsync(ct);
            var listDoc = JsonDocument.Parse(listJson);

            if (!listDoc.RootElement.TryGetProperty("models", out var models) || models.GetArrayLength() == 0)
            {
                sw.Stop();
                _logger.LogWarning("Ollama validation: No models installed");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "No models installed in Ollama",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            // Get the first available model
            var firstModel = models[0];
            string? modelName = null;
            if (firstModel.TryGetProperty("name", out var nameProperty))
            {
                modelName = nameProperty.GetString();
            }

            if (string.IsNullOrEmpty(modelName))
            {
                sw.Stop();
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "Could not determine model name",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            // Step 2: Test with a minimal 2-token completion
            var requestBody = new
            {
                model = modelName,
                prompt = "Hi",
                stream = false,
                options = new
                {
                    num_predict = 2
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts2.CancelAfter(TimeSpan.FromSeconds(15));

            var generateResponse = await _httpClient.PostAsync($"{baseUrl}/api/generate", content, cts2.Token);

            sw.Stop();

            if (generateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ollama validation successful with model {Model}", modelName);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = true,
                    Details = $"Connected successfully (model: {modelName})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else
            {
                _logger.LogWarning("Ollama validation failed: Generate returned {StatusCode}", generateResponse.StatusCode);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"Generate test failed (HTTP {generateResponse.StatusCode})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Ollama validation timed out");
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
            _logger.LogWarning(ex, "Ollama validation failed: Network error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Cannot connect to Ollama at {baseUrl}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Ollama validation failed: Unexpected error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }
}
