using Microsoft.EntityFrameworkCore;

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
        });
    }
}
