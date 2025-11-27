# Context Menu System Documentation

## Overview

Aura Video Studio includes a comprehensive context menu system built on Electron's native menu API. Context menus are available throughout the application for enhanced productivity.

## Architecture

### Components

1. **Electron Main Process** - Menu builder and IPC handlers
2. **Electron Preload** - Bridge between main and renderer processes
3. **React Hooks** - Easy integration into components
4. **Type Definitions** - Full TypeScript support

### Flow Diagram

```
User Right-Click -> React Component -> useContextMenu Hook -> IPC Call ->
Electron Main Process -> Menu Builder -> Native OS Menu -> User Selection ->
IPC Callback -> React Handler -> State Update
```

### File Locations

| Component | Location |
|-----------|----------|
| Type Definitions | `Aura.Desktop/electron/context-menu-types.ts` |
| Menu Builder | `Aura.Desktop/electron/context-menu-builder.js` |
| IPC Handler | `Aura.Desktop/electron/ipc-handlers/context-menu-handler.js` |
| Preload Script | `Aura.Desktop/electron/preload.js` |
| React Hook | `Aura.Web/src/hooks/useContextMenu.ts` |
| Frontend Types | `Aura.Web/src/types/electron-context-menu.ts` |

## Available Context Menus

### 1. Timeline Clip Menu

**Trigger:** Right-click on any clip in the timeline

**Options:**

| Label | Accelerator | Action |
|-------|-------------|--------|
| Cut | Ctrl+X | `onCut` |
| Copy | Ctrl+C | `onCopy` |
| Paste | Ctrl+V | `onPaste` |
| Duplicate | Ctrl+D | `onDuplicate` |
| Split at Playhead | S | `onSplit` |
| Delete | Delete | `onDelete` |
| Ripple Delete | Shift+Delete | `onRippleDelete` |
| Properties | Ctrl+I | `onProperties` |

**Data Interface:**

```typescript
interface TimelineClipMenuData {
  clipId: string;
  clipType: 'video' | 'audio' | 'image';
  startTime: number;
  duration: number;
  trackId: string;
  isLocked: boolean;
  hasAudio: boolean;
  hasClipboardData?: boolean;
}
```

### 2. Timeline Track Menu

**Trigger:** Right-click on track header

**Options:**

| Label | Type | Action |
|-------|------|--------|
| Add Track Above | normal | `onAddTrack` (with 'above') |
| Add Track Below | normal | `onAddTrack` (with 'below') |
| Lock Track | checkbox | `onToggleLock` |
| Mute Track | checkbox | `onToggleMute` |
| Solo Track | checkbox | `onToggleSolo` |
| Rename Track | normal | `onRename` |
| Delete Track | normal | `onDelete` |

**Data Interface:**

```typescript
interface TimelineTrackMenuData {
  trackId: string;
  trackType: 'video' | 'audio' | 'overlay';
  isLocked: boolean;
  isMuted: boolean;
  isSolo: boolean;
  trackIndex: number;
  totalTracks: number;
}
```

### 3. Timeline Empty Space Menu

**Trigger:** Right-click on empty timeline area

**Options:**

| Label | Accelerator | Action |
|-------|-------------|--------|
| Paste | Ctrl+V | `onPaste` |
| Add Marker | M | `onAddMarker` |
| Select All Clips | Ctrl+A | `onSelectAll` |

**Data Interface:**

```typescript
interface TimelineEmptyMenuData {
  timePosition: number;
  trackId?: string;
  hasClipboardData: boolean;
}
```

### 4. Media Asset Menu

**Trigger:** Right-click on asset in media library

**Options:**

| Label | Accelerator | Type | Action |
|-------|-------------|------|--------|
| Add to Timeline | - | normal | `onAddToTimeline` |
| Preview | - | normal | `onPreview` |
| Rename | F2 | normal | `onRename` |
| Add to Favorites | - | checkbox | `onToggleFavorite` |
| Reveal in File Explorer | Ctrl+R | normal | `onRevealInOS` |
| Properties | - | normal | `onProperties` |
| Delete from Library | - | normal | `onDelete` |

**Data Interface:**

```typescript
interface MediaAssetMenuData {
  assetId: string;
  assetType: 'video' | 'audio' | 'image';
  filePath: string;
  isFavorite: boolean;
  tags: string[];
}
```

### 5. AI Script Menu

**Trigger:** Right-click on generated script scene

**Options:**

| Label | Accelerator | Action |
|-------|-------------|--------|
| Regenerate This Scene | - | `onRegenerate` |
| Expand Section | - | `onExpand` |
| Shorten Section | - | `onShorten` |
| Generate B-Roll Suggestions | - | `onGenerateBRoll` |
| Copy Text | Ctrl+C | `onCopyText` |

**Data Interface:**

```typescript
interface AIScriptMenuData {
  sceneIndex: number;
  sceneText: string;
  jobId: string;
}
```

### 6. Job Queue Menu

**Trigger:** Right-click on job in queue

**Options:**

| Label | Enabled When | Action |
|-------|--------------|--------|
| Pause Job | Running | `onPause` |
| Resume Job | Paused | `onResume` |
| Cancel Job | Running/Paused/Queued | `onCancel` |
| View Logs | Always | `onViewLogs` |
| Retry Job | Failed | `onRetry` |
| Open Output File | Completed with output | `onOpenOutput` |
| Reveal Output in Explorer | Completed with output | `onRevealOutput` |

**Data Interface:**

```typescript
interface JobQueueMenuData {
  jobId: string;
  status: 'queued' | 'running' | 'paused' | 'completed' | 'failed' | 'canceled';
  outputPath?: string;
}
```

### 7. Preview Window Menu

**Trigger:** Right-click on video preview

**Options:**

| Label | Accelerator | Action |
|-------|-------------|--------|
| Play/Pause | Space | `onTogglePlayback` |
| Add Marker at Current Frame | M | `onAddMarker` |
| Export Frame as Image | - | `onExportFrame` |
| Zoom (submenu) | - | `onSetZoom` |

**Zoom Submenu:**
- Fit to Window
- 50%
- 100%
- 200%

**Data Interface:**

```typescript
interface PreviewWindowMenuData {
  currentTime: number;
  duration: number;
  isPlaying: boolean;
  zoom: number;
}
```

### 8. AI Provider Menu

**Trigger:** Right-click on provider in settings

**Options:**

| Label | Type | Action |
|-------|------|--------|
| Test Connection | normal | `onTestConnection` |
| View Usage Stats | normal | `onViewStats` |
| Set as Default | checkbox | `onSetDefault` |
| Configure | normal | `onConfigure` |

**Data Interface:**

```typescript
interface AIProviderMenuData {
  providerId: string;
  providerType: 'llm' | 'tts' | 'image';
  isDefault: boolean;
  hasFallback: boolean;
}
```

## Usage in Components

### Basic Example

```tsx
import { useContextMenu, useContextMenuAction } from '@/hooks/useContextMenu';

function MyComponent() {
  const showContextMenu = useContextMenu('timeline-clip');

  const handleContextMenu = (e: React.MouseEvent) => {
    const menuData = {
      clipId: 'clip-123',
      clipType: 'video',
    };
    showContextMenu(e, menuData);
  };

  useContextMenuAction('timeline-clip', 'onDelete', (data) => {
    console.log('Delete clip:', data.clipId);
  });

  return (
    <div onContextMenu={handleContextMenu}>
      Right-click me!
    </div>
  );
}
```

### Advanced Example with Multiple Actions

```tsx
function TimelineClip({ clip }) {
  const showContextMenu = useContextMenu('timeline-clip');

  useContextMenuAction('timeline-clip', 'onCut', handleCut);
  useContextMenuAction('timeline-clip', 'onCopy', handleCopy);
  useContextMenuAction('timeline-clip', 'onPaste', handlePaste);
  useContextMenuAction('timeline-clip', 'onDuplicate', handleDuplicate);
  useContextMenuAction('timeline-clip', 'onSplit', handleSplit);
  useContextMenuAction('timeline-clip', 'onDelete', handleDelete);
  useContextMenuAction('timeline-clip', 'onRippleDelete', handleRippleDelete);
  useContextMenuAction('timeline-clip', 'onProperties', handleProperties);

  const handleContextMenu = (e: React.MouseEvent) => {
    showContextMenu(e, {
      clipId: clip.id,
      clipType: clip.type,
      startTime: clip.startTime,
      duration: clip.duration,
      trackId: clip.trackId,
      isLocked: clip.isLocked,
      hasAudio: clip.hasAudio
    });
  };

  return <div onContextMenu={handleContextMenu}>{/* content */}</div>;
}
```

### Using Specialized Hooks

For common use cases, specialized hooks are provided:

```tsx
import { useJobQueueContextMenu } from '@/hooks/useJobQueueContextMenu';

function JobQueueItem({ job }) {
  const handleContextMenu = useJobQueueContextMenu({
    onPause: (jobId) => pauseJob(jobId),
    onResume: (jobId) => resumeJob(jobId),
    onCancel: (jobId) => cancelJob(jobId),
    onViewLogs: (jobId) => openLogs(jobId),
    onRetry: (jobId) => retryJob(jobId),
  });

  return (
    <div onContextMenu={(e) => handleContextMenu(e, job)}>
      {job.name}
    </div>
  );
}
```

## Keyboard Shortcuts

All context menu accelerators also work as keyboard shortcuts:

| Shortcut | Action |
|----------|--------|
| Ctrl+X | Cut |
| Ctrl+C | Copy |
| Ctrl+V | Paste |
| Ctrl+D | Duplicate |
| S | Split at Playhead |
| M | Add Marker |
| Delete | Delete |
| Shift+Delete | Ripple Delete |
| F2 | Rename |
| Ctrl+I | Properties |
| Ctrl+A | Select All |
| Space | Play/Pause |

## Adding New Context Menus

### Step 1: Add Type Definition

In `Aura.Desktop/electron/context-menu-types.ts`:

```typescript
export type ContextMenuType =
  | 'existing-types'
  | 'my-new-menu';

export interface MyNewMenuData {
  itemId: string;
  // Add other properties as needed
}
```

### Step 2: Add Menu Builder

In `Aura.Desktop/electron/context-menu-builder.js`:

```javascript
buildMyNewMenu(data, callbacks) {
  const template = [
    {
      label: 'My Action',
      click: () => callbacks.onMyAction?.(data)
    }
  ];
  return Menu.buildFromTemplate(template);
}
```

Update the `build` method switch statement:

```javascript
case 'my-new-menu':
  return this.buildMyNewMenu(data, callbacks);
```

### Step 3: Update IPC Handler

In `Aura.Desktop/electron/ipc-handlers/context-menu-handler.js`:

```javascript
const ACTION_MAP = {
  // ... existing actions
  'my-new-menu': ['onMyAction'],
};
```

### Step 4: Update Frontend Types

In `Aura.Web/src/types/electron-context-menu.ts`:

```typescript
export type ContextMenuType =
  | 'existing-types'
  | 'my-new-menu';

export interface MyNewMenuData {
  itemId: string;
}
```

### Step 5: Create React Hook (Optional)

```typescript
export function useMyNewContextMenu(onMyAction: (data: MyNewMenuData) => void) {
  const showContextMenu = useContextMenu<MyNewMenuData>('my-new-menu');
  
  useContextMenuAction('my-new-menu', 'onMyAction', onMyAction);
  
  return showContextMenu;
}
```

### Step 6: Use in Component

```tsx
const handleContextMenu = useMyNewContextMenu((data) => {
  // Handle action
});

return (
  <div onContextMenu={(e) => handleContextMenu(e, { itemId: 'item-1' })}>
    Right-click me
  </div>
);
```

## Troubleshooting

### Menu Not Appearing

1. Verify `window.electron.contextMenu` is available
2. Check console for IPC errors
3. Ensure menu type is registered in builder
4. Confirm preload script is loading correctly

### Actions Not Firing

1. Verify action type is listed in `ACTION_MAP` in context-menu-handler.js
2. Check IPC channel names match exactly
3. Ensure `useContextMenuAction` is called before render
4. Check that callbacks are properly memoized

### Wrong Menu Appearing

1. Check menu type string matches exactly
2. Verify `event.stopPropagation()` on nested elements
3. Ensure data passed to `showContextMenu` is correct

### Menu Items Always Disabled

1. Check the `enabled` property in menu template
2. Verify data properties are being passed correctly
3. Review conditional logic in builder methods

## Performance Considerations

- **Lazy Loading:** Context menus are only built when shown
- **IPC Async:** IPC calls are async - handle promise rejections
- **Memoization:** Use `React.useCallback` for handlers to prevent re-renders
- **Data Preparation:** Avoid heavy computations in menu data preparation

```tsx
// Good - memoized handler
const handleContextMenu = useCallback((e) => {
  showMenu(e, data);
}, [showMenu, data]);

// Avoid - recreates on every render
const handleContextMenu = (e) => showMenu(e, data);
```

## Security Considerations

- File system operations are restricted to Electron main process
- API keys in provider menus should never be logged
- Path validation is performed server-side
- User confirmation required for destructive operations
- Context menu actions are validated against allowed action types

## Browser Compatibility

Context menus only work in Electron desktop app. For web version:

```tsx
function MyComponent() {
  if (!window.electron?.contextMenu) {
    // Fallback for web - use custom HTML/CSS context menu
    return <WebContextMenuFallback />;
  }
  
  // Desktop - use native Electron context menu
  return <DesktopContextMenu />;
}
```

## Testing

### Running Tests

```bash
# Unit tests for menu builder
npm run test:context-menu

# All context menu related tests
npm run test:all-context-menus
```

### Test Coverage

The context menu system includes tests for:
- Menu creation for all 8 menu types
- Callback invocation
- Checkbox state management
- Enabled/disabled state management
- Submenu structure
- Unknown menu type fallback

See `Aura.Desktop/test/test-context-menu-system.js` for test implementation.
