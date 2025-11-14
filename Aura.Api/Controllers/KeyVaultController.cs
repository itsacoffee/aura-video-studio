using System;
using System.Collections.Generic;
using System.IO;
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
    private readonly Aura.Core.Configuration.IKeyStore _keyStore;

    public KeyVaultController(
        ILogger<KeyVaultController> _logger,
        ISecureStorageService secureStorage,
        IKeyValidationService keyValidator,
        Aura.Core.Configuration.IKeyStore keyStore)
    {
        this._logger = _logger;
        _secureStorage = secureStorage;
        _keyValidator = keyValidator;
        _keyStore = keyStore;
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

            await _secureStorage.SaveApiKeyAsync(request.Provider, request.ApiKey).ConfigureAwait(false);
            
            // Invalidate KeyStore cache so validation immediately sees the new key
            _keyStore.Reload();
            _logger.LogDebug("KeyStore cache reloaded after setting API key for {Provider}", sanitizedProvider);

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
            var providers = await _secureStorage.GetConfiguredProvidersAsync().ConfigureAwait(false);

            var providersWithStatus = new List<object>();
            foreach (var provider in providers)
            {
                var hasKey = await _secureStorage.HasApiKeyAsync(provider).ConfigureAwait(false);
                var key = await _secureStorage.GetApiKeyAsync(provider).ConfigureAwait(false);
                
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
                apiKey = await _secureStorage.GetApiKeyAsync(request.Provider).ConfigureAwait(false);
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

            var result = await _keyValidator.TestApiKeyAsync(request.Provider, apiKey, ct).ConfigureAwait(false);

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

            var hasExisting = await _secureStorage.HasApiKeyAsync(request.Provider).ConfigureAwait(false);
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
                    request.Provider, request.NewApiKey, ct).ConfigureAwait(false);
                
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

            await _secureStorage.SaveApiKeyAsync(request.Provider, request.NewApiKey).ConfigureAwait(false);
            
            // Invalidate KeyStore cache so validation immediately sees the new key
            _keyStore.Reload();

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

            var hasKey = await _secureStorage.HasApiKeyAsync(provider).ConfigureAwait(false);
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

            await _secureStorage.DeleteApiKeyAsync(provider).ConfigureAwait(false);
            
            // Invalidate KeyStore cache after deletion
            _keyStore.Reload();

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
    public async Task<IActionResult> GetKeyVaultInfo()
    {
        try
        {
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var storagePath = Path.Combine(localAppData, "Aura", "secure", "apikeys.dat");

            var configuredKeys = await _secureStorage.GetConfiguredProvidersAsync().ConfigureAwait(false);

            DateTime? lastModified = null;
            var fileExists = System.IO.File.Exists(storagePath);
            if (fileExists)
            {
                lastModified = System.IO.File.GetLastWriteTimeUtc(storagePath);
            }

            var response = new Models.KeyVaultInfoResponse
            {
                Success = true,
                Encryption = new Models.KeyVaultEncryptionInfo
                {
                    Platform = isWindows ? "Windows" : "Linux/macOS",
                    Method = isWindows ? "DPAPI (Data Protection API)" : "AES-256",
                    Scope = isWindows ? "CurrentUser" : "Machine-specific key"
                },
                Storage = new Models.KeyVaultStorageInfo
                {
                    Location = isWindows 
                        ? "%LOCALAPPDATA%\\Aura\\secure\\apikeys.dat"
                        : "$HOME/.local/share/Aura/secure/apikeys.dat",
                    Encrypted = true,
                    FileExists = fileExists
                },
                Metadata = new Models.KeyVaultMetadata
                {
                    ConfiguredKeysCount = configuredKeys.Count,
                    LastModified = lastModified?.ToString("o")
                },
                Status = fileExists && configuredKeys.Count > 0 ? "healthy" : "empty"
            };

            return Ok(response);
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

    /// <summary>
    /// Run diagnostics self-check for redaction and security
    /// </summary>
    [HttpPost("diagnostics")]
    public async Task<IActionResult> RunDiagnostics(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Running KeyVault diagnostics, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var checks = new List<string>();
            var allPassed = true;

            var providers = await _secureStorage.GetConfiguredProvidersAsync().ConfigureAwait(false);
            checks.Add($"✓ Storage accessible - {providers.Count} provider(s) configured");

            foreach (var provider in providers)
            {
                var key = await _secureStorage.GetApiKeyAsync(provider).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var masked = SecretMaskingService.MaskApiKey(key);
                    
                    if (masked.Contains("***") && !masked.Equals(key))
                    {
                        checks.Add($"✓ {provider}: Redaction working (key properly masked)");
                    }
                    else
                    {
                        checks.Add($"✗ {provider}: Redaction failed (key may be exposed)");
                        allPassed = false;
                    }
                }
            }

            if (providers.Count == 0)
            {
                checks.Add("ℹ No API keys configured yet");
            }

            var response = new Models.KeyVaultDiagnosticsResponse
            {
                Success = true,
                RedactionCheckPassed = allPassed,
                Checks = checks,
                Message = allPassed 
                    ? "All diagnostics passed. Redaction is working correctly."
                    : "Some diagnostics failed. Review the checks above."
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running diagnostics, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to run diagnostics",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get validation status for all configured API keys
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetAllKeysStatus(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting validation status for all keys, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var providers = await _secureStorage.GetConfiguredProvidersAsync().ConfigureAwait(false);
            var statuses = new Dictionary<string, KeyStatusResponse>();
            var validCount = 0;
            var invalidCount = 0;
            var pendingCount = 0;

            foreach (var provider in providers)
            {
                var hasKey = await _secureStorage.HasApiKeyAsync(provider).ConfigureAwait(false);
                if (!hasKey)
                {
                    continue;
                }

                var statusResponse = new KeyStatusResponse
                {
                    Success = true,
                    Provider = provider,
                    Status = "NotValidated",
                    Message = "API key configured but not yet validated",
                    CanManuallyRevalidate = true
                };

                statuses[provider] = statusResponse;
            }

            var response = new AllKeysStatusResponse
            {
                Success = true,
                Statuses = statuses,
                TotalKeys = providers.Count,
                ValidKeys = validCount,
                InvalidKeys = invalidCount,
                PendingValidation = pendingCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys status, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get keys status",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get validation status for a specific provider's API key
    /// </summary>
    [HttpGet("status/{provider}")]
    public async Task<IActionResult> GetKeyStatus(string provider, CancellationToken ct)
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

            var hasKey = await _secureStorage.HasApiKeyAsync(provider).ConfigureAwait(false);
            if (!hasKey)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"No API key configured for {provider}"
                });
            }

            var statusResponse = new KeyStatusResponse
            {
                Success = true,
                Provider = provider,
                Status = "NotValidated",
                Message = "API key configured but not yet validated",
                CanManuallyRevalidate = true
            };

            return Ok(statusResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key status for {Provider}, CorrelationId: {CorrelationId}",
                provider, HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get key status",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Manually revalidate an API key (user-initiated)
    /// </summary>
    [HttpPost("revalidate")]
    public async Task<IActionResult> RevalidateKey(
        [FromBody] RevalidateKeyRequest request,
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
                apiKey = request.ApiKey;
            }
            else
            {
                apiKey = await _secureStorage.GetApiKeyAsync(request.Provider).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No API key configured for {request.Provider}"
                    });
                }
            }

            var sanitizedProvider = SecretMaskingService.SanitizeForLogging(request.Provider);
            _logger.LogInformation(
                "AUDIT: Manual revalidation initiated for provider: {Provider}, User: {User}, CorrelationId: {CorrelationId}",
                sanitizedProvider,
                "system",
                HttpContext.TraceIdentifier);

            var result = await _keyValidator.TestApiKeyAsync(request.Provider, apiKey, ct).ConfigureAwait(false);

            var statusResponse = new KeyStatusResponse
            {
                Success = result.IsValid,
                Provider = request.Provider,
                Status = result.IsValid ? "Valid" : "Invalid",
                Message = result.Message,
                LastValidated = result.IsValid ? DateTime.UtcNow : null,
                Details = result.Details,
                CanManuallyRevalidate = true
            };

            _logger.LogInformation(
                "AUDIT: Manual revalidation completed for {Provider}. Result: {Result}",
                sanitizedProvider,
                result.IsValid ? "Valid" : "Invalid");

            return Ok(statusResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revalidating key, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to revalidate key",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }
}
