# Aura Video Studio Changelog

This changelog summarizes major fixes and improvements. For detailed implementation information, see the [archive](./archive/) and [implementation](./implementation/) directories.

## Documentation Consolidation (PR-007)

### Root Directory Cleanup

The repository root has been cleaned up from 100+ markdown files to a focused set of essential documentation:

**Essential Files Remaining in Root:**
- `README.md` - Main project documentation
- `INSTALLATION.md` - Installation instructions
- `DEVELOPMENT.md` - Development setup guide
- `BUILD_GUIDE.md` - Build instructions
- `DESKTOP_APP_GUIDE.md` - Electron desktop app guide
- `FIRST_RUN_GUIDE.md` - First run configuration
- `TROUBLESHOOTING.md` - Common issues and solutions
- `ERROR_HANDLING_GUIDE.md` - Error handling patterns
- `CONTRIBUTING.md` - Contribution guidelines
- `CODE_OF_CONDUCT.md` - Community standards
- `SECURITY.md` - Security policy

### Archived Documentation

Historical documentation has been organized into:

- **[docs/archive/fixes/](./archive/fixes/)** - Bug fix summaries and analysis
- **[docs/archive/pr-summaries/](./archive/pr-summaries/)** - Pull request implementation summaries
- **[docs/archive/verification/](./archive/verification/)** - Verification and testing reports
- **[docs/archive/testing-guides/](./archive/testing-guides/)** - Testing documentation
- **[docs/archive/wizard-docs/](./archive/wizard-docs/)** - Setup wizard documentation

### Implementation Documentation

Technical implementation details are available in:

- **[docs/implementation/](./implementation/)** - Implementation summaries for major features

---

## Major Features and Fixes Summary

### Video Generation Pipeline
- FFmpeg integration with hardware acceleration (NVENC, AMF, QuickSync)
- Multi-provider TTS synthesis (ElevenLabs, PlayHT, Windows SAPI, Piper, Mimic3)
- AI script generation with multiple LLM providers

### Setup Wizard
- Comprehensive first-run wizard with provider configuration
- Hardware detection and optimization recommendations
- Automatic fallback chain configuration

### Backend Improvements
- Auto-start backend with health monitoring
- Graceful service startup and shutdown
- Database migrations with automatic schema updates

### Desktop Application
- Electron desktop app with native window management
- Tray integration and system notifications
- IPC-based backend lifecycle management

### Error Handling
- Comprehensive error handling with correlation IDs
- Provider fallback chains for resilience
- Detailed logging and diagnostics

---

For complete documentation, see the [Documentation Index](./DocsIndex.md).
