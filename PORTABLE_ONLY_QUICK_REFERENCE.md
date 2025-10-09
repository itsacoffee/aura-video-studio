# Quick Reference - Portable-Only Policy

## For Users

### Building the Portable Distribution
```powershell
# Windows only
.\scripts\packaging\build-portable.ps1
```

Output: `artifacts/windows/portable/AuraVideoStudio_Portable_x64.zip`

### Running Aura Video Studio
1. Extract the ZIP file to any location
2. Run `Launch.bat`
3. Browser opens automatically to http://127.0.0.1:5005

No installation, no admin rights, no system changes required!

## For Developers

### Checking Policy Compliance
```bash
# Run CI guard to check for violations
./scripts/cleanup/ci_guard.sh
```

Exit code 0 = clean, Exit code 1 = violations found

### Cleaning Up Forbidden Files
```bash
# Preview what would be deleted
./scripts/cleanup/portable_only_cleanup.sh --dry-run

# Actually delete forbidden files
./scripts/cleanup/portable_only_cleanup.sh
```

### What's Forbidden?

**File Patterns:**
- `*.iss` (Inno Setup)
- `*.msix*` (MSIX packages)
- `*.appx*` (APPX packages)
- `*.msixbundle`
- `*.cer` (certificates)

**Directory Patterns (in scripts/packaging/):**
- `*msix*`
- `*inno*`
- `*setup*`
- `*installer*`

**Workflow References:**
- MSIX/APPX build steps
- Inno Setup compilation
- MSBuild with UapAppx parameters

## For CI/CD

The CI guard runs automatically in all workflows:
- `.github/workflows/ci.yml`
- `.github/workflows/ci-linux.yml`
- `.github/workflows/ci-windows.yml`

If forbidden patterns are detected, the build fails with a detailed report.

## Distribution Model

✅ **Portable ZIP Only**
- Self-contained
- No installation
- No admin rights
- No system changes
- Works anywhere

❌ **Not Supported**
- MSIX/APPX packages
- EXE installers
- Windows Store

## Need Help?

- **Full Documentation**: See `scripts/cleanup/README.md`
- **Implementation Details**: See `PORTABLE_ONLY_CLEANUP_SUMMARY.md`
- **Build Instructions**: See `scripts/packaging/README.md`
- **Architecture**: See `ARCHITECTURE.md`

## Common Questions

**Q: Why portable-only?**  
A: Maximum compatibility, no installation overhead, works from any location.

**Q: Can I create an installer?**  
A: No, the policy prohibits installers. Use the portable ZIP.

**Q: What if I need MSIX?**  
A: The portable-only policy is enforced by CI guards. MSIX is not supported.

**Q: How do I distribute the app?**  
A: Share the portable ZIP file. Users extract and run.

**Q: Can I run from a USB drive?**  
A: Yes! That's one of the benefits of portable distribution.
