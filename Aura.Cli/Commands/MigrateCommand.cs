using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Cli.Commands;

/// <summary>
/// Database migration command - applies pending migrations to the database
/// </summary>
public class MigrateCommand : ICommand
{
    private readonly ILogger<MigrateCommand> _logger;
    private readonly AuraDbContext _dbContext;

    public MigrateCommand(ILogger<MigrateCommand> logger, AuraDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args);

        if (options.ShowHelp)
        {
            ShowHelp();
            return 0;
        }

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Database Migration - Apply Pending Changes      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine("Checking for pending migrations...");
            
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            var pendingList = pendingMigrations.ToList();
            
            if (pendingList.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Database is up to date - no pending migrations");
                Console.ResetColor();
                return 0;
            }

            Console.WriteLine($"Found {pendingList.Count} pending migration(s):");
            Console.WriteLine();
            
            foreach (var migration in pendingList)
            {
                Console.WriteLine($"  • {migration}");
            }
            
            Console.WriteLine();

            if (options.DryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ DRY RUN MODE - Migrations not applied");
                Console.ResetColor();
                return 0;
            }

            Console.WriteLine("Applying migrations...");
            
            await _dbContext.Database.MigrateAsync().ConfigureAwait(false);
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Successfully applied {pendingList.Count} migration(s)");
            Console.ResetColor();
            
            _logger.LogInformation("Database migrations applied successfully via CLI: {MigrationCount} migrations", pendingList.Count);
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Migration failed: {ex.Message}");
            Console.ResetColor();
            
            if (options.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
            }
            
            _logger.LogError(ex, "Database migration failed via CLI");
            
            return 1;
        }
    }

    private static Options ParseOptions(string[] args)
    {
        var options = new Options();

        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;
                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
            }
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("aura-cli migrate - Apply pending database migrations");
        Console.WriteLine();
        Console.WriteLine("Usage: aura-cli migrate [options]");
        Console.WriteLine();
        Console.WriteLine("Description:");
        Console.WriteLine("  Applies all pending database migrations to bring the database");
        Console.WriteLine("  schema up to date with the current application version.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help      Show this help message");
        Console.WriteLine("  -v, --verbose   Enable verbose output with detailed error messages");
        Console.WriteLine("  --dry-run       Check for pending migrations without applying them");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli migrate");
        Console.WriteLine("  aura-cli migrate --dry-run");
        Console.WriteLine("  aura-cli migrate -v");
    }

    private class Options
    {
        public bool ShowHelp { get; set; }
        public bool Verbose { get; set; }
        public bool DryRun { get; set; }
    }
}
