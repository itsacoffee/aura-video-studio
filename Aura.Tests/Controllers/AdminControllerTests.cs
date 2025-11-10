using Aura.Api.Controllers;
using Aura.Api.Models.ApiModels.V1;
using Aura.Api.Security;
using Aura.Core.Data;
using Aura.Core.Services.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aura.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly AuraDbContext _dbContext;
    private readonly Mock<IAuditLogger> _mockAuditLogger;
    private readonly Mock<ILogger<AdminController>> _mockLogger;
    private readonly Mock<SystemResourceMonitor> _mockResourceMonitor;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new AuraDbContext(options);
        _mockAuditLogger = new Mock<IAuditLogger>();
        _mockLogger = new Mock<ILogger<AdminController>>();
        _mockResourceMonitor = new Mock<SystemResourceMonitor>();

        _controller = new AdminController(
            _dbContext,
            _mockAuditLogger.Object,
            _mockLogger.Object,
            _mockResourceMonitor.Object
        );

        // Setup controller context with admin user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed roles
        _dbContext.Roles.AddRange(
            new RoleEntity
            {
                Id = "role-admin",
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR",
                Description = "Full system access",
                IsSystemRole = true,
                Permissions = @"[""admin.full_access""]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RoleEntity
            {
                Id = "role-user",
                Name = "User",
                NormalizedName = "USER",
                Description = "Standard user access",
                IsSystemRole = true,
                Permissions = @"[""projects.manage""]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<ActionResult<UserListResponse>>(result);
        var response = Assert.IsType<UserListResponse>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Users);
        Assert.Equal("testuser", response.Users[0].Username);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetUser("user-1");

        // Assert
        var okResult = Assert.IsType<ActionResult<UserDto>>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("testuser", userDto.Username);
        Assert.Equal("test@example.com", userDto.Email);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetUser("invalid-id");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest(
            "newuser",
            "newuser@example.com",
            "New User",
            "password123",
            new List<string> { "role-user" },
            null
        );

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var userDto = Assert.IsType<UserDto>(createdResult.Value);
        Assert.Equal("newuser", userDto.Username);
        Assert.Equal("newuser@example.com", userDto.Email);

        // Verify user was added to database
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        Assert.NotNull(user);
        Assert.True(user!.IsActive);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new CreateUserRequest(
            "testuser",
            "different@example.com",
            null,
            "password",
            null,
            null
        );

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_UpdatesUser()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Old Name",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateUserRequest(
            "New Name",
            "newemail@example.com",
            null,
            null,
            null
        );

        // Act
        var result = await _controller.UpdateUser("user-1", request);

        // Assert
        var okResult = Assert.IsType<ActionResult<UserDto>>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("New Name", userDto.DisplayName);
        Assert.Equal("newemail@example.com", userDto.Email);
    }

    [Fact]
    public async Task SuspendUser_WithValidReason_SuspendsUser()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            IsSuspended = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new SuspendUserRequest(
            "Terms of service violation",
            DateTime.UtcNow.AddDays(7).ToString("o")
        );

        // Act
        var result = await _controller.SuspendUser("user-1", request);

        // Assert
        var okResult = Assert.IsType<ActionResult<UserDto>>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.True(userDto.IsSuspended);
        Assert.Equal("Terms of service violation", userDto.SuspendedReason);
        Assert.NotNull(userDto.SuspendedAt);
    }

    [Fact]
    public async Task UnsuspendUser_RemovesSuspension()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            IsSuspended = true,
            SuspendedAt = DateTime.UtcNow.AddDays(-1),
            SuspendedReason = "Test suspension",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.UnsuspendUser("user-1");

        // Assert
        var okResult = Assert.IsType<ActionResult<UserDto>>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.False(userDto.IsSuspended);
        Assert.Null(userDto.SuspendedAt);
        Assert.Null(userDto.SuspendedReason);
    }

    [Fact]
    public async Task DeleteUser_DeactivatesUser()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteUser("user-1");

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify user is deactivated
        var updatedUser = await _dbContext.Users.FindAsync("user-1");
        Assert.NotNull(updatedUser);
        Assert.False(updatedUser!.IsActive);
    }

    [Fact]
    public async Task AssignRoles_UpdatesUserRoles()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var roleIds = new List<string> { "role-user" };

        // Act
        var result = await _controller.AssignRoles("user-1", roleIds);

        // Assert
        var okResult = Assert.IsType<ActionResult<UserDto>>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Contains("User", userDto.Roles);
    }

    [Fact]
    public async Task UpdateUserQuota_SetsQuotaLimits()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var quotaRequest = new UserQuotaDto(
            ApiRequestsPerDay: 1000,
            VideosPerMonth: 50,
            StorageLimitBytes: 1073741824, // 1GB
            AiTokensPerMonth: 100000,
            MaxConcurrentRenders: 3,
            MaxConcurrentJobs: 5,
            CostLimitUsd: 100
        );

        // Act
        var result = await _controller.UpdateUserQuota("user-1", quotaRequest);

        // Assert
        var okResult = Assert.IsType<ActionResult<UserQuotaSummaryDto>>(result);
        var quota = Assert.IsType<UserQuotaSummaryDto>(okResult.Value);
        Assert.Equal(1000, quota.ApiRequestsPerDay);
        Assert.Equal(50, quota.VideosPerMonth);
        Assert.Equal(1073741824, quota.StorageLimitBytes);
    }

    [Fact]
    public async Task GetRoles_ReturnsAllRoles()
    {
        // Act
        var result = await _controller.GetRoles();

        // Assert
        var okResult = Assert.IsType<ActionResult<List<RoleDto>>>(result);
        var roles = Assert.IsType<List<RoleDto>>(okResult.Value);
        Assert.Equal(2, roles.Count); // Admin and User roles
    }

    [Fact]
    public async Task CreateRole_WithValidData_CreatesRole()
    {
        // Arrange
        var request = new CreateRoleRequest(
            "CustomRole",
            "Custom role for testing",
            new List<string> { "test.permission" }
        );

        // Act
        var result = await _controller.CreateRole(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var roleDto = Assert.IsType<RoleDto>(createdResult.Value);
        Assert.Equal("CustomRole", roleDto.Name);
        Assert.False(roleDto.IsSystemRole);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsLogs()
    {
        // Arrange
        var log = new AuditLogEntity
        {
            Id = "log-1",
            Timestamp = DateTime.UtcNow,
            UserId = "user-1",
            Username = "testuser",
            Action = "UserCreated",
            Success = true
        };
        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAuditLogs();

        // Assert
        var okResult = Assert.IsType<ActionResult<AuditLogResponse>>(result);
        var response = Assert.IsType<AuditLogResponse>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Logs);
    }

    [Fact]
    public async Task GetConfiguration_ReturnsAllConfigs()
    {
        // Arrange
        var config = new ConfigurationEntity
        {
            Key = "test.config",
            Value = "test value",
            Category = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetConfiguration();

        // Assert
        var okResult = Assert.IsType<ActionResult<List<ConfigurationItemDto>>>(result);
        var configs = Assert.IsType<List<ConfigurationItemDto>>(okResult.Value);
        Assert.Single(configs);
        Assert.Equal("test.config", configs[0].Key);
    }

    [Fact]
    public async Task UpdateConfiguration_CreatesNewConfig()
    {
        // Arrange
        var request = new UpdateConfigurationRequest(
            "new.config",
            "new value",
            "NewCategory",
            "Test configuration",
            false,
            true
        );

        // Act
        var result = await _controller.UpdateConfiguration("new.config", request);

        // Assert
        var okResult = Assert.IsType<ActionResult<ConfigurationItemDto>>(result);
        var config = Assert.IsType<ConfigurationItemDto>(okResult.Value);
        Assert.Equal("new.config", config.Key);
        Assert.Equal("new value", config.Value);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
