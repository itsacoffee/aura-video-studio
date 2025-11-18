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

- **React frontend** (Aura.Web) - UI components built with React + TypeScript + Vite, **always bundled into Electron**
- **ASP.NET Core backend** (Aura.Api) - REST API with Server-Sent Events, embedded as child process
- **Electron shell** (Aura.Desktop) - Native desktop wrapper, IPC, window management

**This guide focuses on developing the backend and frontend components in isolation for rapid iteration.** For full desktop app development (Electron main process, IPC, packaging), see [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md).

**Important:** Aura.Web is the React frontend that is **always bundled into Aura.Desktop** (the Electron shell). Running Aura.Web standalone in the browser is useful for faster frontend iteration, **but it is not the deployment target**. The production runtime is the Electron desktop application.

## Desktop App vs Component Development

### Desktop App Development (Production-like)

**When to use:** Final testing, Electron-specific features, IPC development, packaging

**See:** [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for:

- Electron main process development
- IPC handlers and preload scripts
- Native desktop features (menus, tray, dialogs)
- Building installers and distribution

**Workflow:**
```bash
# Build frontend
cd Aura.Web && npm run build:prod

# Run Electron app
cd ../Aura.Desktop && npm run dev
```

**This is the production-like runtime.** Use this for final testing to ensure all Electron IPC, tray, window management, protocol handling, and backend lifecycle behavior work correctly.

### Component Development (Rapid Iteration)

**When to use:** Backend API development, frontend UI development, quick testing

**This guide covers:**

- Running backend API standalone
- Running frontend in browser with Vite hot-reload
- API endpoint development
- UI component development

**Workflow:**
```bash
# Terminal 1: Backend
cd Aura.Api && dotnet run

# Terminal 2: Frontend
cd Aura.Web && npm run dev
# Open browser to http://localhost:5173
```

**⚠️ Important Limitations:**
- This does **not** exercise Electron IPC, tray, native dialogs, window management, or protocol handling
- This does **not** test the embedded backend lifecycle (spawned by Electron main process)
- This is a **development aid only**, not the deployment target
- Use this for rapid UI/API iteration, but **always perform final testing in Electron**

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
├── Aura.Desktop/        # Electron desktop application (primary runtime, production target)
│   ├── electron/        # Main process, IPC handlers, window management
│   ├── assets/          # Icons, splash screen
│   ├── build/           # Build configuration (NSIS, DMG)
│   └── package.json     # Electron dependencies and build scripts
├── Aura.Api/            # REST API + SSE endpoints (embedded as child process)
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
├── Aura.Web/            # React frontend (always bundled into Aura.Desktop)
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

**Note:** **Aura.Web is the React frontend that is always bundled into Aura.Desktop (Electron shell).** Running Aura.Web standalone in the browser is useful for faster frontend iteration, but **it is not the deployment target**. The production runtime is the Electron desktop application, where the frontend is loaded from bundled files and communicates with the embedded backend.

## Development Environment

### Prerequisites

| Tool         | Minimum Version | Required For                  | Notes                                |
| ------------ | --------------- | ----------------------------- | ------------------------------------ |
| **Node.js**  | 20.0+           | Frontend (Aura.Web)           | Use version from .nvmrc if available |
| **npm**      | 9.0+            | Frontend dependencies         | Comes with Node.js                   |
| **.NET SDK** | 8.0+            | Backend (Aura.Api, Aura.Core) | Required                             |
| **Electron** | 32.0+           | Desktop app                   | Installed via npm in Aura.Desktop    |
| **FFmpeg**   | 4.0+            | Video rendering               | Required at runtime                  |

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

### Desktop Data Paths

When running inside the Electron shell, the main process sets a trio of environment variables so the ASP.NET backend and React UI agree on where data lives:

| Variable | Purpose | Default (when unset) |
| --- | --- | --- |
| `AURA_DATA_PATH` | Root for user data (database, settings, projects, media) | `%LOCALAPPDATA%\Aura` on Windows, `~/.local/share/Aura` on Linux/macOS |
| `AURA_LOGS_PATH` | Directory for rolling log files | `<AURA_DATA_PATH>\logs` |
| `AURA_TEMP_PATH` | Scratch space for renders/SSE/temp files | `<AURA_DATA_PATH>\Temp` |

Under `AURA_DATA_PATH` the backend now creates a stable structure:

```
<AURA_DATA_PATH>/
├── AuraData/           # settings.json, provider paths, portable metadata
├── MediaLibrary/       # uploads, thumbnails, chunked uploads
├── Workspace/          # enhanced workspace (projects, exports, cache, previews)
├── Temp/               # transient render + SSE artifacts (also synced to AURA_TEMP_PATH)
├── jobs/               # telemetry JSON artifacts
├── proxy/              # generated proxy media
└── logs/               # Serilog rolling logs (mirrors AURA_LOGS_PATH)
```

You can override any of these by exporting the environment variables above before launching the backend (handy for portable builds, dev sandboxes, or Windows paths with spaces). When unset, the previous portable behavior (storing data next to the executable) is preserved.

**Note:** Aura works without any API keys using free/local providers.

### Backend URL Contract

The frontend, backend, and Electron shell now share a single source of truth for the API origin:

- Set **`AURA_BACKEND_URL`** (preferred) or `ASPNETCORE_URLS` to the desired base URL, e.g. `http://127.0.0.1:5272`.
- Electron reads this value on startup, starts (or attaches to) the backend on that port, and exposes it to the renderer through a typed preload bridge (`window.desktopBridge`).
- The React app always resolves requests through this bridge or `VITE_API_BASE_URL`, so there is no longer any guessing or hard-coded `localhost` URLs.

Example (PowerShell):

```powershell
$env:AURA_BACKEND_URL = "http://127.0.0.1:5272"
cd Aura.Api
dotnet run
```

Then, in another terminal:

```powershell
cd Aura.Web
npm run dev
```

The Vite dev server will proxy `/api/*` calls to the backend URL from the bridge automatically.

### Provider Connectivity Testing

Provider key validation now relies on the unified ping endpoints inside `ProvidersController`:

- `POST /api/providers/{providerId}/ping` — runs a live network call using the stored API key and returns detailed diagnostics (`success`, `statusCode`, `errorCode`, `endpoint`, `latencyMs`, `correlationId`).
- `GET /api/providers/ping-all` — iterates over every supported provider (LLM, TTS, image, and local engines) and returns a map of results for dashboards.

Ping requests never accept secrets; API keys are always retrieved from the secure key vault. Optional metadata (e.g., Azure endpoint, PlayHT user ID) can be passed via the request body when a provider requires additional configuration.

Common error codes:

- `MissingApiKey` — key not stored yet.
- `InvalidApiKey` — provider returned 401/403.
- `RateLimited` — provider throttled the request (HTTP 429).
- `ProviderUnavailable` — provider returned 5xx.
- `NetworkError` / `Timeout` — local connectivity issues.

The web UI surfaces these details inside Settings → Providers, and `settingsService.testApiKey` automatically saves the key, triggers a ping, and displays the HTTP status, latency, and correlation ID for debugging.

## Component Development Workflows

### Quick Start: Component Development Mode

**For rapid backend/frontend iteration without Electron (development aid only):**

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

- ❌ No Electron-specific features (IPC, native dialogs, menus, tray, protocol handling)
- ❌ Not testing final desktop app behavior
- ❌ Browser environment differs from Electron renderer
- ❌ Not testing embedded backend lifecycle (spawned by Electron main process)

**When to use:** Backend API development, frontend UI components, quick prototyping

**When to switch to Electron:** Testing IPC, native features, backend lifecycle, final integration testing

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

**This is the production-like runtime.** Always test in Electron before committing to ensure all features work correctly.

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
     const response = await apiClient.get("/api/v1/your-endpoint");
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

**Use the bundled downloader (Windows desktop builds):**

```powershell
cd Aura.Desktop
npm run ffmpeg:ensure
```

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

## Documentation Tooling

### Markdownlint

All markdown files are linted using markdownlint-cli with configuration in `.markdownlint.json`.

**Installation:**

```bash
npm install -g markdownlint-cli
```

**Usage:**

```bash
# Check all markdown files
markdownlint --config .markdownlint.json "*.md" "docs/**/*.md"

# Auto-fix issues
markdownlint --fix --config .markdownlint.json "*.md" "docs/**/*.md"
```

**Configuration:**
The `.markdownlint.json` file is configured to align with the repository's established style:

- Disabled stylistic rules that don't match our conventions (MD036, MD029, MD026, MD024, etc.)
- Enabled important rules for hard tabs and whitespace
- Allows common patterns like emphasis-as-heading and trailing punctuation in headings

### DocFX

API documentation is generated using DocFX.

**Installation:**

```bash
dotnet tool install -g docfx
```

**Usage:**

```bash
# Build documentation
docfx build docfx.json

# Serve documentation locally
docfx serve _site
```

**Configuration:**

- `docfx.json` - Main configuration
- Generates documentation from XML comments in C# code
- Includes markdown documentation from `docs/` directory

**Note:** There may be a cosmetic warning about file links that appears even when links work correctly in the generated HTML. This is a known DocFX behavior and can be safely ignored if the links render correctly.

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
