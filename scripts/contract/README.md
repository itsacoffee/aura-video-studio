# API Contract Type Generation

This directory contains scripts to generate TypeScript types from the backend API OpenAPI specification.

## Overview

The API V1 contract is maintained in `Aura.Api/Models/ApiModels.V1/` and exposed via OpenAPI/Swagger. TypeScript types are generated from this spec to ensure perfect synchronization between frontend and backend.

## Scripts

### `generate-api-v1-types.js` (Node.js)

Generates TypeScript types for the API V1 contract.

**Prerequisites:**
- Node.js installed
- .NET SDK installed
- API project built (`dotnet build`)

**Usage:**
```bash
# From repository root
node scripts/contract/generate-api-v1-types.js

# Or if you add npm script to Aura.Web/package.json:
cd Aura.Web
npm run generate:api-types
```

**What it does:**
1. Temporarily starts the API server on port 5000
2. Fetches the OpenAPI JSON spec from `/swagger/v1/swagger.json`
3. Uses `openapi-typescript` to generate TypeScript types
4. Saves to `Aura.Web/src/types/api-v1.ts` with auto-generated header
5. Stops the API server

### `generate-api-v1-types.ps1` (PowerShell)

Windows-friendly PowerShell version with the same functionality.

**Usage:**
```powershell
# From repository root
.\scripts\contract\generate-api-v1-types.ps1
```

## Generated Output

The generated `Aura.Web/src/types/api-v1.ts` file contains:

- TypeScript interfaces for all API request/response DTOs
- Enum types matching the backend enums
- Path operations and parameters
- Auto-generated header with timestamp and regeneration instructions

**Example:**
```typescript
/**
 * AUTO-GENERATED - DO NOT EDIT
 * 
 * API V1 Type Definitions
 * Generated from OpenAPI spec at http://localhost:5000/swagger/v1/swagger.json
 * 
 * To regenerate:
 *   node scripts/contract/generate-api-v1-types.js
 * 
 * Last generated: 2025-10-11T00:45:00.000Z
 */

export interface ScriptRequest {
  topic: string;
  audience: string;
  // ... etc
}

export enum Pacing {
  Chill = "Chill",
  Conversational = "Conversational",
  Fast = "Fast"
}
```

## When to Regenerate

Regenerate TypeScript types when:

- Adding/modifying/removing API endpoints
- Changing request/response DTOs in `ApiModels.V1/`
- Adding/modifying enum values
- After merging changes that affect the API contract

## Manual Type Sync (Alternative)

If you prefer not to use auto-generation, you can manually keep types in sync:

1. Make changes to `Aura.Api/Models/ApiModels.V1/`
2. Manually update corresponding interfaces in `Aura.Web/src/types/api-v1.ts`
3. Ensure enum values match exactly

However, **auto-generation is strongly recommended** to prevent drift between backend and frontend.

## Troubleshooting

### Port 5000 already in use
If port 5000 is occupied, either:
- Stop the process using port 5000
- Modify the scripts to use a different port (update `API_PORT` and `SWAGGER_URL`)

### openapi-typescript not found
The script will attempt to install it automatically. If that fails:
```bash
npm install -g openapi-typescript
```

### API server fails to start
Ensure:
- .NET SDK is installed and in PATH
- API project builds successfully: `dotnet build Aura.Api/Aura.Api.csproj`
- No other instance of the API is running

### Generated types look wrong
Check:
- Swashbuckle is properly configured in `Program.cs`
- All DTOs have proper XML documentation comments
- Enums use `[JsonConverter]` attributes if needed
- OpenAPI spec renders correctly in browser: http://localhost:5000/swagger

## Integration with Build Process

Consider adding to CI/CD:

```yaml
# .github/workflows/contract-validation.yml
- name: Generate and validate API types
  run: |
    node scripts/contract/generate-api-v1-types.js
    git diff --exit-code Aura.Web/src/types/api-v1.ts
```

This ensures the generated types are always up-to-date with the backend contract.
