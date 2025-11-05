using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Secure API key management with encryption at rest
/// </summary>
[ApiController]
[Route("api/keys")]
public class KeyVaultController : ControllerBase
{
    private readonly ILogger<KeyVaultController> _logger;
    private readonly ISecureStorageService _secureStorage;
    private readonly IKeyValidationService _keyValidator;

    public KeyVaultController(
        ILogger<KeyVaultController> _logger,
        ISecureStorageService secureStorage,
        IKeyValidationService keyValidator)
    {
        this._logger = _logger;
        _secureStorage = secureStorage;
        _keyValidator = keyValidator;
    }

    /// <summary>
    /// Set or update an API key for a provider (encrypted at rest)
    /// </summary>
    [HttpPost("set")]
    public async Task<IActionResult> SetApiKey(
        [FromBody] SetApiKeyRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Provider name is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "API key is required"
                });
            }

            var sanitizedProvider = SecretMaskingService.SanitizeForLogging(request.Provider);
            _logger.LogInformation(
                "Setting API key for provider: {Provider}, CorrelationId: {CorrelationId}",
                sanitizedProvider, HttpContext.TraceIdentifier);

            await _secureStorage.SaveApiKeyAsync(request.Provider, request.ApiKey);

            return Ok(new
            {
                success = true,
                message = "API key saved securely",
                provider = request.Provider,
                masked = SecretMaskingService.MaskApiKey(request.ApiKey)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting API key, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to save API key",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get list of configured providers (without revealing keys)
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListConfiguredProviders(CancellationToken ct)
    {
        try
        {
            var providers = await _secureStorage.GetConfiguredProvidersAsync();

            var providersWithStatus = new List<object>();
            foreach (var provider in providers)
            {
                var hasKey = await _secureStorage.HasApiKeyAsync(provider);
                var key = await _secureStorage.GetApiKeyAsync(provider);
                
                providersWithStatus.Add(new
                {
                    provider,
                    configured = hasKey,
                    masked = SecretMaskingService.MaskApiKey(key)
                });
            }

            return Ok(new
            {
                success = true,
                providers = providersWithStatus,
                count = providers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing providers, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to list providers",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Test an API key by making an actual connection to the provider
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestApiKey(
        [FromBody] TestApiKeyRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Provider name is required"
                });
            }

            string? apiKey;
            if (!string.IsNullOrWhiteSpace(request.ApiKey))
            {
                // Test provided key without saving
                apiKey = request.ApiKey;
            }
            else
            {
                // Test stored key
                apiKey = await _secureStorage.GetApiKeyAsync(request.Provider);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"No API key configured for {request.Provider}"
                    });
                }
            }

            var sanitizedProvider = SecretMaskingService.SanitizeForLogging(request.Provider);
            _logger.LogInformation(
                "Testing API key for provider: {Provider}, CorrelationId: {CorrelationId}",
                sanitizedProvider, HttpContext.TraceIdentifier);

            var result = await _keyValidator.TestApiKeyAsync(request.Provider, apiKey, ct);

            return Ok(new
            {
                success = result.IsValid,
                message = result.Message,
                provider = request.Provider,
                details = result.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API key, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to test API key",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Rotate an API key (update existing key)
    /// </summary>
    [HttpPost("rotate")]
    public async Task<IActionResult> RotateApiKey(
        [FromBody] RotateApiKeyRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Provider name is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.NewApiKey))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "New API key is required"
                });
            }

            var hasExisting = await _secureStorage.HasApiKeyAsync(request.Provider);
            if (!hasExisting)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"No existing API key found for {request.Provider}. Use /api/keys/set to add a new key."
                });
            }

            var sanitizedProvider = SecretMaskingService.SanitizeForLogging(request.Provider);
            _logger.LogInformation(
                "Rotating API key for provider: {Provider}, CorrelationId: {CorrelationId}",
                sanitizedProvider, HttpContext.TraceIdentifier);

            // Optionally test new key before saving
            if (request.TestBeforeSaving)
            {
                var testResult = await _keyValidator.TestApiKeyAsync(
                    request.Provider, request.NewApiKey, ct);
                
                if (!testResult.IsValid)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "New API key failed validation test",
                        testResult = testResult.Message
                    });
                }
            }

            await _secureStorage.SaveApiKeyAsync(request.Provider, request.NewApiKey);

            return Ok(new
            {
                success = true,
                message = "API key rotated successfully",
                provider = request.Provider,
                masked = SecretMaskingService.MaskApiKey(request.NewApiKey)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating API key, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to rotate API key",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete an API key for a specific provider
    /// </summary>
    [HttpDelete("{provider}")]
    public async Task<IActionResult> DeleteApiKey(string provider, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Provider name is required"
                });
            }

            var hasKey = await _secureStorage.HasApiKeyAsync(provider);
            if (!hasKey)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"No API key found for {provider}"
                });
            }

            var sanitizedProvider = SecretMaskingService.SanitizeForLogging(provider);
            _logger.LogInformation(
                "Deleting API key for provider: {Provider}, CorrelationId: {CorrelationId}",
                sanitizedProvider, HttpContext.TraceIdentifier);

            await _secureStorage.DeleteApiKeyAsync(provider);

            return Ok(new
            {
                success = true,
                message = "API key deleted successfully",
                provider
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to delete API key",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get encryption status and storage location info
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetKeyVaultInfo()
    {
        try
        {
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);

            return Ok(new
            {
                success = true,
                encryption = new
                {
                    platform = isWindows ? "Windows" : "Linux/macOS",
                    method = isWindows ? "DPAPI (Data Protection API)" : "AES-256",
                    scope = isWindows ? "CurrentUser" : "Machine-specific key"
                },
                storage = new
                {
                    location = isWindows 
                        ? "%LOCALAPPDATA%\\Aura\\secure\\apikeys.dat"
                        : "$HOME/.local/share/Aura/secure/apikeys.dat",
                    encrypted = true
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key vault info, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get key vault info",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }
}
