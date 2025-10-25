# Keyboard Shortcuts System

## Overview

The Aura Video Studio application implements a comprehensive keyboard shortcuts system that provides:

- **Context-aware shortcuts**: Different shortcuts for different pages (Video Editor, Timeline, Create, etc.)
- **No conflicts**: Shortcuts are scoped to their context, preventing conflicts
- **Discoverable**: Users can press `Ctrl+K` or `?` to see all available shortcuts
- **Searchable**: The shortcuts panel includes search/filter functionality
- **Customizable**: Users can customize key bindings in Settings
- **Visual feedback**: Tooltips can display keyboard shortcuts for buttons

## Architecture

### Components

1. **KeyboardShortcutManager** (`src/services/keyboardShortcutManager.ts`)
   - Singleton service managing all keyboard shortcuts
   - Handles registration, context switching, and event handling
   - Supports custom key mappings from localStorage

2. **KeyboardShortcutsPanel** (`src/components/KeyboardShortcuts/KeyboardShortcutsPanel.tsx`)
   - Modal panel showing all shortcuts organized by context
   - Includes search functionality
   - Accordion UI grouped by category

3. **Enhanced Tooltip** (`src/components/Tooltip.tsx`)
   - Optional `shortcut` prop to display keyboard shortcuts
   - Consistent styling across the application

### Contexts

The system supports the following contexts:

- `global`: Always active shortcuts (e.g., Ctrl+S, Ctrl+N, Ctrl+O)
- `video-editor`: Video Editor page shortcuts (J/K/L shuttle, I/O points, tools)
- `timeline`: Timeline Editor shortcuts (split, zoom, delete)
- `create`: Create workflow shortcuts (Ctrl+Enter, Escape)
- `settings`: Settings page shortcuts

## Usage

### Registering Shortcuts

In a page component, register shortcuts in a `useEffect`:

```typescript
import { useEffect } from 'react';
import { keyboardShortcutManager } from '../services/keyboardShortcutManager';

export function MyPage() {
  useEffect(() => {
    // Set the active context
    keyboardShortcutManager.setActiveContext('my-context');

    // Register shortcuts
    keyboardShortcutManager.registerMultiple([
      {
        id: 'my-action',
        keys: 'Ctrl+X',
        description: 'Do something',
        context: 'my-context',
        handler: () => {
          // Your handler code
        },
      },
      // ... more shortcuts
    ]);

    // Clean up on unmount
    return () => {
      keyboardShortcutManager.unregisterContext('my-context');
    };
  }, []);

  return <div>My Page Content</div>;
}
```

### Using Tooltips with Shortcuts

```typescript
import { Tooltip } from '../components/Tooltip';
import { Button } from '@fluentui/react-components';

<Tooltip content="Save project" shortcut="Ctrl+S">
  <Button>Save</Button>
</Tooltip>
```

### Opening the Shortcuts Panel

Users can open the shortcuts panel by:
- Pressing `Ctrl+K`
- Pressing `?`

The panel shows all registered shortcuts organized by context with search functionality.

## Video Editor Shortcuts

### J/K/L Shuttle Control

The Video Editor implements professional shuttle controls:

- **J**: Reverse playback (press multiple times for faster speeds: -0.5x, -1x, -1.5x, up to -4x)
- **K**: Pause and reset playback speed to 1x
- **L**: Forward playback (press multiple times for faster speeds: 1.5x, 2x, 2.5x, up to 4x)

### I/O Points

- **I**: Set In point at current time
- **O**: Set Out point at current time
- **Ctrl+Shift+X**: Clear both In/Out points

### Tool Switching

- **1** or **V**: Select Tool
- **2** or **C**: Razor Tool (cut/split)
- **3** or **H**: Hand Tool (pan)

### Playback

- **Space**: Play/Pause
- **Arrow Left**: Previous frame
- **Arrow Right**: Next frame

### Editing

- **Delete** or **Backspace**: Delete selected clip
- **Ctrl+E**: Export video
- **Ctrl+S**: Save project

## Timeline Shortcuts

- **Space**: Play/Pause
- **S**: Split clip at playhead
- **+** or **=**: Zoom in
- **-**: Zoom out
- **Delete**: Delete selected clip
- **Shift+Delete**: Ripple delete (delete and close gap)
- **Home**: Go to beginning
- **End**: Go to end
- **Ctrl+Z**: Undo
- **Ctrl+Y**: Redo

## Create Workflow Shortcuts

- **Ctrl+Enter**: Next step
- **Ctrl+Shift+Enter**: Previous step
- **Escape**: Cancel/Go back

## Global Shortcuts

These work on any page:

- **Ctrl+K**: Open keyboard shortcuts panel
- **?**: Open keyboard shortcuts panel (alternative)
- **Ctrl+,**: Open Settings
- **Ctrl+N**: New Project
- **Ctrl+O**: Open Project
- **Ctrl+S**: Save Project
- **Ctrl+P**: Open Command Palette

## Customization

Users can customize keyboard shortcuts in:
**Settings → Keyboard Shortcuts**

Custom key bindings are stored in localStorage and automatically loaded by the KeyboardShortcutManager.

## Implementation Details

### Event Handling

The keyboard shortcut manager:

1. Listens for keyboard events at the window level
2. Checks if the user is typing in an input field (with exceptions for Ctrl+Enter and Escape)
3. Converts the event to a key combination string (e.g., "Ctrl+T")
4. Looks up matching shortcuts in active contexts (global + current page context)
5. Executes the handler if a match is found
6. Prevents default browser behavior and stops propagation

### Context Management

When navigating between pages:

1. The page component calls `setActiveContext()` in `useEffect`
2. This enables both 'global' context and the page-specific context
3. When unmounting, the component calls `unregisterContext()` to clean up

### Custom Key Mappings

Custom key mappings from Settings are:

1. Stored in localStorage as JSON
2. Loaded on KeyboardShortcutManager initialization
3. Applied when registering shortcuts (custom keys override defaults)

## Future Enhancements

Potential improvements:

- Visual chord system for complex multi-key shortcuts
- Shortcut recording UI for easier customization
- Import/export shortcut configurations
- Conflict detection UI
- Shortcut suggestions based on user behavior
- Per-project shortcut overrides
