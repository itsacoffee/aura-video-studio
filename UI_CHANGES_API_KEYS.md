# UI Changes: Settings Page API Keys

## Before

The Settings page API Keys tab only had fields for:
- OpenAI API Key
- ElevenLabs API Key
- Pexels API Key
- Stability AI API Key

## After

The Settings page API Keys tab now includes fields for ALL stock providers:
- OpenAI API Key (for GPT-based script generation)
- ElevenLabs API Key (for high-quality voice synthesis)
- Pexels API Key (for stock video and images from Pexels)
- **Pixabay API Key** ← NEW (for stock video and images from Pixabay)
- **Unsplash API Key** ← NEW (for stock images from Unsplash)
- Stability AI API Key (for AI image generation)

## Code Changes

### SettingsPage.tsx

#### State Variable
```typescript
// Before
const [apiKeys, setApiKeys] = useState({
  openai: '',
  elevenlabs: '',
  pexels: '',
  stabilityai: '',
});

// After
const [apiKeys, setApiKeys] = useState({
  openai: '',
  elevenlabs: '',
  pexels: '',
  pixabay: '',      // NEW
  unsplash: '',     // NEW
  stabilityai: '',
});
```

#### Save API Keys
```typescript
// Before
body: JSON.stringify({
  openAiKey: apiKeys.openai,
  elevenLabsKey: apiKeys.elevenlabs,
  pexelsKey: apiKeys.pexels,
  stabilityAiKey: apiKeys.stabilityai,
})

// After
body: JSON.stringify({
  openAiKey: apiKeys.openai,
  elevenLabsKey: apiKeys.elevenlabs,
  pexelsKey: apiKeys.pexels,
  pixabayKey: apiKeys.pixabay,        // NEW
  unsplashKey: apiKeys.unsplash,      // NEW
  stabilityAiKey: apiKeys.stabilityai,
})
```

#### UI Fields
```tsx
<Field label="Pixabay API Key" hint="Required for stock video and images from Pixabay">
  <Input 
    type="password" 
    placeholder="..." 
    value={apiKeys.pixabay}
    onChange={(e) => updateApiKey('pixabay', e.target.value)}
  />
</Field>

<Field label="Unsplash API Key" hint="Required for stock images from Unsplash">
  <Input 
    type="password" 
    placeholder="..." 
    value={apiKeys.unsplash}
    onChange={(e) => updateApiKey('unsplash', e.target.value)}
  />
</Field>
```

## User Experience

Users can now:
1. Navigate to Settings → API Keys tab
2. Enter API keys for Pixabay and Unsplash
3. Save the keys (they are stored in `%LOCALAPPDATA%\Aura\apikeys.json`)
4. Use Pixabay and Unsplash as stock sources in the video creation wizard

The keys are displayed as masked (password fields) for security and only the first 8 characters are shown when loading saved keys.
