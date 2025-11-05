# Aura Video Studio - Architecture Overview

## Introduction

Aura Video Studio is a Windows 11 desktop application for creating AI-powered videos. The application follows a **web-based UI architecture** hosted inside native Windows shells, allowing cross-platform development while delivering a native Windows experience.

## Architecture Components

### 1. Aura.Core (Business Logic)
**Technology**: .NET 8 Class Library  
**Purpose**: Platform-agnostic business logic, models, and orchestration

**Key Components**:
- `Models/` - Data models (Brief, PlanSpec, Scene, etc.)
- `Hardware/` - Hardware detection and capability tiering
- `Orchestrator/` - Video generation pipeline orchestration
- `Rendering/` - FFmpeg plan building
- `Providers/` - Provider interfaces (LLM, TTS, Image, Video)
- `Dependencies/` - Dependency manager with SHA-256 verification

**Platforms**: Linux (dev/CI), Windows (production)

### 2. Aura.Providers (Provider Implementations)
**Technology**: .NET 8 Class Library  
**Purpose**: Concrete implementations of provider interfaces

**Free Providers** (no API keys):
- `RuleBasedLlmProvider` - Template-based script generation
- `WindowsTtsProvider` - Windows SAPI text-to-speech
- `FfmpegVideoComposer` - Local FFmpeg rendering
- Stock providers (Pexels, Pixabay, Unsplash)

**Pro Providers** (require API keys):
- `OpenAiLlmProvider` - GPT-4/3.5 via OpenAI API
- ElevenLabs/PlayHT TTS (planned)
- Azure OpenAI, Gemini (planned)

**Platforms**: Linux (dev/CI with mocks), Windows (full functionality)

### 3. Aura.Api (Backend API)
**Technology**: ASP.NET Core 8 Minimal API  
**Purpose**: RESTful API backend for the web UI

**Endpoints**:
- `GET /healthz` - Health check
- `GET /capabilities` - Hardware detection results
- `POST /plan` - Create timeline plan
- `POST /script` - Generate script from brief
- `POST /tts` - Synthesize narration
- `GET /downloads/manifest` - Dependency manifest
- `POST /settings/save`, `GET /settings/load` - Settings persistence

**Additional Planned**:
- `/assets/search`, `/assets/generate` - Asset management
- `/compose`, `/render` - Video composition and rendering
- `/queue` - Render queue management
- `/logs/stream` - Live log streaming (SSE)

**Configuration**:
- Runs on `http://127.0.0.1:5005` by default
- Uses Serilog for structured logging
- Integrates with Aura.Core and Aura.Providers

**Platforms**: Linux (dev/CI), Windows (production)

### 4. Aura.Web (Frontend UI)
**Technology**: React 18 + Vite + TypeScript + Fluent UI React  
**Purpose**: Modern web-based user interface

**Key Features**:
- Fluent UI React components for Windows 11 look and feel
- TypeScript for type safety
- Vite for fast development and optimized builds
- Proxy configuration to forward API calls to Aura.Api

**Planned Views**:
- Create Wizard (6 steps)
- Storyboard & Timeline Editor
- Render Queue
- Publish/Upload
- Settings & Hardware Profile
- Download Center

**Development**:
- `npm run dev` - Development server on port 5173
- `npm run build` - Production build to `dist/`

**Platforms**: Linux (dev/CI), Windows (production)

### 5. Aura.Host.Win (Windows Shells) - **Future Consideration**
**Technology**: Native Windows shells (concept only)
**Purpose**: Would provide native Windows shells that host the web UI via WebView2

**Note**: These are concepts for future exploration. Current distribution follows a portable-only model using the web browser as the primary interface. Native shell variants would need to align with the portable-only distribution policy.

**Platforms**: Windows only (if implemented)

### 6. Aura.App (Legacy WinUI 3 App)
**Technology**: WinUI 3 + XAML  
**Purpose**: Original standalone WinUI 3 application (legacy)

**Status**: This was the original desktop application. The project has transitioned to a web-based architecture with portable distribution. The web-based approach provides better cross-platform development and simpler deployment.

## Data Flow

```
User Interaction
    ↓
Aura.Web (React UI)
    ↓ HTTP
Aura.Api (ASP.NET Core)
    ↓ In-process calls
Aura.Core (Business Logic)
    ↓
Aura.Providers (LLM, TTS, Video, etc.)
    ↓
External Services / Local Tools
```

## Deployment Scenarios

### Development (Linux/Windows)
```
Developer runs:
1. dotnet run --project Aura.Api  (Terminal 1)
2. npm run dev  (Terminal 2, in Aura.Web/)
3. Opens browser to http://localhost:5173
```

### Production - Portable ZIP (Only Supported Format)
```
User extracts:
- AuraVideoStudio_Portable_x64.zip to any folder

User runs:
- Launch.bat to start the API and open browser
- Self-contained, no installation needed
- No system changes or registry modifications
```

**Distribution Policy**: Aura Video Studio follows a portable-only distribution model. MSIX/APPX packages and traditional installers are not supported.

## Platform Strategy

### Linux (Development & CI)
- **Purpose**: Cross-platform development, automated testing
- **What Works**: Aura.Core, Aura.Api, Aura.Web, unit tests
- **What Doesn't**: Windows-specific providers (Windows TTS, hardware detection details)
- **Strategy**: Use mocks and stubs for Windows-only features

### Windows (Production)
- **Purpose**: Final deployment target
- **What Works**: Everything, including full provider support and hardware acceleration
- **Requirements**: Windows 10/11 x64, Modern web browser (Chrome, Edge, Firefox recommended)
- **Distribution**: Portable ZIP only - no installation required

## Build & CI Strategy

### ci-linux.yml
- Runs on `ubuntu-latest`
- Builds Aura.Core, Aura.Providers, Aura.Api
- Builds Aura.Web (npm install + build)
- Runs unit tests
- Starts API and tests basic functionality
- Produces build artifacts

### ci-windows.yml
- Runs on `windows-latest`
- Builds all .NET projects
- Runs unit tests
- Builds portable ZIP distribution
- Generates checksums (SHA-256)
- Uploads portable artifacts and test results
- Enforces portable-only policy (no MSIX/installer packaging)

## Security Considerations

### API Keys
- Stored in `%LOCALAPPDATA%\Aura\settings.json`
- **Windows**: Encrypted with DPAPI (planned)
- **Linux dev**: Plaintext in `~/.aura-dev/` with warnings

### Code Signing
- Portable distributions can be signed with PFX certificate (optional)
- Certificate stored in GitHub Secrets for CI/CD
- Unsigned builds clearly marked in documentation

### Browser Security
- Application served over localhost HTTP (127.0.0.1:5005)
- CORS restricted to localhost only
- Uses standard browser security sandbox
- HTTPS-only for external API calls (OpenAI, etc.)

## Directory Structure

```
aura-video-studio/
├── Aura.Core/              # Business logic (.NET 8)
├── Aura.Providers/         # Provider implementations (.NET 8)
├── Aura.Api/               # Backend API (ASP.NET Core 8)
├── Aura.Web/               # Frontend UI (React + Vite)
├── Aura.Tests/             # Unit tests
├── Aura.E2E/               # Integration tests
├── scripts/
│   ├── ffmpeg/             # FFmpeg binaries
│   ├── packaging/          # Build scripts (Portable ZIP only)
│   └── cleanup/            # Cleanup scripts and CI guard
├── .github/workflows/
│   ├── ci-linux.yml        # Linux CI
│   └── ci-windows.yml      # Windows CI + packaging
└── artifacts/              # Build outputs (created during build)
    └── windows/
        ├── portable/       # Portable ZIP distribution
        ├── checksums.txt
        ├── sbom.json
        └── attributions.txt
```

## Technology Stack Summary

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Aura.Core | .NET 8 | Business logic |
| Aura.Providers | .NET 8 | Provider implementations |
| Aura.Api | ASP.NET Core 8 | Backend API |
| Aura.Web | React 18 + TypeScript | Frontend UI |
| UI Framework | Fluent UI React | Windows 11 design system |
| Build Tool | Vite | Fast bundling |
| Packaging | Portable ZIP | No-install distribution |
| Video | FFmpeg | Rendering engine |
| Audio | NAudio | DSP and mixing |
| Logging | Serilog | Structured logging |
| Testing | xUnit | Unit tests |

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- (Windows only) Visual Studio 2022 or Build Tools
- (Windows only) Windows 11 SDK

### Development Workflow

```bash
# 1. Clone repository
git clone https://github.com/Coffee285/aura-video-studio.git
cd aura-video-studio

# 2. Restore .NET dependencies
dotnet restore

# 3. Install npm dependencies
cd Aura.Web
npm install
cd ..

# 4. Start API (Terminal 1)
dotnet run --project Aura.Api

# 5. Start Web UI (Terminal 2)
cd Aura.Web
npm run dev

# 6. Open browser to http://localhost:5173
```

### Building for Production (Windows)

```powershell
# Run the build script
.\scripts\packaging\build-all.ps1

# Generate SBOM and attributions
.\scripts\packaging\generate-sbom.ps1

# Output in artifacts/windows/
```

## Support & Contact

- **Issues**: https://github.com/Coffee285/aura-video-studio/issues
- **Documentation**: See README.md and individual project READMEs
- **License**: See LICENSE file
