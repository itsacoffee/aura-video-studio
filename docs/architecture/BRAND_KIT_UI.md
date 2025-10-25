# Brand Kit UI Component

## Overview
The BrandKitPanel component provides a user-friendly interface for configuring visual branding in videos.

## Location
`Aura.Web/src/components/BrandKitPanel.tsx`

## Features

### 1. Watermark Configuration
- **Image Path**: Input field for watermark image path (PNG/SVG recommended)
- **Position**: Dropdown with 5 options:
  - Top Left
  - Top Right
  - Bottom Left
  - Bottom Right
  - Center
- **Opacity**: Slider control (0-100%)

### 2. Brand Colors
- **Brand Color**: Primary color input with hex format (#FF6B35)
  - Live color preview box
  - Used for subtle video overlays (5% opacity)
- **Accent Color**: Secondary color input (#00D9FF)
  - Live color preview box
  - For highlights and text elements

### 3. Controls
- **Reset to Defaults**: Button to restore default settings

## Component Interface

```typescript
export interface BrandKitSettings {
  watermarkPath?: string;
  watermarkPosition?: string;
  watermarkOpacity: number;
  brandColor?: string;
  accentColor?: string;
}

interface BrandKitPanelProps {
  settings: BrandKitSettings;
  onSettingsChange: (settings: BrandKitSettings) => void;
}
```

## Usage Example

```tsx
import { BrandKitPanel, BrandKitSettings } from './components/BrandKitPanel';

function MyApp() {
  const [brandSettings, setBrandSettings] = useState<BrandKitSettings>({
    watermarkPath: '/path/to/logo.png',
    watermarkPosition: 'bottom-right',
    watermarkOpacity: 0.8,
    brandColor: '#FF6B35',
    accentColor: '#00D9FF'
  });

  return (
    <BrandKitPanel
      settings={brandSettings}
      onSettingsChange={setBrandSettings}
    />
  );
}
```

## Visual Layout

```
┌─────────────────────────────────────────┐
│ Brand Kit                                │
│ Customize your videos with watermarks,  │
│ colors, and branding.                    │
├─────────────────────────────────────────┤
│ ┌─────────────────────────────────────┐ │
│ │ Watermark Image                     │ │
│ │ [Path to watermark image (PNG/SVG)] │ │
│ │ Recommended: Transparent PNG...     │ │
│ │                                     │ │
│ │ Watermark Position                  │ │
│ │ [▼ Bottom Right                  ]  │ │
│ │                                     │ │
│ │ Watermark Opacity                   │ │
│ │ [━━━━━━━●━━━━━━━━━━━━━]          │ │
│ │ 80%                                 │ │
│ │                                     │ │
│ │ Brand Color                         │ │
│ │ [#FF6B35                         ]  │ │
│ │ ┌─────────────────────────────────┐ │ │
│ │ │ [Orange color preview]          │ │ │
│ │ └─────────────────────────────────┘ │ │
│ │ Primary brand color for overlays    │ │
│ │                                     │ │
│ │ Accent Color                        │ │
│ │ [#00D9FF                         ]  │ │
│ │ ┌─────────────────────────────────┐ │ │
│ │ │ [Cyan color preview]            │ │ │
│ │ └─────────────────────────────────┘ │ │
│ │ Secondary color for highlights      │ │
│ │                                     │ │
│ │ [Reset to Defaults]                 │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

## Integration with Video Pipeline

The settings from this component are passed to the FFmpeg rendering pipeline:

1. **BrandKit Model**: Settings map to the `BrandKit` record in `Aura.Core/Models/Models.cs`
2. **FFmpeg Filters**: Applied via `FFmpegPlanBuilder.BuildFilterGraph()`
   - Watermark: Overlay filter with specified position and opacity
   - Brand Color: Drawbox filter with 5% opacity
   - Ken Burns: Optional zoompan filter for still images

## Styling

The component uses Fluent UI design tokens for consistent styling:
- Spacing: `tokens.spacingVerticalL`, `tokens.spacingVerticalM`
- Colors: `tokens.colorNeutralForeground3`, `tokens.colorNeutralStroke1`
- Typography: `Title3`, `Label`, `Text` components
- Borders: `tokens.borderRadiusMedium`

## Accessibility

- All inputs have associated labels
- Slider has readable value display
- Color previews provide visual feedback
- Help text explains expected formats
