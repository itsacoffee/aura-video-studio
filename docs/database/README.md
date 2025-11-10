# Aura Database Documentation

## Overview

This directory contains comprehensive documentation for the Aura Video Studio database schema, migrations, and data management.

## Documentation Index

### Core Documentation

1. **[DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md)**
   - Complete schema documentation
   - Table definitions and columns
   - Relationships and constraints
   - Indexes and performance optimization
   - Audit trails and soft delete
   - Query examples and best practices

2. **[MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md)**
   - Migration creation and application
   - Common migration scenarios
   - Rollback procedures
   - Best practices
   - Troubleshooting guide
   - CI/CD integration

3. **[ER_DIAGRAM.md](./ER_DIAGRAM.md)**
   - Visual entity relationship diagrams
   - Data flow examples
   - Storage estimates
   - Index strategy
   - Design patterns

4. **[MIGRATION_VERIFICATION.md](./MIGRATION_VERIFICATION.md)**
   - Verification procedures
   - Manual testing steps
   - Automated test examples
   - Performance validation
   - Troubleshooting guide

## Quick Start

### First Time Setup

```bash
# 1. Ensure .NET SDK is installed
dotnet --version  # Should be 8.0 or higher

# 2. Navigate to project root
cd /workspace

# 3. Run migrations
./Scripts/run-migrations.sh migrate

# 4. Verify migration success
./Scripts/run-migrations.sh list
```

### Daily Development

```bash
# Make changes to entity models in Aura.Core/Data/

# Create migration
./Scripts/run-migrations.sh add MyFeature

# Review migration files
ls Aura.Api/Data/Migrations/

# Apply migration
./Scripts/run-migrations.sh migrate

# Verify
./Scripts/run-migrations.sh list
```

## Database Technology Stack

- **Database**: SQLite 3.x
- **ORM**: Entity Framework Core 8.0
- **Migration Tool**: EF Core Migrations
- **Language**: C# (.NET 8.0)
- **Platform**: Cross-platform (Windows, Linux, macOS)

## Key Features

### 1. Audit Trails

All entities automatically track:
- Creation timestamp (`CreatedAt`)
- Last update timestamp (`UpdatedAt`)
- Created by user (`CreatedBy`)
- Modified by user (`ModifiedBy`)

### 2. Soft Delete

Entities support soft delete via `ISoftDeletable` interface:
- Records are marked as deleted, not removed
- Global query filters exclude soft-deleted records
- Can be recovered if needed
- Supports GDPR compliance

### 3. Version Control

Projects have full version history:
- Manual saves
- Autosaves
- Restore points
- Content deduplication via hashing

### 4. Performance Optimization

- Strategic indexes on frequently queried columns
- Composite indexes for complex queries
- Foreign key indexes for joins
- Query performance monitoring

## Database Schema Summary

### Core Tables

| Table | Purpose | Records (typical) |
|-------|---------|-------------------|
| `ProjectStates` | Video projects | 10-1000 |
| `SceneStates` | Project scenes | 50-10000 |
| `AssetStates` | Media files | 100-20000 |
| `RenderCheckpoints` | Recovery points | 10-100 |
| `ProjectVersions` | Version history | 50-500 |
| `ContentBlobs` | Deduplicated content | 100-5000 |

### Configuration Tables

| Table | Purpose | Records (typical) |
|-------|---------|-------------------|
| `SystemConfiguration` | Global config | 1 |
| `UserSetups` | User setup status | 1-100 |
| `Configurations` | Key-value config | 10-100 |

### Template & History Tables

| Table | Purpose | Records (typical) |
|-------|---------|-------------------|
| `Templates` | System templates | 10-50 |
| `CustomTemplates` | User templates | 5-100 |
| `ExportHistory` | Export jobs | 100-10000 |
| `ActionLogs` | Undo/redo log | 1000-50000 |

## Backup and Recovery

### Automated Backups

```bash
# Create backup
./Scripts/backup-database.sh /path/to/aura.db ./backups

# List backups
ls -lh backups/

# Restore backup
./Scripts/restore-database.sh backups/aura_backup_20251110_120000.db
```

### Backup Strategy

- **Development**: Daily automated backups
- **Staging**: Hourly automated backups
- **Production**: Real-time replication + hourly backups

## Migration History

| Version | Migration | Date | Description |
|---------|-----------|------|-------------|
| 1.0.0 | InitialCreate | 2025-11-10 | Initial schema creation |

## Common Tasks

### View Database Contents

```bash
# Using SQLite CLI
sqlite3 /path/to/aura.db

# List tables
.tables

# Query projects
SELECT Id, Title, Status, UpdatedAt FROM ProjectStates LIMIT 10;

# Check migration history
SELECT * FROM __EFMigrationsHistory;
```

### Generate SQL Script

```bash
# Generate idempotent script for all migrations
./Scripts/run-migrations.sh script all-migrations.sql

# Review script
cat all-migrations.sql
```

### Performance Analysis

```bash
# Enable query logging in appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}

# Analyze slow queries
# Review application logs for SQL queries
```

## Repository Pattern

The database uses the Repository and Unit of Work patterns:

```csharp
// Using repository pattern
using var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

// Get data
var project = await unitOfWork.ProjectStates.GetByIdAsync(projectId);

// Update
project.Status = "Completed";
await unitOfWork.ProjectStates.UpdateAsync(project);

// Save changes
await unitOfWork.SaveChangesAsync();
```

## Entity Framework Tips

### Include Related Data

```csharp
var project = await context.ProjectStates
    .Include(p => p.Scenes)
    .Include(p => p.Assets)
    .FirstOrDefaultAsync(p => p.Id == projectId);
```

### Projections for Performance

```csharp
var projectSummaries = await context.ProjectStates
    .Select(p => new { p.Id, p.Title, p.UpdatedAt })
    .ToListAsync();
```

### Pagination

```csharp
var page = await context.ProjectStates
    .OrderByDescending(p => p.UpdatedAt)
    .Skip(pageSize * pageNumber)
    .Take(pageSize)
    .ToListAsync();
```

## Security Best Practices

1. **Connection Strings**: Store in `appsettings.json` or environment variables
2. **Sensitive Data**: Encrypt configurations with `IsSensitive = true`
3. **SQL Injection**: Always use parameterized queries (EF Core does this automatically)
4. **Permissions**: Restrict database file access to application user only
5. **Backups**: Encrypt backup files for production data

## Troubleshooting

### Database Locked

```bash
# Close all connections
# Stop application
# Check for lingering processes
lsof /path/to/aura.db
```

### Migrations Out of Sync

```bash
# Check current state
./Scripts/run-migrations.sh list

# Rollback to known good state
./Scripts/run-migrations.sh rollback InitialCreate

# Re-apply migrations
./Scripts/run-migrations.sh migrate
```

### Performance Issues

1. Check for missing indexes
2. Review query patterns
3. Enable SQL logging
4. Analyze execution plans
5. Consider pagination for large datasets

## Support and Resources

### Internal Resources

- [Schema Documentation](./DATABASE_SCHEMA.md)
- [Migration Guide](./MIGRATION_GUIDE.md)
- [ER Diagram](./ER_DIAGRAM.md)
- [Verification Guide](./MIGRATION_VERIFICATION.md)

### External Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [EF Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [.NET Data Access](https://docs.microsoft.com/en-us/dotnet/standard/data/)

## Contributing

When making database changes:

1. Update entity models
2. Create migration
3. Update documentation
4. Add tests
5. Submit PR with migration files

## License

See main project LICENSE file.

---

**Last Updated**: 2025-11-10  
**Database Version**: 1.0.0  
**Documentation Version**: 1.0.0
