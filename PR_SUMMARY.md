# PR Summary: GitHub Releases API and Mirror Fallback

## Problem Solved

Fixed broken hardcoded GitHub "latest" links that caused 404 errors when release filenames changed. Implemented a robust download flow using the GitHub Releases API with configurable mirrors and support for custom URLs and local file imports.

## Implementation Details

### Backend Changes

1. **GitHubReleaseResolver** (`Aura.Core/Dependencies/GitHubReleaseResolver.cs`)
   - Resolves asset URLs from GitHub Releases API
   - Supports wildcard pattern matching (e.g., `ffmpeg-*-win64-gpl-*.zip`)
   - No authentication required for public repos
   - Graceful handling of 404s and API failures

2. **ComponentDownloader** (`Aura.Core/Dependencies/ComponentDownloader.cs`)
   - Orchestrates download flow: GitHub API → Mirrors → Custom URL → Local File
   - Platform-specific asset pattern support
   - Detailed error reporting with attempted URLs
   - Installation provenance tracking

3. **Components Manifest** (`Aura.Core/Dependencies/components.json`)
   - Centralized configuration for all downloadable components
   - Contains GitHub repo info, asset patterns, and mirror URLs
   - Easily extensible for new components

4. **API Endpoint** (`/api/engines/resolve-url`)
   - Exposes resolved URLs to frontend
   - Returns source information (github-api, mirror, static)

5. **Engine Manifest Updates**
   - Added `githubRepo` and `assetPattern` fields
   - Updated FFmpeg, Ollama, and Piper entries with GitHub metadata

### Frontend Changes

1. **EngineCard Component** (`Aura.Web/src/components/Engines/EngineCard.tsx`)
   - Displays resolved GitHub release URL in collapsible section
   - "Copy" button for URL
   - "Open in Browser" button
   - Custom URL input dialog
   - Local file picker dialog

2. **Type Updates** (`Aura.Web/src/types/engines.ts`)
   - Added `githubRepo` and `assetPattern` fields to EngineManifestEntry

### Testing

Created comprehensive test suites:

1. **GitHubReleaseResolverTests** (6 tests)
   - GitHub API success scenarios
   - Wildcard pattern matching
   - 404 handling
   - Release info retrieval

2. **ComponentDownloaderTests** (5 tests)
   - GitHub API resolution
   - Mirror fallback on failure
   - Custom URL installation
   - Local file import
   - URL resolution for UI display

**All 11 tests passing ✅**

## Key Benefits

1. **No more 404 errors** - Always uses latest release without code changes
2. **Reliability** - Mirror fallback ensures downloads succeed
3. **Transparency** - Users see actual URLs being used
4. **Flexibility** - Multiple installation methods supported
5. **Testability** - Full test coverage with mocked HTTP responses

## Acceptance Criteria Met

✅ Download Center resolves actual asset URL and shows it in UI
✅ If GitHub "latest" asset filename changes, resolver finds correct asset by pattern matching
✅ Mirror fallback works when primary URL fails
✅ Custom URL and local file options available in UI
✅ Copyable URLs displayed with "Open in browser" button
✅ All tests passing

## Files Changed

### Created
- `Aura.Core/Dependencies/components.json` - Component manifest
- `Aura.Core/Dependencies/GitHubReleaseResolver.cs` - GitHub API resolver
- `Aura.Core/Dependencies/ComponentDownloader.cs` - Download orchestrator
- `Aura.Tests/GitHubReleaseResolverTests.cs` - Resolver tests
- `Aura.Tests/ComponentDownloaderTests.cs` - Downloader tests
- `GITHUB_RELEASES_API_IMPLEMENTATION.md` - Implementation docs

### Modified
- `Aura.Core/Downloads/EngineManifest.cs` - Added GitHub fields
- `Aura.Core/Downloads/EngineManifestLoader.cs` - Updated with GitHub metadata
- `Aura.Api/Controllers/EnginesController.cs` - Added resolve-url endpoint
- `Aura.Api/Program.cs` - Registered GitHubReleaseResolver in DI
- `Aura.Web/src/components/Engines/EngineCard.tsx` - Added URL display
- `Aura.Web/src/types/engines.ts` - Added GitHub fields

## Migration Notes

- Backward compatible - existing installations not affected
- No breaking changes to existing APIs
- Falls back gracefully if GitHub API unavailable
- Existing hardcoded URLs still work as fallback

## Next Steps (Optional Future Enhancements)

1. Cache GitHub API responses with TTL
2. Parallel downloads from multiple mirrors
3. Resume partial downloads across mirrors
4. Version pinning and rollback support
5. Telemetry for download success rates per mirror

## Testing Instructions

To test the implementation:

1. Start the API: `cd Aura.Api && dotnet run`
2. Start the Web UI: `cd Aura.Web && npm run dev`
3. Navigate to Download Center / Engines tab
4. Select an engine (e.g., FFmpeg)
5. Observe the "Download Information" section showing resolved URL
6. Test "Copy" button
7. Test "Open in Browser" button
8. Try installing with custom URL
9. Try installing from local file

## Deployment Checklist

- [x] All tests passing
- [x] Code builds successfully
- [x] Documentation complete
- [x] No breaking changes
- [x] Backward compatible
- [x] Error handling in place
- [x] Logging implemented
- [x] DI registration complete
