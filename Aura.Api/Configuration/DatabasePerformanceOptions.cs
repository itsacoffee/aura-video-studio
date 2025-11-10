namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for database performance optimization
/// </summary>
public class DatabasePerformanceOptions
{
    /// <summary>
    /// Maximum number of database connections in the pool (PostgreSQL)
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum number of database connections in the pool (PostgreSQL)
    /// </summary>
    public int MinPoolSize { get; set; } = 10;

    /// <summary>
    /// Connection lifetime in seconds before being recycled (PostgreSQL)
    /// </summary>
    public int ConnectionLifetimeSeconds { get; set; } = 600; // 10 minutes

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable connection pooling statistics logging
    /// </summary>
    public bool EnablePoolingStats { get; set; } = true;

    /// <summary>
    /// Maximum retry count for transient failures
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Maximum retry delay in seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Enable query splitting for complex queries
    /// </summary>
    public bool EnableQuerySplitting { get; set; } = true;

    /// <summary>
    /// SQLite cache size in KB (negative values = size in KB)
    /// </summary>
    public int SqliteCacheSizeKB { get; set; } = 64000; // 64MB

    /// <summary>
    /// SQLite page size in bytes
    /// </summary>
    public int SqlitePageSize { get; set; } = 4096;

    /// <summary>
    /// Enable SQLite Write-Ahead Logging (WAL)
    /// </summary>
    public bool SqliteEnableWAL { get; set; } = true;
}
