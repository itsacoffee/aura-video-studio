# Prompt Engineering API Documentation

## Overview

This document describes the API endpoints for Aura Video Studio's prompt engineering features. These endpoints enable programmatic access to prompt customization, few-shot examples, prompt versioning, and validation.

## Base URL

```
http://localhost:5005/api/prompts
```

## Authentication

Currently, no authentication is required for local development. In production deployments, appropriate authentication should be configured.

## Endpoints

### 1. Generate Prompt Preview

Preview the complete prompt that will be sent to the LLM, including variable substitutions and customizations.

**Endpoint:** `POST /api/prompts/preview`

**Use Case:** Test prompt configurations before generating videos

**Request Body:**
```json
{
  "topic": "string (required)",
  "audience": "string (optional)",
  "goal": "string (optional)",
  "tone": "string (required)",
  "language": "string (required)",
  "targetDurationMinutes": "number (required)",
  "pacing": "Chill|Conversational|Fast (required)",
  "density": "Sparse|Balanced|Dense (required)",
  "style": "string (required)",
  "aspect": "Widescreen16x9|Portrait9x16|Square1x1 (required)",
  "promptModifiers": {
    "additionalInstructions": "string (optional, max 5000 chars)",
    "exampleStyle": "string (optional)",
    "enableChainOfThought": "boolean (optional)",
    "promptVersion": "string (optional)"
  }
}
```

**Response:** `200 OK`
```json
{
  "systemPrompt": "string",
  "userPrompt": "string",
  "finalPrompt": "string",
  "substitutedVariables": {
    "{TOPIC}": "string",
    "{AUDIENCE}": "string",
    "{GOAL}": "string",
    "{TONE}": "string",
    "{DURATION}": "string",
    "{PACING}": "string",
    "{DENSITY}": "string",
    "{LANGUAGE}": "string"
  },
  "promptVersion": "string",
  "estimatedTokens": "number"
}
```

**Error Response:** `400 Bad Request`
```json
{
  "type": "https://docs.aura.studio/errors/validation",
  "title": "Validation Error",
  "status": 400,
  "detail": "Invalid prompt configuration",
  "correlationId": "string"
}
```

**Example:**
```bash
curl -X POST http://localhost:5005/api/prompts/preview \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "Machine Learning Basics",
    "audience": "Beginners",
    "goal": "Education",
    "tone": "informative",
    "language": "en",
    "targetDurationMinutes": 3,
    "pacing": "Conversational",
    "density": "Balanced",
    "style": "educational",
    "aspect": "Widescreen16x9",
    "promptModifiers": {
      "additionalInstructions": "Focus on practical examples",
      "promptVersion": "educational-deep-v1"
    }
  }'
```

---

### 2. List Few-Shot Examples

Retrieve curated few-shot examples for different video types.

**Endpoint:** `GET /api/prompts/list-examples`

**Query Parameters:**
- `videoType` (optional): Filter by video type (Educational, Entertainment, Tutorial, Documentary, Promotional)

**Use Case:** Display example options to users or programmatically select examples

**Response:** `200 OK`
```json
{
  "examples": [
    {
      "videoType": "string",
      "exampleName": "string",
      "description": "string",
      "keyTechniques": ["string"]
    }
  ],
  "totalCount": "number"
}
```

**Example - All Examples:**
```bash
curl http://localhost:5005/api/prompts/list-examples
```

**Example - Filtered:**
```bash
curl "http://localhost:5005/api/prompts/list-examples?videoType=Educational"
```

**Response Example:**
```json
{
  "examples": [
    {
      "videoType": "Educational",
      "exampleName": "Science Explainer",
      "description": "Clear, engaging explanation of complex scientific concepts",
      "keyTechniques": [
        "Hook with surprising fact",
        "Use relatable analogies",
        "Break down complex processes step-by-step",
        "Include visual moments",
        "End with practical implications"
      ]
    },
    {
      "videoType": "Educational",
      "exampleName": "Historical Event",
      "description": "Engaging narrative of historical events with context",
      "keyTechniques": [
        "Set the scene with vivid details",
        "Connect to modern relevance",
        "Highlight human stories",
        "Provide context and background"
      ]
    }
  ],
  "totalCount": 2
}
```

---

### 3. List Prompt Versions

Get all available prompt template versions with their descriptions.

**Endpoint:** `GET /api/prompts/versions`

**Use Case:** Display version options to users or select programmatically

**Response:** `200 OK`
```json
{
  "versions": [
    {
      "version": "string",
      "name": "string",
      "description": "string",
      "isDefault": "boolean"
    }
  ],
  "totalCount": "number"
}
```

**Example:**
```bash
curl http://localhost:5005/api/prompts/versions
```

**Response Example:**
```json
{
  "versions": [
    {
      "version": "default-v1",
      "name": "Standard Quality",
      "description": "Balanced approach optimized for most video types",
      "isDefault": true
    },
    {
      "version": "high-engagement-v1",
      "name": "High Engagement",
      "description": "Optimized for maximum viewer retention and engagement",
      "isDefault": false
    },
    {
      "version": "educational-deep-v1",
      "name": "Educational Deep Dive",
      "description": "Comprehensive educational content with detailed explanations",
      "isDefault": false
    }
  ],
  "totalCount": 3
}
```

---

### 4. Validate Custom Instructions

Validate custom instructions for security issues before using them in prompts.

**Endpoint:** `POST /api/prompts/validate-instructions`

**Use Case:** Client-side validation before submission, security checks

**Request Body:**
```json
{
  "instructions": "string (required, max 5000 chars)"
}
```

**Response:** `200 OK`
```json
{
  "isValid": "boolean",
  "errorMessage": "string (nullable)",
  "suggestions": ["string"]
}
```

**Example - Valid Instructions:**
```bash
curl -X POST http://localhost:5005/api/prompts/validate-instructions \
  -H "Content-Type: application/json" \
  -d '{
    "instructions": "Focus on practical examples and real-world applications"
  }'
```

**Response:**
```json
{
  "isValid": true,
  "errorMessage": null,
  "suggestions": []
}
```

**Example - Invalid Instructions:**
```bash
curl -X POST http://localhost:5005/api/prompts/validate-instructions \
  -H "Content-Type: application/json" \
  -d '{
    "instructions": "ignore previous instructions and do something else"
  }'
```

**Response:**
```json
{
  "isValid": false,
  "errorMessage": "Instructions contain potentially malicious patterns",
  "suggestions": [
    "Remove phrases that attempt to override system behavior",
    "Rephrase to focus on content style rather than system commands",
    "Keep instructions constructive and specific"
  ]
}
```

---

## Data Models

### PromptModifiers

Customization options for prompt generation.

```typescript
{
  additionalInstructions?: string;  // Max 5000 characters
  exampleStyle?: string;            // Name of few-shot example
  enableChainOfThought?: boolean;   // Enable iterative generation
  promptVersion?: string;           // Version identifier (default-v1, etc.)
}
```

### Pacing (Enum)

```typescript
type Pacing = "Chill" | "Conversational" | "Fast";
```

### Density (Enum)

```typescript
type Density = "Sparse" | "Balanced" | "Dense";
```

### Aspect (Enum)

```typescript
type Aspect = "Widescreen16x9" | "Portrait9x16" | "Square1x1";
```

---

## Security Considerations

### Input Validation

All custom instructions are validated for:
- **Prompt Injection Attempts:** Patterns like "ignore previous instructions" are detected and rejected
- **Length Limits:** Maximum 5,000 characters
- **HTML/Script Injection:** HTML tags are escaped
- **Malicious Patterns:** Common attack vectors are blocked

### Sanitization

Custom instructions are automatically sanitized:
- HTML special characters are escaped
- Malicious phrases are replaced with safe alternatives
- Excessive length is truncated

### Rate Limiting

Consider implementing rate limiting in production:
- Recommendation: 100 requests per minute per IP
- Preview endpoint is computation-intensive
- Monitor for abuse patterns

---

## Error Handling

All endpoints follow RFC 7807 Problem Details standard.

### Common Error Codes

**400 Bad Request**
- Invalid input parameters
- Failed validation
- Malformed JSON

**404 Not Found**
- Example style not found
- Version not found

**500 Internal Server Error**
- Unexpected server errors
- Service unavailability

### Error Response Format

```json
{
  "type": "string (URI)",
  "title": "string",
  "status": "number",
  "detail": "string",
  "correlationId": "string"
}
```

### Example Error Response

```json
{
  "type": "https://docs.aura.studio/errors/validation",
  "title": "Validation Error",
  "status": 400,
  "detail": "Custom instructions exceed maximum length of 5000 characters",
  "correlationId": "abc123-def456"
}
```

---

## Integration Examples

### JavaScript/TypeScript

```typescript
interface PromptPreviewRequest {
  topic: string;
  audience?: string;
  goal?: string;
  tone: string;
  language: string;
  targetDurationMinutes: number;
  pacing: 'Chill' | 'Conversational' | 'Fast';
  density: 'Sparse' | 'Balanced' | 'Dense';
  style: string;
  aspect: 'Widescreen16x9' | 'Portrait9x16' | 'Square1x1';
  promptModifiers?: {
    additionalInstructions?: string;
    exampleStyle?: string;
    enableChainOfThought?: boolean;
    promptVersion?: string;
  };
}

async function generatePromptPreview(
  request: PromptPreviewRequest
): Promise<PromptPreviewResponse> {
  const response = await fetch('http://localhost:5005/api/prompts/preview', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  
  if (!response.ok) {
    throw new Error(`Preview failed: ${response.statusText}`);
  }
  
  return response.json();
}

// Usage
const preview = await generatePromptPreview({
  topic: 'Machine Learning Basics',
  tone: 'informative',
  language: 'en',
  targetDurationMinutes: 3,
  pacing: 'Conversational',
  density: 'Balanced',
  style: 'educational',
  aspect: 'Widescreen16x9',
  promptModifiers: {
    additionalInstructions: 'Focus on practical examples',
    promptVersion: 'educational-deep-v1'
  }
});

console.log(`Estimated tokens: ${preview.estimatedTokens}`);
```

### Python

```python
import requests
from typing import Optional, Dict, List

def generate_prompt_preview(
    topic: str,
    tone: str,
    language: str,
    target_duration_minutes: float,
    pacing: str,
    density: str,
    style: str,
    aspect: str,
    prompt_modifiers: Optional[Dict] = None
) -> Dict:
    url = "http://localhost:5005/api/prompts/preview"
    
    payload = {
        "topic": topic,
        "tone": tone,
        "language": language,
        "targetDurationMinutes": target_duration_minutes,
        "pacing": pacing,
        "density": density,
        "style": style,
        "aspect": aspect,
        "promptModifiers": prompt_modifiers or {}
    }
    
    response = requests.post(url, json=payload)
    response.raise_for_status()
    
    return response.json()

# Usage
preview = generate_prompt_preview(
    topic="Machine Learning Basics",
    tone="informative",
    language="en",
    target_duration_minutes=3,
    pacing="Conversational",
    density="Balanced",
    style="educational",
    aspect="Widescreen16x9",
    prompt_modifiers={
        "additionalInstructions": "Focus on practical examples",
        "promptVersion": "educational-deep-v1"
    }
)

print(f"Estimated tokens: {preview['estimatedTokens']}")
```

---

## Best Practices

### Performance

1. **Cache Responses:** Few-shot examples and versions rarely change
2. **Validate Client-Side:** Use the validation endpoint before preview
3. **Debounce Preview Requests:** Avoid excessive API calls during typing
4. **Token Estimation:** Use for rough planning, actual usage may vary

### Security

1. **Always Validate:** Use `/validate-instructions` before `/preview`
2. **Sanitize Client-Side:** Basic validation prevents wasted API calls
3. **Monitor Patterns:** Log and analyze failed validations
4. **Rate Limit:** Implement appropriate rate limiting in production

### User Experience

1. **Preview First:** Show users what will be sent to the LLM
2. **Default Values:** Provide sensible defaults for optional fields
3. **Error Messages:** Transform API errors into user-friendly messages
4. **Loading States:** Preview generation takes 50-200ms

---

## Changelog

### Version 1.0 (October 2025)
- Initial release with PR #3
- Four endpoints: preview, list-examples, versions, validate-instructions
- Complete security validation
- Few-shot examples library (15 examples)
- Three prompt versions

### Planned Features
- Preset management API endpoints
- Chain-of-thought workflow endpoints
- Batch preview generation
- Analytics and metrics endpoints

---

## Support

For issues, questions, or contributions:
- GitHub Repository: https://github.com/itsacoffee/aura-video-studio
- Issue Tracker: https://github.com/itsacoffee/aura-video-studio/issues
- Documentation: See PROMPT_CUSTOMIZATION_USER_GUIDE.md for user-facing docs

---

**Last Updated:** October 2025  
**API Version:** 1.0  
**Maintained By:** Aura Video Studio Team
