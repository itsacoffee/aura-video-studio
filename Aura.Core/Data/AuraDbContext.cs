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
    }
}
