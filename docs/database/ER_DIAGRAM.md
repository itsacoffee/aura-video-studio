# Entity Relationship Diagram

## Database Schema Overview

This document provides a visual representation of the Aura database schema and entity relationships.

## Core Entity Relationships

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              CORE TABLES                                │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────┐
│   ProjectStates          │ (Main project table)
├──────────────────────────┤
│ • Id (PK, GUID)          │
│ • Title                  │
│ • Description            │
│ • Status                 │──┐
│ • CurrentWizardStep      │  │
│ • CurrentStage           │  │
│ • ProgressPercent        │  │
│ • JobId                  │  │
│ • BriefJson              │  │
│ • PlanSpecJson           │  │
│ • VoiceSpecJson          │  │
│ • RenderSpecJson         │  │
│ • CreatedAt, UpdatedAt   │  │
│ • IsDeleted, DeletedAt   │  │
└──────────────────────────┘  │
                              │
        ┌─────────────────────┴─────────────────────┬──────────────────────┐
        │                                           │                      │
        ▼                                           ▼                      ▼
┌──────────────────────┐                  ┌──────────────────┐  ┌──────────────────┐
│   SceneStates        │                  │  AssetStates     │  │ RenderCheckpoints │
├──────────────────────┤                  ├──────────────────┤  ├──────────────────┤
│ • Id (PK, GUID)      │                  │ • Id (PK, GUID)  │  │ • Id (PK, GUID)  │
│ • ProjectId (FK)     │◄─────────────────┤ • ProjectId (FK) │  │ • ProjectId (FK) │
│ • SceneIndex         │ 1:N              │ • AssetType      │  │ • StageName      │
│ • ScriptText         │                  │ • FilePath       │  │ • CheckpointTime │
│ • AudioFilePath      │                  │ • FileSizeBytes  │  │ • CompletedScenes│
│ • ImageFilePath      │                  │ • MimeType       │  │ • TotalScenes    │
│ • DurationSeconds    │                  │ • IsTemporary    │  │ • CheckpointData │
│ • IsCompleted        │                  │ • CreatedAt      │  │ • OutputFilePath │
│ • CreatedAt          │                  └──────────────────┘  │ • IsValid        │
└──────────────────────┘                                        └──────────────────┘
                                                                          │
                                                                          │
        ┌─────────────────────────────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│   ProjectVersions        │ (Version control & snapshots)
├──────────────────────────┤
│ • Id (PK, GUID)          │
│ • ProjectId (FK)         │◄───────┐
│ • VersionNumber          │ 1:N    │
│ • Name                   │        │
│ • Description            │        │
│ • VersionType            │        │
│ • BriefHash     ─────────┼────────┼──┐
│ • PlanHash      ─────────┼────────┼──┤
│ • VoiceHash     ─────────┼────────┼──┤
│ • RenderHash    ─────────┼────────┼──┤
│ • TimelineHash  ─────────┼────────┼──┤
│ • StorageSizeBytes       │        │  │
│ • IsMarkedImportant      │        │  │
│ • IsDeleted, DeletedAt   │        │  │
│ • CreatedAt, UpdatedAt   │        │  │
└──────────────────────────┘        │  │
                                    │  │
                                    │  ▼
                                    │  ┌──────────────────────┐
                                    │  │   ContentBlobs       │
                                    │  ├──────────────────────┤
                                    │  │ • ContentHash (PK)   │
                                    └──┤ • Content            │
                                    M:1│ • ContentType        │
                                       │ • SizeBytes          │
                                       │ • CreatedAt          │
                                       │ • LastReferencedAt   │
                                       │ • ReferenceCount     │
                                       └──────────────────────┘
                                       (Content-addressed storage)


┌─────────────────────────────────────────────────────────────────────────┐
│                          CONFIGURATION TABLES                           │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────┐  ┌──────────────────────────┐  ┌────────────────────┐
│   SystemConfiguration    │  │   Configurations         │  │   UserSetups       │
├──────────────────────────┤  ├──────────────────────────┤  ├────────────────────┤
│ • Id (PK, int) = 1       │  │ • Key (PK, string)       │  │ • Id (PK, string)  │
│ • IsSetupComplete        │  │ • Value                  │  │ • UserId (unique)  │
│ • FFmpegPath             │  │ • Category               │  │ • Completed        │
│ • OutputDirectory        │  │ • ValueType              │  │ • CompletedAt      │
│ • CreatedAt, UpdatedAt   │  │ • Description            │  │ • Version          │
└──────────────────────────┘  │ • IsSensitive            │  │ • LastStep         │
 (Single row, global config) │ • Version                │  │ • UpdatedAt        │
                              │ • IsActive               │  │ • SelectedTier     │
                              │ • CreatedAt, UpdatedAt   │  │ • WizardState      │
                              └──────────────────────────┘  └────────────────────┘


┌─────────────────────────────────────────────────────────────────────────┐
│                          TEMPLATE & HISTORY TABLES                      │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────┐  ┌──────────────────────────┐
│   Templates              │  │   CustomTemplates        │
├──────────────────────────┤  ├──────────────────────────┤
│ • Id (PK, string)        │  │ • Id (PK, string)        │
│ • Name                   │  │ • Name                   │
│ • Description            │  │ • Description            │
│ • Category               │  │ • Category               │
│ • SubCategory            │  │ • Tags                   │
│ • PreviewImage           │  │ • Author                 │
│ • PreviewVideo           │  │ • IsDefault              │
│ • Tags                   │  │ • ScriptStructureJson    │
│ • TemplateData           │  │ • VideoStructureJson     │
│ • Author                 │  │ • LLMPipelineJson        │
│ • IsSystemTemplate       │  │ • VisualPreferencesJson  │
│ • IsCommunityTemplate    │  │ • IsDeleted, DeletedAt   │
│ • UsageCount             │  │ • CreatedAt, UpdatedAt   │
│ • Rating, RatingCount    │  └──────────────────────────┘
│ • CreatedAt, UpdatedAt   │
└──────────────────────────┘

┌──────────────────────────┐  ┌──────────────────────────┐
│   ExportHistory          │  │   ActionLogs             │
├──────────────────────────┤  ├──────────────────────────┤
│ • Id (PK, string)        │  │ • Id (PK, GUID)          │
│ • InputFile              │  │ • UserId                 │
│ • OutputFile             │  │ • ActionType             │
│ • PresetName             │  │ • Description            │
│ • Status                 │  │ • Timestamp              │
│ • Progress               │  │ • Status                 │
│ • CreatedAt              │  │ • PayloadJson            │
│ • StartedAt              │  │ • InverseActionType      │
│ • CompletedAt            │  │ • InversePayloadJson     │
│ • ErrorMessage           │  │ • CanBatch, IsPersistent │
│ • FileSize               │  │ • UndoneAt, UndoneBy     │
│ • DurationSeconds        │  │ • ExpiresAt              │
│ • Platform               │  │ • CorrelationId          │
│ • Resolution, Codec      │  └──────────────────────────┘
└──────────────────────────┘   (Undo/Redo support)
 (Export job tracking)


┌─────────────────────────────────────────────────────────────────────────┐
│                          KEY DESIGN PATTERNS                            │
└─────────────────────────────────────────────────────────────────────────┘

1. CASCADE DELETE
   ProjectStates → SceneStates, AssetStates, RenderCheckpoints, ProjectVersions
   When a project is deleted, all related records are automatically removed.

2. CONTENT-ADDRESSED STORAGE
   ProjectVersions store hashes, ContentBlobs store actual content.
   Same content = same hash = single storage (deduplication).

3. SOFT DELETE
   ProjectStates, CustomTemplates, ProjectVersions use IsDeleted flag.
   Data is never actually removed, only marked as deleted.

4. AUDIT TRAILS
   All entities track CreatedAt, UpdatedAt, CreatedBy, ModifiedBy.
   Automatic timestamp updates via DbContext.SaveChanges override.

5. VERSION CONTROL
   ProjectVersions provide full project history with restore points.
   Supports Manual saves, Autosaves, and automatic RestorePoints.


┌─────────────────────────────────────────────────────────────────────────┐
│                             INDEX STRATEGY                              │
└─────────────────────────────────────────────────────────────────────────┘

High-traffic columns are indexed for performance:

• ProjectStates: Status, UpdatedAt, JobId, IsDeleted
• SceneStates: ProjectId, (ProjectId + SceneIndex)
• AssetStates: ProjectId, IsTemporary
• ProjectVersions: ProjectId, VersionNumber, VersionType
• Templates: Category, IsSystemTemplate
• ExportHistory: Status, CreatedAt
• ActionLogs: UserId, ActionType, Timestamp, CorrelationId
• Configurations: Category, IsActive, IsSensitive

Composite indexes optimize common query patterns:
• (Status, UpdatedAt) for filtered sorted queries
• (ProjectId, SceneIndex) for scene lookups
• (Category, SubCategory) for template filtering


┌─────────────────────────────────────────────────────────────────────────┐
│                          DATA FLOW EXAMPLES                             │
└─────────────────────────────────────────────────────────────────────────┘

1. CREATE PROJECT
   User → ProjectStates (new record)
        → SceneStates (scenes created)
        → ProjectVersions (initial version)
        → ContentBlobs (content stored)

2. UPDATE PROJECT
   User → ProjectStates (updated)
        → ProjectVersions (new version snapshot)
        → ContentBlobs (reuse existing via hash or create new)

3. EXPORT VIDEO
   User → ExportHistory (new job record)
        → Status: Queued → InProgress → Completed
        → FileSize, DurationSeconds populated on completion

4. UNDO ACTION
   User → ActionLogs (find action)
        → Apply InverseActionType with InversePayloadJson
        → Update Status to "Undone"


┌─────────────────────────────────────────────────────────────────────────┐
│                          STORAGE ESTIMATES                              │
└─────────────────────────────────────────────────────────────────────────┘

Typical sizes per record:

• ProjectStates: ~2-5 KB (with JSON fields)
• SceneStates: ~1-2 KB per scene
• ProjectVersions: ~5-10 KB (mostly hashes, content in blobs)
• ContentBlobs: ~1-50 KB per content piece
• Templates: ~2-5 KB
• ExportHistory: ~500 bytes

Example project with 10 scenes and 5 versions:
• 1 ProjectState: 3 KB
• 10 SceneStates: 15 KB
• 10 AssetStates: 5 KB
• 5 ProjectVersions: 25 KB (hashes only)
• Content in blobs (deduplicated): ~50-200 KB
Total: ~100-250 KB per project
