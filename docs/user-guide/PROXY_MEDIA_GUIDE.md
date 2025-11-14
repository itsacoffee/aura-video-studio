# Proxy Media and Performance Optimization Guide

## Overview

Aura Video Studio includes advanced proxy media and caching features to ensure smooth playback and editing, even with large 4K or 8K video files. This guide explains how to use these features to optimize your workflow.

## What is Proxy Media?

Proxy media are lower-resolution versions of your source video files that are automatically generated for faster preview and editing. The original source files are always used for final rendering, ensuring maximum quality in your output.

### Benefits

- **Faster Preview**: Scrub through timelines instantly, even with 4K footage
- **Smoother Playback**: Maintain 24+ FPS during preview playback
- **Lower CPU/GPU Usage**: Reduce system load during editing
- **Better Responsiveness**: Minimize lag when applying effects or edits

## Quality Presets

### Draft (480p)
- **Resolution**: 854x480
- **Bitrate**: 1.5 Mbps
- **Use Case**: Quick previews, rough cuts, low-end systems
- **File Size**: ~70% smaller than source

### Preview (720p) - Default
- **Resolution**: 1280x720
- **Bitrate**: 3 Mbps
- **Use Case**: Balanced quality and performance
- **File Size**: ~40% smaller than source

### High (1080p)
- **Resolution**: 1920x1080
- **Bitrate**: 5 Mbps
- **Use Case**: Detailed review, high-end systems
- **File Size**: ~20% smaller than source

## Enabling Proxy Media

### Performance Settings

1. Open **Settings** > **Performance**
2. Locate the **Preview & Caching** section
3. Enable **Proxy Generation**
4. Adjust **Proxy Quality** slider (25-75%)
5. Click **Save Performance Settings**

### During Editing

Proxies are automatically generated in the background when you import media. You can:

- **Toggle Quality**: Use the quality toggle in the preview panel to switch between proxy and source
- **Monitor Progress**: Check the cache stats button to see proxy generation status
- **Clear Cache**: Free up disk space by clearing unused proxies

## Using the Quality Toggle

The Quality Toggle component appears in preview panels and provides:

### Fast Mode (Proxy Active)
- Blue "Proxy Active" badge
- Uses lower-resolution proxy files
- Optimal for editing and scrubbing

### High Quality Mode
- Green "Source Quality" badge
- Uses original source files
- Optimal for final review before export

### Switching Modes

Click the toggle switch to change between modes. The change takes effect immediately for all preview operations.

## Waveform Caching

Audio waveforms are automatically cached for faster timeline rendering:

- **Persistent Cache**: Waveforms are saved to disk and reused across sessions
- **Priority Loading**: Visible timeline ranges are loaded first
- **Automatic Invalidation**: Cache updates when source files change

## Performance Telemetry

The system tracks performance metrics in real-time:

### Metrics Tracked

- **Playback FPS**: Frames per second during playback (target: 24+)
- **Scrub Latency**: Response time when scrubbing timeline (target: <50ms)
- **Cache Hit Rate**: Percentage of frames served from cache (target: 50%+)

### Viewing Metrics

Performance metrics can be viewed through:
1. **Developer Console**: `performanceTelemetry.getMetricsSummary()`
2. **Performance Settings**: View cache stats button
3. **API Endpoint**: `GET /api/proxy/stats`

## Cache Management

### Viewing Cache Statistics

1. Go to **Settings** > **Performance**
2. Scroll to **Preview & Caching** section
3. Click **View Cache Stats**

You'll see:
- Total number of proxies
- Total cache size in MB
- Space saved percentage

### Clearing Cache

To free up disk space:

1. Go to **Settings** > **Performance**
2. Scroll to **Preview & Caching** section
3. Click **Clear Proxy Cache**
4. Confirm the action

**Note**: Clearing cache does not affect source files. Proxies will be regenerated as needed.

### Cache Location

Proxy files are stored in:
- **Windows**: `%TEMP%\aura-proxy-cache\`
- **macOS/Linux**: `/tmp/aura-proxy-cache/`

Waveform cache is stored in:
- **Windows**: `%TEMP%\aura-waveform-cache\`
- **macOS/Linux**: `/tmp/aura-waveform-cache/`

## Troubleshooting

### Proxy Generation is Slow

**Possible Causes**:
- Large source files (4K/8K)
- Limited CPU resources
- Multiple proxies generating simultaneously

**Solutions**:
1. Close other applications to free up CPU
2. Generate proxies for one file at a time
3. Use Draft quality for faster generation
4. Enable hardware acceleration in settings

### Preview is Choppy

**Check**:
1. Verify proxy mode is enabled (check for "Proxy Active" badge)
2. Ensure proxies have finished generating (check cache stats)
3. Clear cache and regenerate if corrupted
4. Check performance telemetry metrics

**If FPS is low** (<24):
- Lower proxy quality to Draft
- Reduce timeline complexity
- Close other applications
- Check hardware acceleration settings

**If scrub latency is high** (>50ms):
- Enable waveform caching
- Increase cache size limit
- Use SSD for cache directory (if possible)

### Cache Taking Too Much Space

**Solutions**:
1. Reduce cache size limit in Performance Settings
2. Use Draft quality instead of High
3. Clear cache regularly
4. Delete unused proxies manually

### Proxies Not Being Used

**Verify**:
1. Proxy generation is enabled in Settings
2. Proxy mode toggle is ON (blue badge visible)
3. Proxies have completed generation (check status)
4. Source file path hasn't changed

## Best Practices

### For Large Projects

1. **Enable proxy generation early**: Let proxies build while you organize
2. **Use Draft quality initially**: Switch to Preview or High later
3. **Clear cache between projects**: Free up space for new proxies
4. **Monitor cache size**: Set appropriate limits based on available disk space

### For 4K/8K Workflows

1. **Always use proxies**: Source files are too large for smooth preview
2. **Use Preview or High quality**: Maintain sufficient detail for edits
3. **Enable hardware acceleration**: GPU encoding speeds up proxy generation
4. **Batch proxy generation**: Import all media first, generate proxies overnight

### For Low-End Systems

1. **Use Draft quality**: Minimize CPU/GPU load
2. **Reduce cache size**: Avoid disk space issues
3. **Close other applications**: Free up system resources
4. **Enable background rendering**: Generate proxies when idle

## API Reference

For developers integrating with Aura's proxy system:

### Generate Proxy
```
POST /api/proxy/generate
Body: {
  "sourcePath": "/path/to/video.mp4",
  "quality": "Preview",
  "backgroundGeneration": true,
  "priority": 0,
  "overwrite": false
}
```

### Check Proxy Status
```
GET /api/proxy/metadata?sourcePath=/path/to/video.mp4&quality=Preview
```

### Get Cache Stats
```
GET /api/proxy/stats
Response: {
  "totalProxies": 10,
  "totalCacheSizeBytes": 524288000,
  "totalSourceSizeBytes": 1048576000,
  "compressionRatio": 0.5
}
```

## Performance Targets

The system is designed to meet these performance targets:

- **Scrub Latency**: < 50ms for responsive timeline interaction
- **Playback FPS**: 24+ for smooth preview playback
- **Cache Hit Rate**: 50%+ for efficient resource usage
- **Proxy Generation**: < 5 minutes for typical 5-minute 1080p source

When targets are not met, the system logs warnings and suggestions for optimization.

## Technical Details

### Proxy Generation Pipeline

1. **Source Analysis**: FFmpeg analyzes source file properties
2. **Quality Selection**: Choose target resolution and bitrate
3. **Encoding**: FFmpeg generates proxy with H.264 codec
4. **Metadata Storage**: Save proxy info and link to source
5. **Cache Registration**: Add to proxy cache for quick access

### Waveform Generation

1. **Audio Extraction**: FFmpeg extracts PCM audio data
2. **Downsampling**: Reduce to target sample count (typically 1000)
3. **RMS Calculation**: Compute root mean square for each window
4. **Cache Storage**: Save as JSON for fast retrieval
5. **Hash-Based Keys**: Use file hash + size + mtime for cache keys

### Frame Cache

The existing frame cache service provides thumbnail generation:
- **LRU Eviction**: Least recently used frames are removed first
- **Configurable Size**: Default 100MB, adjustable in settings
- **Preloading**: Frames ahead of playhead are loaded in advance
- **Priority System**: Visible frames get higher priority

## Support

For additional help:
- Check the Performance Settings Guide
- Review [Troubleshooting Guide](../../TROUBLESHOOTING.md)
- Contact support with performance telemetry data
