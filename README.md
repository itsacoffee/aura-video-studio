# Aura Video Studio

> **🎬 AI-Powered Video Generation Suite**

Desktop application for AI-powered video generation that transforms creative briefs into complete videos with script generation, text-to-speech synthesis, visual composition, and professional video rendering.

## 🚀 Quick Start

**Download Desktop App:**
- **Windows**: [Download Installer](https://github.com/coffee285/aura-video-studio/releases/latest) (.exe) - Windows 11 (x64) primary
- **macOS**: [Download DMG](https://github.com/coffee285/aura-video-studio/releases/latest) (Intel + Apple Silicon)
- **Linux**: [Download AppImage](https://github.com/coffee285/aura-video-studio/releases/latest) (Universal)

📦 See [INSTALLATION.md](INSTALLATION.md) for detailed installation instructions.

## Key Features

**AI-Powered Video Creation:**
- **AI Script Generation**: Transform creative briefs into structured scripts with scene timing
- **Multi-Provider TTS**: ElevenLabs, PlayHT, Windows SAPI, Piper, Mimic3
- **Visual Generation**: Stable Diffusion integration, stock images, Replicate cloud models
- **Timeline Composition**: Transitions, effects, and professional video editing
- **Hardware-Accelerated Rendering**: NVENC (NVIDIA), AMF (AMD), QuickSync (Intel)

**User Experience:**
- **Guided Mode**: Beginner-friendly workflow with sensible defaults and inline help
- **Advanced Mode**: ML Lab, deep prompt customization, expert render controls
- **Real-time Progress**: Server-Sent Events for live pipeline updates
- **Provider Profiles**: Automatic fallback and retry logic

**Performance & Reliability:**
- FFmpeg-based rendering pipeline with hardware acceleration
- Structured logging and diagnostics
- Preflight validation and post-export integrity checks

## High-Level Architecture

Aura Video Studio is built as an **Electron desktop application**:

```
┌──────────────────────────────────────┐
│     Electron Main Process            │
│  (Node.js, window mgmt, lifecycle)   │
│  Entry: electron/main.js             │
└────┬─────────────────────┬───────────┘
     │                     │
     │ spawns              │ IPC
     ▼                     ▼
┌──────────────┐    ┌─────────────────┐
│  ASP.NET     │◄───┤   Renderer      │
│  Backend     │ HTTP│   Process       │
│  (child)     │───►│  (React UI)     │
└──────────────┘    └─────────────────┘
```

**Technology Stack:**

- **Frontend**: React 18 + TypeScript + Vite 6.4.1 + Fluent UI
  - State: Zustand 5.0.8
  - Routing: React Router 6
  - HTTP: Axios with circuit breaker
  - Testing: Vitest 3.2.4, Playwright 1.56.0
  
- **Backend**: ASP.NET Core 8 (Minimal API + Controllers)
  - .NET 8 SDK with nullable reference types
  - Serilog structured logging
  - RESTful API + Server-Sent Events
  
- **Core Library**: .NET 8 class library
  - Business logic and orchestration
  - FFmpeg command builder and pipeline
  - Hardware detection and optimization
  
- **Provider System**: Modular integrations
  - **LLM**: OpenAI, Anthropic, Google Gemini, Ollama, RuleBased
  - **TTS**: ElevenLabs, PlayHT, Windows SAPI, Piper, Mimic3
  - **Images**: Stable Diffusion WebUI, stock, Replicate
  - **Video**: FFmpeg with hardware acceleration

**Primary Workflows:**

1. **Brief** → User provides creative brief (topic, audience, goal, tone)
2. **Plan** → AI generates script with scenes and timing
3. **Voice** → Configure voice settings and TTS provider
4. **Generate** → Execute full video generation pipeline
5. **Monitor** → Real-time progress updates via SSE
6. **Export** → Download finished video with subtitles

## System Requirements

**Minimum:**
- **OS**: Windows 11 (x64), macOS 10.15+, or Linux (x64)
- **CPU**: Quad-core processor
- **RAM**: 8GB
- **Disk**: 5GB free space
- **Network**: Internet connection for cloud providers

**Recommended:**
- **OS**: Windows 11 (x64)
- **CPU**: 8+ cores
- **RAM**: 16GB+
- **GPU**: NVIDIA RTX 20-series+ or AMD Radeon with hardware encoding
- **Disk**: 10GB+ SSD

**Development Prerequisites:**
- Node.js 20.0.0+ (20.x recommended via .nvmrc)
- npm 9.0.0+
- .NET 8 SDK
- Git with long paths enabled
- FFmpeg 4.0+ for video rendering

## Development Setup

### Electron Desktop Development (Primary Workflow)

The canonical, production-like development experience:

```bash
# 1. Install dependencies
cd Aura.Web && npm install
cd ../Aura.Desktop && npm install

# 2. Build frontend
cd ../Aura.Web && npm run build:prod

# 3. Build backend
cd ../Aura.Api && dotnet build

# 4. Run in Electron (development mode)
cd ../Aura.Desktop && npm run dev
```

This runs the complete application stack including Electron IPC, native dialogs, window management, tray integration, and backend lifecycle.

📖 See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for comprehensive Electron development instructions.

### Browser-Only Development Mode (Component Testing)

For rapid frontend/backend iteration without Electron (development aid only):

```bash
# Terminal 1: Start backend API
cd Aura.Api && dotnet run
# API available at http://localhost:5005

# Terminal 2: Start Vite dev server
cd Aura.Web && npm run dev
# Web UI at http://localhost:5173 with hot reload
```

⚠️ **Limitations**: Does not exercise Electron IPC, tray, protocol, window management, or shutdown behavior. Use only for quick UI/API component development. Always perform final testing in the Electron app.

📖 See [DEVELOPMENT.md](DEVELOPMENT.md) for backend/frontend component development.

## Database Migrations

Aura uses Entity Framework Core migrations to manage database schema. The database schema is automatically updated on API startup, and you can also manage migrations using the CLI.

### Automatic Migrations

When you start the Aura API, it automatically checks for and applies any pending database migrations. You'll see log messages like:

```
Checking for pending database migrations...
Found 3 pending migration(s). Applying migrations...
✓ Database migrations applied successfully
```

### CLI Commands

Manage migrations manually using the Aura CLI:

```bash
# Check migration status
aura-cli status

# Apply pending migrations
aura-cli migrate

# Reset database (⚠️ deletes all data)
aura-cli reset --force
```

### Documentation

- **[User Guide](docs/DATABASE_MIGRATIONS_USER_GUIDE.md)** - How to use migration CLI commands
- **[Developer Guide](docs/DATABASE_MIGRATIONS_DEVELOPER_GUIDE.md)** - How to create new migrations

### Quick Reference

```bash
# After pulling new code
aura-cli status              # Check for new migrations
aura-cli migrate             # Apply them

# Creating a new migration (developers)
dotnet ef migrations add YourMigrationName \
    --project Aura.Api/Aura.Api.csproj \
    --context AuraDbContext

# Troubleshooting
aura-cli migrate -v          # Verbose output
aura-cli reset --dry-run     # See what reset would do
```

## Testing

**Quick Start - All Tests:**
```bash
# Run all tests with coverage
./scripts/test-local.sh

# Run only .NET tests
./scripts/test-local.sh --dotnet-only

# Run only frontend tests
./scripts/test-local.sh --frontend-only

# Run with E2E tests
./scripts/test-local.sh --e2e
```

**Individual Test Suites:**

```bash
# .NET Tests (Unit & Integration)
dotnet test --collect:"XPlat Code Coverage"

# Frontend Tests (Vitest)
cd Aura.Web && npm test

# E2E Tests (Playwright)
cd Aura.Web && npm run playwright
```

📖 See [docs/development/testing.md](docs/development/testing.md) for comprehensive testing guide.

## Documentation

**Essential:**
- [README](README.md) - This file
- [INSTALLATION.md](INSTALLATION.md) - Install the desktop app
- [FIRST_RUN_GUIDE.md](FIRST_RUN_GUIDE.md) - Initial configuration
- [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) - Electron desktop development
- [BUILD_GUIDE.md](BUILD_GUIDE.md) - Build from source
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common issues

**Complete Documentation:**
- [Documentation Index](docs/DocsIndex.md) - Complete map of all documentation
- [Changelog](docs/CHANGELOG.md) - Major fixes and improvements
- [User Guides](docs/user-guide/) - Feature-specific end-user docs
- [Developer Docs](docs/development/) - Contributor setup, testing, CI/CD
- [Architecture](docs/architecture/) - System design and component diagrams
- [API Reference](docs/api/) - REST API and SSE event documentation
- [Providers](docs/providers/) - LLM, TTS, image, and video provider configuration
- [Operations](docs/operations/) - Deployment, configuration, logging, monitoring
- [Archive](docs/archive/) - Historical implementation and fix summaries

## Contributing

We welcome contributions! Please:
- Read [CONTRIBUTING.md](CONTRIBUTING.md) for development workflow and standards
- Discuss significant features via issues first
- Follow code style and the **zero-placeholder policy** (no TODO/FIXME comments)
- Include tests where practical
- Run quality checks before submitting PRs

**Zero-Placeholder Policy:**
All committed code must be production-ready with no TODO, FIXME, HACK, or WIP comments. This is enforced by Husky pre-commit hooks and CI. Create GitHub Issues for future work instead of code comments.

📖 See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## Code Quality

**Quality Gates:**
```bash
# Check all quality gates before committing
./scripts/check-quality-gates.sh

# See status of all gates
cat docs/development/QUALITY_GATES.md
```

**Enforced Standards:**
- TypeScript strict mode (no `any` types)
- ESLint with zero warnings (`--max-warnings 0`)
- .NET 8 with warnings as errors in Release mode
- Pre-commit hooks for placeholder scanning
- CI checks on all pull requests

## Security & Privacy

- Local loopback API by default with restrictive CORS
- API keys encrypted at rest (platform-appropriate)
- Key redaction in logs
- Privacy Mode to redact PII in diagnostics/support bundles

📖 See [SECURITY.md](SECURITY.md) for vulnerability reporting.

## License

See [LICENSE](LICENSE) for details.

---

**Questions?** Check the [Documentation Index](docs/DocsIndex.md) or file an issue.
