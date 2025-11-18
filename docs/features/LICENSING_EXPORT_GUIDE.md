# Licensing Export Guide

## Overview

The Licensing and Provenance Export feature provides a unified manifest of all assets used in a video project, including their licensing information, attribution requirements, and commercial use permissions. This ensures legal compliance and proper credit attribution for all content sources.

## Features

### Unified Manifest

- **Comprehensive Asset Tracking**: Tracks all assets across visuals, music, sound effects, narration, and captions
- **Licensing Information**: Records license type, commercial use permissions, and attribution requirements for each asset
- **Provenance Data**: Captures source provider, creator information, and asset URLs
- **Scene-level Tracking**: Associates each asset with its specific scene in the timeline

### Export Formats

The manifest can be exported in multiple formats:

- **JSON**: Structured data format for programmatic access
- **CSV**: Spreadsheet-compatible format for easy review
- **HTML**: Human-readable web page with formatted tables
- **Text**: Plain text format for documentation

### Pre-Export Sign-Off

Before exporting, users must acknowledge:

1. Commercial use restrictions (if any)
2. Attribution requirements
3. All warnings about missing or incomplete licensing information

This ensures conscious decision-making and legal compliance.

## API Endpoints

### Generate Manifest

```
POST /api/licensing/manifest/generate
```

**Request Body:**
```json
{
  "projectId": "job-123",
  "timelineData": { ... }
}
```

**Response:**
```json
{
  "projectId": "job-123",
  "projectName": "My Video Project",
  "generatedAt": "2024-01-15T10:30:00Z",
  "assets": [...],
  "allCommercialUseAllowed": false,
  "warnings": ["Scene 2: Visual source is unknown"],
  "missingLicensingInfo": ["Scene 2: Visual licensing information"],
  "summary": {
    "totalAssets": 15,
    "assetsByType": {...},
    "assetsBySource": {...},
    "assetsByLicenseType": {...},
    "assetsRequiringAttribution": 5,
    "assetsWithCommercialRestrictions": 2
  }
}
```

### Get Manifest

```
GET /api/licensing/manifest/{projectId}
```

Returns the cached or newly generated licensing manifest for a project.

### Export Manifest

```
POST /api/licensing/manifest/export
```

**Request Body:**
```json
{
  "projectId": "job-123",
  "format": "json"
}
```

**Response:**
```json
{
  "format": "json",
  "content": "...",
  "filename": "licensing-job-123.json",
  "contentType": "application/json"
}
```

### Download Manifest

```
GET /api/licensing/manifest/{projectId}/download?format=json
```

Returns the manifest as a downloadable file with appropriate Content-Disposition headers.

### Record Sign-Off

```
POST /api/licensing/signoff
```

**Request Body:**
```json
{
  "projectId": "job-123",
  "acknowledgedCommercialRestrictions": true,
  "acknowledgedAttributionRequirements": true,
  "acknowledgedWarnings": true,
  "notes": "Reviewed all assets, confirmed compliance"
}
```

**Response:**
```json
{
  "projectId": "job-123",
  "signedOffAt": "2024-01-15T10:35:00Z",
  "message": "Licensing sign-off recorded successfully"
}
```

### Validate Manifest

```
GET /api/licensing/manifest/{projectId}/validate
```

**Response:**
```json
{
  "projectId": "job-123",
  "isValid": true,
  "hasWarnings": true,
  "hasMissingInfo": true,
  "commercialUseAllowed": false,
  "warnings": ["Scene 2: Visual source is unknown"],
  "missingInfo": ["Scene 2: Visual licensing information"]
}
```

## Frontend Integration

### Using the LicensingExportPage Component

```tsx
import LicensingExportPage from './pages/Export/LicensingExportPage';

<LicensingExportPage projectId="job-123" />
```

### Using the API Client Directly

```typescript
import {
  getLicensingManifest,
  exportLicensingManifest,
  recordLicensingSignOff,
  downloadManifestFile,
} from './services/api/licensingApi';

// Get manifest
const manifest = await getLicensingManifest('job-123');

// Export manifest
const result = await exportLicensingManifest({
  projectId: 'job-123',
  format: 'json',
});

// Download exported file
downloadManifestFile(result.content, result.filename, result.contentType);

// Record sign-off
await recordLicensingSignOff({
  projectId: 'job-123',
  acknowledgedCommercialRestrictions: true,
  acknowledgedAttributionRequirements: true,
  acknowledgedWarnings: true,
});
```

## Asset Types

The system tracks the following asset types:

- **Visual**: Images and video clips used in scenes
- **Music**: Background music tracks
- **SoundEffect**: Sound effects (SFX)
- **Narration**: Text-to-speech generated audio
- **Caption**: Subtitles and text overlays
- **Video**: Video files (for future use)

## Licensing Information

For each asset, the following information is captured:

- **License Type**: e.g., "CC0", "CC BY", "Pexels License", "Commercial"
- **License URL**: Link to full license terms
- **Commercial Use Allowed**: Boolean indicating if asset can be used commercially
- **Attribution Required**: Boolean indicating if attribution is needed
- **Attribution Text**: Specific text to use for attribution (if required)
- **Creator**: Name of the content creator
- **Creator URL**: Link to creator's profile or website
- **Source URL**: Original location of the asset

## Warnings and Missing Information

The system generates warnings for:

- Assets with unknown source providers
- Assets missing licensing information
- Assets with commercial use restrictions when commercial use is intended
- Assets requiring attribution when attribution text is missing

## Best Practices

### For Developers

1. **Always generate manifest before export**: Ensure all licensing data is up-to-date
2. **Validate manifest**: Check for warnings and missing information
3. **Enforce sign-off**: Require user acknowledgment of restrictions and requirements
4. **Cache manifests**: Use the manifest cache for performance, but regenerate when timeline changes
5. **Provide clear UI feedback**: Show warnings prominently to users

### For Users

1. **Review warnings carefully**: Address missing licensing information before export
2. **Understand commercial restrictions**: Ensure your use case complies with asset licenses
3. **Keep attribution records**: Export and store licensing manifests for future reference
4. **Document sign-off**: Use the notes field to record compliance decisions
5. **Update manifests**: Regenerate manifests if assets are changed or replaced

## Integration with Content Safety

The licensing export feature complements the Content Safety system by:

- Ensuring legal compliance alongside content appropriateness
- Providing audit trails for asset sourcing
- Supporting safe and compliant content distribution

## Future Enhancements

Planned improvements include:

- **LLM-assisted attribution composition**: Automatically format attribution text based on license requirements
- **Missing field detection**: Guided checklist to fill in incomplete licensing information
- **Video metadata injection**: Embed licensing information directly in video file metadata
- **Batch export**: Export manifests for multiple projects simultaneously
- **License compatibility checker**: Validate that all asset licenses are compatible with each other

## Support

For questions or issues with the licensing export feature:

1. Check the API documentation for endpoint details
2. Review error messages in the browser console
3. Verify that all required fields are filled in the sign-off form
4. Contact the development team for assistance

## References

- API Models
- Core Models
- Licensing Service
- Controller
- Frontend Types
- API Client
- Export Page Component
