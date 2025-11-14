# Architecture Migration Note: Web-Based to Electron Desktop

**Date:** November 2025  
**Status:** Migration Complete

## Overview

Aura Video Studio has migrated from a web-based architecture to an **Electron desktop application**. This document provides context for understanding references in historical documentation and explains the current architecture.

## Architecture Evolution

### Previous Architecture (Pre-Migration)

**Web-Based Application:**
- Standalone React frontend served via Vite dev server
- Separate ASP.NET Core backend API
- Docker Compose for orchestration
- Browser-based access at http://localhost:3000 or http://localhost:5173
- Redis for caching and sessions
- Separate deployment as web application

**Characteristics:**
- Frontend and backend run as separate processes
- Frontend accessed via browser
- Docker containers for dependencies (Redis, FFmpeg)
- Web deployment targets (servers, hosting platforms)

### Current Architecture (Electron Desktop App)

**Electron Desktop Application:**
- **Electron Main Process** - Orchestrates everything
- **React Frontend** - Bundled into Electron, runs in renderer process
- **ASP.NET Core Backend** - Embedded as child process, spawned by Electron
- **Native Desktop App** - Windows installer, macOS DMG, Linux AppImage
- No Docker required (native desktop app)
- No browser required (Electron provides window)

**Process Model:**
```
┌──────────────────────────────────────┐
│     Electron Main Process            │
│  (window mgmt, IPC, lifecycle)       │
└────┬─────────────────────┬───────────┘
     │ spawns              │ IPC
     ▼                     ▼
┌──────────────┐    ┌─────────────────┐
│  ASP.NET     │◄───┤   Renderer      │
│  Backend     │ HTTP│   Process       │
│  (child)     │───►│  (React UI)     │
└──────────────┘    └─────────────────┘
```

**Characteristics:**
- All-in-one desktop application
- Backend embedded as child process (not external service)
- Frontend loaded from bundled files (not via network)
- Native installers for distribution
- No Docker/containers needed (native app)

## Port References in Documentation

Many historical documents reference specific ports. Here's what they mean:

### localhost:5173

**Context:** Vite dev server (component development mode)

**When Used:**
- During frontend component development in browser
- For rapid iteration with hot module replacement (HMR)
- **NOT used in the Electron app** (frontend is bundled)

**Example:**
```bash
# Component development mode (browser-based)
cd Aura.Web
npm run dev
# Frontend runs at http://localhost:5173
```

**Note:** This is only for developing the frontend components in isolation, not for running the desktop app.

### localhost:3000

**Context:** Old Docker web server port (no longer applicable)

**When Used:**
- In the previous Docker Compose architecture
- Web UI served via Docker container
- **No longer used** in Electron architecture

**Why References Exist:**
- Historical implementation summaries
- Old testing guides
- Pre-migration documentation

**Status:** These references are obsolete for current architecture.

### localhost:5005

**Context:** Backend API port (both standalone and in Electron)

**When Used:**
1. **Component Development Mode:**
   - Backend runs standalone: `cd Aura.Api && dotnet run`
   - API available at http://localhost:5005
   - Used for testing API endpoints directly

2. **Electron App (Development):**
   - Backend spawned by Electron main process
   - Usually runs on random port (e.g., 54321)
   - Sometimes uses 5005 if available
   - Frontend connects automatically via Electron configuration

**Example:**
```bash
# Standalone backend for API testing
cd Aura.Api
dotnet run
# API at http://localhost:5005

# Health check
curl http://localhost:5005/health/live
```

## Development Modes

### Electron Desktop App Development

**Purpose:** Develop and test the complete desktop application

**Workflow:**
```bash
# Build frontend
cd Aura.Web && npm run build:prod

# Run Electron app
cd ../Aura.Desktop && npm run dev
```

**Characteristics:**
- Frontend loaded from bundled files
- Backend spawned automatically by Electron
- Native window (not browser)
- Access to Electron APIs (IPC, native dialogs, etc.)
- Tests the actual production experience

### Component Development Mode

**Purpose:** Rapid iteration on backend API or frontend UI

**Workflow:**
```bash
# Terminal 1: Backend
cd Aura.Api && dotnet watch run
# Runs at http://localhost:5005

# Terminal 2: Frontend
cd Aura.Web && npm run dev
# Runs at http://localhost:5173 with hot reload
```

**Characteristics:**
- Frontend runs in browser (Vite dev server)
- Backend runs standalone
- Hot reload for instant updates
- No Electron (browser-based)
- No access to Electron APIs

**When to Use:**
- Backend API endpoint development
- Frontend UI component development
- Quick testing without full Electron overhead

**When NOT to Use:**
- Testing Electron-specific features (IPC, native dialogs, menus)
- Testing backend lifecycle in Electron
- Final integration testing

## Historical Documentation

### Implementation Summaries

Many files like `PR*_IMPLEMENTATION_SUMMARY.md` reference the old architecture. These are **historical records** and should be understood in context:

**Files Referencing localhost:3000:**
- `PR1_IMPLEMENTATION_SUMMARY.md` - Docker-based setup
- `VERIFICATION_CHECKLIST.md` - Old verification steps
- `QUICK_REFERENCE.md` (now updated)

**Files Referencing localhost:5173:**
- Various testing guides for browser-based testing
- Component development instructions
- Still valid for component mode, but not for Electron app

### Updated Documentation

The following files have been **updated for Electron architecture**:
- `README.md` - Electron architecture overview
- `DEVELOPMENT.md` - Component vs Electron development
- `BUILD_GUIDE.md` - Desktop app building
- `DESKTOP_APP_GUIDE.md` - Complete Electron guide
- `QUICK_REFERENCE.md` - Electron workflows

## Migration Impact

### No Longer Applicable

The following are **no longer used** in Electron architecture:
- Docker Compose workflows
- Makefile commands (make dev, make logs, etc.)
- Redis dependency (unless explicitly added)
- Browser-based access for production
- Web deployment configurations
- localhost:3000 references

### Still Applicable

The following are **still valid** in Electron architecture:
- Backend API structure (ASP.NET Core)
- Core business logic (Aura.Core)
- Provider system (Aura.Providers)
- Frontend components (React + TypeScript)
- Build validation scripts
- Testing infrastructure (Vitest, Playwright)
- localhost:5173 for component development mode
- localhost:5005 for standalone backend testing

## For New Contributors

**If you're new to the project:**

1. **Ignore Docker references** - We don't use Docker anymore (desktop app)
2. **Ignore localhost:3000** - That was the old web port
3. **Use Electron for main development** - See [DESKTOP_APP_GUIDE.md](../../DESKTOP_APP_GUIDE.md)
4. **Use component mode for iteration** - See [DEVELOPMENT.md](../../DEVELOPMENT.md)
5. **localhost:5173** - Only for component mode (browser testing)
6. **localhost:5005** - Backend API port (standalone mode)

## References

- [DESKTOP_APP_GUIDE.md](../../DESKTOP_APP_GUIDE.md) - Complete Electron development guide
- [DEVELOPMENT.md](../../DEVELOPMENT.md) - Component development workflows
- [BUILD_GUIDE.md](../../BUILD_GUIDE.md) - Building desktop app installers
- [Electron README](../../Aura.Desktop/electron/README.md) - Electron architecture details

## Questions?

If you encounter documentation that references old architecture patterns:
1. Check if it's in an archived/historical context
2. Refer to this document for clarification
3. Use the current documentation links above
4. Ask in GitHub Discussions if unclear

---

**Last Updated:** November 2025 (Electron migration complete)
