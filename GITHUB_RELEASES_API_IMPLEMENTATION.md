# GitHub Releases API and Mirror Fallback Implementation

This PR implements robust download functionality using the GitHub Releases API with configurable mirrors and local file import support.

## Overview

The implementation provides:
1. **GitHub Releases API Resolution**: Automatically resolves the latest release asset URLs from GitHub
2. **Mirror Fallback**: Falls back to configured mirrors if GitHub API fails
3. **Custom URL Support**: Allows users to provide custom download URLs
4. **Local File Import**: Supports importing from local files
5. **UI Transparency**: Shows resolved URLs in the UI with copy and open buttons

## Architecture

### Backend Components

#### 1. GitHubReleaseResolver (`Aura.Core/Dependencies/GitHubReleaseResolver.cs`)
- Resolves asset URLs from GitHub Releases API
- Supports wildcard pattern matching (e.g., `ffmpeg-*-win64-gpl-*.zip`)
- Handles API failures gracefully
- No authentication required for public repos

#### 2. ComponentDownloader (`Aura.Core/Dependencies/ComponentDownloader.cs`)
- Orchestrates download flow: GitHub API → Mirrors → Custom URL → Local File
- Supports platform-specific asset patterns
- Records provenance of installations
- Provides detailed error reporting with attempted URLs

#### 3. Components Manifest (`Aura.Core/Dependencies/components.json`)
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
      ]
    }
  ]
}
```

#### 4. API Endpoint (`/api/engines/resolve-url`)
- Exposes resolved URLs to the frontend
- Returns source information (github-api, mirror, static)
- Caches GitHub API responses where appropriate

### Frontend Components

#### EngineCard Component
- Displays resolved GitHub release URL
- Shows collapsible "Download Information" section
- Provides "Copy" and "Open in Browser" buttons
- Supports Custom URL and Local File installation

## Download Flow

```
1. User clicks "Install" on engine card
   ↓
2. Frontend calls /api/engines/resolve-url
   ↓
3. Backend attempts GitHub API resolution
   ├─ Success → Return resolved URL
   └─ Failure → Return first mirror URL
   ↓
4. Frontend displays resolved URL with options
   ↓
5. User chooses installation method:
   ├─ Official Mirrors (uses resolved URL + mirrors)
   ├─ Custom URL (user provides URL)
   └─ Local File (user provides file path)
   ↓
6. Download proceeds with fallback chain:
   GitHub URL → Mirror 1 → Mirror 2 → ... → Error
```

## Benefits

1. **Resilience**: No more 404 errors from hardcoded release URLs
2. **Transparency**: Users can see and copy actual download URLs
3. **Flexibility**: Multiple ways to install (official, custom, local)
4. **Automatic Updates**: Always uses latest GitHub release without code changes
5. **Testability**: Full unit and integration test coverage

## Testing

### Unit Tests
- `GitHubReleaseResolverTests`: Tests GitHub API parsing and pattern matching
- `ComponentDownloaderTests`: Tests download orchestration and fallback logic

### Test Coverage
- ✅ GitHub API success scenarios
- ✅ Pattern matching with wildcards
- ✅ Mirror fallback on 404
- ✅ Custom URL installation
- ✅ Local file import
- ✅ Error handling and reporting

## API Usage Examples

### Resolve Download URL
```bash
GET /api/engines/resolve-url?engineId=ffmpeg
Response:
{
  "url": "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n7.1-latest-win64-gpl.zip",
  "source": "github-api",
  "githubRepo": "BtbN/FFmpeg-Builds"
}
```

### Install with Custom URL
```bash
POST /api/engines/install
{
  "engineId": "ffmpeg",
  "customUrl": "https://example.com/ffmpeg.zip"
}
```

### Install from Local File
```bash
POST /api/engines/install
{
  "engineId": "ffmpeg",
  "localFilePath": "C:\\Downloads\\ffmpeg.zip"
}
```

## Configuration

### Adding a New Component

Edit `Aura.Core/Dependencies/components.json`:

```json
{
  "id": "new-tool",
  "name": "New Tool",
  "githubRepo": "owner/repo",
  "assetPattern": "tool-*-platform-*.zip",
  "mirrors": [
    "https://mirror1.com/tool.zip",
    "https://mirror2.com/tool.zip"
  ]
}
```

### Platform-Specific Patterns

Use JSON object for platform-specific patterns:

```json
{
  "assetPattern": {
    "windows": "tool-windows-*.zip",
    "linux": "tool-linux-*.tar.gz"
  }
}
```

## Migration Notes

Existing installations are not affected. The new system:
- Maintains backward compatibility with hardcoded URLs
- Augments with GitHub API resolution
- Falls back gracefully if GitHub API is unavailable

## Future Enhancements

Potential improvements:
1. Cache GitHub API responses with TTL
2. Parallel downloads from multiple mirrors
3. Torrent support for large files
4. Resume partial downloads across mirrors
5. Version pinning and rollback support
