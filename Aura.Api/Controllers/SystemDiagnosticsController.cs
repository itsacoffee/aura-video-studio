using Aura.Core.Errors;
using Aura.Core.Providers;
using Aura.Core.Services.FFmpeg;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// System and network diagnostics endpoints for troubleshooting connectivity and configuration issues
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemDiagnosticsController : ControllerBase
{
    private readonly ILogger<SystemDiagnosticsController> _logger;
    private readonly IFFmpegStatusService? _ffmpegStatusService;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly Aura.Core.Configuration.IFfmpegConfigurationService? _ffmpegConfigurationService;
    private readonly Aura.Core.Configuration.ProviderSettings? _providerSettings;

    public SystemDiagnosticsController(
        ILogger<SystemDiagnosticsController> logger,
        IConfiguration configuration,
        IFFmpegStatusService? ffmpegStatusService = null,
        IHttpClientFactory? httpClientFactory = null,
        Aura.Core.Configuration.IFfmpegConfigurationService? ffmpegConfigurationService = null,
        Aura.Core.Configuration.ProviderSettings? providerSettings = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _ffmpegStatusService = ffmpegStatusService;
        _httpClientFactory = httpClientFactory;
        _ffmpegConfigurationService = ffmpegConfigurationService;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Get comprehensive network and system diagnostics
    /// </summary>
    /// <remarks>
    /// Checks:
    /// - Backend health and configuration
    /// - FFmpeg status
    /// - Provider connectivity (if configured)
    /// - CORS configuration
    /// - Base URL configuration
    /// 
    /// Always returns 200 OK with diagnostic results, even if checks fail.
    /// </remarks>
    [HttpGet("network/diagnostics")]
    [ProducesResponseType(typeof(SystemDiagnosticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetworkDiagnostics(CancellationToken ct = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("[{CorrelationId}] GET /api/system/network/diagnostics", correlationId);

        try
        {
            var diagnostics = new SystemDiagnosticsResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                BackendReachable = true, // We're responding, so backend is reachable
                BackendVersion = GetBackendVersion(),
                Configuration = await CheckConfigurationAsync(ct).ConfigureAwait(false),
                FFmpeg = await CheckFFmpegStatusAsync(ct).ConfigureAwait(false),
                Providers = await CheckProvidersAsync(ct).ConfigureAwait(false),
                Network = CheckNetworkConfiguration()
            };

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error generating diagnostics", correlationId);

            // Even if diagnostics fail, return a stable response
            return Ok(new SystemDiagnosticsResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                BackendReachable = true,
                BackendVersion = "unknown",
                Configuration = new ConfigurationCheck
                {
                    IsValid = false,
                    ErrorCode = "ERR999_UnknownError",
                    ErrorMessage = "Failed to check configuration",
                    Issues = new List<string> { ex.Message }
                },
                FFmpeg = new FFmpegCheck
                {
                    Installed = false,
                    Valid = false,
                    ErrorCode = "ERR999_UnknownError",
                    ErrorMessage = "Failed to check FFmpeg status"
                },
                Providers = new List<ProviderCheck>(),
                Network = new NetworkCheck
                {
                    CorsConfigured = false,
                    BaseUrlConfigured = false,
                    Issues = new List<string> { "Failed to check network configuration" }
                }
            });
        }
    }

    private string GetBackendVersion()
    {
        try
        {
            var assembly = typeof(SystemDiagnosticsController).Assembly;
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private Task<ConfigurationCheck> CheckConfigurationAsync(CancellationToken ct)
    {
        var issues = new List<string>();
        var isValid = true;

        try
        {
            // Check for required configuration sections
            var requiredSections = new[] { "Logging", "AllowedHosts" };
            foreach (var section in requiredSections)
            {
                if (_configuration.GetSection(section) == null || !_configuration.GetSection(section).Exists())
                {
                    issues.Add($"Missing required configuration section: {section}");
                    isValid = false;
                }
            }

            // Check CORS configuration
            var corsSection = _configuration.GetSection("Cors");
            if (corsSection.Exists())
            {
                var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>();
                if (allowedOrigins == null || allowedOrigins.Length == 0)
                {
                    issues.Add("CORS AllowedOrigins is not configured or empty");
                    isValid = false;
                }
            }
            else
            {
                issues.Add("CORS configuration section not found");
                isValid = false;
            }

            return Task.FromResult(new ConfigurationCheck
            {
                IsValid = isValid,
                ErrorCode = isValid ? null : ConfigurationErrorCodes.InvalidConfiguration,
                ErrorMessage = isValid ? null : "Configuration validation failed",
                Issues = issues
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking configuration");
            return Task.FromResult(new ConfigurationCheck
            {
                IsValid = false,
                ErrorCode = ConfigurationErrorCodes.InvalidConfiguration,
                ErrorMessage = "Exception while checking configuration",
                Issues = new List<string> { ex.Message }
            });
        }
    }

    private async Task<FFmpegCheck> CheckFFmpegStatusAsync(CancellationToken ct)
    {
        try
        {
            if (_ffmpegStatusService == null)
            {
                return new FFmpegCheck
                {
                    Installed = false,
                    Valid = false,
                    ErrorCode = "ServiceNotAvailable",
                    ErrorMessage = "FFmpeg status service not configured"
                };
            }

            var status = await _ffmpegStatusService.GetStatusAsync(ct).ConfigureAwait(false);

            return new FFmpegCheck
            {
                Installed = status.Installed,
                Valid = status.Valid,
                Version = status.Version,
                Path = status.Path,
                ErrorCode = status.ErrorCode,
                ErrorMessage = status.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking FFmpeg status");
            return new FFmpegCheck
            {
                Installed = false,
                Valid = false,
                ErrorCode = "ERR999_UnknownError",
                ErrorMessage = $"Exception while checking FFmpeg: {ex.Message}"
            };
        }
    }

    private async Task<List<ProviderCheck>> CheckProvidersAsync(CancellationToken ct)
    {
        var providers = new List<ProviderCheck>();

        try
        {
            // Check configured providers
            var providerSection = _configuration.GetSection("Providers");
            if (!providerSection.Exists())
            {
                return providers;
            }

            // Common provider endpoints to check
            var providerEndpoints = new Dictionary<string, string>
            {
                ["OpenAI"] = "https://api.openai.com/v1/models",
                ["Anthropic"] = "https://api.anthropic.com/v1/messages",
                ["ElevenLabs"] = "https://api.elevenlabs.io/v1/voices",
                ["StabilityAI"] = "https://api.stability.ai/v1/engines/list"
            };

            foreach (var provider in providerEndpoints)
            {
                var providerCheck = await CheckProviderConnectivity(provider.Key, provider.Value, ct).ConfigureAwait(false);
                providers.Add(providerCheck);
            }

            return providers;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking providers");
            return providers;
        }
    }

    private async Task<ProviderCheck> CheckProviderConnectivity(string providerName, string endpoint, CancellationToken ct)
    {
        try
        {
            if (_httpClientFactory == null)
            {
                return new ProviderCheck
                {
                    Name = providerName,
                    Reachable = false,
                    ErrorCode = "ServiceNotAvailable",
                    ErrorMessage = "HTTP client factory not configured"
                };
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            // Any response (even 401/403) means the endpoint is reachable
            var reachable = true;
            string? errorCode = null;
            string? errorMessage = null;

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                errorCode = AuthenticationErrorCodes.ApiKeyInvalid;
                errorMessage = "Provider is reachable but authentication may be required";
            }

            return new ProviderCheck
            {
                Name = providerName,
                Reachable = reachable,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
        catch (HttpRequestException ex)
        {
            return new ProviderCheck
            {
                Name = providerName,
                Reachable = false,
                ErrorCode = NetworkErrorCodes.ProviderUnavailable,
                ErrorMessage = $"Cannot reach provider: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new ProviderCheck
            {
                Name = providerName,
                Reachable = false,
                ErrorCode = NetworkErrorCodes.NetworkTimeout,
                ErrorMessage = "Provider check timed out"
            };
        }
        catch (Exception ex)
        {
            return new ProviderCheck
            {
                Name = providerName,
                Reachable = false,
                ErrorCode = NetworkErrorCodes.NetworkUnreachable,
                ErrorMessage = $"Error checking provider: {ex.Message}"
            };
        }
    }

    private NetworkCheck CheckNetworkConfiguration()
    {
        var issues = new List<string>();
        var corsConfigured = false;
        var baseUrlConfigured = false;

        try
        {
            // Check CORS configuration
            var corsSection = _configuration.GetSection("Cors");
            if (corsSection.Exists())
            {
                var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>();
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    corsConfigured = true;
                }
                else
                {
                    issues.Add("CORS AllowedOrigins is empty");
                }
            }
            else
            {
                issues.Add("CORS configuration not found");
            }

            // Check base URL/host configuration
            var urls = _configuration.GetValue<string>("Urls");
            var allowedHosts = _configuration.GetValue<string>("AllowedHosts");
            
            if (!string.IsNullOrEmpty(urls) || !string.IsNullOrEmpty(allowedHosts))
            {
                baseUrlConfigured = true;
            }
            else
            {
                issues.Add("Base URL or AllowedHosts not configured");
            }

            return new NetworkCheck
            {
                CorsConfigured = corsConfigured,
                BaseUrlConfigured = baseUrlConfigured,
                Issues = issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking network configuration");
            return new NetworkCheck
            {
                CorsConfigured = false,
                BaseUrlConfigured = false,
                Issues = new List<string> { $"Error checking network configuration: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Get FFmpeg configuration diagnostics (unified configuration status)
    /// </summary>
    /// <remarks>
    /// Shows the effective FFmpeg configuration from the unified configuration service.
    /// Useful for debugging FFmpeg detection and configuration issues.
    /// 
    /// Available in all environments for troubleshooting.
    /// </remarks>
    [HttpGet("diagnostics/ffmpeg-config")]
    [ProducesResponseType(typeof(FfmpegConfigDiagnostics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFfmpegConfigDiagnostics(CancellationToken ct = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("[{CorrelationId}] GET /api/system/diagnostics/ffmpeg-config", correlationId);

        try
        {
            if (_ffmpegConfigurationService == null)
            {
                return Ok(new FfmpegConfigDiagnostics
                {
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow,
                    Available = false,
                    ErrorMessage = "FFmpeg configuration service not available"
                });
            }

            var config = await _ffmpegConfigurationService.GetEffectiveConfigurationAsync(ct).ConfigureAwait(false);

            return Ok(new FfmpegConfigDiagnostics
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Available = true,
                Mode = config.Mode.ToString(),
                Path = config.Path,
                IsValid = config.IsValid,
                Source = config.Source,
                LastValidatedAt = config.LastValidatedAt,
                ValidationResult = config.LastValidationResult.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting FFmpeg config diagnostics", correlationId);
            return Ok(new FfmpegConfigDiagnostics
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Available = false,
                ErrorMessage = $"Error retrieving FFmpeg configuration: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get provider configuration diagnostics (non-secret snapshot)
    /// </summary>
    /// <remarks>
    /// Shows current provider configuration URLs and settings (no API keys).
    /// Useful for debugging provider connectivity and configuration issues.
    /// 
    /// Available in all environments for troubleshooting.
    /// </remarks>
    [HttpGet("diagnostics/providers-config")]
    [ProducesResponseType(typeof(ProviderConfigDiagnostics), StatusCodes.Status200OK)]
    public IActionResult GetProvidersConfigDiagnostics()
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("[{CorrelationId}] GET /api/system/diagnostics/providers-config", correlationId);

        try
        {
            if (_providerSettings == null)
            {
                return Ok(new ProviderConfigDiagnostics
                {
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow,
                    Available = false,
                    ErrorMessage = "Provider settings service not available"
                });
            }

            // Get non-secret configuration snapshot
            var snapshot = new ProviderConfigSnapshot
            {
                OpenAI = new OpenAIConfig
                {
                    Endpoint = _providerSettings.GetOpenAiEndpoint(),
                    HasApiKey = !string.IsNullOrWhiteSpace(_providerSettings.GetOpenAiApiKey())
                },
                Ollama = new OllamaConfig
                {
                    Url = _providerSettings.GetOllamaUrl(),
                    Model = _providerSettings.GetOllamaModel(),
                    ExecutablePath = _providerSettings.GetOllamaExecutablePath()
                },
                StableDiffusion = new StableDiffusionConfig
                {
                    Url = _providerSettings.GetStableDiffusionUrl()
                },
                Anthropic = new AnthropicConfig
                {
                    HasApiKey = !string.IsNullOrWhiteSpace(_providerSettings.GetAnthropicKey())
                },
                Gemini = new GeminiConfig
                {
                    HasApiKey = !string.IsNullOrWhiteSpace(_providerSettings.GetGeminiApiKey())
                },
                ElevenLabs = new ElevenLabsConfig
                {
                    HasApiKey = !string.IsNullOrWhiteSpace(_providerSettings.GetElevenLabsApiKey())
                },
                Azure = new AzureConfig
                {
                    SpeechRegion = _providerSettings.GetAzureSpeechRegion(),
                    HasSpeechKey = !string.IsNullOrWhiteSpace(_providerSettings.GetAzureSpeechKey()),
                    HasOpenAIKey = !string.IsNullOrWhiteSpace(_providerSettings.GetAzureOpenAiApiKey()),
                    OpenAIEndpoint = _providerSettings.GetAzureOpenAiEndpoint()
                },
                Paths = new PathsConfig
                {
                    PortableRoot = _providerSettings.GetPortableRootPath(),
                    ToolsDirectory = _providerSettings.GetToolsDirectory(),
                    AuraDataDirectory = _providerSettings.GetAuraDataDirectory(),
                    ProjectsDirectory = _providerSettings.GetProjectsDirectory(),
                    OutputDirectory = _providerSettings.GetOutputDirectory()
                }
            };

            return Ok(new ProviderConfigDiagnostics
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Available = true,
                Configuration = snapshot
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting provider config diagnostics", correlationId);
            return Ok(new ProviderConfigDiagnostics
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Available = false,
                ErrorMessage = $"Error retrieving provider configuration: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Response model for system diagnostics
/// </summary>
public class SystemDiagnosticsResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool BackendReachable { get; set; }
    public string BackendVersion { get; set; } = string.Empty;
    public ConfigurationCheck Configuration { get; set; } = new();
    public FFmpegCheck FFmpeg { get; set; } = new();
    public List<ProviderCheck> Providers { get; set; } = new();
    public NetworkCheck Network { get; set; } = new();
}

public class ConfigurationCheck
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class FFmpegCheck
{
    public bool Installed { get; set; }
    public bool Valid { get; set; }
    public string? Version { get; set; }
    public string? Path { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ProviderCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Reachable { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NetworkCheck
{
    public bool CorsConfigured { get; set; }
    public bool BaseUrlConfigured { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// FFmpeg configuration diagnostics response
/// </summary>
public class FfmpegConfigDiagnostics
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Available { get; set; }
    public string? Mode { get; set; }
    public string? Path { get; set; }
    public bool IsValid { get; set; }
    public string? Source { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public string? ValidationResult { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Provider configuration diagnostics response
/// </summary>
public class ProviderConfigDiagnostics
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Available { get; set; }
    public ProviderConfigSnapshot? Configuration { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ProviderConfigSnapshot
{
    public OpenAIConfig OpenAI { get; set; } = new();
    public OllamaConfig Ollama { get; set; } = new();
    public StableDiffusionConfig StableDiffusion { get; set; } = new();
    public AnthropicConfig Anthropic { get; set; } = new();
    public GeminiConfig Gemini { get; set; } = new();
    public ElevenLabsConfig ElevenLabs { get; set; } = new();
    public AzureConfig Azure { get; set; } = new();
    public PathsConfig Paths { get; set; } = new();
}

public class OpenAIConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
}

public class OllamaConfig
{
    public string Url { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
}

public class StableDiffusionConfig
{
    public string Url { get; set; } = string.Empty;
}

public class AnthropicConfig
{
    public bool HasApiKey { get; set; }
}

public class GeminiConfig
{
    public bool HasApiKey { get; set; }
}

public class ElevenLabsConfig
{
    public bool HasApiKey { get; set; }
}

public class AzureConfig
{
    public string SpeechRegion { get; set; } = string.Empty;
    public bool HasSpeechKey { get; set; }
    public bool HasOpenAIKey { get; set; }
    public string? OpenAIEndpoint { get; set; }
}

public class PathsConfig
{
    public string PortableRoot { get; set; } = string.Empty;
    public string ToolsDirectory { get; set; } = string.Empty;
    public string AuraDataDirectory { get; set; } = string.Empty;
    public string ProjectsDirectory { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
}
