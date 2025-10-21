# Profile System API Reference

## Base URL
All endpoints are relative to: `/api/profiles`

## Authentication
Currently no authentication required (local API). Future versions may add user authentication.

---

## Endpoints

### 1. Get User Profiles
Get all profiles for a specific user.

**Endpoint:** `GET /api/profiles/user/{userId}`

**Parameters:**
- `userId` (path, required): User identifier

**Response:**
```json
{
  "success": true,
  "profiles": [
    {
      "profileId": "abc123",
      "profileName": "My Gaming Channel",
      "description": "Let's Play videos",
      "isDefault": true,
      "isActive": true,
      "lastUsed": "2025-10-21T20:00:00Z",
      "contentType": "gaming"
    }
  ],
  "count": 1
}
```

---

### 2. Create Profile
Create a new profile for a user.

**Endpoint:** `POST /api/profiles`

**Request Body:**
```json
{
  "userId": "user123",
  "profileName": "Corporate Training",
  "description": "Professional training videos",
  "fromTemplateId": "corporate-training"  // Optional
}
```

**Response:**
```json
{
  "success": true,
  "profile": {
    "profileId": "xyz789",
    "profileName": "Corporate Training",
    "description": "Professional training videos",
    "isDefault": false,
    "isActive": false,
    "createdAt": "2025-10-21T20:00:00Z",
    "lastUsed": "2025-10-21T20:00:00Z"
  }
}
```

---

### 3. Get Profile Details
Get detailed information about a specific profile, including preferences.

**Endpoint:** `GET /api/profiles/{profileId}`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Response:**
```json
{
  "success": true,
  "profile": {
    "profileId": "abc123",
    "profileName": "My Gaming Channel",
    "description": "Let's Play videos",
    "isDefault": true,
    "isActive": true,
    "createdAt": "2025-10-20T10:00:00Z",
    "lastUsed": "2025-10-21T20:00:00Z",
    "updatedAt": "2025-10-21T15:00:00Z"
  },
  "preferences": {
    "contentType": "gaming",
    "tone": {
      "formality": 20,
      "energy": 90,
      "personalityTraits": ["energetic", "humorous", "enthusiastic"],
      "customDescription": "Energetic and engaging"
    },
    "visual": {
      "aesthetic": "vibrant",
      "colorPalette": "vibrant",
      "shotTypePreference": "dynamic",
      "compositionStyle": "dynamic",
      "pacingPreference": "fast cuts",
      "bRollUsage": "moderate"
    },
    "audio": {
      "musicGenres": ["electronic", "upbeat", "energetic"],
      "musicEnergy": 85,
      "musicProminence": "prominent feature",
      "soundEffectsUsage": "heavy",
      "voiceStyle": "energetic",
      "audioMixing": "voice-focused"
    },
    "editing": {
      "pacing": 85,
      "cutFrequency": 85,
      "transitionStyle": "dynamic transitions",
      "effectUsage": "prominent",
      "sceneDuration": 3,
      "editingPhilosophy": "stylized editing"
    },
    "platform": {
      "primaryPlatform": "YouTube",
      "secondaryPlatforms": ["Twitch"],
      "aspectRatio": "16:9",
      "targetDurationSeconds": 900,
      "audienceDemographic": "Young adults, gamers, 18-35"
    },
    "aiBehavior": {
      "assistanceLevel": 70,
      "suggestionVerbosity": "moderate",
      "autoApplySuggestions": false,
      "suggestionFrequency": "proactive",
      "creativityLevel": 75,
      "overridePermissions": ["major_changes"]
    }
  }
}
```

---

### 4. Update Profile
Update profile metadata (name, description).

**Endpoint:** `PUT /api/profiles/{profileId}`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Request Body:**
```json
{
  "profileName": "Updated Name",
  "description": "Updated description"
}
```

**Response:**
```json
{
  "success": true,
  "profile": {
    "profileId": "abc123",
    "profileName": "Updated Name",
    "description": "Updated description",
    "updatedAt": "2025-10-21T20:00:00Z"
  }
}
```

---

### 5. Delete Profile
Delete a profile. Cannot delete the only profile or a default profile without promoting another.

**Endpoint:** `DELETE /api/profiles/{profileId}`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Response:**
```json
{
  "success": true,
  "message": "Profile abc123 deleted successfully"
}
```

**Error Response:**
```json
{
  "error": "Cannot delete the only profile"
}
```

---

### 6. Activate Profile
Set a profile as the active profile for the user. Deactivates all other profiles.

**Endpoint:** `POST /api/profiles/{profileId}/activate`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Response:**
```json
{
  "success": true,
  "profile": {
    "profileId": "abc123",
    "profileName": "My Gaming Channel",
    "isActive": true,
    "lastUsed": "2025-10-21T20:00:00Z"
  }
}
```

---

### 7. Duplicate Profile
Create a copy of an existing profile with all its preferences.

**Endpoint:** `POST /api/profiles/{profileId}/duplicate`

**Parameters:**
- `profileId` (path, required): Source profile identifier

**Request Body:**
```json
{
  "newProfileName": "Copy of My Gaming Channel"
}
```

**Response:**
```json
{
  "success": true,
  "profile": {
    "profileId": "newid456",
    "profileName": "Copy of My Gaming Channel",
    "description": "Let's Play videos",
    "createdAt": "2025-10-21T20:00:00Z"
  }
}
```

---

### 8. List Templates
Get all available profile templates.

**Endpoint:** `GET /api/profiles/templates`

**Response:**
```json
{
  "success": true,
  "templates": [
    {
      "templateId": "youtube-gaming",
      "name": "YouTube Gaming",
      "description": "High-energy gaming content",
      "category": "gaming"
    },
    {
      "templateId": "corporate-training",
      "name": "Corporate Training",
      "description": "Professional training videos",
      "category": "corporate"
    }
  ],
  "count": 8
}
```

---

### 9. Create from Template
Create a new profile from a template.

**Endpoint:** `POST /api/profiles/from-template`

**Request Body:**
```json
{
  "userId": "user123",
  "profileName": "My Training Videos",
  "description": "Company training content",
  "fromTemplateId": "corporate-training"
}
```

**Response:**
```json
{
  "success": true,
  "profile": {
    "profileId": "newid789",
    "profileName": "My Training Videos",
    "description": "Company training content",
    "isDefault": false,
    "isActive": false,
    "createdAt": "2025-10-21T20:00:00Z"
  },
  "templateUsed": "Corporate Training"
}
```

---

### 10. Update Preferences
Update specific preference categories. Only provided categories are updated; others remain unchanged.

**Endpoint:** `PUT /api/profiles/{profileId}/preferences`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Request Body:**
```json
{
  "contentType": "tutorial",
  "tone": {
    "formality": 60,
    "energy": 55,
    "personalityTraits": ["friendly", "patient", "encouraging"],
    "customDescription": "Clear and approachable"
  },
  "platform": {
    "primaryPlatform": "YouTube",
    "secondaryPlatforms": ["Udemy"],
    "aspectRatio": "16:9",
    "targetDurationSeconds": 720,
    "audienceDemographic": "Learners, students"
  }
}
```

**Response:**
```json
{
  "success": true,
  "preferences": {
    "profileId": "abc123",
    "contentType": "tutorial",
    "tone": { /* updated tone */ },
    "visual": { /* unchanged */ },
    "audio": { /* unchanged */ },
    "editing": { /* unchanged */ },
    "platform": { /* updated platform */ },
    "aiBehavior": { /* unchanged */ }
  }
}
```

---

### 11. Record Decision
Record a user's decision on an AI suggestion for decision tracking.

**Endpoint:** `POST /api/profiles/{profileId}/decisions/record`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Request Body:**
```json
{
  "suggestionType": "tone_adjustment",
  "decision": "accepted",
  "context": {
    "originalValue": "formal",
    "suggestedValue": "casual",
    "confidence": "high"
  }
}
```

**Valid Decision Values:**
- `accepted` - User accepted the suggestion
- `rejected` - User rejected the suggestion
- `modified` - User modified the suggestion before accepting

**Response:**
```json
{
  "success": true,
  "message": "Decision recorded successfully"
}
```

---

### 12. Get Preferences Summary
Get a summary of preferences and decision statistics for a profile.

**Endpoint:** `GET /api/profiles/{profileId}/preferences/summary`

**Parameters:**
- `profileId` (path, required): Profile identifier

**Response:**
```json
{
  "success": true,
  "summary": {
    "profileId": "abc123",
    "preferences": {
      "contentType": "gaming",
      "tone": {
        "formality": 20,
        "energy": 90,
        "personalityTraits": ["energetic", "humorous"]
      },
      "visual": {
        "aesthetic": "vibrant",
        "colorPalette": "vibrant"
      },
      "audio": {
        "musicGenres": ["electronic", "upbeat"],
        "musicEnergy": 85
      },
      "editing": {
        "pacing": 85,
        "cutFrequency": 85
      },
      "platform": {
        "primaryPlatform": "YouTube",
        "aspectRatio": "16:9"
      },
      "aiBehavior": {
        "assistanceLevel": 70,
        "creativityLevel": 75
      }
    },
    "decisionHistory": {
      "totalDecisions": 15,
      "byType": [
        {
          "suggestionType": "tone_adjustment",
          "total": 8,
          "accepted": 6,
          "rejected": 1,
          "modified": 1
        },
        {
          "suggestionType": "visual_style",
          "total": 7,
          "accepted": 5,
          "rejected": 2,
          "modified": 0
        }
      ]
    }
  }
}
```

---

## Error Responses

All endpoints may return error responses in the following format:

```json
{
  "error": "Error message description"
}
```

Common HTTP status codes:
- `200 OK` - Success
- `400 Bad Request` - Invalid request parameters
- `404 Not Found` - Profile or template not found
- `500 Internal Server Error` - Server error

---

## Data Models

### TonePreferences
```json
{
  "formality": 0-100,
  "energy": 0-100,
  "personalityTraits": ["trait1", "trait2"],
  "customDescription": "string"
}
```

### VisualPreferences
```json
{
  "aesthetic": "cinematic|corporate|vibrant|minimalist|documentary",
  "colorPalette": "warm|cool|monochrome|vibrant|pastel|brand",
  "shotTypePreference": "close-ups|wide shots|balanced",
  "compositionStyle": "rule of thirds|centered|dynamic",
  "pacingPreference": "fast cuts|longer shots|moderate",
  "bRollUsage": "minimal|moderate|heavy"
}
```

### AudioPreferences
```json
{
  "musicGenres": ["genre1", "genre2"],
  "musicEnergy": 0-100,
  "musicProminence": "subtle background|balanced|prominent feature",
  "soundEffectsUsage": "none|minimal|moderate|heavy",
  "voiceStyle": "authoritative|warm|energetic|calm",
  "audioMixing": "voice-focused|balanced with music"
}
```

### EditingPreferences
```json
{
  "pacing": 0-100,
  "cutFrequency": 0-100,
  "transitionStyle": "simple cuts|subtle fades|dynamic transitions",
  "effectUsage": "none|subtle|moderate|prominent",
  "sceneDuration": number,
  "editingPhilosophy": "invisible editing|stylized editing"
}
```

### PlatformPreferences
```json
{
  "primaryPlatform": "string",
  "secondaryPlatforms": ["platform1", "platform2"],
  "aspectRatio": "16:9|9:16|1:1|4:5",
  "targetDurationSeconds": number,
  "audienceDemographic": "string"
}
```

### AIBehaviorSettings
```json
{
  "assistanceLevel": 0-100,
  "suggestionVerbosity": "brief|moderate|detailed",
  "autoApplySuggestions": boolean,
  "suggestionFrequency": "only when asked|moderate|proactive",
  "creativityLevel": 0-100,
  "overridePermissions": ["permission1", "permission2"]
}
```

---

## Available Templates

1. **youtube-gaming** - YouTube Gaming
2. **corporate-training** - Corporate Training
3. **educational-tutorial** - Educational Tutorial
4. **product-review** - Product Review
5. **vlog-personal** - Vlog/Personal
6. **marketing-promotional** - Marketing/Promotional
7. **documentary** - Documentary
8. **quick-tips-shorts** - Quick Tips/Shorts

See template details with `GET /api/profiles/templates`.
