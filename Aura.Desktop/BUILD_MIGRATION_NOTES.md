# Database Migration in Build Process

## Overview

The build scripts (`build-desktop.ps1` and `build-desktop.sh`) include an optional database migration step (Step 2b) that attempts to apply Entity Framework Core migrations during build time.

## Important Notes

### Auto-Migration at Runtime

The application **automatically applies database migrations** when it starts up. This is handled in `Aura.Api/Program.cs` via `Database.MigrateAsync()`. Therefore, build-time migration is:

- **Optional**: The application will work correctly even if build-time migration fails
- **Helpful**: Can catch database issues earlier in the development workflow
- **Not Critical**: Failures during build-time migration do not prevent the application from running

### Build-Time Migration Behavior

**When it succeeds:**
```
[INFO] Applying database migrations...
[INFO] Entity Framework tools already installed, attempting update...
[SUCCESS]   ✓ Entity Framework tools updated
[INFO] Checking for pending migrations...
[SUCCESS]   ✓ Database migrations applied successfully
```

**When it fails (common and expected):**
```
[INFO] Applying database migrations...
[INFO] Installing Entity Framework tools...
[SUCCESS]   ✓ Entity Framework tools installed
[INFO] Checking for pending migrations...
[WARNING]   Could not apply migrations during build (will be applied on first app start)
  Migration output: [actual error message]
```

### Common Failure Scenarios

Build-time migrations may fail for several legitimate reasons:

1. **Cross-platform builds**: Building for Windows on Linux/macOS (or vice versa)
   - The published app is configured for a different runtime than the build machine
   - Error: "library 'libhostpolicy.so' required to execute the application was not found"

2. **Missing dependencies**: Project not restored yet
   - Error: "Assets file 'obj/project.assets.json' not found"

3. **Permission issues**: Database file or directory not writable

4. **Connection string issues**: Database connection not configured for build environment

### What Changed (This PR)

**Before:**
- Error output was suppressed (`2>&1 | Out-Null` in PowerShell)
- Users saw generic warning: "Could not install dotnet-ef tools"
- No actual error details visible
- Bash script didn't have this step at all

**After:**
- Actual error messages are captured and displayed
- Clear messaging that migrations will run automatically at app start
- Both PowerShell and Bash scripts have consistent behavior
- Better user experience when diagnosing issues

## When to Worry

**Don't worry if:**
- ✅ Build-time migration shows a warning but build completes successfully
- ✅ You're building for Windows on a Linux/macOS machine (or vice versa)
- ✅ The warning message clearly indicates the issue

**Do investigate if:**
- ❌ The application fails to start and reports migration errors
- ❌ You're building on the same platform as your target and migrations still fail
- ❌ Migration errors persist after `dotnet restore`

## Testing

To test the migration step independently:

**PowerShell:**
```powershell
cd Aura.Desktop
.\build-desktop.ps1 -SkipFrontend -SkipInstaller
```

**Bash:**
```bash
cd Aura.Desktop
./build-desktop.sh --skip-frontend --skip-installer
```

Watch for Step 2b output in the build logs.

## Troubleshooting

If you encounter persistent migration issues:

1. Ensure .NET 8 SDK is installed: `dotnet --version`
2. Restore project dependencies: `cd Aura.Api && dotnet restore`
3. Manually install EF tools: `dotnet tool install --global dotnet-ef`
4. Test migrations manually: `cd Aura.Api && dotnet ef database update`
5. Check `appsettings.json` for correct connection string

## Reference

- Application auto-migration code: `Aura.Api/Program.cs`
- Migration files: `Aura.Api/Migrations/`
- Database context: `Aura.Core/Data/AuraDbContext.cs`
