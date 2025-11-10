using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Aura.Core.Data;

/// <summary>
/// Database context for Aura Video Studio
/// </summary>
public class AuraDbContext : DbContext
{
    public AuraDbContext(DbContextOptions<AuraDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Override SaveChanges to automatically handle audit fields
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically handle audit fields
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Update audit fields for entities implementing IAuditableEntity and ISoftDeletable
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);

        var timestamp = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            // Handle IAuditableEntity
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAt = timestamp;
                    auditableEntity.UpdatedAt = timestamp;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.UpdatedAt = timestamp;
                }
            }

            // Handle soft delete
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                // Instead of actually deleting, mark as deleted
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = timestamp;
            }
        }
    }

    /// <summary>
    /// Export history records
    /// </summary>
    public DbSet<ExportHistoryEntity> ExportHistory { get; set; } = null!;

    /// <summary>
    /// Project templates
    /// </summary>
    public DbSet<TemplateEntity> Templates { get; set; } = null!;

    /// <summary>
    /// User setup status for first-run wizard
    /// </summary>
    public DbSet<UserSetupEntity> UserSetups { get; set; } = null!;

    /// <summary>
    /// Project states for persistence and recovery
    /// </summary>
    public DbSet<ProjectStateEntity> ProjectStates { get; set; } = null!;

    /// <summary>
    /// Scene states within projects
    /// </summary>
    public DbSet<SceneStateEntity> SceneStates { get; set; } = null!;

    /// <summary>
    /// Asset states (files) within projects
    /// </summary>
    public DbSet<AssetStateEntity> AssetStates { get; set; } = null!;

    /// <summary>
    /// Render checkpoints for recovery
    /// </summary>
    public DbSet<RenderCheckpointEntity> RenderCheckpoints { get; set; } = null!;

    /// <summary>
    /// Custom video templates
    /// </summary>
    public DbSet<CustomTemplateEntity> CustomTemplates { get; set; } = null!;

    /// <summary>
    /// Action log for server-side undo/redo operations
    /// </summary>
    public DbSet<ActionLogEntity> ActionLogs { get; set; } = null!;

    /// <summary>
    /// Project versions for snapshots and restore points
    /// </summary>
    public DbSet<ProjectVersionEntity> ProjectVersions { get; set; } = null!;

    /// <summary>
    /// Content blobs for deduplicated storage
    /// </summary>
    public DbSet<ContentBlobEntity> ContentBlobs { get; set; } = null!;

    /// <summary>
    /// Application configuration stored in database
    /// </summary>
    public DbSet<ConfigurationEntity> Configurations { get; set; } = null!;

    /// <summary>
    /// System-wide configuration and setup status
    /// </summary>
    public DbSet<SystemConfigurationEntity> SystemConfigurations { get; set; } = null!;

    /// <summary>
    /// System users
    /// </summary>
    public DbSet<UserEntity> Users { get; set; } = null!;

    /// <summary>
    /// User roles
    /// </summary>
    public DbSet<RoleEntity> Roles { get; set; } = null!;

    /// <summary>
    /// User role assignments
    /// </summary>
    public DbSet<UserRoleEntity> UserRoles { get; set; } = null!;

    /// <summary>
    /// User quotas and usage tracking
    /// </summary>
    public DbSet<UserQuotaEntity> UserQuotas { get; set; } = null!;

    /// <summary>
    /// Audit logs for administrative and security events
    /// </summary>
    public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Media library items
    /// </summary>
    public DbSet<MediaEntity> MediaItems { get; set; } = null!;

    /// <summary>
    /// Media collections (folders)
    /// </summary>
    public DbSet<MediaCollectionEntity> MediaCollections { get; set; } = null!;

    /// <summary>
    /// Media tags
    /// </summary>
    public DbSet<MediaTagEntity> MediaTags { get; set; } = null!;

    /// <summary>
    /// Media usage tracking
    /// </summary>
    public DbSet<MediaUsageEntity> MediaUsages { get; set; } = null!;

    /// <summary>
    /// Upload sessions for chunked uploads
    /// </summary>
    public DbSet<UploadSessionEntity> UploadSessions { get; set; } = null!;

    /// <summary>
    /// Job queue entries for background processing
    /// </summary>
    public DbSet<JobQueueEntity> JobQueue { get; set; } = null!;

    /// <summary>
    /// Job progress history for tracking and analytics
    /// </summary>
    public DbSet<JobProgressHistoryEntity> JobProgressHistory { get; set; } = null!;

    /// <summary>
    /// Queue configuration settings
    /// </summary>
    public DbSet<QueueConfigurationEntity> QueueConfiguration { get; set; } = null!;

    /// <summary>
    /// Usage statistics for local analytics
    /// </summary>
    public DbSet<UsageStatisticsEntity> UsageStatistics { get; set; } = null!;

    /// <summary>
    /// Cost tracking for budget monitoring
    /// </summary>
    public DbSet<CostTrackingEntity> CostTracking { get; set; } = null!;

    /// <summary>
    /// Performance metrics for optimization insights
    /// </summary>
    public DbSet<PerformanceMetricsEntity> PerformanceMetrics { get; set; } = null!;

    /// <summary>
    /// Analytics data retention settings
    /// </summary>
    public DbSet<AnalyticsRetentionSettingsEntity> AnalyticsRetentionSettings { get; set; } = null!;

    /// <summary>
    /// Pre-aggregated analytics summaries
    /// </summary>
    public DbSet<AnalyticsSummaryEntity> AnalyticsSummaries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure value converters for enums (store as strings for readability)
        ConfigureEnumConverters(modelBuilder);

        // Configure ExportHistoryEntity
        modelBuilder.Entity<ExportHistoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });

        // Configure TemplateEntity
        modelBuilder.Entity<TemplateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsSystemTemplate);
            entity.HasIndex(e => e.IsCommunityTemplate);
            entity.HasIndex(e => new { e.Category, e.SubCategory });
        });

        // Configure UserSetupEntity
        modelBuilder.Entity<UserSetupEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Completed);
            entity.HasIndex(e => e.UpdatedAt);
        });

        // Configure ProjectStateEntity
        modelBuilder.Entity<ProjectStateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => new { e.Status, e.UpdatedAt });
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt });
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Category, e.CreatedAt });
            entity.HasIndex(e => new { e.Status, e.Category });
        });

        // Configure SceneStateEntity
        modelBuilder.Entity<SceneStateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.ProjectId, e.SceneIndex });
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Scenes)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AssetStateEntity
        modelBuilder.Entity<AssetStateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.ProjectId, e.AssetType });
            entity.HasIndex(e => e.IsTemporary);
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Assets)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RenderCheckpointEntity
        modelBuilder.Entity<RenderCheckpointEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.ProjectId, e.StageName });
            entity.HasIndex(e => e.CheckpointTime);
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Checkpoints)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CustomTemplateEntity
        modelBuilder.Entity<CustomTemplateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Category, e.CreatedAt });
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt });
        });

        // Configure ActionLogEntity
        modelBuilder.Entity<ActionLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ActionType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => new { e.Status, e.Timestamp });
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Configure ProjectVersionEntity
        modelBuilder.Entity<ProjectVersionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.ProjectId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.VersionType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ProjectId, e.CreatedAt });
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt });
            entity.HasIndex(e => e.IsMarkedImportant);
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ContentBlobEntity
        modelBuilder.Entity<ContentBlobEntity>(entity =>
        {
            entity.HasKey(e => e.ContentHash);
            entity.HasIndex(e => e.ContentType);
            entity.HasIndex(e => e.LastReferencedAt);
            entity.HasIndex(e => e.ReferenceCount);
        });

        // Configure ConfigurationEntity
        modelBuilder.Entity<ConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsSensitive);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => new { e.Category, e.IsActive });
            entity.HasIndex(e => new { e.Category, e.UpdatedAt });
        });

        // Configure SystemConfigurationEntity
        modelBuilder.Entity<SystemConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Seed default record
            entity.HasData(new SystemConfigurationEntity
            {
                Id = 1,
                IsSetupComplete = false,
                FFmpegPath = null,
                OutputDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AuraVideoStudio",
                    "Output"
                ),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        });

        // Configure UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSuspended);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure RoleEntity
        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.HasIndex(e => e.IsSystemRole);

            // Seed default roles
            entity.HasData(
                new RoleEntity
                {
                    Id = "role-admin",
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    Description = "Full system access",
                    IsSystemRole = true,
                    Permissions = @"[""admin.full_access"",""users.manage"",""config.write"",""audit.view""]",
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
                    Permissions = @"[""projects.manage"",""videos.create"",""assets.manage""]",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new RoleEntity
                {
                    Id = "role-viewer",
                    Name = "Viewer",
                    NormalizedName = "VIEWER",
                    Description = "Read-only access",
                    IsSystemRole = true,
                    Permissions = @"[""projects.view"",""videos.view""]",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        });

        // Configure UserRoleEntity
        modelBuilder.Entity<UserRoleEntity>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserQuotaEntity
        modelBuilder.Entity<UserQuotaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithOne(u => u.Quota)
                .HasForeignKey<UserQuotaEntity>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLogEntity
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.ResourceType);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => new { e.Action, e.Timestamp });
        });

        // Configure MediaEntity
        modelBuilder.Entity<MediaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.ProcessingStatus);
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => new { e.Type, e.CreatedAt });
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt });
            entity.HasOne(e => e.Collection)
                .WithMany(c => c.MediaItems)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure MediaCollectionEntity
        modelBuilder.Entity<MediaCollectionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt });
        });

        // Configure MediaTagEntity
        modelBuilder.Entity<MediaTagEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MediaId);
            entity.HasIndex(e => e.Tag);
            entity.HasIndex(e => new { e.MediaId, e.Tag }).IsUnique();
            entity.HasOne(e => e.Media)
                .WithMany(m => m.Tags)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure MediaUsageEntity
        modelBuilder.Entity<MediaUsageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MediaId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.UsedAt);
            entity.HasIndex(e => new { e.MediaId, e.UsedAt });
            entity.HasOne(e => e.Media)
                .WithMany(m => m.Usages)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UploadSessionEntity
        modelBuilder.Entity<UploadSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure JobQueueEntity
        modelBuilder.Entity<JobQueueEntity>(entity =>
        {
            entity.HasKey(e => e.JobId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => new { e.Status, e.Priority });
            entity.HasIndex(e => e.EnqueuedAt);
            entity.HasIndex(e => e.NextRetryAt);
            entity.HasIndex(e => e.WorkerId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure JobProgressHistoryEntity
        modelBuilder.Entity<JobProgressHistoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.JobId, e.Timestamp });
        });

        // Configure QueueConfigurationEntity
        modelBuilder.Entity<QueueConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Seed default configuration
            entity.HasData(new QueueConfigurationEntity
            {
                Id = 1,
                MaxConcurrentJobs = 2,
                PauseOnBattery = true,
                CpuThrottleThreshold = 85,
                MemoryThrottleThreshold = 85,
                IsEnabled = true,
                PollingIntervalSeconds = 5,
                JobHistoryRetentionDays = 7,
                FailedJobRetentionDays = 30,
                RetryBaseDelaySeconds = 5,
                RetryMaxDelaySeconds = 300,
                EnableNotifications = true,
                UpdatedAt = DateTime.UtcNow
            });
        });

        // Configure UsageStatisticsEntity
        modelBuilder.Entity<UsageStatisticsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.GenerationType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => new { e.Provider, e.Timestamp });
            entity.HasIndex(e => new { e.GenerationType, e.Timestamp });
            entity.HasIndex(e => new { e.Success, e.Timestamp });
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.JobId);
        });

        // Configure CostTrackingEntity
        modelBuilder.Entity<CostTrackingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.YearMonth);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.Provider, e.YearMonth });
            entity.HasIndex(e => new { e.YearMonth, e.Timestamp });
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.UsageStatisticsId);
            entity.HasOne(e => e.UsageStatistics)
                .WithMany()
                .HasForeignKey(e => e.UsageStatisticsId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure PerformanceMetricsEntity
        modelBuilder.Entity<PerformanceMetricsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => new { e.OperationType, e.Timestamp });
            entity.HasIndex(e => new { e.Success, e.Timestamp });
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.JobId);
        });

        // Configure AnalyticsRetentionSettingsEntity
        modelBuilder.Entity<AnalyticsRetentionSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Seed default settings
            entity.HasData(new AnalyticsRetentionSettingsEntity
            {
                Id = 1,
                IsEnabled = true,
                UsageStatisticsRetentionDays = 90,
                CostTrackingRetentionDays = 365,
                PerformanceMetricsRetentionDays = 30,
                AutoCleanupEnabled = true,
                CleanupHourUtc = 3,
                TrackSuccessOnly = false,
                CollectHardwareMetrics = true,
                AggregateOldData = true,
                AggregationThresholdDays = 30,
                MaxDatabaseSizeMB = 500,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        });

        // Configure AnalyticsSummaryEntity
        modelBuilder.Entity<AnalyticsSummaryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodType);
            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => new { e.PeriodType, e.PeriodId }).IsUnique();
            entity.HasIndex(e => e.PeriodStart);
            entity.HasIndex(e => new { e.PeriodType, e.PeriodStart });
        });

        // Apply global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSoftDeleteQueryFilter();
            }
        }

        // Configure row version for optimistic concurrency
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IVersionedEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property("RowVersion")
                    .IsRowVersion();
            }
        }
    }

    /// <summary>
    /// Configure enum to string converters for better readability in database
    /// </summary>
    private static void ConfigureEnumConverters(ModelBuilder modelBuilder)
    {
        // No enum columns in entities currently - all status fields are already strings
        // This method is here for future use when enum columns are added
    }
}

/// <summary>
/// Extension methods for EF Core model building
/// </summary>
internal static class ModelBuilderExtensions
{
    /// <summary>
    /// Add a global query filter to exclude soft-deleted entities
    /// </summary>
    public static void AddSoftDeleteQueryFilter(this Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var filter = System.Linq.Expressions.Expression.Lambda(
            System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
            parameter);

        entityType.SetQueryFilter(filter);
    }
}
