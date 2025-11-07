using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models.Providers;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Runtime.InteropServices;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IHardwareDetector _hardwareDetector;
    private readonly IKeyStore _keyStore;
    private readonly LlmProviderRecommendationService? _recommendationService;
    private readonly ProviderHealthMonitoringService? _healthMonitoringService;
    private readonly ProviderCostTrackingService? _costTrackingService;
    private readonly ProviderSettings _settings;
    private readonly OpenAIKeyValidationService _openAIValidationService;
    private readonly Aura.Core.Services.IKeyValidationService _keyValidationService;
    private readonly Aura.Core.Services.ISecureStorageService _secureStorageService;

    public ProvidersController(
        IHardwareDetector hardwareDetector, 
        IKeyStore keyStore,
        ProviderSettings settings,
        OpenAIKeyValidationService openAIValidationService,
        Aura.Core.Services.IKeyValidationService keyValidationService,
        Aura.Core.Services.ISecureStorageService secureStorageService,
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
            var systemProfile = await _hardwareDetector.DetectSystemAsync();
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
                cancellationToken: HttpContext.RequestAborted);

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
    public async Task<IActionResult> TestConnection([FromBody] TestProviderConnectionRequest request)
    {
        try
        {
            return Ok(new { 
                message = "Provider connection test - implementation in progress",
                provider = request.ProviderName,
                hasApiKey = !string.IsNullOrEmpty(request.ApiKey)
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error testing provider connection");
            return Problem($"Error testing provider connection: {ex.Message}", statusCode: 500);
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
                cancellationToken);

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
                    ResponseTimeMs: result.ResponseTimeMs));

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating OpenAI API key, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Validation Error",
                detail: "An unexpected error occurred while validating the API key.",
                statusCode: 500,
                type: "https://docs.aura.studio/errors/validation-error",
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
            var result = await _keyValidationService.TestApiKeyAsync("elevenlabs", request.ApiKey, cancellationToken);
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
                type: "https://docs.aura.studio/errors/validation-error",
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
            var result = await _keyValidationService.TestApiKeyAsync("playht", request.ApiKey, cancellationToken);
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
                type: "https://docs.aura.studio/errors/validation-error",
                instance: correlationId);
        }
    }

    /// <summary>
    /// Get provider status dashboard showing which providers are configured
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetProviderStatus()
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            Log.Information("Getting provider status, CorrelationId: {CorrelationId}", correlationId);

            var configuredProviders = await _secureStorageService.GetConfiguredProvidersAsync();
            
            var providerStatuses = new List<ProviderStatusDto>
            {
                new ProviderStatusDto(
                    Name: "OpenAI",
                    IsConfigured: configuredProviders.Contains("openai"),
                    IsAvailable: configuredProviders.Contains("openai"),
                    Status: configuredProviders.Contains("openai") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "Anthropic",
                    IsConfigured: configuredProviders.Contains("anthropic"),
                    IsAvailable: configuredProviders.Contains("anthropic"),
                    Status: configuredProviders.Contains("anthropic") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "Gemini",
                    IsConfigured: configuredProviders.Contains("gemini") || configuredProviders.Contains("google"),
                    IsAvailable: configuredProviders.Contains("gemini") || configuredProviders.Contains("google"),
                    Status: configuredProviders.Contains("gemini") || configuredProviders.Contains("google") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "ElevenLabs",
                    IsConfigured: configuredProviders.Contains("elevenlabs"),
                    IsAvailable: configuredProviders.Contains("elevenlabs"),
                    Status: configuredProviders.Contains("elevenlabs") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "PlayHT",
                    IsConfigured: configuredProviders.Contains("playht"),
                    IsAvailable: configuredProviders.Contains("playht"),
                    Status: configuredProviders.Contains("playht") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "Pexels",
                    IsConfigured: configuredProviders.Contains("pexels"),
                    IsAvailable: configuredProviders.Contains("pexels"),
                    Status: configuredProviders.Contains("pexels") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "StabilityAI",
                    IsConfigured: configuredProviders.Contains("stabilityai") || configuredProviders.Contains("stability"),
                    IsAvailable: configuredProviders.Contains("stabilityai") || configuredProviders.Contains("stability"),
                    Status: configuredProviders.Contains("stabilityai") || configuredProviders.Contains("stability") ? "Configured" : "Not Configured"),
                new ProviderStatusDto(
                    Name: "RuleBased",
                    IsConfigured: true,
                    IsAvailable: true,
                    Status: "Always Available (Offline)")
            };

            return Ok(providerStatuses);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting provider status, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Provider Status Error",
                detail: "An unexpected error occurred while getting provider status.",
                statusCode: 500,
                type: "https://docs.aura.studio/errors/provider-status-error",
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
