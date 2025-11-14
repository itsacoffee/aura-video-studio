# Aura Video Studio - Development Guide

This guide covers local development for Aura Video Studio's **backend and frontend components**. For Electron desktop app development, see [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md).

## Table of Contents

- [Overview](#overview)
- [Desktop App vs Component Development](#desktop-app-vs-component-development)
- [Architecture](#architecture)
- [Development Environment](#development-environment)
- [Component Development Workflows](#component-development-workflows)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## Overview

Aura Video Studio is an **Electron desktop application** that bundles:
- **React frontend** (Aura.Web) - UI components built with React + TypeScript + Vite
- **ASP.NET Core backend** (Aura.Api) - REST API with Server-Sent Events
- **Electron shell** (Aura.Desktop) - Native desktop wrapper, IPC, window management

This guide focuses on developing the backend and frontend **components** in isolation for rapid iteration. For full desktop app development (Electron main process, IPC, packaging), see [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md).

## Desktop App vs Component Development

### Desktop App Development (Production-like)
**When to use:** Final testing, Electron-specific features, IPC development, packaging

**See:** [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for:
- Electron main process development
- IPC handlers and preload scripts
- Native desktop features (menus, tray, dialogs)
- Building installers and distribution

### Component Development (Rapid Iteration)
**When to use:** Backend API development, frontend UI development, quick testing

**This guide covers:**
- Running backend API standalone
- Running frontend in browser with Vite hot-reload
- API endpoint development
- UI component development

## Architecture

### High-Level Overview (Electron Desktop App)

```
┌──────────────────────────────────────────────────────────────┐
│              Electron Main Process                           │
│         (Node.js, Window Mgmt, IPC, Lifecycle)              │
└────┬──────────────────────────────┬──────────────────────────┘
     │ spawns child process         │ IPC communication
     ▼                              ▼
┌────────────────┐         ┌─────────────────────────────────┐
│   ASP.NET      │ HTTP    │    Electron Renderer Process    │
│   Backend      │◄────────┤      (React + Vite UI)          │
│   (Aura.Api)   │────────►│      Sandboxed Browser          │
└────────────────┘         └─────────────────────────────────┘
     │
     ▼
┌────────────────────────────────────────────────────────┐
│  Aura.Core (Domain Logic)                              │
│  Aura.Providers (LLM, TTS, Images, Video)              │
│  FFmpeg (Video Rendering)                              │
└────────────────────────────────────────────────────────┘
```

**In Production (Electron App):**
- Electron main process spawns the .NET backend as a child process
- Backend runs on random available port (e.g., http://localhost:54321)
- React frontend is loaded from bundled files into Electron window
- Frontend communicates with backend via HTTP/REST and SSE
- Frontend communicates with Electron main process via IPC (through preload script)

**In Development (Component Mode):**
- Backend runs standalone on http://localhost:5005
- Frontend runs in browser with Vite dev server on http://localhost:5173
- Hot reload enabled for fast iteration
- No Electron process (browser-only)

### Project Structure

```
Aura/
├── Aura.Desktop/        # Electron desktop application
│   ├── electron/        # Main process, IPC handlers, window management
│   ├── assets/          # Icons, splash screen
│   ├── build/           # Build configuration (NSIS, DMG)
│   └── package.json     # Electron dependencies and build scripts
├── Aura.Api/            # REST API + SSE endpoints
│   ├── Controllers/     # API controllers
│   ├── HealthChecks/    # Health check implementations
│   ├── Data/            # Database context and seed data
│   ├── Middleware/      # Custom middleware
│   └── Services/        # Application services
├── Aura.Core/           # Domain logic and orchestration
│   ├── Models/          # Domain models
│   ├── Orchestrator/    # Video generation orchestration
│   ├── Planner/         # Content planning
│   └── Data/            # Data access layer
├── Aura.Providers/      # External provider integrations
│   ├── Llm/             # Language model providers
│   ├── Tts/             # Text-to-speech providers
│   ├── Images/          # Image providers
│   └── Video/           # Video processing
├── Aura.Web/            # React frontend (bundled into Electron)
│   ├── src/
│   │   ├── api/         # API client
│   │   ├── components/  # React components
│   │   ├── pages/       # Page components
│   │   ├── services/    # Frontend services
│   │   └── state/       # State management (Zustand)
│   └── public/          # Static assets
├── Aura.Tests/          # Unit and integration tests
└── Aura.E2E/            # End-to-end tests (Playwright)
```

## Development Environment

### Prerequisites

| Tool | Minimum Version | Required For | Notes |
|------|----------------|--------------|-------|
| **Node.js** | 20.0+ | Frontend (Aura.Web) | Use version from .nvmrc if available |
| **npm** | 9.0+ | Frontend dependencies | Comes with Node.js |
| **.NET SDK** | 8.0+ | Backend (Aura.Api, Aura.Core) | Required |
| **Electron** | 32.0+ | Desktop app | Installed via npm in Aura.Desktop |
| **FFmpeg** | 4.0+ | Video rendering | Required at runtime |

**Optional:**
- **Git** - Version control
- **Visual Studio Code** - Recommended editor
- **Docker** - For containerized dependencies (alternative approach)

### First-Time Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-org/aura-video-studio.git
   cd aura-video-studio
   ```

2. **Install frontend dependencies:**
   ```bash
   cd Aura.Web
   npm install
   ```

3. **Install Electron dependencies:**
   ```bash
   cd ../Aura.Desktop
   npm install
   ```

4. **Restore .NET dependencies:**
   ```bash
   cd ../Aura.Api
   dotnet restore
   ```

5. **Configure environment (optional):**
   ```bash
   # Copy example config
   cp .env.example .env
   
   # Edit .env to add API keys for premium features (optional)
   nano .env
   ```

### Environment Variables

Create a `.env` file in the repository root (copy from `.env.example`):

```bash
# Core API
AURA_DATABASE_PATH=./data/aura.db
AURA_FFMPEG_PATH=/path/to/ffmpeg  # Leave empty for auto-detection

# Provider API Keys (optional - enables premium features)
AURA_OPENAI_API_KEY=      # For GPT-4 script generation
AURA_ELEVENLABS_API_KEY=  # For premium TTS voices

# Feature Flags
AURA_OFFLINE_MODE=false
AURA_ENABLE_ADVANCED_MODE=false
```

**Note:** Aura works without any API keys using free/local providers.

## Component Development Workflows

### Quick Start: Component Development Mode

**For rapid backend/frontend iteration without Electron:**

```bash
# Terminal 1: Start backend API
cd Aura.Api
dotnet run
# API runs at http://localhost:5005

# Terminal 2: Start frontend dev server
cd Aura.Web
npm run dev
# Frontend runs at http://localhost:5173 with hot reload

# Open browser to http://localhost:5173
```

**Benefits:**
- ✅ Frontend hot-reload for instant UI updates
- ✅ Backend watch mode available via `dotnet watch run`
- ✅ Browser DevTools for debugging
- ✅ Fast iteration cycle

**Limitations:**
- ❌ No Electron-specific features (IPC, native dialogs, menus)
- ❌ Not testing final desktop app behavior
- ❌ Browser environment differs from Electron renderer

**When to use:** Backend API development, frontend UI components, quick prototyping

**When to switch to Electron:** Testing IPC, native features, final integration testing

### Desktop App Development Mode

**For full Electron app development (recommended for final testing):**

See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for complete instructions.

```bash
# Build frontend
cd Aura.Web
npm run build:prod

# Run Electron in dev mode
cd ../Aura.Desktop
npm run dev
```

### Typical Development Session

```bash
# Morning: Start component development mode
cd Aura.Api && dotnet watch run &  # Backend with auto-reload
cd Aura.Web && npm run dev         # Frontend with hot reload

# Work on features in your editor
# Changes auto-reload in browser

# Before committing: Test in Electron
cd Aura.Web && npm run build:prod
cd ../Aura.Desktop && npm run dev

# Run tests
cd Aura.Web && npm test
dotnet test

# Commit changes
git add .
git commit -m "feat: your feature"
```

### Backend Development

**Running with auto-reload:**
```bash
cd Aura.Api
dotnet watch run
# Backend auto-reloads on .cs file changes
```

**Making changes:**
1. Edit .cs files in Aura.Api, Aura.Core, or Aura.Providers
2. Save file - backend automatically rebuilds and restarts
3. Test API endpoint via browser, Postman, or frontend

**Testing endpoints:**
```bash
# Health check
curl http://localhost:5005/health/live

# API endpoint example
curl http://localhost:5005/api/v1/jobs
```

### Frontend Development

**Running with hot reload:**
```bash
cd Aura.Web
npm run dev
# Vite dev server with instant hot module replacement (HMR)
```

**Making changes:**
1. Edit .tsx/.ts files in Aura.Web/src
2. Save file - browser automatically updates (no page reload)
3. Changes reflect instantly in browser

**Code quality checks:**
```bash
cd Aura.Web

# Type checking
npm run type-check

# Linting
npm run lint

# Format checking
npm run format:check

# All checks at once
npm run quality-check
```

### Building for Electron

**When you need to test in the Electron app:**

```bash
# 1. Build frontend production bundle
cd Aura.Web
npm run build:prod
# Creates dist/ folder with optimized bundle

# 2. Build backend (if changed)
cd ../Aura.Api
dotnet build -c Release

# 3. Run Electron
cd ../Aura.Desktop
npm run dev
# Or build full installer: npm run build:win
```

### Adding a New API Endpoint

1. **Create/update controller:**
   ```csharp
   // Aura.Api/Controllers/YourController.cs
   [ApiController]
   [Route("api/v1/[controller]")]
   public class YourController : ControllerBase
   {
       [HttpGet]
       public IActionResult Get() { /* ... */ }
   }
   ```

2. **Test the endpoint:**
   ```bash
   # With backend running on localhost:5005
   curl http://localhost:5005/api/v1/your-endpoint
   ```

3. **Update frontend API client (if needed):**
   ```typescript
   // Aura.Web/src/services/api/apiClient.ts
   export async function getYourData() {
     const response = await apiClient.get('/api/v1/your-endpoint');
     return response.data;
   }
   ```

4. **Test in both modes:**
   - Component mode: Browser at http://localhost:5173
   - Electron mode: `cd Aura.Desktop && npm run dev`

## Testing

### Unit Tests

```bash
# Run all .NET tests
dotnet test

# Run specific test project
dotnet test Aura.Tests/Aura.Tests.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

```bash
cd Aura.Web

# Run unit tests
npm run test

# Run with coverage
npm run test:coverage

# Run in watch mode
npm run test:watch
```

### E2E Tests

```bash
# Using test-specific compose file
docker-compose -f docker-compose.test.yml up --abort-on-container-exit

# Or run Playwright tests directly
cd Aura.Web
npm run playwright

# With UI
npm run playwright:ui
```

### Integration Tests

```bash
# Start services
make dev-detached

# Run integration tests
dotnet test Aura.E2E/Aura.E2E.csproj

# Cleanup
make clean
```

## Troubleshooting

### Backend won't start

**Check .NET SDK:**
```bash
dotnet --version
# Should be 8.0.x or higher
```

**Check port availability:**
```bash
# Windows
netstat -ano | findstr :5005

# Linux/macOS
lsof -i :5005
```

**Check logs:**
```bash
cd Aura.Api
dotnet run --verbosity detailed
```

### Frontend dev server fails

**Check Node.js version:**
```bash
node --version
# Should be 20.0.0 or higher
```

**Clear cache and reinstall:**
```bash
cd Aura.Web
rm -rf node_modules .vite dist
npm install
npm run dev
```

### Frontend can't connect to backend

**Verify backend is running:**
```bash
curl http://localhost:5005/health/live
```

**Check CORS configuration:**
- Backend should allow http://localhost:5173 in development
- Check Aura.Api/Program.cs for CORS policy

**Check frontend API base URL:**
```typescript
// Aura.Web/src/services/api/apiClient.ts
// Should point to http://localhost:5005 in development
```

### FFmpeg not found

**Install FFmpeg:**
```bash
# Windows (winget)
winget install ffmpeg

# macOS
brew install ffmpeg

# Linux (Ubuntu/Debian)
sudo apt-get install ffmpeg
```

**Verify installation:**
```bash
ffmpeg -version
```

### Electron app issues

For Electron-specific issues, see [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) troubleshooting section.

### Clean slate reset

```bash
# Remove all build artifacts
cd Aura.Web
rm -rf node_modules dist .vite
npm install

cd ../Aura.Desktop
rm -rf node_modules
npm install

cd ../Aura.Api
dotnet clean
dotnet restore
```

## Performance Optimization

### Component Development Performance

1. **Backend watch mode:**
   - Use `dotnet watch run` for auto-reload
   - Only rebuilds changed projects
   - Faster than full rebuild

2. **Frontend hot reload:**
   - Vite HMR is near-instant
   - Keep dev server running
   - Use `npm run dev` (not `npm run build`)

3. **Selective testing:**
   ```bash
   # Test specific file
   npm test -- path/to/file.test.ts
   
   # Test specific .NET project
   dotnet test Aura.Tests/Aura.Tests.csproj
   ```

### Electron Build Performance

See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for Electron-specific optimizations.

## Contributing

### Before Submitting a PR

1. **Run all checks:**
   ```bash
   make test
   cd Aura.Web && npm run quality-check
   ```

2. **Ensure clean state:**
   ```bash
   make clean
   make dev
   make health
   ```

3. **Update documentation:**
   - Add/update relevant docs
   - Update CHANGELOG.md
   - Include inline code comments

4. **Follow conventions:**
   - See [CONTRIBUTING.md](CONTRIBUTING.md) for code style
   - Use conventional commits
   - Write tests for new features

### Commit Message Format

```
type(scope): subject

body

footer
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Test changes
- `chore`: Build/tooling changes

**Example:**
```
feat(api): add video export queue endpoint

Implements a new endpoint for queuing video exports with priority support.
Includes rate limiting and authentication.

Closes #123
```

## Additional Resources

- **[README.md](README.md)** - Project overview and quick start
- **[DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md)** - Electron desktop app development (CRITICAL for production)
- **[BUILD_GUIDE.md](BUILD_GUIDE.md)** - Building installers and packaging
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines
- **docs/architecture/** - System design and architecture details
- **docs/api/** - REST API documentation
- **Electron README** - Electron main process architecture

## Getting Help

- **Issues:** GitHub Issues for bug reports and feature requests
- **Discussions:** GitHub Discussions for questions and ideas
- **Documentation:** Check `docs/troubleshooting/` for common issues
- **Logs:**
  - Backend: Console output from `dotnet run`
  - Frontend: Browser console (F12)
  - Electron: See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md)

---

**For Electron/Desktop Development:** See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md)

**Happy coding!** 🚀
