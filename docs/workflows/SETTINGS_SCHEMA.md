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
- **Export**: By default, keys are **NOT** included in exports (redacted)
- **Export with secrets**: Requires explicit opt-in with per-key selection and warnings

**Managed via**:
- **API**: `/api/keys/*` endpoints (set, list, test, rotate, delete)
- **CLI**: `aura keys` commands
- **UI**: Key management interface with test and masked display

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

### Export Security

⚠️ **By default, API keys are NOT exported** - they are redacted from settings exports.

⚠️ **If you choose to export with secrets**:
- Requires explicit opt-in with per-key selection
- Shows redaction preview before export
- Displays prominent security warnings
- Exported files contain API keys in **plain text**

**Best practices for exported files with secrets**:
- ✅ Use `.gitignore` to exclude `*-settings.json` files
- ✅ Encrypt exported files before storing in cloud storage
- ✅ Use a password manager or secure vault for storage
- ✅ Delete exported files after use
- ✅ Regularly rotate API keys
- ❌ Never commit secrets to version control
- ❌ Never share via email, chat, or public channels

## Import Validation

When importing settings, the application validates:
1. ✓ JSON format is valid
2. ✓ Schema version is compatible
3. ✓ Required fields are present
4. ✓ Data types match schema

If validation fails, import is rejected with an error message.

## Programmatic Usage

### Export
```typescript
const settingsData = {
  version: '1.0.0',
  exported: new Date().toISOString(),
  settings: { /* ... */ },
  apiKeys: { /* ... */ },
  providerPaths: { /* ... */ },
  profiles: [ /* ... */ ]
};

const blob = new Blob([JSON.stringify(settingsData, null, 2)], { 
  type: 'application/json' 
});
// Download blob as file
```

### Import
```typescript
const file = /* File object from input */;
const text = await file.text();
const data = JSON.parse(text);

// Validate schema
if (!data.version || !data.settings) {
  throw new Error('Invalid settings format');
}

// Apply settings
applySettings(data);
```
