# Chroma Key and Compositing Features

This document describes the green screen / chroma key and advanced compositing features implemented in Aura Video Studio.

## Features

### 1. Chroma Key Effect

Remove green or blue screen backgrounds with professional-grade keying.

**Key Parameters:**
- **Key Color**: Select the color to key out (green, blue, or custom)
- **Similarity**: Controls the color range to be keyed (0-100)
- **Smoothness**: Edge feathering for natural blending (0-100)
- **Spill Suppression**: Removes color cast from subject edges (0-100)

**Edge Refinement Tools:**
- **Edge Thickness**: Thin or thicken matte edges (-10 to 10)
- **Edge Feather**: Soften edge transitions (0-50)
- **Choke**: Shrink or expand the matte (-10 to 10)
- **Matte Cleanup**: Remove noise from semi-transparent pixels (0-100)

**Quick Presets:**
- **Studio**: Optimized for well-lit studio green screens
- **Natural Light**: For outdoor or natural lighting conditions
- **Low Light**: For darker or poorly lit backgrounds
- **Uneven Lighting**: For inconsistently lit backgrounds
- **Blue Screen**: Optimized for blue screen keying

### 2. Multi-Layer Compositing

Stack multiple video layers with blend modes and transform controls.

**Blend Modes:**
- Normal
- Multiply
- Screen
- Overlay
- Add

**Transform Controls (with Keyframe Support):**
- Position X/Y (-1000 to 1000)
- Scale X/Y (0.1 to 5.0)
- Rotation (-360 to 360 degrees)
- Opacity (0-100%)

### 3. Matte Preview

Split-view visualization showing:
- **Original**: Source video before keying
- **Matte**: Alpha channel visualization (black = transparent, white = opaque)
- **Composite**: Final keyed result with background

### 4. Motion Tracking

Track objects in video to lock graphics and effects to movement.

**Features:**
- Add tracking points by clicking on preview
- Automatic frame-by-frame tracking
- Interpolated position data for smooth animation
- Export/import tracking data
- Multiple tracking points per video

## Usage

### Basic Chroma Key Workflow

1. Add the Chroma Key effect to your video clip
2. Select the key color (green or blue)
3. Adjust Similarity until the background disappears
4. Fine-tune Smoothness for natural edges
5. Use Spill Suppression to remove color cast
6. Apply edge refinement tools as needed
7. Use presets for quick starting points

### Layer Compositing Workflow

1. Create multiple video layers
2. Apply Chroma Key to foreground layers
3. Set blend modes for creative effects
4. Adjust opacity for transparency
5. Use transform controls to position layers
6. Add keyframes to animate transforms over time

### Motion Tracking Workflow

1. Add a tracking point with a descriptive name
2. Click on the preview to place the tracking point
3. Play the video to track the point through frames
4. Use tracked position data to lock graphics/effects
5. Export tracking data for reuse

## Technical Implementation

### Services

- **chromaKeyService.ts**: Core chroma keying algorithms
  - CPU-based implementation using canvas ImageData
  - WebGL-accelerated implementation for performance
  - Edge refinement algorithms
  - Spill suppression color correction

- **motionTrackingService.ts**: Motion tracking system
  - Template matching for point tracking
  - Confidence scoring
  - Position interpolation
  - Export/import functionality

### Components

- **ChromaKeyEffect.tsx**: Chroma key controls UI
- **LayerStack.tsx**: Multi-layer management
- **MattePreview.tsx**: Split-view preview
- **MotionTracking.tsx**: Tracking controls
- **CompositingPanel.tsx**: Integrated panel

### Effects Engine

The effects engine (`effectsEngine.ts`) has been extended to support:
- Chroma key processing
- Blend mode compositing
- Integration with existing keyframe animation system

## Performance Considerations

- WebGL acceleration is used when available for real-time keying
- CPU fallback ensures compatibility with all environments
- Edge refinement operations are optimized for minimal latency
- Motion tracking uses efficient template matching algorithms

## Future Enhancements

- Advanced motion tracking using feature detection (SIFT, SURF)
- Real-time preview with hardware acceleration
- 3D camera tracking
- Planar tracking for screen replacement
- Multi-point tracking for object deformation
