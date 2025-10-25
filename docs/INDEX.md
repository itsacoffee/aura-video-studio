# Documentation Index

Welcome to the Aura Video Studio documentation. This index provides a roadmap to all available documentation.

## Quick Start

- [README](../README.md) - Project overview and getting started
- [QUICKSTART](user-guide/QUICKSTART.md) - Quick start guide
- [CONTRIBUTING](../CONTRIBUTING.md) - How to contribute to this project

## User Documentation

**[User Guide Overview](user-guide/README.md)** - Complete index of user documentation

### Getting Started
- [Portable Mode Guide](workflows/PORTABLE_MODE_GUIDE.md) - Running Aura in portable mode
- [Quick Demo](workflows/QUICK_DEMO.md) - Quick demonstration workflow
- [First Run Guide](user-guide/PORTABLE_FIRST_RUN.md) - First run experience

### User Guides
- [AI Learning System Guide](user-guide/AI_LEARNING_SYSTEM_GUIDE.md)
- [Asset Library Guide](user-guide/ASSET_LIBRARY_GUIDE.md)
- [Content Verification Guide](user-guide/CONTENT_VERIFICATION_GUIDE.md)
- [Context Management Guide](user-guide/CONTEXT_MANAGEMENT_GUIDE.md)
- [Pacing Optimization Guide](user-guide/PACING_OPTIMIZATION_GUIDE.md)
- [Pipeline Validation Guide](user-guide/PIPELINE_VALIDATION_GUIDE.md)
- [Script Enhancement User Guide](user-guide/SCRIPT_ENHANCEMENT_USER_GUIDE.md)
- [Theme System Guide](user-guide/THEME_SYSTEM_GUIDE.md)
- [Timeline Editor UI Guide](user-guide/TIMELINE_EDITOR_UI_GUIDE.md)
- [User Profile System Guide](user-guide/USER_PROFILE_SYSTEM_GUIDE.md)
- [Visual Waveforms & Thumbnails Guide](user-guide/VISUAL_WAVEFORMS_THUMBNAILS_GUIDE.md)
- [Local Providers Setup](user-guide/LOCAL_PROVIDERS_SETUP.md)
- [Download Center Guide](user-guide/DOWNLOAD_CENTER.md)

### Configuration
- [Settings Schema](workflows/SETTINGS_SCHEMA.md)
- [Portable Reference](user-guide/PORTABLE_ONLY_QUICK_REFERENCE.md)

## Developer Documentation

**[Developer Documentation Overview](developer/README.md)** - Complete index of developer docs

### Getting Started with Development
- [Build and Run Guide](developer/BUILD_AND_RUN.md)
- [Installation Guide](developer/INSTALL.md)
- [Deployment Guide](developer/DEPLOYMENT.md)

### Architecture

**[Architecture Overview](architecture/README.md)** - Complete index of architecture documentation
- [System Architecture](architecture/ARCHITECTURE.md)
- [Provider Selection Architecture](architecture/PROVIDER_SELECTION_ARCHITECTURE.md)
- [Service Initialization Order](architecture/SERVICE_INITIALIZATION_ORDER.md)
- [Error Flow Diagram](architecture/ERROR_FLOW_DIAGRAM.md)
- [SSE Event Flow](architecture/SSE_EVENT_FLOW.md)
- [FFmpeg Locator Flow](architecture/FFMPEG_SINGLE_LOCATOR_FLOW.md)
- [Wizard State Machine Diagram](architecture/WIZARD_STATE_MACHINE_DIAGRAM.md)

### Design Documents
- [Advanced Timeline Features](architecture/ADVANCED_TIMELINE_FEATURES.md)
- [Brand Kit UI](architecture/BRAND_KIT_UI.md)
- [Error Modal UI Design](architecture/ERROR_MODAL_UI_DESIGN.md)
- [Professional Wizard UX](architecture/PROFESSIONAL_WIZARD_UX.md)

### API Documentation
- [API Overview](api/README.md)
- [API Contract V1](api/API_CONTRACT_V1.md)
- [Providers API](api/providers.md)
- [Health API](api/health.md)
- [Errors API](api/errors.md)
- [Jobs API](api/jobs.md)
- [Profile API Reference](developer/PROFILE_API_REFERENCE.md)
- [Quality Validation API](developer/QUALITY_VALIDATION_API.md)
- [FFmpeg Install API](developer/FFMPEG_INSTALL_API.md)

### Development Guides
- [FFmpeg Setup Guide](FFmpeg_Setup_Guide.md)
- [Integration Testing Guide](INTEGRATION_TESTING_GUIDE.md)
- [UX Guide](workflows/UX_GUIDE.md)
- [Future Enhancements Removal](developer/FUTURE_ENHANCEMENTS_REMOVAL.md)

### UI/UX Documentation
- [UI Improvements Visual Guide](user-guide/UI_IMPROVEMENTS_VISUAL_GUIDE.md)
- [Dependency Rescan UI Guide](user-guide/DEPENDENCY_RESCAN_UI_GUIDE.md)
- [UI Changes Documentation](developer/UI_CHANGES_DOCUMENTATION.md)
- [UI Screenshots Description](developer/UI_SCREENSHOTS_DESCRIPTION.md)
- [UI/UX Improvements](developer/UI_UX_IMPROVEMENTS.md)
- [UI Visual Comparison](developer/UI_VISUAL_COMPARISON.md)

## Testing & Quality Assurance

### Testing
- [Integration Testing Guide](INTEGRATION_TESTING_GUIDE.md)
- [Troubleshooting Integration Tests](troubleshooting/TROUBLESHOOTING_INTEGRATION_TESTS.md)
- [Verification Guide](VERIFICATION.md)

### Best Practices
- [Best Practices](best-practices/README.md)
- [Troubleshooting](troubleshooting/README.md)

## CI/CD & Deployment

- [CI Documentation](CI.md)
- [Deployment Validation Checklist](DEPLOYMENT_VALIDATION_CHECKLIST.md)
- [Deployment Guide](developer/DEPLOYMENT.md)

## Security

- [Security Policy](../SECURITY.md) - Main security policy and reporting
- [Security Documentation Overview](security/README.md) - Security documentation index
- [Security Summaries](security/) - Detailed security reviews by feature

## Historical Documentation

**[Archive Overview](archive/README.md)** - Index of historical documentation

Implementation notes, PR summaries, and historical documentation are archived in [docs/archive/](archive/).

## Contributing to Documentation

When adding new documentation:

1. Place user-facing documentation in `docs/user-guide/`
2. Place developer documentation in `docs/developer/`
3. Place API documentation in `docs/api/`
4. Place architecture docs in `docs/architecture/`
5. Update this index when adding new documentation
6. Follow the existing documentation style and format
7. Keep the root directory clean - only core docs (README, CONTRIBUTING, SECURITY, LICENSE)

For more information, see [CONTRIBUTING.md](../CONTRIBUTING.md).
