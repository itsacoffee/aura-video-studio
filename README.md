# Aura Video Studio

Create high-quality videos fast with an AI-first workflow designed for everyone. Aura Video Studio combines a beginner-friendly Guided Mode with an Advanced Mode for power users—unlocking a full ML Lab, deep prompt customization, expert render controls, and more.

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

- Aura.Web — React + TypeScript + Vite UI
- Aura.Api — ASP.NET Core backend (REST + SSE)
- Aura.Core — domain, orchestration, models, validation, rendering plan
- Aura.Providers — LLM, TTS, images, video, planner providers
- Aura.Cli — cross-platform CLI for headless testing and automation

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

- 📖 Read [DEVELOPMENT.md](DEVELOPMENT.md) for architecture and workflows
- 🔧 Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) if you encounter issues
- 🚀 See [FIRST_RUN_GUIDE.md](FIRST_RUN_GUIDE.md) for initial configuration

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

- Unit and integration tests for core systems and providers
- Playwright E2E for critical UI flows
- CI runs on every PR: .NET build/test, web lint/build/test, E2E, docs build, markdown lint, and link checks

See the CI workflow in `.github/workflows/ci.yml`.

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
