# Network Contract Implementation

## Overview

This document describes the implementation of the network contract system that ensures a single source of truth for backend URL configuration across Electron, the backend API, and the frontend.

## Problem Statement

Previously, there was a risk of:
- Hardcoded ports scattered across the codebase
- Backend starting on one port while frontend pointed to another
- Silent failures when configuration was invalid
- No validation of URL format or port ranges
- Inconsistent URL resolution across components

## Solution: Network Contract

A validated, enforced contract that all components must use to communicate with the backend.

### Architecture

```
┌─────────────────────────────────────────────────────┐
│          Environment Variables                      │
│  AURA_BACKEND_URL or ASPNETCORE_URLS               │
└───────────────────┬─────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│     network-contract.js                             │
│  resolveBackendContract({ isDev })                  │
│  - Validates URL format                             │
│  - Validates port range (1-65535)                   │
│  - Validates health endpoints                       │
│  - Returns NetworkContract or throws error          │
└───────────────────┬─────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│     BackendService / ExternalBackendService         │
│  - Enforces networkContract in constructor          │
│  - Refuses to start without valid contract          │
│  - No fallback logic                                │
└───────────────────┬─────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│     preload.js (Security Bridge)                    │
│  - Exposes contract URL to renderer                 │
│  - window.desktopBridge.backend.getUrl()            │
│  - window.AURA_BACKEND_URL (legacy)                 │
└───────────────────┬─────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│     apiBaseUrl.ts (Frontend)                        │
│  - Resolves URL in priority order:                  │
│    1. desktopBridge.backend.getUrl()                │
│    2. VITE_API_BASE_URL                             │
│    3. window.location.origin                        │
│    4. Fallback with warning                         │
└─────────────────────────────────────────────────────┘
```

## Implementation Details

### 1. network-contract.js

**Location**: `Aura.Desktop/electron/network-contract.js`

**Responsibilities**:
- Resolve backend URL from environment variables
- Validate URL format using Node.js URL API
- Validate port is in valid range (1-65535)
- Validate health endpoints are non-empty strings
- Return fully validated `NetworkContract` object
- Throw descriptive errors if validation fails

**NetworkContract Type**:
```javascript
/**
 * @typedef {Object} NetworkContract
 * @property {string} protocol - Protocol (http or https)
 * @property {string} host - Hostname (e.g., 127.0.0.1)
 * @property {number} port - Port number
 * @property {string} baseUrl - Fully qualified base URL
 * @property {string} raw - Raw URL string from environment
 * @property {string} healthEndpoint - Health check path
 * @property {string} readinessEndpoint - Readiness check path
 * @property {boolean} shouldSelfHost - Whether Electron spawns backend
 * @property {number} maxStartupMs - Startup timeout in milliseconds
 * @property {number} pollIntervalMs - Health check poll interval
 */
```

### 2. BackendService

**Location**: `Aura.Desktop/electron/backend-service.js`

**Changes**:
- Constructor now requires `networkContract` parameter
- Validates contract immediately in constructor
- Throws if `networkContract`, `baseUrl`, or `port` are invalid
- Removed fallback logic that allowed operation without contract
- Uses contract values for all backend communication

**Validation**:
```javascript
if (!networkContract) {
  throw new Error("BackendService requires a valid networkContract.");
}
if (!networkContract.baseUrl || typeof networkContract.baseUrl !== "string") {
  throw new Error("BackendService networkContract missing baseUrl.");
}
if (!networkContract.port || typeof networkContract.port !== "number" || networkContract.port <= 0) {
  throw new Error("BackendService networkContract missing valid port.");
}
```

### 3. ExternalBackendService

**Location**: `Aura.Desktop/electron/external-backend-service.js`

**Changes**:
- Mirrors BackendService validation logic
- Constructor requires `networkContract` parameter
- Validates contract immediately
- Throws descriptive errors if invalid

### 4. preload.js

**Location**: `Aura.Desktop/electron/preload.js`

**Changes**:
- Enhanced `desktopBridge` object to expose contract URL
- Added `backend.getUrl()` method that returns contract URL
- Maintains backward compatibility with legacy globals
- Exposes both `window.desktopBridge.backend.getUrl()` and `window.AURA_BACKEND_URL`

**Bridge Structure**:
```javascript
const desktopBridge = {
  backend: {
    getUrl: () => runtimeBootstrap?.backend?.baseUrl || null,
    // ... other backend properties
  },
  getBackendBaseUrl: () => runtimeBootstrap?.backend?.baseUrl || null,
  // ... other bridge properties
};
```

### 5. apiBaseUrl.ts

**Location**: `Aura.Web/src/config/apiBaseUrl.ts`

**Changes**:
- Enhanced `getBridgeBackendUrl()` to check `desktopBridge.backend.getUrl()`
- Priority order:
  1. `window.aura.runtime.getCachedDiagnostics().backend.baseUrl`
  2. `window.desktopBridge.backend.getUrl()` (NEW)
  3. `window.desktopBridge.getBackendBaseUrl()` (legacy)
- Maintains existing fallback chain for non-Electron environments

## Testing

### Unit Tests

**File**: `Aura.Desktop/test/test-network-contract-validation.js`

Tests cover:
- ✓ URL format validation
- ✓ Port validation
- ✓ Required field presence
- ✓ BackendService constructor validation
- ✓ ExternalBackendService constructor validation
- ✓ Error message descriptiveness
- ✓ Default value correctness

**Results**: 10/10 tests passing

### Frontend Tests

**File**: `Aura.Web/src/config/__tests__/apiBaseUrl.test.ts`

Added tests for:
- ✓ `desktopBridge.backend.getUrl()` resolution
- ✓ Contract-based URL prioritization
- ✓ Fallback behavior

### Integration Test

**File**: `Aura.Desktop/test/manual-test-network-contract.js`

Validates end-to-end contract flow:
1. Contract resolution from environment
2. Contract structure validation
3. BackendService validation
4. ExternalBackendService validation
5. Preload bridge exposure
6. Frontend URL resolution

**Usage**:
```bash
AURA_BACKEND_URL=http://127.0.0.1:5272 node test/manual-test-network-contract.js
```

## Environment Variables

### Primary Configuration

- **`AURA_BACKEND_URL`** (preferred)
  - Full base URL including protocol, host, and port
  - Example: `http://127.0.0.1:5272`
  - Used by both Electron and backend

- **`ASPNETCORE_URLS`** (alternative)
  - ASP.NET Core binding URLs
  - Example: `http://127.0.0.1:5272`

### Optional Configuration

- **`AURA_BACKEND_HEALTH_ENDPOINT`**
  - Health check path (default: `/api/health`)

- **`AURA_BACKEND_READY_ENDPOINT`**
  - Readiness check path (default: `/health/ready`)

- **`AURA_BACKEND_STARTUP_TIMEOUT_MS`**
  - Startup timeout in milliseconds (default: `60000`)

- **`AURA_BACKEND_HEALTH_POLL_INTERVAL_MS`**
  - Health check poll interval (default: `1000`)

- **`AURA_LAUNCH_BACKEND`**
  - Whether Electron should spawn backend (default: `true`)
  - Set to `false` for external backend mode

## Benefits

### 1. Fail-Fast Validation
- Invalid configurations are caught at startup
- Clear error messages guide users to fix issues
- No silent failures or undefined behavior

### 2. Single Source of Truth
- One canonical URL shared across all components
- No risk of diverging configurations
- Easy to change port or host for all components

### 3. No Hardcoded Ports
- All port configuration comes from environment
- Easy to run multiple instances on different ports
- Supports development, staging, and production environments

### 4. Type Safety
- Full JSDoc definitions for NetworkContract
- TypeScript types for frontend resolution
- IDE autocomplete and type checking

### 5. Better Debugging
- Contract validation errors include descriptive messages
- Easy to trace URL resolution through logs
- Integration test validates full flow

## Migration Guide

### For Developers

No changes required! The system maintains backward compatibility:
- Existing `AURA_BACKEND_URL` usage continues to work
- Frontend code automatically uses contract URLs
- Tests validate both new and legacy paths

### For Users

No changes required! Default URLs remain the same:
- Development: `http://127.0.0.1:5272`
- Production: `http://127.0.0.1:5890`

### For CI/CD

Consider setting `AURA_BACKEND_URL` explicitly in CI to:
- Make configuration explicit
- Avoid relying on defaults
- Ensure consistent behavior across environments

## Future Enhancements

Potential improvements for future PRs:
- Add HTTPS support with certificate validation
- Support multiple backend instances (load balancing)
- Add contract versioning for breaking changes
- Implement contract caching for faster restarts
- Add telemetry for contract resolution success/failure

## References

- [DEVELOPMENT.md](../DEVELOPMENT.md#backend-url-contract) - Usage guide
- [ARCHITECTURE.md](./architecture/ARCHITECTURE.md) - Architecture overview
- [DESKTOP_APP_GUIDE.md](../DESKTOP_APP_GUIDE.md) - Electron development guide

## Summary

The network contract implementation provides a robust, validated, and enforced system for managing backend URL configuration across the entire Aura Video Studio application. By centralizing URL resolution and validation, we eliminate configuration drift, catch errors early, and provide a better developer experience.
