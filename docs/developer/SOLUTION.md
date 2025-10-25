# Aura Video Studio - Solution Structure

This document describes the implemented solution structure for Aura Video Studio.

## Project Overview

Aura Video Studio is a Windows 11 desktop application built with WinUI 3 for creating complete YouTube videos from a simple user brief. The application follows a modular architecture with clear separation of concerns.

## Solution Structure

```
Aura.sln
├── Aura.App/              # WinUI 3 Desktop Application
│   ├── ViewModels/        # MVVM ViewModels using CommunityToolkit.Mvvm
│   │   ├── CreateViewModel.cs
│   │   ├── StoryboardViewModel.cs
│   │   ├── RenderViewModel.cs
│   │   ├── PublishViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   └── HardwareProfileViewModel.cs
│   ├── App.xaml(.cs)      # Application entry point with DI configuration
│   └── MainWindow.xaml(.cs)
│
├── Aura.Core/             # Core Business Logic (Class Library)
│   ├── Models/            # Data models and enums
│   │   ├── Models.cs      # Brief, Scene, PlanSpec, etc.
│   │   └── Enums.cs       # Pacing, Density, Aspect, etc.
│   ├── Providers/         # Provider interfaces
│   │   └── IProviders.cs  # ILlmProvider, ITtsProvider, etc.
│   ├── Orchestrator/      # Pipeline orchestration
│   │   └── VideoOrchestrator.cs
│   ├── Timeline/          # Timeline and subtitle generation
│   │   └── TimelineBuilder.cs
│   ├── Rendering/         # Render configuration
│   │   └── RenderPresets.cs
│   ├── Hardware/          # Hardware detection and profiling
│   │   └── HardwareDetector.cs
│   └── Dependencies/      # Dependency management
│       └── DependencyManager.cs
│
├── Aura.Providers/        # Provider Implementations (Class Library)
│   ├── Llm/               # Language model providers
│   │   └── RuleBasedLlmProvider.cs
│   ├── Tts/               # Text-to-speech providers
│   │   └── WindowsTtsProvider.cs
│   └── Video/             # Video composition
│       └── FfmpegVideoComposer.cs
│
├── Aura.Tests/            # Unit Tests (xUnit)
│   ├── ModelsTests.cs
│   ├── RuleBasedLlmProviderTests.cs
│   ├── RenderPresetsTests.cs
│   └── TimelineBuilderTests.cs
│
├── Aura.E2E/              # End-to-End Tests (xUnit)
│
└── scripts/
    └── ffmpeg/            # FFmpeg binaries (user must download)
        └── README.md
```

## Implementation Status

### ✅ Completed Components

#### Core Infrastructure
- **Solution File**: Aura.sln with all projects configured
- **Project Files**: All .csproj files created with proper dependencies
- **Configuration**: appsettings.json with comprehensive settings
- **.gitignore**: Proper exclusions for build artifacts and large files

#### Core Business Logic (Aura.Core)
- **Models**: Complete data model definitions (Brief, Scene, PlanSpec, VoiceSpec, etc.)
- **Enums**: Pacing, Density, Aspect, PauseStyle, HardwareTier
- **VideoOrchestrator**: Full pipeline implementation (Script → TTS → Assets → Render)
- **TimelineBuilder**: Scene timing calculation, subtitle generation (SRT/VTT)
- **RenderPresets**: YouTube, Instagram, and custom render configurations
- **HardwareDetector**: System profiling with tier classification
- **DependencyManager**: Download management with SHA-256 verification

#### Provider Implementations (Aura.Providers)
- **RuleBasedLlmProvider**: Template-based script generation (no API required)
- **WindowsTtsProvider**: Windows SAPI voice synthesis with conditional compilation
- **FfmpegVideoComposer**: Video rendering pipeline

#### ViewModels (Aura.App)
- **CreateViewModel**: Brief creation and video generation workflow
- **StoryboardViewModel**: Timeline editing state management
- **RenderViewModel**: Render settings and progress tracking
- **PublishViewModel**: YouTube metadata and upload
- **SettingsViewModel**: Application configuration
- **HardwareProfileViewModel**: Hardware detection and profiling

#### Testing
- **465 .NET Unit Tests**: All passing (Aura.Tests)
  - Models validation
  - Provider implementations and fallback chains
  - RenderPresets configuration
  - TimelineBuilder timing and subtitles
  - API contract and serialization
  - Orchestrator workflows
  - Hardware detection and probes
- **51 Web Tests**: All passing (Vitest + React Testing Library)
  - Wizard workflows
  - Provider selection UI
  - Timeline state management
  - Engine management
- **E2E Integration Tests**: Complete (Aura.E2E)
  - Smoke tests for Free and Local modes
  - Complete workflow validation
  - Provider selection and fallback
  - Dependency management
- **CI/CD Workflows**: Active on GitHub Actions
  - Linux build and test workflow
  - Windows build and package workflow
  - Placeholder detection workflow

### ✅ Provider Implementations (All Complete)

#### LLM Providers
- **RuleBasedLlmProvider**: Template-based script generation (always available, no API required)
- **OllamaLlmProvider**: Local LLM support via Ollama API
- **OpenAiLlmProvider**: GPT-4/GPT-3.5 via OpenAI API
- **AzureOpenAiLlmProvider**: Azure OpenAI service integration
- **GeminiLlmProvider**: Google Gemini API integration

#### TTS Providers
- **WindowsTtsProvider**: Windows SAPI voice synthesis (conditional compilation for Windows)
- **MockTtsProvider**: CI/Linux testing provider
- **PiperTtsProvider**: Local TTS via Piper CLI
- **Mimic3TtsProvider**: Local TTS via Mimic3 HTTP server
- **ElevenLabsTtsProvider**: ElevenLabs cloud TTS
- **PlayHTTtsProvider**: PlayHT cloud TTS

#### Image Providers
- **LocalStockProvider**: User folder images
- **PixabayStockProvider**: Free stock images via Pixabay API
- **PexelsStockProvider**: Free stock images via Pexels API
- **UnsplashStockProvider**: Free stock images via Unsplash API
- **StableDiffusionWebUiProvider**: Local image generation via SD WebUI (NVIDIA-only with VRAM gating)

#### Video Composer
- **FfmpegVideoComposer**: Complete video rendering pipeline with multi-encoder support

### ✅ UI Architecture (Web-Based)

The application now uses a **React + Vite + TypeScript + Fluent UI** web frontend:
- **Aura.Api**: ASP.NET Core backend with RESTful endpoints
- **Aura.Web**: Modern React SPA with:
  - Wizard workflow for video creation
  - Settings pages for API keys and providers
  - Download Center for engine management
  - Timeline editor with state management
  - Provider selection UI with validation
  - Preflight checks and health monitoring
- **Aura.App**: WinUI 3 desktop app (coexists as alternative UI)

## Building the Solution

### Prerequisites
- .NET 8.0 SDK
- Windows 10 SDK 10.0.19041.0 or later (for WinUI 3)
- Visual Studio 2022 or later (recommended)

### Build Commands

```bash
# Restore packages
dotnet restore

# Build all projects
dotnet build Aura.sln

# Build individual projects
dotnet build Aura.Core/Aura.Core.csproj
dotnet build Aura.Providers/Aura.Providers.csproj
dotnet build Aura.Tests/Aura.Tests.csproj

# Run tests
dotnet test Aura.Tests/Aura.Tests.csproj
```

### Platform Notes

- **Aura.App**: Requires Windows for WinUI 3 compilation
- **Aura.Core**: Cross-platform compatible
- **Aura.Providers**: Multi-targeted (net8.0 and net8.0-windows10.0.19041.0)
  - Windows-specific TTS code uses conditional compilation
- **Tests**: Can run on any platform with .NET 8.0

## Key Design Decisions

### 1. MVVM Architecture
- Uses CommunityToolkit.Mvvm for clean separation
- Observable properties and relay commands
- ViewModels contain no UI-specific code

### 2. Dependency Injection
- Microsoft.Extensions.Hosting for DI container
- Services registered in App.xaml.cs
- Supports provider swapping (Free vs Pro)

### 3. Provider Abstraction
- Clean interfaces for all external services
- Multiple implementations per interface
- Easy to add new providers without changing core logic

### 4. Hardware Awareness
- Auto-detection with manual override
- Tier-based defaults (A/B/C/D)
- NVIDIA-only policy for local diffusion

### 5. Testing Strategy
- Comprehensive unit tests for business logic
- Deterministic tests with fixed random seeds
- Integration tests for provider implementations

## Configuration

See `appsettings.json` for complete configuration schema including:
- Provider settings (LLM, TTS, Images, Video)
- Hardware detection and overrides
- Download targets and locations
- Render presets
- Brand customization

## Dependencies

### Core Dependencies
- .NET 8.0
- Microsoft.Extensions.* (Logging, DI, Hosting)
- System.Management (Windows hardware detection)

### UI Dependencies (Aura.App)
- Microsoft.WindowsAppSDK 1.5
- CommunityToolkit.Mvvm 8.2

### Test Dependencies
- xUnit
- FluentAssertions (recommended)

## License

See LICENSE file for details.

## Contributing

This project follows the specification in README.md. All contributions should:
1. Follow the existing architecture patterns
2. Include unit tests
3. Maintain cross-platform compatibility where possible
4. Document Windows-specific requirements

## Next Steps

1. Implement XAML Views and Controls
2. Add remaining provider implementations
3. Create E2E test suite
4. Set up CI/CD pipeline
5. Package as MSIX
6. Create installation guide
