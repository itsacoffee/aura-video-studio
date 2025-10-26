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
    }
}
