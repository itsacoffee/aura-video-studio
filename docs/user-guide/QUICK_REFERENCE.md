# Aura Video Studio - Quick Reference

Fast reference for common development tasks.

## Getting Started

### Desktop App (Electron)

```bash
# First time setup
cd Aura.Web && npm install
cd ../Aura.Desktop && npm install
cd ../Aura.Api && dotnet restore

# Build and run Electron app
cd Aura.Web && npm run build:prod
cd ../Aura.Desktop && npm run dev
```

### Component Development

```bash
# Terminal 1: Backend
cd Aura.Api && dotnet watch run

# Terminal 2: Frontend (browser)
cd Aura.Web && npm run dev
```

## Common Commands

### Desktop App Development

```bash
# Run Electron in dev mode
cd Aura.Desktop && npm run dev

# Build installer (Windows)
cd Aura.Desktop && npm run build:win

# Run tests
cd Aura.Desktop && npm test
```

### Component Development

```bash
# Backend (local with auto-reload)
cd Aura.Api && dotnet watch run

# Frontend (browser with hot-reload)
cd Aura.Web && npm run dev

# Type checking
cd Aura.Web && npm run type-check

# Linting
cd Aura.Web && npm run lint
cd Aura.Web && npm run lint:fix
```

### Testing

```bash
# Frontend unit tests
cd Aura.Web && npm test
cd Aura.Web && npm run test:watch

# E2E tests
cd Aura.Web && npm run playwright

# Backend tests
dotnet test Aura.Tests
```

## URLs

### Electron App
- **App:** Runs in native Electron window
- **Backend:** Random port (e.g., http://localhost:54321)
- **Dev Tools:** Menu → View → Toggle Developer Tools

### Component Development Mode
- **Web UI (Vite):** http://localhost:5173
- **API:** http://localhost:5005
- **API Swagger:** http://localhost:5005/swagger
- **API Health:** http://localhost:5005/health/live

## Directory Structure

```
aura-video-studio/
├── data/              # SQLite database (runtime)
├── logs/              # Application logs (runtime)
├── temp-media/        # Temporary media files (runtime)
├── Aura.Desktop/      # Electron desktop app
│   ├── electron/      # Main process, IPC handlers
│   ├── assets/        # Icons, splash screen
│   └── resources/     # Bundled backend, frontend, FFmpeg
├── Aura.Web/          # React frontend (builds to dist/)
├── Aura.Api/          # ASP.NET Core backend
├── Aura.Core/         # Domain logic
├── Aura.Providers/    # Provider integrations (LLM, TTS, etc.)
└── scripts/           # Build and utility scripts
```

## Environment Variables

Key variables in `.env` (optional, for API keys):

```bash
# Provider API Keys (optional - enables premium features)
AURA_OPENAI_API_KEY=       # GPT-4 script generation
AURA_ELEVENLABS_API_KEY=   # Premium TTS voices

# Providers (optional)
AURA_OPENAI_API_KEY=
AURA_STABILITY_API_KEY=
AURA_RUNWAY_API_KEY=

# Features
AURA_OFFLINE_MODE=false
AURA_ENABLE_ADVANCED_MODE=false
```

## Troubleshooting

### Electron app won't start
```bash
# Check if frontend is built
ls -la Aura.Web/dist/

# Rebuild frontend
cd Aura.Web && npm run build:prod

# Check backend
cd Aura.Api && dotnet build
```

### Backend fails in Electron
```bash
# Check logs in Electron DevTools (Menu → View → Toggle Developer Tools)
# Or check log files in user data directory:
# Windows: %APPDATA%/aura-video-studio/logs/
# macOS: ~/Library/Application Support/aura-video-studio/logs/
# Linux: ~/.config/aura-video-studio/logs/
```

### Component mode - Port conflicts
```bash
# Linux/macOS
lsof -i :5005  # Backend
lsof -i :5173  # Frontend (Vite)

# Windows
netstat -ano | findstr :5005
netstat -ano | findstr :5173
```

### Frontend not connecting to backend
```bash
# Check CORS configuration in Aura.Api/Program.cs
# Ensure localhost:5173 is allowed in development

# Check backend is running
curl http://localhost:5005/health/live
```

### Clean slate
```bash
cd Aura.Web
rm -rf node_modules dist
npm install

cd ../Aura.Desktop
rm -rf node_modules
npm install

cd ../Aura.Api
dotnet clean
dotnet restore
```

## VS Code

### Debugging

**Electron App:**
- Use Electron DevTools (Menu → View → Toggle Developer Tools)
- Renderer process: Standard browser debugging
- Main process: See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md)

**Component Mode:**
1. Press `F5` or use Debug panel
2. Select configuration:
   - "Launch API (Local)" - Debug backend
   - "Launch Web (Chrome)" - Debug frontend in browser

### Recommended Extensions

Install all recommended extensions:
`Ctrl+Shift+P` → "Extensions: Show Recommended Extensions"

## Git Workflow

```bash
# Create feature branch
git checkout -b feature/your-feature

# Make changes and commit
git add .
git commit -m "feat: your feature description"

# Run tests before pushing
make test
cd Aura.Web && npm run quality-check

# Push
git push origin feature/your-feature
```

## Help

- **Full guide:** [DEVELOPMENT.md](DEVELOPMENT.md)
- **Troubleshooting:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **API docs:** http://localhost:5005/swagger
- **Issues:** GitHub Issues
- **Questions:** GitHub Discussions
