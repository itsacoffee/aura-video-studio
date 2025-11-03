# Global Undo/Redo System - Visual Guide

## UI Components Overview

This guide describes the visual components of the global undo/redo system.

## 1. Toolbar Undo/Redo Buttons

**Location**: Top bar of the application, left side (before Results Tray)

**Components**:

- Undo button with left-facing arrow icon (↶)
- Redo button with right-facing arrow icon (↷)

**Visual States**:

```
┌─────────────────────────────────────────────────────────┐
│  ↶  ↷          [Application Content]      Results Tray  │
└─────────────────────────────────────────────────────────┘
     ↑  ↑
     │  └─ Redo button (disabled when no redo available)
     └──── Undo button (disabled when no undo available)
```

**Enabled State**:

- Full opacity
- Clickable cursor on hover
- Tooltip shows:
  - Action description: "Undo: Add video clip"
  - Keyboard shortcut: "(Ctrl+Z)" or "(⌘Z)"

**Disabled State**:

- Reduced opacity
- Not clickable
- Tooltip shows only shortcut: "Undo (Ctrl+Z)"

## 2. Tooltips

**Undo Button Tooltip Examples**:

```
┌──────────────────────────┐
│ Undo: Add video clip     │
│ (Ctrl+Z)                 │
└──────────────────────────┘

┌──────────────────────────┐
│ Undo: Toggle Properties  │
│ (⌘Z)                     │ ← macOS
└──────────────────────────┘
```

**Redo Button Tooltip Examples**:

```
┌──────────────────────────┐
│ Redo: Delete clip        │
│ (Ctrl+Y)                 │
└──────────────────────────┘

┌──────────────────────────┐
│ Redo: Change layout      │
│ (⌘⇧Z)                    │ ← macOS
└──────────────────────────┘
```

## 3. Action History Panel

**Trigger**: `Ctrl/Cmd+Shift+U` or clicking history button (if implemented)

**Layout**:

```
┌───────────────────────────────────────┐
│ Action History                    ✕   │
├───────────────────────────────────────┤
│                                       │
│  ┌─────────────────────────────────┐ │
│  │ Change layout to Color Grading  │ │
│  │ 2m ago                          │ │
│  └─────────────────────────────────┘ │
│                                       │
│  ┌─────────────────────────────────┐ │
│  │ Add video clip                  │ │
│  │ 5m ago                          │ │
│  └─────────────────────────────────┘ │
│                                       │
│  ┌─────────────────────────────────┐ │
│  │ Toggle Properties Panel         │ │
│  │ 10m ago                         │ │
│  └─────────────────────────────────┘ │
│                                       │
└───────────────────────────────────────┘
```

**Empty State**:

```
┌───────────────────────────────────────┐
│ Action History                    ✕   │
├───────────────────────────────────────┤
│                                       │
│            🕐                         │
│                                       │
│        No actions yet                 │
│                                       │
│   Your undo/redo history will         │
│   appear here                         │
│                                       │
└───────────────────────────────────────┘
```

**Features**:

- Most recent action at top
- Time stamps (relative: "Just now", "2m ago", "1h ago", "Yesterday")
- Scrollable list for long history
- Each entry shows:
  - Action description
  - Timestamp
  - Visual card with border

## 4. Keyboard Shortcuts Panel Integration

When user presses `?` or `Ctrl/Cmd+K`, the shortcuts panel now includes:

```
Global Shortcuts:
┌─────────────────────────────────────────────┐
│ Undo last action              Ctrl+Z        │
│ Redo last undone action       Ctrl+Y        │
│ Redo (alternative)            Ctrl+Shift+Z  │
│ Show action history           Ctrl+Shift+U  │
└─────────────────────────────────────────────┘
```

## 5. Integration with Video Editor

**Timeline Context**:

```
┌────────────────────────────────────────────────┐
│ File  Edit  View  Timeline  Help              │
├────────────────────────────────────────────────┤
│  ↶  ↷  [Other tools...]                       │
│                                                │
│  ┌──────────────────────────────────────────┐ │
│  │         Video Preview                    │ │
│  └──────────────────────────────────────────┘ │
│                                                │
│  ┌──────────────────────────────────────────┐ │
│  │  Track 1: [Clip 1] [Clip 2]             │ │
│  │  Track 2: [Clip 3]                       │ │
│  └──────────────────────────────────────────┘ │
└────────────────────────────────────────────────┘
```

**Undoable Actions in Video Editor**:

- Add clip to timeline
- Delete clip from timeline
- Move clip position
- Trim clip duration
- Add effect to clip
- Remove effect from clip
- Update clip properties (transform, opacity, etc.)

## 6. User Workflows

### Workflow 1: Simple Undo/Redo

```
1. User performs action (e.g., adds clip)
   → Clip appears on timeline
   → Undo button becomes enabled

2. User presses Ctrl+Z
   → Clip is removed
   → Undo button disabled (no more history)
   → Redo button enabled

3. User presses Ctrl+Y
   → Clip reappears
   → Redo button disabled
   → Undo button enabled again
```

### Workflow 2: Multiple Undo

```
1. User adds Clip A → Timeline: [A]
2. User adds Clip B → Timeline: [A][B]
3. User adds Clip C → Timeline: [A][B][C]

4. Press Ctrl+Z → Timeline: [A][B]  (removed C)
5. Press Ctrl+Z → Timeline: [A]     (removed B)
6. Press Ctrl+Z → Timeline: []      (removed A)

7. Press Ctrl+Y → Timeline: [A]     (restored A)
8. Press Ctrl+Y → Timeline: [A][B]  (restored B)
```

### Workflow 3: New Action Clears Redo

```
1. User adds Clip A → Timeline: [A]
2. User adds Clip B → Timeline: [A][B]

3. Press Ctrl+Z → Timeline: [A]
   (Redo stack: [B])

4. User adds Clip C → Timeline: [A][C]
   (Redo stack: empty - [B] is lost)

5. Press Ctrl+Y → Nothing happens (no redo available)
```

### Workflow 4: Viewing History

```
1. Press Ctrl+Shift+U
   → Action History panel opens on right side

2. View recent actions:
   - "Add Clip C" (Just now)
   - "Delete Clip B" (1m ago)
   - "Add Clip B" (2m ago)
   - "Add Clip A" (5m ago)

3. Press Escape or click X
   → Panel closes
```

## 7. Responsive Behavior

**Desktop (1920x1080)**:

```
┌──────────────────────────────────────────────────┐
│  ↶  ↷                     Results Tray    Theme  │
└──────────────────────────────────────────────────┘
   Full buttons with icons
```

**Tablet (768x1024)**:

```
┌────────────────────────────────────┐
│  ↶  ↷          Results Tray        │
└────────────────────────────────────┘
   Buttons still visible with icons
```

**Mobile (375x667)** - Not primary target but handled:

```
┌──────────────────────┐
│  ↶ ↷   Results       │
└──────────────────────┘
   Compact layout
```

## 8. Accessibility Features

**Screen Reader Announcements**:

```
Button: "Undo Add video clip, Ctrl+Z"
Button: "Redo Delete clip, Ctrl+Y" (when focused)

Panel: "Action History, showing 3 recent actions"
```

**Keyboard Navigation**:

- Tab to focus undo button
- Tab to focus redo button
- Enter/Space to activate
- Escape to close history panel

**Focus Indicators**:

- Clear focus ring on buttons
- High contrast in focus mode
- Visible in both light and dark themes

## 9. Dark Mode Support

**Light Theme**:

- Buttons: Subtle gray background
- Icons: Dark gray
- Disabled: Light gray with transparency

**Dark Theme**:

- Buttons: Subtle lighter background
- Icons: Light gray/white
- Disabled: Dark gray with transparency

Both themes maintain consistent:

- Icon size (24px)
- Button padding
- Tooltip contrast
- History panel contrast

## 10. Animation & Feedback

**Button States**:

```
Idle → Hover → Active → Success
  ↓      ↓       ↓         ↓
Normal  Scale   Press    Flash
        1.05x   0.95x    color
```

**Transitions**:

- Button hover: 150ms ease-out
- Panel slide: 250ms ease-in-out
- Tooltip fade: 200ms
- Disabled state: 150ms

**Visual Feedback**:

- Successful undo/redo: Button briefly highlights
- Failed operation: Subtle shake animation
- History panel: Smooth slide from right

---

## Testing Checklist

When testing the undo/redo UI:

- [ ] Buttons appear in top toolbar
- [ ] Tooltips show correct action descriptions
- [ ] Tooltips show platform-appropriate shortcuts
- [ ] Undo button disables when no history
- [ ] Redo button disables when no redo available
- [ ] Keyboard shortcuts work globally
- [ ] Keyboard shortcuts don't trigger in text inputs
- [ ] History panel opens with Ctrl+Shift+U
- [ ] History panel shows actions in reverse chronological order
- [ ] History panel closes with Escape or X button
- [ ] Actions appear in history after execution
- [ ] Undone actions move to redo stack
- [ ] New actions clear redo stack
- [ ] Works in both light and dark themes
- [ ] Screen reader announces button states
- [ ] Focus indicators are visible
- [ ] Works across different pages

---

**Implementation Files**:

- `src/components/UndoRedo/UndoRedoButtons.tsx` - Toolbar buttons
- `src/components/UndoRedo/ActionHistoryPanel.tsx` - History panel
- `src/components/Layout.tsx` - Integration point
- `src/hooks/useGlobalUndoShortcuts.ts` - Keyboard shortcuts
- `src/state/undoManager.ts` - State management

**Related Documentation**:

- See `UNDO_REDO_GUIDE.md` for developer integration guide
- See PR 39 implementation details for technical architecture
