# Troubleshooting Guide

## Enum Compatibility

Aura Video Studio has evolved its API to use more descriptive enum values. To maintain backward compatibility, the system now accepts both canonical names and legacy aliases.

### Density Values

The API accepts the following density values for content pacing:

| Canonical Value | Legacy Alias | Description |
|----------------|--------------|-------------|
| `Sparse` | - | Less content per minute, slower pacing |
| `Balanced` | `Normal` | Moderate content density (recommended) |
| `Dense` | - | More content per minute, faster pacing |

**Example:**
```json
{
  "density": "Balanced"  // Recommended
}
```

**Legacy Support:**
```json
{
  "density": "Normal"  // Still works, maps to "Balanced"
}
```

### Aspect Ratio Values

The API accepts the following aspect ratio values:

| Canonical Value | Legacy Alias | Description |
|----------------|--------------|-------------|
| `Widescreen16x9` | `16:9` | Standard widescreen (1920x1080) |
| `Vertical9x16` | `9:16` | Mobile/portrait format (1080x1920) |
| `Square1x1` | `1:1` | Square format (1080x1080) |

**Example:**
```json
{
  "aspect": "Widescreen16x9"  // Recommended
}
```

**Legacy Support:**
```json
{
  "aspect": "16:9"  // Still works, maps to "Widescreen16x9"
}
```

### Error Handling

If you provide an invalid or unsupported enum value, the API will return an RFC7807 ProblemDetails response with error code `E303`:

```json
{
  "type": "https://docs.aura.studio/errors/E303",
  "title": "Invalid Enum Value",
  "status": 400,
  "detail": "Unknown Density value: 'Medium'. Valid values are: Sparse, Balanced (or Normal), Dense"
}
```

The error message will include:
- The invalid value you provided
- A list of all valid values (including aliases)

### Best Practices

1. **Use Canonical Values**: Prefer canonical enum names (`Balanced`, `Widescreen16x9`) over legacy aliases for clarity
2. **Client-Side Validation**: The Web UI automatically normalizes enum values before sending requests
3. **Check Console Warnings**: When using legacy values, the Web UI logs compatibility warnings to help you migrate
4. **Update Your Code**: While legacy values are supported, consider updating to canonical names for better maintainability

### Migration Guide

If you're migrating from legacy enum values:

**Before:**
```typescript
const request = {
  density: "Normal",
  aspect: "16:9"
};
```

**After:**
```typescript
const request = {
  density: "Balanced",
  aspect: "Widescreen16x9"
};
```

### Client Libraries

If you're using the Aura API from external applications:

**JavaScript/TypeScript:**
```typescript
import { normalizeEnumsForApi } from './utils/enumNormalizer';

// Normalize before sending
const { brief, planSpec } = normalizeEnumsForApi(myBrief, myPlanSpec);
await fetch('/api/script', {
  method: 'POST',
  body: JSON.stringify({ ...brief, ...planSpec })
});
```

**C#:**
The API server automatically handles normalization - you can send either canonical or alias values.

### Common Issues

#### "Failed to generate script" with enum error

**Symptom:** Request fails with E303 error mentioning invalid enum value

**Solution:** 
1. Check that your enum values match either canonical names or supported aliases
2. Ensure proper casing (enums are case-insensitive for parsing, but canonical values use PascalCase)
3. Review the error message for the list of valid values

#### Console warnings about deprecated values

**Symptom:** Browser console shows warnings like "Density 'Normal' is deprecated"

**Solution:**
These are informational warnings and won't block your requests. To eliminate them, update your code to use canonical values.

### Additional Resources

- [API Reference](./API.md) - Complete API documentation
- [Error Codes](./ErrorCodes.md) - Full list of error codes and meanings
- [Examples](./Examples.md) - Working examples of API requests
