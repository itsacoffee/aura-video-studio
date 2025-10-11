using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aura.Core.Hardware;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Aura.Cli.Commands;

namespace Aura.Tests.Cli;

/// <summary>
/// Integration tests for CLI commands
/// </summary>
public class CliCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IServiceProvider _services;

    public CliCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aura-cli-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Error);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<HardwareDetector>();
                services.AddTransient<RuleBasedLlmProvider>();
                services.AddTransient<ILlmProvider>(sp => sp.GetRequiredService<RuleBasedLlmProvider>());
                services.AddTransient<PreflightCommand>();
                services.AddTransient<ScriptCommand>();
                services.AddTransient<ComposeCommand>();
                services.AddTransient<RenderCommand>();
                services.AddTransient<QuickCommand>();
            })
            .Build();

        _services = host.Services;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PreflightCommand_Should_Complete_Successfully()
    {
        // Arrange
        var command = _services.GetRequiredService<PreflightCommand>();
        var args = new[] { "--verbose" };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task QuickCommand_Should_Generate_Files()
    {
        // Arrange
        var command = _services.GetRequiredService<QuickCommand>();
        var outputDir = Path.Combine(_tempDir, "quick-output");
        var args = new[] 
        { 
            "-t", "Test Topic",
            "-d", "2",
            "-o", outputDir
        };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(outputDir));
        Assert.True(File.Exists(Path.Combine(outputDir, "brief.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, "plan.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, "script.txt")));

        // Verify content
        var scriptContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "script.txt"));
        Assert.NotEmpty(scriptContent);
        Assert.Contains("Test Topic", scriptContent);
    }

    [Fact]
    public async Task QuickCommand_DryRun_Should_Not_Generate_Files()
    {
        // Arrange
        var command = _services.GetRequiredService<QuickCommand>();
        var outputDir = Path.Combine(_tempDir, "dry-run-output");
        var args = new[] 
        { 
            "-t", "Test Topic",
            "-o", outputDir,
            "--dry-run"
        };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(outputDir));
    }

    [Fact]
    public async Task QuickCommand_Without_Topic_Should_Show_Help()
    {
        // Arrange
        var command = _services.GetRequiredService<QuickCommand>();
        var args = Array.Empty<string>();

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(1, exitCode); // Should fail without topic
    }

    [Fact]
    public async Task ScriptCommand_Should_Fail_Without_Required_Args()
    {
        // Arrange
        var command = _services.GetRequiredService<ScriptCommand>();
        var args = Array.Empty<string>();

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert - should fail without required arguments
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ComposeCommand_Should_Fail_Without_Input()
    {
        // Arrange
        var command = _services.GetRequiredService<ComposeCommand>();
        var args = Array.Empty<string>();

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(Aura.Cli.ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task ComposeCommand_Should_Process_Valid_Input()
    {
        // Arrange
        var command = _services.GetRequiredService<ComposeCommand>();
        var inputFile = Path.Combine(_tempDir, "timeline.json");
        var outputFile = Path.Combine(_tempDir, "compose-plan.json");
        
        // Create a sample timeline JSON
        await File.WriteAllTextAsync(inputFile, "{\"timeline\": \"sample\"}");
        
        var args = new[] { "-i", inputFile, "-o", outputFile };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
    }

    [Fact]
    public async Task RenderCommand_Should_Fail_Without_RenderSpec()
    {
        // Arrange
        var command = _services.GetRequiredService<RenderCommand>();
        var args = Array.Empty<string>();

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(Aura.Cli.ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task RenderCommand_DryRun_Should_Complete()
    {
        // Arrange
        var command = _services.GetRequiredService<RenderCommand>();
        var specFile = Path.Combine(_tempDir, "spec.json");
        
        // Create a sample render spec
        await File.WriteAllTextAsync(specFile, "{\"spec\": \"sample\"}");
        
        var args = new[] { "-r", specFile, "--dry-run" };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert - dry run should succeed even without FFmpeg
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task QuickCommand_With_Profile_Should_Complete()
    {
        // Arrange
        var command = _services.GetRequiredService<QuickCommand>();
        var outputDir = Path.Combine(_tempDir, "profile-output");
        var args = new[] 
        { 
            "-t", "Test Topic",
            "-o", outputDir,
            "--profile", "Free-Only",
            "--dry-run"
        };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task QuickCommand_Offline_Mode_Should_Complete()
    {
        // Arrange
        var command = _services.GetRequiredService<QuickCommand>();
        var outputDir = Path.Combine(_tempDir, "offline-output");
        var args = new[] 
        { 
            "-t", "Offline Test",
            "-o", outputDir,
            "--offline",
            "--dry-run"
        };

        // Act
        var exitCode = await command.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
    }
}
