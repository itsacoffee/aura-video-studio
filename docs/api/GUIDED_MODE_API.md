# Guided Mode API Documentation

## Overview

The Guided Mode API provides endpoints for AI-powered explanations, artifact improvements, constrained regeneration, and configuration management.

**Base URL**: `/api`

**Authentication**: Same as main API (if applicable)

## Endpoints

### Explain Controller

#### POST /explain/artifact

Explain an artifact (script, plan, brief) to the user.

**Request Body:**
```json
{
  "artifactType": "script",
  "artifactContent": "Scene 1: Introduction...",
  "specificQuestion": "Why is this scene 10 seconds?"
}
```

**Response:**
```json
{
  "success": true,
  "explanation": "This script contains 12 lines organized into scenes...",
  "keyPoints": [
    "Structured into timed scenes for natural pacing",
    "Narration matches target audience comprehension level",
    "Scene transitions maintain engagement"
  ],
  "errorMessage": null
}
```

**Parameters:**
- `artifactType` (string, required): Type of artifact (script, plan, brief)
- `artifactContent` (string, required): Content to explain
- `specificQuestion` (string, optional): Specific question about the artifact

**Status Codes:**
- `200`: Success
- `400`: Bad request (missing content)
- `500`: Server error

---

#### POST /explain/improve

Improve an artifact with a specific action.

**Request Body:**
```json
{
  "artifactType": "script",
  "artifactContent": "Scene 1: Introduction...",
  "improvementAction": "improve clarity",
  "targetAudience": "Beginners",
  "lockedSections": [
    {
      "startIndex": 0,
      "endIndex": 2,
      "content": "Scene 1: Opening...",
      "reason": "Brand messaging approved"
    }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "improvedContent": "Scene 1: Clear introduction...",
  "changesSummary": "Applied 'improve clarity' to script. Modified 8 sections while preserving 1 locked area.",
  "promptDiff": {
    "originalPrompt": "Generate script",
    "modifiedPrompt": "Generate script with improve clarity",
    "intendedOutcome": "Improved script with clarity enhancements applied",
    "changes": [
      {
        "type": "Action",
        "description": "Added improve clarity constraint",
        "oldValue": "Standard generation",
        "newValue": "improve clarity"
      }
    ]
  },
  "errorMessage": null
}
```

**Parameters:**
- `artifactType` (string, required): Type of artifact
- `artifactContent` (string, required): Current content
- `improvementAction` (string, required): Action to apply (improve clarity, adapt for audience, shorten, expand)
- `targetAudience` (string, optional): Target audience for adaptation
- `lockedSections` (array, optional): Sections to preserve during improvement

**Status Codes:**
- `200`: Success
- `400`: Bad request
- `500`: Server error

---

#### POST /explain/regenerate

Regenerate artifact with constraints (locked sections).

**Request Body:**
```json
{
  "artifactType": "script",
  "currentContent": "Scene 1: Introduction...",
  "regenerationType": "alternative",
  "lockedSections": [
    {
      "startIndex": 0,
      "endIndex": 2,
      "content": "Scene 1: Opening...",
      "reason": "Keep original intro"
    }
  ],
  "promptModifiers": {
    "additionalInstructions": "Focus on technical details",
    "enableChainOfThought": true
  }
}
```

**Response:**
```json
{
  "success": true,
  "regeneratedContent": "Scene 1: Opening... [locked]\nScene 2: Technical deep dive...",
  "promptDiff": {
    "originalPrompt": "Generate script",
    "modifiedPrompt": "Regenerate script with type 'alternative' preserving 1 locked sections",
    "intendedOutcome": "Regenerated script with locked sections preserved",
    "changes": [
      {
        "type": "Regeneration",
        "description": "Applying alternative regeneration",
        "oldValue": "Original content",
        "newValue": "Regenerated with alternative"
      },
      {
        "type": "Constraints",
        "description": "Preserving 1 locked sections",
        "oldValue": null,
        "newValue": "1 sections locked"
      }
    ]
  },
  "requiresConfirmation": true,
  "errorMessage": null
}
```

**Parameters:**
- `artifactType` (string, required): Type of artifact
- `currentContent` (string, required): Current content
- `regenerationType` (string, required): Type of regeneration
- `lockedSections` (array, optional): Sections to preserve
- `promptModifiers` (object, optional): Additional prompt customizations

**Status Codes:**
- `200`: Success
- `400`: Bad request
- `500`: Server error

---

#### POST /explain/prompt-diff

Get prompt diff preview before regeneration (without executing).

**Request Body:**
```json
{
  "artifactType": "script",
  "currentContent": "Scene 1: Introduction...",
  "regenerationType": "alternative",
  "lockedSections": []
}
```

**Response:**
```json
{
  "originalPrompt": "Standard script generation",
  "modifiedPrompt": "alternative regeneration with 0 locked sections",
  "intendedOutcome": "Regenerated script preserving specified sections",
  "changes": [
    {
      "type": "Operation",
      "description": "Regeneration type: alternative",
      "oldValue": "Standard generation",
      "newValue": "alternative"
    }
  ]
}
```

**Status Codes:**
- `200`: Success
- `500`: Server error

---

### Guided Mode Controller

#### GET /guidedmode/config

Get current guided mode configuration.

**Response:**
```json
{
  "enabled": true,
  "experienceLevel": "beginner",
  "showTooltips": true,
  "showWhyLinks": true,
  "requirePromptDiffConfirmation": true
}
```

**Status Codes:**
- `200`: Success
- `500`: Server error

---

#### POST /guidedmode/config

Update guided mode configuration.

**Request Body:**
```json
{
  "enabled": true,
  "experienceLevel": "intermediate",
  "showTooltips": false,
  "showWhyLinks": true,
  "requirePromptDiffConfirmation": true
}
```

**Response:**
```json
{
  "success": true,
  "config": {
    "enabled": true,
    "experienceLevel": "intermediate",
    "showTooltips": false,
    "showWhyLinks": true,
    "requirePromptDiffConfirmation": true
  }
}
```

**Status Codes:**
- `200`: Success
- `500`: Server error

---

#### GET /guidedmode/defaults/{experienceLevel}

Get default configuration for a specific experience level.

**Parameters:**
- `experienceLevel` (string, path): Experience level (beginner, intermediate, advanced)

**Response:**
```json
{
  "enabled": true,
  "experienceLevel": "beginner",
  "showTooltips": true,
  "showWhyLinks": true,
  "requirePromptDiffConfirmation": true
}
```

**Status Codes:**
- `200`: Success
- `500`: Server error

---

#### POST /guidedmode/telemetry

Track guided mode feature usage telemetry.

**Request Body:**
```json
{
  "featureUsed": "explain",
  "artifactType": "script",
  "durationMs": 1250,
  "success": true,
  "feedbackRating": "positive",
  "metadata": {
    "action": "improve_clarity",
    "lockedSections": "0"
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Telemetry recorded"
}
```

**Parameters:**
- `featureUsed` (string, required): Feature that was used
- `artifactType` (string, required): Type of artifact
- `durationMs` (number, required): Operation duration in milliseconds
- `success` (boolean, required): Whether operation succeeded
- `feedbackRating` (string, optional): User feedback (positive, negative)
- `metadata` (object, optional): Additional context

**Status Codes:**
- `200`: Success
- `500`: Server error

---

## Data Types

### LockedSectionDto

```typescript
{
  startIndex: number;      // Start line index
  endIndex: number;        // End line index
  content: string;         // Content of locked section
  reason: string;          // Reason for locking
}
```

### PromptDiffDto

```typescript
{
  originalPrompt: string;           // Original AI prompt
  modifiedPrompt: string;           // Modified AI prompt
  intendedOutcome: string;          // Expected result
  changes: PromptChangeDto[];       // List of changes
}
```

### PromptChangeDto

```typescript
{
  type: string;                     // Type of change
  description: string;              // Human-readable description
  oldValue?: string | null;         // Previous value
  newValue?: string | null;         // New value
}
```

### PromptModifiersDto

```typescript
{
  additionalInstructions?: string | null;
  exampleStyle?: string | null;
  enableChainOfThought?: boolean;
  promptVersion?: string | null;
}
```

## Error Handling

All endpoints return standardized error responses:

```json
{
  "success": false,
  "explanation": null,
  "keyPoints": null,
  "errorMessage": "Detailed error message"
}
```

### Common Error Codes

- `400 Bad Request`: Missing required parameters or invalid input
- `500 Internal Server Error`: Server-side processing error

## Rate Limiting

No specific rate limits applied currently. Standard API rate limits apply.

## Examples

### Example 1: Explain a Script

```bash
curl -X POST http://localhost:5005/api/explain/artifact \
  -H "Content-Type: application/json" \
  -d '{
    "artifactType": "script",
    "artifactContent": "Scene 1: Welcome to AI video generation...",
    "specificQuestion": null
  }'
```

### Example 2: Improve Script Clarity

```bash
curl -X POST http://localhost:5005/api/explain/improve \
  -H "Content-Type: application/json" \
  -d '{
    "artifactType": "script",
    "artifactContent": "Scene 1: Introduction...",
    "improvementAction": "improve clarity",
    "targetAudience": null,
    "lockedSections": []
  }'
```

### Example 3: Get Prompt Diff

```bash
curl -X POST http://localhost:5005/api/explain/prompt-diff \
  -H "Content-Type: application/json" \
  -d '{
    "artifactType": "script",
    "currentContent": "Scene 1...",
    "regenerationType": "alternative",
    "lockedSections": []
  }'
```

### Example 4: Track Telemetry

```bash
curl -X POST http://localhost:5005/api/guidedmode/telemetry \
  -H "Content-Type: application/json" \
  -d '{
    "featureUsed": "explain",
    "artifactType": "script",
    "durationMs": 1234,
    "success": true,
    "feedbackRating": "positive"
  }'
```

## Integration Notes

### Frontend Integration

```typescript
import { guidedModeService } from '@/services/guidedModeService';

// Explain artifact
const response = await guidedModeService.explainArtifact({
  artifactType: 'script',
  artifactContent: scriptText,
  specificQuestion: 'Why this pacing?'
});

// Improve artifact
const improved = await guidedModeService.improveArtifact({
  artifactType: 'script',
  artifactContent: scriptText,
  improvementAction: 'improve clarity',
  lockedSections: lockedSections
});

// Track telemetry
await guidedModeService.trackTelemetry({
  featureUsed: 'explain',
  artifactType: 'script',
  durationMs: 1500,
  success: true
});
```

### Backend Extension

To add new improvement actions:

1. Update `ExplainController.ApplyImprovementAsync()`
2. Add new case to switch statement
3. Implement improvement logic
4. Update API documentation

## Versioning

Current API version: **v1**

Breaking changes will be released as v2, v3, etc. with backward compatibility maintained for at least 6 months.

---

**Last Updated**: 2025-11-04  
**Maintained By**: Aura Video Studio Team
