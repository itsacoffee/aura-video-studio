# Visual Waveforms and Video Thumbnails - Usage Guide

This guide explains how to use the new visual waveform and video thumbnail features added to the timeline editor.

## Features Overview

### 1. Visual Waveforms for Audio Clips

The timeline now displays real audio waveforms using WaveSurfer.js, replacing the previous mock implementation.

**Features:**
- Real-time audio visualization
- Color-coded tracks:
  - 🔵 Blue for narration
  - 🟢 Green for music
  - 🟠 Orange for sound effects (SFX)
- Muted tracks shown in grayscale
- Selection highlighting with border
- Interactive scrubbing support

**Usage Example:**
```tsx
import { TimelineTrack } from './components/Editor/Timeline/TimelineTrack';

<TimelineTrack
  name="Background Music"
  type="music"
  audioPath="/path/to/audio.mp3"
  duration={120}
  zoom={50}
  muted={false}
  selected={false}
  onSeek={(time) => console.log('Seeked to:', time)}
/>
```

### 2. Video Thumbnails on Timeline Clips

Scene blocks now display video thumbnail previews extracted using FFmpeg.

**Features:**
- Automatic thumbnail extraction from video files
- Customizable timestamp for thumbnail capture
- Configurable dimensions
- Graceful fallback to placeholder icons
- Only shows when scene width > 100px for better UX

**Usage Example:**
```tsx
import { SceneBlock } from './components/Editor/Timeline/SceneBlock';

<SceneBlock
  index={0}
  heading="Opening Scene"
  start={0}
  duration={10}
  zoom={50}
  videoPath="/path/to/video.mp4"
  thumbnailTimestamp={1}
  selected={false}
  onSelect={() => console.log('Selected')}
/>
```

### Standalone Video Thumbnail Component

You can also use the VideoThumbnail component independently:

```tsx
import { VideoThumbnail } from './components/Editor/Timeline/VideoThumbnail';

<VideoThumbnail
  videoPath="/path/to/video.mp4"
  timestamp={5}
  width={160}
  height={90}
/>
```

## Technical Details

### Dependencies Added
- `wavesurfer.js@7.8.12` - Audio waveform visualization
- `@ffmpeg/ffmpeg@0.12.10` - Video processing
- `@ffmpeg/util@0.12.1` - FFmpeg utilities

All dependencies have been verified against the GitHub Advisory Database with no vulnerabilities found.

### Browser Requirements

**For Waveforms:**
- Modern browser with Web Audio API support
- Audio file formats: MP3, WAV, OGG, etc.

**For Thumbnails:**
- SharedArrayBuffer support (requires Cross-Origin-Opener-Policy headers)
- WASM support
- Video file formats: MP4, WebM, etc.

### Performance Considerations

1. **Waveforms**: Load on-demand when audio path is provided
2. **Thumbnails**: FFmpeg is loaded once and reused for all thumbnails
3. **Memory**: Thumbnails are cleaned up when component unmounts
4. **Network**: FFmpeg core (~2MB) is loaded from CDN on first use

## Error Handling

Both features include graceful error handling:

- Waveforms fall back to empty track on load failure
- Thumbnails show placeholder icons if FFmpeg fails to initialize
- Console logging for debugging issues

## Testing

The implementation includes comprehensive tests:
- 10 tests for TimelineTrack with WaveSurfer integration
- 5 tests for VideoThumbnail component

Run tests with:
```bash
npm test
```

## Future Enhancements

Potential improvements for future PRs:
- Caching extracted thumbnails for performance
- Multiple thumbnails along long clips
- Waveform zoom synchronization
- Custom waveform colors per track
- Progress indicators during thumbnail extraction
