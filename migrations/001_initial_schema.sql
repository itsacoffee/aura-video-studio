-- Migration: 001_initial_schema
-- Description: Initial database schema for Aura Video Studio
-- This is a reference schema - actual migrations are handled by Entity Framework
-- This file documents the expected schema for manual database setup if needed

-- NOTE: Aura uses Entity Framework migrations. This SQL is for documentation
-- and manual setup purposes only. The actual schema is managed by EF Core migrations
-- in Aura.Api/Migrations/

-- Projects table
-- Stores video project metadata
CREATE TABLE IF NOT EXISTS Projects (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT,
    Status INTEGER NOT NULL DEFAULT 0,
    Theme TEXT,
    DurationSeconds INTEGER,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    Version INTEGER NOT NULL DEFAULT 1,
    Metadata TEXT
);

CREATE INDEX IF NOT EXISTS IX_Projects_Status ON Projects(Status);
CREATE INDEX IF NOT EXISTS IX_Projects_CreatedAt ON Projects(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Projects_IsDeleted ON Projects(IsDeleted);

-- UserSetup table
-- Stores user setup and first-run wizard completion status
CREATE TABLE IF NOT EXISTS UserSetup (
    Id TEXT PRIMARY KEY,
    WizardCompleted INTEGER NOT NULL DEFAULT 0,
    FirstRunDate TEXT,
    Version TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

-- ProjectState table
-- Stores serialized project state for persistence
CREATE TABLE IF NOT EXISTS ProjectState (
    Id TEXT PRIMARY KEY,
    ProjectId TEXT NOT NULL,
    StateJson TEXT NOT NULL,
    Version INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ProjectState_ProjectId ON ProjectState(ProjectId);
CREATE INDEX IF NOT EXISTS IX_ProjectState_UpdatedAt ON ProjectState(UpdatedAt);

-- ActionLog table
-- Stores audit trail of user actions
CREATE TABLE IF NOT EXISTS ActionLog (
    Id TEXT PRIMARY KEY,
    ProjectId TEXT,
    Action TEXT NOT NULL,
    Category TEXT,
    Details TEXT,
    UserId TEXT,
    Timestamp TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_ActionLog_ProjectId ON ActionLog(ProjectId);
CREATE INDEX IF NOT EXISTS IX_ActionLog_Timestamp ON ActionLog(Timestamp);
CREATE INDEX IF NOT EXISTS IX_ActionLog_Category ON ActionLog(Category);

-- SystemConfiguration table
-- Stores system-wide configuration and settings
CREATE TABLE IF NOT EXISTS SystemConfiguration (
    Id TEXT PRIMARY KEY,
    Key TEXT NOT NULL UNIQUE,
    Value TEXT,
    Description TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_SystemConfiguration_Key ON SystemConfiguration(Key) WHERE IsDeleted = 0;

-- WizardProjects table
-- Stores projects created through the wizard workflow
CREATE TABLE IF NOT EXISTS WizardProjects (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Topic TEXT NOT NULL,
    Duration INTEGER NOT NULL,
    Status TEXT NOT NULL,
    CurrentStep INTEGER NOT NULL DEFAULT 0,
    CompletedSteps TEXT,
    ErrorMessage TEXT,
    StateJson TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT
);

CREATE INDEX IF NOT EXISTS IX_WizardProjects_Status ON WizardProjects(Status);
CREATE INDEX IF NOT EXISTS IX_WizardProjects_CreatedAt ON WizardProjects(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_WizardProjects_IsDeleted ON WizardProjects(IsDeleted);

-- Insert migration version tracking
INSERT OR IGNORE INTO SystemConfiguration (Id, Key, Value, Description, CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    lower(hex(randomblob(16))),
    'database.schema_version',
    '001',
    'Current database schema version',
    datetime('now'),
    datetime('now'),
    0
);
