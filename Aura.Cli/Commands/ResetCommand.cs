using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Cli.Commands;

/// <summary>
/// Database reset command - drops the database and recreates it with all migrations
/// </summary>
public class ResetCommand : ICommand
{
    private readonly ILogger<ResetCommand> _logger;
    private readonly AuraDbContext _dbContext;

    public ResetCommand(ILogger<ResetCommand> logger, AuraDbContext dbContext)
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
        Console.WriteLine("║         Database Reset - Drop and Recreate              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("⚠ WARNING: This will delete ALL data in the database!");
        Console.ResetColor();
        Console.WriteLine();

        if (!options.Force)
        {
            Console.Write("Are you sure you want to continue? Type 'yes' to confirm: ");
            var confirmation = Console.ReadLine();

            if (confirmation?.ToLowerInvariant() != "yes")
            {
                Console.WriteLine("Operation cancelled.");
                return 0;
            }
        }

        Console.WriteLine();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync().ConfigureAwait(false);

            if (canConnect)
            {
                Console.WriteLine("Dropping existing database...");
                
                if (options.DryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ DRY RUN MODE - Database would be dropped here");
                    Console.ResetColor();
                }
                else
                {
                    await _dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Database dropped successfully");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("Database does not exist yet.");
            }

            Console.WriteLine();
            Console.WriteLine("Creating database with all migrations...");

            if (options.DryRun)
            {
                // In dry-run mode, show what migrations would be applied
                // We can't use GetMigrationsAsync, so we'll show pending + applied
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
                var totalCount = appliedMigrations.Count() + pendingMigrations.Count();
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ DRY RUN MODE - Would apply approximately {totalCount} migration(s)");
                Console.ResetColor();
            }
            else
            {
                await _dbContext.Database.MigrateAsync().ConfigureAwait(false);
                
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
                var appliedCount = appliedMigrations.Count();
                
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Database created successfully with {appliedCount} migration(s)");
                Console.ResetColor();
            }

            if (!options.DryRun)
            {
                _logger.LogWarning("Database reset via CLI - all data deleted and migrations reapplied");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Database reset failed: {ex.Message}");
            Console.ResetColor();

            if (options.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
            }

            _logger.LogError(ex, "Database reset failed via CLI");

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
                case "-f":
                case "--force":
                    options.Force = true;
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
        Console.WriteLine("aura-cli reset - Drop and recreate the database");
        Console.WriteLine();
        Console.WriteLine("Usage: aura-cli reset [options]");
        Console.WriteLine();
        Console.WriteLine("Description:");
        Console.WriteLine("  Drops the existing database and recreates it by applying all");
        Console.WriteLine("  migrations from scratch. This will DELETE ALL DATA.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help      Show this help message");
        Console.WriteLine("  -v, --verbose   Enable verbose output with detailed error messages");
        Console.WriteLine("  -f, --force     Skip confirmation prompt");
        Console.WriteLine("  --dry-run       Show what would be done without actually doing it");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli reset");
        Console.WriteLine("  aura-cli reset --force");
        Console.WriteLine("  aura-cli reset --dry-run");
        Console.WriteLine();
        Console.WriteLine("WARNING:");
        Console.WriteLine("  This command will permanently delete all data in the database.");
        Console.WriteLine("  Make sure you have backups before running this command.");
    }

    private class Options
    {
        public bool ShowHelp { get; set; }
        public bool Verbose { get; set; }
        public bool Force { get; set; }
        public bool DryRun { get; set; }
    }
}
