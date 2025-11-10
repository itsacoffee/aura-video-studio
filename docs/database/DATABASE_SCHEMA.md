# Aura Database Schema Documentation

## Overview

The Aura database uses **SQLite** with Entity Framework Core for data persistence. The schema is designed to support:

- Project state management and recovery
- Version control and snapshots
- User configuration and setup tracking
- Export history and templates
- Audit trails and soft delete
- Content deduplication

## Database Technology

- **Database**: SQLite 3.x
- **ORM**: Entity Framework Core 8.0
- **Migration Tool**: EF Core Migrations
- **Connection String**: Configured in `appsettings.json`

## Schema Design Principles

1. **Audit Trails**: All entities implement `IAuditableEntity` for automatic timestamp tracking
2. **Soft Delete**: Entities implement `ISoftDeletable` to preserve data integrity
3. **Optimistic Concurrency**: Version columns prevent concurrent update conflicts
4. **Indexed Queries**: Strategic indexes for common query patterns
5. **Content Deduplication**: Blobs are content-addressed using SHA-256 hashing

## Core Tables

### 1. ProjectStates

Primary table for video generation projects.

**Columns:**
- `Id` (GUID, PK): Unique project identifier
- `Title` (string, required): Project title
- `Description` (string, nullable): Project description
- `CurrentWizardStep` (int): Current step in wizard (0-based)
- `Status` (string): InProgress, Completed, Failed, Cancelled
- `CreatedAt`, `UpdatedAt` (DateTime): Audit timestamps
- `CompletedAt` (DateTime, nullable): When project completed
- `IsDeleted`, `DeletedAt`, `DeletedBy`: Soft delete support
- `CreatedBy`, `ModifiedBy`: User tracking
- `CurrentStage` (string): Script, TTS, Images, Composition, Render
- `ProgressPercent` (int): Overall progress (0-100)
- `JobId` (string, nullable): Associated background job
- `BriefJson`, `PlanSpecJson`, `VoiceSpecJson`, `RenderSpecJson` (TEXT): JSON data
- `ErrorMessage` (TEXT, nullable): Last error if failed

**Relationships:**
- One-to-Many with `SceneStates`
- One-to-Many with `AssetStates`
- One-to-Many with `RenderCheckpoints`
- One-to-Many with `ProjectVersions`

**Indexes:**
- `Status`
- `UpdatedAt`
- `Status, UpdatedAt` (composite)
- `JobId`
- `IsDeleted`
- `IsDeleted, DeletedAt` (composite)

---

### 2. SceneStates

Stores individual scenes within a project.

**Columns:**
- `Id` (GUID, PK): Unique scene identifier
- `ProjectId` (GUID, FK): Parent project
- `SceneIndex` (int): Scene order (0-based)
- `ScriptText` (TEXT, required): Scene script
- `AudioFilePath` (string, nullable): Generated audio file
- `ImageFilePath` (string, nullable): Generated image file
- `DurationSeconds` (double): Scene duration
- `IsCompleted` (bool): Scene processing complete
- `CreatedAt` (DateTime): Creation timestamp

**Relationships:**
- Many-to-One with `ProjectStates` (cascade delete)

**Indexes:**
- `ProjectId`
- `ProjectId, SceneIndex` (composite)

---

### 3. AssetStates

Tracks files (audio, images, videos) associated with projects.

**Columns:**
- `Id` (GUID, PK): Unique asset identifier
- `ProjectId` (GUID, FK): Parent project
- `AssetType` (string, required): Audio, Image, Video, Subtitle
- `FilePath` (string, required): File system path
- `FileSizeBytes` (long): File size
- `MimeType` (string, nullable): MIME type
- `IsTemporary` (bool): Cleanup flag
- `CreatedAt` (DateTime): Creation timestamp

**Relationships:**
- Many-to-One with `ProjectStates` (cascade delete)

**Indexes:**
- `ProjectId`
- `ProjectId, AssetType` (composite)
- `IsTemporary`

---

### 4. RenderCheckpoints

Recovery checkpoints during video rendering.

**Columns:**
- `Id` (GUID, PK): Unique checkpoint identifier
- `ProjectId` (GUID, FK): Parent project
- `StageName` (string, required): Stage name
- `CheckpointTime` (DateTime, required): When checkpoint was created
- `CompletedScenes` (int): Number of completed scenes
- `TotalScenes` (int): Total number of scenes
- `CheckpointData` (TEXT, nullable): JSON checkpoint data
- `OutputFilePath` (string, nullable): Intermediate output file
- `IsValid` (bool): Checkpoint validity flag

**Relationships:**
- Many-to-One with `ProjectStates` (cascade delete)

**Indexes:**
- `ProjectId`
- `ProjectId, StageName` (composite)
- `CheckpointTime`

---

### 5. ProjectVersions

Version control and snapshots for projects.

**Columns:**
- `Id` (GUID, PK): Unique version identifier
- `ProjectId` (GUID, FK): Parent project
- `VersionNumber` (int, required): Auto-incremented version number
- `Name` (string, nullable): User-provided name
- `Description` (string, nullable): Version description
- `VersionType` (string, required): Manual, Autosave, RestorePoint
- `Trigger` (string, nullable): What triggered this version
- `CreatedAt` (DateTime, required): Creation timestamp
- `CreatedByUserId` (string, nullable): User who created version
- `BriefJson`, `PlanSpecJson`, `VoiceSpecJson`, `RenderSpecJson`, `TimelineJson` (TEXT): Versioned data
- `BriefHash`, `PlanHash`, `VoiceHash`, `RenderHash`, `TimelineHash` (string): Content hashes for deduplication
- `StorageSizeBytes` (long): Total storage used
- `IsMarkedImportant` (bool): Protected from auto-pruning
- `IsDeleted`, `DeletedAt`, `DeletedBy`: Soft delete support
- `UpdatedAt`, `CreatedBy`, `ModifiedBy`: Audit fields

**Relationships:**
- Many-to-One with `ProjectStates` (cascade delete)

**Indexes:**
- `ProjectId`
- `ProjectId, VersionNumber` (composite, unique)
- `VersionType`
- `CreatedAt`
- `ProjectId, CreatedAt` (composite)
- `IsDeleted`
- `IsDeleted, DeletedAt` (composite)
- `IsMarkedImportant`

---

### 6. ContentBlobs

Deduplicated content storage using content-addressable storage.

**Columns:**
- `ContentHash` (string, PK): SHA-256 hash of content
- `Content` (TEXT, required): Actual content
- `ContentType` (string, required): Brief, Plan, Voice, Render, Timeline, Asset
- `SizeBytes` (long, required): Content size
- `CreatedAt` (DateTime, required): First created
- `LastReferencedAt` (DateTime, required): Last referenced by a version
- `ReferenceCount` (int, required): Number of versions referencing this
- `UpdatedAt`, `CreatedBy`, `ModifiedBy`: Audit fields

**Indexes:**
- `ContentType`
- `LastReferencedAt`
- `ReferenceCount`

---

### 7. UserSetups

Tracks first-run wizard completion status.

**Columns:**
- `Id` (string, PK): Unique setup identifier
- `UserId` (string, required, unique): User identifier
- `Completed` (bool, required): Wizard completed flag
- `CompletedAt` (DateTime, nullable): Completion timestamp
- `Version` (string, nullable): Wizard version
- `LastStep` (int): Last completed step
- `UpdatedAt` (DateTime, required): Last update time
- `SelectedTier` (string, nullable): Selected pricing tier
- `WizardState` (TEXT, nullable): JSON state blob

**Indexes:**
- `UserId` (unique)
- `Completed`
- `UpdatedAt`

---

### 8. SystemConfiguration

System-wide configuration (single row table).

**Columns:**
- `Id` (int, PK): Always 1
- `IsSetupComplete` (bool, required): Initial setup complete
- `FFmpegPath` (string, nullable): Path to FFmpeg executable
- `OutputDirectory` (string, required): Default output directory
- `CreatedAt`, `UpdatedAt` (DateTime, required): Audit timestamps

**Seeded Data:**
- Default record with `IsSetupComplete = false`

---

### 9. Configurations

Key-value configuration storage.

**Columns:**
- `Key` (string, PK): Configuration key
- `Value` (TEXT, required): Configuration value (JSON)
- `Category` (string, required): Grouping category
- `ValueType` (string, required): string, json, boolean, number
- `Description` (string, nullable): Configuration description
- `IsSensitive` (bool): Sensitive data flag (API keys, etc.)
- `Version` (int): Schema version
- `CreatedAt`, `UpdatedAt` (DateTime, required): Audit timestamps
- `CreatedBy`, `ModifiedBy` (string, nullable): User tracking
- `IsActive` (bool): Active flag

**Indexes:**
- `Category`
- `IsSensitive`
- `IsActive`
- `UpdatedAt`
- `Category, IsActive` (composite)
- `Category, UpdatedAt` (composite)

---

### 10. Templates

System and community project templates.

**Columns:**
- `Id` (string, PK): Unique template identifier
- `Name` (string, required): Template name
- `Description` (string, required): Template description
- `Category` (string, required): Template category
- `SubCategory` (string): Sub-category
- `PreviewImage`, `PreviewVideo` (string): Preview media paths
- `Tags` (string): Comma-separated tags
- `TemplateData` (TEXT, required): JSON template data
- `CreatedAt`, `UpdatedAt` (DateTime, required): Audit timestamps
- `Author` (string, required): Template author
- `IsSystemTemplate`, `IsCommunityTemplate` (bool): Template type flags
- `UsageCount` (int): Number of times used
- `Rating` (double): Average rating
- `RatingCount` (int): Number of ratings

**Indexes:**
- `Category`
- `IsSystemTemplate`
- `IsCommunityTemplate`
- `Category, SubCategory` (composite)

---

### 11. CustomTemplates

User-created custom templates.

**Columns:**
- `Id` (string, PK): Unique template identifier
- `Name` (string, required): Template name
- `Description` (string): Template description
- `Category` (string, required): Template category
- `Tags` (string): Comma-separated tags
- `CreatedAt`, `UpdatedAt` (DateTime, required): Audit timestamps
- `CreatedBy`, `ModifiedBy` (string, nullable): User tracking
- `Author` (string, required): Template author
- `IsDefault` (bool): Default template flag
- `IsDeleted`, `DeletedAt`, `DeletedBy`: Soft delete support
- `ScriptStructureJson`, `VideoStructureJson`, `LLMPipelineJson`, `VisualPreferencesJson` (TEXT): JSON configurations

**Indexes:**
- `Category`
- `IsDefault`
- `CreatedAt`
- `Category, CreatedAt` (composite)
- `IsDeleted`
- `IsDeleted, DeletedAt` (composite)

---

### 12. ExportHistory

Export job history and status.

**Columns:**
- `Id` (string, PK): Unique export identifier
- `InputFile` (string, required): Source file path
- `OutputFile` (string, required): Output file path
- `PresetName` (string, required): Export preset name
- `Status` (string, required): Queued, InProgress, Completed, Failed
- `Progress` (double): Progress percentage
- `CreatedAt` (DateTime, required): Job creation time
- `StartedAt`, `CompletedAt` (DateTime, nullable): Job timing
- `ErrorMessage` (string, nullable): Error message if failed
- `FileSize` (long, nullable): Output file size
- `DurationSeconds` (double, nullable): Video duration
- `Platform` (string, nullable): Target platform
- `Resolution` (string, nullable): Video resolution
- `Codec` (string, nullable): Video codec

**Indexes:**
- `Status`
- `CreatedAt`
- `Status, CreatedAt` (composite)

---

### 13. ActionLogs

Server-side undo/redo action log.

**Columns:**
- `Id` (GUID, PK): Unique action identifier
- `UserId` (string, required): User who performed action
- `ActionType` (string, required): Action type name
- `Description` (string, required): Human-readable description
- `Timestamp` (DateTime, required): When action occurred
- `Status` (string, required): Applied, Undone, Failed, Expired
- `AffectedResourceIds` (string, nullable): Comma-separated resource IDs
- `PayloadJson` (TEXT, nullable): Action payload
- `InverseActionType` (string, nullable): Undo action type
- `InversePayloadJson` (TEXT, nullable): Undo payload
- `CanBatch` (bool): Batchable flag
- `IsPersistent` (bool): Persistence flag
- `UndoneAt` (DateTime, nullable): When action was undone
- `UndoneByUserId` (string, nullable): User who undone action
- `ExpiresAt` (DateTime, nullable): Expiration for cleanup
- `ErrorMessage` (TEXT, nullable): Error message if failed
- `CorrelationId` (string, nullable): Correlation ID for tracking

**Indexes:**
- `UserId`
- `ActionType`
- `Status`
- `Timestamp`
- `UserId, Timestamp` (composite)
- `Status, Timestamp` (composite)
- `CorrelationId`
- `ExpiresAt`

---

## Entity Relationships

```
ProjectStates (1) ─┬─ (*) SceneStates
                   ├─ (*) AssetStates
                   ├─ (*) RenderCheckpoints
                   └─ (*) ProjectVersions

ProjectVersions (*) ──→ ContentBlobs (content-addressed)
```

## Audit Trail Features

All entities implementing `IAuditableEntity` automatically track:
- `CreatedAt`: Set on entity creation
- `UpdatedAt`: Updated on every modification
- `CreatedBy`: User who created entity (optional)
- `ModifiedBy`: User who last modified entity (optional)

## Soft Delete Features

All entities implementing `ISoftDeletable` support:
- `IsDeleted`: Soft delete flag
- `DeletedAt`: When entity was deleted
- `DeletedBy`: User who deleted entity

**Global Query Filter**: Soft-deleted entities are automatically excluded from queries unless explicitly requested with `IgnoreQueryFilters()`.

## Concurrency Control

EF Core's built-in optimistic concurrency is used. If row version conflicts occur, a `DbUpdateConcurrencyException` is thrown and handled by the application.

## Performance Considerations

### Indexed Columns

All frequently queried columns have indexes:
- Foreign keys
- Status/state columns
- Timestamp columns
- Composite indexes for common query patterns

### Query Tips

1. **Include related entities** when needed to avoid N+1 queries:
   ```csharp
   var project = await context.ProjectStates
       .Include(p => p.Scenes)
       .Include(p => p.Assets)
       .FirstOrDefaultAsync(p => p.Id == projectId);
   ```

2. **Use projections** to select only needed columns:
   ```csharp
   var projectTitles = await context.ProjectStates
       .Select(p => new { p.Id, p.Title })
       .ToListAsync();
   ```

3. **Paginate large result sets**:
   ```csharp
   var page = await context.ProjectStates
       .OrderByDescending(p => p.UpdatedAt)
       .Skip(pageSize * pageNumber)
       .Take(pageSize)
       .ToListAsync();
   ```

## Backup and Restore

### Backup

```bash
./Scripts/backup-database.sh [db-path] [backup-dir]
```

### Restore

```bash
./Scripts/restore-database.sh <backup-file> [db-path]
```

## Migration Management

See [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) for detailed migration instructions.

## Security Considerations

1. **Sensitive Data**: Configurations with `IsSensitive = true` should be encrypted
2. **Row-Level Security**: Implement in application layer (SQLite has limited built-in support)
3. **SQL Injection**: EF Core uses parameterized queries automatically
4. **Connection String**: Store securely in `appsettings.json` or environment variables

## GDPR Compliance

- **Soft Delete**: Preserves data integrity while allowing "deletion"
- **Audit Trails**: Track who accessed/modified what data
- **Data Export**: Repository methods support exporting user data
- **Right to be Forgotten**: Use `DeleteAsync` with `forceDelete: true` for hard deletes

## Troubleshooting

### Connection Issues

1. Check database file permissions
2. Verify connection string in `appsettings.json`
3. Ensure SQLite native binaries are installed

### Migration Failures

1. Check for conflicting migrations
2. Review migration logs
3. Use rollback scripts if needed

### Performance Issues

1. Review query execution plans
2. Check for missing indexes
3. Enable SQL logging for debugging

## Future Enhancements

- [ ] Add full-text search indexes
- [ ] Implement database sharding for large datasets
- [ ] Add database replication for high availability
- [ ] Implement column-level encryption for PII
