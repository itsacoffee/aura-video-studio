# Advanced Mode - Visual Guide

This guide shows what the Advanced Mode feature looks like in the UI.

## Settings Page - General Tab

### Before Enabling Advanced Mode

**General Settings Section:**
```
┌─────────────────────────────────────────────────────────────┐
│ General Settings                                            │
│ Configure basic application behavior and preferences       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ [Default Project Save Location]                            │
│ [Autosave Settings]                                         │
│ [Language & Locale]                                         │
│ [Theme]                                                     │
│ [Startup Behavior]                                          │
│ [✓ Check for Updates on Startup]                           │
│                                                             │
│ ─────────────────────────────────────────────────────────  │
│                                                             │
│ Advanced Mode                           [ℹ️] What's included│
│ Enable expert features for advanced users                  │
│ [○ Disabled]                                                │
│                                                             │
│ [💾 Save General Settings]                                  │
└─────────────────────────────────────────────────────────────┘
```

**Info Popover (when clicking ℹ️):**
```
┌──────────────────────────────┐
│ Advanced Mode Features       │
├──────────────────────────────┤
│ • ML retraining for frame    │
│   importance                 │
│ • Deep prompt customization  │
│   and internals              │
│ • Low-level render flags     │
│   and optimization           │
│ • Chroma key compositing     │
│   controls                   │
│ • Motion graphics recipes    │
│   and templates              │
│ • Expert provider tuning     │
│   and configuration          │
└──────────────────────────────┘
```

### After Enabling Advanced Mode

**Warning Banner appears at top of Settings:**
```
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️  Advanced Mode Active                                        │
│                                                                 │
│ You have enabled Advanced Mode, which reveals expert features  │
│ that may require technical knowledge. These include ML          │
│ retraining controls, deep prompt customization, low-level      │
│ render flags, chroma key compositing, motion graphics recipes, │
│ and expert provider tuning. Learn more about Advanced Mode →   │
│                                                                 │
│                         [Revert to Simple Mode]                 │
└─────────────────────────────────────────────────────────────────┘
```

**General Settings with Advanced Mode Enabled:**
```
┌─────────────────────────────────────────────────────────────┐
│ General Settings                                            │
│ Configure basic application behavior and preferences       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ [Default Project Save Location]                            │
│ [Autosave Settings]                                         │
│ [Language & Locale]                                         │
│ [Theme]                                                     │
│ [Startup Behavior]                                          │
│ [✓ Check for Updates on Startup]                           │
│                                                             │
│ ─────────────────────────────────────────────────────────  │
│                                                             │
│ Advanced Mode                           [ℹ️] What's included│
│ Enable expert features for advanced users                  │
│ [● Enabled]                          ← Toggle is now ON    │
│                                                             │
│ [💾 Save General Settings]                                  │
└─────────────────────────────────────────────────────────────┘
```

## Navigation Sidebar

### Simple Mode (Default) - 10 Items Visible

```
┌──────────────────────┐
│  🎬 Aura Studio      │
├──────────────────────┤
│  🏠 Welcome          │
│  📄 Dashboard        │
│  💡 Ideation         │
│  📈 Trending Topics  │
│  📅 Content Planning │
│  🎬 Create           │
│  📁 Projects         │
│  🖼️  Asset Library    │
│  ▶️  Render          │
│  ⚙️  Settings        │
│                      │
│  [Theme Toggle]      │
└──────────────────────┘
```

### Advanced Mode Enabled - 15 Items Visible

```
┌──────────────────────┐
│  🎬 Aura Studio      │
├──────────────────────┤
│  🏠 Welcome          │
│  📄 Dashboard        │
│  💡 Ideation         │
│  📈 Trending Topics  │
│  📅 Content Planning │
│  🎬 Create           │
│  📁 Projects         │
│  🖼️  Asset Library    │
│  ⚡ Pacing Analyzer   │  ← NEW: Advanced only
│  ▶️  Render          │
│  🪄 AI Editing       │  ← NEW: Advanced only
│  👁️  Visual Aesthetics│ ← NEW: Advanced only
│  📝 Prompt Management│  ← NEW: Advanced only
│  📊 Performance      │  ← NEW: Advanced only
│  ⚙️  Settings        │
│                      │
│  [Theme Toggle]      │
└──────────────────────┘
```

## Diagnostics API Response

### Before Enabling Advanced Mode

**GET /api/diagnostics/json**
```json
{
  "timestamp": "2024-11-03T22:50:00.000Z",
  "advancedMode": false,
  "advancedFeaturesNote": "Advanced features are disabled. Enable Advanced Mode in Settings > General to access expert features.",
  "systemProfile": {
    "tier": "B",
    "cpu": { "physical": 4, "logical": 8 },
    "ram": { "gb": 16 },
    "gpu": { "vendor": "NVIDIA", "model": "RTX 3060", "vramGB": 12 }
  },
  "environment": {
    "os": "Windows 11",
    "dotnetVersion": "8.0.0"
  }
}
```

### After Enabling Advanced Mode

**GET /api/diagnostics/json**
```json
{
  "timestamp": "2024-11-03T22:50:00.000Z",
  "advancedMode": true,
  "advancedFeaturesNote": "Advanced features are enabled",
  "systemProfile": {
    "tier": "B",
    "cpu": { "physical": 4, "logical": 8 },
    "ram": { "gb": 16 },
    "gpu": { "vendor": "NVIDIA", "model": "RTX 3060", "vramGB": 12 }
  },
  "environment": {
    "os": "Windows 11",
    "dotnetVersion": "8.0.0"
  }
}
```

## User Settings JSON

**File**: `%LOCALAPPDATA%\Aura\user-settings.json`

### Default Settings (Simple Mode)
```json
{
  "general": {
    "defaultProjectSaveLocation": "",
    "autosaveIntervalSeconds": 300,
    "autosaveEnabled": true,
    "language": "en-US",
    "locale": "en-US",
    "theme": "Auto",
    "startupBehavior": "ShowDashboard",
    "checkForUpdatesOnStartup": true,
    "advancedModeEnabled": false  ← Default is OFF
  },
  "apiKeys": { },
  "fileLocations": { },
  "videoDefaults": { },
  "editorPreferences": { },
  "ui": { },
  "advanced": { },
  "version": "1.0.0",
  "lastUpdated": "2024-11-03T22:45:00.000Z"
}
```

### With Advanced Mode Enabled
```json
{
  "general": {
    "defaultProjectSaveLocation": "",
    "autosaveIntervalSeconds": 300,
    "autosaveEnabled": true,
    "language": "en-US",
    "locale": "en-US",
    "theme": "Auto",
    "startupBehavior": "ShowDashboard",
    "checkForUpdatesOnStartup": true,
    "advancedModeEnabled": true  ← Changed to ON
  },
  "apiKeys": { },
  "fileLocations": { },
  "videoDefaults": { },
  "editorPreferences": { },
  "ui": { },
  "advanced": { },
  "version": "1.0.0",
  "lastUpdated": "2024-11-03T22:47:00.000Z"
}
```

## State Flow Diagram

```
┌─────────────────┐
│  User Opens     │
│  Application    │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ Load User Settings      │
│ advancedModeEnabled: ?  │
└────────┬────────────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
 false      true
    │         │
    ▼         ▼
┌────────┐ ┌──────────────┐
│ Simple │ │   Advanced   │
│  Mode  │ │     Mode     │
└────┬───┘ └───┬──────────┘
     │         │
     │         ├─► Show Warning Banner
     │         │
     ├─────────┼─► Filter Navigation Items
     │         │   (10 vs 15 items)
     │         │
     └─────────┴─► Render UI
```

## Quick Revert Flow

```
User Clicks "Revert to Simple Mode" Button
         │
         ▼
┌──────────────────────┐
│ setAdvancedMode(false)│
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ Save to Backend &    │
│ Update Local State   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ Banner Disappears    │
│ Navigation Shrinks   │
│ (No reload needed)   │
└──────────────────────┘
```

## Key Visual Features

1. **Warning Banner**: High-visibility yellow/orange color scheme to draw attention
2. **Info Icon**: Blue "i" icon next to toggle for additional context
3. **Popover**: Detailed list of features that unlock when hovering/clicking info icon
4. **Toggle Switch**: Standard Fluent UI switch component for on/off state
5. **Navigation Animation**: Smooth transition when items appear/disappear
6. **Save Button**: Highlighted when unsaved changes detected

## Accessibility Features

- **Keyboard Navigation**: Tab through all controls, Enter to activate
- **Screen Reader**: All elements have proper ARIA labels
- **Focus Indicators**: Clear visual focus outline on all interactive elements
- **Color Contrast**: WCAG 2.1 Level AA compliant
- **Tooltips**: Available on hover for all navigation items

---

**Visual Design**: Consistent with Fluent UI design system  
**Animations**: Subtle fade-in/out transitions (200-400ms)  
**Responsive**: Works on all screen sizes (tested 1024px-4K)
