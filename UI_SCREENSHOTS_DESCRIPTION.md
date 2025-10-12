# UI Changes - Visual Description

## Overview

This document describes the visual changes made to the Engines UI to support flexible engine installs and better display of engine instances.

## 1. Engine Instances Section (New/Enhanced)

### Before
- Basic list of instances with minimal information
- No clear indication of Managed vs External mode
- No way to copy paths
- Actions were in a dropdown menu

### After

```
╔══════════════════════════════════════════════════════════════════════════╗
║                          Engine Instances                                ║
║  Manage your engine installations (both app-managed and external)        ║
╚══════════════════════════════════════════════════════════════════════════╝

┌────────────────────────────────────────────────────────────────────────┐
│ FFmpeg [Managed ⓘ] [installed]                  [Open Folder] [Open UI]│
│                                                                          │
│ Path: C:\Users\user\AppData\Local\Aura\Tools\ffmpeg      [📋 Copy]    │
│ Executable: C:\Users\...\ffmpeg\bin\ffmpeg.exe           [📋 Copy]    │
└────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────┐
│ Stable Diffusion WebUI [External ⓘ] [running] [Healthy] [Open Folder] │
│                                                  [Open Web UI]           │
│ Path: C:\Tools\stable-diffusion-webui                     [📋 Copy]    │
│ Port: 7860                                                [📋 Copy]    │
│ Executable: C:\Tools\stable-diffusion-webui\webui.bat     [📋 Copy]    │
│ Notes: My production SD setup with custom models                        │
└────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────┐
│ Ollama [External ⓘ] [running] [Healthy]         [Open Folder] [Open UI]│
│                                                                          │
│ Path: C:\Program Files\Ollama                             [📋 Copy]    │
│ Port: 11434                                               [📋 Copy]    │
└────────────────────────────────────────────────────────────────────────┘
```

## 2. Mode Badge Tooltips

### Managed Badge (Blue)
**Hover shows tooltip:**
```
┌──────────────────────────────────────┐
│ Managed                               │
│ ─────────────────────────────────────│
│ App controls start/stop/process       │
└──────────────────────────────────────┘
```

### External Badge (Green)
**Hover shows tooltip:**
```
┌──────────────────────────────────────┐
│ External                              │
│ ─────────────────────────────────────│
│ You run it; app only detects/uses it │
└──────────────────────────────────────┘
```

## 3. Copy-to-Clipboard Feature

When hovering over paths/ports, a copy icon appears:
```
Path: C:\Tools\ffmpeg\bin  [📋]
                            ↑
                        Copy icon
```

Click behavior:
1. Click copy icon
2. Icon changes or tooltip shows "Copied!"
3. After 2 seconds, reverts to normal

## 4. Engine Card - Attach Button

### Before
```
┌─────────────────────────────────────────────┐
│ Stable Diffusion WebUI                      │
│ Version: 1.7.0 • Size: 4.2 GB              │
│                                              │
│ [Install]  [Attach Existing Install]        │
└─────────────────────────────────────────────┘
```

### After
```
┌─────────────────────────────────────────────┐
│ Stable Diffusion WebUI                      │
│ Version: 1.7.0 • Size: 4.2 GB              │
│                                              │
│ [Install]  or  [Attach Existing Install]    │
└─────────────────────────────────────────────┘
```

The "or" makes it clearer that these are two alternative paths.

## 5. Attach Engine Dialog

```
╔════════════════════════════════════════════════╗
║  Attach Existing Stable Diffusion WebUI       ║
╠════════════════════════════════════════════════╣
║                                                ║
║  Install Path (required) *                     ║
║  ┌──────────────────────────────────────────┐ ║
║  │ e.g., C:\Tools\sd-webui                  │ ║
║  └──────────────────────────────────────────┘ ║
║  Absolute path to the installation directory   ║
║                                                ║
║  Executable Path (optional)                    ║
║  ┌──────────────────────────────────────────┐ ║
║  │ e.g., webui.bat or python main.py       │ ║
║  └──────────────────────────────────────────┘ ║
║  Path to the main executable or start script   ║
║                                                ║
║  Port (optional)                               ║
║  ┌──────────────────────────────────────────┐ ║
║  │ e.g., 7860                               │ ║
║  └──────────────────────────────────────────┘ ║
║  Web UI port number                            ║
║                                                ║
║  Health Check URL (optional)                   ║
║  ┌──────────────────────────────────────────┐ ║
║  │ e.g., http://localhost:7860/health       │ ║
║  └──────────────────────────────────────────┘ ║
║  URL endpoint for health checks                ║
║                                                ║
║  Notes (optional)                              ║
║  ┌──────────────────────────────────────────┐ ║
║  │ e.g., Custom installation with models    │ ║
║  │                                          │ ║
║  └──────────────────────────────────────────┘ ║
║                                                ║
║              [Cancel]        [Attach]          ║
╚════════════════════════════════════════════════╝
```

## 6. Color Scheme

### Mode Badges
- **Managed**: Blue/Brand color (`colorBrandForeground1`)
- **External**: Green/Success color (`colorSuccessForeground1`)

### Status Badges
- **Running**: Green with success color
- **Installed**: Blue with informative color
- **Not Installed**: Gray with subtle color

### Health Badges
- **Healthy**: Green filled badge (only shown when running)

### Copy Buttons
- Default: Neutral color
- Hover: Brand color
- Clicked: Shows "Copied!" feedback

## 7. Layout Improvements

### Instance Cards
```
┌─────────────────────────────────────────────────────────────┐
│ [Header Row - Name + Badges]                    [Actions]   │
│ ─────────────────────────────────────────────────────────── │
│ [Path info with copy buttons]                               │
│ [Port info with copy button]                                │
│ [Executable info with copy button]                          │
│ [Notes if present]                                          │
└─────────────────────────────────────────────────────────────┘
```

Header row wraps if needed to accommodate all badges.

Actions are aligned to the right for easy access.

### Monospace Paths
All paths, ports, and executables use monospace font for:
- Easy reading
- Clear distinction from regular text
- Professional appearance
- Better copy/paste behavior

## 8. Responsive Design

The layout adapts to screen size:
- Mobile: Badges stack vertically, actions move below header
- Tablet: Badges wrap, actions stay to the right
- Desktop: Full horizontal layout with all elements visible

## 9. Accessibility

- All badges have hover tooltips explaining their meaning
- Copy buttons have clear "Copy to clipboard" and "Copied!" states
- Keyboard navigation works for all interactive elements
- Screen readers announce badge meanings and copy status
- High contrast mode supported via Fluent UI tokens

## Key Visual Principles

1. **Clarity**: Mode badges immediately show whether Aura controls the engine
2. **Efficiency**: Copy buttons enable quick path copying for troubleshooting
3. **Discoverability**: Tooltips explain features without cluttering the UI
4. **Consistency**: Uses Fluent UI design system throughout
5. **Information Density**: Shows all relevant info without overwhelming users

## Summary

The new UI makes it immediately clear:
- ✅ Which engines are installed and where
- ✅ Whether Aura manages them or you do
- ✅ Current status (installed/running/healthy)
- ✅ How to access them (Open Folder/Web UI)
- ✅ How to copy paths for configuration/troubleshooting

The result is a more transparent, user-friendly engine management experience that works for both beginners (Managed mode) and advanced users (External mode).
