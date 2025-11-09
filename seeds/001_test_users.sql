-- Seed: 001_test_users
-- Description: Create test user setup for local development
-- This seed ensures the first-run wizard is marked as completed

-- Insert default user setup (wizard completed)
INSERT OR REPLACE INTO UserSetup (
    Id,
    WizardCompleted,
    FirstRunDate,
    Version,
    IsDeleted,
    CreatedAt,
    UpdatedAt
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    1,
    datetime('now', '-7 days'),
    '1.0.0',
    0,
    datetime('now', '-7 days'),
    datetime('now', '-7 days')
);

-- Insert system configuration for development
INSERT OR REPLACE INTO SystemConfiguration (
    Id,
    Key,
    Value,
    Description,
    IsDeleted,
    CreatedAt,
    UpdatedAt
) VALUES 
(
    '00000000-0000-0000-0001-000000000001',
    'environment',
    'development',
    'Current environment',
    0,
    datetime('now'),
    datetime('now')
),
(
    '00000000-0000-0000-0001-000000000002',
    'seeded',
    'true',
    'Indicates database has been seeded',
    0,
    datetime('now'),
    datetime('now')
),
(
    '00000000-0000-0000-0001-000000000003',
    'seed_version',
    '001',
    'Current seed data version',
    0,
    datetime('now'),
    datetime('now')
);
