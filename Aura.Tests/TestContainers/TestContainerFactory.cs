using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Tests.TestContainers;

/// <summary>
/// Factory for creating and managing test containers
/// Provides isolated test environments for integration testing
/// </summary>
public class TestContainerFactory : IDisposable
{
    private readonly List<IDisposable> _containers = new();
    private readonly ILogger<TestContainerFactory> _logger;
    private bool _disposed;

    public TestContainerFactory(ILogger<TestContainerFactory>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TestContainerFactory>.Instance;
    }

    /// <summary>
    /// Creates a test database container (e.g., PostgreSQL, SQL Server)
    /// </summary>
    public async Task<ITestContainer> CreateDatabaseContainerAsync(DatabaseType type = DatabaseType.InMemory)
    {
        _logger.LogInformation("Creating {DatabaseType} test container", type);
        
        ITestContainer container = type switch
        {
            DatabaseType.InMemory => new InMemoryDatabaseContainer(),
            DatabaseType.Sqlite => new SqliteTestContainer(),
            DatabaseType.PostgreSql => throw new NotImplementedException("PostgreSQL container not yet implemented"),
            DatabaseType.SqlServer => throw new NotImplementedException("SQL Server container not yet implemented"),
            _ => throw new ArgumentException($"Unknown database type: {type}", nameof(type))
        };

        await container.StartAsync();
        _containers.Add(container);
        
        _logger.LogInformation("Test container started: {ContainerType}", container.GetType().Name);
        
        return container;
    }

    /// <summary>
    /// Creates a message queue container (e.g., RabbitMQ, Redis)
    /// </summary>
    public async Task<ITestContainer> CreateMessageQueueContainerAsync(MessageQueueType type = MessageQueueType.InMemory)
    {
        _logger.LogInformation("Creating {QueueType} test container", type);
        
        ITestContainer container = type switch
        {
            MessageQueueType.InMemory => new InMemoryQueueContainer(),
            MessageQueueType.Redis => throw new NotImplementedException("Redis container not yet implemented"),
            MessageQueueType.RabbitMq => throw new NotImplementedException("RabbitMQ container not yet implemented"),
            _ => throw new ArgumentException($"Unknown queue type: {type}", nameof(type))
        };

        await container.StartAsync();
        _containers.Add(container);
        
        return container;
    }

    /// <summary>
    /// Creates a blob storage container (e.g., Azurite, MinIO)
    /// </summary>
    public async Task<ITestContainer> CreateBlobStorageContainerAsync(StorageType type = StorageType.InMemory)
    {
        _logger.LogInformation("Creating {StorageType} test container", type);
        
        ITestContainer container = type switch
        {
            StorageType.InMemory => new InMemoryStorageContainer(),
            StorageType.Azurite => throw new NotImplementedException("Azurite container not yet implemented"),
            StorageType.MinIO => throw new NotImplementedException("MinIO container not yet implemented"),
            _ => throw new ArgumentException($"Unknown storage type: {type}", nameof(type))
        };

        await container.StartAsync();
        _containers.Add(container);
        
        return container;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing {Count} test containers", _containers.Count);

        foreach (var container in _containers)
        {
            try
            {
                container.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing test container");
            }
        }

        _containers.Clear();
        _disposed = true;
    }
}

/// <summary>
/// Base interface for test containers
/// </summary>
public interface ITestContainer : IDisposable
{
    string ConnectionString { get; }
    Task StartAsync();
    Task StopAsync();
}

public enum DatabaseType
{
    InMemory,
    Sqlite,
    PostgreSql,
    SqlServer
}

public enum MessageQueueType
{
    InMemory,
    Redis,
    RabbitMq
}

public enum StorageType
{
    InMemory,
    Azurite,
    MinIO
}

/// <summary>
/// In-memory database container for fast testing
/// </summary>
internal class InMemoryDatabaseContainer : ITestContainer
{
    public string ConnectionString => "DataSource=:memory:";

    public Task StartAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public void Dispose() { }
}

/// <summary>
/// SQLite test container
/// </summary>
internal class SqliteTestContainer : ITestContainer
{
    private readonly string _databaseFile;

    public SqliteTestContainer()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
    }

    public string ConnectionString => $"Data Source={_databaseFile}";

    public Task StartAsync()
    {
        // SQLite doesn't need explicit start
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (File.Exists(_databaseFile))
        {
            try
            {
                File.Delete(_databaseFile);
            }
            catch
            {
                // Best effort cleanup
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}

/// <summary>
/// In-memory queue container
/// </summary>
internal class InMemoryQueueContainer : ITestContainer
{
    public string ConnectionString => "memory://localhost";

    public Task StartAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public void Dispose() { }
}

/// <summary>
/// In-memory storage container
/// </summary>
internal class InMemoryStorageContainer : ITestContainer
{
    public string ConnectionString => "memory://localhost/storage";

    public Task StartAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public void Dispose() { }
}
