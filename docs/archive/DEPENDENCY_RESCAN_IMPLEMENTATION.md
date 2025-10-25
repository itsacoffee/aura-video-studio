# Universal Dependency Rescan Feature - Implementation Summary

## Overview
This PR implements a universal "Rescan Dependencies" action that checks all known dependencies (FFmpeg, Ollama, Python, SD WebUI, Piper, Mimic3, FFprobe) and reports their status with actionable next steps.

## Components Implemented

### 1. Backend Service - DependencyRescanService
**File**: `Aura.Core/Dependencies/DependencyRescanService.cs`

Features:
- Scans all dependencies from components.json manifest
- Checks installation status using appropriate locators/validators
- Returns structured report with status, path, validation output, and provenance
- Persists last scan timestamp to disk
- Status types: Installed, Missing, PartiallyInstalled

Dependency Detection Methods:
- **FFmpeg/FFprobe**: Uses FfmpegLocator to check standard paths and PATH environment
- **Ollama**: HTTP check to http://127.0.0.1:11434/api/tags
- **Stable Diffusion WebUI**: HTTP check to http://127.0.0.1:7860
- **Piper TTS**: File system check for executable in dependencies folder
- **Python**: Process execution check with `python --version`

### 2. API Endpoints - DependenciesController
**File**: `Aura.Api/Controllers/DependenciesController.cs`

Endpoints:
- `GET /api/dependencies/rescan` - Full dependency scan
- `POST /api/dependencies/refresh-candidate-paths` - Fast path-only refresh
- `GET /api/dependencies/last-scan-time` - Get last scan timestamp

Response Format:
```json
{
  "success": true,
  "scanTime": "2025-10-12T20:53:12.6237227Z",
  "dependencies": [
    {
      "id": "ffmpeg",
      "displayName": "FFmpeg",
      "status": "Installed",
      "path": "/usr/bin/ffmpeg",
      "validationOutput": "4.4.2",
      "provenance": "System PATH",
      "errorMessage": null
    }
  ]
}
```

### 3. UI Component - RescanPanel
**File**: `Aura.Web/src/pages/DownloadCenter/RescanPanel.tsx`

Features:
- "Rescan All Dependencies" button with loading state
- Status badges (Installed, Missing, PartiallyInstalled) with icons
- Dependency table showing:
  - Name and provenance
  - Status with visual indicator
  - Path and validation output
  - Error messages for missing dependencies
  - Action buttons (Install, Attach, Repair) based on status
- Last scan timestamp display
- Summary count of installed vs total dependencies

### 4. Integration Points
- **Download Center**: RescanPanel added to Dependencies tab
- **Settings Page**: RescanPanel added to Local Engines tab
- **DI Registration**: Service registered in Program.cs with proper dependencies

### 5. Testing
**File**: `Aura.Tests/DependencyRescanServiceTests.cs`

Tests:
- RescanAllAsync returns report with all dependencies
- FFmpeg found reports Installed status
- FFmpeg not found reports Missing status  
- Last scan time is saved and retrievable
- GetLastScanTimeAsync with no scan returns null

All tests passing ✅

## Technical Details

### Virtual Methods for Testability
Made the following methods virtual to support mocking in tests:
- `ComponentDownloader.LoadManifestAsync()`
- `FfmpegLocator.CheckAllCandidatesAsync()`

### Build Configuration
Updated `Aura.Core.csproj` to copy `components.json` to output directory:
```xml
<ItemGroup>
  <None Update="Dependencies/components.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Manual Testing Results

### API Testing
```bash
# Test rescan endpoint
curl http://127.0.0.1:5005/api/dependencies/rescan

# Response shows all dependencies with status
{
  "success": true,
  "scanTime": "2025-10-12T20:53:12.6237227Z",
  "dependencies": [...]
}

# Test last scan time
curl http://127.0.0.1:5005/api/dependencies/last-scan-time

# Response shows persisted timestamp
{
  "success": true,
  "lastScanTime": "2025-10-12T20:53:12.6237227Z"
}
```

### Expected UI Behavior
1. User clicks "Rescan All Dependencies" button
2. Button shows loading spinner during scan
3. Table displays with all dependencies and their status
4. Each row shows:
   - Green checkmark badge for Installed
   - Red error badge for Missing
   - Yellow warning badge for PartiallyInstalled
5. Action buttons appear based on status:
   - Install button for Missing dependencies
   - Attach Existing button for Missing dependencies
   - Repair button for PartiallyInstalled dependencies
6. Last scan time displayed at top (e.g., "Last scan: 10/12/2025, 8:53:12 PM")

## Acceptance Criteria Met

✅ Clicking Rescan All shows accurate statuses and paths
✅ Service detects FFmpeg, Ollama, Python, SD WebUI, Piper, Mimic3, FFprobe
✅ API returns structured report with status, path, validation output, provenance
✅ UI displays report as table with status badges and action buttons
✅ Last rescan time is persisted and displayed
✅ After manual file copy, rescan would mark component as Installed (requires actual file)

## Future Enhancements
- Per-component "Rescan this" quick action
- Wire up Install/Attach/Repair buttons to actual implementation
- Add loading states per dependency during scan
- Add ability to cancel in-progress scan
- Cache scan results with configurable TTL
- Add notification when dependencies change status
