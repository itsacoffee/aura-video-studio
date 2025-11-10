using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

#region User Management DTOs

public record UserDto(
    string Id,
    string Username,
    string Email,
    string? DisplayName,
    bool IsActive,
    bool IsSuspended,
    DateTime? SuspendedAt,
    string? SuspendedReason,
    DateTime? LastLoginAt,
    string? LastLoginIp,
    int FailedLoginAttempts,
    DateTime? LockoutEnd,
    bool EmailVerified,
    string? PhoneNumber,
    bool PhoneVerified,
    bool TwoFactorEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Roles,
    UserQuotaSummaryDto? Quota
);

public record CreateUserRequest(
    string Username,
    string Email,
    string? DisplayName,
    string? Password,
    List<string>? RoleIds,
    UserQuotaDto? Quota
);

public record UpdateUserRequest(
    string? DisplayName,
    string? Email,
    string? PhoneNumber,
    bool? IsActive,
    bool? TwoFactorEnabled
);

public record SuspendUserRequest(
    string Reason,
    DateTime? UntilDate
);

public record UserQuotaSummaryDto(
    int? ApiRequestsPerDay,
    int ApiRequestsUsedToday,
    int? VideosPerMonth,
    int VideosGeneratedThisMonth,
    long? StorageLimitBytes,
    long StorageUsedBytes,
    long? AiTokensPerMonth,
    long AiTokensUsedThisMonth,
    decimal TotalCostUsd,
    decimal? CostLimitUsd
);

public record UserQuotaDto(
    int? ApiRequestsPerDay,
    int? VideosPerMonth,
    long? StorageLimitBytes,
    long? AiTokensPerMonth,
    int? MaxConcurrentRenders,
    int? MaxConcurrentJobs,
    decimal? CostLimitUsd
);

public record UserListResponse(
    List<UserDto> Users,
    int TotalCount,
    int Page,
    int PageSize
);

public record UserActivityDto(
    string UserId,
    string Username,
    DateTime Timestamp,
    string Action,
    string? ResourceType,
    string? ResourceId,
    bool Success
);

#endregion

#region Role Management DTOs

public record RoleDto(
    string Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    List<string> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int UserCount
);

public record CreateRoleRequest(
    string Name,
    string? Description,
    List<string> Permissions
);

public record UpdateRoleRequest(
    string? Name,
    string? Description,
    List<string>? Permissions
);

#endregion

#region Audit Log DTOs

public record AuditLogDto(
    string Id,
    DateTime Timestamp,
    string? UserId,
    string? Username,
    string Action,
    string? ResourceType,
    string? ResourceId,
    string? IpAddress,
    string? UserAgent,
    bool Success,
    string? ErrorMessage,
    Dictionary<string, object>? Changes,
    string? Severity
);

public record AuditLogQueryRequest(
    string? UserId,
    string? Action,
    string? ResourceType,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? SuccessOnly,
    int Page = 1,
    int PageSize = 50
);

public record AuditLogResponse(
    List<AuditLogDto> Logs,
    int TotalCount,
    int Page,
    int PageSize
);

#endregion

#region System Metrics DTOs

public record SystemMetricsDto(
    DateTime Timestamp,
    SystemResourcesDto Resources,
    ApplicationMetricsDto Application,
    ProviderMetricsDto Providers,
    CostMetricsDto Costs
);

public record SystemResourcesDto(
    double CpuUsagePercent,
    long MemoryUsedBytes,
    long MemoryTotalBytes,
    double MemoryUsagePercent,
    List<DiskUsageDto> Disks,
    double? GpuUsagePercent,
    long? GpuMemoryUsedBytes,
    long? GpuMemoryTotalBytes
);

public record DiskUsageDto(
    string DriveName,
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    double UsagePercent
);

public record ApplicationMetricsDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalProjects,
    int ActiveProjects,
    int TotalVideos,
    int VideosToday,
    int JobsInProgress,
    int JobsQueued,
    int JobsFailed,
    double AverageRenderTimeSeconds,
    long CacheHits,
    long CacheMisses,
    double CacheHitRate
);

public record ProviderMetricsDto(
    List<ProviderStatusDto> Providers,
    int TotalRequests,
    int FailedRequests,
    double ErrorRate,
    double AverageLatencyMs
);

public record ProviderStatusDto(
    string Name,
    string Status,
    int RequestCount,
    int ErrorCount,
    double AverageLatencyMs,
    DateTime LastUsed
);

public record CostMetricsDto(
    decimal TotalCostToday,
    decimal TotalCostThisMonth,
    decimal TotalCostAllTime,
    Dictionary<string, decimal> CostByProvider,
    Dictionary<string, decimal> CostByUser,
    List<CostBreakdownDto> TopCostItems
);

public record CostBreakdownDto(
    string Category,
    string Item,
    decimal Cost,
    int UsageCount
);

#endregion

#region Configuration DTOs

public record ConfigurationItemDto(
    string Key,
    string Value,
    string? Category,
    string? Description,
    bool IsSensitive,
    bool IsActive,
    DateTime UpdatedAt
);

public record UpdateConfigurationRequest(
    string Key,
    string Value,
    string? Category,
    string? Description,
    bool IsSensitive = false,
    bool IsActive = true
);

public record ConfigurationCategoryDto(
    string Category,
    List<ConfigurationItemDto> Items
);

#endregion

#region Operational Tools DTOs

public record CacheStatsDto(
    string CacheType,
    long Hits,
    long Misses,
    double HitRate,
    long TotalEntries,
    long SizeBytes,
    DateTime LastCleared
);

public record ClearCacheRequest(
    string? CacheType,
    bool ClearAll = false
);

public record JobQueueStatsDto(
    int TotalJobs,
    int PendingJobs,
    int RunningJobs,
    int CompletedJobs,
    int FailedJobs,
    double AverageWaitTimeSeconds,
    double AverageExecutionTimeSeconds,
    List<JobDto> RecentJobs
);

public record JobDto(
    string Id,
    string Type,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? DurationSeconds,
    string? ErrorMessage
);

public record LogQueryRequest(
    string? Level,
    string? Source,
    string? Message,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page = 1,
    int PageSize = 100
);

public record LogEntryDto(
    DateTime Timestamp,
    string Level,
    string Source,
    string Message,
    string? Exception,
    Dictionary<string, object>? Properties
);

public record LogQueryResponse(
    List<LogEntryDto> Logs,
    int TotalCount,
    int Page,
    int PageSize
);

#endregion
