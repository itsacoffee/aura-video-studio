using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aura.Core.Services.Providers.Stickiness;
using Aura.Api.Models.ApiModels.V1;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing provider profile locks
/// </summary>
[ApiController]
[Route("api/provider-lock")]
public class ProviderProfileLockController : ControllerBase
{
    private readonly ILogger<ProviderProfileLockController> _logger;
    private readonly ProviderProfileLockService _profileLockService;

    public ProviderProfileLockController(
        ILogger<ProviderProfileLockController> logger,
        ProviderProfileLockService profileLockService)
    {
        _logger = logger;
        _profileLockService = profileLockService;
    }

    /// <summary>
    /// Get the current profile lock status for a job
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetProfileLockStatus([FromQuery] string? jobId)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation(
            "Getting profile lock status for job {JobId}, CorrelationId: {CorrelationId}",
            jobId ?? "all",
            correlationId);

        try
        {
            var statistics = _profileLockService.GetStatistics();
            
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return Ok(new ProfileLockStatusResponse(
                    null,
                    false,
                    null,
                    new ProfileLockStatisticsDto(
                        statistics.TotalSessionLocks,
                        statistics.TotalProjectLocks,
                        statistics.EnabledSessionLocks,
                        statistics.EnabledProjectLocks,
                        statistics.OfflineModeLocksCount)));
            }

            var activeLock = _profileLockService.GetProfileLock(jobId);
            var hasActiveLock = activeLock != null && activeLock.IsEnabled;

            ProfileLockResponse? lockResponse = null;
            if (activeLock != null)
            {
                lockResponse = new ProfileLockResponse(
                    activeLock.JobId,
                    activeLock.ProviderName,
                    activeLock.ProviderType,
                    activeLock.IsEnabled,
                    activeLock.CreatedAt,
                    activeLock.OfflineModeEnabled,
                    activeLock.ApplicableStages,
                    new ProfileLockMetadataDto(
                        activeLock.Metadata.CreatedByUser,
                        activeLock.Metadata.Reason,
                        activeLock.Metadata.Tags,
                        activeLock.Metadata.Source,
                        activeLock.Metadata.AllowManualFallback,
                        activeLock.Metadata.MaxWaitBeforeFallbackSeconds),
                    activeLock.Metadata.Source);
            }

            return Ok(new ProfileLockStatusResponse(
                jobId,
                hasActiveLock,
                lockResponse,
                new ProfileLockStatisticsDto(
                    statistics.TotalSessionLocks,
                    statistics.TotalProjectLocks,
                    statistics.EnabledSessionLocks,
                    statistics.EnabledProjectLocks,
                    statistics.OfflineModeLocksCount)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting profile lock status, CorrelationId: {CorrelationId}",
                correlationId);
            
            return Problem(
                "Failed to retrieve profile lock status",
                statusCode: 500,
                title: "Profile Lock Status Error");
        }
    }

    /// <summary>
    /// Set a provider profile lock for a job
    /// </summary>
    [HttpPost("set")]
    public async Task<IActionResult> SetProfileLock(
        [FromBody] SetProfileLockRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation(
            "Setting profile lock for job {JobId}, Provider: {Provider}, CorrelationId: {CorrelationId}",
            request.JobId,
            request.ProviderName,
            correlationId);

        try
        {
            ProviderProfileLockMetadata? metadata = null;
            if (request.Metadata != null)
            {
                metadata = new ProviderProfileLockMetadata
                {
                    CreatedByUser = request.Metadata.CreatedByUser,
                    Reason = request.Metadata.Reason,
                    Tags = request.Metadata.Tags,
                    Source = request.Metadata.Source,
                    AllowManualFallback = request.Metadata.AllowManualFallback,
                    MaxWaitBeforeFallbackSeconds = request.Metadata.MaxWaitBeforeFallbackSeconds
                };
            }

            var profileLock = await _profileLockService.SetProfileLockAsync(
                request.JobId,
                request.ProviderName,
                request.ProviderType,
                request.IsEnabled,
                request.OfflineModeEnabled,
                request.ApplicableStages,
                metadata,
                request.IsSessionLevel,
                ct).ConfigureAwait(false);

            var response = new ProfileLockResponse(
                profileLock.JobId,
                profileLock.ProviderName,
                profileLock.ProviderType,
                profileLock.IsEnabled,
                profileLock.CreatedAt,
                profileLock.OfflineModeEnabled,
                profileLock.ApplicableStages,
                new ProfileLockMetadataDto(
                    profileLock.Metadata.CreatedByUser,
                    profileLock.Metadata.Reason,
                    profileLock.Metadata.Tags,
                    profileLock.Metadata.Source,
                    profileLock.Metadata.AllowManualFallback,
                    profileLock.Metadata.MaxWaitBeforeFallbackSeconds),
                profileLock.Metadata.Source);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error setting profile lock for job {JobId}, CorrelationId: {CorrelationId}",
                request.JobId,
                correlationId);
            
            return Problem(
                "Failed to set profile lock",
                statusCode: 500,
                title: "Profile Lock Set Error");
        }
    }

    /// <summary>
    /// Unlock a provider profile lock, allowing provider switching
    /// </summary>
    [HttpPost("unlock")]
    public IActionResult UnlockProfileLock([FromBody] UnlockProfileLockRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation(
            "Unlocking profile lock for job {JobId}, CorrelationId: {CorrelationId}",
            request.JobId,
            correlationId);

        try
        {
            var unlocked = _profileLockService.UnlockProfileLock(
                request.JobId,
                request.IsSessionLevel);

            if (!unlocked)
            {
                return NotFound(new
                {
                    title = "Profile Lock Not Found",
                    detail = $"No profile lock found for job {request.JobId}",
                    correlationId
                });
            }

            return Ok(new
            {
                jobId = request.JobId,
                unlocked = true,
                reason = request.Reason,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error unlocking profile lock for job {JobId}, CorrelationId: {CorrelationId}",
                request.JobId,
                correlationId);
            
            return Problem(
                "Failed to unlock profile lock",
                statusCode: 500,
                title: "Profile Lock Unlock Error");
        }
    }

    /// <summary>
    /// Check if a provider is compatible with offline mode
    /// </summary>
    [HttpGet("offline-compatible")]
    public IActionResult CheckOfflineCompatibility([FromQuery] string providerName)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation(
            "Checking offline compatibility for provider {Provider}, CorrelationId: {CorrelationId}",
            providerName,
            correlationId);

        try
        {
            var isCompatible = _profileLockService.IsOfflineCompatible(providerName, out var message);

            var offlineProviders = new[] { "RuleBased", "Ollama", "Windows", "Piper", "Mimic3", "LocalSD", "Stock" };

            return Ok(new OfflineCompatibilityResponse(
                providerName,
                isCompatible,
                message,
                offlineProviders));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error checking offline compatibility, CorrelationId: {CorrelationId}",
                correlationId);
            
            return Problem(
                "Failed to check offline compatibility",
                statusCode: 500,
                title: "Offline Compatibility Check Error");
        }
    }

    /// <summary>
    /// Validate a provider request against the active profile lock
    /// </summary>
    [HttpPost("validate")]
    public IActionResult ValidateProvider([FromBody] ValidateProviderRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogDebug(
            "Validating provider {Provider} for job {JobId}, stage {Stage}, CorrelationId: {CorrelationId}",
            request.ProviderName,
            request.JobId,
            request.StageName,
            correlationId);

        try
        {
            var isValid = _profileLockService.ValidateProviderRequest(
                request.JobId,
                request.ProviderName,
                request.StageName,
                request.ProviderRequiresNetwork,
                out var validationError);

            var activeLock = _profileLockService.GetProfileLock(request.JobId);
            ProfileLockResponse? lockResponse = null;
            
            if (activeLock != null)
            {
                lockResponse = new ProfileLockResponse(
                    activeLock.JobId,
                    activeLock.ProviderName,
                    activeLock.ProviderType,
                    activeLock.IsEnabled,
                    activeLock.CreatedAt,
                    activeLock.OfflineModeEnabled,
                    activeLock.ApplicableStages,
                    new ProfileLockMetadataDto(
                        activeLock.Metadata.CreatedByUser,
                        activeLock.Metadata.Reason,
                        activeLock.Metadata.Tags,
                        activeLock.Metadata.Source,
                        activeLock.Metadata.AllowManualFallback,
                        activeLock.Metadata.MaxWaitBeforeFallbackSeconds),
                    activeLock.Metadata.Source);
            }

            return Ok(new ValidateProviderResponse(
                isValid,
                validationError,
                lockResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error validating provider request, CorrelationId: {CorrelationId}",
                correlationId);
            
            return Problem(
                "Failed to validate provider request",
                statusCode: 500,
                title: "Provider Validation Error");
        }
    }

    /// <summary>
    /// Remove a profile lock completely
    /// </summary>
    [HttpDelete("{jobId}")]
    public IActionResult RemoveProfileLock(
        string jobId,
        [FromQuery] bool isSessionLevel = true)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation(
            "Removing profile lock for job {JobId}, CorrelationId: {CorrelationId}",
            jobId,
            correlationId);

        try
        {
            var removed = _profileLockService.RemoveProfileLock(jobId, isSessionLevel);

            if (!removed)
            {
                return NotFound(new
                {
                    title = "Profile Lock Not Found",
                    detail = $"No profile lock found for job {jobId}",
                    correlationId
                });
            }

            return Ok(new
            {
                jobId,
                removed = true,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error removing profile lock for job {JobId}, CorrelationId: {CorrelationId}",
                jobId,
                correlationId);
            
            return Problem(
                "Failed to remove profile lock",
                statusCode: 500,
                title: "Profile Lock Remove Error");
        }
    }
}
