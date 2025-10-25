# Agent 04: Download Center Acceptance Criteria

## Problem Statement Requirements

### Files Created/Modified ✅

1. **manifest.json** ✅
   - Location: `/manifest.json`
   - Contains all downloadable items (FFmpeg, Ollama, SD WebUI, CC0 packs)
   - Each item includes:
     - `sizeBytes`: File size in bytes
     - `sha256`: SHA-256 checksum for verification
     - `url`: Download URL
     - `extractPath`: Where to extract files
     - `postInstallProbe`: Validation method

2. **Aura.Api/Services/DownloadService.cs** ✅
   - Location: `/Aura.Api/Services/DownloadService.cs`
   - Service layer wrapping DependencyManager
   - Methods:
     - `GetManifestAsync()`: Load manifest
     - `IsComponentInstalledAsync()`: Check installation status
     - `InstallComponentAsync()`: Download and install with progress
     - `VerifyComponentAsync()`: Verify integrity
     - `RepairComponentAsync()`: Repair corrupted files
     - `RemoveComponentAsync()`: Remove component
     - `GetComponentDirectory()`: Get install path
     - `GetManualInstructions()`: Offline installation instructions
   - Uses HttpClient with resume support (delegated to DependencyManager)
   - SHA-256 checksum verification (delegated to DependencyManager)
   - Registered in DI container in Program.cs

3. **Aura.Web/src/pages/DownloadCenter.tsx** ✅
   - Location: `/Aura.Web/src/pages/DownloadCenter.tsx`
   - Re-exports DownloadsPage as DownloadCenter
   - Maintains backward compatibility with existing routing
   - Full implementation exists in DownloadsPage.tsx with:
     - Component list with sizes and status
     - Install/Repair/Verify/Remove buttons
     - Progress indicators and ETA
     - Manual installation instructions
     - Error handling and status display

### Implementation Steps ✅

1. **Manifest describing all downloadable items** ✅
   - manifest.json includes FFmpeg, Ollama, OllamaModel, StableDiffusion, StableDiffusionXL, CC0StockPack, CC0MusicPack
   - All items have sizeBytes and sha256 fields

2. **DownloadService with HttpClient, resume, and checksum verification** ✅
   - DownloadService.cs created
   - HttpClient configured in DependencyManager
   - Resume support via HTTP Range requests (in DependencyManager)
   - SHA-256 verification after download (in DependencyManager)

3. **DownloadCenter page tied to manifest items** ✅
   - DownloadCenter.tsx exports DownloadsPage
   - DownloadsPage.tsx fetches manifest from API
   - UI displays all components with status, sizes, and actions

4. **Persist install state under %LOCALAPPDATA%/Aura** ✅
   - DependencyManager configured to use:
     - Manifest: `%LOCALAPPDATA%/Aura/manifest.json`
     - Downloads: `%LOCALAPPDATA%/Aura/dependencies`
   - Cross-platform compatible (uses `Environment.SpecialFolder.LocalApplicationData`)

### Features ✅

1. **Manifest-driven downloads** ✅
   - All components defined in manifest.json
   - API endpoint: `GET /api/downloads/manifest`

2. **Buttons: Install, Repair, Verify, Remove** ✅
   - Install: `POST /api/downloads/{component}/install`
   - Repair: `POST /api/downloads/{component}/repair`
   - Verify: `GET /api/downloads/{component}/verify`
   - Remove: `DELETE /api/downloads/{component}`

3. **Progress and ETA** ✅
   - DownloadProgress class with BytesDownloaded, TotalBytes, PercentComplete
   - IProgress<DownloadProgress> interface for real-time updates
   - UI displays progress and status

4. **Offline Mode** ✅
   - Manual installation instructions: `GET /api/downloads/{component}/manual`
   - Disables network fetches when in offline mode
   - Provides checksums, URLs, and install paths

5. **SHA-256 verification** ✅
   - All files verified after download
   - Checksum mismatch triggers repair
   - Verification method in DependencyManager.VerifyChecksumAsync()

6. **Resume support** ✅
   - HTTP Range requests used when supported
   - Partial downloads tracked and resumed
   - Implemented in DependencyManager.DownloadFileAsync()

### Tests ✅

**Unit Tests** (11 tests in Aura.Tests/DependencyManagerTests.cs)
- ✅ Manifest loading and default creation
- ✅ Checksum verification (pass/fail)
- ✅ Component installation status
- ✅ Missing and corrupted file detection
- ✅ Component removal
- ✅ Directory path retrieval
- ✅ Manual instructions generation
- ✅ Resume download logic

**E2E Tests** (7 tests in Aura.E2E/DependencyDownloadE2ETests.cs)
- ✅ Manifest-driven component flow
- ✅ Component verification (missing/corrupted)
- ✅ Component lifecycle (verify → install → repair → remove)
- ✅ Manual installation instructions
- ✅ Post-install probe configuration
- ✅ Component directory validation

**Test Results**: All 18 dependency-related tests pass

### Acceptance Criteria ✅

1. ✅ **Users can install FFmpeg and providers from inside the app**
   - Install button available in UI
   - API endpoints functional
   - Downloads to local cache

2. ✅ **Checksums verified**
   - SHA-256 verification after each download
   - Verification API endpoint available
   - Corrupt files detected automatically

3. ✅ **Resume support**
   - HTTP Range requests implemented
   - Partial downloads tracked
   - Downloads continue from last byte

4. ✅ **Repair re-downloads and replaces corrupted files safely**
   - Verify endpoint detects corruption
   - Repair endpoint re-downloads only corrupted files
   - UI shows "Needs Repair" status

5. ✅ **Offline mode support**
   - Manual instructions endpoint available
   - No network operations when offline
   - Checksums provided for manual verification

## Commit and PR

- ✅ Commits made in logical chunks
- ✅ PR branch: `copilot/featdownload-center`
- ✅ Commit message: "feat: add DownloadService and DownloadCenter as requested"

## Summary

All requirements from Agent 04 problem statement have been successfully implemented:

1. ✅ manifest.json with SHA-256 and sizes
2. ✅ DownloadService.cs with resume and checksums
3. ✅ DownloadCenter.tsx with full UI
4. ✅ Persistence under %LOCALAPPDATA%/Aura
5. ✅ All tests passing
6. ✅ Offline mode support
7. ✅ Install, Repair, Verify, Remove functionality

The implementation is complete and production-ready.
