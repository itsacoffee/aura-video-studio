# Proxy Media Timeline Integration Guide

## Overview

This guide provides instructions for integrating proxy media seamlessly into the timeline components for smooth playback and scrubbing performance.

## Current State

### Implemented ✅
- Proxy media generation with quality presets (Draft/Preview/High)
- LRU cache eviction with configurable size limits
- Background automatic eviction service
- Hardware-based quality preset suggestions
- Cache management UI component (ProxyCacheManager)
- Quality toggle component (QualityToggle)

### Pending ⏳
- Timeline playback integration with proxy switching
- Proxy-in-use indicators in timeline UI
- Seamless proxy/source switching during scrubbing
- Performance telemetry for timeline operations

## Integration Steps

### 1. Timeline Component Enhancement

**File**: `Aura.Web/src/components/Timeline/TimelineView.tsx`

#### Add Proxy Support to Timeline State

```typescript
import { proxyMediaService } from '@/services/proxyMediaService';
import { QualityToggle } from '@/components/Preview/QualityToggle';

interface TimelineViewProps {
  clips: TimelineClip[];
  // ... existing props
}

export const TimelineView: FC<TimelineViewProps> = ({ clips, ...props }) => {
  const [useProxy, setUseProxy] = useState(proxyMediaService.isProxyModeEnabled());
  const [effectivePaths, setEffectivePaths] = useState<Map<string, string>>(new Map());

  // Resolve proxy paths for all clips
  useEffect(() => {
    const resolvePaths = async () => {
      const newPaths = new Map<string, string>();
      
      for (const clip of clips) {
        const effectivePath = await proxyMediaService.getEffectiveMediaPath(clip.sourcePath);
        newPaths.set(clip.id, effectivePath);
      }
      
      setEffectivePaths(newPaths);
    };
    
    resolvePaths();
  }, [clips, useProxy]);

  const handleQualityToggle = (newUseProxy: boolean) => {
    setUseProxy(newUseProxy);
  };

  return (
    <div className="timeline-container">
      <div className="timeline-controls">
        <QualityToggle onToggle={handleQualityToggle} />
        {/* ... other controls */}
      </div>
      
      <div className="timeline-tracks">
        {clips.map(clip => (
          <TimelineClip
            key={clip.id}
            clip={clip}
            effectivePath={effectivePaths.get(clip.id) || clip.sourcePath}
            isUsingProxy={effectivePaths.get(clip.id) !== clip.sourcePath}
          />
        ))}
      </div>
    </div>
  );
};
```

### 2. Timeline Clip Component Enhancement

**File**: `Aura.Web/src/components/Timeline/TimelineClip.tsx`

#### Add Proxy Indicator Badge

```typescript
import { Badge } from '@fluentui/react-components';
import { VideoClip24Regular } from '@fluentui/react-icons';

interface TimelineClipProps {
  clip: TimelineClip;
  effectivePath: string;
  isUsingProxy: boolean;
  // ... existing props
}

export const TimelineClip: FC<TimelineClipProps> = ({ 
  clip, 
  effectivePath, 
  isUsingProxy,
  ...props 
}) => {
  return (
    <div className="timeline-clip">
      {/* Clip thumbnail */}
      <img src={generateThumbnail(effectivePath)} alt={clip.name} />
      
      {/* Proxy indicator badge */}
      {isUsingProxy && (
        <Badge 
          appearance="tint" 
          color="brand" 
          icon={<VideoClip24Regular />}
          size="small"
          style={{ position: 'absolute', top: 4, right: 4 }}
        >
          Proxy
        </Badge>
      )}
      
      {/* Clip name and duration */}
      <div className="clip-info">
        <span>{clip.name}</span>
        <span>{formatDuration(clip.duration)}</span>
      </div>
    </div>
  );
};
```

### 3. Video Player Integration

**File**: `Aura.Web/src/components/VideoPreview/VideoPreview.tsx`

#### Use Effective Path for Playback

```typescript
import { proxyMediaService } from '@/services/proxyMediaService';

export const VideoPreview: FC<VideoPreviewProps> = ({ sourcePath, ...props }) => {
  const [effectivePath, setEffectivePath] = useState(sourcePath);
  const [isLoadingProxy, setIsLoadingProxy] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    const loadEffectivePath = async () => {
      setIsLoadingProxy(true);
      
      try {
        const path = await proxyMediaService.getEffectiveMediaPath(sourcePath);
        setEffectivePath(path);
        
        // Preserve playback position if video was playing
        if (videoRef.current && !videoRef.current.paused) {
          const currentTime = videoRef.current.currentTime;
          videoRef.current.src = path;
          videoRef.current.currentTime = currentTime;
          videoRef.current.play();
        }
      } catch (error) {
        console.error('Error loading effective path:', error);
        setEffectivePath(sourcePath); // Fallback to source
      } finally {
        setIsLoadingProxy(false);
      }
    };
    
    loadEffectivePath();
  }, [sourcePath]);

  return (
    <div className="video-preview">
      {isLoadingProxy && <Spinner size="small" label="Loading..." />}
      
      <video
        ref={videoRef}
        src={effectivePath}
        controls
        preload="metadata"
      />
    </div>
  );
};
```

### 4. Scrubbing Performance Enhancement

**File**: `Aura.Web/src/services/timeline/TimelineEditor.ts`

#### Add Proxy-Aware Scrubbing

```typescript
export class TimelineEditor {
  private proxyMediaService = proxyMediaService;
  private effectivePathCache = new Map<string, string>();

  async loadClip(clipId: string, sourcePath: string): Promise<string> {
    // Check cache first
    if (this.effectivePathCache.has(clipId)) {
      return this.effectivePathCache.get(clipId)!;
    }

    // Resolve effective path (proxy or source)
    const effectivePath = await this.proxyMediaService.getEffectiveMediaPath(sourcePath);
    this.effectivePathCache.set(clipId, effectivePath);
    
    return effectivePath;
  }

  async scrubToTime(time: number): Promise<void> {
    const clip = this.getClipAtTime(time);
    
    if (!clip) {
      return;
    }

    const effectivePath = await this.loadClip(clip.id, clip.sourcePath);
    
    // Update video player with effective path
    this.updatePlayerSource(effectivePath, time - clip.startTime);
  }

  clearCache(): void {
    this.effectivePathCache.clear();
  }
}
```

### 5. Settings Integration

**File**: `Aura.Web/src/pages/Settings/PerformanceSettings.tsx`

#### Add Cache Management Section

```typescript
import { ProxyCacheManager } from '@/components/Preview/ProxyCacheManager';

export const PerformanceSettings: FC = () => {
  return (
    <div className="performance-settings">
      <h2>Performance Settings</h2>
      
      {/* Existing performance settings */}
      
      <section>
        <h3>Proxy Media Cache</h3>
        <p>
          Proxy media improves timeline scrubbing and playback performance by using
          lower-resolution versions of your source media. The cache is managed automatically.
        </p>
        
        <ProxyCacheManager 
          onStatsChanged={(stats) => {
            console.log('Cache stats updated:', stats);
          }}
        />
      </section>
    </div>
  );
};
```

## Performance Considerations

### Proxy Generation Triggers

1. **On Asset Import**: Automatically generate proxy when media is imported
2. **Background Processing**: Generate proxies for all timeline clips in background
3. **On-Demand**: Generate proxy when clip is added to timeline if not already cached

### Recommended Implementation

```typescript
// In asset import service
export class AssetImportService {
  async importAsset(file: File): Promise<Asset> {
    const asset = await this.uploadAsset(file);
    
    // Trigger background proxy generation
    this.generateProxyInBackground(asset);
    
    return asset;
  }

  private async generateProxyInBackground(asset: Asset): Promise<void> {
    // Don't block UI
    setTimeout(async () => {
      try {
        const quality = await this.suggestProxyQuality(asset);
        await proxyMediaService.generateProxy({
          sourcePath: asset.path,
          quality,
          backgroundGeneration: true,
          priority: 0,
        });
      } catch (error) {
        console.error('Background proxy generation failed:', error);
      }
    }, 0);
  }

  private async suggestProxyQuality(asset: Asset): Promise<string> {
    // Get system hardware tier
    const hardwareTier = await this.getHardwareTier();
    
    // Simple suggestion logic (can be replaced with API call)
    if (asset.width >= 3840) {
      return hardwareTier === 'A' ? 'High' : 'Preview';
    } else if (asset.width >= 1920) {
      return 'Preview';
    } else {
      return 'Draft';
    }
  }
}
```

## Testing Strategy

### Unit Tests

```typescript
describe('Timeline with Proxy Integration', () => {
  it('should use proxy path when proxy mode is enabled', async () => {
    proxyMediaService.setUseProxyMode(true);
    
    const clip = { id: '1', sourcePath: '/source/video.mp4' };
    const proxyPath = '/cache/proxy/video_preview.mp4';
    
    // Mock proxy service
    vi.spyOn(proxyMediaService, 'getEffectiveMediaPath')
      .mockResolvedValue(proxyPath);
    
    const timeline = new TimelineEditor();
    const effectivePath = await timeline.loadClip(clip.id, clip.sourcePath);
    
    expect(effectivePath).toBe(proxyPath);
  });

  it('should show proxy indicator badge when using proxy', () => {
    const clip = { id: '1', name: 'Video', sourcePath: '/source/video.mp4' };
    
    const { getByText } = render(
      <TimelineClip
        clip={clip}
        effectivePath="/cache/proxy/video_preview.mp4"
        isUsingProxy={true}
      />
    );
    
    expect(getByText('Proxy')).toBeInTheDocument();
  });
});
```

### Integration Tests

```typescript
describe('Timeline Playback with Proxy', () => {
  it('should seamlessly switch between proxy and source', async () => {
    const timeline = render(<TimelineView clips={testClips} />);
    
    // Start with proxy mode
    proxyMediaService.setUseProxyMode(true);
    await waitFor(() => {
      expect(timeline.getByText('Proxy Active')).toBeInTheDocument();
    });
    
    // Toggle to source mode
    const toggle = timeline.getByRole('switch');
    fireEvent.click(toggle);
    
    await waitFor(() => {
      expect(timeline.getByText('Source Quality')).toBeInTheDocument();
    });
  });
});
```

### Performance Tests

```typescript
describe('Scrubbing Performance', () => {
  it('should measure FPS improvement with proxies', async () => {
    const timeline = new TimelineEditor();
    
    // Measure without proxy
    proxyMediaService.setUseProxyMode(false);
    const fpsWithoutProxy = await measureScrubbingFPS(timeline);
    
    // Measure with proxy
    proxyMediaService.setUseProxyMode(true);
    const fpsWithProxy = await measureScrubbingFPS(timeline);
    
    // Expect at least 50% improvement
    expect(fpsWithProxy).toBeGreaterThan(fpsWithoutProxy * 1.5);
  });
});

async function measureScrubbingFPS(timeline: TimelineEditor): Promise<number> {
  const frames: number[] = [];
  const duration = 5000; // 5 seconds
  const startTime = performance.now();
  
  while (performance.now() - startTime < duration) {
    const frameStart = performance.now();
    await timeline.scrubToTime(Math.random() * 60);
    const frameTime = performance.now() - frameStart;
    frames.push(1000 / frameTime);
  }
  
  return frames.reduce((a, b) => a + b) / frames.length;
}
```

## Final Render Verification

### Ensure Original Media is Used

```typescript
export class RenderService {
  async renderTimeline(timeline: Timeline): Promise<string> {
    // CRITICAL: Always use source paths for final render
    const clips = timeline.clips.map(clip => ({
      ...clip,
      // Force source path, not proxy path
      path: clip.sourcePath,
    }));

    console.log('Rendering with source quality:', clips);
    
    const result = await this.ffmpegService.render({
      clips,
      resolution: timeline.resolution,
      bitrate: timeline.bitrate,
      // Use original quality settings
    });

    return result.outputPath;
  }
}
```

## Acceptance Criteria Checklist

- [ ] Proxy media is generated on asset ingest
- [ ] Timeline uses proxy media for preview playback
- [ ] Scrubbing is smooth with proxies enabled (24+ FPS)
- [ ] Proxy-in-use indicators visible in timeline
- [ ] Quality toggle switches between proxy/source seamlessly
- [ ] Cache size limits are enforced
- [ ] LRU eviction removes least recently used proxies
- [ ] Manual cache purge works via UI
- [ ] **Final render always uses original source media (CRITICAL)**
- [ ] Performance improvement is measurable (2x+ FPS improvement)

## Common Pitfalls to Avoid

1. **Using Proxy in Final Render**: Always verify render service uses source paths
2. **Not Handling Missing Proxies**: Always fall back to source if proxy doesn't exist
3. **Memory Leaks**: Clear path caches when components unmount
4. **Race Conditions**: Handle concurrent proxy generation requests properly
5. **Cache Invalidation**: Update effective paths when proxy mode changes

## Next Steps

1. Implement timeline component integration following this guide
2. Add performance telemetry to measure FPS improvements
3. Run performance benchmarks with/without proxies
4. Verify final render uses original media
5. Update user documentation with proxy usage guide

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-05  
**Status**: ⏳ Integration Guide Ready for Implementation
