using Microsoft.EntityFrameworkCore;

namespace Aura.Core.Data;

/// <summary>
/// Factory for creating AuraDbContext instances with pre-configured options.
/// This is used to resolve DI scope conflicts when singleton services need to create DbContext instances.
/// </summary>
public class AuraDbContextFactory : IDbContextFactory<AuraDbContext>
{
    private readonly DbContextOptions<AuraDbContext> _options;

    public AuraDbContextFactory(DbContextOptions<AuraDbContext> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public AuraDbContext CreateDbContext()
    {
        return new AuraDbContext(_options);
    }
}
