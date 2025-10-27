# Smoke Tests

This directory contains smoke tests for Aura Video Studio production readiness validation (PR 40).

## Overview

Smoke tests are designed to validate critical paths and ensure the application is production-ready. These tests complement the existing E2E tests and focus specifically on:

1. **Dependency Detection** - First-run wizard, FFmpeg detection, Python/AI services
2. **Quick Demo** - End-to-end video generation from Quick Demo feature
3. **Export Pipeline** - Video export functionality and error handling
4. **AI Features** - AI-powered features like scene detection, auto-captions, etc.
5. **Settings** - Configuration persistence and validation

## Test Structure

```
tests/smoke/
├── README.md                      # This file
├── dependency-detection.test.ts   # PHASE 1: Dependency detection tests
├── quick-demo.test.ts             # PHASE 2: Quick Demo E2E tests
├── export-pipeline.test.ts        # PHASE 3: Export pipeline tests
├── ai-features.test.ts            # PHASE 4: AI features tests
└── settings.test.ts               # PHASE 5: Settings tests
```

## Running Smoke Tests

### Run all smoke tests
```bash
npm test -- tests/smoke
```

### Run specific smoke test suite
```bash
npm test -- tests/smoke/dependency-detection.test.ts
npm test -- tests/smoke/quick-demo.test.ts
npm test -- tests/smoke/export-pipeline.test.ts
npm test -- tests/smoke/ai-features.test.ts
npm test -- tests/smoke/settings.test.ts
```

### Run with watch mode
```bash
npm run test:watch -- tests/smoke
```

## Test Phases

### Phase 1: Dependency Detection and Initialization
- Fresh installation dependency detection
- Auto-install functionality
- Python/AI service detection
- Service initialization order
- Dependency status persistence

### Phase 2: Quick Demo End-to-End
- Quick Demo from clean state
- Workflow completion (script → visuals → voiceover → timeline)
- Error handling and recovery

### Phase 3: Export Pipeline
- Generate Video button functionality
- Export end-to-end (format selection, progress tracking, completion)
- Export error scenarios

### Phase 4: AI Features
- Scene detection
- Highlight detection
- Beat detection
- Auto-framing
- Smart B-roll placement
- Auto-captions
- Video stabilization
- Noise reduction

### Phase 5: Settings and Configuration
- Settings page completeness
- FFmpeg path configuration
- Workspace preferences persistence

## Test Data

Test data and fixtures are stored in:
- `src/test/factories/` - Mock data factories
- `tests/fixtures/` - Sample media files (if needed)

## Mocking Strategy

Smoke tests use the same mocking strategy as E2E tests:
- API calls are mocked using `page.route()` in Playwright
- Unit-level tests use axios-mock-adapter
- Service-level tests use mock implementations

## Success Criteria

All smoke tests must pass before declaring production-ready:
- ✅ All dependency detection scenarios work
- ✅ Quick Demo generates complete video
- ✅ Export pipeline produces valid video files
- ✅ All AI features process without errors
- ✅ Settings persist correctly

## Related Documentation

- [Production Readiness Checklist](../../PRODUCTION_READINESS_CHECKLIST.md)
- [Testing Results](../../docs/TESTING_RESULTS.md)
- [E2E Tests](../e2e/README.md)
- [Main Testing Guide](../../TESTING.md)

## Notes

- These tests focus on critical paths only
- More comprehensive testing is in `tests/e2e/`
- Unit tests are in `src/**/__tests__/`
- Integration tests are in `tests/integration/`
