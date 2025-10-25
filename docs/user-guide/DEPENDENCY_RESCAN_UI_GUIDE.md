# Universal Dependency Rescan Feature - Visual Guide

## UI Screenshots and Mockups

### Download Center - Dependencies Tab

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Download Center                                                          │
│ Manage dependencies, engines, and external tools                        │
├─────────────────────────────────────────────────────────────────────────┤
│ [Dependencies] [Engines]                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ Download & Manage Dependencies                                      │ │
│ │                                                                       │ │
│ │ This page helps you download and manage all required components...  │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ Dependency Scanner                    [🔄 Rescan All Dependencies]  │ │
│ │ Last scan: 10/12/2025, 8:53:12 PM                                   │ │
│ │                                                                       │ │
│ │ Found 1 of 5 dependencies installed                                 │ │
│ │                                                                       │ │
│ │ ┌─────────────────────────────────────────────────────────────────┐ │ │
│ │ │ Dependency       │ Status          │ Path / Details  │ Actions  │ │ │
│ │ ├─────────────────────────────────────────────────────────────────┤ │ │
│ │ │ FFmpeg          │ ✅ Installed    │ /usr/bin/ffmpeg │          │ │ │
│ │ │ (System PATH)   │                 │ 4.4.2           │          │ │ │
│ │ ├─────────────────────────────────────────────────────────────────┤ │ │
│ │ │ Ollama          │ ❌ Missing      │ Cannot connect  │ [Install]│ │ │
│ │ │                 │                 │ to Ollama       │ [Attach] │ │ │
│ │ ├─────────────────────────────────────────────────────────────────┤ │ │
│ │ │ Stable          │ ❌ Missing      │ Cannot connect  │ [Install]│ │ │
│ │ │ Diffusion WebUI │                 │ to SD WebUI     │ [Attach] │ │ │
│ │ ├─────────────────────────────────────────────────────────────────┤ │ │
│ │ │ Piper TTS       │ ❌ Missing      │ Piper executable│ [Install]│ │ │
│ │ │                 │                 │ not found       │ [Attach] │ │ │
│ │ ├─────────────────────────────────────────────────────────────────┤ │ │
│ │ │ FFprobe         │ ⚠️  Partially  │ ffprobe not     │ [Repair] │ │ │
│ │ │ (Bundled)       │    Installed    │ found at path   │          │ │ │
│ │ └─────────────────────────────────────────────────────────────────┘ │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

### Status Badge Colors

- **Installed** (✅): Green badge with checkmark icon
  - Indicates dependency is found and validated
  - Shows path and version information
  - No action buttons needed

- **Missing** (❌): Red badge with error icon
  - Indicates dependency is not found
  - Shows error message explaining why
  - Action buttons: Install, Attach Existing

- **Partially Installed** (⚠️): Yellow/Orange badge with warning icon
  - Indicates dependency is partially present but not fully functional
  - Shows specific issue (e.g., "ffprobe not found")
  - Action button: Repair

### Settings Page - Local Engines Tab

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Settings                                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│ [System] [UI] [Providers] [Local Providers] [Local Engines] [API Keys]  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ Local Engines                                                        │ │
│ │                                                                       │ │
│ │ Manage local AI engines (Stable Diffusion, ComfyUI, Piper, Mimic3)  │ │
│ │ with automatic installation and configuration.                       │ │
│ │                                                                       │ │
│ │ [Engine management interface...]                                     │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ Dependency Scanner                    [🔄 Rescan All Dependencies]  │ │
│ │ Last scan: 10/12/2025, 8:53:12 PM                                   │ │
│ │                                                                       │ │
│ │ [Dependency status table as above...]                               │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

## User Workflows

### Workflow 1: Initial Setup Check
1. User navigates to Download Center
2. Sees RescanPanel with "No scan performed yet" message
3. Clicks "Rescan All Dependencies" button
4. Button shows loading spinner
5. Table populates with dependency status
6. User sees which dependencies need to be installed
7. Clicks Install/Attach buttons as needed

### Workflow 2: After Manual Installation
1. User manually copies FFmpeg to dependencies folder
2. Returns to Download Center
3. Clicks "Rescan All Dependencies"
4. System detects FFmpeg in new location
5. FFmpeg status changes from Missing to Installed
6. Path and version displayed

### Workflow 3: Troubleshooting
1. User has issues with video rendering
2. Goes to Settings > Local Engines
3. Sees RescanPanel at bottom
4. Clicks Rescan to check all dependencies
5. Identifies Piper TTS is partially installed
6. Clicks Repair button to fix

### Workflow 4: Regular Maintenance
1. User periodically checks Download Center
2. Reviews last scan time
3. If old, clicks Rescan to refresh status
4. Verifies all dependencies still working
5. Updates as needed

## Interaction Details

### Rescan Button Behavior
- **Normal state**: Blue primary button with sync icon
- **Loading state**: Button disabled, shows spinner, text changes to "Scanning..."
- **After scan**: Button re-enables, table updates with fresh data

### Status Table Features
- **Sortable columns**: Click headers to sort by name, status, etc.
- **Expandable rows** (future): Click row to see detailed validation output
- **Color coding**: Visual indicators for quick status assessment
- **Monospace paths**: File paths displayed in monospace font for clarity
- **Error messages**: Red text for error details

### Action Buttons
- **Install**: Opens installer for that component
- **Attach Existing**: Opens dialog to browse for existing installation
- **Repair**: Attempts to fix partial installation
- All buttons respect processing state (disable during operations)

## API Response Examples

### Successful Scan
```json
{
  "success": true,
  "scanTime": "2025-10-12T20:53:12.623Z",
  "dependencies": [
    {
      "id": "ffmpeg",
      "displayName": "FFmpeg",
      "status": "Installed",
      "path": "/usr/bin/ffmpeg",
      "validationOutput": "4.4.2",
      "provenance": "System PATH"
    },
    {
      "id": "python",
      "displayName": "Python",
      "status": "Installed",
      "path": "python3",
      "validationOutput": "Python 3.10.12",
      "provenance": "System PATH"
    }
  ]
}
```

### Dependency Not Found
```json
{
  "id": "ollama",
  "displayName": "Ollama",
  "status": "Missing",
  "errorMessage": "Cannot connect to Ollama: Connection refused (127.0.0.1:11434)"
}
```

### Partial Installation
```json
{
  "id": "ffprobe",
  "displayName": "FFprobe",
  "status": "PartiallyInstalled",
  "errorMessage": "FFprobe not found at /usr/bin/ffprobe"
}
```

## Accessibility Features
- All status badges have ARIA labels
- Keyboard navigation supported for all buttons
- Screen reader friendly table structure
- Color is not the only indicator (icons + text)
- Focus indicators for all interactive elements

## Performance Considerations
- Scans run asynchronously to avoid blocking UI
- Each dependency check has 5-second timeout
- Results cached and displayed immediately on subsequent views
- Refresh is quick if dependencies haven't changed
- Progress indicators for long-running scans

## Error Handling
- Network errors caught and displayed
- Invalid responses handled gracefully
- Timeout errors show specific message
- General errors show user-friendly message
- Console logs detailed errors for debugging
