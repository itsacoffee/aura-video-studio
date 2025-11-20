using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Validates database connection strings and configuration to catch common errors early
/// </summary>
public class DatabaseConfigurationValidator
{
    private readonly ILogger<DatabaseConfigurationValidator> _logger;
    
    /// <summary>
    /// List of unsupported keywords that should trigger warnings.
    /// These keywords are not supported by Microsoft.Data.Sqlite connection strings
    /// and should be configured via PRAGMA statements after connection.
    /// </summary>
    private static readonly string[] UnsupportedKeywords = 
    {
        "journal mode",
        "journal_mode"
    };
    
    public DatabaseConfigurationValidator(ILogger<DatabaseConfigurationValidator> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Validates a SQLite connection string for common configuration errors
    /// </summary>
    /// <param name="connectionString">The connection string to validate</param>
    /// <param name="errorMessage">Output parameter containing error details if validation fails</param>
    /// <returns>True if the connection string is valid, false otherwise</returns>
    public bool ValidateConnectionString(string connectionString, out string errorMessage)
    {
        errorMessage = string.Empty;
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errorMessage = "Connection string cannot be null or empty";
            _logger.LogError(errorMessage);
            return false;
        }
        
        try
        {
            // Check for unsupported keywords before attempting connection
            var lowerConnectionString = connectionString.ToLowerInvariant();
            foreach (var keyword in UnsupportedKeywords)
            {
                if (lowerConnectionString.Contains(keyword))
                {
                    errorMessage = $"Connection string contains unsupported keyword '{keyword}'. " +
                                   "Use PRAGMA statements after connection instead. " +
                                   "Example: await connection.ExecuteSqlRawAsync(\"PRAGMA journal_mode=WAL;\");";
                    _logger.LogError(errorMessage);
                    return false;
                }
            }
            
            // Try to build connection to validate format
            var builder = new SqliteConnectionStringBuilder(connectionString);
            
            // Validate that a data source is specified
            if (string.IsNullOrWhiteSpace(builder.DataSource))
            {
                errorMessage = "Connection string must specify a Data Source";
                _logger.LogError(errorMessage);
                return false;
            }
            
            _logger.LogInformation("Connection string validation passed for data source: {DataSource}", 
                builder.DataSource);
            return true;
        }
        catch (ArgumentException ex)
        {
            errorMessage = $"Invalid connection string format: {ex.Message}";
            _logger.LogError(ex, "Connection string validation failed");
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error validating connection string: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during connection string validation");
            return false;
        }
    }
}
