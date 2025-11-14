# Media Library and Asset Management Guide

## Overview

The Media Library is a comprehensive system for managing, organizing, and utilizing media assets in Aura. It provides a centralized location for all videos, audio files, images, and other media used in video projects.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Media Import](#media-import)
- [Organization](#organization)
- [Storage Management](#storage-management)
- [Integration with Generation](#integration-with-generation)
- [API Reference](#api-reference)
- [Best Practices](#best-practices)

## Features

### 1. Media Management
- **Upload & Import**: Drag-and-drop file upload with bulk import support
- **Format Support**: Videos (MP4, MOV, AVI, MKV, WebM), Audio (MP3, WAV, OGG, M4A), Images (JPG, PNG, GIF, WebP)
- **Automatic Processing**: Thumbnail generation, metadata extraction, and duplicate detection
- **Chunked Upload**: Support for large files (up to 5GB) with resumable uploads

### 2. Organization System
- **Collections**: Group related media items into collections (folders)
- **Tagging**: Add multiple tags to media for easy searching
- **Smart Filters**: Search by type, source, tags, date, and more
- **Sorting**: Sort by name, date, size, or usage count

### 3. Media Preview
- **In-App Playback**: View videos and listen to audio directly in the browser
- **Image Viewer**: View images with zoom capabilities
- **Metadata Display**: View detailed information about each media item
- **Waveform Visualization**: For audio files (planned feature)

### 4. Storage Management
- **Usage Tracking**: Monitor storage consumption by media type
- **Quota Management**: Set and track storage limits
- **Cleanup Tools**: Identify and remove unused or duplicate media
- **Optimization**: Compress and optimize media files

### 5. Generation Integration
- **Asset Reuse**: Use library assets in new video projects
- **Auto-Save**: Automatically save generated content to library
- **Version Management**: Track different versions of media
- **Usage Analytics**: See which assets are used in which projects

## Architecture

### Backend Components

#### Domain Models (`Aura.Core/Models/Media/`)
- `MediaEntity`: Database entity for media items
- `MediaCollectionEntity`: Collections/folders for organizing media
- `MediaTagEntity`: Tags for categorization
- `MediaUsageEntity`: Tracks media usage in projects
- `UploadSessionEntity`: Manages chunked uploads

#### Services (`Aura.Core/Services/Media/`)
- `MediaService`: Core media management operations
- `MediaRepository`: Database operations for media
- `ThumbnailGenerationService`: Generate thumbnails for media
- `MediaMetadataService`: Extract metadata from files
- `MediaGenerationIntegrationService`: Integration with video generation pipeline

#### API Controllers (`Aura.Api/Controllers/`)
- `MediaController`: REST API endpoints for media operations

### Frontend Components

#### Pages (`Aura.Web/src/pages/MediaLibrary/`)
- `MediaLibraryPage`: Main media library interface
- `MediaGrid`: Grid view of media items
- `MediaList`: List view of media items
- `MediaUploadDialog`: File upload interface
- `MediaPreviewDialog`: Media preview and playback
- `MediaFilterPanel`: Advanced filtering options
- `BulkOperationsBar`: Bulk operations on selected media
- `StorageStats`: Storage usage visualization
- `StorageManagementPanel`: Storage management interface

#### API Client (`Aura.Web/src/api/`)
- `mediaLibraryApi`: TypeScript client for API communication

## Getting Started

### Accessing the Media Library

1. Navigate to the Media Library from the main menu
2. The main interface shows all your media in grid or list view
3. Use the search bar and filters to find specific media

### First Upload

1. Click the "Upload Media" button in the top-right corner
2. Drag and drop files or click to browse
3. Add optional description, tags, and assign to a collection
4. Click "Upload" to start the process
5. Monitor upload progress for each file

### Creating Collections

Collections help organize related media:

```typescript
// Via UI
1. Click "Create Collection" button
2. Enter collection name and description
3. Assign media to collection during or after upload

// Via API
const collection = await mediaLibraryApi.createCollection({
  name: "My Project Assets",
  description: "All assets for my video project"
});
```

## Media Import

### Supported Formats

**Videos:**
- MP4 (H.264, H.265)
- MOV (QuickTime)
- AVI
- MKV (Matroska)
- WebM

**Audio:**
- MP3
- WAV
- OGG
- M4A
- AAC

**Images:**
- JPEG/JPG
- PNG
- GIF
- WebP
- SVG

### Import Methods

#### 1. Drag and Drop
```typescript
// Simple drag-and-drop implementation
<MediaUploadDialog
  onDrop={handleDrop}
  collections={collections}
  tags={availableTags}
/>
```

#### 2. Bulk Import
```typescript
// Upload multiple files at once
const files = Array.from(fileInput.files);
for (const file of files) {
  await mediaLibraryApi.uploadMedia(file, {
    fileName: file.name,
    type: getMediaType(file),
    generateThumbnail: true,
    extractMetadata: true
  });
}
```

#### 3. Chunked Upload (Large Files)
```typescript
// For files > 100MB, use chunked upload
const CHUNK_SIZE = 10 * 1024 * 1024; // 10MB chunks
const totalChunks = Math.ceil(file.size / CHUNK_SIZE);

// Initiate session
const session = await mediaLibraryApi.initiateChunkedUpload(
  file.name,
  file.size,
  totalChunks
);

// Upload chunks
for (let i = 0; i < totalChunks; i++) {
  const start = i * CHUNK_SIZE;
  const end = Math.min(start + CHUNK_SIZE, file.size);
  const chunk = file.slice(start, end);
  
  await mediaLibraryApi.uploadChunk(session.sessionId, i, chunk);
}

// Complete upload
const media = await mediaLibraryApi.completeChunkedUpload(
  session.sessionId,
  uploadRequest
);
```

## Organization

### Collections

Collections act as folders for organizing media:

```csharp
// Create a collection
var collection = await _mediaService.CreateCollectionAsync(new MediaCollectionRequest
{
    Name = "Product Videos",
    Description = "All product demonstration videos"
});

// Add media to collection
await _mediaService.UpdateMediaAsync(mediaId, new MediaUploadRequest
{
    FileName = media.FileName,
    Type = media.Type,
    CollectionId = collection.Id
});
```

### Tagging System

Tags provide flexible categorization:

```typescript
// Add tags during upload
await mediaLibraryApi.uploadMedia(file, {
  fileName: file.name,
  type: 'Video',
  tags: ['product', 'demo', 'tutorial']
});

// Search by tags
const results = await mediaLibraryApi.searchMedia({
  tags: ['product', 'demo'],
  pageSize: 50
});
```

### Smart Collections

Pre-defined smart collections based on criteria:

- **Recent**: Media added in the last 7 days
- **Favorites**: User-marked favorites
- **Unused**: Media not used in any project
- **Generated**: AI-generated media
- **Imported**: User-uploaded media

## Storage Management

### Monitoring Usage

```typescript
// Get storage statistics
const stats = await mediaLibraryApi.getStorageStats();

console.log(`Total used: ${formatFileSize(stats.totalSizeBytes)}`);
console.log(`Available: ${formatFileSize(stats.availableBytes)}`);
console.log(`Usage: ${stats.usagePercentage.toFixed(1)}%`);

// Breakdown by type
for (const [type, size] of Object.entries(stats.sizeByType)) {
  console.log(`${type}: ${formatFileSize(size)}`);
}
```

### Cleanup Suggestions

The system automatically suggests cleanup actions:

```csharp
public class StorageCleanupSuggestion
{
    public string Message { get; set; }
    public string Action { get; set; }
    public int FilesAffected { get; set; }
    public long SpaceToFree { get; set; }
}

// Get cleanup suggestions
var suggestions = await _storageManagementService.GetCleanupSuggestionsAsync();
```

### Optimization

```csharp
// Optimize storage
public class StorageOptimizationOptions
{
    public bool CompressImages { get; set; } = true;
    public bool OptimizeVideos { get; set; } = true;
    public bool RemoveDuplicates { get; set; } = true;
    public bool ArchiveOldProjects { get; set; } = false;
}

await _storageManagementService.OptimizeStorageAsync(options);
```

## Integration with Generation

### Using Library Assets in Projects

```csharp
// Get media suitable for a project
var projectMedia = await _integrationService.GetProjectMediaAsync(projectId);

// Link media to project
await _integrationService.LinkMediaToProjectAsync(
    mediaId,
    projectId,
    "My Video Project"
);
```

### Saving Generated Content

```csharp
// Save generated video to library
var media = await _integrationService.SaveGeneratedMediaAsync(
    filePath: "/path/to/generated-video.mp4",
    type: MediaType.Video,
    projectId: "project-123",
    description: "AI-generated product showcase",
    tags: new List<string> { "generated", "product", "ai" }
);
```

### Version Management

```csharp
// Track asset versions
public class AssetVersion
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
    public int VersionNumber { get; set; }
    public string Changes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Usage Analytics

```csharp
// Get media usage info
var usage = await _mediaService.GetMediaUsageAsync(mediaId);

Console.WriteLine($"Total uses: {usage.TotalUsages}");
Console.WriteLine($"Last used: {usage.LastUsedAt}");
Console.WriteLine($"Used in projects: {string.Join(", ", usage.UsedInProjects)}");
```

## API Reference

### Media Operations

#### Get Media by ID
```http
GET /api/media/{id}
```

#### Search Media
```http
POST /api/media/search
Content-Type: application/json

{
  "searchTerm": "product",
  "types": ["Video", "Image"],
  "tags": ["demo"],
  "page": 1,
  "pageSize": 50,
  "sortBy": "CreatedAt",
  "sortDescending": true
}
```

#### Upload Media
```http
POST /api/media/upload
Content-Type: multipart/form-data

file: (binary)
type: Video
description: "Product demo"
tags: "product,demo"
generateThumbnail: true
extractMetadata: true
```

#### Update Media
```http
PUT /api/media/{id}
Content-Type: application/json

{
  "fileName": "updated-name.mp4",
  "description": "Updated description",
  "tags": ["updated", "tag"]
}
```

#### Delete Media
```http
DELETE /api/media/{id}
```

### Collection Operations

#### Get All Collections
```http
GET /api/media/collections
```

#### Create Collection
```http
POST /api/media/collections
Content-Type: application/json

{
  "name": "My Collection",
  "description": "Collection description"
}
```

#### Update Collection
```http
PUT /api/media/collections/{id}
Content-Type: application/json

{
  "name": "Updated Name",
  "description": "Updated description"
}
```

#### Delete Collection
```http
DELETE /api/media/collections/{id}
```

### Bulk Operations

#### Bulk Actions
```http
POST /api/media/bulk
Content-Type: application/json

{
  "mediaIds": ["guid1", "guid2", "guid3"],
  "operation": "Delete", // or "Move", "AddTags", "RemoveTags", "ChangeCollection"
  "targetCollectionId": "collection-guid",
  "tags": ["tag1", "tag2"]
}
```

### Storage Statistics

#### Get Stats
```http
GET /api/media/stats
```

Response:
```json
{
  "totalSizeBytes": 1073741824,
  "quotaBytes": 10737418240,
  "availableBytes": 9663676416,
  "usagePercentage": 10.0,
  "totalFiles": 150,
  "filesByType": {
    "Video": 50,
    "Image": 80,
    "Audio": 20
  },
  "sizeByType": {
    "Video": 858993459,
    "Image": 214748364,
    "Audio": 0
  }
}
```

## Best Practices

### 1. Organization

- **Use Collections**: Group related media into collections by project or theme
- **Tag Consistently**: Develop a tagging convention and stick to it
- **Name Descriptively**: Use clear, descriptive file names
- **Regular Cleanup**: Periodically review and remove unused media

### 2. Storage Optimization

- **Compress Before Upload**: Optimize files before uploading when possible
- **Use Appropriate Formats**: Choose the right format for your needs
- **Archive Old Projects**: Move completed project media to archives
- **Monitor Quotas**: Keep an eye on storage usage and clean up regularly

### 3. Performance

- **Use Thumbnails**: Rely on thumbnails for browsing, not full media
- **Lazy Loading**: Implement lazy loading for large media libraries
- **Pagination**: Use pagination for large result sets
- **Caching**: Cache frequently accessed media locally

### 4. Integration

- **Track Usage**: Link media to projects for better organization
- **Version Control**: Keep track of different versions of assets
- **Metadata**: Add comprehensive metadata for better searchability
- **Backup**: Regularly backup your media library

### 5. Security

- **Access Control**: Implement proper access controls for sensitive media
- **Secure Upload**: Use HTTPS for all uploads
- **Content Validation**: Validate file types and sizes on upload
- **Duplicate Detection**: Use content hashing to detect duplicates

## Troubleshooting

### Upload Issues

**Problem**: Upload fails for large files  
**Solution**: Use chunked upload for files > 100MB

**Problem**: Thumbnail not generating  
**Solution**: Ensure FFmpeg is installed and configured

**Problem**: Metadata extraction fails  
**Solution**: Verify file format is supported and not corrupted

### Performance Issues

**Problem**: Slow media library loading  
**Solution**: Reduce page size, use lazy loading, check network connection

**Problem**: Search is slow  
**Solution**: Add database indexes, optimize queries, use full-text search

### Storage Issues

**Problem**: Running out of storage  
**Solution**: Use cleanup tools, compress media, increase quota

**Problem**: Can't delete media  
**Solution**: Check if media is in use by active projects

## Future Enhancements

- **AI Tagging**: Automatic tagging using computer vision
- **Advanced Search**: Semantic search using embeddings
- **Collaboration**: Share collections with team members
- **Cloud Sync**: Sync media across devices
- **CDN Integration**: Faster delivery via CDN
- **Video Editing**: Basic editing tools in the library
- **Format Conversion**: Convert between formats in-app
- **External Storage**: Support for external drives and cloud storage

## Support

For issues or questions:
- Check the [troubleshooting section](#troubleshooting)
- Review API documentation
- Contact support team

## Related Documentation

- Storage Configuration Guide
- [API Reference](api/index.md)
- Video Generation Guide
