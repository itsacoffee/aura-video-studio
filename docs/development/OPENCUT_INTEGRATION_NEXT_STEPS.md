# OpenCut Integration - Next Steps

## Current Status

OpenCut is partially integrated into Aura Video Studio. The current implementation:

- ✅ OpenCut page loads directly (no forwarding page)
- ✅ Iframe attempts to load OpenCut even if health check fails
- ✅ OpenCutManager automatically starts the server in Electron
- ✅ Health check with fallback logic
- ⚠️ Server startup may take time on first load
- ⚠️ Manual server start required in web-only mode

## Issues Fixed in This PR

1. **79% Stuck Issue**: Fixed progress calculation to properly transition from Image generation (80%) to Rendering stage
2. **OpenCut Forwarding Page**: Removed forwarding page, now loads OpenCut directly in iframe
3. **Connection Handling**: Improved health check logic and timeout handling

## Remaining Work for Full Integration

### 1. Server Startup Reliability

**Problem**: OpenCut server may not start reliably, especially on first launch or if dependencies are missing.

**Next Steps**:

- [ ] Add startup verification that waits for server to be ready before showing iframe
- [ ] Implement exponential backoff for server startup retries
- [ ] Add better error messages when server fails to start (missing dependencies, port conflicts, etc.)
- [ ] Create a startup status indicator in the UI showing server startup progress

**Files to Modify**:

- `Aura.Desktop/electron/opencut-manager.js` - Add startup verification
- `Aura.Web/src/pages/OpenCutPage.tsx` - Add startup status UI

### 2. Web-Only Mode Support

**Problem**: When running Aura in web-only mode (not Electron), OpenCut server must be started manually.

**Next Steps**:

- [ ] Create a backend API endpoint to start/stop OpenCut server
- [ ] Add UI controls to start/stop server from the OpenCut page
- [ ] Add automatic detection of running OpenCut server
- [ ] Provide clear instructions for manual server startup

**Files to Create/Modify**:

- `Aura.Api/Controllers/OpenCutController.cs` - New controller for server management
- `Aura.Web/src/pages/OpenCutPage.tsx` - Add server control UI
- `Aura.Web/src/services/opencutService.ts` - New service for OpenCut API calls

### 3. Deep Integration Features

**Problem**: OpenCut currently runs as a separate app in an iframe with limited integration.

**Next Steps**:

- [ ] Implement postMessage API for communication between Aura and OpenCut
- [ ] Add ability to pass video projects from Aura to OpenCut
- [ ] Add ability to import edited videos back from OpenCut to Aura
- [ ] Share authentication/authorization between Aura and OpenCut
- [ ] Implement shared state management (current project, user preferences, etc.)

**Files to Create/Modify**:

- `Aura.Web/src/services/opencutBridge.ts` - Communication bridge
- `OpenCut/apps/web/src/lib/aura-bridge.ts` - OpenCut-side bridge
- `Aura.Web/src/pages/OpenCutPage.tsx` - Add message handlers

### 4. Error Handling and User Experience

**Problem**: Error states are not always clear, and users may not know what to do when OpenCut fails to load.

**Next Steps**:

- [ ] Add detailed error messages with actionable steps
- [ ] Implement automatic retry with exponential backoff
- [ ] Add diagnostic information (server logs, port status, etc.)
- [ ] Create troubleshooting guide
- [ ] Add "Report Issue" button that collects diagnostic information

**Files to Modify**:

- `Aura.Web/src/pages/OpenCutPage.tsx` - Enhanced error UI
- `Aura.Web/src/components/OpenCutDiagnostics.tsx` - New diagnostic component

### 5. Performance and Resource Management

**Problem**: OpenCut server consumes resources even when not in use.

**Next Steps**:

- [ ] Implement lazy loading - only start server when OpenCut page is accessed
- [ ] Add server auto-shutdown after inactivity
- [ ] Monitor server resource usage
- [ ] Add option to disable auto-start for users who prefer manual control

**Files to Modify**:

- `Aura.Desktop/electron/opencut-manager.js` - Add lazy loading and auto-shutdown
- `Aura.Web/src/pages/OpenCutPage.tsx` - Add resource monitoring UI

### 6. Testing and Quality Assurance

**Problem**: Limited testing coverage for OpenCut integration.

**Next Steps**:

- [ ] Add unit tests for OpenCutManager
- [ ] Add integration tests for server startup/shutdown
- [ ] Add E2E tests for OpenCut page loading
- [ ] Test on different platforms (Windows, macOS, Linux)
- [ ] Test with different Node.js versions

**Files to Create**:

- `Aura.Desktop/electron/__tests__/opencut-manager.test.js`
- `Aura.Web/src/pages/__tests__/OpenCutPage.test.tsx`
- `Aura.E2E/OpenCutIntegrationTests.cs`

## Implementation Priority

1. **High Priority** (Critical for basic functionality):

   - Server startup reliability (#1)
   - Web-only mode support (#2)
   - Error handling improvements (#4)

2. **Medium Priority** (Enhances user experience):

   - Deep integration features (#3)
   - Performance optimizations (#5)

3. **Low Priority** (Quality and maintainability):
   - Testing coverage (#6)

## Technical Notes

### Server Startup

The OpenCut server is started by `OpenCutManager` in the Electron main process. It:

- Checks if port 3100 is available
- Spawns the Next.js dev server (dev mode) or standalone server (packaged mode)
- Monitors server health with periodic checks
- Retries on failure (up to 3 attempts)

### Health Check

The frontend performs health checks via:

1. `/api/health` endpoint (preferred)
2. Direct HEAD request to root URL (fallback)

Health check failures are non-blocking - the iframe will still attempt to load.

### Port Configuration

Default port: `3100`

- Can be overridden with `OPENCUT_PORT` environment variable
- Port conflicts are detected and logged

### Development vs Production

- **Development**: Runs Next.js dev server from `OpenCut/apps/web`
- **Production**: Runs standalone Next.js server from `resources/opencut`

## Related Files

- `Aura.Web/src/pages/OpenCutPage.tsx` - Main OpenCut page component
- `Aura.Desktop/electron/opencut-manager.js` - Server management
- `Aura.Desktop/electron/main.js` - Initializes OpenCutManager
- `OpenCut/apps/web/src/app/api/health/route.ts` - Health check endpoint

## References

- [Next.js Standalone Output](https://nextjs.org/docs/advanced-features/output-file-tracing)
- [Electron Child Process Management](https://www.electronjs.org/docs/latest/api/child-process)
- [PostMessage API](https://developer.mozilla.org/en-US/docs/Web/API/Window/postMessage)
