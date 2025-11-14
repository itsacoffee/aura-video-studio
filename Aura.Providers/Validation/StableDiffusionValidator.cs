using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Validates Stable Diffusion WebUI connectivity by listing models and testing with minimal 256x256 8-step generation
/// </summary>
public class StableDiffusionValidator : IProviderValidator
{
    private readonly ILogger<StableDiffusionValidator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "StableDiffusion";

    public StableDiffusionValidator(ILogger<StableDiffusionValidator> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var baseUrl = configUrl ?? "http://127.0.0.1:7860";

        try
        {
            // Step 1: Check if SD WebUI is running by testing the root endpoint first
            // This is more reliable as it doesn't require --api flag for basic connectivity
            using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts1.CancelAfter(TimeSpan.FromSeconds(5));

            // Try the root endpoint first to see if SD WebUI is running at all
            var rootResponse = await _httpClient.GetAsync(baseUrl, cts1.Token).ConfigureAwait(false);
            
            if (!rootResponse.IsSuccessStatusCode)
            {
                sw.Stop();
                _logger.LogWarning("SD WebUI validation failed: Base URL {BaseUrl} returned {StatusCode}", baseUrl, rootResponse.StatusCode);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"SD WebUI not responding at {baseUrl} (HTTP {rootResponse.StatusCode})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            
            // Step 2: Try to list models to verify API is enabled
            using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts2.CancelAfter(TimeSpan.FromSeconds(5));
            
            var listResponse = await _httpClient.GetAsync($"{baseUrl}/sdapi/v1/sd-models", cts2.Token).ConfigureAwait(false);

            if (!listResponse.IsSuccessStatusCode)
            {
                sw.Stop();
                _logger.LogWarning("SD WebUI validation: API endpoint not available. Make sure to run with --api flag");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "SD WebUI is running but API is not enabled. Please start with --api flag",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            var listJson = await listResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var models = JsonDocument.Parse(listJson).RootElement;

            if (models.GetArrayLength() == 0)
            {
                sw.Stop();
                _logger.LogWarning("SD WebUI validation: No models installed");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = "No models installed in SD WebUI",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            // Step 3: Test with minimal 256x256 8-step generation
            var requestBody = new
            {
                prompt = "test",
                negative_prompt = "",
                steps = 8,
                width = 256,
                height = 256,
                cfg_scale = 7.0,
                sampler_name = "Euler a",
                seed = -1
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts3 = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts3.CancelAfter(TimeSpan.FromSeconds(30)); // SD generation can take time even for small images

            var generateResponse = await _httpClient.PostAsync($"{baseUrl}/sdapi/v1/txt2img", content, cts3.Token).ConfigureAwait(false);

            sw.Stop();

            if (generateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("SD WebUI validation successful");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = true,
                    Details = "Connected successfully",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else
            {
                var errorContent = await generateResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("SD WebUI validation failed: Generate returned {StatusCode}", generateResponse.StatusCode);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"Generation test failed (HTTP {generateResponse.StatusCode})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("SD WebUI validation timed out");
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
            _logger.LogWarning(ex, "SD WebUI validation failed: Network error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Cannot connect to SD WebUI at {baseUrl}. Make sure SD WebUI is running with --api flag",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SD WebUI validation failed: Unexpected error");
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
