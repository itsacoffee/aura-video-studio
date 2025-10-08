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

### 5. Aura.Host.Win (Windows Shells) - **Planned**
**Technology**: WinUI 3 (packaged) + WPF (portable)  
**Purpose**: Native Windows shells that host the web UI via WebView2

**Two Variants**:

#### 5a. WinUI 3 Packaged Shell (MSIX)
- Windows App SDK
- Mica window chrome
- WebView2 for hosting Aura.Web
- Starts Aura.Api as child process
- Waits for `/healthz` then navigates to `http://127.0.0.1:5005`

#### 5b. WPF Portable Shell (EXE/ZIP)
- Classic WPF window
- WebView2 control
- Same API-hosting logic as WinUI 3
- No Windows App SDK dependency
- Self-contained deployment

**Platforms**: Windows only

### 6. Aura.App (Current WinUI 3 App)
**Technology**: WinUI 3 + XAML  
**Purpose**: Original standalone WinUI 3 application

**Status**: Functional with ViewModels and XAML views. Will coexist with new web-based architecture as an alternative UI option.

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

### Production - MSIX Package (Recommended)
```
User installs:
- AuraVideoStudio_x64.msix (via sideload or Store)

User launches:
1. WinUI 3 shell starts
2. Shell starts Aura.Api child process
3. Shell waits for API /healthz
4. WebView2 navigates to API URL
5. Aura.Web served from API static files
```

### Production - Setup EXE
```
User installs:
- AuraVideoStudio_Setup.exe (Inno Setup installer)
- Installs to C:\Program Files\AuraVideoStudio

User launches:
1. WPF shell starts (AuraVideoStudio.exe)
2. Rest same as MSIX
```

### Production - Portable ZIP
```
User extracts:
- AuraVideoStudio_Portable_x64.zip to any folder

User runs:
- Launch.bat or AuraVideoStudio.exe directly
- Self-contained, no installation needed
```

## Platform Strategy

### Linux (Development & CI)
- **Purpose**: Cross-platform development, automated testing
- **What Works**: Aura.Core, Aura.Api, Aura.Web, unit tests
- **What Doesn't**: Windows-specific providers (Windows TTS, hardware detection details)
- **Strategy**: Use mocks and stubs for Windows-only features

### Windows (Production)
- **Purpose**: Final deployment target
- **What Works**: Everything, including Windows shells, MSIX packaging, code signing
- **Requirements**: Windows 11 x64, .NET 8 Runtime, WebView2 Evergreen

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
- Builds all projects including Aura.App
- Runs unit tests
- Builds WinUI 3 packaged app (MSIX)
- Generates checksums (SHA-256)
- Optional: Signs artifacts with PFX certificate
- Uploads MSIX packages and logs

## Security Considerations

### API Keys
- Stored in `%LOCALAPPDATA%\Aura\settings.json`
- **Windows**: Encrypted with DPAPI (planned)
- **Linux dev**: Plaintext in `~/.aura-dev/` with warnings

### Code Signing
- MSIX and EXE can be signed with PFX certificate
- Certificate stored in GitHub Secrets for CI/CD
- Unsigned builds clearly marked

### WebView2
- Uses Evergreen runtime (auto-updates)
- Sandboxed JavaScript execution
- HTTPS-only for external resources (API is local HTTP)

## Future Enhancements

1. **Server-Sent Events (SSE)** for live log streaming and render progress
2. **SignalR Hub** for real-time collaboration features
3. **Electron-based Linux/macOS versions** (if demand exists)
4. **Microsoft Store submission** for MSIX distribution
5. **Auto-update mechanism** for portable distributions
6. **Telemetry and crash reporting** (with user opt-out)

## Directory Structure

```
aura-video-studio/
├── Aura.Core/              # Business logic (.NET 8)
├── Aura.Providers/         # Provider implementations (.NET 8)
├── Aura.Api/               # Backend API (ASP.NET Core 8)
├── Aura.Web/               # Frontend UI (React + Vite)
├── Aura.Host.Win/          # Windows shells (WinUI 3 + WPF) [Planned]
│   ├── Packaged/           # WinUI 3 for MSIX
│   └── Portable/           # WPF for EXE/ZIP
├── Aura.App/               # Original WinUI 3 app
├── Aura.Tests/             # Unit tests
├── Aura.E2E/               # Integration tests
├── scripts/
│   ├── ffmpeg/             # FFmpeg binaries
│   └── packaging/          # Build scripts (MSIX, EXE, ZIP)
├── .github/workflows/
│   ├── ci-linux.yml        # Linux CI
│   └── ci-windows.yml      # Windows CI + packaging
└── artifacts/              # Build outputs (created during build)
    └── windows/
        ├── msix/
        ├── exe/
        ├── portable/
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
| Windows Shell (MSIX) | WinUI 3 + WebView2 | Native MSIX app |
| Windows Shell (Portable) | WPF + WebView2 | Self-contained EXE |
| Packaging (MSIX) | Windows App SDK | Store-ready package |
| Packaging (EXE) | Inno Setup | Traditional installer |
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
