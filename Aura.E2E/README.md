# Aura.E2E - End-to-End Integration Tests

This project contains end-to-end integration tests for Aura Video Studio. These tests validate the complete pipeline from user input to final video output.

## Test Categories

### 1. Smoke Tests (`SmokeTests.cs`)
Quick validation tests that ensure core functionality works end-to-end:

- **FreeOnlySmoke_Should_GenerateShortVideoWithCaptions**: Tests Free-only path using RuleBased LLM and stock visuals. Generates 10-15s video without external dependencies.
- **MixedModeSmoke_Should_DowngradeAndGenerateVideo**: Tests Mixed-mode with automatic fallback to Free providers when Pro providers unavailable.
- **Caption/Render validation**: Validates SRT/VTT caption generation and FFmpeg command building.

### 2. Component Integration Tests (`UnitTest1.cs`)
Tests individual components and their integration:

- Hardware detection and system profiling
- LLM provider selection and fallback logic
- Script generation with various configurations
- FFmpeg render command generation
- Provider mixing and profile selection

### 3. API Integration Tests (`ProviderValidationApiTests.cs`)
Tests REST API endpoints (requires running API server):

- Provider validation endpoint
- Offline mode enforcement
- Error handling and validation

## Running Tests

### All Tests
```bash
dotnet test Aura.E2E/Aura.E2E.csproj --configuration Release
```

### Smoke Tests Only
```bash
dotnet test Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~SmokeTests"
```

### CI Environment
Tests run automatically in CI on both Windows and Linux:
- Windows: Uses PowerShell smoke script after tests
- Linux: Uses Bash smoke script after tests

## Test Philosophy

### Fast & Focused
- Smoke tests complete in under 5 seconds
- No external API calls (mocked or stubbed)
- Minimal file I/O

### Cross-Platform
- Tests run on both Windows and Linux
- Platform-specific code is abstracted
- Windows TTS is mocked on Linux

### Deterministic
- RuleBased provider uses fixed seed (42)
- No random failures or flaky tests
- Predictable output for validation

## Coverage Goals

- **Core Pipeline**: 100% of main generation path
- **Provider Mixing**: All fallback scenarios
- **Hardware Detection**: All tier levels
- **Render Specs**: All standard presets

## Adding New Tests

When adding new E2E tests:

1. **Name clearly**: Use descriptive test names that explain what's being validated
2. **Keep fast**: Aim for <5s execution time
3. **No external deps**: Mock APIs, use test doubles
4. **Assert meaningfully**: Validate actual output, not just "doesn't crash"
5. **Document purpose**: Add XML comments explaining what scenario is tested

## Smoke Test Workflow

The smoke tests validate:
1. ✅ Hardware detection completes
2. ✅ Provider selection works (Free or Mixed)
3. ✅ Script generation produces valid output (100-2000 chars)
4. ✅ Render commands are well-formed
5. ✅ Duration targets are met (9-20s range)

## Dependencies

- **Aura.Core**: Core models and orchestration
- **Aura.Providers**: LLM and provider implementations
- **xUnit**: Test framework
- **coverlet.collector**: Code coverage

## CI Integration

These tests are part of the CI pipeline:
- `.github/workflows/ci.yml`: Main CI workflow
- `.github/workflows/ci-windows.yml`: Windows-specific with PowerShell smoke
- `.github/workflows/ci-linux.yml`: Linux-specific with Bash smoke

Test results are uploaded as artifacts and available for 30 days.
