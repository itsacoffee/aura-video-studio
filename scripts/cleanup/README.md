# Cleanup Scripts

This directory contains scripts to enforce the portable-only distribution policy for Aura Video Studio.

## Distribution Policy

**Aura Video Studio follows a portable-only distribution model:**
- ✅ Portable ZIP - Only supported distribution format
- ❌ MSIX/APPX packages - Not supported
- ❌ Traditional installers (EXE/Inno Setup) - Not supported

## Scripts

### `portable_only_cleanup.ps1` / `.sh`

Removes all MSIX/EXE packaging files from the repository.

**Usage (PowerShell)**:
```powershell
# Dry run (preview what would be deleted)
.\scripts\cleanup\portable_only_cleanup.ps1 -DryRun

# Actually delete files
.\scripts\cleanup\portable_only_cleanup.ps1
```

**Usage (Bash)**:
```bash
# Dry run (preview what would be deleted)
./scripts/cleanup/portable_only_cleanup.sh --dry-run

# Actually delete files
./scripts/cleanup/portable_only_cleanup.sh
```

**What it deletes**:
- `*.iss` (Inno Setup scripts)
- `*.appx*`, `*.msix*`, `*.msixbundle` (MSIX/APPX files)
- `*.cer` (certificate files)
- Files/directories matching `*msix*`, `*inno*`, `*setup*`, `*installer*` in `scripts/packaging/`

### `ci_guard.sh`

CI guard that fails the build if MSIX/EXE packaging patterns are detected.

**Usage**:
```bash
./scripts/cleanup/ci_guard.sh
```

**What it checks**:
- Forbidden file patterns (*.iss, *.msix*, *.appx*, etc.)
- Forbidden directory patterns in `scripts/packaging/`
- MSIX/EXE references in GitHub workflows (`.github/workflows/*.yml`)

**Exit codes**:
- `0` - No violations found (CI passes)
- `1` - Violations found (CI fails)

## CI Integration

The CI guard is automatically run in all GitHub Actions workflows:
- `.github/workflows/ci.yml`
- `.github/workflows/ci-linux.yml`
- `.github/workflows/ci-windows.yml`

This ensures that MSIX/EXE packaging infrastructure cannot accidentally be reintroduced into the repository.

## Why Portable-Only?

The portable-only policy provides several benefits:
- **No installation required** - Extract and run
- **No administrator privileges needed** - Runs from any directory
- **No system changes** - No registry modifications or system files
- **Maximum compatibility** - Works on any Windows system
- **Easy distribution** - Single ZIP file
- **Easy updates** - Replace files in place

## Troubleshooting

If the CI guard fails:
1. Review the violation report to see which files or patterns were detected
2. Run the cleanup script to remove forbidden files: `./scripts/cleanup/portable_only_cleanup.sh`
3. Remove any MSIX/EXE references from workflow files
4. Commit and push the changes

## Manual Cleanup

If you need to manually clean up:
```bash
# Remove Inno Setup files
find . -name "*.iss" -delete

# Remove MSIX/APPX manifests
find . -name "*.appxmanifest" -delete
find . -name "*.appx*" -delete
find . -name "*.msix*" -delete

# Remove certificate files
find . -name "*.cer" -delete
```
