using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Response containing status of all providers
/// </summary>
public record ProvidersStatusResponse(
    List<ProviderStatusDto> Providers,
    DateTimeOffset Timestamp,
    string? CorrelationId = null);

/// <summary>
/// Status of a single provider
/// </summary>
public record ProviderStatusDto(
    string ProviderId,
    string Type,
    bool Enabled,
    bool CredentialsConfigured,
    string ValidationStatus,
    DateTimeOffset? LastValidationAt,
    string? LastErrorCode,
    string? LastErrorMessage,
    int Priority);

/// <summary>
/// Request to validate a provider
/// </summary>
public record ValidateProviderRequestDto(
    string ProviderId,
    string? ApiKey = null,
    string? BaseUrl = null,
    string? OrganizationId = null,
    string? ProjectId = null,
    Dictionary<string, string>? AdditionalSettings = null);

/// <summary>
/// Response from provider validation
/// </summary>
public record ProviderValidationResponseDto(
    bool IsValid,
    string ProviderId,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    int? HttpStatusCode = null,
    long ResponseTimeMs = 0,
    string? DiagnosticInfo = null,
    string? CorrelationId = null);

/// <summary>
/// Response from validating all providers
/// </summary>
public record ValidateAllProvidersResponseDto(
    List<ProviderValidationResultItem> Results,
    DateTimeOffset Timestamp,
    int TotalValidated,
    int ValidCount,
    int InvalidCount,
    string? CorrelationId = null);

/// <summary>
/// Single provider validation result item
/// </summary>
public record ProviderValidationResultItem(
    string ProviderId,
    bool IsValid,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    long ResponseTimeMs = 0);

/// <summary>
/// Request to save provider credentials
/// </summary>
public record SaveProviderCredentialsRequestDto(
    string? ApiKey = null,
    string? BaseUrl = null,
    string? OrganizationId = null,
    string? ProjectId = null,
    Dictionary<string, string>? AdditionalSettings = null,
    bool? Enabled = null);
