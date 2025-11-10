# Video Effects System Guide

## Overview

The Video Effects System provides professional-grade video effects, transitions, and filters to enhance generated videos. This comprehensive guide covers all aspects of the effects system, from basic usage to advanced customization.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Effects Library](#effects-library)
3. [Timeline Editor](#timeline-editor)
4. [Custom Effects](#custom-effects)
5. [Effect Types](#effect-types)
6. [API Reference](#api-reference)
7. [Performance Optimization](#performance-optimization)
8. [Best Practices](#best-practices)

---

## Getting Started

### Basic Workflow

1. **Select Effects**: Browse the effects library and choose effects
2. **Apply to Timeline**: Drag effects onto the timeline
3. **Adjust Parameters**: Fine-tune effect properties
4. **Preview**: See effects in real-time
5. **Render**: Apply effects to your final video

### Quick Start Example

```typescript
// Apply a cinematic preset to a video
const result = await videoEffectsApi.applyPreset({
  inputPath: '/path/to/video.mp4',
  presetId: 'cinematic',
  outputPath: '/path/to/output.mp4'
});
```

---

## Effects Library

### Built-in Presets

The system includes several professional presets:

#### Cinematic
- High contrast color grading
- Warm temperature adjustment
- Professional film look

#### Vintage
- Old film aesthetic
- Grain and vignette effects
- Sepia toning

#### Dramatic
- High contrast
- Enhanced saturation
- Moody atmosphere

#### Black & White
- Classic monochrome conversion
- Subtle vignette
- Film grain option

### Browsing Effects

```tsx
import { EffectsLibrary } from '@/components/VideoEffects/EffectsLibrary';

function MyComponent() {
  return (
    <EffectsLibrary
      onEffectAdd={(effect) => addToTimeline(effect)}
      onPresetApply={(preset) => applyPreset(preset)}
    />
  );
}
```

---

## Timeline Editor

### Features

- **Visual Timeline**: See all effects on a timeline
- **Drag & Drop**: Easily position effects
- **Resize**: Adjust effect duration by dragging handles
- **Layering**: Stack multiple effects
- **Keyframe Animation**: Animate parameters over time
- **Real-time Preview**: See changes instantly

### Using the Timeline

```tsx
import { EffectsTimeline } from '@/components/VideoEffects/EffectsTimeline';

function MyComponent() {
  const [effects, setEffects] = useState<VideoEffect[]>([]);
  const [currentTime, setCurrentTime] = useState(0);

  return (
    <EffectsTimeline
      videoDuration={60}
      effects={effects}
      currentTime={currentTime}
      onEffectsChange={setEffects}
      onTimeChange={setCurrentTime}
      onEffectSelect={(effect) => console.log('Selected:', effect)}
      selectedEffect={null}
    />
  );
}
```

### Timeline Controls

- **Play/Pause**: Preview with effects
- **Zoom**: Adjust timeline scale
- **Scrub**: Navigate through video
- **Snap to Grid**: Align effects precisely

---

## Custom Effects

### Creating Custom Presets

```typescript
const customPreset: EffectPreset = {
  id: 'my-custom-effect',
  name: 'My Custom Look',
  description: 'A unique visual style',
  category: 'Custom',
  isBuiltIn: false,
  isFavorite: false,
  tags: ['custom', 'unique'],
  effects: [
    {
      id: '1',
      name: 'Color Grade',
      type: 'ColorCorrection',
      category: 'ColorGrading',
      startTime: 0,
      duration: 60,
      intensity: 1.0,
      enabled: true,
      layer: 0,
      keyframes: [],
      parameters: {
        brightness: 0.1,
        contrast: 0.2,
        saturation: -0.1
      },
      tags: []
    }
  ],
  parameters: {},
  usageCount: 0,
  createdAt: new Date(),
  modifiedAt: new Date()
};

// Save the preset
await videoEffectsApi.savePreset(customPreset);
```

### Effect Parameters

Each effect type supports different parameters:

#### Color Correction
- `brightness`: -1.0 to 1.0
- `contrast`: -1.0 to 1.0
- `saturation`: -1.0 to 1.0
- `hue`: 0 to 360 degrees
- `gamma`: 0.1 to 10.0
- `temperature`: -100 to 100
- `tint`: -100 to 100

#### Blur Effects
- `strength`: 0 to 100
- `type`: Gaussian, Box, Motion, Radial, Zoom
- `angle`: 0 to 360 degrees (for motion blur)
- `centerX`: 0.0 to 1.0 (for radial/zoom)
- `centerY`: 0.0 to 1.0 (for radial/zoom)

#### Text Effects
- `text`: String
- `fontSize`: Integer (pixels)
- `fontColor`: Hex color or name
- `positionX`: Expression or pixel value
- `positionY`: Expression or pixel value
- `fontFamily`: String
- `alignment`: left, center, right

---

## Effect Types

### 1. Transitions

Smooth transitions between clips or within videos.

**Available Types:**
- Fade
- Dissolve
- Wipe (left, right, up, down)
- Slide (left, right, up, down)
- Circle (open/close)
- Pixelize
- Radial
- 3D transitions (cube, flip, rotate)

**Example:**
```csharp
var transition = new TransitionEffect
{
    Name = "Fade Transition",
    TransitionType = TransitionType.Fade,
    Duration = 1.0,
    Offset = 5.0,
    Easing = EasingFunction.EaseInOut
};
```

### 2. Color Filters

Adjust colors, brightness, and contrast.

**Available Filters:**
- Color Correction
- Vintage/Retro
- Black & White
- Sepia
- Color Grading

**Example:**
```csharp
var colorEffect = new ColorCorrectionEffect
{
    Name = "Cinematic Grade",
    Brightness = 0.1,
    Contrast = 0.2,
    Saturation = -0.1,
    Temperature = 10,
    Duration = 60
};
```

### 3. Blur Effects

Various blur types for different effects.

**Available Types:**
- Gaussian Blur
- Box Blur
- Motion Blur
- Radial Blur
- Zoom Blur

**Example:**
```csharp
var blur = new BlurEffect
{
    Name = "Soft Focus",
    Type = BlurType.Gaussian,
    Strength = 5.0,
    Duration = 60
};
```

### 4. Text Animations

Animated text overlays.

**Available Animations:**
- Typewriter Effect
- Fade In/Out
- Sliding Text
- Kinetic Typography
- Scrolling Text (credits/ticker)

**Example:**
```csharp
var text = new TypewriterEffect
{
    Name = "Title",
    Text = "Hello World",
    FontSize = 48,
    FontColor = "white",
    Speed = 10.0,
    Duration = 5.0
};
```

### 5. Artistic Effects

Creative visual effects.

**Available Effects:**
- Chromatic Aberration
- Sharpen
- Vignette
- Film Grain
- Distortion

---

## API Reference

### REST Endpoints

#### Get All Presets
```http
GET /api/video-effects/presets?category=Cinematic
```

#### Get Specific Preset
```http
GET /api/video-effects/presets/{id}
```

#### Save Custom Preset
```http
POST /api/video-effects/presets
Content-Type: application/json

{
  "name": "My Preset",
  "description": "Custom effect",
  "category": "Custom",
  "effects": [...]
}
```

#### Apply Effects to Video
```http
POST /api/video-effects/apply
Content-Type: application/json

{
  "inputPath": "/path/to/video.mp4",
  "outputPath": "/path/to/output.mp4",
  "effects": [...],
  "useCache": true
}
```

#### Apply Preset
```http
POST /api/video-effects/apply-preset
Content-Type: application/json

{
  "inputPath": "/path/to/video.mp4",
  "presetId": "cinematic"
}
```

#### Generate Preview
```http
POST /api/video-effects/preview
Content-Type: application/json

{
  "inputPath": "/path/to/video.mp4",
  "effect": {...},
  "previewDurationSeconds": 5.0
}
```

#### Get Recommendations
```http
GET /api/video-effects/recommendations?videoPath=/path/to/video.mp4
```

#### Cache Management
```http
GET /api/video-effects/cache/stats
DELETE /api/video-effects/cache
```

### TypeScript/JavaScript API

```typescript
import { videoEffectsApi } from '@/services/api/videoEffects';

// Get presets
const presets = await videoEffectsApi.getPresets('Cinematic');

// Apply effects
const result = await videoEffectsApi.applyEffects({
  inputPath: '/path/to/video.mp4',
  effects: [...],
  useCache: true
});

// Generate preview
const preview = await videoEffectsApi.generatePreview({
  inputPath: '/path/to/video.mp4',
  effect: myEffect,
  previewDurationSeconds: 5.0
});

// Validate effect
const validation = await videoEffectsApi.validateEffect(myEffect);
```

### C# API

```csharp
// Inject service
private readonly IVideoEffectService _effectService;

// Apply effects
var outputPath = await _effectService.ApplyEffectsAsync(
    inputPath: "/path/to/video.mp4",
    outputPath: "/path/to/output.mp4",
    effects: effects,
    progressCallback: (progress) => Console.WriteLine($"Progress: {progress}%"),
    cancellationToken: cancellationToken
);

// Apply preset
var result = await _effectService.ApplyPresetAsync(
    inputPath: "/path/to/video.mp4",
    outputPath: "/path/to/output.mp4",
    presetId: "cinematic",
    progressCallback: (progress) => Console.WriteLine($"Progress: {progress}%"),
    cancellationToken: cancellationToken
);
```

---

## Performance Optimization

### Caching

The system automatically caches effect results for faster subsequent applications.

**Cache Configuration:**
```csharp
// Cache is enabled by default
var result = await videoEffectsApi.applyEffects({
  inputPath: '/path/to/video.mp4',
  effects: [...],
  useCache: true  // Default: true
});

// Check cache statistics
const stats = await videoEffectsApi.getCacheStats();
console.log(`Hit rate: ${stats.hitRate * 100}%`);
```

**Cache Management:**
```typescript
// Clear cache
await videoEffectsApi.clearCache();

// Get statistics
const stats = await videoEffectsApi.getCacheStats();
```

### GPU Acceleration

Effects automatically use hardware acceleration when available:
- NVIDIA NVENC/NVDEC
- Intel Quick Sync
- AMD VCE/AMF

### Preview Quality

Generate lower-quality previews for faster iteration:

```typescript
const preview = await videoEffectsApi.generatePreview({
  inputPath: '/path/to/video.mp4',
  effect: myEffect,
  previewDurationSeconds: 5.0  // Short duration for quick preview
});
```

### Background Rendering

Apply effects in the background without blocking the UI:

```typescript
// Use async/await for non-blocking execution
const applyEffectsAsync = async () => {
  try {
    const result = await videoEffectsApi.applyEffects({
      inputPath: videoPath,
      effects: effects
    });
    console.log('Effects applied:', result.outputPath);
  } catch (error) {
    console.error('Failed to apply effects:', error);
  }
};
```

---

## Best Practices

### 1. Effect Organization

- Use layers to organize effects
- Name effects descriptively
- Group related effects in presets
- Tag effects for easy searching

### 2. Performance

- Use cache for repeated operations
- Generate low-quality previews first
- Apply effects in batches when possible
- Clean up old cache entries periodically

### 3. Quality

- Start with subtle effects
- Build up complexity gradually
- Test on different videos
- Use presets as starting points

### 4. Workflow

- Save work as custom presets
- Use favorites for commonly used effects
- Leverage A/B testing
- Document custom effects

### 5. Timeline Management

- Keep effects organized by layer
- Use consistent timing
- Align transitions with audio
- Preview frequently

### 6. Custom Effects

- Test with different videos
- Document parameter ranges
- Share useful presets
- Version control presets

---

## Troubleshooting

### Common Issues

#### Effect Not Applying
- Check effect is enabled
- Verify timing (startTime + duration)
- Check layer visibility
- Validate effect parameters

#### Poor Performance
- Reduce preview quality
- Clear cache if full
- Check hardware acceleration
- Simplify effect stack

#### Preview Doesn't Match Final
- Ensure same quality settings
- Check cache invalidation
- Verify FFmpeg version
- Review effect parameters

### Debug Mode

Enable detailed logging:

```csharp
// In appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Aura.Core.Services.VideoEffects": "Debug"
    }
  }
}
```

---

## Examples

### Example 1: Cinematic Look

```typescript
const cinematicEffects: VideoEffect[] = [
  {
    id: '1',
    name: 'Color Grade',
    type: 'ColorCorrection',
    category: 'ColorGrading',
    startTime: 0,
    duration: 60,
    intensity: 1.0,
    enabled: true,
    layer: 0,
    keyframes: [],
    parameters: {
      contrast: 0.2,
      saturation: -0.1,
      temperature: 10
    },
    tags: ['cinematic']
  }
];
```

### Example 2: Title Sequence

```typescript
const titleSequence: VideoEffect[] = [
  {
    id: '1',
    name: 'Title Fade In',
    type: 'TextAnimation',
    category: 'Basic',
    startTime: 0,
    duration: 3,
    intensity: 1.0,
    enabled: true,
    layer: 1,
    keyframes: [],
    parameters: {
      text: 'My Video Title',
      fontSize: 72,
      fontColor: 'white',
      fadeInDuration: 1.0,
      fadeOutDuration: 1.0
    },
    tags: ['title', 'text']
  }
];
```

### Example 3: Vintage Film

```typescript
const vintageEffect: VideoEffect[] = [
  {
    id: '1',
    name: 'Vintage Look',
    type: 'Filter',
    category: 'Vintage',
    startTime: 0,
    duration: 60,
    intensity: 0.8,
    enabled: true,
    layer: 0,
    keyframes: [],
    parameters: {
      style: 'OldFilm',
      grain: 0.4,
      vignette: 0.6,
      scratches: 0.2
    },
    tags: ['vintage', 'retro']
  }
];
```

---

## Advanced Topics

### Keyframe Animation

Animate effect parameters over time:

```typescript
const animatedEffect: VideoEffect = {
  id: '1',
  name: 'Animated Blur',
  type: 'Filter',
  category: 'Blur',
  startTime: 0,
  duration: 10,
  intensity: 1.0,
  enabled: true,
  layer: 0,
  keyframes: [
    {
      time: 0,
      parameterName: 'strength',
      value: 0,
      easing: 'EaseInOut'
    },
    {
      time: 5,
      parameterName: 'strength',
      value: 10,
      easing: 'EaseInOut'
    },
    {
      time: 10,
      parameterName: 'strength',
      value: 0,
      easing: 'EaseInOut'
    }
  ],
  parameters: {},
  tags: ['animated']
};
```

### Effect Stacking

Combine multiple effects for complex looks:

```typescript
const effectStack: EffectStack = {
  id: 'stack1',
  name: 'Film Look',
  effects: [
    colorGradeEffect,
    grainEffect,
    vignetteEffect
  ],
  blendMode: 'normal',
  opacity: 1.0,
  enabled: true
};
```

### Custom FFmpeg Filters

For advanced users, implement custom effects:

```csharp
public class CustomEffect : VideoEffect
{
    public override string ToFFmpegFilter()
    {
        // Your custom FFmpeg filter string
        return "custom_filter=param1=value1:param2=value2";
    }
}
```

---

## Support

For issues, feature requests, or questions:
- GitHub Issues: [Link to repo]
- Documentation: [Link to docs]
- Community Forum: [Link to forum]

---

## License

[Your License Information]
