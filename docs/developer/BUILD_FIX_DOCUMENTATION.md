# Build Fix Documentation

## Issue
The portable build script (`scripts/packaging/build-portable.ps1`) was failing at step [3/6] with the error:
```
Error: npm build failed
```

## Root Cause
The npm build (`npm run build`) in the `Aura.Web` directory was failing due to **TypeScript compilation errors**, not npm installation issues.

## Specific Errors Fixed

### Previous Fixes
1. **LocalEngines.tsx** (5 errors)
   - Removed unused imports: `Tooltip`, `DialogTrigger`, `ErrorCircle20Filled`
   - Removed unused variable: `engines` from useEnginesStore destructuring
   - Fixed type error: `parseInt()` can return `NaN`, and `engineConfig.defaultPort` is optional (`number | undefined`)
   
2. **ProviderSelection.tsx** (1 error)
   - Removed unused variable: `engines` from useEnginesStore destructuring

3. **engine-workflows.test.ts** (1 error)
   - Removed unused import: `vi` from vitest

### Latest Fix (2025-10-12)
4. **FailureModal.tsx** (1 error)
   - Fixed unused parameter error: `jobId` is declared but never used
   - Solution: Renamed parameter to `_jobId` to indicate it's intentionally unused
   - This preserves the interface for potential future use while satisfying TypeScript

## Solution Applied
### LocalEngines.tsx Changes
```typescript
// Before: Unused imports
import { Tooltip, Dialog, DialogTrigger, ... } from '@fluentui/react-components';
import { ErrorCircle20Filled, ... } from '@fluentui/react-icons';

// After: Only used imports
import { Dialog, ... } from '@fluentui/react-components';
// ErrorCircle20Filled removed

// Before: Unused variable
const { engines, engineStatuses, ... } = useEnginesStore();

// After: Only used variables
const { engineStatuses, ... } = useEnginesStore();

// Before: Type error - parseInt can return NaN, defaultPort can be undefined
onChange={(e) => updateEngineConfig(engineConfig.id, 'port', parseInt(e.target.value) || engineConfig.defaultPort)}

// After: Proper NaN and undefined handling
onChange={(e) => {
  const parsedValue = parseInt(e.target.value);
  if (!isNaN(parsedValue)) {
    updateEngineConfig(engineConfig.id, 'port', parsedValue);
  } else if (engineConfig.defaultPort !== undefined) {
    updateEngineConfig(engineConfig.id, 'port', engineConfig.defaultPort);
  }
}}
```

### Other File Changes
- **ProviderSelection.tsx**: Removed `engines` from destructuring
- **engine-workflows.test.ts**: Removed `vi` from import
- **FailureModal.tsx** (Latest): Renamed `jobId` parameter to `_jobId` to indicate intentionally unused

### Additional Fix
Removed `Aura.Web/pnpm-lock.yaml` to prevent package manager conflicts (repository uses npm, not pnpm).

## Verification
After the fix:
- ✅ `npm run build` completes successfully
- ✅ `npm run typecheck` reports no errors
- ✅ `npm test` passes all 51 unit tests
- ✅ Production bundle generated: 865 KB (gzipped: 239 KB)
- ✅ All .NET projects build successfully

## How to Build Now
```powershell
# From scripts/packaging directory
.\build-portable.ps1
```

This will:
1. Build all .NET projects
2. Install npm dependencies (if needed)
3. Build the web UI successfully
4. Publish the API
5. Copy web UI to wwwroot
6. Create portable ZIP

## Prevention
To prevent similar issues in the future:
1. Always run `npm run typecheck` before committing Aura.Web changes
2. Ensure `npm run build` succeeds locally before pushing
3. Use `npm run test` to catch issues early
4. Keep only one package manager lock file (package-lock.json for npm)
