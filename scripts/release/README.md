# Release Scripts

This directory contains scripts for managing releases of Aura Video Studio.

## Scripts

### `update-version.js`

Updates the version number in `version.json`.

**Usage:**

```bash
# Increment patch version (1.0.0 → 1.0.1)
node scripts/release/update-version.js patch

# Increment minor version (1.0.0 → 1.1.0)
node scripts/release/update-version.js minor

# Increment major version (1.0.0 → 2.0.0)
node scripts/release/update-version.js major

# Set explicit version
node scripts/release/update-version.js 1.2.3
```

**What it does:**
- Updates `version` field in `version.json`
- Updates `semanticVersion` and `informationalVersion` to match
- Sets `buildDate` to current date (YYYY-MM-DD)
- Validates version format (X.Y.Z)

### `generate-release-notes.js`

Generates release notes from conventional commits between two git references.

**Usage:**

```bash
# Generate notes between two tags
node scripts/release/generate-release-notes.js v1.0.0 v1.1.0

# Generate notes from last tag to HEAD
node scripts/release/generate-release-notes.js v1.0.0 HEAD

# Generate notes for recent commits (no tag specified)
node scripts/release/generate-release-notes.js
```

**What it does:**
- Parses git commits using conventional commit format
- Categorizes commits into sections:
  - ⚠️ BREAKING CHANGES
  - 🚀 Features
  - 🐛 Bug Fixes
  - ⚡ Performance Improvements
  - 📚 Documentation
  - ♻️ Code Refactoring
- Generates statistics (total commits, contributors, counts by type)
- Creates `RELEASE_NOTES.md` with formatted output

**Conventional Commit Format:**

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Supported types:**
- `feat`: New feature
- `fix`: Bug fix
- `perf`: Performance improvement
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Test changes
- `chore`: Build/tooling changes
- `build`: Build system changes
- `ci`: CI/CD changes
- `revert`: Revert previous commit

**Breaking changes:**
- Add `!` after type: `feat!: Remove old API`
- Or include `BREAKING CHANGE:` in footer

**Examples:**

```
feat(api): Add version endpoint

Add GET /api/version endpoint that returns application version information
including semantic version, build date, and runtime version.

Closes #123
```

```
fix(ui): Correct version display in footer

The version was not updating after API calls. Fixed by adding proper
state management with useEffect.
```

```
feat!: Remove deprecated /api/v1/old endpoint

BREAKING CHANGE: The /api/v1/old endpoint has been removed. Use /api/v2/new instead.
Migration guide: https://docs.example.com/migration
```

## Workflow Integration

These scripts are used by `.github/workflows/release.yml`:

1. **On tag push** (`v*.*.*`):
   - Validate version format
   - Run security scans (placeholder check, secret scan)
   - Build artifacts (portable ZIP, SBOM, checksums)
   - Run E2E tests (Windows + Linux)
   - Generate release notes
   - Create GitHub Release with artifacts

2. **Manual trigger**:
   - Specify tag version and pre-release flag
   - Same validation and build process

## Local Testing

You can test the release process locally without creating a tag:

```bash
# 1. Update version
node scripts/release/update-version.js 1.2.3

# 2. Generate release notes
node scripts/release/generate-release-notes.js v1.2.2 HEAD

# 3. Review RELEASE_NOTES.md
cat RELEASE_NOTES.md

# 4. Revert if testing
git checkout version.json
rm RELEASE_NOTES.md
```

## Version File Format

`version.json` structure:

```json
{
  "version": "1.0.0",
  "buildDate": "2025-11-06",
  "semanticVersion": "1.0.0",
  "informationalVersion": "1.0.0",
  "description": "AI-Powered Video Generation Suite"
}
```

**Fields:**
- `version`: Semantic version (X.Y.Z)
- `buildDate`: ISO date when version was last updated
- `semanticVersion`: Same as version (for consistency)
- `informationalVersion`: Can include metadata (e.g., `1.0.0+abc123`)
- `description`: Application description

## Best Practices

1. **Commit Messages**
   - Use conventional commit format for all commits
   - Include scope when applicable: `feat(api):`
   - Write clear, descriptive messages
   - Reference issues: `Closes #123`

2. **Versioning**
   - Follow Semantic Versioning (semver.org)
   - Major: Breaking changes
   - Minor: New features (backward compatible)
   - Patch: Bug fixes (backward compatible)

3. **Release Process**
   - Update version first
   - Commit version change: `chore: Bump version to X.Y.Z`
   - Create annotated tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`
   - Push tag: `git push origin vX.Y.Z`
   - Workflow handles the rest

4. **Pre-releases**
   - Use workflow manual trigger with pre-release flag
   - Tag format: `v1.0.0-beta.1`, `v1.0.0-rc.1`
   - Marked as pre-release in GitHub

## Troubleshooting

**Version script fails with "Invalid version format"**
- Ensure version follows X.Y.Z format (three numbers separated by dots)
- No leading zeros: `1.0.0` not `1.00.0`

**Release notes missing commits**
- Check if commits follow conventional format
- Use `git log` to verify commits exist in range
- Ensure `from-tag` exists: `git tag -l`

**Workflow fails at validation**
- Check placeholder scan results
- Check secret scan results
- Ensure version format is valid: `vX.Y.Z`

**Artifacts not generated**
- Verify build scripts exist in `scripts/packaging/`
- Check workflow logs for build errors
- Ensure FFmpeg installed (for full build)

## See Also

- [ReleasePlaybook.md](../../ReleasePlaybook.md) - Complete release deployment guide
- [PRODUCTION_READINESS_CHECKLIST.md](../../PRODUCTION_READINESS_CHECKLIST.md) - Pre-release validation
- [.github/workflows/release.yml](../../.github/workflows/release.yml) - Release workflow definition
