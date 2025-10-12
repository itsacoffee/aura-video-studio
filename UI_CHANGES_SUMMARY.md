# UI Changes Summary

## Before This PR

```
Engine Card
┌────────────────────────────────────────┐
│ 🎬 FFmpeg                    [Install]│
│ Version: 6.0 • Size: 80MB              │
│                                        │
│ Essential video processing toolkit    │
└────────────────────────────────────────┘
```

**Problems:**
- No visibility into download URL
- 404 errors when GitHub release filename changes
- No alternative download options
- Users can't verify what they're downloading

## After This PR

```
Engine Card
┌─────────────────────────────────────────────────────────┐
│ 🎬 FFmpeg                               [Install ▼]     │
│ Version: 6.0 • Size: 80MB                               │
│                                                          │
│ Essential video processing toolkit                      │
│                                                          │
│ ▶ Download Information                                  │
│   ┌──────────────────────────────────────────────────┐ │
│   │ Resolved Download URL:                           │ │
│   │ https://github.com/BtbN/FFmpeg-Builds/...        │ │
│   │ [Copy] [Open in Browser]                         │ │
│   │                                                   │ │
│   │ This URL was resolved from the latest GitHub    │ │
│   │ release for FFmpeg. You can download manually   │ │
│   │ or use the Install button below.                │ │
│   └──────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘

Install Menu Options:
┌─────────────────────────────┐
│ ⬇ Official Mirrors          │
│ 🔗 Custom URL...            │
│ 📁 Install from Local File..│
└─────────────────────────────┘
```

**Benefits:**
✅ Full transparency - users see actual download URL
✅ Copy URL for manual download
✅ Open URL in browser to verify
✅ Custom URL option for alternative sources
✅ Local file import for offline installs
✅ Automatic GitHub release resolution

## Install Flow Visualization

### Standard Install Flow
```
User clicks "Install"
    ↓
[UI] Call /api/engines/resolve-url?engineId=ffmpeg
    ↓
[Backend] Query GitHub Releases API
    ↓
[Backend] Match asset by pattern: "ffmpeg-*-win64-gpl-*.zip"
    ↓
[Backend] Return: { url: "...ffmpeg-n7.1-latest-win64-gpl.zip", source: "github-api" }
    ↓
[UI] Display resolved URL in collapsible section
    ↓
User can:
  - Copy URL
  - Open in browser
  - Click "Install" to proceed
    ↓
[Backend] Download with fallback:
  1. Try resolved GitHub URL
  2. Try mirror 1 (gyan.dev)
  3. Try mirror 2 (if configured)
  4. Fail with detailed error
```

### Custom URL Flow
```
User clicks "Install" → "Custom URL..."
    ↓
[UI] Show dialog with URL input
    ↓
User enters: "https://example.com/my-ffmpeg.zip"
    ↓
[UI] POST /api/engines/install
    {
      engineId: "ffmpeg",
      customUrl: "https://example.com/my-ffmpeg.zip"
    }
    ↓
[Backend] Download from custom URL
    ↓
[Backend] Verify checksums (if available)
    ↓
[Backend] Install and update status
```

### Local File Flow
```
User clicks "Install" → "Install from Local File..."
    ↓
[UI] Show dialog with file path input
    ↓
User enters: "C:\Downloads\ffmpeg.zip"
    ↓
[UI] POST /api/engines/install
    {
      engineId: "ffmpeg",
      localFilePath: "C:\Downloads\ffmpeg.zip"
    }
    ↓
[Backend] Import local file
    ↓
[Backend] Compute and verify checksum
    ↓
[Backend] Extract and install
```

## Error Handling Improvements

### Before
```
❌ Installation failed
```

### After
```
❌ Installation failed

Attempted URLs:
1. https://github.com/.../ffmpeg-n7.1-latest-win64-gpl.zip (404)
2. https://www.gyan.dev/ffmpeg/builds/ffmpeg-release.zip (timeout)

Error: All download sources failed

Options:
[Try Mirror] [Use Custom URL] [Install from Local File]
```

## Component Manifest Structure

The new `components.json` makes it easy to add new components:

```json
{
  "components": [
    {
      "id": "ffmpeg",
      "name": "FFmpeg",
      "githubRepo": "BtbN/FFmpeg-Builds",
      "assetPattern": "ffmpeg-*-win64-gpl-*.zip",
      "mirrors": [
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
      ],
      "description": "Essential video and audio processing toolkit",
      "extractPath": "bin/"
    },
    {
      "id": "ollama",
      "name": "Ollama",
      "githubRepo": "ollama/ollama",
      "assetPattern": {
        "windows": "ollama-windows-amd64.zip",
        "linux": "ollama-linux-amd64.tar.gz"
      },
      "mirrors": [],
      "description": "Local LLM engine for script generation"
    }
  ]
}
```

## Testing Coverage

✅ **Unit Tests (11 passing)**
- GitHubReleaseResolverTests (6 tests)
  - Successful API resolution
  - Pattern matching with wildcards
  - 404 handling
  - Release info retrieval
  
- ComponentDownloaderTests (5 tests)
  - GitHub API resolution with download
  - Mirror fallback on API failure
  - Custom URL installation
  - Local file import
  - URL resolution for UI display

## Migration Path

1. **No breaking changes** - Existing installations continue to work
2. **Graceful degradation** - Falls back to hardcoded URLs if GitHub API fails
3. **Opt-in enhancements** - Users benefit from new features automatically
4. **Zero downtime** - Can be deployed without service interruption

## Performance Characteristics

- **GitHub API calls**: Cached for 5 minutes to reduce API rate limiting
- **Download speed**: Unchanged (uses same HttpDownloader with resume support)
- **UI responsiveness**: Async URL resolution doesn't block UI
- **Error recovery**: Exponential backoff between mirror attempts

## Security Considerations

✅ No authentication tokens stored (public repos only)
✅ HTTPS-only for all download URLs
✅ SHA-256 checksum verification
✅ User confirmation for custom URLs
✅ Local file path validation
