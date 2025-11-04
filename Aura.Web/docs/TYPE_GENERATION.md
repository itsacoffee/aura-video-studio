# API Type Generation

This document explains how to generate TypeScript types from the OpenAPI specification.

## Overview

The frontend uses automatically generated TypeScript types from the backend's OpenAPI (Swagger) specification. This ensures type safety and keeps frontend and backend in sync.

## Prerequisites

1. The backend API must be running locally
2. The API must be accessible at `http://localhost:5005` (or configured URL)
3. Swagger must be enabled (it is by default in all environments)

## Generating Types

### Step 1: Start the API

```bash
# From the repository root
cd Aura.Api
dotnet run
```

Wait for the API to start. You should see output indicating the server is running on `http://localhost:5005`.

### Step 2: Generate Types

```bash
# From Aura.Web directory
npm run generate:api-types
```

This will:

1. Download the OpenAPI spec from `/swagger/v1/swagger.json`
2. Generate TypeScript types using `openapi-typescript`
3. Save the types to `src/api/generated/schema.ts`
4. Create a barrel export at `src/api/generated/index.ts`

### Step 3: Verify Generation

Check that the following files were created/updated:

- `src/api/generated/schema.ts` - Generated TypeScript types
- `src/api/generated/index.ts` - Barrel export

## Using Generated Types

### Basic Usage

```typescript
import type { paths, components } from '@/api/generated';

// Use path types for request/response
type JobsGetResponse =
  paths['/api/jobs/{id}']['get']['responses']['200']['content']['application/json'];

// Use component schemas directly
type JobDto = components['schemas']['JobDto'];
```

### With openapi-fetch (Future)

When we fully migrate to openapi-fetch:

```typescript
import createClient from 'openapi-fetch';
import type { paths } from '@/api/generated';

const client = createClient<paths>({ baseUrl: 'http://localhost:5005' });

// Fully typed requests
const { data, error } = await client.GET('/api/jobs/{id}', {
  params: { path: { id: '123' } },
});

// TypeScript knows the shape of data and error
if (data) {
  console.log(data.status); // ✅ Type-safe
}
```

## Configuration

### Custom Swagger URL

To generate from a different URL:

```bash
npm run generate:api-types http://production-api.com/swagger/v1/swagger.json
```

### CI/CD Integration

Add the generation step to your CI pipeline:

```yaml
# .github/workflows/build.yml
- name: Start API
  run: |
    cd Aura.Api
    dotnet run &
    sleep 10  # Wait for API to start

- name: Generate API Types
  run: |
    cd Aura.Web
    npm run generate:api-types

- name: Verify Types
  run: |
    cd Aura.Web
    npm run type-check
```

## Troubleshooting

### API Not Running

**Error:** `Failed to download swagger.json`

**Solution:** Ensure the API is running:

```bash
cd Aura.Api
dotnet run
```

### Connection Refused

**Error:** `curl: (7) Failed to connect to localhost port 5005`

**Solution:** Check if another process is using port 5005, or wait longer for the API to start.

### Invalid OpenAPI Spec

**Error:** `Failed to generate types`

**Solution:**

1. Download the spec manually: `curl http://localhost:5005/swagger/v1/swagger.json > swagger.json`
2. Validate it at https://validator.swagger.io/
3. Fix any issues in the backend API

### Type Errors After Generation

**Error:** TypeScript errors in components using API types

**Solution:**

1. Run `npm run type-check` to see all errors
2. Update component types to match the new schema
3. Check for breaking changes in the API

## Best Practices

1. **Generate Regularly**: Regenerate types whenever the backend API changes
2. **Commit Generated Files**: Check in `schema.ts` so other developers have types
3. **Version Control**: Track changes to understand API evolution
4. **Validate Before Merge**: Run type generation in CI to catch drift
5. **Document Changes**: When the API changes significantly, update this guide

## Manual Type Updates

If you need to manually update types (not recommended):

1. **Don't** edit `schema.ts` directly - it will be overwritten
2. **Do** create separate type definitions in `src/types/`
3. **Do** use TypeScript's utility types to transform generated types

Example:

```typescript
import type { components } from '@/api/generated';

// Extend a generated type
type ExtendedJob = components['schemas']['JobDto'] & {
  localId: string;
  cached: boolean;
};

// Pick specific fields
type JobSummary = Pick<components['schemas']['JobDto'], 'id' | 'status' | 'createdAt'>;
```

## Future Enhancements

- [ ] Automatic generation on API changes (file watcher)
- [ ] Multiple API versions (v1, v2)
- [ ] Mock data generation from schemas
- [ ] Validation at runtime with generated schemas
- [ ] Migration to openapi-fetch for full type safety

## Related Documentation

- [API Client Guide](./API_CLIENT_GUIDE.md)
- [OpenAPI Specification](https://swagger.io/specification/)
- [openapi-typescript](https://github.com/drwpow/openapi-typescript)
