-- Seed: 002_sample_projects
-- Description: Create sample projects for local development and testing

-- Sample Project 1: Welcome Tutorial
INSERT OR REPLACE INTO Projects (
    Id,
    Title,
    Description,
    Status,
    Theme,
    DurationSeconds,
    CreatedAt,
    UpdatedAt,
    IsDeleted,
    Version,
    Metadata
) VALUES (
    '10000000-0000-0000-0000-000000000001',
    'Welcome to Aura - Sample Project',
    'This is a sample project to help you get started with Aura Video Studio. It demonstrates a simple video workflow with script, voiceover, and visuals.',
    0, -- Draft status
    'Technology',
    60,
    datetime('now', '-5 days'),
    datetime('now', '-5 days'),
    0,
    1,
    '{"tags":["sample","tutorial","getting-started"],"difficulty":"beginner"}'
);

-- Sample Project 2: Quick Start Tutorial
INSERT OR REPLACE INTO Projects (
    Id,
    Title,
    Description,
    Status,
    Theme,
    DurationSeconds,
    CreatedAt,
    UpdatedAt,
    IsDeleted,
    Version,
    Metadata
) VALUES (
    '10000000-0000-0000-0000-000000000002',
    'Quick Start Tutorial',
    'Learn how to create your first video in Aura using the Guided Mode. This tutorial covers the basics of script generation, voice selection, and rendering.',
    0, -- Draft status
    'Education',
    90,
    datetime('now', '-3 days'),
    datetime('now', '-2 days'),
    0,
    1,
    '{"tags":["tutorial","beginner","guided-mode"],"difficulty":"beginner"}'
);

-- Sample Project 3: Advanced Features Demo
INSERT OR REPLACE INTO Projects (
    Id,
    Title,
    Description,
    Status,
    Theme,
    DurationSeconds,
    CreatedAt,
    UpdatedAt,
    IsDeleted,
    Version,
    Metadata
) VALUES (
    '10000000-0000-0000-0000-000000000003',
    'Advanced Features Demo',
    'Explore advanced features like ML Lab, custom prompt templates, and expert render controls. Perfect for power users.',
    0, -- Draft status
    'Technology',
    120,
    datetime('now', '-1 day'),
    datetime('now', '-12 hours'),
    0,
    1,
    '{"tags":["advanced","ml-lab","power-user"],"difficulty":"advanced","advancedMode":true}'
);

-- Sample Wizard Project (in progress)
INSERT OR REPLACE INTO WizardProjects (
    Id,
    Title,
    Topic,
    Duration,
    Status,
    CurrentStep,
    CompletedSteps,
    CreatedAt,
    UpdatedAt,
    IsDeleted
) VALUES (
    '20000000-0000-0000-0000-000000000001',
    'My First Video',
    'Introduction to Video Creation',
    60,
    'InProgress',
    2,
    'topic,duration',
    datetime('now', '-1 hour'),
    datetime('now', '-30 minutes'),
    0
);

-- Log sample actions for demonstration
INSERT OR REPLACE INTO ActionLog (
    Id,
    ProjectId,
    Action,
    Category,
    Details,
    Timestamp,
    IsDeleted
) VALUES 
(
    '30000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'ProjectCreated',
    'Project',
    'Sample project created for tutorial purposes',
    datetime('now', '-5 days'),
    0
),
(
    '30000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000002',
    'ProjectCreated',
    'Project',
    'Quick start tutorial project created',
    datetime('now', '-3 days'),
    0
),
(
    '30000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000003',
    'ProjectCreated',
    'Project',
    'Advanced demo project created',
    datetime('now', '-1 day'),
    0
);
