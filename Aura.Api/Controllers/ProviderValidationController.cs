using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for provider validation and status management
/// </summary>
[ApiController]
[Route("api/v1/providers")]
public class ProviderValidationController : ControllerBase
{
    private readonly ILogger<ProviderValidationController> _logger;
    private readonly ProviderValidationService _validationService;

    public ProviderValidationController(
        ILogger<ProviderValidationController> logger,
        ProviderValidationService validationService)
    {
        _logger = logger;
        _validationService = validationService;
    }

    /// <summary>
    /// Get status of all providers
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ProvidersStatusResponse>> GetProvidersStatus(CancellationToken ct)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation("Getting providers status, CorrelationId: {CorrelationId}", correlationId);

            var providerStates = await _validationService.GetAllProviderStatesAsync(ct).ConfigureAwait(false);

            var response = new ProvidersStatusResponse(
                Providers: providerStates.Values.Select(MapProviderState).ToList(),
                Timestamp: DateTimeOffset.UtcNow,
                CorrelationId: correlationId
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting providers status");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error getting providers status",
                Status = 500,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Validate a specific provider
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ProviderValidationResponseDto>> ValidateProvider(
        [FromBody] ValidateProviderRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "Validating provider {ProviderId}, CorrelationId: {CorrelationId}",
                request.ProviderId,
                correlationId);

            if (string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Status = 400,
                    Detail = "ProviderId is required"
                });
            }

            var credentials = new ProviderCredentials
            {
                ApiKey = request.ApiKey,
                BaseUrl = request.BaseUrl,
                OrganizationId = request.OrganizationId,
                ProjectId = request.ProjectId
            };

            if (request.AdditionalSettings != null)
            {
                foreach (var kvp in request.AdditionalSettings)
                {
                    credentials.AdditionalSettings[kvp.Key] = kvp.Value;
                }
            }

            var result = await _validationService.ValidateProviderAsync(
                request.ProviderId,
                credentials,
                ct).ConfigureAwait(false);

            var response = new ProviderValidationResponseDto(
                IsValid: result.IsValid,
                ProviderId: request.ProviderId,
                ErrorCode: result.ErrorCode,
                ErrorMessage: result.ErrorMessage,
                HttpStatusCode: result.HttpStatusCode,
                ResponseTimeMs: result.ResponseTimeMs,
                DiagnosticInfo: result.DiagnosticInfo,
                CorrelationId: correlationId
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating provider {ProviderId}", request?.ProviderId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error validating provider",
                Status = 500,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Validate all enabled providers
    /// </summary>
    [HttpPost("validate-all")]
    public async Task<ActionResult<ValidateAllProvidersResponseDto>> ValidateAllProviders(CancellationToken ct)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation("Validating all enabled providers, CorrelationId: {CorrelationId}", correlationId);

            var results = await _validationService.ValidateAllProvidersAsync(ct).ConfigureAwait(false);

            var response = new ValidateAllProvidersResponseDto(
                Results: results.Select(r => new ProviderValidationResultItem(
                    ProviderId: r.Key,
                    IsValid: r.Value.IsValid,
                    ErrorCode: r.Value.ErrorCode,
                    ErrorMessage: r.Value.ErrorMessage,
                    ResponseTimeMs: r.Value.ResponseTimeMs
                )).ToList(),
                Timestamp: DateTimeOffset.UtcNow,
                TotalValidated: results.Count,
                ValidCount: results.Count(r => r.Value.IsValid),
                InvalidCount: results.Count(r => !r.Value.IsValid),
                CorrelationId: correlationId
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating all providers");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error validating providers",
                Status = 500,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Save provider credentials
    /// </summary>
    [HttpPut("{providerId}/credentials")]
    public async Task<ActionResult> SaveProviderCredentials(
        string providerId,
        [FromBody] SaveProviderCredentialsRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "Saving credentials for provider {ProviderId}, CorrelationId: {CorrelationId}",
                providerId,
                correlationId);

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Status = 400,
                    Detail = "ProviderId is required"
                });
            }

            var credentials = new ProviderCredentials
            {
                ApiKey = request.ApiKey,
                BaseUrl = request.BaseUrl,
                OrganizationId = request.OrganizationId,
                ProjectId = request.ProjectId
            };

            if (request.AdditionalSettings != null)
            {
                foreach (var kvp in request.AdditionalSettings)
                {
                    credentials.AdditionalSettings[kvp.Key] = kvp.Value;
                }
            }

            await _validationService.SaveProviderCredentialsAsync(providerId, credentials, ct)
                .ConfigureAwait(false);

            if (request.Enabled.HasValue)
            {
                await _validationService.SetProviderEnabledAsync(providerId, request.Enabled.Value, ct)
                    .ConfigureAwait(false);
            }

            return Ok(new { success = true, message = "Credentials saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving credentials for provider {ProviderId}", providerId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error saving credentials",
                Status = 500,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    private static ProviderStatusDto MapProviderState(ProviderState state)
    {
        return new ProviderStatusDto(
            ProviderId: state.ProviderId,
            Type: state.Type.ToString(),
            Enabled: state.Enabled,
            CredentialsConfigured: state.CredentialsConfigured,
            ValidationStatus: MapValidationStatus(state.ValidationStatus),
            LastValidationAt: state.LastValidationAt,
            LastErrorCode: state.LastErrorCode,
            LastErrorMessage: state.LastErrorMessage,
            Priority: state.Priority
        );
    }

    private static string MapValidationStatus(ProviderValidationStatus status)
    {
        return status switch
        {
            ProviderValidationStatus.Unknown => "unknown",
            ProviderValidationStatus.Valid => "valid",
            ProviderValidationStatus.Invalid => "invalid",
            ProviderValidationStatus.Error => "error",
            ProviderValidationStatus.NotConfigured => "not-configured",
            _ => "unknown"
        };
    }
}
