# Database Migration Guide

## Overview

This guide covers how to create, apply, and manage database migrations for the Aura Video Studio application using Entity Framework Core.

## Prerequisites

- .NET 8.0 SDK installed
- EF Core CLI tools installed: `dotnet tool install --global dotnet-ef`
- Access to the project repository

## Migration Commands

### Apply Migrations

Run all pending migrations to update the database to the latest version:

```bash
./Scripts/run-migrations.sh migrate
```

Or using dotnet CLI directly:

```bash
cd /workspace
dotnet ef database update \
  --project Aura.Api \
  --startup-project Aura.Api \
  --context AuraDbContext
```

### Create a New Migration

When you make changes to entity models, create a new migration:

```bash
./Scripts/run-migrations.sh add <MigrationName>
```

Example:

```bash
./Scripts/run-migrations.sh add AddUserEmailColumn
```

Or using dotnet CLI:

```bash
dotnet ef migrations add AddUserEmailColumn \
  --project Aura.Api \
  --startup-project Aura.Api \
  --context AuraDbContext \
  --output-dir Data/Migrations
```

### Remove Last Migration

If you made a mistake in the last migration (before applying it):

```bash
./Scripts/run-migrations.sh remove
```

**Warning**: Only works if the migration hasn't been applied to the database yet.

### Rollback to a Specific Migration

Revert the database to a previous migration:

```bash
./Scripts/run-migrations.sh rollback <MigrationName>
```

Example:

```bash
./Scripts/run-migrations.sh rollback InitialCreate
```

To rollback all migrations:

```bash
./Scripts/run-migrations.sh rollback 0
```

### List All Migrations

Show all migrations and their status:

```bash
./Scripts/run-migrations.sh list
```

### Generate SQL Script

Generate an idempotent SQL script for all migrations:

```bash
./Scripts/run-migrations.sh script migration.sql
```

This is useful for:
- Reviewing changes before applying
- Manual database updates
- CI/CD pipelines

## Migration Workflow

### 1. Development Workflow

```bash
# 1. Make changes to entity models
# Edit files in Aura.Core/Data/

# 2. Create migration
./Scripts/run-migrations.sh add MyNewFeature

# 3. Review generated migration
# Check Aura.Api/Data/Migrations/

# 4. Test migration locally
./Scripts/run-migrations.sh migrate

# 5. Commit migration files
git add Aura.Api/Data/Migrations/
git commit -m "Add migration: MyNewFeature"
```

### 2. Production Deployment Workflow

```bash
# 1. Backup database BEFORE applying migrations
./Scripts/backup-database.sh /path/to/aura.db ./backups

# 2. Review migration SQL script
./Scripts/run-migrations.sh script review.sql
cat review.sql

# 3. Apply migrations
./Scripts/run-migrations.sh migrate

# 4. Verify migration success
./Scripts/run-migrations.sh list

# 5. Test application functionality

# 6. If issues occur, rollback
./Scripts/restore-database.sh ./backups/aura_backup_YYYYMMDD_HHMMSS.db
```

### 3. Staging Environment Workflow

```bash
# 1. Deploy to staging
git pull origin main

# 2. Run migrations in staging
./Scripts/run-migrations.sh migrate

# 3. Run integration tests
dotnet test

# 4. Validate data integrity
# Check key tables and queries

# 5. If successful, proceed to production
```

## Common Scenarios

### Adding a New Column

```csharp
// In entity class
public class ProjectStateEntity
{
    // Existing properties...
    
    [MaxLength(100)]
    public string? Priority { get; set; } // New column
}
```

```bash
# Create migration
./Scripts/run-migrations.sh add AddProjectPriority

# Apply migration
./Scripts/run-migrations.sh migrate
```

### Renaming a Column

```csharp
// In migration Up method
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "OldName",
        table: "ProjectStates",
        newName: "NewName");
}

// In migration Down method
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "NewName",
        table: "ProjectStates",
        newName: "OldName");
}
```

### Adding an Index

```csharp
// In AuraDbContext.OnModelCreating
modelBuilder.Entity<ProjectStateEntity>(entity =>
{
    entity.HasIndex(e => e.Priority);
});
```

```bash
# Create and apply migration
./Scripts/run-migrations.sh add AddPriorityIndex
./Scripts/run-migrations.sh migrate
```

### Data Migration

When you need to transform existing data:

```csharp
// In migration Up method
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add new column
    migrationBuilder.AddColumn<string>(
        name: "FullName",
        table: "Users",
        nullable: true);
    
    // Migrate data
    migrationBuilder.Sql(@"
        UPDATE Users 
        SET FullName = FirstName || ' ' || LastName
        WHERE FirstName IS NOT NULL AND LastName IS NOT NULL
    ");
    
    // Make column required if needed
    migrationBuilder.AlterColumn<string>(
        name: "FullName",
        table: "Users",
        nullable: false);
}
```

### Seeding Data

```csharp
// In AuraDbContext.OnModelCreating
modelBuilder.Entity<TemplateEntity>().HasData(
    new TemplateEntity
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Default Template",
        Category = "General",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    }
);
```

## Best Practices

### 1. Migration Naming

Use descriptive names that indicate what changed:

✅ **Good**:
- `AddUserEmailColumn`
- `CreateProjectIndexes`
- `UpdateTemplateSchema`
- `SeedInitialTemplates`

❌ **Bad**:
- `Migration1`
- `UpdateStuff`
- `Fix`

### 2. Migration Content

- Keep migrations small and focused
- One logical change per migration
- Include both `Up` and `Down` methods
- Test rollback functionality

### 3. Never Modify Applied Migrations

Once a migration is applied to any environment (dev, staging, prod), **never modify it**. Instead, create a new migration to make additional changes.

### 4. Review Generated Migrations

Always review auto-generated migrations before applying:
- Check for data loss warnings
- Verify nullable constraints
- Ensure indexes are created
- Review default values

### 5. Test Migrations

Before production deployment:
1. Test migration up
2. Test migration down (rollback)
3. Verify data integrity
4. Test application functionality

## Troubleshooting

### Migration Fails with "Table already exists"

**Cause**: Migration was partially applied or database is out of sync.

**Solution**:

```bash
# Option 1: Reset database (development only)
rm aura.db
./Scripts/run-migrations.sh migrate

# Option 2: Manually fix and sync
# Edit migration to skip existing table
# Or manually drop conflicting table
```

### Cannot Rollback Migration

**Cause**: `Down` method not implemented or data loss would occur.

**Solution**:

```bash
# Restore from backup
./Scripts/restore-database.sh ./backups/latest_backup.db

# Or manually implement Down method
```

### "No migrations configuration type was found"

**Cause**: EF Core tools can't find the DbContext.

**Solution**:

```bash
# Ensure you're in the project root
cd /workspace

# Specify all parameters explicitly
dotnet ef database update \
  --project Aura.Api \
  --startup-project Aura.Api \
  --context AuraDbContext
```

### Migrations Work Locally But Fail in Production

**Cause**: Different database state or missing permissions.

**Solution**:

```bash
# 1. Generate SQL script
./Scripts/run-migrations.sh script production.sql

# 2. Review script for issues
cat production.sql

# 3. Run script manually or with proper permissions
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Run Migrations

on:
  push:
    branches: [ main ]

jobs:
  migrate:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Run migrations
      run: |
        dotnet ef database update \
          --project Aura.Api \
          --startup-project Aura.Api \
          --context AuraDbContext
    
    - name: Run tests
      run: dotnet test
```

### Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY . .

# Run migrations on container startup
ENTRYPOINT ["sh", "-c", "dotnet ef database update && dotnet Aura.Api.dll"]
```

## Schema Versioning

Track schema version in application:

```csharp
public class DatabaseVersionService
{
    private readonly AuraDbContext _context;
    
    public async Task<string> GetCurrentVersionAsync()
    {
        var lastMigration = await _context.Database
            .GetAppliedMigrationsAsync()
            .LastOrDefaultAsync();
        
        return lastMigration ?? "No migrations applied";
    }
}
```

## Migration Checklist

Before creating a migration:
- [ ] Entity changes are complete and tested
- [ ] Relationships are properly configured
- [ ] Indexes are defined for query performance
- [ ] Validation attributes are in place

Before applying a migration:
- [ ] Database backup created
- [ ] Migration reviewed for correctness
- [ ] Down method tested (rollback works)
- [ ] SQL script generated and reviewed

After applying a migration:
- [ ] Migration status verified (`list` command)
- [ ] Application functionality tested
- [ ] Data integrity checked
- [ ] Performance validated

## Additional Resources

- [EF Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Entity Framework Core Tools Reference](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [Database Schema Documentation](./DATABASE_SCHEMA.md)

## Support

For migration issues:
1. Check this guide
2. Review database logs
3. Consult the team
4. Create an issue with detailed error messages
