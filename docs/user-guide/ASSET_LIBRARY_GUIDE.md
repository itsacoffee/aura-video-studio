# Asset Library System - User Guide

## Overview

The Asset Library is a comprehensive system for managing all media assets in Aura Video Studio. It provides a centralized location to organize, search, preview, and manage images, videos, audio files, and AI-generated content.

## Features

### Core Functionality

- **Centralized Asset Management**: Browse and manage all media assets in one place
- **Intelligent Search**: Full-text search with filters by type, tags, date, resolution, and more
- **Automatic Tagging**: AI-powered tagging generates relevant tags for all assets automatically
- **Collections**: Organize assets into named collections with visual indicators
- **Stock Image Integration**: Search and download images from Pexels and Pixabay
- **Asset Preview**: View asset details, metadata, and tags in dedicated preview panel
- **Usage Tracking**: See which timelines use each asset and prevent accidental deletion

### Supported Asset Types

- **Images**: JPG, PNG, GIF, BMP, WebP
- **Videos**: MP4, AVI, MOV, MKV, WebM
- **Audio**: MP3, WAV, OGG, M4A, FLAC

## Getting Started

### Accessing the Asset Library

1. Launch Aura Video Studio
2. Click **"Asset Library"** in the main navigation menu
3. The library opens with a three-panel layout:
   - **Left Sidebar**: Filters and collections
   - **Center Panel**: Asset grid
   - **Right Panel**: Preview and details

### Importing Assets

#### Upload Local Files

1. Click **"Import Assets"** button in the header
2. Select one or multiple files from your computer
3. Assets are automatically imported with:
   - Thumbnail generation
   - Metadata extraction (resolution, duration, file size)
   - Automatic tag generation

#### Using Stock Images

1. Click **"Stock Images"** button in the header
2. Enter a search term (e.g., "sunset beach")
3. Browse results from Pexels and Pixabay
4. Click any image to download and add to library
5. Stock images are automatically tagged and attributed

**Note**: To use stock images, configure API keys in `appsettings.json`:
```json
{
  "StockImages": {
    "PexelsApiKey": "your-pexels-api-key",
    "PixabayApiKey": "your-pixabay-api-key"
  }
}
```

Free API keys available at:
- Pexels: https://www.pexels.com/api/
- Pixabay: https://pixabay.com/api/docs/

## Using the Asset Library

### Searching and Filtering

#### Quick Search
Type in the search bar to instantly filter assets by:
- Asset title
- Description
- Tags

#### Filter by Type
Click type buttons in the left sidebar:
- **All Assets**: Show everything
- **Images**: Show only image files
- **Videos**: Show only video files
- **Audio**: Show only audio files

#### Filter by Collection
Click any collection in the sidebar to show only assets in that collection.

### Managing Tags

#### View Tags
- Tags appear below asset preview
- Each tag shows its confidence score (0-100)
- Tags are automatically generated on import

#### Add Custom Tags
1. Select an asset
2. View tags in the preview panel
3. Tags are displayed as colored chips
4. *Note: Manual tag editing UI coming soon*

### Working with Collections

#### Create a Collection
1. Click the **+** button next to "Collections" in sidebar
2. Enter collection name and optional description
3. Click **"Create"**

#### Add Assets to Collection
*Note: Drag-and-drop coming soon. Currently use API or backend.*

#### View Collection Contents
Click any collection name in the sidebar to filter assets.

### Asset Preview

When you click an asset, the right panel shows:
- **Large preview** (placeholder icon for now)
- **Title** and description
- **Metadata**:
  - Asset type (Image/Video/Audio)
  - Source (Uploaded, Stock, AI Generated)
  - Resolution (width x height)
  - File size
  - Duration (for video/audio)
- **Tags** with confidence scores
- **Add to Timeline** button (integration coming soon)

## API Integration

### REST Endpoints

The asset library exposes a complete REST API:

#### Assets
- `GET /api/assets` - Search and list assets
- `GET /api/assets/{id}` - Get specific asset
- `POST /api/assets/upload` - Upload new asset
- `POST /api/assets/{id}/tags` - Add tags to asset
- `DELETE /api/assets/{id}` - Delete asset

#### Stock Images
- `GET /api/assets/stock/search?query={query}` - Search stock images
- `POST /api/assets/stock/download` - Download stock image

#### Collections
- `GET /api/assets/collections` - List all collections
- `POST /api/assets/collections` - Create collection
- `POST /api/assets/collections/{collectionId}/assets/{assetId}` - Add to collection

#### AI Generation (when available)
- `POST /api/assets/ai/generate` - Generate AI image

### Example Usage

```javascript
// Search assets
const response = await fetch('/api/assets?query=landscape&type=Image&page=1&pageSize=20');
const result = await response.json();
console.log(`Found ${result.totalCount} assets`);

// Upload asset
const formData = new FormData();
formData.append('file', fileInput.files[0]);
const asset = await fetch('/api/assets/upload', {
  method: 'POST',
  body: formData
}).then(r => r.json());
```

## Storage and Data

### File Storage

Assets are stored in the configured output directory:
```
{OutputDirectory}/AssetLibrary/
├── assets/          # Actual media files
├── thumbnails/      # Generated thumbnails
├── assets.json      # Asset metadata
└── collections.json # Collection data
```

### Metadata Storage

Asset metadata is stored in JSON format:
- **assets.json**: All asset records with metadata
- **collections.json**: Collection definitions

### Backup Recommendations

Back up these files to preserve your asset library:
- `assets.json`
- `collections.json`
- All files in `assets/` directory
- (Thumbnails can be regenerated)

## Automatic Tagging

### How It Works

When assets are imported, the AssetTagger service automatically generates tags:

#### For Images
- Asset type tag ("image")
- Resolution tags ("hd", "4k")
- Orientation ("landscape", "portrait", "square")
- Keywords extracted from filename

#### For Videos
- Asset type tag ("video")
- Resolution tags ("hd", "4k")
- Duration tags ("short", "medium", "long")
- Keywords from filename

#### For Audio
- Asset type tag ("audio")
- Duration tags ("short", "medium", "long")
- Mood keywords from filename ("upbeat", "corporate", "dramatic", etc.)
- Keywords from filename

### Tag Confidence Scores

Each tag includes a confidence score (0-100):
- **100**: Direct facts (file type, resolution)
- **90-95**: Calculated values (orientation, duration category)
- **80-85**: Inferred from filename or patterns
- **70-79**: Lower confidence inferences

## Advanced Features

### Asset Usage Tracking

The system tracks where assets are used:
- Records which timelines use each asset
- Prevents deletion of assets in use
- Shows usage count in asset details
- *Timeline integration coming soon*

### Safe Deletion

When deleting an asset:
1. System checks if asset is used in any timelines
2. If used, shows warning with timeline list
3. Option to delete from library only (keep file)
4. Option to delete from library and disk

### Batch Operations

*Coming soon*:
- Bulk tag editing
- Bulk collection assignment
- Bulk export as ZIP
- Batch delete with confirmation

## Troubleshooting

### No Assets Showing
- Check that assets have been imported
- Clear search filters
- Check selected collection (click "All Assets")

### Stock Images Not Working
- Verify API keys are configured in `appsettings.json`
- Check internet connection
- Verify API keys are valid and not rate-limited

### Upload Fails
- Check file format is supported
- Verify file size is reasonable (< 100MB recommended)
- Check disk space in output directory
- Check file permissions

### Thumbnails Not Generating
- Placeholder thumbnails shown currently
- Full thumbnail generation requires FFmpeg integration
- Check logs for errors

## Performance Tips

### For Large Libraries (1000+ assets)

- Use search and filters to narrow results
- Collections help organize assets
- Thumbnails are cached for performance
- Search is indexed for fast results

### Recommended Practices

- Tag assets consistently
- Use collections to organize by project
- Clean up unused assets periodically
- Back up metadata regularly

## Future Enhancements

Planned features for future releases:

- **Enhanced Thumbnails**: Real image/video thumbnails with FFmpeg
- **Lightbox View**: Full-screen preview with zoom and pan
- **Drag-and-Drop**: Drag assets directly to timeline
- **AI Image Generation**: Generate images with Stable Diffusion
- **Advanced Search**: Search operators and filters
- **Batch Operations**: UI for bulk operations
- **Timeline Integration**: Full integration with video editor
- **Smart Collections**: Auto-updating collections based on rules
- **Asset Analytics**: Usage reports and insights
- **Team Collaboration**: Share collections with team members

## Support

For issues or questions:
1. Check this guide for common solutions
2. Review application logs at `logs/aura-api-*.log`
3. Open an issue on GitHub
4. Consult API documentation at `/swagger` endpoint

## Technical Details

### Technology Stack

**Backend**:
- ASP.NET Core 8.0
- C# Services and Controllers
- JSON file storage
- Async/await throughout

**Frontend**:
- React with TypeScript
- Fluent UI v9 components
- CSS-in-JS styling
- Responsive design

### Architecture

```
Frontend (React)
    ↓
assetService.ts (API client)
    ↓
AssetsController (REST API)
    ↓
Asset Services (Business Logic)
    ├── AssetLibraryService
    ├── AssetTagger
    ├── ThumbnailGenerator
    ├── StockImageService
    ├── AIImageGenerator
    └── AssetUsageTracker
    ↓
JSON Storage (Persistence)
```

### Dependencies

- `Microsoft.Extensions.Logging` - Logging framework
- `System.Text.Json` - JSON serialization
- `Microsoft.Extensions.Http` - HTTP client factory
- No additional NuGet packages required

## Changelog

### Version 1.0.0 (Current)

**Initial Release**:
- ✅ Complete asset library system
- ✅ Search and filtering
- ✅ Automatic tagging
- ✅ Collections management
- ✅ Stock image integration (Pexels, Pixabay)
- ✅ Three-panel UI layout
- ✅ REST API with 12 endpoints
- ✅ Unit tests (11 tests passing)

---

*Last updated: October 2025*
