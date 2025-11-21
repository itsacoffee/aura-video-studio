using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Cli.Commands;

/// <summary>
/// Database status command - displays current migration status
/// </summary>
public class StatusCommand : ICommand
{
    private readonly ILogger<StatusCommand> _logger;
    private readonly AuraDbContext _dbContext;

    public StatusCommand(ILogger<StatusCommand> logger, AuraDbContext dbContext)
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
        Console.WriteLine("║           Database Migration Status Report              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync().ConfigureAwait(false);
            
            if (!canConnect)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Cannot connect to database");
                Console.ResetColor();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Database connection successful");
            Console.ResetColor();
            Console.WriteLine();

            // Get applied migrations
            var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
            var appliedList = appliedMigrations.ToList();

            // Get pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            var pendingList = pendingMigrations.ToList();

            // Calculate total migrations (applied + pending)
            var totalCount = appliedList.Count + pendingList.Count;

            Console.WriteLine("Database Status:");
            Console.WriteLine($"  Total Migrations: {totalCount}");
            Console.WriteLine($"  Applied: {appliedList.Count}");
            Console.WriteLine($"  Pending: {pendingList.Count}");
            Console.WriteLine();

            if (appliedList.Count > 0)
            {
                Console.WriteLine("Applied Migrations:");
                if (options.Verbose)
                {
                    foreach (var migration in appliedList)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ {migration}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    var recentApplied = appliedList.TakeLast(5).ToList();
                    foreach (var migration in recentApplied)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ {migration}");
                        Console.ResetColor();
                    }
                    
                    if (appliedList.Count > 5)
                    {
                        Console.WriteLine($"  ... and {appliedList.Count - 5} more (use -v to see all)");
                    }
                }
                Console.WriteLine();
            }

            if (pendingList.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Pending Migrations:");
                Console.ResetColor();
                
                foreach (var migration in pendingList)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ {migration}");
                    Console.ResetColor();
                }
                Console.WriteLine();
                
                Console.WriteLine("Run 'aura-cli migrate' to apply pending migrations.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Database is up to date - no pending migrations");
                Console.ResetColor();
            }

            _logger.LogInformation("Database status checked via CLI: {Applied} applied, {Pending} pending", 
                appliedList.Count, pendingList.Count);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Failed to check database status: {ex.Message}");
            Console.ResetColor();

            if (options.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
            }

            _logger.LogError(ex, "Failed to check database status via CLI");

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
            }
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("aura-cli status - Display database migration status");
        Console.WriteLine();
        Console.WriteLine("Usage: aura-cli status [options]");
        Console.WriteLine();
        Console.WriteLine("Description:");
        Console.WriteLine("  Shows the current status of database migrations, including");
        Console.WriteLine("  applied migrations and any pending migrations that need to be applied.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help      Show this help message");
        Console.WriteLine("  -v, --verbose   Show all applied migrations (default shows last 5)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  aura-cli status");
        Console.WriteLine("  aura-cli status -v");
    }

    private class Options
    {
        public bool ShowHelp { get; set; }
        public bool Verbose { get; set; }
    }
}
