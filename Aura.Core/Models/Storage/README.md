# Storage Models

This directory contains models for the local storage and caching system.

## Models

### LocalStorageConfiguration
Configuration for the enhanced local storage service including:
- Storage root path
- Storage quota in bytes
- Low space threshold
- Cache size limits
- Auto-cleanup settings

### StorageStatistics
Real-time storage usage statistics:
- Total/used/available bytes
- Usage percentage
- Per-folder size breakdown
- Total file count
- Low space indicator

### DiskSpaceInfo
System-level disk space information:
- Drive name and paths
- Total/available/used space
- Formatted size displays
- Low space warnings

### CacheEntry
Metadata for cached items:
- Unique ID and key
- File path and category
- Size and creation time
- Access statistics
- Expiration time
- Custom metadata

### CacheStatistics
Cache performance metrics:
- Total size and entry count
- Usage percentage
- Per-category breakdowns
- Hit/miss rates
- Oldest/newest entries

### CacheCleanupResult
Results from cache cleanup operations:
- Entries removed
- Bytes freed
- Duration
- Affected categories

## Usage

These models are used by the `EnhancedLocalStorageService` to provide comprehensive storage management and monitoring capabilities.
