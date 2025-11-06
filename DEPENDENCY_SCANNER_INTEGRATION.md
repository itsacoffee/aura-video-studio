# Dependency Scanner Integration Guide

## Overview

The new Dependency Scanner provides a unified, comprehensive system validation with SSE progress streaming and actionable remediation steps.

## Backend API

### Endpoints

#### POST /api/system/scan
Immediate JSON scan with optional caching.

**Query Parameters:**
- `forceRefresh` (boolean, optional): Force a new scan even if cached result exists

**Response:**
```json
{
  "scanTime": "2024-01-15T10:30:00Z",
  "duration": "00:00:05",
  "systemInfo": {
    "platform": "Windows",
    "architecture": "X64",
    "osVersion": "Windows 11",
    "cpuCores": 8,
    "totalMemoryMb": 16384,
    "gpu": {
      "vendor": "NVIDIA",
      "model": "GeForce RTX 3060",
      "vramMb": 12288,
      "supportsHardwareAcceleration": true
    }
  },
  "issues": [
    {
      "id": "ffmpeg-missing",
      "category": "FFmpeg",
      "severity": "Error",
      "title": "FFmpeg Not Found",
      "description": "FFmpeg is required for video rendering but was not found on your system.",
      "remediation": "Install FFmpeg using the 'Install Managed FFmpeg' button or attach an existing installation.",
      "actionId": "install-ffmpeg",
      "docsUrl": "https://docs.aura.studio/dependencies/ffmpeg"
    }
  ],
  "success": true,
  "hasErrors": true,
  "hasWarnings": false,
  "correlationId": "abc-123"
}
```

#### GET /api/system/scan/stream
SSE stream with real-time progress.

**Query Parameters:**
- `forceRefresh` (boolean, optional): Force a new scan

**Events:**
- `started`: Scan started
- `step`: Progress update
- `issue`: Issue discovered
- `completed`: Scan completed

**Event Data:**
```
event: step
data: {"message":"Checking FFmpeg installation","percentComplete":25}

event: issue
data: {"issue":{"id":"ffmpeg-missing","title":"FFmpeg Not Found",...}}

event: completed
data: {"scanTime":"...","duration":"...","issueCount":1,"hasErrors":true}
```

## Frontend Component

### DependencyScanner Component

Location: `Aura.Web/src/components/System/DependencyScanner.tsx`

#### Props

```typescript
interface DependencyScannerProps {
  autoScan?: boolean;          // Auto-start scanning on mount
  onScanComplete?: (result: DependencyScanResult) => void;
  onFixAction?: (actionId: string, issue: DependencyIssue) => Promise<void>;
  showRescanButton?: boolean;  // Show rescan button
}
```

#### Basic Usage

```tsx
import { DependencyScanner } from '../components/System/DependencyScanner';

function OnboardingStep8() {
  const handleScanComplete = (result) => {
    console.log('Scan complete:', result);
    if (!result.hasErrors) {
      // Allow user to proceed
    }
  };
  
  const handleFixAction = async (actionId, issue) => {
    switch (actionId) {
      case 'install-ffmpeg':
        await installFFmpeg();
        break;
      case 'install-ollama':
        await installOllama();
        break;
    }
  };
  
  return (
    <DependencyScanner
      autoScan={true}
      onScanComplete={handleScanComplete}
      onFixAction={handleFixAction}
      showRescanButton={true}
    />
  );
}
```

### Service Layer

Location: `Aura.Web/src/services/dependencyScanService.ts`

```typescript
import { scanDependencies, scanDependenciesStream, getCachedScan } from '../services/dependencyScanService';

// Immediate scan
const result = await scanDependencies(forceRefresh);

// Stream with progress
const es = scanDependenciesStream(
  forceRefresh,
  (progress) => console.log('Progress:', progress),
  (complete) => console.log('Complete:', complete),
  (error) => console.error('Error:', error)
);

// Get cached result
const cached = await getCachedScan();
```

## Integration Points

### 1. Onboarding Step 8

**Current State:** Uses old preflight check API showing "Validation Failed" with empty list.

**New Implementation:**
```tsx
// In FirstRunWizard.tsx, update renderStep7 or renderStep8:
const renderStep8 = () => {
  const handleScanComplete = (result: DependencyScanResult) => {
    if (result.hasErrors) {
      dispatch({ type: 'VALIDATION_FAILED' });
    } else {
      dispatch({ type: 'VALIDATION_SUCCESS' });
    }
  };
  
  const handleFixFFmpeg = async () => {
    await fetch('/api/dependencies/ffmpeg/install', { method: 'POST' });
  };
  
  const handleFixAction = async (actionId: string) => {
    switch (actionId) {
      case 'install-ffmpeg':
        await handleFixFFmpeg();
        break;
    }
  };
  
  return (
    <DependencyScanner
      autoScan={true}
      onScanComplete={handleScanComplete}
      onFixAction={handleFixAction}
    />
  );
};
```

### 2. Settings Page - Dependencies Tab

Add a "System Validation" section:

```tsx
import { DependencyScanner } from '../components/System/DependencyScanner';

function DependenciesSettingsTab() {
  return (
    <div>
      <Title2>System Validation</Title2>
      <DependencyScanner showRescanButton={true} />
      
      {/* Existing dependency management UI */}
    </div>
  );
}
```

### 3. Preflight Before Generation

Before starting video generation, check dependencies:

```tsx
async function preflightCheck() {
  const result = await scanDependencies(false); // Use cache
  
  if (result.hasErrors) {
    // Show error dialog with issues
    showDialog({
      title: 'System Validation Failed',
      content: <DependencyScanner autoScan={false} />,
    });
    return false;
  }
  
  return true;
}

async function startGeneration() {
  if (!(await preflightCheck())) {
    return;
  }
  
  // Proceed with generation
}
```

## Issue Categories

- **FFmpeg**: FFmpeg installation and version issues
- **Network**: Internet connectivity and provider reachability
- **Provider**: API key and provider availability issues
- **Storage**: Disk space and write permissions
- **System**: General system requirements
- **Runtime**: Runtime environment issues

## Severity Levels

- **Error**: Blocks functionality, must be fixed
- **Warning**: Doesn't block but should be addressed
- **Info**: Informational, no action required

## Fix Actions

Common fix actions that can be implemented:

| Action ID | Description | Implementation |
|-----------|-------------|----------------|
| `install-ffmpeg` | Install managed FFmpeg | POST `/api/dependencies/ffmpeg/install` |
| `update-ffmpeg` | Update FFmpeg version | POST `/api/dependencies/ffmpeg/install` |
| `install-ollama` | Install Ollama | Open download page |
| `add-api-key` | Add missing API key | Navigate to Settings > API Keys |

## Caching

- Results are cached for 5 minutes
- Use `forceRefresh=true` to bypass cache
- Cache is cleared on fix actions
- Cache endpoint: GET `/api/system/scan/cached`

## Error Handling

All endpoints return ProblemDetails on error:

```json
{
  "title": "Dependency Scan Failed",
  "detail": "An error occurred while scanning system dependencies",
  "status": 500,
  "correlationId": "abc-123",
  "timestamp": "2024-01-15T10:30:00Z",
  "errorMessage": "...",
  "errorType": "Exception"
}
```

## Testing

### Unit Tests

```typescript
describe('DependencyScanner', () => {
  it('displays issues from scan result', async () => {
    // Test issue display
  });
  
  it('calls onFixAction when fix button clicked', async () => {
    // Test fix action callback
  });
  
  it('shows progress during SSE scan', async () => {
    // Test progress display
  });
});
```

### E2E Tests

```typescript
test('Onboarding Step 8 shows validation issues', async ({ page }) => {
  await page.goto('/onboarding');
  // Navigate to step 8
  // Verify issues are displayed
  // Click fix button
  // Verify rescan occurs
});
```
