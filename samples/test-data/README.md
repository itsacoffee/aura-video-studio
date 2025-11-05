# Test Data

This directory contains hermetic test data for E2E testing of Aura Video Studio.

## Overview

Test data is organized to support isolated, reproducible testing without external dependencies.

## Directory Structure

```
test-data/
├── briefs/               # Synthetic test briefs
│   └── synthetic-briefs.json
├── configs/              # Hermetic test configurations
│   └── hermetic-test-config.json
└── fixtures/             # Mock provider responses
    └── (to be added)
```

## Synthetic Briefs

**Location**: `briefs/synthetic-briefs.json`

Contains LLM-generated test scenarios with various complexity levels and edge cases:

- **Simple tutorials**: Low complexity, short duration
- **Technical explainers**: High complexity, detailed content
- **Marketing shorts**: Fast-paced, promotional
- **Educational long-form**: Comprehensive, structured
- **Edge cases**: Unicode, special characters, minimal/maximal input

### Brief Structure

```json
{
  "id": "unique-id",
  "name": "Human-readable name",
  "topic": "Video topic",
  "audience": "Target audience",
  "goal": "Video goal",
  "tone": "Tone/style",
  "language": "Language",
  "duration": 30,
  "expectedScenes": 5,
  "expectedComplexity": "medium",
  "tags": ["category", "type"]
}
```

### Usage in Tests

```typescript
import syntheticBriefs from '../../../samples/test-data/briefs/synthetic-briefs.json';

// Get a specific test case
const tutorialBrief = syntheticBriefs.briefs.find(b => b.id === 'tutorial-simple');

// Get all edge cases
const edgeCases = syntheticBriefs.briefs.filter(b => b.tags?.includes('edge-case'));

// Use in test
test('should handle tutorial brief', async ({ page }) => {
  await fillBriefForm(page, tutorialBrief);
  // ... test implementation
});
```

## Hermetic Test Config

**Location**: `configs/hermetic-test-config.json`

Configuration for isolated testing:

- **Provider Settings**: Uses local/mock providers only
- **Rendering**: Mocks FFmpeg for faster tests
- **Test Mode**: Skips network calls, uses accelerated time
- **Retry Policy**: Handles transient failures
- **Timeouts**: Per-phase timeout configuration
- **Artifacts**: Cleanup policies

### Usage

```typescript
import hermeticConfig from '../../../samples/test-data/configs/hermetic-test-config.json';

// Apply config in test setup
test.beforeAll(async () => {
  // Set environment to use hermetic config
  process.env.TEST_CONFIG = 'hermetic';
  
  // Configure providers
  await configureProviders(hermeticConfig.providers);
});
```

## Fixtures

**Location**: `fixtures/`

Mock responses for external providers (to be populated):

- LLM provider responses
- TTS provider responses
- Image provider responses
- FFmpeg output samples

### Creating New Fixtures

```json
// fixtures/llm-responses.json
{
  "simple-script": {
    "provider": "RuleBased",
    "input": "Topic about getting started",
    "output": "Sample script content...",
    "duration": 15,
    "scenes": 3
  }
}
```

## Test Data Principles

1. **Hermetic**: Self-contained, no external dependencies
2. **Reproducible**: Same input always produces same output
3. **Diverse**: Covers happy path, edge cases, and error scenarios
4. **Realistic**: Data resembles actual user input
5. **Documented**: Clear structure and usage examples

## Adding New Test Data

1. **Identify test scenario**: What are you testing?
2. **Create data file**: Add to appropriate subdirectory
3. **Document structure**: Add comments and examples
4. **Update this README**: Add usage instructions
5. **Reference in tests**: Import and use in E2E tests

## Maintenance

- **Review quarterly**: Ensure data is current and relevant
- **Update on feature changes**: Keep data in sync with app features
- **Remove obsolete data**: Clean up unused test data
- **Version data**: Track changes alongside code versions

## Related Documentation

- [E2E Testing Guide](../../E2E_TESTING_GUIDE.md)
- [SSE Integration Testing Guide](../../SSE_INTEGRATION_TESTING_GUIDE.md)
- [Production Readiness Checklist](../../PRODUCTION_READINESS_CHECKLIST.md)

---

**Last Updated**: 2025-11-05  
**Maintainer**: Aura Development Team
