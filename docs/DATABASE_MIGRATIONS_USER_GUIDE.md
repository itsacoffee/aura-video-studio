# Database Migration CLI Commands - User Guide

This guide explains how to use the Aura CLI database management commands to manage your database schema.

## Overview

Aura Video Studio uses Entity Framework Core migrations to manage database schema changes. The CLI provides three commands to help you manage these migrations:

- **`migrate`** - Apply pending database migrations
- **`status`** - Display the current migration status
- **`reset`** - Drop and recreate the database (⚠️ WARNING: deletes all data)

## Prerequisites

- .NET 8 SDK installed
- Aura CLI built (`dotnet build Aura.Cli/Aura.Cli.csproj`)
- Database configured (default: SQLite in `~/.local/share/Aura/aura.db`)

## Commands

### `aura-cli migrate`

Applies all pending database migrations to bring your database schema up to date.

#### Usage

```bash
aura-cli migrate [options]
```

#### Options

- `-h, --help` - Show help message
- `-v, --verbose` - Enable verbose output with detailed error messages
- `--dry-run` - Check for pending migrations without applying them

#### Examples

Apply all pending migrations:
```bash
aura-cli migrate
```

Check what migrations would be applied without actually applying them:
```bash
aura-cli migrate --dry-run
```

Apply migrations with verbose output:
```bash
aura-cli migrate -v
```

#### Output

When there are pending migrations:
```
╔══════════════════════════════════════════════════════════╗
║         Database Migration - Apply Pending Changes      ║
╚══════════════════════════════════════════════════════════╝

Checking for pending migrations...
Found 3 pending migration(s):

  • 20251109170431_AddSystemConfiguration
  • 20251110000000_AddJobQueueSupport
  • 20251110120000_AddLocalAnalytics

Applying migrations...

✓ Successfully applied 3 migration(s)
```

When database is up to date:
```
╔══════════════════════════════════════════════════════════╗
║         Database Migration - Apply Pending Changes      ║
╚══════════════════════════════════════════════════════════╝

Checking for pending migrations...
✓ Database is up to date - no pending migrations
```

---

### `aura-cli status`

Displays the current status of database migrations, including which migrations have been applied and which are pending.

#### Usage

```bash
aura-cli status [options]
```

#### Options

- `-h, --help` - Show help message
- `-v, --verbose` - Show all applied migrations (default shows last 5)

#### Examples

Check current migration status:
```bash
aura-cli status
```

Show all applied migrations:
```bash
aura-cli status -v
```

#### Output

Standard output:
```
╔══════════════════════════════════════════════════════════╗
║           Database Migration Status Report              ║
╚══════════════════════════════════════════════════════════╝

✓ Database connection successful

Database Status:
  Total Migrations: 7
  Applied: 7
  Pending: 0

Applied Migrations:
  ✓ 20251102050640_AddProjectStatePersistence
  ✓ 20251103202216_AddActionLogAndSoftDelete
  ✓ 20251108184353_AddWizardProjectManagement
  ✓ 20251109170431_AddSystemConfiguration
  ✓ 20251121045900_AddCreatedByToConfigurations
  ... and 2 more (use -v to see all)

✓ Database is up to date - no pending migrations
```

When there are pending migrations:
```
Pending Migrations:
  ⚠ 20251110000000_AddJobQueueSupport
  ⚠ 20251110120000_AddLocalAnalytics

Run 'aura-cli migrate' to apply pending migrations.
```

---

### `aura-cli reset`

Drops the existing database and recreates it by applying all migrations from scratch.

⚠️ **WARNING**: This command will permanently delete all data in the database!

#### Usage

```bash
aura-cli reset [options]
```

#### Options

- `-h, --help` - Show help message
- `-v, --verbose` - Enable verbose output with detailed error messages
- `-f, --force` - Skip confirmation prompt
- `--dry-run` - Show what would be done without actually doing it

#### Examples

Reset database with confirmation prompt:
```bash
aura-cli reset
```

Reset without confirmation (useful for scripts):
```bash
aura-cli reset --force
```

See what would be done without actually resetting:
```bash
aura-cli reset --dry-run --force
```

#### Output

With confirmation:
```
╔══════════════════════════════════════════════════════════╗
║         Database Reset - Drop and Recreate              ║
╚══════════════════════════════════════════════════════════╝

⚠ WARNING: This will delete ALL data in the database!

Are you sure you want to continue? Type 'yes' to confirm: yes

Dropping existing database...
✓ Database dropped successfully

Creating database with all migrations...

✓ Database created successfully with 7 migration(s)
```

With --force flag:
```
╔══════════════════════════════════════════════════════════╗
║         Database Reset - Drop and Recreate              ║
╚══════════════════════════════════════════════════════════╝

⚠ WARNING: This will delete ALL data in the database!

Dropping existing database...
✓ Database dropped successfully

Creating database with all migrations...

✓ Database created successfully with 7 migration(s)
```

---

## Common Workflows

### Initial Setup

When setting up Aura for the first time:

```bash
# Check if database exists and its status
aura-cli status

# If database doesn't exist or has pending migrations
aura-cli migrate
```

### After Updating Aura

After pulling new code or updating Aura:

```bash
# Check for new migrations
aura-cli status

# Apply any pending migrations
aura-cli migrate
```

### Development/Testing

When you need to start fresh (⚠️ deletes all data):

```bash
# Reset database to initial state
aura-cli reset --force

# Verify it was reset
aura-cli status
```

### Troubleshooting

If you encounter migration errors:

```bash
# Check current status
aura-cli status -v

# Try migrating with verbose output
aura-cli migrate -v

# If migrations are corrupted, reset and start fresh
aura-cli reset --force
```

---

## Error Handling

All commands handle errors gracefully and provide clear error messages:

- **Cannot connect to database** - Check database path and permissions
- **Migration failed** - Review error message, check migration files, try verbose mode
- **Assembly not found** - Ensure Aura.Api.dll is in the CLI output directory

Exit codes:
- **0** - Success
- **1** - Error occurred

---

## Database Location

By default, the database is stored at:
- **Linux/macOS**: `~/.local/share/Aura/aura.db`
- **Windows**: `%LOCALAPPDATA%\Aura\aura.db`

You can verify the location by checking the CLI output or looking at the connection string in the logs.

---

## Automatic Migrations on API Startup

In addition to the CLI commands, the Aura API automatically checks for and applies pending migrations on startup. This means:

1. When you start the API server, it will check for pending migrations
2. If found, it will apply them automatically
3. The server will log the migration status
4. If migration fails, the server will start anyway but log an error

This ensures your database is always up to date when running the API.

---

## Best Practices

1. **Always check status before migrating** - Use `status` to see what will be applied
2. **Use dry-run for safety** - Test with `--dry-run` before actual migration
3. **Backup before reset** - Always backup your database before using `reset`
4. **Check logs** - Review verbose output if something goes wrong
5. **Version control** - Keep migration files in version control
6. **Test migrations** - Test on development database before production

---

## Getting Help

For command-specific help:
```bash
aura-cli migrate --help
aura-cli status --help
aura-cli reset --help
```

For general CLI help:
```bash
aura-cli --help
```

---

## Related Documentation

- [Developer Guide - Adding Migrations](DATABASE_MIGRATIONS_DEVELOPER_GUIDE.md)
- [Architecture Documentation](../docs/architecture.md)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
