# Database Migration Guide

## Overview
This guide explains how to create, apply, and manage Entity Framework Core migrations for Aura Video Studio.

## Prerequisites
- .NET 8.0 SDK installed
- Entity Framework Core CLI tools installed

### Install EF Core Tools
```bash
dotnet tool install --global dotnet-ef
# Or update if already installed
dotnet tool update --global dotnet-ef
```

## Migration Workflow

### 1. Create Initial Migration (Already Done)
The initial migration has been created, but if you need to create a new one:

```bash
cd /workspace
dotnet ef migrations add InitialCreate \
    --project Aura.Api \
    --startup-project Aura.Api \
    --context AuraDbContext
```

### 2. View Migration Code
Migrations are located in:
- `Aura.Api/Migrations/` (Application migrations)
- `Aura.Api/Data/Migrations/` (Some migrations may be here)

Check the latest migration file to review the changes.

### 3. Apply Migrations

#### Development Environment
```bash
# Apply all pending migrations
dotnet ef database update \
    --project Aura.Api \
    --startup-project Aura.Api

# Apply to specific migration
dotnet ef database update MigrationName \
    --project Aura.Api \
    --startup-project Aura.Api
```

#### Production Environment
Generate SQL script and apply manually:

```bash
# Generate SQL script for all migrations
dotnet ef migrations script \
    --project Aura.Api \
    --startup-project Aura.Api \
    --output migration.sql

# Generate SQL script from specific migration
dotnet ef migrations script FromMigration ToMigration \
    --project Aura.Api \
    --startup-project Aura.Api \
    --output migration.sql \
    --idempotent
```

The `--idempotent` flag makes the script safe to run multiple times.

### 4. Verify Migration Status
```bash
# List all migrations
dotnet ef migrations list \
    --project Aura.Api \
    --startup-project Aura.Api

# Check which migrations have been applied
dotnet ef database update --list \
    --project Aura.Api \
    --startup-project Aura.Api
```

### 5. Rollback Migration
```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName \
    --project Aura.Api \
    --startup-project Aura.Api

# Remove last migration (if not applied)
dotnet ef migrations remove \
    --project Aura.Api \
    --startup-project Aura.Api
```

## Creating New Migrations

### When to Create a Migration
Create a new migration when you:
- Add new entities
- Modify existing entities (add/remove/change properties)
- Add/remove relationships
- Change indexes or constraints
- Update seed data

### Migration Best Practices

#### 1. Descriptive Names
Use clear, descriptive names that indicate what changed:
```bash
dotnet ef migrations add AddUserProfileTable
dotnet ef migrations add UpdateProjectStateIndexes
dotnet ef migrations add AddContentBlobDeduplication
```

#### 2. Review Before Applying
Always review the generated migration code before applying:
- Check for unintended changes
- Verify data type mappings
- Ensure indexes are correct
- Review cascade delete rules

#### 3. Test Locally First
```bash
# Create test database
dotnet ef database update --connection "Data Source=test.db"

# Verify schema
# Test CRUD operations
# Run integration tests

# Clean up
rm test.db
```

#### 4. Make Migrations Idempotent
For production deployments, always use idempotent scripts:
```bash
dotnet ef migrations script --idempotent --output migration.sql
```

## Common Migration Scenarios

### Adding a New Entity

1. Create the entity class:
```csharp
public class NewEntity : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
```

2. Add DbSet to AuraDbContext:
```csharp
public DbSet<NewEntity> NewEntities { get; set; } = null!;
```

3. Configure in OnModelCreating:
```csharp
modelBuilder.Entity<NewEntity>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.Name);
});
```

4. Create migration:
```bash
dotnet ef migrations add AddNewEntity --project Aura.Api
```

### Modifying Existing Entity

1. Update entity class:
```csharp
public class ExistingEntity
{
    // ... existing properties ...
    
    // Add new property
    public string? NewProperty { get; set; }
}
```

2. Create migration:
```bash
dotnet ef migrations add AddNewPropertyToExistingEntity --project Aura.Api
```

3. Review migration:
   - Check if default value is needed
   - Verify nullable/required constraints

### Adding Relationships

1. Add navigation properties:
```csharp
public class Parent
{
    public Guid Id { get; set; }
    public ICollection<Child> Children { get; set; } = new List<Child>();
}

public class Child
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public Parent Parent { get; set; } = null!;
}
```

2. Configure relationship:
```csharp
modelBuilder.Entity<Child>(entity =>
{
    entity.HasOne(c => c.Parent)
        .WithMany(p => p.Children)
        .HasForeignKey(c => c.ParentId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

3. Create migration:
```bash
dotnet ef migrations add AddParentChildRelationship --project Aura.Api
```

### Adding Indexes

1. Configure in OnModelCreating:
```csharp
modelBuilder.Entity<EntityName>(entity =>
{
    entity.HasIndex(e => e.ColumnName);
    entity.HasIndex(e => new { e.Column1, e.Column2 }); // Composite
    entity.HasIndex(e => e.UniqueColumn).IsUnique();
});
```

2. Create migration:
```bash
dotnet ef migrations add AddIndexesToEntity --project Aura.Api
```

## Seed Data in Migrations

### Option 1: OnModelCreating (Simple)
For static, unchanging seed data:

```csharp
modelBuilder.Entity<SystemConfigurationEntity>(entity =>
{
    entity.HasData(new SystemConfigurationEntity
    {
        Id = 1,
        IsSetupComplete = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });
});
```

### Option 2: SeedData Service (Recommended)
For complex or development-only seed data:

Location: `Aura.Api/Data/SeedData.cs`

Called in `Program.cs`:
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();
    var seeder = new SeedData(context, logger);
    await seeder.SeedAsync();
}
```

### Option 3: Migration Data Operations
For data migrations:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        @"INSERT INTO Templates (Id, Name, Category, TemplateData, CreatedAt, UpdatedAt) 
          VALUES ('guid', 'Template Name', 'Category', '{}', datetime('now'), datetime('now'))");
}
```

## Database Schema Management

### Export Current Schema
```bash
# Generate script for current state
dotnet ef migrations script 0 \
    --project Aura.Api \
    --startup-project Aura.Api \
    --output schema.sql
```

### Compare Schemas
```bash
# List migrations in database
dotnet ef migrations list --project Aura.Api

# Check pending migrations
dotnet ef migrations has-pending-model-changes --project Aura.Api
```

### Reset Database (Development Only)
```bash
# Drop database
dotnet ef database drop --project Aura.Api --force

# Recreate
dotnet ef database update --project Aura.Api
```

## Continuous Integration

### CI/CD Pipeline Steps

1. **Verify Migrations**
```bash
# Check for pending model changes
dotnet ef migrations has-pending-model-changes --project Aura.Api
if [ $? -ne 0 ]; then
  echo "Warning: Model changes detected but no migration created"
fi
```

2. **Generate Migration Script**
```bash
# Generate idempotent script
dotnet ef migrations script \
    --project Aura.Api \
    --startup-project Aura.Api \
    --output migrations.sql \
    --idempotent

# Store as artifact
```

3. **Run Tests**
```bash
# Run all tests including database tests
dotnet test --filter "FullyQualifiedName~Aura.Tests"
```

4. **Deploy to Staging**
```bash
# Apply migrations to staging database
dotnet ef database update --project Aura.Api --connection "$STAGING_CONNECTION_STRING"
```

5. **Deploy to Production**
```bash
# Apply idempotent script to production
# Manual review recommended
```

## Troubleshooting

### Migration Failed to Apply

**Problem**: Migration fails with constraint error

**Solution**:
1. Check foreign key relationships
2. Ensure referenced tables exist
3. Verify data integrity
4. Consider adding migration steps to clean up invalid data

### Cannot Remove Migration

**Problem**: `dotnet ef migrations remove` fails

**Solution**:
```bash
# If migration was applied, rollback first
dotnet ef database update PreviousMigration --project Aura.Api

# Then remove
dotnet ef migrations remove --project Aura.Api
```

### Model Out of Sync

**Problem**: Model doesn't match database

**Solution**:
```bash
# Create migration to sync
dotnet ef migrations add SyncModel --project Aura.Api

# Review changes carefully
# Apply migration
dotnet ef database update --project Aura.Api
```

### Connection String Issues

**Problem**: Cannot connect to database

**Solution**:
1. Check `appsettings.json` for connection string
2. Verify database file location (SQLite)
3. Check permissions on database file/directory
4. Use `--connection` parameter to override:
```bash
dotnet ef database update --connection "Data Source=./aura.db"
```

### SQLite Limitations

**Problem**: SQLite doesn't support certain operations

**Common Limitations**:
- Cannot drop columns (workaround: recreate table)
- Limited ALTER TABLE support
- No rename column (use EF Core convention)

**Solution**:
Use migration operations that SQLite supports:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Instead of DropColumn, recreate table
    migrationBuilder.RenameTable("OldTable", newName: "TempTable");
    // Create new table with desired schema
    // Copy data from TempTable
    // Drop TempTable
}
```

## Best Practices Summary

### ✅ Do
- Use descriptive migration names
- Review migrations before applying
- Test migrations locally first
- Use idempotent scripts for production
- Keep migrations small and focused
- Document complex migrations
- Version control all migrations
- Use transactions when possible

### ❌ Don't
- Modify applied migrations
- Delete migration files
- Skip migrations
- Apply untested migrations to production
- Use non-idempotent scripts in production
- Mix schema and data changes unnecessarily

## Quick Reference

### Common Commands
```bash
# Create migration
dotnet ef migrations add MigrationName --project Aura.Api

# Apply migrations
dotnet ef database update --project Aura.Api

# Generate SQL script
dotnet ef migrations script --idempotent --output migration.sql --project Aura.Api

# List migrations
dotnet ef migrations list --project Aura.Api

# Remove last migration
dotnet ef migrations remove --project Aura.Api

# Drop database
dotnet ef database drop --project Aura.Api --force
```

### Connection Strings

**Development (SQLite)**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=aura.db"
  }
}
```

**Production (PostgreSQL)**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aura;Username=user;Password=pass"
  }
}
```

**Production (SQL Server)**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=aura;User Id=user;Password=pass;TrustServerCertificate=true"
  }
}
```

## Resources

- [EF Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core CLI Reference](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)
- [Database Providers](https://docs.microsoft.com/en-us/ef/core/providers/)
- [SQLite Limitations](https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations)

## Support

For issues or questions:
1. Check existing migrations in `Aura.Api/Migrations/`
2. Review `DATABASE_VERIFICATION_GUIDE.md`
3. Run tests: `dotnet test --filter "FullyQualifiedName~Aura.Tests.Data"`
4. Check logs for EF Core warnings
5. Consult EF Core documentation
