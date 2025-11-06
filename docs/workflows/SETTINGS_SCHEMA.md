# Settings Export/Import JSON Schema

This document describes the JSON schema used for exporting and importing Aura Video Studio settings.

## Schema Version 1.0.0

### Root Object

```json
{
  "version": "1.0.0",
  "exported": "2025-10-10T20:30:00.000Z",
  "settings": { },
  "apiKeys": { },
  "providerPaths": { },
  "profiles": [ ]
}
```

### Fields

#### `version` (string, required)
Schema version identifier. Current version is "1.0.0".

#### `exported` (string, required)
ISO 8601 timestamp of when the settings were exported.

#### `settings` (object, required)
General application settings.

```json
{
  "offlineMode": false,
  "uiScale": 100,
  "compactMode": false
}
```

- **offlineMode** (boolean): When true, disables all cloud providers
- **uiScale** (number): UI scale percentage (75-150)
- **compactMode** (boolean): Whether to use compact layout

#### `apiKeys` (object, optional)
API keys for external services. 

**Storage**: 
- **At Rest**: Keys are encrypted using platform-specific encryption (DPAPI on Windows, AES-256 on Linux/macOS)
- **Location**: `%LOCALAPPDATA%\Aura\secure\apikeys.dat` (Windows) or `$HOME/.local/share/Aura/secure/apikeys.dat` (Linux/macOS)
- **Export**: By default, keys are **NOT** included in exports (secretless by default)
- **Export with secrets**: Requires explicit opt-in with per-key selection, security warnings, and acknowledgment

**Export API** (`POST /api/settings/export`):
- **Default behavior**: Exports WITHOUT secrets (secure by default)
- **Include secrets**: Requires `includeSecrets: true`, `selectedSecretKeys` array, and `acknowledgeWarning: true`
- **Preview endpoint**: `GET /api/settings/export/preview` shows what would be redacted
- **Logging**: Consent event logged (no secrets in logs)

**Import API** (`POST /api/settings/import`):
- **Dry-run mode**: Default `dryRun: true` analyzes conflicts without applying changes
- **Conflict detection**: Shows what will be overwritten before applying
- **Explicit approval**: Requires `dryRun: false` and `overwriteExisting: true` to apply changes
- **Conflict summary**: Returns detailed comparison of current vs. new values

**Managed via**:
- **API**: `/api/settings/export`, `/api/settings/import`, `/api/settings/export/preview` endpoints
- **CLI**: `aura keys` commands for key management
- **UI**: Settings → Import/Export tab with modal dialogs for safe export/import

```json
{
  "openai": "sk-...",
  "anthropic": "sk-ant-...",
  "gemini": "AIza...",
  "elevenlabs": "...",
  "stabilityai": "sk-...",
  "playht": "..."
}
```

**Supported providers**:
- **openai** (string): OpenAI API key (starts with `sk-`)
- **anthropic** (string): Anthropic API key (starts with `sk-ant-`)
- **gemini** or **google** (string): Google Gemini API key (starts with `AIza`)
- **elevenlabs** (string): ElevenLabs API key
- **stabilityai** (string): Stability AI API key (starts with `sk-`)
- **playht** (string): PlayHT API key
- **azure** (string): Azure OpenAI API key

**Note**: API keys are never stored in `settings.json`. They are stored separately in encrypted storage.

#### `providerPaths` (object, required)
Local provider configuration paths and URLs.

```json
{
  "stableDiffusionUrl": "http://127.0.0.1:7860",
  "ollamaUrl": "http://127.0.0.1:11434",
  "ffmpegPath": "",
  "ffprobePath": "",
  "outputDirectory": ""
}
```

- **stableDiffusionUrl** (string): URL for Stable Diffusion WebUI
- **ollamaUrl** (string): URL for Ollama server
- **ffmpegPath** (string): Path to ffmpeg executable
- **ffprobePath** (string): Path to ffprobe executable
- **outputDirectory** (string): Default output directory for rendered videos

#### `profiles` (array, required)
List of available provider profiles.

```json
[
  {
    "name": "Free-Only",
    "description": "No API keys required"
  },
  {
    "name": "Balanced Mix",
    "description": "Mix of free and paid"
  },
  {
    "name": "Pro-Max",
    "description": "Best quality with all providers"
  }
]
```

## Example Complete Export

```json
{
  "version": "1.0.0",
  "exported": "2025-10-10T20:30:00.000Z",
  "settings": {
    "offlineMode": false,
    "uiScale": 100,
    "compactMode": false
  },
  "apiKeys": {
    "openai": "sk-proj-...",
    "elevenlabs": "a1b2c3d4...",
    "pexels": "563492ad6f91...",
    "stabilityai": "sk-..."
  },
  "providerPaths": {
    "stableDiffusionUrl": "http://127.0.0.1:7860",
    "ollamaUrl": "http://127.0.0.1:11434",
    "ffmpegPath": "C:\\ffmpeg\\bin\\ffmpeg.exe",
    "ffprobePath": "C:\\ffmpeg\\bin\\ffprobe.exe",
    "outputDirectory": "C:\\Users\\YourName\\Videos\\AuraOutput"
  },
  "profiles": [
    {
      "name": "Free-Only",
      "description": "No API keys required"
    },
    {
      "name": "Balanced Mix",
      "description": "Mix of free and paid"
    },
    {
      "name": "Pro-Max",
      "description": "Best quality with all providers"
    }
  ]
}
```

## Profile Templates

### Free-Only
Uses only free/local providers:
- Script: Template-based generation
- TTS: Windows TTS
- Visuals: Free stock sources (Pexels, Pixabay, Unsplash)
- No API keys required

### Balanced Mix
Combines free and paid services:
- Script: GPT-4 (requires OpenAI key)
- TTS: ElevenLabs (requires key)
- Visuals: Free stock sources
- Moderate cost, good quality

### Pro-Max
Premium providers for maximum quality:
- Script: GPT-4 (requires OpenAI key)
- TTS: ElevenLabs (requires key)
- Visuals: Stability AI (requires key)
- Highest cost, best quality

## Security Notes

### Encryption at Rest

✅ **All API keys are encrypted at rest** using platform-specific encryption:
- **Windows**: DPAPI (Data Protection API) with CurrentUser scope
- **Linux/macOS**: AES-256 with machine-specific key

### Export Security (Hardened in v1.1)

✅ **Default behavior: Secretless by default** - API keys are automatically excluded from exports for your security.

⚠️ **If you choose to export with secrets (opt-in required)**:
- ✅ Requires explicit `includeSecrets: true` flag in API request
- ✅ Requires per-key selection via `selectedSecretKeys` array
- ✅ Requires security warning acknowledgment via `acknowledgeWarning: true`
- ✅ Shows masked preview of keys before export (`GET /api/settings/export/preview`)
- ✅ Logs consent event (no secrets in logs)
- ⚠️ Exported files contain selected API keys in **plain text**

**UI Workflow**:
1. User clicks "Export Settings" → Opens modal
2. Modal shows "Default: Secrets Excluded" message
3. User must toggle "Include API Keys and Secrets" switch
4. Security warning box appears (yellow/orange, with warning icon)
5. User selects specific keys via checkboxes (shows masked previews)
6. User must check "I understand the security risks" acknowledgment
7. Export button enabled only after all confirmations
8. Downloaded file name includes `-with-secrets` suffix if secrets included

**Best practices for exported files with secrets**:
- ✅ Use `.gitignore` to exclude `*-with-secrets.json` files
- ✅ Encrypt exported files before storing in cloud storage
- ✅ Use a password manager or secure vault for storage
- ✅ Delete exported files after use
- ✅ Regularly rotate API keys
- ❌ Never commit secrets to version control
- ❌ Never share via email, chat, or public channels

## Import Validation (Hardened in v1.1)

### Dry-Run Mode (Default)

✅ **Import is safe by default** - Uses dry-run mode to preview changes before applying.

**Import workflow**:
1. User selects settings JSON file
2. **Dry-run analysis** (`dryRun: true`) runs automatically
3. Shows conflict summary:
   - **General Settings**: What will change (theme, language, etc.)
   - **API Keys**: Which keys will be updated (masked values)
   - **Provider Paths**: Path changes (may be machine-specific)
4. User reviews conflicts and decides
5. User clicks "Apply Import" to execute (`dryRun: false`, `overwriteExisting: true`)
6. Success message displayed, page refresh recommended

**Validation checks**:
1. ✓ JSON format is valid
2. ✓ Schema version is compatible (`version: "1.0.0"`)
3. ✓ Required fields are present (`settings` object)
4. ✓ Data types match schema
5. ✓ Conflicts detected and summarized
6. ✓ Recommended resolution provided for each conflict

**Conflict Resolution**:
- **General Settings**: Recommended action is "Use new value"
- **API Keys**: Recommended action is "Review before overwriting"
- **Provider Paths**: Recommended action is "Keep current (path may be machine-specific)"

If validation fails, import is rejected with an error message. Dry-run analysis prevents accidental overwrites.

## Programmatic Usage (v1.1 API)

### Export Preview (Check what would be redacted)
```typescript
const response = await fetch('/api/settings/export/preview');
const preview = await response.json();
// {
//   totalSecrets: 4,
//   availableKeys: ['openai', 'anthropic', 'elevenlabs', 'stabilityai'],
//   redactionPreview: {
//     'openai': 'sk-p...xyz',
//     'anthropic': 'sk-a...abc',
//     'elevenlabs': 'ab12...cd34',
//     'stabilityai': 'sk-s...789'
//   }
// }
```

### Export WITHOUT secrets (Default, Secure)
```typescript
const response = await fetch('/api/settings/export', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    includeSecrets: false,
    selectedSecretKeys: [],
    acknowledgeWarning: false
  })
});

const data = await response.json();
// data.settings.apiKeys will be empty or redacted
// data.metadata.secretsIncluded === false

const blob = new Blob([JSON.stringify(data, null, 2)], { 
  type: 'application/json' 
});
// Download as aura-settings-2025-11-06.json
```

### Export WITH secrets (Opt-in, Requires Acknowledgment)
```typescript
const response = await fetch('/api/settings/export', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    includeSecrets: true,
    selectedSecretKeys: ['openai', 'anthropic'], // Per-key selection
    acknowledgeWarning: true // Must acknowledge security risks
  })
});

const data = await response.json();
// data.settings.apiKeys will contain selected keys in plain text
// data.metadata.secretsIncluded === true
// data.metadata.includedSecretKeys === ['openai', 'anthropic']

const blob = new Blob([JSON.stringify(data, null, 2)], { 
  type: 'application/json' 
});
// Download as aura-settings-with-secrets-2025-11-06.json
```

### Import with Dry-Run (Default, Safe)
```typescript
const file = /* File object from input */;
const text = await file.text();
const data = JSON.parse(text);

// Step 1: Dry-run to check conflicts
const dryRunResponse = await fetch('/api/settings/import', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    version: data.version,
    settings: data.settings,
    dryRun: true, // Default: analyze without applying
    overwriteExisting: false
  })
});

const dryRunResult = await dryRunResponse.json();
// {
//   success: true,
//   message: "Found 3 conflicts. Please review and confirm.",
//   conflicts: {
//     totalConflicts: 3,
//     generalSettings: [
//       { key: 'theme', currentValue: 'dark', newValue: 'light', recommendedResolution: 'UseNew' }
//     ],
//     apiKeys: [
//       { key: 'openai', currentValue: 'sk-p...xyz', newValue: 'sk-n...abc', recommendedResolution: 'KeepCurrent' }
//     ],
//     providerPaths: [
//       { key: 'FFmpegPath', currentValue: '/usr/bin/ffmpeg', newValue: 'C:\\ffmpeg\\bin\\ffmpeg.exe', recommendedResolution: 'KeepCurrent' }
//     ]
//   }
// }

// Step 2: User reviews conflicts, then applies
const applyResponse = await fetch('/api/settings/import', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    version: data.version,
    settings: data.settings,
    dryRun: false, // Apply changes
    overwriteExisting: true // Resolve conflicts by overwriting
  })
});

const result = await applyResponse.json();
// { success: true, message: "Settings imported successfully" }
```
