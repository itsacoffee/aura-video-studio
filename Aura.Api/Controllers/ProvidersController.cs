using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models.Providers;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private readonly IHardwareDetector _hardwareDetector;
    private readonly IKeyStore _keyStore;
    private readonly LlmProviderRecommendationService? _recommendationService;
    private readonly ProviderHealthMonitoringService? _healthMonitoringService;
    private readonly ProviderCostTrackingService? _costTrackingService;
    private readonly ProviderSettings _settings;
    private readonly OpenAIKeyValidationService _openAIValidationService;
    private readonly Aura.Core.Services.IKeyValidationService _keyValidationService;
    private readonly Aura.Core.Services.ISecureStorageService _secureStorageService;
    private readonly ProviderPingService _providerPingService;

    public ProvidersController(
        IHardwareDetector hardwareDetector, 
        IKeyStore keyStore,
        ProviderSettings settings,
        OpenAIKeyValidationService openAIValidationService,
        Aura.Core.Services.IKeyValidationService keyValidationService,
        Aura.Core.Services.ISecureStorageService secureStorageService,
        ProviderPingService providerPingService,
        LlmProviderRecommendationService? recommendationService = null,
        ProviderHealthMonitoringService? healthMonitoringService = null,
        ProviderCostTrackingService? costTrackingService = null)
    {
        _hardwareDetector = hardwareDetector;
        _keyStore = keyStore;
        _settings = settings;
        _openAIValidationService = openAIValidationService;
        _keyValidationService = keyValidationService;
        _secureStorageService = secureStorageService;
        _providerPingService = providerPingService;
        _recommendationService = recommendationService;
        _healthMonitoringService = healthMonitoringService;
        _costTrackingService = costTrackingService;
    }

    /// <summary>
    /// Get provider capabilities based on hardware, API keys, and OS detection
    /// </summary>
    [HttpGet("capabilities")]
    public async Task<IActionResult> GetCapabilities()
    {
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            var capabilities = new List<ProviderCapability>();

            // Stable Diffusion provider
            var sdCapability = GetStableDiffusionCapability(systemProfile);
            capabilities.Add(sdCapability);

            return Ok(capabilities);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error detecting provider capabilities");
            return Problem("Error detecting provider capabilities", statusCode: 500);
        }
    }

    private ProviderCapability GetStableDiffusionCapability(Aura.Core.Models.SystemProfile systemProfile)
    {
        var available = true;
        var reasonCodes = new List<string>();
        
        // Check for NVIDIA GPU
        var hasNvidiaGpu = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "NVIDIA";
        if (!hasNvidiaGpu)
        {
            available = false;
            reasonCodes.Add("RequiresNvidiaGPU");
        }

        // Check for STABLE_KEY API key
        var stableKey = _keyStore.GetKey("STABLE_KEY") ?? _keyStore.GetKey("stabilityai");
        if (string.IsNullOrWhiteSpace(stableKey))
        {
            available = false;
            reasonCodes.Add("MissingApiKey:STABLE_KEY");
        }

        // Check VRAM requirement (6GB minimum)
        if (systemProfile.Gpu?.VramGB < 6)
        {
            available = false;
            reasonCodes.Add("InsufficientVRAM");
        }

        // Check OS (Windows or Linux)
        var isWindowsOrLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                               RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (!isWindowsOrLinux)
        {
            available = false;
            reasonCodes.Add("UnsupportedOS");
        }

        return new ProviderCapability
        {
            Name = "StableDiffusion",
            Available = available,
            ReasonCodes = reasonCodes.ToArray(),
            Requirements = new ProviderRequirements
            {
                NeedsKey = new[] { "STABLE_KEY" },
                NeedsGPU = "nvidia",
                MinVRAMMB = 6144,
                Os = new[] { "windows", "linux" }
            }
        };
    }

    /// <summary>
    /// Get all configured AI providers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Information("Fetching all providers, CorrelationId: {CorrelationId}", correlationId);

            var configuredProviders = await _secureStorageService.GetConfiguredProvidersAsync().ConfigureAwait(false);
            var providers = new List<AIProviderDto>();

            // Define all known providers
            var allProviders = new (string id, string name, string type, string keyName)[]
            {
                ("openai", "OpenAI", "llm", "openai"),
                ("anthropic", "Anthropic", "llm", "anthropic"),
                ("gemini", "Google Gemini", "llm", "gemini"),
                ("ollama", "Ollama", "llm", "ollama"),
                ("elevenlabs", "ElevenLabs", "tts", "elevenlabs"),
                ("playht", "PlayHT", "tts", "playht"),
                ("windows", "Windows TTS", "tts", "windows"),
                ("stabilityai", "Stability AI", "image", "stabilityai"),
                ("stablediffusion", "Stable Diffusion", "image", "stablediffusion"),
            };

            foreach (var (id, name, type, keyName) in allProviders)
            {
                var hasKey = await _secureStorageService.HasApiKeyAsync(keyName).ConfigureAwait(false);
                var isConfigured = configuredProviders.Contains(id, StringComparer.OrdinalIgnoreCase) || hasKey;
                var isLocal = IsLocalProvider(id);

                if (isConfigured || isLocal)
                {
                    providers.Add(new AIProviderDto(
                        Id: id,
                        Name: name,
                        Type: type,
                        IsDefault: id.Equals("openai", StringComparison.OrdinalIgnoreCase) || 
                                   id.Equals("elevenlabs", StringComparison.OrdinalIgnoreCase) ||
                                   id.Equals("stabilityai", StringComparison.OrdinalIgnoreCase),
                        IsEnabled: isConfigured || isLocal,
                        Model: null,
                        HasFallback: true
                    ));
                }
            }

            return Ok(providers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching providers, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Provider Fetch Error",
                detail: "An error occurred while fetching providers.",
                statusCode: 500,
                instance: correlationId);
        }
    }

    /// <summary>
    /// Test connection to a specific provider
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestProviderConnection(string id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Information("Testing provider connection for {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);

            var result = await _providerPingService.PingAsync(id, null, cancellationToken).ConfigureAwait(false);

            return Ok(new
            {
                success = result.Success,
                message = result.Message ?? (result.Success ? "Connection successful" : "Connection failed"),
                latency = result.LatencyMs ?? 0,
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error testing provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);
            return Ok(new
            {
                success = false,
                message = $"Error testing provider: {ex.Message}",
                latency = 0,
                correlationId
            });
        }
    }

    /// <summary>
    /// Get usage statistics for a specific provider
    /// </summary>
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetProviderStats(string id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Information("Fetching stats for provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);

            // Attempt to get stats from cost tracking service
            if (_costTrackingService != null)
            {
                var costByProvider = _costTrackingService.GetCostByProvider();
                var providerCost = costByProvider.TryGetValue(id, out var cost) ? cost : 0m;

                return Ok(new ProviderStatsDto(
                    TotalRequests: 0,
                    SuccessfulRequests: 0,
                    FailedRequests: 0,
                    AverageLatency: 0,
                    TotalTokensUsed: 0,
                    TotalCost: providerCost,
                    LastUsed: DateTime.UtcNow.ToString("o")
                ));
            }

            // Return empty stats if service not available
            return Ok(new ProviderStatsDto(
                TotalRequests: 0,
                SuccessfulRequests: 0,
                FailedRequests: 0,
                AverageLatency: 0,
                TotalTokensUsed: 0,
                TotalCost: 0m,
                LastUsed: DateTime.UtcNow.ToString("o")
            ));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching stats for provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);
            return Problem(
                title: "Stats Fetch Error",
                detail: $"Error fetching stats for provider: {ex.Message}",
                statusCode: 500,
                instance: correlationId);
        }
    }

    /// <summary>
    /// Set a provider as the default for its type
    /// </summary>
    [HttpPost("{id}/set-default")]
    public Task<IActionResult> SetDefaultProvider(string id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Information("Setting provider {ProviderId} as default, CorrelationId: {CorrelationId}", id, correlationId);

            // For now, just acknowledge the request - full implementation would persist this preference
            return Task.FromResult<IActionResult>(Ok(new
            {
                success = true,
                message = $"Provider {id} set as default",
                correlationId
            }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting default provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);
            return Task.FromResult<IActionResult>(Problem(
                title: "Set Default Error",
                detail: $"Error setting default provider: {ex.Message}",
                statusCode: 500,
                instance: correlationId));
        }
    }

    /// <summary>
    /// Get configuration for a specific provider
    /// </summary>
    [HttpGet("{id}/config")]
    public async Task<IActionResult> GetProviderConfig(string id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Information("Fetching config for provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);

            var hasKey = await _secureStorageService.HasApiKeyAsync(id).ConfigureAwait(false);
            var providerType = DetermineCategory(id).ToLowerInvariant() switch
            {
                "llm" => "llm",
                "tts" => "tts",
                "image" => "image",
                _ => "unknown"
            };

            return Ok(new ProviderSettingsDto(
                ApiKey: hasKey ? "********" : null,
                Endpoint: null,
                Model: null,
                MaxTokens: providerType == "llm" ? 4096 : null,
                Temperature: providerType == "llm" ? 0.7 : null,
                IsEnabled: hasKey || IsLocalProvider(id),
                HasFallback: true,
                Type: providerType
            ));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching config for provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);
            return Problem(
                title: "Config Fetch Error",
                detail: $"Error fetching config for provider: {ex.Message}",
                statusCode: 500,
                instance: correlationId);
        }
    }

    /// <summary>
    /// Update configuration for a specific provider
    /// </summary>
    [HttpPut("{id}/config")]
    public async Task<IActionResult> UpdateProviderConfig(
        string id,
        [FromBody] UpdateProviderConfigRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Information("Updating config for provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);

            // Save API key if provided and not masked
            if (!string.IsNullOrWhiteSpace(request.ApiKey) && !request.ApiKey.StartsWith("*"))
            {
                await _secureStorageService.SaveApiKeyAsync(id, request.ApiKey).ConfigureAwait(false);
            }

            return Ok(new
            {
                success = true,
                message = $"Configuration updated for provider {id}",
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating config for provider {ProviderId}, CorrelationId: {CorrelationId}", id, correlationId);
            return Problem(
                title: "Config Update Error",
                detail: $"Error updating config for provider: {ex.Message}",
                statusCode: 500,
                instance: correlationId);
        }
    }

    /// <summary>
    /// Get provider recommendations for a specific operation type
    /// </summary>
    [HttpGet("recommendations/{operationType}")]
    public async Task<IActionResult> GetRecommendations(
        string operationType,
        [FromQuery] int estimatedInputTokens = 1000)
    {
        if (_recommendationService == null)
        {
            return Problem("Provider recommendation service not available", statusCode: 503);
        }

        try
        {
            if (!Enum.TryParse<LlmOperationType>(operationType, true, out var opType))
            {
                return BadRequest(new { error = $"Invalid operation type: {operationType}" });
            }

            var recommendations = await _recommendationService.GetRecommendationsAsync(
                opType,
                estimatedInputTokens: estimatedInputTokens,
                cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

            var dtos = recommendations.Select(r => new ProviderRecommendationDto(
                r.ProviderName,
                r.Reasoning,
                r.QualityScore,
                r.EstimatedCost,
                r.ExpectedLatencySeconds,
                r.IsAvailable,
                r.HealthStatus.ToString(),
                r.Confidence)).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting provider recommendations for {OperationType}", operationType);
            return Problem($"Error getting provider recommendations: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get health status of all LLM providers
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetProviderHealth()
    {
        if (_healthMonitoringService == null)
        {
            return Problem("Provider health monitoring service not available", statusCode: 503);
        }

        try
        {
            var healthMetrics = _healthMonitoringService.GetAllProviderHealth();
            var dtos = healthMetrics.Select(m => new ProviderHealthDto(
                m.ProviderName,
                m.SuccessRatePercent,
                m.AverageLatencySeconds,
                m.TotalRequests,
                m.ConsecutiveFailures,
                m.Status.ToString())).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting provider health metrics");
            return Problem($"Error getting provider health metrics: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get cost tracking summary for current month
    /// </summary>
    [HttpGet("cost-tracking")]
    public IActionResult GetCostTracking()
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            var totalCost = _costTrackingService.GetTotalMonthlyCost();
            var costByProvider = _costTrackingService.GetCostByProvider();
            var costByOperation = _costTrackingService.GetCostByOperation();

            var costByOperationStrings = costByOperation.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value);

            var dto = new CostTrackingSummaryDto(
                totalCost,
                costByProvider,
                costByOperationStrings);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting cost tracking summary");
            return Problem($"Error getting cost tracking summary: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Estimate cost for a specific operation
    /// </summary>
    [HttpPost("cost-estimate")]
    public IActionResult EstimateCost([FromBody] ProviderRecommendationRequest request)
    {
        if (_recommendationService == null)
        {
            return Problem("Provider recommendation service not available", statusCode: 503);
        }

        try
        {
            if (!Enum.TryParse<LlmOperationType>(request.OperationType, true, out var opType))
            {
                return BadRequest(new { error = $"Invalid operation type: {request.OperationType}" });
            }

            return Ok(new { message = "Cost estimation endpoint - implementation in progress" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error estimating cost");
            return Problem($"Error estimating cost: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get available provider profiles
    /// </summary>
    [HttpGet("profiles")]
    public IActionResult GetProviderProfiles()
    {
        try
        {
            var profiles = Enum.GetValues<ProviderProfile>()
                .Select(p => new
                {
                    Name = p.ToString(),
                    Description = p switch
                    {
                        ProviderProfile.MaximumQuality => "Always use highest quality provider regardless of cost",
                        ProviderProfile.Balanced => "Balance between quality, cost, and speed",
                        ProviderProfile.BudgetConscious => "Prefer cheaper providers that meet minimum quality threshold",
                        ProviderProfile.SpeedOptimized => "Optimize for fastest response times",
                        ProviderProfile.LocalOnly => "Only use local/offline providers (Ollama, RuleBased)",
                        ProviderProfile.Custom => "User-defined custom rules",
                        _ => ""
                    }
                })
                .ToList();

            return Ok(profiles);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting provider profiles");
            return Problem($"Error getting provider profiles: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Test provider connection with API key
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] TestProviderConnectionRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProviderName))
            {
                return BadRequest(new { 
                    success = false,
                    message = "Provider name is required",
                    correlationId
                });
            }

            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new { 
                    success = false,
                    message = "API key is required",
                    correlationId
                });
            }

            Log.Information(
                "Testing provider connection for {Provider}, CorrelationId: {CorrelationId}",
                request.ProviderName,
                correlationId);

            var startTime = DateTime.UtcNow;
            var result = await _keyValidationService.TestApiKeyAsync(
                request.ProviderName.ToLowerInvariant(),
                request.ApiKey,
                cancellationToken).ConfigureAwait(false);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            Log.Information(
                "Provider connection test completed for {Provider}, Success: {Success}, ResponseTime: {ResponseTime}ms, CorrelationId: {CorrelationId}",
                request.ProviderName,
                result.IsValid,
                responseTime,
                correlationId);

            return Ok(new { 
                success = result.IsValid,
                message = result.Message,
                details = result.Details,
                responseTimeMs = responseTime,
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error testing provider connection for {Provider}, CorrelationId: {CorrelationId}", 
                request.ProviderName, correlationId);
            return Problem(
                title: "Connection Test Error",
                detail: $"An error occurred while testing the connection: {ex.Message}",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#connection-test-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Validate OpenAI API key with live network verification
    /// </summary>
    [HttpPost("openai/validate")]
    public async Task<IActionResult> ValidateOpenAIKey(
        [FromBody] ValidateOpenAIKeyRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new ProviderValidationResponse(
                    IsValid: false,
                    Status: "Invalid",
                    Message: "API key is required",
                    CorrelationId: correlationId,
                    Details: new ValidationDetails(
                        Provider: "OpenAI",
                        KeyFormat: "empty",
                        FormatValid: false)));
            }

            Log.Information(
                "Validating OpenAI API key, CorrelationId: {CorrelationId}",
                correlationId);

            var result = await _openAIValidationService.ValidateKeyAsync(
                request.ApiKey,
                request.BaseUrl,
                request.OrganizationId,
                request.ProjectId,
                correlationId,
                cancellationToken).ConfigureAwait(false);

            var response = new ProviderValidationResponse(
                IsValid: result.IsValid,
                Status: result.Status,
                Message: result.Message,
                CorrelationId: correlationId,
                Details: new ValidationDetails(
                    Provider: "OpenAI",
                    KeyFormat: result.FormatValid ? "valid" : "invalid",
                    FormatValid: result.FormatValid,
                    NetworkCheckPassed: result.NetworkCheckPassed,
                    HttpStatusCode: result.HttpStatusCode,
                    ErrorType: result.ErrorType,
                    ResponseTimeMs: result.ResponseTimeMs,
                    DiagnosticInfo: result.DiagnosticInfo));

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating OpenAI API key, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Validation Error",
                detail: "An unexpected error occurred while validating the API key.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#validation-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Get available OpenAI models for a validated API key
    /// </summary>
    [HttpPost("openai/models")]
    public async Task<IActionResult> GetOpenAIModels(
        [FromBody] ValidateOpenAIKeyRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new { 
                    success = false,
                    message = "API key is required",
                    correlationId
                });
            }

            Log.Information(
                "Fetching OpenAI models, CorrelationId: {CorrelationId}",
                correlationId);

            var result = await _openAIValidationService.GetAvailableModelsAsync(
                request.ApiKey,
                request.BaseUrl,
                request.OrganizationId,
                request.ProjectId,
                cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                return Ok(new { 
                    success = false,
                    message = result.ErrorMessage,
                    correlationId
                });
            }

            return Ok(new { 
                success = true,
                models = result.Models,
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching OpenAI models, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Models Fetch Error",
                detail: "An unexpected error occurred while fetching models.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#models-fetch-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Test OpenAI script generation with a simple prompt
    /// </summary>
    [HttpPost("openai/test-generation")]
    public async Task<IActionResult> TestOpenAIGeneration(
        [FromBody] TestOpenAIGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new { 
                    success = false,
                    message = "API key is required",
                    correlationId
                });
            }

            if (string.IsNullOrWhiteSpace(request.Model))
            {
                return BadRequest(new { 
                    success = false,
                    message = "Model is required",
                    correlationId
                });
            }

            Log.Information(
                "Testing OpenAI script generation with model {Model}, CorrelationId: {CorrelationId}",
                request.Model,
                correlationId);

            var result = await _openAIValidationService.TestScriptGenerationAsync(
                request.ApiKey,
                request.Model,
                request.BaseUrl,
                request.OrganizationId,
                request.ProjectId,
                cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                return Ok(new { 
                    success = false,
                    message = result.ErrorMessage,
                    responseTimeMs = result.ResponseTimeMs,
                    correlationId
                });
            }

            return Ok(new { 
                success = true,
                generatedText = result.GeneratedText,
                model = result.Model,
                responseTimeMs = result.ResponseTimeMs,
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error testing OpenAI generation, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Generation Test Error",
                detail: "An unexpected error occurred while testing script generation.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#generation-test-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Validate ElevenLabs API key with live network verification
    /// </summary>
    [HttpPost("elevenlabs/validate")]
    public async Task<IActionResult> ValidateElevenLabsKey(
        [FromBody] ValidateElevenLabsKeyRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new ProviderValidationResponse(
                    IsValid: false,
                    Status: "Invalid",
                    Message: "API key is required",
                    CorrelationId: correlationId,
                    Details: new ValidationDetails(
                        Provider: "ElevenLabs",
                        KeyFormat: "empty",
                        FormatValid: false)));
            }

            Log.Information(
                "Validating ElevenLabs API key, CorrelationId: {CorrelationId}",
                correlationId);

            var startTime = DateTime.UtcNow;
            var result = await _keyValidationService.TestApiKeyAsync("elevenlabs", request.ApiKey, cancellationToken).ConfigureAwait(false);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new ProviderValidationResponse(
                IsValid: result.IsValid,
                Status: result.IsValid ? "Valid" : "Invalid",
                Message: result.Message,
                CorrelationId: correlationId,
                Details: new ValidationDetails(
                    Provider: "ElevenLabs",
                    KeyFormat: "valid",
                    FormatValid: true,
                    NetworkCheckPassed: result.IsValid,
                    ResponseTimeMs: responseTime));

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating ElevenLabs API key, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Validation Error",
                detail: "An unexpected error occurred while validating the API key.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#validation-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Validate PlayHT API key with live network verification
    /// </summary>
    [HttpPost("playht/validate")]
    public async Task<IActionResult> ValidatePlayHTKey(
        [FromBody] ValidatePlayHTKeyRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new ProviderValidationResponse(
                    IsValid: false,
                    Status: "Invalid",
                    Message: "API key is required",
                    CorrelationId: correlationId,
                    Details: new ValidationDetails(
                        Provider: "PlayHT",
                        KeyFormat: "empty",
                        FormatValid: false)));
            }

            Log.Information(
                "Validating PlayHT API key, CorrelationId: {CorrelationId}",
                correlationId);

            var startTime = DateTime.UtcNow;
            var result = await _keyValidationService.TestApiKeyAsync("playht", request.ApiKey, cancellationToken).ConfigureAwait(false);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new ProviderValidationResponse(
                IsValid: result.IsValid,
                Status: result.IsValid ? "Valid" : "Invalid",
                Message: result.Message,
                CorrelationId: correlationId,
                Details: new ValidationDetails(
                    Provider: "PlayHT",
                    KeyFormat: "valid",
                    FormatValid: true,
                    NetworkCheckPassed: result.IsValid,
                    ResponseTimeMs: responseTime));

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating PlayHT API key, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Validation Error",
                detail: "An unexpected error occurred while validating the API key.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#validation-error",
                instance: correlationId);
        }
    }



    /// <summary>
    /// Get current provider recommendation preferences
    /// </summary>
    [HttpGet("preferences")]
    public IActionResult GetPreferences()
    {
        try
        {
            var dto = new ProviderPreferencesDto(
                EnableRecommendations: _settings.GetEnableRecommendations(),
                AssistanceLevel: _settings.GetAssistanceLevel(),
                EnableHealthMonitoring: _settings.GetEnableHealthMonitoring(),
                EnableCostTracking: _settings.GetEnableCostTracking(),
                EnableLearning: _settings.GetEnableLearning(),
                EnableProfiles: _settings.GetEnableProfiles(),
                EnableAutoFallback: _settings.GetEnableAutoFallback(),
                GlobalDefault: null,
                AlwaysUseDefault: false,
                PerOperationOverrides: null,
                ActiveProfile: "Balanced",
                ExcludedProviders: null,
                PinnedProvider: null,
                FallbackChains: null,
                MonthlyBudgetLimit: null,
                PerProviderBudgetLimits: null,
                HardBudgetLimit: false
            );

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting provider preferences");
            return Problem($"Error getting provider preferences: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Update provider recommendation preferences
    /// </summary>
    [HttpPost("preferences")]
    public IActionResult UpdatePreferences([FromBody] ProviderPreferencesDto preferences)
    {
        try
        {
            _settings.SetRecommendationPreferences(
                enableRecommendations: preferences.EnableRecommendations,
                assistanceLevel: preferences.AssistanceLevel,
                enableHealthMonitoring: preferences.EnableHealthMonitoring,
                enableCostTracking: preferences.EnableCostTracking,
                enableLearning: preferences.EnableLearning,
                enableProfiles: preferences.EnableProfiles,
                enableAutoFallback: preferences.EnableAutoFallback
            );

            return Ok(new { message = "Preferences updated successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating provider preferences");
            return Problem($"Error updating provider preferences: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get status of all configured providers
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetProviderStatus(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("Fetching provider status, CorrelationId: {CorrelationId}", correlationId);

            var statuses = new List<ProviderValidationStatusDto>();
            var configuredProviders = await _secureStorageService.GetConfiguredProvidersAsync().ConfigureAwait(false);

            var providerNames = new[] { "OpenAI", "Anthropic", "Google", "Ollama", "ElevenLabs", "PlayHT", "Windows", "StabilityAI", "StableDiffusion", "Stock" };

            foreach (var provider in providerNames)
            {
                var isConfigured = configuredProviders.Contains(provider, StringComparer.OrdinalIgnoreCase);
                var hasKey = await _secureStorageService.HasApiKeyAsync(provider).ConfigureAwait(false);

                var status = new ProviderValidationStatusDto(
                    Name: provider,
                    IsConfigured: isConfigured || hasKey,
                    IsAvailable: isConfigured || hasKey || IsLocalProvider(provider),
                    Status: GetProviderStatus(provider, hasKey),
                    LastValidated: null,
                    ErrorMessage: null
                );

                statuses.Add(status);
            }

            return Ok(statuses);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching provider status, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Provider Status Error",
                detail: "An error occurred while fetching provider status.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#provider-status-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Get provider priorities
    /// </summary>
    [HttpGet("priorities")]
    public async Task<IActionResult> GetProviderPriorities(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("Fetching provider priorities, CorrelationId: {CorrelationId}", correlationId);

            var configPath = Path.Combine(
                _settings.GetAuraDataDirectory(),
                "configurations",
                "provider-config.json");

            if (!System.IO.File.Exists(configPath))
            {
                var defaultPriorities = new Dictionary<string, int>
                {
                    { "OpenAI", 1 },
                    { "Anthropic", 2 },
                    { "Google", 3 },
                    { "Ollama", 4 },
                    { "ElevenLabs", 1 },
                    { "PlayHT", 2 },
                    { "Windows", 3 },
                    { "StabilityAI", 1 },
                    { "StableDiffusion", 2 },
                    { "Stock", 3 }
                };
                
                return Ok(defaultPriorities);
            }

            var json = await System.IO.File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
            var config = System.Text.Json.JsonSerializer.Deserialize<ProviderConfigurationResponse>(json);
            
            var priorities = config?.Providers?.ToDictionary(
                p => p.Name,
                p => p.Priority
            ) ?? new Dictionary<string, int>();

            return Ok(priorities);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching provider priorities, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Provider Priorities Error",
                detail: "An error occurred while fetching provider priorities.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#provider-priorities-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Update provider priorities
    /// </summary>
    [HttpPut("priorities")]
    public async Task<IActionResult> UpdateProviderPriorities(
        [FromBody] Dictionary<string, int> priorities,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (priorities == null || priorities.Count == 0)
            {
                return BadRequest(new { 
                    success = false,
                    message = "Priorities dictionary is required",
                    correlationId
                });
            }

            Log.Information("Updating provider priorities, CorrelationId: {CorrelationId}", correlationId);

            var configPath = Path.Combine(
                _settings.GetAuraDataDirectory(),
                "configurations",
                "provider-config.json");

            if (!System.IO.File.Exists(configPath))
            {
                return NotFound(new { 
                    success = false,
                    message = "Provider configuration not found",
                    correlationId
                });
            }

            var json = await System.IO.File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);
            var config = System.Text.Json.JsonSerializer.Deserialize<ProviderConfigurationResponse>(json);

            if (config?.Providers == null)
            {
                return Problem(
                    title: "Invalid Configuration",
                    detail: "Provider configuration is invalid or empty.",
                    statusCode: 500);
            }

            foreach (var provider in config.Providers)
            {
                if (priorities.TryGetValue(provider.Name, out var priority))
                {
                    provider.Priority = priority;
                }
            }

            var updatedJson = JsonSerializer.Serialize(config, s_jsonOptions);
            await System.IO.File.WriteAllTextAsync(configPath, updatedJson, cancellationToken).ConfigureAwait(false);

            Log.Information("Provider priorities updated successfully, CorrelationId: {CorrelationId}", correlationId);

            return Ok(new { 
                success = true,
                message = "Priorities updated successfully",
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating provider priorities, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Provider Priorities Update Error",
                detail: $"An error occurred while updating provider priorities: {ex.Message}",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#provider-priorities-update-error",
                instance: correlationId);
        }
    }

    private static bool IsLocalProvider(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "ollama" => true,
            "windows" => true,
            "stock" => true,
            "stablediffusion" => true,
            _ => false
        };
    }

    private static string GetProviderStatus(string provider, bool hasKey)
    {
        if (IsLocalProvider(provider))
        {
            return "Available";
        }
        return hasKey ? "Configured" : "Not configured";
    }

    /// <summary>
    /// Get Ollama service status with version and model count
    /// </summary>
    [HttpGet("ollama/status")]
    public async Task<IActionResult> GetOllamaStatus(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            var healthCheckService = HttpContext.RequestServices.GetService(typeof(Aura.Core.Services.Providers.OllamaHealthCheckService)) 
                as Aura.Core.Services.Providers.OllamaHealthCheckService;

            if (healthCheckService == null)
            {
                return Ok(new
                {
                    isAvailable = false,
                    isHealthy = false,
                    version = (string?)null,
                    modelsCount = 0,
                    runningModelsCount = 0,
                    message = "Ollama health check service not initialized",
                    correlationId
                });
            }

            var health = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

            Log.Information(
                "Ollama status check: Healthy={Healthy}, Version={Version}, Models={ModelsCount}, Running={RunningCount}, CorrelationId: {CorrelationId}",
                health.IsHealthy,
                health.Version,
                health.AvailableModels.Count,
                health.RunningModels.Count,
                correlationId);

            return Ok(new
            {
                isAvailable = health.IsHealthy,
                isHealthy = health.IsHealthy,
                version = health.Version,
                modelsCount = health.AvailableModels.Count,
                runningModelsCount = health.RunningModels.Count,
                baseUrl = health.BaseUrl,
                responseTimeMs = health.ResponseTimeMs,
                message = health.IsHealthy 
                    ? $"Ollama running with {health.AvailableModels.Count} models available"
                    : health.ErrorMessage ?? "Ollama service not running",
                lastChecked = health.LastChecked,
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking Ollama status, CorrelationId: {CorrelationId}", correlationId);
            return Ok(new
            {
                isAvailable = false,
                isHealthy = false,
                version = (string?)null,
                modelsCount = 0,
                runningModelsCount = 0,
                message = $"Error checking Ollama: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get list of available Ollama models
    /// </summary>
    [HttpGet("ollama/models")]
    public async Task<IActionResult> GetOllamaModels(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            var ollamaDetectionService = HttpContext.RequestServices.GetService(typeof(Aura.Core.Services.Providers.OllamaDetectionService)) 
                as Aura.Core.Services.Providers.OllamaDetectionService;

            if (ollamaDetectionService == null)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Ollama detection service not initialized",
                    installationInstructions = "Install Ollama: curl -fsSL https://ollama.com/install.sh | sh",
                    correlationId
                });
            }

            var status = await ollamaDetectionService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            
            if (!status.IsRunning)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Ollama service not running",
                    installationInstructions = "Start Ollama: ollama serve (or install: curl -fsSL https://ollama.com/install.sh | sh)",
                    correlationId
                });
            }

            var models = await ollamaDetectionService.GetModelsAsync(cancellationToken).ConfigureAwait(false);

            Log.Information(
                "Ollama models fetched: {ModelsCount} models, CorrelationId: {CorrelationId}",
                models.Count,
                correlationId);

            return Ok(new
            {
                success = true,
                models = models.Select(m => new
                {
                    name = m.Name,
                    size = m.Size,
                    sizeFormatted = FormatBytes(m.Size),
                    modified = m.ModifiedAt,
                    modifiedFormatted = m.ModifiedAt != null 
                        ? DateTime.Parse(m.ModifiedAt, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                        : null
                }).ToList(),
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching Ollama models, CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(503, new
            {
                success = false,
                message = $"Error fetching Ollama models: {ex.Message}",
                installationInstructions = "Install Ollama: curl -fsSL https://ollama.com/install.sh | sh",
                correlationId
            });
        }
    }

    /// <summary>
    /// Validate Ollama service availability
    /// </summary>
    [HttpPost("ollama/validate")]
    public async Task<IActionResult> ValidateOllama(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            var ollamaDetectionService = HttpContext.RequestServices.GetService(typeof(Aura.Core.Services.Providers.OllamaDetectionService)) 
                as Aura.Core.Services.Providers.OllamaDetectionService;

            if (ollamaDetectionService == null)
            {
                return Ok(new
                {
                    success = false,
                    message = "Ollama detection service not initialized",
                    isAvailable = false,
                    modelsCount = 0,
                    models = Array.Empty<object>(),
                    correlationId
                });
            }

            var status = await ollamaDetectionService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            var models = status.IsRunning 
                ? await ollamaDetectionService.GetModelsAsync(cancellationToken).ConfigureAwait(false)
                : new List<Aura.Core.Services.Providers.OllamaModel>();

            Log.Information(
                "Ollama validation: Available={Available}, Models={ModelsCount}, CorrelationId: {CorrelationId}",
                status.IsRunning,
                models.Count,
                correlationId);

            return Ok(new
            {
                success = status.IsRunning,
                message = status.IsRunning 
                    ? $"Ollama is available with {models.Count} models"
                    : status.ErrorMessage ?? "Ollama service not available",
                isAvailable = status.IsRunning,
                version = status.Version,
                modelsCount = models.Count,
                models = models.Select(m => new
                {
                    name = m.Name,
                    size = m.Size,
                    sizeFormatted = FormatBytes(m.Size),
                    modified = m.ModifiedAt
                }).ToList(),
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating Ollama, CorrelationId: {CorrelationId}", correlationId);
            return Ok(new
            {
                success = false,
                message = $"Error validating Ollama: {ex.Message}",
                isAvailable = false,
                modelsCount = 0,
                models = Array.Empty<object>(),
                correlationId
            });
        }
    }

    /// <summary>
    /// Pull a model from Ollama library
    /// </summary>
    [HttpPost("ollama/pull")]
    public async Task<IActionResult> PullOllamaModel(
        [FromBody] PullOllamaModelRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.ModelName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Model name is required",
                    correlationId
                });
            }

            var ollamaDetectionService = HttpContext.RequestServices.GetService(typeof(Aura.Core.Services.Providers.OllamaDetectionService)) 
                as Aura.Core.Services.Providers.OllamaDetectionService;

            if (ollamaDetectionService == null)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Ollama detection service not initialized",
                    correlationId
                });
            }

            var status = await ollamaDetectionService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (!status.IsRunning)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Ollama service not running",
                    correlationId
                });
            }

            Log.Information("Starting model pull: {ModelName}, CorrelationId: {CorrelationId}", request.ModelName, correlationId);

            var success = await ollamaDetectionService.PullModelAsync(
                request.ModelName, 
                null,
                cancellationToken).ConfigureAwait(false);

            if (success)
            {
                Log.Information("Model pull completed: {ModelName}, CorrelationId: {CorrelationId}", request.ModelName, correlationId);
                
                return Ok(new
                {
                    success = true,
                    message = $"Model {request.ModelName} pulled successfully",
                    modelName = request.ModelName,
                    correlationId
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Failed to pull model {request.ModelName}",
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error pulling Ollama model, CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new
            {
                success = false,
                message = $"Error pulling model: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get running Ollama models
    /// </summary>
    [HttpGet("ollama/running")]
    public async Task<IActionResult> GetRunningOllamaModels(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            var healthCheckService = HttpContext.RequestServices.GetService(typeof(Aura.Core.Services.Providers.OllamaHealthCheckService)) 
                as Aura.Core.Services.Providers.OllamaHealthCheckService;

            if (healthCheckService == null)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Ollama health check service not initialized",
                    correlationId
                });
            }

            var health = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

            if (!health.IsHealthy)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = health.ErrorMessage ?? "Ollama service not available",
                    correlationId
                });
            }

            Log.Information(
                "Running models fetched: {ModelsCount} models, CorrelationId: {CorrelationId}",
                health.RunningModels.Count,
                correlationId);

            return Ok(new
            {
                success = true,
                runningModels = health.RunningModels,
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching running Ollama models, CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new
            {
                success = false,
                message = $"Error fetching running models: {ex.Message}",
                correlationId
            });
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Enhanced provider validation with field-level validation
    /// </summary>
    [HttpPost("validate-enhanced")]
    public Task<IActionResult> ValidateProviderEnhanced(
        [FromBody] EnhancedProviderValidationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();
            Log.Information("Enhanced provider validation for {Provider}, CorrelationId: {CorrelationId}",
                request.Provider, correlationId);

            var fieldErrors = new List<FieldValidationError>();
            var fieldValidationStatus = new Dictionary<string, bool>();

            switch (request.Provider.ToLowerInvariant())
            {
                case "openai":
                    ValidateOpenAIConfiguration(request.Configuration, fieldErrors, fieldValidationStatus);
                    break;
                case "elevenlabs":
                    ValidateElevenLabsConfiguration(request.Configuration, fieldErrors, fieldValidationStatus);
                    break;
                case "playht":
                    ValidatePlayHTConfiguration(request.Configuration, fieldErrors, fieldValidationStatus);
                    break;
                default:
                    return Task.FromResult<IActionResult>(BadRequest(new EnhancedProviderValidationResponse(
                        false,
                        "Invalid",
                        request.Provider,
                        new List<FieldValidationError> {
                            new("Provider", "UNKNOWN_PROVIDER", $"Provider '{request.Provider}' is not supported")
                        },
                        null,
                        $"Provider '{request.Provider}' is not supported",
                        correlationId
                    )));
            }

            var isValid = fieldErrors.Count == 0;
            var status = isValid ? "Valid" : "Invalid";

            // If partial validation and at least one field is valid, return partial success
            if (request.PartialValidation && fieldValidationStatus.Values.Any(v => v))
            {
                status = "PartiallyValid";
            }

            return Task.FromResult<IActionResult>(Ok(new EnhancedProviderValidationResponse(
                isValid,
                status,
                request.Provider,
                fieldErrors.Count > 0 ? fieldErrors : null,
                fieldValidationStatus,
                isValid ? "All fields validated successfully" : $"{fieldErrors.Count} field(s) have validation errors",
                correlationId
            )));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Enhanced provider validation failed for {Provider}, CorrelationId: {CorrelationId}",
                request.Provider, request.CorrelationId);
            return Task.FromResult<IActionResult>(StatusCode(500, new EnhancedProviderValidationResponse(
                false,
                "Error",
                request.Provider,
                new List<FieldValidationError> {
                    new("Internal", "VALIDATION_ERROR", ex.Message)
                },
                null,
                "Internal validation error",
                request.CorrelationId
            )));
        }
    }

    private void ValidateOpenAIConfiguration(
        Dictionary<string, string?> config,
        List<FieldValidationError> errors,
        Dictionary<string, bool> status)
    {
        // Validate API Key
        if (!config.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add(new FieldValidationError(
                "ApiKey",
                "REQUIRED",
                "API Key is required",
                "Obtain an API key from https://platform.openai.com/api-keys"
            ));
            status["ApiKey"] = false;
        }
        else if (!apiKey.StartsWith("sk-", StringComparison.Ordinal))
        {
            errors.Add(new FieldValidationError(
                "ApiKey",
                "INVALID_FORMAT",
                "OpenAI API keys must start with 'sk-'",
                "Check your API key format"
            ));
            status["ApiKey"] = false;
        }
        else
        {
            status["ApiKey"] = true;
        }

        // Validate Base URL if provided
        if (config.TryGetValue("BaseUrl", out var baseUrl) && !string.IsNullOrWhiteSpace(baseUrl))
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                errors.Add(new FieldValidationError(
                    "BaseUrl",
                    "INVALID_URL",
                    "Base URL must be a valid URL",
                    "Use format: https://api.openai.com/v1"
                ));
                status["BaseUrl"] = false;
            }
            else
            {
                status["BaseUrl"] = true;
            }
        }
    }

    private void ValidateElevenLabsConfiguration(
        Dictionary<string, string?> config,
        List<FieldValidationError> errors,
        Dictionary<string, bool> status)
    {
        if (!config.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add(new FieldValidationError(
                "ApiKey",
                "REQUIRED",
                "API Key is required",
                "Obtain an API key from https://elevenlabs.io/app/settings"
            ));
            status["ApiKey"] = false;
        }
        else
        {
            status["ApiKey"] = true;
        }
    }

    private void ValidatePlayHTConfiguration(
        Dictionary<string, string?> config,
        List<FieldValidationError> errors,
        Dictionary<string, bool> status)
    {
        if (!config.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            errors.Add(new FieldValidationError(
                "ApiKey",
                "REQUIRED",
                "API Key is required",
                "Obtain an API key from https://play.ht/app/api-access"
            ));
            status["ApiKey"] = false;
        }
        else if (!config.TryGetValue("UserId", out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            errors.Add(new FieldValidationError(
                "UserId",
                "REQUIRED",
                "User ID is required for PlayHT",
                "Find your User ID in the PlayHT API settings"
            ));
            status["UserId"] = false;
        }
        else
        {
            status["ApiKey"] = true;
            status["UserId"] = true;
        }
    }

    /// <summary>
    /// Save partial provider configuration
    /// </summary>
    [HttpPost("save-partial-config")]
    public async Task<IActionResult> SavePartialConfiguration(
        [FromBody] SavePartialConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();
            Log.Information("Saving partial configuration for {Provider}, CorrelationId: {CorrelationId}",
                request.Provider, correlationId);

            foreach (var kvp in request.PartialConfiguration)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    var keyName = $"{request.Provider}_{kvp.Key}";
                    await _secureStorageService.SaveApiKeyAsync(keyName, kvp.Value)
                        .ConfigureAwait(false);
                }
            }

            return Ok(new
            {
                success = true,
                message = "Partial configuration saved successfully",
                savedFields = request.PartialConfiguration.Keys.Where(k =>
                    !string.IsNullOrWhiteSpace(request.PartialConfiguration[k])).ToList(),
                correlationId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save partial configuration for {Provider}, CorrelationId: {CorrelationId}",
                request.Provider, request.CorrelationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to save partial configuration",
                detail = ex.Message,
                correlationId = request.CorrelationId
            });
        }
    }
    /// <summary>
    /// Validate a specific provider with detailed error information
    /// </summary>
    [HttpPost("{name}/validate-detailed")]
    public async Task<IActionResult> ValidateProviderDetailed(
        string name,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("[{CorrelationId}] Validating provider: {Name}", correlationId, name);

            var statusService = HttpContext.RequestServices.GetService(typeof(ProviderStatusService)) 
                as ProviderStatusService;

            if (statusService == null)
            {
                return Problem(
                    title: "Service Unavailable",
                    detail: "Provider status service is not available",
                    statusCode: 503);
            }

            var result = await statusService.ValidateProviderAsync(name, cancellationToken).ConfigureAwait(false);

            var response = new
            {
                name = result.Name,
                configured = result.Configured,
                reachable = result.Reachable,
                errorCode = result.ErrorCode,
                errorMessage = result.ErrorMessage,
                howToFix = result.HowToFix,
                lastValidated = result.LastValidated,
                category = result.Category,
                tier = result.Tier,
                success = result.Configured && result.Reachable,
                message = result.Reachable 
                    ? $"{name} is configured and reachable" 
                    : result.ErrorMessage ?? $"{name} validation failed",
                correlationId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error validating provider {Name}", correlationId, name);
            return Problem(
                title: "Provider Validation Error",
                detail: $"Failed to validate provider: {name}",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/api/errors.md#provider-validation-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Ping a specific provider to test connectivity and API key
    /// </summary>
    [HttpPost("{name}/ping")]
    public async Task<IActionResult> PingProvider(
        string name,
        [FromBody] Aura.Core.Services.Providers.ProviderPingRequest? request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("[{CorrelationId}] POST /api/providers/{Name}/ping", correlationId, name);

            var result = await _providerPingService
                .PingAsync(name, request, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.ProviderId))
            {
                result = result with { ProviderId = name };
            }

            var response = new ProviderPingResult(
                Provider: result.ProviderId,
                Attempted: result.Attempted,
                Success: result.Success,
                Message: result.Message,
                ErrorCode: result.ErrorCode,
                StatusCode: result.StatusCode,
                Endpoint: result.Endpoint,
                LatencyMs: result.LatencyMs,
                CorrelationId: correlationId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error pinging provider {Name}", correlationId, name);
            return Problem(
                title: "Provider Ping Error",
                detail: $"Failed to ping provider: {name}",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/api/errors.md#provider-ping-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Ping all configured providers to test connectivity
    /// </summary>
    [HttpGet("ping-all")]
    public async Task<IActionResult> PingAllProviders(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("[{CorrelationId}] GET /api/providers/ping-all", correlationId);

            var results = new Dictionary<string, ProviderPingResult>(StringComparer.OrdinalIgnoreCase);

            foreach (var providerId in ProviderPingService.SupportedProviders)
            {
                var result = await _providerPingService
                    .PingAsync(providerId, null, cancellationToken)
                    .ConfigureAwait(false);

                var apiResult = new ProviderPingResult(
                    Provider: string.IsNullOrWhiteSpace(result.ProviderId) ? providerId : result.ProviderId,
                    Attempted: result.Attempted,
                    Success: result.Success,
                    Message: result.Message,
                    ErrorCode: result.ErrorCode,
                    StatusCode: result.StatusCode,
                    Endpoint: result.Endpoint,
                    LatencyMs: result.LatencyMs,
                    CorrelationId: correlationId);

                results[apiResult.Provider] = apiResult;
            }

            var response = new ProviderPingAllResponse(
                Results: results,
                Timestamp: DateTime.UtcNow,
                CorrelationId: correlationId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error pinging all providers", correlationId);
            return Problem(
                title: "Provider Ping Error",
                detail: "Failed to ping providers",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/api/errors.md#provider-ping-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Get status of all configured providers with validation info
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetProvidersStatus(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("[{CorrelationId}] Getting provider status", correlationId);

            var providers = new List<ProviderConnectionStatusDto>();

            // Ping all supported providers
            foreach (var providerId in ProviderPingService.SupportedProviders)
            {
                var pingResult = await _providerPingService
                    .PingAsync(providerId, null, cancellationToken)
                    .ConfigureAwait(false);

                var status = new ProviderConnectionStatusDto(
                    Name: providerId,
                    Configured: pingResult.Attempted,
                    Reachable: pingResult.Success,
                    ErrorCode: pingResult.ErrorCode,
                    ErrorMessage: pingResult.Message,
                    HowToFix: DetermineHowToFix(providerId, pingResult.ErrorCode),
                    LastValidated: pingResult.Attempted ? DateTime.UtcNow : null,
                    Category: DetermineCategory(providerId),
                    Tier: DetermineTier(providerId)
                );

                providers.Add(status);
            }

            var configuredCount = providers.Count(p => p.Configured);
            var reachableCount = providers.Count(p => p.Reachable);

            var response = new AllProvidersStatusResponse(
                Providers: providers,
                LastUpdated: DateTime.UtcNow,
                ConfiguredCount: configuredCount,
                ReachableCount: reachableCount
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error getting provider status", correlationId);
            return Problem(
                title: "Provider Status Error",
                detail: "Failed to get provider status",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/api/errors.md#provider-status-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Validate a specific provider's configuration and connectivity
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateProvider(
        [FromBody] ValidateProviderKeyRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return BadRequest(new { 
                    success = false,
                    message = "Provider name is required",
                    correlationId
                });
            }

            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new { 
                    success = false,
                    message = "API key is required",
                    correlationId
                });
            }

            Log.Information(
                "[{CorrelationId}] Validating provider {Provider}",
                correlationId,
                request.Provider);

            var startTime = DateTime.UtcNow;
            var testResult = await _keyValidationService.TestApiKeyAsync(
                request.Provider.ToLowerInvariant(),
                request.ApiKey,
                cancellationToken).ConfigureAwait(false);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var errorCode = testResult.IsValid ? null : DetermineErrorCode(testResult.Message);
            var howToFix = testResult.IsValid ? new List<string>() : DetermineHowToFix(request.Provider, errorCode);

            var status = new ProviderConnectionStatusDto(
                Name: request.Provider,
                Configured: true,
                Reachable: testResult.IsValid,
                ErrorCode: errorCode,
                ErrorMessage: testResult.Message,
                HowToFix: howToFix,
                LastValidated: DateTime.UtcNow,
                Category: DetermineCategory(request.Provider),
                Tier: DetermineTier(request.Provider)
            );

            var response = new ValidateProviderConnectionResponse(
                Status: status,
                Success: testResult.IsValid,
                Message: testResult.Message ?? (testResult.IsValid ? "Provider validated successfully" : "Provider validation failed")
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error validating provider {Provider}", 
                correlationId, request.Provider);
            return Problem(
                title: "Provider Validation Error",
                detail: "An unexpected error occurred while validating the provider",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/api/errors.md#provider-validation-error",
                instance: correlationId);
        }
    }

    private static string DetermineCategory(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" or "anthropic" or "gemini" or "azureopenai" or "ollama" => "LLM",
            "elevenlabs" or "playht" => "TTS",
            "stabilityai" or "stablediffusion" => "Image",
            "pexels" or "pixabay" or "unsplash" => "Stock Media",
            _ => "Unknown"
        };
    }

    private static string DetermineTier(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" or "anthropic" or "gemini" or "elevenlabs" or "playht" => "Premium",
            "ollama" => "Free/Local",
            "pexels" or "pixabay" or "unsplash" => "Free",
            _ => "Unknown"
        };
    }

    private static string? DetermineErrorCode(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return null;

        var lowerMessage = errorMessage.ToLowerInvariant();

        if (lowerMessage.Contains("not configured") || lowerMessage.Contains("api key") || lowerMessage.Contains("key is required"))
            return "ProviderNotConfigured";
        if (lowerMessage.Contains("invalid") || lowerMessage.Contains("unauthorized") || lowerMessage.Contains("forbidden"))
            return "ProviderKeyInvalid";
        if (lowerMessage.Contains("timeout") || lowerMessage.Contains("timed out"))
            return "ProviderNetworkTimeout";
        if (lowerMessage.Contains("rate limit") || lowerMessage.Contains("too many requests"))
            return "ProviderRateLimited";
        if (lowerMessage.Contains("network") || lowerMessage.Contains("connection"))
            return "ProviderNetworkError";

        return "ProviderError";
    }

    private static List<string> DetermineHowToFix(string provider, string? errorCode)
    {
        var howToFix = new List<string>();

        switch (errorCode)
        {
            case "ProviderNotConfigured":
            case "ProviderKeyMissing":
                howToFix.Add($"Configure your {provider} API key in Settings");
                howToFix.Add($"Get an API key from the {provider} website");
                break;

            case "ProviderKeyInvalid":
                howToFix.Add("Check your API key for typos");
                howToFix.Add("Verify the key is still valid");
                howToFix.Add("Generate a new API key if needed");
                break;

            case "ProviderNetworkTimeout":
            case "ProviderNetworkError":
                howToFix.Add("Check your internet connection");
                howToFix.Add("Verify the provider service is accessible");
                howToFix.Add("Try again in a few moments");
                break;

            case "ProviderRateLimited":
                howToFix.Add("Wait a few minutes before trying again");
                howToFix.Add("Check your usage limits");
                howToFix.Add("Consider upgrading your plan");
                break;

            case "ProviderError":
                howToFix.Add("Check the application logs for details");
                howToFix.Add("Try again later");
                break;
        }

        return howToFix;
    }
}

/// <summary>
/// Provider configuration response for JSON deserialization
/// </summary>
internal sealed class ProviderConfigurationResponse
{
    public List<ProviderItem> Providers { get; set; } = new();
}

/// <summary>
/// Provider item for JSON deserialization
/// </summary>
internal sealed class ProviderItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Represents a provider's capability and availability status
/// </summary>
public class ProviderCapability
{
    public string Name { get; set; } = string.Empty;
    public bool Available { get; set; }
    public string[] ReasonCodes { get; set; } = Array.Empty<string>();
    public ProviderRequirements Requirements { get; set; } = new();
}

/// <summary>
/// Represents requirements for a provider
/// </summary>
public class ProviderRequirements
{
    public string[] NeedsKey { get; set; } = Array.Empty<string>();
    public string? NeedsGPU { get; set; }
    public int? MinVRAMMB { get; set; }
    public string[] Os { get; set; } = Array.Empty<string>();
}
