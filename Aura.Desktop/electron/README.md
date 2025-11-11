# Aura Video Studio - Electron Main Process

This directory contains the foundational Electron main process architecture for Windows desktop application.

## Architecture Overview

The Electron main process is organized into modular components for maintainability and scalability:

```
electron/
├── main.js                    # Main entry point - orchestrates all modules
├── preload.js                 # Security bridge for IPC (context-isolated)
├── types.d.ts                 # TypeScript type definitions for IPC
├── window-manager.js          # Window lifecycle and state management
├── app-config.js              # Configuration and settings management
├── backend-service.js         # .NET backend process management
├── tray-manager.js            # Windows system tray integration
├── menu-builder.js            # Application menu system
├── protocol-handler.js        # Custom protocol (aura://) handler
└── ipc-handlers/              # Organized IPC handlers
    ├── config-handler.js      # Configuration IPC
    ├── system-handler.js      # System operations (dialogs, shell, etc.)
    ├── video-handler.js       # Video generation commands
    └── backend-handler.js     # Backend health checks and status
```

## Key Features

### 1. Window Management
- **State Persistence**: Window position, size, and maximized state are saved and restored
- **Multi-monitor Support**: Validates window position is visible on available displays
- **Security**: Content Security Policy (CSP) headers, context isolation, sandboxing
- **Development vs Production**: Different CSP policies and DevTools behavior

### 2. IPC Architecture
All IPC communication is:
- **Type-safe**: TypeScript definitions in `types.d.ts`
- **Validated**: Input sanitization and validation
- **Rate-limited**: Prevents abuse of IPC channels
- **Secure**: Channel whitelisting, path traversal prevention

Available IPC channels:
- **Configuration**: Settings, secure storage, recent projects
- **Dialogs**: File/folder selection, save dialogs, message boxes
- **Shell**: External URL/path opening with validation
- **Video**: Generation control (start, pause, resume, cancel)
- **Backend**: Health checks, provider status, FFmpeg status
- **App**: Version, paths, locale, restart/quit

### 3. Security Measures

#### Content Security Policy (CSP)
- **Production**: Strict CSP with no unsafe-inline/eval
- **Development**: Permissive for hot-reload support

#### IPC Security
- Channel whitelisting
- Input validation and sanitization
- Rate limiting (10 calls/second per channel)
- Path traversal prevention
- URL protocol validation (only http/https/mailto)

#### Process Isolation
- Context isolation enabled
- Node integration disabled
- Sandbox enabled
- Remote module disabled (deprecated)

### 4. Backend Integration
- **Auto-discovery**: Finds available port automatically
- **Health Monitoring**: Periodic health checks with auto-restart
- **Crash Recovery**: Up to 3 restart attempts on unexpected exit
- **Process Management**: Graceful startup and shutdown
- **FFmpeg Integration**: Automatic path configuration

### 5. Menu System
Comprehensive application menu with:
- File operations (New, Open, Save, Import, Export)
- Edit operations (Undo, Redo, Copy, Paste)
- View controls (Zoom, DevTools, Fullscreen)
- Tools (Settings, FFmpeg Config, Cache management)
- Help (Documentation, Updates, About)

### 6. System Tray (Windows)
- Show/Hide window
- Quick actions (New Project, Open Project)
- Operation status display
- Cancel current operation
- Backend URL display
- Version info
- Quit option

### 7. Protocol Handler
Custom protocol `aura://` for deep linking:
- `aura://open?path=/path/to/project` - Open project
- `aura://create?template=basic` - Create new project
- `aura://generate?script=...` - Start generation
- `aura://settings` - Open settings
- `aura://help` - Open help
- `aura://about` - Show about

All URLs are validated and sanitized to prevent injection attacks.

### 8. Error Handling
- **Global Exception Handler**: Catches and logs all uncaught exceptions
- **Unhandled Rejection Handler**: Catches promise rejections
- **Crash Logging**: Saves detailed crash reports to logs directory
- **User Notifications**: Friendly error messages with recovery options
- **Crash Limits**: Prevents infinite crash loops (max 3 crashes)

### 9. Process Lifecycle
- **Single Instance Lock**: Prevents multiple instances
- **Graceful Shutdown**: Cleans up resources on exit
- **Auto-restart**: Backend auto-restarts on crash
- **Temp Cleanup**: Removes temporary files on quit
- **State Persistence**: Saves application state

### 10. Development vs Production

#### Development Mode
- DevTools open by default
- Verbose logging
- Permissive CSP
- Hot reload support
- Mock backend option
- Update checks disabled

#### Production Mode
- DevTools disabled (enable via menu)
- Minimal logging
- Strict CSP
- Auto-updater enabled
- Crash reporting enabled
- Performance monitoring

## Usage

### Starting the Application

```bash
# Development mode
npm run dev

# Production mode
npm start

# Build for Windows
npm run build:win
```

### Environment Variables

```bash
# Disable hardware acceleration (fixes some Windows issues)
DISABLE_HARDWARE_ACCELERATION=true

# Custom encryption key for secure storage
AURA_ENCRYPTION_KEY=your-secret-key

# Backend configuration
DOTNET_ENVIRONMENT=Development|Production
ASPNETCORE_URLS=http://localhost:5005
```

### Frontend Integration

The preload script exposes a `window.electron` API to the renderer process:

```typescript
// TypeScript (with types)
import type { ElectronAPI } from '../electron/types';

const electron = window.electron;

// Configuration
await electron.config.get('theme');
await electron.config.set('theme', 'dark');

// Dialogs
const folder = await electron.dialog.openFolder();
const file = await electron.dialog.openFile({
  filters: [{ name: 'Videos', extensions: ['mp4', 'avi'] }]
});

// Video generation
const result = await electron.video.generate.start(config);
electron.video.onProgress((data) => {
  console.log(`Progress: ${data.progress}%`);
});

// Backend health
electron.backend.onHealthUpdate((status) => {
  console.log('Backend status:', status);
});

// Menu actions
electron.menu.onNewProject(() => {
  // Handle new project from menu
});
```

### Adding New IPC Handlers

1. Create handler in `ipc-handlers/`:

```javascript
// ipc-handlers/my-handler.js
const { ipcMain } = require('electron');

class MyHandler {
  constructor(deps) {
    this.deps = deps;
  }

  register() {
    ipcMain.handle('my:action', async (event, ...args) => {
      // Validate inputs
      // Perform action
      // Return result
    });
  }
}

module.exports = MyHandler;
```

2. Register in `main.js`:

```javascript
const MyHandler = require('./ipc-handlers/my-handler');

function registerIpcHandlers() {
  // ...existing handlers...
  
  ipcHandlers.my = new MyHandler(dependencies);
  ipcHandlers.my.register();
}
```

3. Add to preload whitelist in `preload.js`:

```javascript
const VALID_CHANNELS = {
  // ...existing channels...
  MY: ['my:action']
};
```

4. Expose in preload API:

```javascript
contextBridge.exposeInMainWorld('electron', {
  // ...existing APIs...
  my: {
    action: (...args) => safeInvoke('my:action', ...args)
  }
});
```

5. Add TypeScript types in `types.d.ts`:

```typescript
export interface ElectronAPI {
  // ...existing APIs...
  my: {
    action(...args: any[]): Promise<any>;
  };
}
```

## Security Best Practices

1. **Always validate IPC inputs** - Never trust data from renderer
2. **Use channel whitelisting** - Only allow predefined channels
3. **Sanitize file paths** - Prevent path traversal attacks
4. **Validate URLs** - Only allow safe protocols
5. **Rate limit IPC calls** - Prevent abuse
6. **Use context isolation** - Keep Node.js and DOM separate
7. **Disable nodeIntegration** - Never expose Node.js to renderer
8. **Enable sandbox** - Isolate renderer processes
9. **Implement CSP** - Prevent XSS attacks
10. **Secure storage** - Encrypt sensitive data

## Testing

### Manual Testing Checklist
- [ ] Window opens and displays correctly
- [ ] Window state persists (position, size, maximized)
- [ ] Multi-monitor support works
- [ ] Tray icon functions correctly
- [ ] Menu items work as expected
- [ ] Protocol handler responds to aura:// URLs
- [ ] Backend starts and responds to health checks
- [ ] IPC communication works bidirectionally
- [ ] DevTools can be opened in development
- [ ] Security policies are enforced
- [ ] Crash recovery works
- [ ] Application quits gracefully

### Automated Testing
See `Aura.E2E/` directory for end-to-end tests.

## Troubleshooting

### Backend Won't Start
- Check logs in `%APPDATA%/aura-video-studio/logs/`
- Verify backend executable exists
- Check port availability
- Review environment variables

### Window Position Issues
- Delete window-state.json in userData directory
- Check display configuration
- Verify multi-monitor setup

### IPC Communication Fails
- Check console for validation errors
- Verify channel is whitelisted
- Check rate limiting
- Review error logs

### Tray Icon Not Showing
- Verify icon file exists in `assets/icons/`
- Check Windows notification settings
- Review console for errors

## Additional Resources

- [Electron Security Guidelines](https://www.electronjs.org/docs/latest/tutorial/security)
- [Electron IPC Documentation](https://www.electronjs.org/docs/latest/tutorial/ipc)
- [electron-builder Documentation](https://www.electron.build/)
- [Project Main README](../README.md)

## License

MIT - See LICENSE.txt for details
