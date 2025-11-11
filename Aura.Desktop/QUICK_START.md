# Quick Start Guide - New Electron Architecture

This guide helps you quickly get started with the new modular Electron architecture.

## Running the Application

### Development Mode

```bash
cd Aura.Desktop
npm install
npm run dev
```

This will:
- Launch Electron with `--dev` flag
- Open DevTools automatically
- Use local backend build
- Enable verbose logging
- Disable auto-updater

### Production Mode

```bash
npm start
```

This will:
- Launch Electron in production mode
- Use strict security policies
- Enable auto-updater
- Use bundled backend (if available)

## Project Structure

```
Aura.Desktop/
├── electron/                      # Main process modules
│   ├── main.js                   # Application entry point
│   ├── preload.js                # Renderer IPC bridge
│   ├── types.d.ts                # TypeScript definitions
│   ├── window-manager.js         # Window lifecycle
│   ├── app-config.js             # Configuration
│   ├── backend-service.js        # Backend management
│   ├── tray-manager.js           # System tray
│   ├── menu-builder.js           # Application menu
│   ├── protocol-handler.js       # aura:// protocol
│   └── ipc-handlers/             # IPC channel handlers
│       ├── config-handler.js
│       ├── system-handler.js
│       ├── video-handler.js
│       └── backend-handler.js
├── package.json                   # Updated to use electron/main.js
└── assets/                        # Icons and resources
```

## Using IPC in React Frontend

### 1. Import Types

```typescript
// In your React component
import type { ElectronAPI } from '../Aura.Desktop/electron/types';

// Or create a global type file:
// src/types/electron.d.ts
declare global {
  interface Window {
    electron: import('../../Aura.Desktop/electron/types').ElectronAPI;
  }
}
```

### 2. Use IPC Methods

```typescript
// Configuration
const theme = await window.electron.config.get('theme', 'dark');
await window.electron.config.set('theme', 'light');

// Dialogs
const folder = await window.electron.dialog.openFolder();
const file = await window.electron.dialog.openFile({
  title: 'Select Video',
  filters: [{ name: 'Videos', extensions: ['mp4', 'avi', 'mov'] }]
});

// App info
const version = await window.electron.app.getVersion();
const paths = await window.electron.app.getPaths();

// Backend
const health = await window.electron.backend.health();
const backendUrl = await window.electron.backend.getUrl();
```

### 3. Listen to Events

```typescript
import { useEffect } from 'react';

function MyComponent() {
  useEffect(() => {
    // Video progress
    const unsubscribe = window.electron.video.onProgress((data) => {
      console.log(`Progress: ${data.progress}%`);
    });
    
    // Cleanup on unmount
    return () => unsubscribe();
  }, []);
  
  return <div>...</div>;
}
```

### 4. Menu Actions

```typescript
useEffect(() => {
  // Handle menu "New Project"
  const unsub = window.electron.menu.onNewProject(() => {
    // Create new project
    navigate('/project/new');
  });
  
  return () => unsub();
}, []);
```

## Adding a New IPC Channel

### Step 1: Create Handler

```javascript
// electron/ipc-handlers/my-feature-handler.js
const { ipcMain } = require('electron');

class MyFeatureHandler {
  constructor(dependencies) {
    this.deps = dependencies;
  }

  register() {
    ipcMain.handle('myFeature:doSomething', async (event, arg1, arg2) => {
      // Validate inputs
      if (!arg1 || typeof arg1 !== 'string') {
        throw new Error('Invalid argument');
      }
      
      // Do something
      const result = await this.deps.service.doThing(arg1, arg2);
      
      return result;
    });
  }
}

module.exports = MyFeatureHandler;
```

### Step 2: Register in Main Process

```javascript
// electron/main.js
const MyFeatureHandler = require('./ipc-handlers/my-feature-handler');

function registerIpcHandlers() {
  // ... existing handlers ...
  
  ipcHandlers.myFeature = new MyFeatureHandler(dependencies);
  ipcHandlers.myFeature.register();
}
```

### Step 3: Add to Preload Whitelist

```javascript
// electron/preload.js
const VALID_CHANNELS = {
  // ... existing channels ...
  MY_FEATURE: ['myFeature:doSomething']
};
```

### Step 4: Expose in Preload API

```javascript
// electron/preload.js
contextBridge.exposeInMainWorld('electron', {
  // ... existing APIs ...
  myFeature: {
    doSomething: (arg1, arg2) => safeInvoke('myFeature:doSomething', arg1, arg2)
  }
});
```

### Step 5: Add TypeScript Types

```typescript
// electron/types.d.ts
export interface ElectronAPI {
  // ... existing APIs ...
  myFeature: {
    doSomething(arg1: string, arg2: number): Promise<any>;
  };
}
```

### Step 6: Use in React

```typescript
const result = await window.electron.myFeature.doSomething('test', 42);
```

## Configuration Management

### Get/Set Config

```typescript
// Simple values
const theme = await window.electron.config.get('theme', 'dark');
await window.electron.config.set('theme', 'light');

// Get all config
const allConfig = await window.electron.config.getAll();

// Reset to defaults
await window.electron.config.reset();
```

### Secure Storage (API Keys)

```typescript
// Store encrypted
await window.electron.config.setSecure('openai_api_key', 'sk-...');

// Retrieve encrypted
const apiKey = await window.electron.config.getSecure('openai_api_key');

// Delete
await window.electron.config.deleteSecure('openai_api_key');
```

### Recent Projects

```typescript
// Add recent project
await window.electron.config.addRecentProject(
  'C:/Users/John/project.aura',
  'My Project'
);

// Get recent projects
const recent = await window.electron.config.getRecentProjects();
// Returns: [{ path: '...', name: '...', timestamp: 123... }]

// Clear recent
await window.electron.config.clearRecentProjects();
```

## Video Generation

```typescript
// Start generation
const generation = await window.electron.video.generate.start({
  script: 'My video script',
  settings: { /* ... */ }
});

// Listen for progress
window.electron.video.onProgress((data) => {
  if (data.generationId === generation.id) {
    setProgress(data.progress);
  }
});

// Pause/Resume/Cancel
await window.electron.video.generate.pause(generation.id);
await window.electron.video.generate.resume(generation.id);
await window.electron.video.generate.cancel(generation.id);

// Check status
const status = await window.electron.video.generate.status(generation.id);
```

## Backend Health Monitoring

```typescript
// One-time health check
const health = await window.electron.backend.health();
// Returns: { status: 'healthy' | 'unhealthy', data?: ..., error?: ... }

// Quick ping
const ping = await window.electron.backend.ping();
// Returns: { success: true, responseTime: 123 }

// Listen for health updates (sent every 30 seconds)
window.electron.backend.onHealthUpdate((status) => {
  if (status.status === 'unhealthy') {
    showError('Backend is offline');
  }
});

// Get backend URL
const url = await window.electron.backend.getUrl();
// Use this for direct API calls if needed
```

## Dialogs

```typescript
// Folder selection
const folder = await window.electron.dialog.openFolder();

// File selection
const file = await window.electron.dialog.openFile({
  title: 'Select Video File',
  filters: [
    { name: 'Videos', extensions: ['mp4', 'avi', 'mov'] },
    { name: 'All Files', extensions: ['*'] }
  ]
});

// Multiple files
const files = await window.electron.dialog.openMultipleFiles({
  filters: [{ name: 'Images', extensions: ['jpg', 'png', 'gif'] }]
});

// Save file
const savePath = await window.electron.dialog.saveFile({
  title: 'Export Video',
  defaultPath: 'my-video.mp4',
  filters: [{ name: 'MP4 Video', extensions: ['mp4'] }]
});

// Message box
const response = await window.electron.dialog.showMessage({
  type: 'question',
  title: 'Confirm',
  message: 'Are you sure?',
  detail: 'This action cannot be undone.',
  buttons: ['Yes', 'No'],
  defaultId: 0,
  cancelId: 1
});
// Returns: 0 (Yes) or 1 (No)

// Error dialog
await window.electron.dialog.showError('Error', 'Something went wrong!');
```

## Shell Operations

```typescript
// Open URL in default browser
await window.electron.shell.openExternal('https://github.com');

// Open file/folder in default application
await window.electron.shell.openPath('C:/Users/John/Documents');

// Show file in explorer
await window.electron.shell.showItemInFolder('C:/Users/John/video.mp4');

// Move to trash
await window.electron.shell.trashItem('C:/Users/John/old-file.txt');
```

## Platform Detection

```typescript
// Check platform
if (window.electron.platform.isWindows) {
  // Windows-specific code
}

if (window.electron.platform.isMac) {
  // macOS-specific code
}

// Get platform info
const os = window.electron.platform.os; // 'win32', 'darwin', 'linux'
const arch = window.electron.platform.arch; // 'x64', 'arm64', etc.

// Get versions
const versions = window.electron.platform.versions;
// { node: '18.x.x', chrome: '120.x.x', electron: '28.x.x' }
```

## Window Controls

```typescript
// Minimize window
await window.electron.window.minimize();

// Maximize/restore toggle
await window.electron.window.maximize();

// Close window
await window.electron.window.close();

// Hide window
await window.electron.window.hide();

// Show window
await window.electron.window.show();
```

## Debugging

### Enable Verbose Logging

```bash
# Run in development mode
npm run dev

# Or set environment variable
DEBUG=* npm start
```

### Check Logs

Logs are saved to:
- Windows: `%APPDATA%/aura-video-studio/logs/`
- macOS: `~/Library/Application Support/aura-video-studio/logs/`
- Linux: `~/.config/aura-video-studio/logs/`

```typescript
// Open logs folder from app
await window.electron.shell.openPath(
  (await window.electron.app.getPaths()).logs
);
```

### DevTools

```typescript
// Development mode: DevTools open by default
// Production mode: Use menu View > Toggle Developer Tools
// Or keyboard: Ctrl+Shift+I (Windows/Linux), Cmd+Option+I (Mac)
```

## Common Patterns

### Safe IPC Calls with Error Handling

```typescript
async function getConfig(key: string, defaultValue: any) {
  try {
    return await window.electron.config.get(key, defaultValue);
  } catch (error) {
    console.error('Failed to get config:', error);
    return defaultValue;
  }
}
```

### React Hook for IPC

```typescript
import { useEffect, useState } from 'react';

function useElectronConfig<T>(key: string, defaultValue: T) {
  const [value, setValue] = useState<T>(defaultValue);
  
  useEffect(() => {
    window.electron.config.get(key, defaultValue).then(setValue);
  }, [key, defaultValue]);
  
  const updateValue = async (newValue: T) => {
    await window.electron.config.set(key, newValue);
    setValue(newValue);
  };
  
  return [value, updateValue] as const;
}

// Usage
function MyComponent() {
  const [theme, setTheme] = useElectronConfig('theme', 'dark');
  
  return (
    <button onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}>
      Toggle Theme
    </button>
  );
}
```

### Event Listener Hook

```typescript
function useElectronEvent(
  listener: (callback: Function) => () => void,
  callback: Function,
  deps: any[] = []
) {
  useEffect(() => {
    const unsubscribe = listener(callback);
    return () => unsubscribe();
  }, deps);
}

// Usage
function VideoProgress() {
  useElectronEvent(
    window.electron.video.onProgress,
    (data) => {
      console.log('Progress:', data.progress);
    }
  );
  
  return <div>...</div>;
}
```

## Troubleshooting

### "electron is not defined"

Make sure you're checking for Electron:

```typescript
if (window.electron) {
  // Electron-specific code
} else {
  // Browser fallback
}
```

### IPC call fails with "Invalid channel"

1. Check channel is in `VALID_CHANNELS` in preload.js
2. Verify channel name matches exactly
3. Check handler is registered in main.js

### "Rate limit exceeded"

You're calling IPC too frequently (>10 calls/second per channel). Add debouncing:

```typescript
import { debounce } from 'lodash';

const debouncedSave = debounce(async (value) => {
  await window.electron.config.set('key', value);
}, 500);
```

### Backend won't start

1. Check console logs in DevTools
2. Verify Aura.Api is compiled
3. Check backend executable path
4. Review logs in %APPDATA%/aura-video-studio/logs/

## Next Steps

- Read full documentation: `electron/README.md`
- Review type definitions: `electron/types.d.ts`
- Check implementation summary: `PR_A1_IMPLEMENTATION_SUMMARY.md`
- Run tests: `PR_A1_CHECKLIST.md`

## Need Help?

- Check console for errors
- Review logs in userData/logs/
- Read module-specific comments in code
- See examples in this guide

Happy coding! 🚀
