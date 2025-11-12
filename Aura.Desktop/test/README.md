# Startup Logging Tests

This directory contains comprehensive tests for the startup logging and diagnostics system.

## Test Files

### test-startup-logger.js
Tests the `StartupLogger` module functionality:
- Logger initialization
- Basic logging (info, warn, error, debug)
- Step tracking (start/end)
- Slow step detection (>2 seconds)
- Async function tracking
- Sync function tracking
- Error tracking with stack traces
- Summary generation
- Log file creation and verification
- Log rotation (keeps last 10 logs)

### test-startup-diagnostics.js
Tests the `StartupDiagnostics` module functionality:
- Diagnostics initialization
- Platform detection
- Node.js version check
- Memory availability check
- Disk space check
- Directory accessibility check
- FFmpeg availability check
- .NET runtime availability check
- Port availability check
- Overall health assessment
- Warning and error collection

## Running Tests

### Run all tests
```bash
npm test
```

### Run specific tests
```bash
npm run test:logger        # Run logger tests only
npm run test:diagnostics   # Run diagnostics tests only
```

### Run manually
```bash
node test/test-startup-logger.js
node test/test-startup-diagnostics.js
```

## Expected Output

All tests should pass with `ALL TESTS PASSED ✓` message at the end.

### Logger Tests
- Creates temporary log files in `/tmp/aura-test-logs/`
- Tests log rotation by creating 12 files and verifying only 10 remain
- Verifies structured JSON log format
- Tests performance warning for steps >2 seconds
- Cleans up all test files after completion

### Diagnostics Tests
- Creates temporary test directories in `/tmp/aura-test-diagnostics/`
- Runs comprehensive system checks
- Identifies available resources (Node.js, .NET, FFmpeg, etc.)
- Collects warnings for missing optional components
- Cleans up all test files after completion

## Test Coverage

The tests cover all critical requirements from PR 4:

1. ✅ Structured JSON logging with level, timestamp, component, message, metadata
2. ✅ Timestamped log entry at START and END of every function
3. ✅ Log file creation before other initialization
4. ✅ Performance timing with warnings for steps >2 seconds
5. ✅ Log aggregation with summary file
6. ✅ Persistent logging to disk (userData/logs/startup-{timestamp}.log)
7. ✅ Debug mode support (tested in logger initialization)
8. ✅ Startup diagnostics with health checks
9. ✅ Log rotation (keeps last 10 logs)
10. ✅ Proper error context with stack traces

## Continuous Integration

These tests can be integrated into CI/CD pipelines:

```yaml
- name: Test Startup Logging
  run: |
    cd Aura.Desktop
    npm install
    npm test
```

## Troubleshooting

### Tests fail with "Cannot find module"
Ensure you're in the correct directory:
```bash
cd Aura.Desktop
npm install
npm test
```

### Disk space check fails
The disk space check is platform-specific and may not work in all test environments. This is expected and handled gracefully.

### FFmpeg not found
FFmpeg is an optional dependency. Tests expect this warning in test environments.

### .NET not found
If .NET is not installed in the test environment, tests will handle this gracefully with a warning.

## Adding New Tests

When adding new startup logging features, follow this pattern:

```javascript
// Test new feature
console.log('\n{N}. Testing {feature name}...');
// ... test code ...
console.log('✓ {Feature name} works');
```

Ensure:
- Tests are isolated and don't depend on other tests
- Cleanup is performed after each test
- Both success and failure cases are tested
- Mock objects are used where appropriate
- Tests run quickly (<5 seconds per test)

## Files Not Under Test

These files use the logging system but are not directly tested here:
- `electron/main.js` - Integration with Electron app lifecycle
- `electron/ipc-handlers/startup-logs-handler.js` - IPC handlers (requires Electron runtime)
- `electron/preload.js` - Preload script (requires Electron renderer)

These should be tested as part of E2E testing with the full Electron app running.
