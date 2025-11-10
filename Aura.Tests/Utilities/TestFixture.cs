using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Utilities;

/// <summary>
/// Base test fixture providing common test setup and utilities
/// </summary>
public abstract class TestFixture : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected ILoggerFactory LoggerFactory { get; }
    private readonly List<string> _temporaryFiles = new();
    private readonly List<string> _temporaryDirectories = new();
    private bool _disposed;

    protected TestFixture()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure services
        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();
        LoggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
    }

    /// <summary>
    /// Override to configure test services
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Creates a temporary file for testing
    /// </summary>
    protected string CreateTempFile(string? content = null, string? extension = null)
    {
        var fileName = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}{extension ?? ".tmp"}");
        
        if (content != null)
        {
            File.WriteAllText(fileName, content);
        }
        else
        {
            File.Create(fileName).Dispose();
        }

        _temporaryFiles.Add(fileName);
        return fileName;
    }

    /// <summary>
    /// Creates a temporary directory for testing
    /// </summary>
    protected string CreateTempDirectory()
    {
        var dirName = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(dirName);
        _temporaryDirectories.Add(dirName);
        return dirName;
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a logger for the specified type
    /// </summary>
    protected ILogger<T> GetLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }

    public virtual void Dispose()
    {
        if (_disposed) return;

        // Clean up temporary files
        foreach (var file in _temporaryFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        // Clean up temporary directories
        foreach (var dir in _temporaryDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        // Dispose service provider
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Collection definition for test fixtures
/// Allows sharing fixtures across test classes
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class is just a marker for xUnit to collect tests
}

/// <summary>
/// Shared fixture for integration tests
/// </summary>
public class IntegrationTestFixture : TestFixture
{
    public IntegrationTestFixture()
    {
        // Initialization code that runs once per test collection
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        
        // Add common integration test services
        // services.AddDbContext<TestDbContext>();
        // services.AddHttpClient();
        // etc.
    }
}
