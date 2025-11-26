# PR 4: Add Frontend Connection Resilience and Better Error Messages

## Priority: MEDIUM
## Can run in parallel with: PR 2, PR 3 (after PR 1 merges)

## Problem
Frontend shows generic "Backend Server Not Reachable" error even when backend is starting up or has database issues. Users get no actionable information about what's wrong or how to fix it.

## Solution

### Step 1: Add Backend Health Endpoint with Detailed Status

File: `Aura.Api/Controllers/HealthController.cs`

Create a new controller with two endpoints:

**GET /api/health**
- Returns basic health status with 200 OK
- Includes: status, timestamp, version
- Includes basic database health check result

**GET /api/health/detailed**
- Returns detailed health status
- Returns 200 OK if healthy, 503 Service Unavailable if degraded
- Includes:
  - Overall status (healthy/degraded/unhealthy)
  - Timestamp
  - Version
  - Database health with detailed breakdown
  - Component status for API and database
  - List of issues and warnings

Create a private method `CheckDatabaseHealthAsync()` that:
- Checks database connection using CanConnectAsync()
- Checks for pending migrations
- Checks if critical tables exist (Settings, QueueConfiguration, AnalyticsRetentionSettings)
- Catches SqliteException for missing tables
- Returns DatabaseHealth record with:
  - IsHealthy (bool)
  - Message (string)
  - Issues (List<string>)
  - Warnings (List<string>)

### Step 2: Create Frontend Health API Module

File: `Aura.Web/src/api/health.ts`

Create TypeScript module with:

**HealthStatus interface:**
```typescript
interface HealthStatus {
  status: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: string;
  version: string;
  database: {
    isHealthy: boolean;
    message: string;
    issues?: string[];
    warnings?: string[];
  };
  components?: {
    api: { status: string; message: string };
    database: { status: string; message: string; issues?: string[] };
  };
}
```

**checkHealth() function:**
- Makes GET request to /api/health/detailed
- Timeout of 5000ms
- Returns HealthStatus
- Throws on error

**checkBasicHealth() function:**
- Makes GET request to /api/health
- Timeout of 5000ms
- Returns true if 200 OK, false otherwise
- Catches all errors and returns false

### Step 3: Create Connection Error Component

File: `Aura.Web/src/components/ConnectionError.tsx`

Create React component with:

**Props:**
- onRetry?: () => void

**State:**
- healthStatus: HealthStatus | null
- isChecking: boolean
- countdown: number (starts at 10)

**Effects:**
- Auto-retry countdown that calls checkServerHealth() every 10 seconds
- Initial health check on mount

**Methods:**
- checkServerHealth(): Calls health API, updates state, auto-retries if healthy
- handleManualRetry(): Resets countdown and checks health immediately

**UI rendering based on status:**

When backend not reachable (healthStatus === null):
- Show "Backend Server Not Reachable" title
- Explain possible causes (server starting, not running, network issue)
- Show step-by-step instructions to start backend:
  1. Open terminal in project root
  2. Run: `dotnet run --project Aura.Api`
  3. Wait for "Application started" message
  4. Page will auto-retry in X seconds
- Use MessageBar component with error intent
- Show countdown timer

When backend degraded (healthStatus.status === 'degraded'):
- Show "Backend Server Has Issues" title
- Explain that server is running but has database issues
- List detected issues from healthStatus.database.issues
- Show step-by-step instructions to fix:
  1. Stop backend server (Ctrl+C)
  2. Restart with: `dotnet run --project Aura.Api`
  3. Wait for "✓ Database migrations applied successfully"
- Use MessageBar component with warning intent
- Show countdown timer

**Action buttons:**
- "Retry Now" button (primary) that calls handleManualRetry()
- Show spinner when isChecking is true
- Show last checked timestamp

**Warnings section:**
- If healthStatus.database.warnings exists and has items
- Show separate MessageBar with warning intent
- List all warnings

### Step 4: Update Setup Flow to Use Connection Error Component

File: `Aura.Web/src/pages/SetupFlow.tsx`

Update component to:
1. Add state for isConnected (boolean | null, starts as null)
2. Create checkConnection async function that:
   - Calls checkBasicHealth()
   - Sets isConnected state
   - If connected, navigates to /setup/welcome
3. Call checkConnection on mount using useEffect
4. Show loading spinner while isConnected === null
5. Show ConnectionError component when isConnected === false
6. Pass checkConnection as onRetry prop to ConnectionError
7. Return null when connected (will navigate away)

### Step 5: Add Styling and UX Polish

The ConnectionError component should:
- Be centered on page with max-width of 800px
- Have 20px padding
- Show MessageBar with appropriate intent colors
- Use monospace font for code snippets
- Style code snippets with light gray background and border radius
- Show countdown timer in smaller, muted text
- Space buttons and text appropriately
- Be responsive and mobile-friendly

### Step 6: Create Unit Tests

File: `Aura.Tests/Api/Controllers/HealthControllerTests.cs`

Create tests that verify:
1. GET /api/health returns 200 OK when healthy
2. GET /api/health/detailed returns 200 OK when healthy
3. GET /api/health/detailed returns 503 when database has issues
4. GET /api/health/detailed shows database issues correctly
5. GET /api/health/detailed shows pending migrations as warnings

File: `Aura.Web/src/components/__tests__/ConnectionError.test.tsx`

Create tests that verify:
1. Component shows correct message when backend unreachable
2. Component shows correct message when backend degraded
3. Auto-retry countdown works correctly
4. Manual retry button works
5. Component calls onRetry when backend becomes healthy

## Acceptance Criteria

- [ ] Health endpoint returns detailed status including database issues
- [ ] Frontend shows helpful, actionable error messages
- [ ] Auto-retry mechanism works correctly with countdown
- [ ] Manual retry button works immediately
- [ ] Different messages shown for unreachable vs degraded states
- [ ] Step-by-step fix instructions are clear and accurate
- [ ] Component is styled professionally and is mobile-friendly
- [ ] Unit tests pass for both backend and frontend
- [ ] Warnings are displayed separately from errors
- [ ] Countdown timer is visible and accurate

## Build Enforcement
- All TypeScript must be strongly typed (no 'any')
- All React components must use hooks (no class components)
- All error handling must be explicit
- All UI text must be clear and actionable
- Tests must cover all component states
