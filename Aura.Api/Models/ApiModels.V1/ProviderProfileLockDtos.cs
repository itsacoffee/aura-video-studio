using System;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request to set a provider profile lock
/// </summary>
public record SetProfileLockRequest(
    string JobId,
    string ProviderName,
    string ProviderType,
    bool IsEnabled,
    bool OfflineModeEnabled = false,
    string[]? ApplicableStages = null,
    bool IsSessionLevel = true,
    ProfileLockMetadataDto? Metadata = null);

/// <summary>
/// Response containing profile lock information
/// </summary>
public record ProfileLockResponse(
    string JobId,
    string ProviderName,
    string ProviderType,
    bool IsEnabled,
    DateTime CreatedAt,
    bool OfflineModeEnabled,
    string[] ApplicableStages,
    ProfileLockMetadataDto Metadata,
    string Source);

/// <summary>
/// Request to unlock a provider profile lock
/// </summary>
public record UnlockProfileLockRequest(
    string JobId,
    bool IsSessionLevel = true,
    string? Reason = null);

/// <summary>
/// Request to validate provider offline compatibility
/// </summary>
public record CheckOfflineCompatibilityRequest(
    string ProviderName);

/// <summary>
/// Response for offline compatibility check
/// </summary>
public record OfflineCompatibilityResponse(
    string ProviderName,
    bool IsCompatible,
    string? Message,
    string[] OfflineCompatibleProviders);

/// <summary>
/// Response for profile lock status query
/// </summary>
public record ProfileLockStatusResponse(
    string? JobId,
    bool HasActiveLock,
    ProfileLockResponse? ActiveLock,
    ProfileLockStatisticsDto Statistics);

/// <summary>
/// Metadata for profile locks (API DTO)
/// </summary>
public record ProfileLockMetadataDto(
    string? CreatedByUser = null,
    string? Reason = null,
    string[] Tags = null!,
    string Source = "User",
    bool AllowManualFallback = true,
    int? MaxWaitBeforeFallbackSeconds = null)
{
    public string[] Tags { get; init; } = Tags ?? Array.Empty<string>();
}

/// <summary>
/// Statistics about profile locks
/// </summary>
public record ProfileLockStatisticsDto(
    int TotalSessionLocks,
    int TotalProjectLocks,
    int EnabledSessionLocks,
    int EnabledProjectLocks,
    int OfflineModeLocksCount);

/// <summary>
/// Request to validate a provider request against profile lock
/// </summary>
public record ValidateProviderRequest(
    string JobId,
    string ProviderName,
    string StageName,
    bool ProviderRequiresNetwork);

/// <summary>
/// Response for provider validation
/// </summary>
public record ValidateProviderResponse(
    bool IsValid,
    string? ValidationError,
    ProfileLockResponse? ActiveLock);
