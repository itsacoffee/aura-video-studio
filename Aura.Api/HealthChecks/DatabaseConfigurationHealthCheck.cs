using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates database configuration is correct
/// Catches unsupported connection string parameters early
/// </summary>
public class DatabaseConfigurationHealthCheck : IHealthCheck
{
    private readonly ILogger<DatabaseConfigurationHealthCheck> _logger;
    private readonly IConfiguration _configuration;
    private readonly DatabaseConfigurationValidator _validator;

    public DatabaseConfigurationHealthCheck(
        ILogger<DatabaseConfigurationHealthCheck> logger,
        IConfiguration configuration,
        DatabaseConfigurationValidator validator)
    {
        _logger = logger;
        _configuration = configuration;
        _validator = validator;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var databaseProvider = _configuration.GetValue<string>("Database:Provider") ?? "SQLite";
            var usePostgreSQL = string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase);

            // Only validate SQLite connection strings
            if (usePostgreSQL)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    "PostgreSQL database configuration (validation not applicable)",
                    data: new Dictionary<string, object>
                    {
                        ["provider"] = "PostgreSQL"
                    }));
            }

            // Get SQLite connection string from configuration
            var sqliteFileName = _configuration.GetValue<string>("Database:SQLiteFileName") ?? "aura.db";
            var configuredSqlitePath = _configuration.GetValue<string>("Database:SQLitePath");
            var envSqlitePath = Environment.GetEnvironmentVariable("AURA_DATABASE_PATH");
            
            var defaultUserDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Aura");
            
            var sqlitePath = !string.IsNullOrWhiteSpace(configuredSqlitePath)
                ? configuredSqlitePath
                : !string.IsNullOrWhiteSpace(envSqlitePath)
                    ? envSqlitePath
                    : Path.Combine(defaultUserDataRoot, sqliteFileName);

            var connectionString = $"Data Source={sqlitePath};Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True";

            // Validate the connection string
            if (!_validator.ValidateConnectionString(connectionString, out var errorMessage))
            {
                _logger.LogError("Database configuration validation failed: {Error}", errorMessage);
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Database configuration invalid: {errorMessage}",
                    data: new Dictionary<string, object>
                    {
                        ["provider"] = "SQLite",
                        ["validation_error"] = errorMessage
                    }));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Database configuration valid",
                data: new Dictionary<string, object>
                {
                    ["provider"] = "SQLite",
                    ["database_path"] = sqlitePath
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database configuration health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Database configuration check failed",
                exception: ex));
        }
    }
}
