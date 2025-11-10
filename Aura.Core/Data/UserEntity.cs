using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Entity representing a system user
/// </summary>
[Table("users")]
public class UserEntity : IAuditableEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("username")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("display_name")]
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [Column("password_hash")]
    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_suspended")]
    public bool IsSuspended { get; set; } = false;

    [Column("suspended_at")]
    public DateTime? SuspendedAt { get; set; }

    [Column("suspended_reason")]
    public string? SuspendedReason { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("last_login_ip")]
    [MaxLength(45)]
    public string? LastLoginIp { get; set; }

    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    [Column("lockout_end")]
    public DateTime? LockoutEnd { get; set; }

    [Column("email_verified")]
    public bool EmailVerified { get; set; } = false;

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("phone_verified")]
    public bool PhoneVerified { get; set; } = false;

    [Column("two_factor_enabled")]
    public bool TwoFactorEnabled { get; set; } = false;

    [Column("two_factor_secret")]
    [MaxLength(200)]
    public string? TwoFactorSecret { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("metadata")]
    public string? Metadata { get; set; }

    // Navigation properties
    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();
    public UserQuotaEntity? Quota { get; set; }
}

/// <summary>
/// Entity representing a role in the system
/// </summary>
[Table("roles")]
public class RoleEntity : IAuditableEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("normalized_name")]
    [MaxLength(100)]
    public string? NormalizedName { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column("is_system_role")]
    public bool IsSystemRole { get; set; } = false;

    [Column("permissions")]
    public string? Permissions { get; set; } // JSON array of permission strings

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();
}

/// <summary>
/// Join entity for many-to-many relationship between Users and Roles
/// </summary>
[Table("user_roles")]
public class UserRoleEntity
{
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("role_id")]
    public string RoleId { get; set; } = string.Empty;

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("assigned_by")]
    [MaxLength(200)]
    public string? AssignedBy { get; set; }

    // Navigation properties
    public UserEntity User { get; set; } = null!;
    public RoleEntity Role { get; set; } = null!;
}

/// <summary>
/// Entity for tracking user quotas and usage
/// </summary>
[Table("user_quotas")]
public class UserQuotaEntity : IAuditableEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    // API Usage Limits
    [Column("api_requests_per_day")]
    public int? ApiRequestsPerDay { get; set; }

    [Column("api_requests_used_today")]
    public int ApiRequestsUsedToday { get; set; } = 0;

    [Column("api_requests_reset_at")]
    public DateTime? ApiRequestsResetAt { get; set; }

    // Video Generation Limits
    [Column("videos_per_month")]
    public int? VideosPerMonth { get; set; }

    [Column("videos_generated_this_month")]
    public int VideosGeneratedThisMonth { get; set; } = 0;

    [Column("videos_reset_at")]
    public DateTime? VideosResetAt { get; set; }

    // Storage Limits
    [Column("storage_limit_bytes")]
    public long? StorageLimitBytes { get; set; }

    [Column("storage_used_bytes")]
    public long StorageUsedBytes { get; set; } = 0;

    // AI Token Limits
    [Column("ai_tokens_per_month")]
    public long? AiTokensPerMonth { get; set; }

    [Column("ai_tokens_used_this_month")]
    public long AiTokensUsedThisMonth { get; set; } = 0;

    [Column("ai_tokens_reset_at")]
    public DateTime? AiTokensResetAt { get; set; }

    // Concurrent Operations
    [Column("max_concurrent_renders")]
    public int? MaxConcurrentRenders { get; set; }

    [Column("max_concurrent_jobs")]
    public int? MaxConcurrentJobs { get; set; }

    // Cost Tracking
    [Column("total_cost_usd")]
    public decimal TotalCostUsd { get; set; } = 0;

    [Column("cost_limit_usd")]
    public decimal? CostLimitUsd { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public UserEntity User { get; set; } = null!;
}

/// <summary>
/// Entity for audit logging of administrative and user actions
/// </summary>
[Table("audit_logs")]
public class AuditLogEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("user_id")]
    [MaxLength(200)]
    public string? UserId { get; set; }

    [Column("username")]
    [MaxLength(200)]
    public string? Username { get; set; }

    [Required]
    [Column("action")]
    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [Column("resource_type")]
    [MaxLength(100)]
    public string? ResourceType { get; set; }

    [Column("resource_id")]
    [MaxLength(200)]
    public string? ResourceId { get; set; }

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Column("success")]
    public bool Success { get; set; } = true;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("changes")]
    public string? Changes { get; set; } // JSON serialized changes

    [Column("metadata")]
    public string? Metadata { get; set; } // Additional JSON data

    [Column("severity")]
    [MaxLength(50)]
    public string? Severity { get; set; } = "Information";
}

/// <summary>
/// Soft deletable interface for entities that support soft delete
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Versioned entity interface for optimistic concurrency
/// </summary>
public interface IVersionedEntity
{
    byte[]? RowVersion { get; set; }
}
