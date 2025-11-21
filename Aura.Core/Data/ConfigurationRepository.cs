using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Aura.Core.Data;

/// <summary>
/// Repository for configuration operations with caching and validation
/// </summary>
public class ConfigurationRepository
{
    private readonly AuraDbContext? _context;
    private readonly IDbContextFactory<AuraDbContext>? _contextFactory;
    private readonly ILogger<ConfigurationRepository> _logger;
    private readonly ResiliencePipeline<List<ConfigurationEntity>> _retryPipeline;

    // Constructor for UnitOfWork pattern (backward compatibility)
    public ConfigurationRepository(
        AuraDbContext context,
        ILogger<ConfigurationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPipeline = BuildRetryPipeline();
    }

    // Constructor for factory pattern (new, for ConfigurationManager)
    public ConfigurationRepository(
        IDbContextFactory<AuraDbContext> contextFactory,
        ILogger<ConfigurationRepository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPipeline = BuildRetryPipeline();
    }

    private ResiliencePipeline<List<ConfigurationEntity>> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<List<ConfigurationEntity>>()
            .AddRetry(new RetryStrategyOptions<List<ConfigurationEntity>>
            {
                ShouldHandle = new PredicateBuilder<List<ConfigurationEntity>>()
                    .Handle<Microsoft.Data.Sqlite.SqliteException>(ex => 
                        // Don't retry schema errors (SQLITE_ERROR = 1) - these are not transient
                        ex.SqliteErrorCode != 1),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {AttemptNumber}/3 for configuration query. Reason: {Exception}",
                        args.AttemptNumber,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private async Task<AuraDbContext> GetContextAsync(CancellationToken ct = default)
    {
        if (_context != null)
        {
            return _context;
        }
        
        if (_contextFactory != null)
        {
            return await _contextFactory.CreateDbContextAsync(ct);
        }
        
        throw new InvalidOperationException("Neither context nor context factory is available");
    }

    /// <summary>
    /// Get a configuration value by key
    /// </summary>
    public async Task<ConfigurationEntity?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var context = await GetContextAsync(ct);
            var shouldDispose = _contextFactory != null;
            
            try
            {
                return await context.Configurations
                    .Where(c => c.Key == key && c.IsActive)
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                if (shouldDispose)
                {
                    await context.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Get all configurations in a category
    /// </summary>
    public async Task<List<ConfigurationEntity>> GetByCategoryAsync(
        string category, 
        CancellationToken ct = default)
    {
        try
        {
            var context = await GetContextAsync(ct);
            var shouldDispose = _contextFactory != null;
            
            try
            {
                return await context.Configurations
                    .Where(c => c.Category == category && c.IsActive)
                    .OrderBy(c => c.Key)
                    .ToListAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                if (shouldDispose)
                {
                    await context.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for category {Category}", category);
            throw;
        }
    }

    /// <summary>
    /// Get all active configurations
    /// </summary>
    public async Task<List<ConfigurationEntity>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        try
        {
            return await _retryPipeline.ExecuteAsync(async token =>
            {
                var context = await GetContextAsync(token);
                var shouldDispose = _contextFactory != null;
                
                try
                {
                    var query = context.Configurations.AsQueryable();
                    
                    if (!includeInactive)
                    {
                        query = query.Where(c => c.IsActive);
                    }
                    
                    return await query
                        .OrderBy(c => c.Category)
                        .ThenBy(c => c.Key)
                        .ToListAsync(token);
                }
                finally
                {
                    if (shouldDispose)
                    {
                        await context.DisposeAsync();
                    }
                }
            }, ct);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // SQLITE_ERROR (1) - typically indicates schema mismatch like "no such column"
            _logger.LogError(ex, "Database schema mismatch detected in Configurations table: {Message}", ex.Message);
            _logger.LogWarning("This usually means migrations need to be applied or the database needs to be recreated.");
            _logger.LogWarning("Try deleting the database file at: {DbPath}", GetDatabasePath());
            
            // Return empty list to allow application to continue with defaults
            return new List<ConfigurationEntity>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configurations");
            
            // Return empty list to allow graceful degradation
            return new List<ConfigurationEntity>();
        }
    }

    private string GetDatabasePath()
    {
        try
        {
            var auraDataRoot = Environment.GetEnvironmentVariable("AURA_DATA_ROOT") 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura");
            return Path.Combine(auraDataRoot, "aura.db");
        }
        catch
        {
            return "[Unable to determine database path]";
        }
    }

    /// <summary>
    /// Set or update a configuration value
    /// </summary>
    public async Task<ConfigurationEntity> SetAsync(
        string key,
        string value,
        string category,
        string valueType = "string",
        string? description = null,
        bool isSensitive = false,
        string? modifiedBy = null,
        CancellationToken ct = default)
    {
        try
        {
            var context = await GetContextAsync(ct);
            var shouldDispose = _contextFactory != null;
            
            try
            {
                var existing = await context.Configurations
                    .Where(c => c.Key == key)
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);

                if (existing != null)
                {
                    existing.Value = value;
                    existing.Category = category;
                    existing.ValueType = valueType;
                    existing.Description = description;
                    existing.IsSensitive = isSensitive;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.ModifiedBy = modifiedBy;
                    existing.Version++;

                    _logger.LogInformation(
                        "Updated configuration {Key} in category {Category} (version {Version})",
                        key, category, existing.Version);
                }
                else
                {
                    existing = new ConfigurationEntity
                    {
                        Key = key,
                        Value = value,
                        Category = category,
                        ValueType = valueType,
                        Description = description,
                        IsSensitive = isSensitive,
                        ModifiedBy = modifiedBy,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = 1
                    };

                    context.Configurations.Add(existing);

                    _logger.LogInformation(
                        "Created new configuration {Key} in category {Category}",
                        key, category);
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return existing;
            }
            finally
            {
                if (shouldDispose)
                {
                    await context.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Set multiple configurations in a single transaction
    /// </summary>
    public async Task<List<ConfigurationEntity>> SetManyAsync(
        Dictionary<string, (string value, string category, string valueType)> configurations,
        string? modifiedBy = null,
        CancellationToken ct = default)
    {
        var results = new List<ConfigurationEntity>();

        try
        {
            var context = await GetContextAsync(ct);
            var shouldDispose = _contextFactory != null;
            
            try
            {
                using var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

                foreach (var kvp in configurations)
                {
                    var existing = await context.Configurations
                        .Where(c => c.Key == kvp.Key)
                        .FirstOrDefaultAsync(ct).ConfigureAwait(false);

                    if (existing != null)
                    {
                        existing.Value = kvp.Value.value;
                        existing.Category = kvp.Value.category;
                        existing.ValueType = kvp.Value.valueType;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.ModifiedBy = modifiedBy;
                        existing.Version++;
                    }
                    else
                    {
                        existing = new ConfigurationEntity
                        {
                            Key = kvp.Key,
                            Value = kvp.Value.value,
                            Category = kvp.Value.category,
                            ValueType = kvp.Value.valueType,
                            ModifiedBy = modifiedBy,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = 1
                        };

                        context.Configurations.Add(existing);
                    }

                    results.Add(existing);
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "Bulk updated {Count} configurations",
                    configurations.Count);

                return results;
            }
            finally
            {
                if (shouldDispose)
                {
                    await context.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple configurations");
            throw;
        }
    }

    /// <summary>
    /// Delete a configuration (soft delete by setting IsActive = false)
    /// </summary>
    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var context = await GetContextAsync(ct);
            var shouldDispose = _contextFactory != null;
            
            try
            {
                var config = await context.Configurations
                    .Where(c => c.Key == key)
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);

                if (config == null)
                {
                    return false;
                }

                config.IsActive = false;
                config.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync(ct).ConfigureAwait(false);

                _logger.LogInformation("Deleted configuration {Key}", key);
                return true;
            }
            finally
            {
                if (shouldDispose)
                {
                    await context.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Get configuration history (all versions)
    /// </summary>
    public async Task<List<ConfigurationEntity>> GetHistoryAsync(
        string key,
        CancellationToken ct = default)
    {
        try
        {
            var context = await GetContextAsync(ct);
            var shouldDispose = _contextFactory != null;
            
            try
            {
                return await context.Configurations
                    .Where(c => c.Key == key)
                    .OrderByDescending(c => c.Version)
                    .ToListAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                if (shouldDispose)
                {
                    await context.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for configuration {Key}", key);
            throw;
        }
    }
}
