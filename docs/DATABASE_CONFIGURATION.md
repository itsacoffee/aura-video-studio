# Database Configuration Guide

## Overview

Aura Video Studio uses SQLite with Microsoft.Data.Sqlite provider. This guide explains how to properly configure database connection strings and avoid common configuration errors.

## SQLite Connection String Format

The connection string must use only parameters supported by Microsoft.Data.Sqlite. Using unsupported parameters will cause startup failures.

### Supported Parameters

The following connection string parameters are supported:

| Parameter | Aliases | Description | Valid Values | Example |
|-----------|---------|-------------|--------------|---------|
| `Data Source` | `Filename` | Path to database file | File path or `:memory:` | `Data Source=C:\Users\YourUser\AppData\Local\Aura\aura.db` |
| `Mode` | | Database access mode | `ReadWriteCreate`, `ReadWrite`, `ReadOnly`, `Memory` | `Mode=ReadWriteCreate` |
| `Cache` | | Connection cache mode | `Shared`, `Private` | `Cache=Shared` |
| `Foreign Keys` | | Enable foreign key constraints | `True`, `False` | `Foreign Keys=True` |
| `Recursive Triggers` | | Enable recursive triggers | `True`, `False` | `Recursive Triggers=True` |
| `Password` | | Database encryption password | String | `Password=MySecretKey` |

### Example Connection Strings

#### Basic Connection
```
Data Source=C:\Users\YourUser\AppData\Local\Aura\aura.db;Mode=ReadWriteCreate
```

#### With Foreign Keys and Shared Cache
```
Data Source=C:\Users\YourUser\AppData\Local\Aura\aura.db;Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True
```

#### In-Memory Database
```
Data Source=:memory:;Mode=Memory
```

#### Unix/Linux Path
```
Data Source=/home/user/.local/share/aura/aura.db;Mode=ReadWriteCreate;Foreign Keys=True
```

## WAL Mode Configuration

⚠️ **CRITICAL: Do NOT include `Journal Mode=WAL` in the connection string.**

The `Journal Mode` parameter is **not supported** by Microsoft.Data.Sqlite and will cause the application to fail at startup.

### Incorrect Configuration (Will Fail)

```csharp
// ❌ WRONG - This will cause startup failure
var connectionString = "Data Source=aura.db;Journal Mode=WAL;Mode=ReadWriteCreate";
```

### Correct Configuration

Instead, set WAL (Write-Ahead Logging) mode programmatically after connection using PRAGMA commands:

```csharp
// ✅ CORRECT - Configure WAL via PRAGMA after connection
using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

using var command = connection.CreateCommand();
command.CommandText = "PRAGMA journal_mode=WAL;";
await command.ExecuteNonQueryAsync();
```

Or with Entity Framework Core:

```csharp
// ✅ CORRECT - Configure WAL via ExecuteSqlRaw
await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
```

### Benefits of WAL Mode

WAL (Write-Ahead Logging) mode provides:
- **Better concurrency**: Readers don't block writers
- **Improved performance**: Faster write operations
- **Atomic commits**: Better crash recovery

Aura automatically configures WAL mode during database initialization via `DatabaseInitializationService`.

## Common Configuration Errors

### Error: "Connection string keyword 'journal mode' is not supported"

**Cause**: Connection string contains `Journal Mode=WAL` or similar parameter.

**Solution**: Remove the journal mode parameter from the connection string. WAL mode is configured programmatically via PRAGMA commands after the connection is established.

**Example Fix**:
```csharp
// Before (incorrect):
var connectionString = "Data Source=aura.db;Journal Mode=WAL;Mode=ReadWriteCreate";

// After (correct):
var connectionString = "Data Source=aura.db;Mode=ReadWriteCreate;Foreign Keys=True";
// WAL mode is set by DatabaseInitializationService after connection
```

### Error: "Connection string keyword 'cache size' is not supported"

**Cause**: Connection string contains `Cache Size=-64000` or similar parameter.

**Solution**: Remove the cache size parameter from the connection string. Cache size should be configured via PRAGMA commands if needed.

**Example Fix**:
```csharp
// Before (incorrect):
var connectionString = "Data Source=aura.db;Cache Size=-64000";

// After (correct):
var connectionString = "Data Source=aura.db;Mode=ReadWriteCreate";

// Optional: Configure cache size via PRAGMA if needed
await context.Database.ExecuteSqlRawAsync("PRAGMA cache_size=-64000;");
```

### Error: "Connection string must specify a Data Source"

**Cause**: Connection string is missing the required `Data Source` or `Filename` parameter.

**Solution**: Add a valid data source to the connection string.

**Example Fix**:
```csharp
// Before (incorrect):
var connectionString = "Mode=ReadWriteCreate;Foreign Keys=True";

// After (correct):
var connectionString = "Data Source=aura.db;Mode=ReadWriteCreate;Foreign Keys=True";
```

## Database Performance Configuration

### Recommended PRAGMA Settings

For optimal performance with Aura, the following PRAGMA settings are automatically applied during initialization:

```sql
PRAGMA journal_mode=WAL;        -- Write-Ahead Logging for better concurrency
PRAGMA synchronous=NORMAL;      -- Balanced durability and performance
PRAGMA temp_store=MEMORY;       -- Store temporary tables in memory
PRAGMA locking_mode=NORMAL;     -- Allow multiple connections
```

### Additional Performance Options

For specific use cases, you may want to configure additional PRAGMA settings:

```sql
-- For large databases
PRAGMA cache_size=-64000;       -- 64MB cache (negative value = KB)

-- For heavy write workloads
PRAGMA wal_autocheckpoint=1000; -- Checkpoint every 1000 pages

-- For read-heavy workloads
PRAGMA mmap_size=268435456;     -- 256MB memory-mapped I/O
```

Apply these after the connection is established:

```csharp
await context.Database.ExecuteSqlRawAsync("PRAGMA cache_size=-64000;");
```

## Configuration Validation

Aura includes built-in connection string validation via `DatabaseConfigurationValidator`:

- Validates connection string format
- Detects unsupported keywords
- Provides clear error messages with solutions
- Runs automatically at application startup

If your connection string contains errors, the application will fail fast at startup with a descriptive error message, preventing runtime failures.

## Troubleshooting

### Database Locked Errors

If you encounter "database is locked" errors:

1. Ensure WAL mode is enabled (default in Aura)
2. Check for long-running transactions
3. Verify no other processes are accessing the database
4. Consider increasing busy timeout:
   ```sql
   PRAGMA busy_timeout=5000;  -- 5 seconds
   ```

### Slow Database Performance

1. Verify WAL mode is active:
   ```sql
   PRAGMA journal_mode;  -- Should return "wal"
   ```

2. Check cache size:
   ```sql
   PRAGMA cache_size;
   ```

3. Ensure indexes are present on frequently queried columns

4. Review query patterns for N+1 query issues

### Database Corruption

If you suspect database corruption:

1. Run integrity check:
   ```sql
   PRAGMA integrity_check;
   ```

2. If issues are found, restore from backup

3. Consider using `vacuum` to rebuild database:
   ```sql
   VACUUM;
   ```

## Environment Variables

You can override the database path using environment variables:

```bash
# Set custom database path
export AURA_DATABASE_PATH="/custom/path/to/aura.db"

# Or on Windows
set AURA_DATABASE_PATH=C:\custom\path\to\aura.db
```

## Configuration Files

### appsettings.json

```json
{
  "Database": {
    "Provider": "SQLite",
    "SQLitePath": "",
    "ConnectionString": "",
    "Performance": {
      "MaxPoolSize": 100,
      "MinPoolSize": 10,
      "SqliteEnableWAL": true
    }
  }
}
```

### Best Practices

1. **Never include unsupported parameters** in connection strings
2. **Always use `Foreign Keys=True`** to maintain referential integrity
3. **Enable WAL mode** for production deployments (done automatically)
4. **Use absolute paths** for database files
5. **Keep backups** before running migrations
6. **Test connection strings** in development before deploying

## References

- [Microsoft.Data.Sqlite Documentation](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [SQLite PRAGMA Statements](https://www.sqlite.org/pragma.html)
- [SQLite WAL Mode](https://www.sqlite.org/wal.html)
- [Entity Framework Core with SQLite](https://docs.microsoft.com/en-us/ef/core/providers/sqlite/)

## Support

For additional help:

1. Check [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) for common issues
2. Review [Database Documentation](./database/README.md) for schema details
3. Open an issue on GitHub with connection string details (redact sensitive paths)

---

**Last Updated**: 2025-11-20  
**Version**: 1.0.0
