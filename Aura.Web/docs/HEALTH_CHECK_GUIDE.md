# Health Check Service Implementation Guide

## Overview

This document describes the robust health check and retry logic implementation added in PR 2. The implementation provides intelligent backend connectivity checking with exponential backoff, progress tracking, and user-friendly error messages.

## Components

### 1. HealthCheckService

**Location**: `src/services/HealthCheckService.ts`

A service class that performs health checks with configurable retry logic and exponential backoff.

**Features**:

- Configurable retry attempts (default: 10)
- Exponential backoff with 10-second cap
- Progress callbacks for UI updates
- Quick check method for fast polling
- Timeout support with custom durations
- Handles both 200 (healthy) and 404 (backend running) responses

**Usage**:

```typescript
import { healthCheckService } from '@/services/HealthCheckService';

// Basic health check with default settings
const result = await healthCheckService.checkHealth();
if (result.isHealthy) {
  console.log('Backend is ready!', result.latencyMs);
}

// Health check with progress tracking
const result = await healthCheckService.checkHealth((attempt, maxAttempts) => {
  console.log(`Checking... ${attempt}/${maxAttempts}`);
});

// Quick check (2-second timeout, single attempt)
const isOnline = await healthCheckService.quickCheck();

// Wait for backend with timeout
const result = await healthCheckService.waitForBackend(30000);
```

**Custom Configuration**:

```typescript
import { HealthCheckService } from '@/services/HealthCheckService';

const customService = new HealthCheckService({
  maxRetries: 15,
  retryDelayMs: 2000,
  timeoutMs: 5000,
  exponentialBackoff: true,
  backendUrl: 'http://localhost:8080',
});
```

### 2. SetupWizard Component

**Location**: `src/components/SetupWizard.tsx`

A React component that checks backend health on mount and displays appropriate UI states.

**Features**:

- Automatic health check on mount
- Progress indicator during connection attempts
- Error display with troubleshooting steps
- Retry button with attempt counter
- Renders children when backend is healthy

**Usage**:

```typescript
import { SetupWizard } from '@/components/SetupWizard';

function App() {
  const handleBackendReady = () => {
    console.log('Backend is ready, proceeding with setup!');
  };

  return (
    <SetupWizard onBackendReady={handleBackendReady}>
      {/* Your setup wizard steps */}
      <WizardStep1 />
      <WizardStep2 />
    </SetupWizard>
  );
}
```

**States**:

1. **Checking**: Shows spinner and progress bar
2. **Healthy**: Renders children
3. **Unhealthy**: Shows error message with troubleshooting steps

### 3. HealthStatusIndicator Component

**Location**: `src/components/Health/HealthStatusIndicator.tsx`

A fixed-position status indicator that continuously monitors backend health.

**Features**:

- Fixed bottom-right position
- Color-coded badge (green/red)
- Last check timestamp
- Automatic polling every 15 seconds

**Usage**:

```typescript
import { HealthStatusIndicator } from '@/components/Health/HealthStatusIndicator';

function Layout() {
  return (
    <div>
      {/* Your app content */}
      <HealthStatusIndicator />
    </div>
  );
}
```

### 4. useBackendHealthWithRetry Hook

**Location**: `src/hooks/useBackendHealthWithRetry.ts`

A React hook for managing health checks with retry logic and state tracking.

**Features**:

- Tracks checking state
- Stores health check results
- Counts retry attempts
- Provides reset functionality

**Usage**:

```typescript
import { useBackendHealthWithRetry } from '@/hooks/useBackendHealthWithRetry';

function MyComponent() {
  const {
    isChecking,
    result,
    retryCount,
    checkHealth,
    reset
  } = useBackendHealthWithRetry({
    maxRetries: 5,
    onHealthChange: (isHealthy) => {
      console.log('Health status changed:', isHealthy);
    }
  });

  return (
    <div>
      {isChecking && <Spinner />}
      {result && (
        <div>
          Status: {result.isHealthy ? 'Online' : 'Offline'}
          {result.latencyMs && <span>Latency: {result.latencyMs}ms</span>}
        </div>
      )}
      <Button onClick={checkHealth}>Check Health</Button>
      <Button onClick={reset}>Reset</Button>
    </div>
  );
}
```

## Integration Examples

### Example 1: First-Run Setup Wizard

```typescript
import { SetupWizard } from '@/components/SetupWizard';
import { FirstRunSteps } from '@/components/FirstRun';

function FirstRunWizard() {
  return (
    <SetupWizard onBackendReady={() => {
      // Initialize analytics
      // Load user preferences
    }}>
      <FirstRunSteps />
    </SetupWizard>
  );
}
```

### Example 2: Dashboard with Health Monitoring

```typescript
import { HealthStatusIndicator } from '@/components/Health/HealthStatusIndicator';
import { Dashboard } from '@/components/Dashboard';

function App() {
  return (
    <div>
      <Dashboard />
      <HealthStatusIndicator />
    </div>
  );
}
```

### Example 3: Custom Retry Logic

```typescript
import { HealthCheckService } from '@/services/HealthCheckService';
import { useState } from 'react';

function ConnectionChecker() {
  const [status, setStatus] = useState('idle');

  const checkWithCustomRetry = async () => {
    setStatus('checking');

    const service = new HealthCheckService({
      maxRetries: 20,
      retryDelayMs: 500,
      exponentialBackoff: false
    });

    const result = await service.checkHealth((attempt, max) => {
      console.log(`Attempt ${attempt} of ${max}`);
    });

    setStatus(result.isHealthy ? 'connected' : 'failed');
  };

  return (
    <button onClick={checkWithCustomRetry}>
      Check Connection
    </button>
  );
}
```

## Testing

### Running Tests

```bash
# Run all health check tests
npm test -- src/services/__tests__/HealthCheckService.test.ts

# Run SetupWizard tests
npm test -- src/components/__tests__/SetupWizard.test.tsx

# Run all tests with coverage
npm test -- --coverage
```

### Test Coverage

- **HealthCheckService**: 18 tests covering all scenarios
  - Successful connections
  - Failed connections with retries
  - Exponential backoff
  - Timeout handling
  - Progress callbacks
  - Custom configurations

- **SetupWizard**: 8 tests covering UI states
  - Loading state
  - Success state
  - Error state
  - Retry functionality
  - Progress tracking

## Troubleshooting

### Backend Not Detected

If the health check fails to detect the backend:

1. Verify the backend is running on the correct port (default: 5000)
2. Check firewall settings
3. Ensure the backend has a health endpoint
4. Check the browser console for detailed error messages

### Slow Connection Times

If health checks are taking too long:

1. Reduce `maxRetries` in the service configuration
2. Decrease `retryDelayMs` for faster retries
3. Disable exponential backoff for consistent timing

### False Positives

If the health check reports healthy but backend is not functioning:

1. Verify the health endpoint returns proper status
2. Check that the backend is fully initialized
3. Consider implementing a more comprehensive health check

## Configuration Options

### HealthCheckService Options

| Option             | Type    | Default                 | Description                            |
| ------------------ | ------- | ----------------------- | -------------------------------------- |
| maxRetries         | number  | 10                      | Maximum number of retry attempts       |
| retryDelayMs       | number  | 1000                    | Delay between retries in milliseconds  |
| timeoutMs          | number  | 3000                    | Request timeout in milliseconds        |
| exponentialBackoff | boolean | true                    | Enable exponential backoff for retries |
| backendUrl         | string  | 'http://localhost:5000' | Backend base URL                       |

### useBackendHealthWithRetry Options

All `HealthCheckService` options plus:

| Option         | Type     | Default   | Description                         |
| -------------- | -------- | --------- | ----------------------------------- |
| onHealthChange | function | undefined | Callback when health status changes |

## Best Practices

1. **Use SetupWizard for initial setup**: Always wrap your setup flow with SetupWizard to ensure backend is ready

2. **Add HealthStatusIndicator to layouts**: Include the status indicator in your app layout for continuous monitoring

3. **Customize retry logic for your use case**: Adjust retry settings based on your backend startup time and network conditions

4. **Handle health change events**: Use the `onHealthChange` callback to trigger app-wide actions when backend status changes

5. **Test with slow backends**: Simulate slow backend startup in tests to ensure proper handling

## Future Enhancements

Potential improvements for future PRs:

- Add WebSocket-based health monitoring for real-time status
- Implement circuit breaker integration with health checks
- Add health check metrics and analytics
- Support for multiple backend endpoints
- Add visual notifications for health status changes
