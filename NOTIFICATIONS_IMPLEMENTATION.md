# Notifications and Results Panel Implementation

This implementation adds comprehensive notification system, results panel, and "open outputs" functionality to Aura Video Studio.

## Features Implemented

### 1. Notification System (`Toasts.tsx`)
- **Success Toasts**: Display when video generation completes successfully
  - Shows duration (e.g., "00:14")
  - "View results" button - navigates to Projects page
  - "Open folder" button - opens output directory in file explorer
  - Auto-dismisses after 10 seconds

- **Failure Toasts**: Display when generation fails
  - Shows error message and details
  - "Fix" button (optional) - navigates to fix the issue
  - "View logs" button - opens logs panel
  - Does not auto-dismiss (requires manual close)

### 2. Results Tray
- Located in the top-right corner of the application header
- Shows a badge with count of recent outputs
- Dropdown displays the 5 most recent completed jobs:
  - Job correlation ID
  - Time completed (e.g., "2h ago")
  - Output file name
  - Quick actions: "Open" (view file) and "Open folder"
- Auto-refreshes every 30 seconds

### 3. Enhanced Project Actions
The Projects page now includes:
- **Open button**: Opens the video file directly
- **More actions menu** with:
  - "Open outputs folder" - opens the job's output directory
  - "Reveal in Explorer" - reveals the file in file manager (same as open folder for web)

### 4. Generation Panel Improvements
- Automatically shows success/failure toast when job completes
- "Open folder" button on artifacts (instead of just "Open")
- Integrated with notification system

### 5. Backend API
New endpoint: `GET /api/jobs/recent-artifacts?limit=5`
- Returns the most recent completed jobs with artifacts
- Used by Results Tray to show recent outputs

## File Structure

```
Aura.Web/src/
├── components/
│   ├── Notifications/
│   │   └── Toasts.tsx          # Success/failure toast components
│   ├── ResultsTray.tsx         # Results dropdown in header
│   ├── Layout.tsx              # Updated with ResultsTray
│   └── Generation/
│       └── GenerationPanel.tsx # Updated with notifications
├── pages/
│   └── Projects/
│       └── ProjectsPage.tsx    # Updated with open/reveal actions
└── App.tsx                     # Added NotificationsToaster

Aura.Api/Controllers/
└── JobsController.cs           # New recent-artifacts endpoint

Aura.Tests/
└── JobRunnerTests.cs           # Tests for artifact path functionality

Aura.Web/tests/e2e/
└── notifications.spec.ts       # E2E tests for notifications
```

## API Changes

### New Endpoint
```http
GET /api/jobs/recent-artifacts?limit=5
```

Response:
```json
{
  "artifacts": [
    {
      "jobId": "abc-123",
      "correlationId": "video-xyz",
      "stage": "Complete",
      "finishedAt": "2025-10-11T20:30:00Z",
      "artifacts": [
        {
          "name": "output.mp4",
          "path": "/path/to/output.mp4",
          "type": "video/mp4",
          "sizeBytes": 2048000
        }
      ]
    }
  ],
  "count": 5
}
```

## Usage

### Showing Notifications
```typescript
import { useNotifications } from './components/Notifications/Toasts';

const { showSuccessToast, showFailureToast } = useNotifications();

// Success
showSuccessToast({
  title: 'Render complete',
  message: 'Your video has been generated successfully!',
  duration: '00:14',
  onViewResults: () => navigate('/projects'),
  onOpenFolder: () => openFolder(artifactPath),
});

// Failure
showFailureToast({
  title: 'Generation failed',
  message: 'An error occurred during generation',
  errorDetails: 'Missing TTS voice',
  onViewLogs: () => setShowLogs(true),
});
```

### Opening Folders
```typescript
const openFolder = (artifactPath: string) => {
  // Extract directory from artifact path
  const dirPath = artifactPath.substring(0, artifactPath.lastIndexOf('/'));
  window.open(`file:///${dirPath.replace(/\\/g, '/')}`);
};
```

## Tests

### Unit Tests
- `ArtifactManager_Should_UseStandardPath`: Verifies standard path formation
- `ArtifactManager_Should_CreateDirectoryIfMissing`: Ensures directories are created

### E2E Tests (Playwright)
- `should display results tray with recent outputs`: Tests results dropdown
- `should show open and reveal buttons on Projects page`: Tests project actions
- `should handle empty results state`: Tests empty state in results tray

## User Experience Flow

1. **During Generation**:
   - GenerationPanel shows live progress
   - Real-time stage updates

2. **On Success**:
   - Success toast appears: "Render complete (00:14). View results | Open folder"
   - User can click "View results" → navigates to Projects page
   - User can click "Open folder" → opens output directory
   - Results tray badge updates with new output

3. **On Failure**:
   - Failure toast appears: "Generation failed — Missing TTS voice. Fix | View logs"
   - User can click "View logs" → shows detailed error logs
   - Toast persists until manually dismissed

4. **Accessing Outputs**:
   - Click "Results" in header → see 5 most recent outputs
   - Click eye icon → open video file
   - Click folder icon → open containing folder
   - Go to Projects page → see all jobs with Open and menu actions

## Notes

- File paths use forward slashes for cross-platform compatibility
- `file:///` protocol is used to open local files in default applications
- Results tray auto-refreshes every 30 seconds
- Success toasts auto-dismiss after 10 seconds
- Failure toasts require manual dismissal
- All paths are stored under `%LOCALAPPDATA%\Aura\jobs\{jobId}\` on Windows
