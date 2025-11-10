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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // Apply global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSoftDeleteQueryFilter();
            }
        }
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
