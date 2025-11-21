# Database Migrations - Developer Guide

This guide explains how to create and manage Entity Framework Core migrations in Aura Video Studio.

## Overview

Aura uses Entity Framework Core Code-First migrations to manage database schema changes. Migrations are stored in the `Aura.Api/Migrations/` directory and are automatically applied on API startup or manually via CLI commands.

## Prerequisites

- .NET 8 SDK
- Entity Framework Core CLI tools
- Understanding of Entity Framework Core concepts
- Aura solution built and running

## Migration Files Location

All migrations are stored in:
```
Aura.Api/Migrations/
```

The migrations assembly is configured as `Aura.Api` in the DbContext setup.

## Creating a New Migration

### Step 1: Modify Entity Classes

First, make your changes to the entity classes in `Aura.Core/Data/`:

```csharp
// Aura.Core/Data/Entities/MyNewEntity.cs
public class MyNewEntity : IAuditableEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Step 2: Add DbSet to AuraDbContext

Update `Aura.Core/Data/AuraDbContext.cs`:

```csharp
public class AuraDbContext : DbContext
{
    // ... existing DbSets ...
    
    /// <summary>
    /// My new entity collection
    /// </summary>
    public DbSet<MyNewEntity> MyNewEntities { get; set; } = null!;
    
    // ... rest of the class ...
}
```

### Step 3: Configure Entity (Optional)

If needed, add entity configuration in the `OnModelCreating` method:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // ... existing configurations ...
    
    // Configure MyNewEntity
    modelBuilder.Entity<MyNewEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Name);
        entity.HasIndex(e => e.CreatedAt);
        
        // Add seed data if needed
        entity.HasData(new MyNewEntity
        {
            Id = "default-1",
            Name = "Default Item",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    });
}
```

### Step 4: Create the Migration

From the solution root directory, run:

```bash
dotnet ef migrations add YourMigrationName \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext
```

#### Naming Convention

Migration names should:
- Use PascalCase
- Be descriptive of the change
- Start with a verb (Add, Update, Remove, etc.)

Examples:
- `AddUserPreferencesTable`
- `AddIndexToProjectStates`
- `UpdateJobQueueConstraints`
- `RemoveDeprecatedFields`

### Step 5: Review the Generated Migration

The EF Core tools will generate two files in `Aura.Api/Migrations/`:

1. **`{timestamp}_{MigrationName}.cs`** - Contains Up() and Down() methods
2. **`{timestamp}_{MigrationName}.Designer.cs`** - Contains model snapshot

Example migration file:
```csharp
public partial class AddUserPreferencesTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserPreferences",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                UserId = table.Column<string>(nullable: false),
                Theme = table.Column<string>(nullable: true),
                Language = table.Column<string>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPreferences", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserPreferences_UserId",
            table: "UserPreferences",
            column: "UserId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserPreferences");
    }
}
```

### Step 6: Customize Migration (If Needed)

You can manually edit the generated migration to:
- Add custom SQL
- Insert seed data
- Create indexes
- Add data transformations

Example of adding custom SQL:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Generated code
    migrationBuilder.CreateTable(...);
    
    // Custom SQL
    migrationBuilder.Sql(@"
        CREATE TRIGGER UpdateTimestamp
        AFTER UPDATE ON UserPreferences
        FOR EACH ROW
        BEGIN
            UPDATE UserPreferences 
            SET UpdatedAt = CURRENT_TIMESTAMP 
            WHERE Id = NEW.Id;
        END;
    ");
}
```

### Step 7: Test the Migration

#### Option 1: Using CLI

```bash
# Apply migration to development database
aura-cli migrate

# Check status
aura-cli status -v
```

#### Option 2: Using API

Start the API server - it will automatically apply the migration on startup.

#### Option 3: Using EF Core CLI

```bash
# Apply migration directly
dotnet ef database update \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext
```

### Step 8: Verify Migration

Check that:
1. Migration applied successfully (check logs)
2. Database schema is correct (inspect with DB browser)
3. Seed data was inserted (if applicable)
4. Indexes were created
5. Application runs without errors

### Step 9: Test Rollback (Optional)

If your migration supports rollback, test the Down() method:

```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext
```

## Best Practices

### Migration Design

1. **Keep migrations small** - One logical change per migration
2. **Make migrations reversible** - Implement Down() method properly
3. **Test both Up and Down** - Ensure migrations can be rolled back
4. **Add appropriate indexes** - Consider query performance
5. **Include seed data** - For required default values

### Data Migrations

When migrating data:

1. **Separate schema and data changes** - Create separate migrations if possible
2. **Handle large datasets** - Use batching for large data migrations
3. **Preserve existing data** - Don't delete data unnecessarily
4. **Test with production-like data** - Use realistic data volumes

Example data migration:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add new column
    migrationBuilder.AddColumn<string>(
        name: "Status",
        table: "Projects",
        nullable: true);
    
    // Migrate existing data
    migrationBuilder.Sql(@"
        UPDATE Projects
        SET Status = CASE
            WHEN IsCompleted = 1 THEN 'Completed'
            WHEN IsActive = 1 THEN 'Active'
            ELSE 'Draft'
        END
    ");
    
    // Make column required
    migrationBuilder.AlterColumn<string>(
        name: "Status",
        table: "Projects",
        nullable: false);
}
```

### Testing

1. **Test on clean database** - Use `aura-cli reset` to start fresh
2. **Test on existing database** - Verify migration works with existing data
3. **Test rollback** - Ensure Down() method works correctly
4. **Test in integration tests** - Add tests to verify migration behavior

### Code Review

Before committing:

1. **Review generated code** - Ensure migration does what you expect
2. **Check for breaking changes** - Avoid breaking existing functionality
3. **Verify naming** - Use consistent, descriptive names
4. **Test thoroughly** - Don't skip testing
5. **Document complex migrations** - Add comments for non-obvious changes

## Common Scenarios

### Adding a New Table

```bash
# 1. Create entity class in Aura.Core/Data/Entities/
# 2. Add DbSet to AuraDbContext
# 3. Configure entity in OnModelCreating
# 4. Create migration
dotnet ef migrations add AddMyNewTable \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext

# 5. Test migration
aura-cli migrate
aura-cli status -v
```

### Adding a Column

```bash
# 1. Add property to entity class
# 2. Create migration
dotnet ef migrations add AddColumnToTable \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext

# 3. Customize if needed (default value, data migration)
# 4. Test migration
aura-cli migrate
```

### Adding an Index

```bash
# 1. Add index configuration in OnModelCreating
modelBuilder.Entity<MyEntity>(entity =>
{
    entity.HasIndex(e => e.MyProperty);
});

# 2. Create migration
dotnet ef migrations add AddIndexToMyEntity \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext

# 3. Test migration
aura-cli migrate
```

### Renaming a Column

```bash
# 1. Rename property in entity class
# 2. Create migration
dotnet ef migrations add RenameColumnInTable \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext

# 3. Customize migration to use RenameColumn
migrationBuilder.RenameColumn(
    name: "OldName",
    table: "MyTable",
    newName: "NewName");

# 4. Test migration
aura-cli migrate
```

## Troubleshooting

### Migration Creation Fails

**Error**: "No DbContext was found"

**Solution**: Specify the context explicitly:
```bash
dotnet ef migrations add YourMigration \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext
```

### Migration Application Fails

**Error**: "Could not load file or assembly 'Aura.Api'"

**Solution**: Rebuild the solution:
```bash
dotnet build Aura.Api/Aura.Api.csproj
dotnet build Aura.Cli/Aura.Cli.csproj
```

### Conflicting Migrations

**Error**: "The migration '{name}' has already been applied"

**Solution**: Check current status and remove duplicate:
```bash
aura-cli status -v

# If duplicate, remove the migration file and recreate
dotnet ef migrations remove \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext
```

### Schema Conflicts

**Error**: "The table '{name}' already exists"

**Solution**: Either:
1. Reset database: `aura-cli reset --force`
2. Or manually fix the database schema
3. Or create a migration to handle the conflict

## Migration Lifecycle

```
┌─────────────────┐
│ Modify Entities │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Create Migration│ ─── dotnet ef migrations add
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Review & Test   │ ─── aura-cli status/migrate
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Commit to Git   │ ─── git add/commit
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Deploy & Apply  │ ─── Auto on API startup
└─────────────────┘
```

## Additional Resources

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core CLI Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [Migration Operations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/operations)
- [User Guide](DATABASE_MIGRATIONS_USER_GUIDE.md)

## Getting Help

For issues with migrations:
1. Check this guide first
2. Review EF Core documentation
3. Check Aura logs for detailed error messages
4. Use verbose mode: `aura-cli migrate -v`
5. Create an issue on GitHub with details

---

Last Updated: November 2024
