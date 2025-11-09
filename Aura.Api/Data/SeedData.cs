using Aura.Core.Data;
using Aura.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aura.Api.Data;

/// <summary>
/// Seeds the database with initial test data for local development
/// </summary>
public class SeedData
{
    private readonly AuraDbContext _context;
    private readonly ILogger<SeedData> _logger;

    public SeedData(AuraDbContext context, ILogger<SeedData> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed the database with test data if it's empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check if we already have data
            var hasData = await _context.Projects.AnyAsync();
            if (hasData)
            {
                _logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding database with test data...");

            // Seed test users/setup
            await SeedUserSetup();

            // Seed sample projects
            await SeedProjects();

            // Save changes
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private async Task SeedUserSetup()
    {
        var existingSetup = await _context.UserSetup.AnyAsync();
        if (existingSetup)
        {
            return;
        }

        // Create a default user setup indicating wizard has been completed
        var userSetup = new UserSetup
        {
            Id = Guid.NewGuid(),
            WizardCompleted = true,
            FirstRunDate = DateTime.UtcNow.AddDays(-7), // Simulate completed a week ago
            Version = "1.0.0",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserSetup.Add(userSetup);
        _logger.LogInformation("Seeded user setup data");
    }

    private async Task SeedProjects()
    {
        var existingProjects = await _context.Projects.AnyAsync();
        if (existingProjects)
        {
            return;
        }

        var projects = new[]
        {
            new Project
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Aura - Sample Project",
                Description = "This is a sample project to help you get started with Aura Video Studio. " +
                             "It demonstrates a simple video workflow with script, voiceover, and visuals.",
                Status = ProjectStatus.Draft,
                Theme = "Technology",
                DurationSeconds = 60,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                IsDeleted = false,
                Version = 1,
                Metadata = @"{""tags"":[""sample"",""tutorial"",""getting-started""]}"
            },
            new Project
            {
                Id = Guid.NewGuid(),
                Title = "Quick Start Tutorial",
                Description = "Learn how to create your first video in Aura using the Guided Mode. " +
                             "This tutorial covers the basics of script generation, voice selection, and rendering.",
                Status = ProjectStatus.Draft,
                Theme = "Education",
                DurationSeconds = 90,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                IsDeleted = false,
                Version = 1,
                Metadata = @"{""tags"":[""tutorial"",""beginner"",""guided-mode""]}"
            },
            new Project
            {
                Id = Guid.NewGuid(),
                Title = "Advanced Features Demo",
                Description = "Explore advanced features like ML Lab, custom prompt templates, " +
                             "and expert render controls. Perfect for power users.",
                Status = ProjectStatus.Draft,
                Theme = "Technology",
                DurationSeconds = 120,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-12),
                IsDeleted = false,
                Version = 1,
                Metadata = @"{""tags"":[""advanced"",""ml-lab"",""power-user""],""advancedMode"":true}"
            }
        };

        _context.Projects.AddRange(projects);
        _logger.LogInformation("Seeded {Count} sample projects", projects.Length);
    }
}
