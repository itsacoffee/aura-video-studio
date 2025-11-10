# Aura Video Studio

> **🎬 AI-Powered Video Generation Studio**

Create high-quality videos fast with an AI-first workflow designed for everyone. Aura Video Studio combines a beginner-friendly Guided Mode with an Advanced Mode for power users—unlocking a full ML Lab, deep prompt customization, expert render controls, and more.

## 🚀 Download Desktop App

**Get started in minutes with our one-click installers:**

- **Windows**: [Download Installer](https://github.com/coffee285/aura-video-studio/releases/latest) (.exe)
- **macOS**: [Download DMG](https://github.com/coffee285/aura-video-studio/releases/latest) (Intel + Apple Silicon)
- **Linux**: [Download AppImage](https://github.com/coffee285/aura-video-studio/releases/latest) (Universal)

📦 See [INSTALLATION.md](INSTALLATION.md) for detailed installation instructions.

---

## What makes Aura different

- Beginner-first Guided Mode
  - Start-to-finish flow with sensible defaults
  - “Why this choice?” hints and inline help
  - Explain/improve buttons on every AI step

- Advanced Mode (for power users)
  - ML Lab: annotate frames and retrain “frame importance” on your own content
  - Deep prompt customization and templates
  - Expert render flags, motion graphics recipes, and chroma key tools
  - Provider tuning, strict structured outputs, and reliability controls

- Reliability by design
  - Provider profiles with fallback and auto-retry
  - Strict schemas on LLM outputs to prevent bad downstream states
  - Structured logging, diagnostics, and support bundles

- Performance-focused editing
  - Responsive timeline and scrubbing
  - Proxy media and cached waveforms/thumbnails (configurable)
  - Export presets with preflight validation and post-export integrity checks

## Architecture

- **Aura.Desktop** — Electron desktop app with native installers (Windows, macOS, Linux)
- **Aura.Web** — React + TypeScript + Vite UI
- **Aura.Api** — ASP.NET Core backend (REST + SSE)
- **Aura.Core** — Domain, orchestration, models, validation, rendering plan
- **Aura.Providers** — LLM, TTS, images, video, planner providers
- **Aura.Cli** — Cross-platform CLI for headless testing and automation

## Installation Options

### Option 1: Desktop App (Recommended for Users)

**No command-line knowledge required!**

1. Download the installer for your platform from [Releases](https://github.com/coffee285/aura-video-studio/releases/latest)
2. Run the installer
3. Follow the setup wizard
4. Start creating videos!

See [INSTALLATION.md](INSTALLATION.md) for detailed instructions.

### Option 2: Development Setup

## Quick start (Development)

### One-Command Setup

Get a complete local development environment running in minutes:

```bash
# 1. Run the setup script
./scripts/setup-local.sh   # Linux/macOS
# OR
.\scripts\setup-local.ps1  # Windows PowerShell

# 2. Start all services (API, Web, Redis, FFmpeg)
make dev

# 3. Open your browser to http://localhost:3000
```

That's it! The setup script checks prerequisites, installs dependencies, and configures everything automatically.

### What You Get

- ✅ **API** running at `http://localhost:5005` with health checks
- ✅ **Web UI** at `http://localhost:3000` with hot reload
- ✅ **Redis** for caching and sessions
- ✅ **FFmpeg** container for video rendering
- ✅ **SQLite database** with test data
- ✅ **Structured logging** to `./logs/`

### Common Commands

```bash
make help         # Show all available commands
make logs         # View all service logs
make health       # Check service health
make stop         # Stop all services
make clean        # Remove all containers and data
make test         # Run all tests
```

### Manual Setup (Alternative)

If you prefer to run services individually:

```bash
# Backend API
cd Aura.Api
dotnet restore
dotnet run

# Web UI (in a new terminal)
cd Aura.Web
npm ci
npm run dev

# Open http://localhost:5173
```

**Note:** Manual setup requires Redis and FFmpeg installed locally. See [DEVELOPMENT.md](DEVELOPMENT.md) for details.

### Next Steps

- **For Users:**
  - 📥 [INSTALLATION.md](INSTALLATION.md) - Install the desktop app
  - 🎓 [FIRST_RUN_GUIDE.md](FIRST_RUN_GUIDE.md) - Initial configuration
  - 🎬 Start creating your first video!

- **For Developers:**
  - 📖 [DEVELOPMENT.md](DEVELOPMENT.md) - Architecture and workflows
  - 🖥️ [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) - Desktop app development
  - 🔧 [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common issues

## Quick start (End users)

- Download the portable release ZIP (Windows) from GitHub Releases
- Extract to a folder (e.g., C:\AuraStudio)
- Run Aura and follow the Guided Mode onboarding

## Advanced Mode

Advanced Mode reveals expert features that assume familiarity with video tooling and ML concepts.

- How to enable
  - Settings → “Advanced Mode” toggle
  - Changes are reversible anytime

- What it unlocks
  - ML Lab (frame importance retraining): annotate frames, train, deploy, and rollback
  - Deep prompt controls and templates
  - Expert render flags and motion/chroma tools
  - Provider tuning and health views

- Safety & resource notes
  - Preflight checks run before training
  - Model swaps are atomic with backups and an easy “Revert to default”

## Provider profiles

Use built-in profiles to balance cost, quality, and offline use:
- Free-Only: no API keys required; deterministic/scripted path
- Balanced Mix: use pro providers when available with free fallbacks
- Pro-Max: premium providers end-to-end (API keys required)

## Security & privacy

- Local loopback API by default, restrictive CORS, and key redaction in logs
- Keys encrypted at rest (platform-appropriate)
- Privacy Mode to redact PII in diagnostics/support bundles

## Testing & CI

Aura Video Studio has comprehensive test coverage across all layers:

### Running Tests Locally

**Quick Start - All Tests:**
```bash
# Run all tests with coverage (recommended)
./scripts/test-local.sh

# Run only .NET tests
./scripts/test-local.sh --dotnet-only

# Run only frontend tests
./scripts/test-local.sh --frontend-only

# Run with E2E tests
./scripts/test-local.sh --e2e
```

**Individual Test Suites:**

**.NET Tests (Unit & Integration):**
```bash
# Run all .NET tests with coverage
dotnet test Aura.Tests/Aura.Tests.csproj --collect:"XPlat Code Coverage"

# Generate HTML coverage report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html"
# Open TestResults/CoverageReport/index.html
```

**Frontend Tests (Vitest):**
```bash
cd Aura.Web

# Run once
npm test

# Watch mode (auto-rerun on changes)
npm run test:watch

# With coverage
npm run test:coverage
# Open coverage/index.html
```

**E2E Tests (Playwright):**
```bash
cd Aura.Web

# Install browsers (first time only)
npx playwright install --with-deps chromium

# Run tests
npm run playwright

# Interactive mode (debug tests)
npm run playwright:ui

# View report
npx playwright show-report
```

### Continuous Integration

CI runs automatically on every pull request and push to main/develop branches:

- **`.github/workflows/ci-unified.yml`** - Main CI pipeline
  - .NET Build & Test (Linux-compatible projects)
  - Frontend Build, Lint & Test
  - E2E Tests (Playwright with full browser automation)
  - Coverage collection and artifact upload

**CI Artifacts:**
- Test results (TRX, JUnit formats)
- Coverage reports (Cobertura XML + HTML)
- Playwright HTML report with traces and videos
- Built frontend assets

**CI Features:**
- NuGet package caching for faster builds
- Parallel test execution where safe
- Continue-on-error for non-critical failures
- Comprehensive summary in GitHub Actions UI

See the [CI Platform Requirements](CI_PLATFORM_REQUIREMENTS.md) for details on cross-platform builds.

## Roadmap highlights

- Advanced Mode gating
- ML Lab: annotation + training + rollback UX
- Provider health + circuit breakers + strict schema validation
- Proxy media, waveform/thumbnail caching for smooth editing
- Export presets, preflight validation, and post-export integrity
- Observability: correlation IDs, diagnostics, and support bundles

## Contributing

We welcome contributions! Please:
- Discuss significant features via issues first
- Follow code style and docs standards
- Include tests where practical

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## Documentation

Comprehensive documentation is available to help you get started and make the most of Aura Video Studio:

- **[Documentation Index](docs/DocsIndex.md)** - Complete map of all documentation
- **[Getting Started Guide](docs/getting-started/QUICK_START.md)** - Quick setup and first video
- **[First Run Guide](FIRST_RUN_GUIDE.md)** - Initial configuration walkthrough
- **[Build Guide](BUILD_GUIDE.md)** - Build from source
- **[User Guides](docs/user-guide/)** - Feature-specific end-user documentation
- **[API Reference](docs/api/)** - REST API and SSE event documentation
- **[Architecture](docs/architecture/)** - System design and technical details
- **[Style Guide](docs/style/DocsStyleGuide.md)** - Documentation standards

For a complete list of guides organized by audience (end users, developers, operations) and topic, see [docs/DocsIndex.md](docs/DocsIndex.md).

## License

See [LICENSE](LICENSE).
