# E2E Test Configurations

Hermetic test configurations for end-to-end testing of Aura Video Studio.

## Overview

These configurations provide predefined, reproducible test scenarios for automated E2E tests. All configurations use offline-capable providers to ensure tests can run without external API dependencies.

## Test Categories

### Smoke Tests
- **basic-tutorial.json** - Standard 15-second tutorial video
- Quick validation of core generation pipeline
- Uses: RuleBased LLM, Windows TTS, Stock visuals

### Edge Cases
- **edge-case-short.json** - Minimum duration video (5 seconds)
- Tests boundary conditions and error handling
- Validates system behavior at extremes

## Configuration Schema

Each configuration file includes:

```json
{
  "name": "Test Name",
  "description": "Description",
  "version": "1.0.0",
  "brief": { ... },
  "plan": { ... },
  "providers": { ... },
  "renderSettings": { ... },
  "expectedResults": { ... },
  "metadata": { ... }
}
```

### Fields

- **brief**: User input specification (topic, audience, goal, tone)
- **plan**: Video planning parameters (duration, pacing, density)
- **providers**: Provider selection for script, TTS, and visuals
- **renderSettings**: Video output configuration
- **expectedResults**: Expected outputs for validation
- **metadata**: Test categorization and properties

## Usage in Tests

### Frontend (Playwright)

```typescript
import * as basicConfig from '../../samples/e2e-test-configs/basic-tutorial.json';

test('should generate video from config', async ({ page }) => {
  await page.route('**/api/jobs', (route) => {
    const request = basicConfig.brief;
    // Mock API response based on config
  });
});
```

### Backend (xUnit)

```csharp
var config = JsonSerializer.Deserialize<TestConfig>(
    File.ReadAllText("samples/e2e-test-configs/basic-tutorial.json")
);

var brief = new Brief(
    Topic: config.Brief.Topic,
    Audience: config.Brief.Audience,
    Goal: config.Brief.Goal,
    Tone: config.Brief.Tone
);
```

## Adding New Configurations

1. Create a new JSON file following the schema
2. Ensure `hermetic: true` in metadata
3. Use only offline-capable providers
4. Define expected results for validation
5. Document in this README

## Hermetic Testing Principles

All configurations follow hermetic testing principles:

- **No external dependencies**: Only offline providers
- **Reproducible**: Deterministic inputs and outputs
- **Fast**: Quick execution without network calls
- **Isolated**: No side effects on system state

## Provider Requirements

### Offline Providers
- **LLM**: RuleBased (deterministic script generation)
- **TTS**: WindowsTTS (Windows SAPI), Piper (cross-platform)
- **Visuals**: Stock (bundled assets)

### Provider Tiers
- **Free**: No API keys required
- **ProIfAvailable**: Fallback to Free when Pro unavailable
- **Pro**: Premium providers (not used in hermetic tests)

## Expected Results Validation

Each configuration specifies expected results:

- **durationRange**: [min, max] seconds
- **minScenes/maxScenes**: Scene count bounds
- **expectedArtifacts**: Required output files

Tests should validate:
- All expected artifacts present
- Duration within specified range
- Scene count within bounds
- No errors or warnings

## CI Integration

These configurations are used in CI workflows:

- **Windows CI**: Full pipeline with Windows TTS
- **Linux CI**: Headless with Piper TTS fallback
- **Smoke Tests**: Quick validation with basic-tutorial.json

## Maintenance

When updating configurations:
- Increment version number
- Update this README
- Run validation tests
- Commit changes atomically
