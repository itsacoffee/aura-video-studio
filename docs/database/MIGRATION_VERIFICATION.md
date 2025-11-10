# Migration Verification Guide

## Overview

This document provides verification procedures to ensure database migrations work correctly. Use these tests before deploying migrations to production.

## Verification Checklist

### Pre-Migration Verification

- [ ] All entity models have proper data annotations
- [ ] Relationships are correctly configured
- [ ] Indexes are defined for frequently queried columns
- [ ] Soft delete is implemented where needed
- [ ] Audit trails are configured
- [ ] Migration files are generated and reviewed
- [ ] Database backup is created

### Post-Migration Verification

- [ ] Migration applied successfully
- [ ] All tables created with correct schema
- [ ] Indexes are present
- [ ] Foreign keys are enforced
- [ ] Seed data is populated
- [ ] Audit trails are working
- [ ] Soft delete is functioning
- [ ] Rollback (Down migration) works
- [ ] Application can connect and query database

## Manual Verification Steps

### Step 1: Check Migration Status

```bash
# List all migrations and their status
./Scripts/run-migrations.sh list

# Expected output should show:
# ✓ InitialCreate (applied)
```

### Step 2: Verify Tables Exist

Using SQLite CLI:

```bash
# Open database
sqlite3 /path/to/aura.db

# List all tables
.tables

# Expected tables:
# ProjectStates, SceneStates, AssetStates, RenderCheckpoints
# ProjectVersions, ContentBlobs, UserSetups, SystemConfiguration
# Configurations, Templates, CustomTemplates, ExportHistory, ActionLogs
```

### Step 3: Verify Table Schema

```sql
-- Check ProjectStates schema
.schema ProjectStates

-- Verify columns exist
PRAGMA table_info(ProjectStates);

-- Expected: Id, Title, Description, Status, CreatedAt, UpdatedAt, etc.
```

### Step 4: Verify Indexes

```sql
-- List all indexes
SELECT name, tbl_name, sql 
FROM sqlite_master 
WHERE type = 'index';

-- Should see indexes on:
-- - ProjectStates: Status, UpdatedAt, JobId, IsDeleted
-- - SceneStates: ProjectId, (ProjectId + SceneIndex)
-- - And others as documented
```

### Step 5: Verify Foreign Keys

```bash
# Enable foreign key enforcement (should be on by default)
sqlite3 aura.db "PRAGMA foreign_keys = ON;"

# Check foreign keys
sqlite3 aura.db "PRAGMA foreign_key_list(SceneStates);"

# Expected: Foreign key from ProjectId to ProjectStates(Id)
```

### Step 6: Verify Seed Data

```sql
-- Check system configuration
SELECT * FROM system_configuration;
-- Expected: 1 row with IsSetupComplete = 0

-- Check for development seed data (if seeded)
SELECT COUNT(*) FROM ProjectStates;
SELECT COUNT(*) FROM Templates;
SELECT COUNT(*) FROM UserSetups;
```

### Step 7: Test Audit Trail Functionality

Create a test record and verify audit fields are populated:

```csharp
// In a test or console app
using var context = new AuraDbContext(options);

var project = new ProjectStateEntity
{
    Title = "Test Project",
    Status = "Draft"
};

await context.ProjectStates.AddAsync(project);
await context.SaveChangesAsync();

// Verify:
// - CreatedAt should be set to current UTC time
// - UpdatedAt should be set to current UTC time
// - Both should be equal on creation

Console.WriteLine($"CreatedAt: {project.CreatedAt}");
Console.WriteLine($"UpdatedAt: {project.UpdatedAt}");
```

### Step 8: Test Soft Delete Functionality

```csharp
// Create and soft delete a custom template
var template = new CustomTemplateEntity
{
    Name = "Test Template",
    Category = "Test"
};

context.CustomTemplates.Add(template);
await context.SaveChangesAsync();

// Soft delete
context.CustomTemplates.Remove(template);
await context.SaveChangesAsync();

// Verify it's still in database but marked as deleted
var allTemplates = await context.CustomTemplates
    .IgnoreQueryFilters() // Bypass soft delete filter
    .ToListAsync();

// Should find the template with IsDeleted = true, DeletedAt set
var deleted = allTemplates.First(t => t.Id == template.Id);
Console.WriteLine($"IsDeleted: {deleted.IsDeleted}");
Console.WriteLine($"DeletedAt: {deleted.DeletedAt}");
```

### Step 9: Test Relationships and Cascade Delete

```csharp
// Create a project with scenes
var project = new ProjectStateEntity
{
    Title = "Test Project",
    Status = "Draft"
};

context.ProjectStates.Add(project);
await context.SaveChangesAsync();

var scene = new SceneStateEntity
{
    ProjectId = project.Id,
    SceneIndex = 0,
    ScriptText = "Test scene"
};

context.SceneStates.Add(scene);
await context.SaveChangesAsync();

// Delete project (should cascade to scenes)
context.ProjectStates.Remove(project);
await context.SaveChangesAsync();

// Verify scene was deleted
var sceneExists = await context.SceneStates
    .AnyAsync(s => s.Id == scene.Id);

// sceneExists should be false
Console.WriteLine($"Scene exists after project delete: {sceneExists}");
```

### Step 10: Test Migration Rollback

```bash
# Rollback to initial state
./Scripts/run-migrations.sh rollback 0

# Verify tables are dropped
sqlite3 aura.db ".tables"
# Should show empty or minimal tables

# Re-apply migrations
./Scripts/run-migrations.sh migrate

# Verify everything is back
./Scripts/run-migrations.sh list
```

## Automated Verification Tests

### Integration Test Example

```csharp
using Xunit;
using Microsoft.EntityFrameworkCore;
using Aura.Core.Data;

public class MigrationTests
{
    [Fact]
    public async Task InitialMigration_CreatesAllTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AuraDbContext(options);
        
        // Act
        await context.Database.EnsureCreatedAsync();
        
        // Assert
        Assert.True(await context.Database.CanConnectAsync());
        
        // Verify each DbSet is accessible
        var projectCount = await context.ProjectStates.CountAsync();
        var sceneCount = await context.SceneStates.CountAsync();
        var assetCount = await context.AssetStates.CountAsync();
        
        // All should return 0 (no data yet) but not throw
        Assert.Equal(0, projectCount);
        Assert.Equal(0, sceneCount);
        Assert.Equal(0, assetCount);
    }
    
    [Fact]
    public async Task AuditTrail_AutomaticallySetTimestamps()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AuraDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        // Act
        var project = new ProjectStateEntity
        {
            Title = "Test",
            Status = "Draft"
        };
        
        context.ProjectStates.Add(project);
        await context.SaveChangesAsync();
        
        // Assert
        Assert.NotEqual(default(DateTime), project.CreatedAt);
        Assert.NotEqual(default(DateTime), project.UpdatedAt);
        Assert.Equal(project.CreatedAt, project.UpdatedAt);
    }
    
    [Fact]
    public async Task SoftDelete_MarksEntityAsDeleted()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AuraDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var template = new CustomTemplateEntity
        {
            Name = "Test",
            Category = "Test"
        };
        
        context.CustomTemplates.Add(template);
        await context.SaveChangesAsync();
        
        // Act
        context.CustomTemplates.Remove(template);
        await context.SaveChangesAsync();
        
        // Assert
        var deletedTemplate = await context.CustomTemplates
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == template.Id);
            
        Assert.True(deletedTemplate.IsDeleted);
        Assert.NotNull(deletedTemplate.DeletedAt);
        
        // Verify it's filtered from normal queries
        var normalQuery = await context.CustomTemplates
            .Where(t => t.Id == template.Id)
            .FirstOrDefaultAsync();
            
        Assert.Null(normalQuery);
    }
    
    [Fact]
    public async Task CascadeDelete_DeletesRelatedEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AuraDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var project = new ProjectStateEntity
        {
            Title = "Test",
            Status = "Draft"
        };
        
        context.ProjectStates.Add(project);
        await context.SaveChangesAsync();
        
        var scene = new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 0,
            ScriptText = "Test"
        };
        
        context.SceneStates.Add(scene);
        await context.SaveChangesAsync();
        
        var sceneId = scene.Id;
        
        // Act
        context.ProjectStates.Remove(project);
        await context.SaveChangesAsync();
        
        // Assert
        var sceneExists = await context.SceneStates
            .AnyAsync(s => s.Id == sceneId);
            
        Assert.False(sceneExists);
    }
}
```

## Performance Verification

### Query Performance Tests

```csharp
using System.Diagnostics;

public class PerformanceTests
{
    [Fact]
    public async Task ProjectLookupByStatus_UsesIndex()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestData(context, projectCount: 1000);
        
        // Act
        var sw = Stopwatch.StartNew();
        var inProgressProjects = await context.ProjectStates
            .Where(p => p.Status == "InProgress")
            .ToListAsync();
        sw.Stop();
        
        // Assert
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"Query took {sw.ElapsedMilliseconds}ms (should be < 100ms with index)");
    }
    
    [Fact]
    public async Task SceneLookupByProject_UsesCompositeIndex()
    {
        // Arrange
        using var context = CreateContext();
        var projectId = await SeedProjectWithScenes(context, sceneCount: 100);
        
        // Act
        var sw = Stopwatch.StartNew();
        var scenes = await context.SceneStates
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.SceneIndex)
            .ToListAsync();
        sw.Stop();
        
        // Assert
        Assert.Equal(100, scenes.Count);
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Query took {sw.ElapsedMilliseconds}ms (should be < 50ms with index)");
    }
}
```

## Common Issues and Solutions

### Issue: Migrations Not Detected

**Symptoms**: `dotnet ef migrations list` shows no migrations

**Solutions**:
1. Ensure you're in the correct directory
2. Check that migration files exist in `Aura.Api/Data/Migrations/`
3. Verify project references are correct
4. Clean and rebuild solution

### Issue: "Database is locked"

**Symptoms**: SQLite error during migration

**Solutions**:
1. Close all connections to database
2. Stop application if running
3. Check for other processes using the database file

### Issue: Foreign Key Constraint Violation

**Symptoms**: Cannot insert/update due to FK violation

**Solutions**:
1. Ensure foreign key exists before creating child record
2. Check cascade delete configuration
3. Verify relationship navigation properties

### Issue: Audit Fields Not Populating

**Symptoms**: CreatedAt/UpdatedAt remain default values

**Solutions**:
1. Verify entity implements `IAuditableEntity`
2. Check `SaveChanges` override in DbContext
3. Ensure `UpdateAuditFields()` method is called

### Issue: Soft Delete Not Working

**Symptoms**: Records are hard-deleted or still appear in queries

**Solutions**:
1. Verify entity implements `ISoftDeletable`
2. Check global query filter is applied
3. Ensure `SaveChanges` override handles soft delete

## Continuous Integration Verification

### GitHub Actions Test

```yaml
name: Verify Migrations

on: [push, pull_request]

jobs:
  verify:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run migration tests
      run: dotnet test --filter Category=Migration
    
    - name: Generate migration script
      run: |
        dotnet ef migrations script \
          --project Aura.Api \
          --output migration.sql \
          --idempotent
    
    - name: Upload migration script
      uses: actions/upload-artifact@v3
      with:
        name: migration-script
        path: migration.sql
```

## Sign-Off Checklist

Before marking migration as verified:

- [ ] All automated tests pass
- [ ] Manual verification steps completed
- [ ] Performance tests within acceptable range
- [ ] Rollback tested successfully
- [ ] Documentation updated
- [ ] Team review completed
- [ ] Staging environment validated
- [ ] Production deployment plan reviewed

## Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Developer | _____ | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |
| DBA | _____ | _____ | _____ |

## Notes

Add any additional notes or observations here:

---

**Last Updated**: 2025-11-10
**Migration Version**: InitialCreate
**Database Version**: 1.0.0
