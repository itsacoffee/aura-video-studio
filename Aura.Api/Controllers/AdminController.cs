using Aura.Api.Models.ApiModels.V1;
using Aura.Api.Security;
using Aura.Core.Data;
using Aura.Core.Services.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aura.Api.Controllers;

/// <summary>
/// Administrative endpoints for user management, system monitoring, and configuration
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
public class AdminController : ControllerBase
{
    private readonly AuraDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AdminController> _logger;
    private readonly SystemResourceMonitor? _resourceMonitor;

    public AdminController(
        AuraDbContext dbContext,
        IAuditLogger auditLogger,
        ILogger<AdminController> logger,
        SystemResourceMonitor? resourceMonitor = null)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;
        _logger = logger;
        _resourceMonitor = resourceMonitor;
    }

    #region User Management

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserListResponse>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isSuspended = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (isSuspended.HasValue)
                query = query.Where(u => u.IsSuspended == isSuspended.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct).ConfigureAwait(false);

            var userDtos = users.Select(MapToUserDto).ToList();

            return Ok(new UserListResponse(userDtos, totalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return Problem("Failed to retrieve users", statusCode: 500);
        }
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return Problem("Failed to retrieve user", statusCode: 500);
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Validate username uniqueness
            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username, ct).ConfigureAwait(false))
                return BadRequest(new { message = "Username already exists" });

            // Validate email uniqueness
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email, ct).ConfigureAwait(false))
                return BadRequest(new { message = "Email already exists" });

            var user = new UserEntity
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                DisplayName = request.DisplayName,
                PasswordHash = !string.IsNullOrEmpty(request.Password) 
                    ? HashPassword(request.Password) 
                    : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);

            // Assign roles
            if (request.RoleIds?.Any() == true)
            {
                foreach (var roleId in request.RoleIds)
                {
                    var role = await _dbContext.Roles.FindAsync(new object[] { roleId }, ct).ConfigureAwait(false);
                    if (role != null)
                    {
                        _dbContext.UserRoles.Add(new UserRoleEntity
                        {
                            UserId = user.Id,
                            RoleId = roleId,
                            AssignedAt = DateTime.UtcNow,
                            AssignedBy = User.Identity?.Name
                        });
                    }
                }
            }

            // Create quota if provided
            if (request.Quota != null)
            {
                _dbContext.UserQuotas.Add(new UserQuotaEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    ApiRequestsPerDay = request.Quota.ApiRequestsPerDay,
                    VideosPerMonth = request.Quota.VideosPerMonth,
                    StorageLimitBytes = request.Quota.StorageLimitBytes,
                    AiTokensPerMonth = request.Quota.AiTokensPerMonth,
                    MaxConcurrentRenders = request.Quota.MaxConcurrentRenders,
                    MaxConcurrentJobs = request.Quota.MaxConcurrentJobs,
                    CostLimitUsd = request.Quota.CostLimitUsd,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("UserCreated", $"User {user.Username} created", new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            // Reload user with relationships
            var createdUser = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstAsync(u => u.Id == user.Id, ct).ConfigureAwait(false);

            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, MapToUserDto(createdUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Problem("Failed to create user", statusCode: 500);
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("users/{userId}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateUser(
        string userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user == null)
                return NotFound(new { message = "User not found" });

            var changes = new Dictionary<string, object>();

            if (request.DisplayName != null && user.DisplayName != request.DisplayName)
            {
                changes["DisplayName"] = new { Old = user.DisplayName, New = request.DisplayName };
                user.DisplayName = request.DisplayName;
            }

            if (request.Email != null && user.Email != request.Email)
            {
                // Check email uniqueness
                if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId, ct).ConfigureAwait(false))
                    return BadRequest(new { message = "Email already exists" });

                changes["Email"] = new { Old = user.Email, New = request.Email };
                user.Email = request.Email;
                user.EmailVerified = false;
            }

            if (request.PhoneNumber != null && user.PhoneNumber != request.PhoneNumber)
            {
                changes["PhoneNumber"] = new { Old = user.PhoneNumber, New = request.PhoneNumber };
                user.PhoneNumber = request.PhoneNumber;
                user.PhoneVerified = false;
            }

            if (request.IsActive.HasValue && user.IsActive != request.IsActive.Value)
            {
                changes["IsActive"] = new { Old = user.IsActive, New = request.IsActive.Value };
                user.IsActive = request.IsActive.Value;
            }

            if (request.TwoFactorEnabled.HasValue && user.TwoFactorEnabled != request.TwoFactorEnabled.Value)
            {
                changes["TwoFactorEnabled"] = new { Old = user.TwoFactorEnabled, New = request.TwoFactorEnabled.Value };
                user.TwoFactorEnabled = request.TwoFactorEnabled.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            if (changes.Any())
            {
                _auditLogger.LogSecurityEvent("UserUpdated", $"User {user.Username} updated", new Dictionary<string, object>
                {
                    ["UserId"] = user.Id,
                    ["Changes"] = changes,
                    ["AdminUser"] = User.Identity?.Name ?? "Unknown"
                });
            }

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return Problem("Failed to update user", statusCode: 500);
        }
    }

    /// <summary>
    /// Suspend a user account
    /// </summary>
    [HttpPost("users/{userId}/suspend")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SuspendUser(
        string userId,
        [FromBody] SuspendUserRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsSuspended = true;
            user.SuspendedAt = DateTime.UtcNow;
            user.SuspendedReason = request.Reason;
            user.LockoutEnd = request.UntilDate;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("UserSuspended", $"User {user.Username} suspended", new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["Reason"] = request.Reason,
                ["UntilDate"] = request.UntilDate?.ToString() ?? "Indefinite",
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending user {UserId}", userId);
            return Problem("Failed to suspend user", statusCode: 500);
        }
    }

    /// <summary>
    /// Unsuspend a user account
    /// </summary>
    [HttpPost("users/{userId}/unsuspend")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UnsuspendUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsSuspended = false;
            user.SuspendedAt = null;
            user.SuspendedReason = null;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("UserUnsuspended", $"User {user.Username} unsuspended", new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsuspending user {UserId}", userId);
            return Problem("Failed to unsuspend user", statusCode: 500);
        }
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("UserDeleted", $"User {user.Username} deleted", new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Problem("Failed to delete user", statusCode: 500);
        }
    }

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> AssignRoles(
        string userId,
        [FromBody] List<string> roleIds,
        CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Remove existing roles
            var existingRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync(ct).ConfigureAwait(false);
            _dbContext.UserRoles.RemoveRange(existingRoles);

            // Add new roles
            foreach (var roleId in roleIds)
            {
                var role = await _dbContext.Roles.FindAsync(new object[] { roleId }, ct).ConfigureAwait(false);
                if (role != null)
                {
                    _dbContext.UserRoles.Add(new UserRoleEntity
                    {
                        UserId = userId,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = User.Identity?.Name
                    });
                }
            }

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("UserRolesUpdated", $"Roles updated for user {user.Username}", new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["NewRoles"] = roleIds,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            // Reload user
            user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Quota)
                .FirstAsync(u => u.Id == userId, ct).ConfigureAwait(false);

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning roles to user {UserId}", userId);
            return Problem("Failed to assign roles", statusCode: 500);
        }
    }

    /// <summary>
    /// Update user quota
    /// </summary>
    [HttpPut("users/{userId}/quota")]
    [ProducesResponseType(typeof(UserQuotaSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserQuotaSummaryDto>> UpdateUserQuota(
        string userId,
        [FromBody] UserQuotaDto request,
        CancellationToken ct = default)
    {
        try
        {
            var quota = await _dbContext.UserQuotas.FirstOrDefaultAsync(q => q.UserId == userId, ct).ConfigureAwait(false);
            
            if (quota == null)
            {
                quota = new UserQuotaEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.UserQuotas.Add(quota);
            }

            quota.ApiRequestsPerDay = request.ApiRequestsPerDay;
            quota.VideosPerMonth = request.VideosPerMonth;
            quota.StorageLimitBytes = request.StorageLimitBytes;
            quota.AiTokensPerMonth = request.AiTokensPerMonth;
            quota.MaxConcurrentRenders = request.MaxConcurrentRenders;
            quota.MaxConcurrentJobs = request.MaxConcurrentJobs;
            quota.CostLimitUsd = request.CostLimitUsd;
            quota.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("UserQuotaUpdated", $"Quota updated for user {userId}", new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return Ok(new UserQuotaSummaryDto(
                quota.ApiRequestsPerDay,
                quota.ApiRequestsUsedToday,
                quota.VideosPerMonth,
                quota.VideosGeneratedThisMonth,
                quota.StorageLimitBytes,
                quota.StorageUsedBytes,
                quota.AiTokensPerMonth,
                quota.AiTokensUsedThisMonth,
                quota.TotalCostUsd,
                quota.CostLimitUsd
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user quota {UserId}", userId);
            return Problem("Failed to update user quota", statusCode: 500);
        }
    }

    #endregion

    #region Role Management

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoleDto>>> GetRoles(CancellationToken ct = default)
    {
        try
        {
            var roles = await _dbContext.Roles
                .Include(r => r.UserRoles)
                .OrderBy(r => r.Name)
                .ToListAsync(ct).ConfigureAwait(false);

            var roleDtos = roles.Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                string.IsNullOrEmpty(r.Permissions) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(r.Permissions) ?? new List<string>(),
                r.CreatedAt,
                r.UpdatedAt,
                r.UserRoles.Count
            )).ToList();

            return Ok(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return Problem("Failed to retrieve roles", statusCode: 500);
        }
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost("roles")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleDto>> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (await _dbContext.Roles.AnyAsync(r => r.Name == request.Name, ct).ConfigureAwait(false))
                return BadRequest(new { message = "Role name already exists" });

            var role = new RoleEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                NormalizedName = request.Name.ToUpperInvariant(),
                Description = request.Description,
                IsSystemRole = false,
                Permissions = JsonSerializer.Serialize(request.Permissions),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("RoleCreated", $"Role {role.Name} created", new Dictionary<string, object>
            {
                ["RoleId"] = role.Id,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return CreatedAtAction(nameof(GetRoles), new RoleDto(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystemRole,
                request.Permissions,
                role.CreatedAt,
                role.UpdatedAt,
                0
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return Problem("Failed to create role", statusCode: 500);
        }
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    [HttpPut("roles/{roleId}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDto>> UpdateRole(
        string roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var role = await _dbContext.Roles.FindAsync(new object[] { roleId }, ct).ConfigureAwait(false);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            if (role.IsSystemRole)
                return BadRequest(new { message = "Cannot modify system roles" });

            if (request.Name != null)
                role.Name = request.Name;

            if (request.Description != null)
                role.Description = request.Description;

            if (request.Permissions != null)
                role.Permissions = JsonSerializer.Serialize(request.Permissions);

            role.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("RoleUpdated", $"Role {role.Name} updated", new Dictionary<string, object>
            {
                ["RoleId"] = role.Id,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return Ok(new RoleDto(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystemRole,
                request.Permissions ?? new List<string>(),
                role.CreatedAt,
                role.UpdatedAt,
                0
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return Problem("Failed to update role", statusCode: 500);
        }
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRole(string roleId, CancellationToken ct = default)
    {
        try
        {
            var role = await _dbContext.Roles.FindAsync(new object[] { roleId }, ct).ConfigureAwait(false);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            if (role.IsSystemRole)
                return BadRequest(new { message = "Cannot delete system roles" });

            _dbContext.Roles.Remove(role);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("RoleDeleted", $"Role {role.Name} deleted", new Dictionary<string, object>
            {
                ["RoleId"] = role.Id,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return Problem("Failed to delete role", statusCode: 500);
        }
    }

    #endregion

    #region Audit Logs

    /// <summary>
    /// Query audit logs
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogResponse>> GetAuditLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? successOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(log => log.UserId == userId);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(log => log.Action.Contains(action));

            if (!string.IsNullOrWhiteSpace(resourceType))
                query = query.Where(log => log.ResourceType == resourceType);

            if (startDate.HasValue)
                query = query.Where(log => log.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.Timestamp <= endDate.Value);

            if (successOnly.HasValue)
                query = query.Where(log => log.Success == successOnly.Value);

            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct).ConfigureAwait(false);

            var logDtos = logs.Select(log => new AuditLogDto(
                log.Id,
                log.Timestamp,
                log.UserId,
                log.Username,
                log.Action,
                log.ResourceType,
                log.ResourceId,
                log.IpAddress,
                log.UserAgent,
                log.Success,
                log.ErrorMessage,
                string.IsNullOrEmpty(log.Changes) 
                    ? null 
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(log.Changes),
                log.Severity
            )).ToList();

            return Ok(new AuditLogResponse(logDtos, totalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return Problem("Failed to retrieve audit logs", statusCode: 500);
        }
    }

    /// <summary>
    /// Get user activity history
    /// </summary>
    [HttpGet("users/{userId}/activity")]
    [ProducesResponseType(typeof(List<UserActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserActivityDto>>> GetUserActivity(
        string userId,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        try
        {
            var activities = await _dbContext.AuditLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.Timestamp)
                .Take(limit)
                .Select(log => new UserActivityDto(
                    log.UserId!,
                    log.Username ?? "Unknown",
                    log.Timestamp,
                    log.Action,
                    log.ResourceType,
                    log.ResourceId,
                    log.Success
                ))
                .ToListAsync(ct).ConfigureAwait(false);

            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for {UserId}", userId);
            return Problem("Failed to retrieve user activity", statusCode: 500);
        }
    }

    #endregion

    #region System Metrics

    /// <summary>
    /// Get comprehensive system metrics
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(SystemMetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics(CancellationToken ct = default)
    {
        try
        {
            // System resources
            SystemResourcesDto resources;
            if (_resourceMonitor != null)
            {
                var systemMetrics = await _resourceMonitor.CollectSystemMetricsAsync(ct).ConfigureAwait(false);
                resources = new SystemResourcesDto(
                    systemMetrics.Cpu.ProcessUsagePercent,
                    systemMetrics.Memory.ProcessUsageBytes,
                    systemMetrics.Memory.TotalBytes,
                    systemMetrics.Memory.UsagePercent,
                    systemMetrics.Disks.Select(d => new DiskUsageDto(
                        d.DriveName,
                        d.TotalBytes,
                        d.UsedBytes,
                        d.AvailableBytes,
                        d.UsagePercent
                    )).ToList(),
                    systemMetrics.Gpu?.UsagePercent,
                    systemMetrics.Gpu?.UsedMemoryBytes,
                    systemMetrics.Gpu?.TotalMemoryBytes
                );
            }
            else
            {
                resources = new SystemResourcesDto(0, 0, 0, 0, new List<DiskUsageDto>(), null, null, null);
            }

            // Application metrics
            var totalUsers = await _dbContext.Users.CountAsync(ct).ConfigureAwait(false);
            var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive, ct).ConfigureAwait(false);
            var totalProjects = await _dbContext.ProjectStates.CountAsync(ct).ConfigureAwait(false);
            var activeProjects = await _dbContext.ProjectStates
                .CountAsync(p => p.Status == "Active" || p.Status == "InProgress", ct).ConfigureAwait(false);
            
            var application = new ApplicationMetricsDto(
                totalUsers,
                activeUsers,
                totalProjects,
                activeProjects,
                0, // Total videos - would need separate tracking
                0, // Videos today
                0, // Jobs in progress - would query job queue
                0, // Jobs queued
                0, // Jobs failed
                0, // Average render time
                0, // Cache hits
                0, // Cache misses
                0  // Cache hit rate
            );

            // Provider metrics (placeholder)
            var providers = new ProviderMetricsDto(
                new List<ProviderStatusDto>(),
                0,
                0,
                0,
                0
            );

            // Cost metrics (placeholder)
            var costs = new CostMetricsDto(
                0,
                0,
                0,
                new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>(),
                new List<CostBreakdownDto>()
            );

            var metrics = new SystemMetricsDto(
                DateTime.UtcNow,
                resources,
                application,
                providers,
                costs
            );

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system metrics");
            return Problem("Failed to retrieve system metrics", statusCode: 500);
        }
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// Get all configuration items
    /// </summary>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(List<ConfigurationItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConfigurationItemDto>>> GetConfiguration(
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.Configurations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => c.Category == category);

            var configs = await query
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Key)
                .ToListAsync(ct).ConfigureAwait(false);

            var configDtos = configs.Select(c => new ConfigurationItemDto(
                c.Key,
                c.IsSensitive ? "***SENSITIVE***" : c.Value,
                c.Category,
                c.Description,
                c.IsSensitive,
                c.IsActive,
                c.UpdatedAt
            )).ToList();

            return Ok(configDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration");
            return Problem("Failed to retrieve configuration", statusCode: 500);
        }
    }

    /// <summary>
    /// Get configuration by category
    /// </summary>
    [HttpGet("configuration/categories")]
    [ProducesResponseType(typeof(List<ConfigurationCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConfigurationCategoryDto>>> GetConfigurationByCategory(
        CancellationToken ct = default)
    {
        try
        {
            var configs = await _dbContext.Configurations
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Key)
                .ToListAsync(ct).ConfigureAwait(false);

            var categories = configs
                .GroupBy(c => c.Category ?? "General")
                .Select(g => new ConfigurationCategoryDto(
                    g.Key,
                    g.Select(c => new ConfigurationItemDto(
                        c.Key,
                        c.IsSensitive ? "***SENSITIVE***" : c.Value,
                        c.Category,
                        c.Description,
                        c.IsSensitive,
                        c.IsActive,
                        c.UpdatedAt
                    )).ToList()
                ))
                .ToList();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration categories");
            return Problem("Failed to retrieve configuration categories", statusCode: 500);
        }
    }

    /// <summary>
    /// Update configuration item
    /// </summary>
    [HttpPut("configuration/{key}")]
    [ProducesResponseType(typeof(ConfigurationItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationItemDto>> UpdateConfiguration(
        string key,
        [FromBody] UpdateConfigurationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var config = await _dbContext.Configurations.FindAsync(new object[] { key }, ct).ConfigureAwait(false);
            
            var oldValue = config?.Value;
            var isNew = config == null;

            if (config == null)
            {
                config = new ConfigurationEntity
                {
                    Key = request.Key,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Configurations.Add(config);
            }

            config.Value = request.Value;
            config.Category = request.Category;
            config.Description = request.Description;
            config.IsSensitive = request.IsSensitive;
            config.IsActive = request.IsActive;
            config.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogConfigurationChange(
                User.Identity?.Name ?? "Unknown",
                config.Key,
                oldValue ?? "[new]",
                request.Value
            );

            return Ok(new ConfigurationItemDto(
                config.Key,
                config.IsSensitive ? "***SENSITIVE***" : config.Value,
                config.Category,
                config.Description,
                config.IsSensitive,
                config.IsActive,
                config.UpdatedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {Key}", key);
            return Problem("Failed to update configuration", statusCode: 500);
        }
    }

    /// <summary>
    /// Delete configuration item
    /// </summary>
    [HttpDelete("configuration/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConfiguration(string key, CancellationToken ct = default)
    {
        try
        {
            var config = await _dbContext.Configurations.FindAsync(new object[] { key }, ct).ConfigureAwait(false);
            if (config == null)
                return NotFound(new { message = "Configuration not found" });

            _dbContext.Configurations.Remove(config);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            _auditLogger.LogSecurityEvent("ConfigurationDeleted", $"Configuration {key} deleted", new Dictionary<string, object>
            {
                ["Key"] = key,
                ["AdminUser"] = User.Identity?.Name ?? "Unknown"
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Key}", key);
            return Problem("Failed to delete configuration", statusCode: 500);
        }
    }

    #endregion

    // Helper methods
    private UserDto MapToUserDto(UserEntity user)
    {
        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.IsSuspended,
            user.SuspendedAt,
            user.SuspendedReason,
            user.LastLoginAt,
            user.LastLoginIp,
            user.FailedLoginAttempts,
            user.LockoutEnd,
            user.EmailVerified,
            user.PhoneNumber,
            user.PhoneVerified,
            user.TwoFactorEnabled,
            user.CreatedAt,
            user.UpdatedAt,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.Quota != null ? new UserQuotaSummaryDto(
                user.Quota.ApiRequestsPerDay,
                user.Quota.ApiRequestsUsedToday,
                user.Quota.VideosPerMonth,
                user.Quota.VideosGeneratedThisMonth,
                user.Quota.StorageLimitBytes,
                user.Quota.StorageUsedBytes,
                user.Quota.AiTokensPerMonth,
                user.Quota.AiTokensUsedThisMonth,
                user.Quota.TotalCostUsd,
                user.Quota.CostLimitUsd
            ) : null
        );
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "Aura.VideoStudio.Salt");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
