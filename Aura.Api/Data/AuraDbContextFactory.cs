using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aura.Api.Data;

/// <summary>
/// Design-time factory for creating AuraDbContext during migrations
/// </summary>
public class AuraDbContextFactory : IDesignTimeDbContextFactory<AuraDbContext>
{
    public AuraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuraDbContext>();
        
        // Use a temporary database path for migrations
        var dbPath = Path.Combine(Path.GetTempPath(), "aura-migrations.db");
        var connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared;";
        
        optionsBuilder.UseSqlite(connectionString, 
            sqliteOptions => sqliteOptions.MigrationsAssembly("Aura.Api"));

        return new AuraDbContext(optionsBuilder.Options);
    }
}
