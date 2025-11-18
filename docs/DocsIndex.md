# Aura Video Studio Documentation Index

Complete map of all documentation in the Aura Video Studio repository, organized by audience and purpose.

> **Note**: This index includes only current, canonical documentation. Historical implementation notes have been archived in [docs/archive/](archive/).

## Quick Start

New to Aura Video Studio? Start here:

1. **[README.md](../README.md)** - Project overview and architecture
2. **[INSTALLATION.md](../INSTALLATION.md)** - Install the desktop app
3. **[FIRST_RUN_GUIDE.md](../FIRST_RUN_GUIDE.md)** - Initial configuration
4. **[Quick Start](getting-started/QUICK_START.md)** - Get running fast

## Essential Documentation

### For Everyone

- **[README.md](../README.md)** - Project overview, features, and quick start
- **[CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md)** - Community standards
- **[SECURITY.md](../SECURITY.md)** - Vulnerability reporting

### For Users

- **[INSTALLATION.md](../INSTALLATION.md)** - Install and setup
- **[FIRST_RUN_GUIDE.md](../FIRST_RUN_GUIDE.md)** - Initial configuration walkthrough
- **[TROUBLESHOOTING.md](../TROUBLESHOOTING.md)** - Common issues and solutions

### For Developers

- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - How to contribute
- **[BUILD_GUIDE.md](../BUILD_GUIDE.md)** - Build from source
- **[DESKTOP_APP_GUIDE.md](../DESKTOP_APP_GUIDE.md)** - Electron development
- **[DEVELOPMENT.md](../DEVELOPMENT.md)** - Component development

---

## Documentation by Topic

### 📚 Getting Started

**Installation & Setup:**
- [Installation Guide](getting-started/INSTALLATION.md)
- [Quick Start Guide](getting-started/QUICK_START.md)
- [First Run Guide](getting-started/FIRST_RUN_GUIDE.md)
- [First Run FAQ](getting-started/FIRST_RUN_FAQ.md)

### 👥 User Guides

**Core Features:**
- [User Manual](user-guide/USER_MANUAL.md) - Complete guide covering all features
- [FAQ](user-guide/FAQ.md) - Frequently asked questions
- [How to Launch](user-guide/HOW_TO_LAUNCH.md) - Launch and startup guide
- [Quick Reference](user-guide/QUICK_REFERENCE.md) - Command reference

**Configuration & Settings:**
- [Configuration Guide](user-guide/CONFIGURATION_GUIDE.md) - Application settings
- [Configuration Persistence](user-guide/CONFIGURATION_PERSISTENCE_GUIDE.md) - Saving settings
- [Configuration System](user-guide/CONFIGURATION_SYSTEM_USER_GUIDE.md) - Advanced config
- [Settings User Guide](user-guide/SETTINGS_USER_GUIDE.md) - Settings interface
- [User Customization](user-guide/USER_CUSTOMIZATION_GUIDE.md) - Personalization

**Provider Configuration:**
- [Provider Integration Guide](user-guide/PROVIDER_INTEGRATION_GUIDE.md) - Configure LLM, TTS, image providers
- [Mandatory First Run Setup](user-guide/MANDATORY_FIRST_RUN_SETUP.md) - Required configuration
- [Pricing Update Guide](user-guide/PRICING_UPDATE_GUIDE.md) - Provider pricing

**Advanced Features:**
- [Advanced Mode Guide](user-guide/ADVANCED_MODE_GUIDE.md) - Power user features
- [Advanced Mode Visual Guide](user-guide/ADVANCED_MODE_VISUAL_GUIDE.md) - Visual walkthrough
- [Guided Mode User Guide](user-guide/GUIDED_MODE_USER_GUIDE.md) - Beginner-friendly workflow
- [Model Selection UI](user-guide/MODEL_SELECTION_UI_GUIDE.md) - Choosing AI models
- [Ollama Auto-Detection](user-guide/OLLAMA_AUTO_DETECTION_VISUAL_GUIDE.md) - Local AI setup

**Content & Customization:**
- [Content Adaptation Guide](user-guide/CONTENT_ADAPTATION_GUIDE.md) - Adapt content for audiences
- [Content Safety Guide](user-guide/CONTENT_SAFETY_GUIDE.md) - Content moderation
- [Document Import Guide](user-guide/DOCUMENT_IMPORT_GUIDE.md) - Import external docs
- [Prompt Customization](user-guide/PROMPT_CUSTOMIZATION_USER_GUIDE.md) - Customize AI prompts
- [Translation User Guide](user-guide/TRANSLATION_USER_GUIDE.md) - Multi-language support
- [Windows 11 Visual Overview](user-guide/WINDOWS11_VISUAL_OVERVIEW.md) - Platform guide

**Additional Guides:**
- [Asset Library Guide](user-guide/ASSET_LIBRARY_GUIDE.md)
- [Context Management Guide](user-guide/CONTEXT_MANAGEMENT_GUIDE.md)
- [Content Verification Guide](user-guide/CONTENT_VERIFICATION_GUIDE.md)
- [Pacing Optimization Guide](user-guide/PACING_OPTIMIZATION_GUIDE.md)
- [Pipeline Validation Guide](user-guide/PIPELINE_VALIDATION_GUIDE.md)
- [Portable First Run](user-guide/PORTABLE_FIRST_RUN.md)
- [Script Enhancement Guide](user-guide/SCRIPT_ENHANCEMENT_USER_GUIDE.md)
- [Theme System Guide](user-guide/THEME_SYSTEM_GUIDE.md)
- [Timeline Editor UI](user-guide/TIMELINE_EDITOR_UI_GUIDE.md)

### 🎬 Features

**Video Generation:**
- [Script Generation Guide](features/SCRIPT_GENERATION_GUIDE.md)
- [Advanced Script Generation](features/ADVANCED_SCRIPT_GENERATION_GUIDE.md)
- [Script Refinement Guide](features/SCRIPT_REFINEMENT_GUIDE.md)
- [Video Rendering Pipeline](features/VIDEO_RENDERING_PIPELINE_GUIDE.md)
- [Advanced Rendering Guide](features/ADVANCED_RENDERING_GUIDE.md)
- [Animation System](features/ANIMATION_SYSTEM_GUIDE.md)
- [Video Editor UI Modernization](features/VIDEO_EDITOR_UI_MODERNIZATION.md)

**Media & Assets:**
- [Media Library Guide](features/MEDIA_LIBRARY_GUIDE.md)
- [Sample Assets Guide](features/SAMPLE_ASSETS_GUIDE.md)
- [RAG Guide](features/RAG_GUIDE.md) - Retrieval-Augmented Generation
- [Refinement Guide](features/REFINEMENT_GUIDE.md)

**Project Management:**
- [Project Management Setup](features/PROJECT_MANAGEMENT_SETUP_GUIDE.md)
- [Project Management Quick Reference](features/PROJECT_MANAGEMENT_QUICK_REFERENCE.md)
- [Project Versioning](features/PROJECT_VERSIONING_GUIDE.md)
- [Wizard Project Integration](features/WIZARD_PROJECT_INTEGRATION_GUIDE.md)

**Advanced Features:**
- [ML Training Backend](features/ML_TRAINING_BACKEND_GUIDE.md)
- [Licensing & Export](features/LICENSING_EXPORT_GUIDE.md)
- [Proxy Timeline Integration](features/PROXY_TIMELINE_INTEGRATION_GUIDE.md)

**Engine Documentation:**
- [Video Generation Engines](features/ENGINES.md)
- [Stable Diffusion Integration](features/ENGINES_SD.md)
- [Timeline Editor](features/TIMELINE.md)
- [Text-to-Speech & Captions](features/TTS-and-Captions.md)
- [Local TTS Providers](features/TTS_LOCAL.md)
- [Command Line Interface](features/CLI.md)

### 🔌 Providers

**Overview:**
- **[Provider System Overview](providers/README.md)** - Complete provider documentation

**LLM Providers:**
- [LLM Orchestrator Guide](providers/UNIFIED_LLM_ORCHESTRATOR_GUIDE.md)
- [LLM Cache Guide](providers/LLM_CACHE_GUIDE.md)
- [LLM Latency Management](providers/LLM_LATENCY_MANAGEMENT.md)
- [LLM Output Validation](providers/LLM_OUTPUT_VALIDATION_GUIDE.md)
- [Ollama Model Selection](providers/OLLAMA_MODEL_SELECTION.md)
- [Windows LLM Provider Testing](providers/WINDOWS_LLM_PROVIDER_TESTING_GUIDE.md)

**TTS Providers:**
- [TTS Validation Index](providers/TTS_VALIDATION_INDEX.md)
- [TTS Quick Start](providers/TTS_VALIDATION_QUICK_START.md)

**Configuration:**
- [Provider API Key Management](providers/PROVIDER_API_KEY_MANAGEMENT.md)
- [Provider Stickiness Usage](providers/PROVIDER_STICKINESS_USAGE_EXAMPLES.md)

### 🏗️ Architecture

**Overview:**
- **[Frontend Architecture](architecture/frontend.md)** - React, Zustand, routing, API patterns
- [Architecture Overview](architecture/ARCHITECTURE.md)
- [Architecture Migration Note](architecture/ARCHITECTURE_MIGRATION_NOTE.md)
- [README](architecture/README.md)

**System Design:**
- [Provider Selection Architecture](architecture/PROVIDER_SELECTION_ARCHITECTURE.md)
- [Service Initialization Order](architecture/SERVICE_INITIALIZATION_ORDER.md)
- [Local Storage Architecture](architecture/LOCAL_STORAGE_ARCHITECTURE.md)
- [Menu Event System](architecture/MENU_EVENT_SYSTEM.md)
- [Model Selection Architecture](architecture/MODEL_SELECTION_ARCHITECTURE.md)
- [Process Model](architecture/PROCESS_MODEL.md)

**Process & Lifecycle:**
- [Desktop Shutdown Model](architecture/DESKTOP_SHUTDOWN_MODEL.md)
- [Electron Process Lifecycle](architecture/ELECTRON_PROCESS_LIFECYCLE_HARDENING.md)

**UI/UX:**
- [Wizard State Machine](architecture/WIZARD_STATE_MACHINE_DIAGRAM.md)
- [Professional Wizard UX](architecture/PROFESSIONAL_WIZARD_UX.md)
- [Error Modal UI Design](architecture/ERROR_MODAL_UI_DESIGN.md)
- [Brand Kit UI](architecture/BRAND_KIT_UI.md)
- [Advanced Timeline Features](architecture/ADVANCED_TIMELINE_FEATURES.md)

**Data Flow:**
- [SSE Event Flow](architecture/SSE_EVENT_FLOW.md)
- [Error Flow Diagram](architecture/ERROR_FLOW_DIAGRAM.md)
- [FFmpeg Single Locator Flow](architecture/FFMPEG_SINGLE_LOCATOR_FLOW.md)

**Frontend Integration:**
- [Frontend Backend Integration](architecture/FRONTEND_BACKEND_INTEGRATION.md)
- [Latency Patience Policy](architecture/LATENCY_PATIENCE_POLICY.md)

**Architecture Decision Records (ADRs):**
- [ADRs Index](adr/README.md)
- [ADR-001: Monorepo Structure](adr/001-monorepo-structure.md)
- [ADR-002: ASP.NET Core Backend](adr/002-aspnet-core-backend.md)
- [ADR-006: Server-Sent Events](adr/006-server-sent-events.md)
- [ADR-009: Secrets Encryption](adr/009-secrets-encryption.md)

### 🔧 Development

**Setup & Building:**
- **[BUILD_GUIDE.md](../BUILD_GUIDE.md)** - Build from source
- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - Contribution guidelines
- **[DESKTOP_APP_GUIDE.md](../DESKTOP_APP_GUIDE.md)** - Electron development
- **[DEVELOPMENT.md](../DEVELOPMENT.md)** - Component development
- [Build and Run](developer/BUILD_AND_RUN.md)
- [Installation](developer/INSTALL.md)
- [Deployment](developer/DEPLOYMENT.md)

**Testing:**
- **[Testing Guide](development/testing.md)** - Comprehensive testing documentation
- [E2E Testing Guide](development/E2E_TESTING_GUIDE.md)
- [Testing Quick Start](development/TESTING_QUICK_START.md)
- [SSE Integration Testing](development/SSE_INTEGRATION_TESTING_GUIDE.md)
- [TESTING.md](TESTING.md) - Additional testing info
- [Integration Testing Guide](INTEGRATION_TESTING_GUIDE.md)

**CI/CD:**
- **[CI/CD Guide](development/ci-cd.md)** - Workflows, quality gates, debugging
- [CI Platform Requirements](development/CI_PLATFORM_REQUIREMENTS.md)
- [Quality Gates](development/QUALITY_GATES.md)
- [Windows Build Quickstart](development/WINDOWS_BUILD_QUICKSTART.md)

**Code Standards:**
- [Zero Placeholder Policy](development/ZERO_PLACEHOLDER_POLICY.md) - No TODO/FIXME
- [TypeScript Guidelines](TYPESCRIPT_GUIDELINES.md)
- [Spacing Conventions](development/SPACING_CONVENTIONS.md)

**Integration & API:**
- [Frontend API Integration](development/FRONTEND_API_INTEGRATION_GUIDE.md)
- [Electron React Hydration Debug](development/ELECTRON_REACT_HYDRATION_DEBUG_GUIDE.md)
- [Error Handling Guide](development/ERROR_HANDLING_GUIDE.md)
- [Network Resilience](development/NETWORK_RESILIENCE_GUIDE.md)
- [SSE Implementation Guide](development/SSE_IMPLEMENTATION_GUIDE.md)

**Performance:**
- [Performance Optimization Guide](development/PERFORMANCE_OPTIMIZATION_GUIDE.md)
- [Performance Testing Guide](development/PERFORMANCE_TESTING_GUIDE.md)
- [Performance Usage Examples](development/PERFORMANCE_USAGE_EXAMPLES.md)

**Dependencies:**
- [DEPENDENCIES.md](DEPENDENCIES.md)
- [Dependency Path Selection](dependency-path-selection.md)

### 🌐 API Reference

**Overview:**
- **[API Overview](api/README.md)** - Complete API documentation

**Endpoints:**
- [API Contract v1](api/API_CONTRACT_V1.md)
- [Health Endpoints](api/health.md)
- [Job Management](api/jobs.md)
- [Provider System](api/providers.md)
- [Rate Limits](api/rate-limits.md)

**Documentation:**
- [Swagger/OpenAPI Guide](api/SWAGGER_GUIDE.md) - Interactive API docs
- [Error Handling](api/errors.md)

**Integration:**
- [JOB_QUEUE_SSE_SPEC.md](JOB_QUEUE_SSE_SPEC.md)
- [NETWORK_CONTRACT_IMPLEMENTATION.md](NETWORK_CONTRACT_IMPLEMENTATION.md)

### ⚙️ Operations

**Configuration:**
- [Configuration System Index](operations/CONFIGURATION_SYSTEM_INDEX.md)
- [FFmpeg Configuration](operations/FFMPEG_CONFIGURATION_UNIFIED.md)
- [FFmpeg Status Testing](operations/FFMPEG_STATUS_TESTING_GUIDE.md)

**Deployment & Release:**
- [Release Notes](operations/RELEASE_NOTES.md)
- [Release Playbook](operations/ReleasePlaybook.md)
- [Deployment Validation Checklist](DEPLOYMENT_VALIDATION_CHECKLIST.md)

**Monitoring & Diagnostics:**
- [Diagnostics](operations/DIAGNOSTICS.md)
- [Run Telemetry Guide](operations/RUN_TELEMETRY_GUIDE.md)
- [Resilience Runbook](operations/RESILIENCE_RUNBOOK.md)
- [Oncall Runbook](operations/OncallRunbook.md)
- [Resource Management Guide](operations/RESOURCE_MANAGEMENT_GUIDE.md)

**Database:**
- [Database Migration Guide](operations/DATABASE_MIGRATION_GUIDE.md)
- [DATABASE_VERIFICATION_GUIDE.md](database/DATABASE_VERIFICATION_GUIDE.md)

**FFmpeg Setup:**
- [FFmpeg Setup Guide](FFmpeg_Setup_Guide.md)
- [Managed FFmpeg Guide](MANAGED_FFMPEG_GUIDE.md)

**Installation & Configuration:**
- [Windows Installation](WINDOWS_INSTALLATION.md)
- [Building Windows Installer](BUILDING_WINDOWS_INSTALLER.md)
- [Asset Path Resolution](ASSET_PATH_RESOLUTION.md)
- [Code Signing](CODE_SIGNING.md)

### 🔀 Workflows

**User Workflows:**
- [Quick Demo Workflow](workflows/QUICK_DEMO.md)
- [Portable Mode Guide](workflows/PORTABLE_MODE_GUIDE.md)
- [Portable Mode Guide (alt)](workflows/PORTABLE.md)
- [Settings Schema](workflows/SETTINGS_SCHEMA.md)
- [UX Guide](workflows/UX_GUIDE.md)

### 📖 Additional Documentation

**Performance:**
- [Performance Benchmarks](PERFORMANCE_BENCHMARKS.md)

**Security:**
- [Security Hardening](security/) - Security documentation

**Project-Specific:**
- Aura.Api README - Backend API project
- Aura.Web README - Frontend web project
- Aura.Desktop/electron/README.md - Electron main process

**Style & Best Practices:**
- [Docs Style Guide](style/DocsStyleGuide.md)
- [Best Practices](best-practices/) - Development best practices

**Implementation Guides:**
- [Provider Profiles Implementation](PROVIDER_PROFILES_IMPLEMENTATION.md)
- [Provider Quality Configuration](PROVIDER_QUALITY_CONFIGURATION_GUIDE.md)
- [Export Presets Implementation](EXPORT_PRESETS_IMPLEMENTATION.md)
- [Stage Interface Guide](STAGE_INTERFACE_GUIDE.md)
- [Video Effects Guide](VIDEO_EFFECTS_GUIDE.md)
- [Validation Patience Strategy](VALIDATION_PATIENCE_STRATEGY.md)
- [Orchestration Runbook](ORCHESTRATION_RUNBOOK.md)
- [Pipeline Architecture](PIPELINE_ARCHITECTURE.md)
- [Offline Mode Improvements](OFFLINE_MODE_IMPROVEMENTS.md)
- [Menu Command System](MENU_COMMAND_SYSTEM.md)
- [Menu Command Architecture](MENU_COMMAND_ARCHITECTURE.md)

---

## Historical Documentation

Implementation summaries, PR reports, and build fixes from past work:
- **[Archive](archive/)** - 326+ historical documents

---

## Finding Documentation

### By User Type

**End Users** → Start with [User Guides](#-user-guides)  
**Developers** → Start with [CONTRIBUTING.md](../CONTRIBUTING.md) and [Development](#-development)  
**Operations** → Start with [Operations](#️-operations)  
**API Users** → Start with [API Reference](#-api-reference)

### By Task

**Installing** → [Installation Guide](getting-started/INSTALLATION.md)  
**Contributing** → [CONTRIBUTING.md](../CONTRIBUTING.md)  
**Building** → [BUILD_GUIDE.md](../BUILD_GUIDE.md)  
**Testing** → [Testing Guide](development/testing.md)  
**Configuring Providers** → [Provider System](providers/README.md)  
**Troubleshooting** → [TROUBLESHOOTING.md](../TROUBLESHOOTING.md)

---

**Last updated**: 2025-11-18 (Documentation reorganization)

For questions or suggestions about documentation, please file an issue on GitHub.
