# Global Undo/Redo System - Developer Guide

## Overview

This guide explains how to use the global undo/redo system in Aura Video Studio. The system provides application-wide undo/redo functionality with keyboard shortcuts and UI controls.

## Key Features

- **Global Keyboard Shortcuts**:
  - `Ctrl/Cmd+Z`: Undo
  - `Ctrl/Cmd+Y`: Redo (Windows/Linux)
  - `Ctrl/Cmd+Shift+Z`: Redo (macOS)
  - `Ctrl/Cmd+Shift+U`: Show action history panel

- **UI Components**:
  - Undo/Redo buttons in the top toolbar
  - Action history panel (accessible via keyboard shortcut)
  - Tooltips showing action descriptions and shortcuts

- **Command Pattern**:
  - All undoable operations use the Command pattern
  - Each command implements `execute()` and `undo()`
  - Commands are tracked in a global history

## Architecture

### Core Components

1. **UndoManager Store** (`src/state/undoManager.ts`)
   - Zustand store managing global undo/redo state
   - Wraps the existing `CommandHistory` infrastructure
   - Provides actions: `execute`, `undo`, `redo`, `clear`

2. **Command Interface** (`src/services/commandHistory.ts`)

   ```typescript
   interface Command {
     execute(): void;
     undo(): void;
     getDescription(): string;
     getTimestamp(): Date;
   }
   ```

3. **UI Components**
   - `UndoRedoButtons`: Toolbar buttons (in `src/components/UndoRedo/`)
   - `ActionHistoryPanel`: Drawer showing action history

4. **Keyboard Shortcuts** (`src/hooks/useGlobalUndoShortcuts.ts`)
   - Automatically registered in App.tsx
   - Platform-aware (Windows/macOS/Linux)

## Usage Examples

### Example 1: Using the Global Undo Manager

The simplest way to add undo/redo support is to use the global undo manager:

```typescript
import { useUndoManager } from '../state/undoManager';
import { MyCommand } from '../commands/myCommands';

function MyComponent() {
  const { execute } = useUndoManager();

  const handleAction = () => {
    const command = new MyCommand(/* parameters */);
    execute(command); // Executes and adds to global history
  };

  return <button onClick={handleAction}>Do Action</button>;
}
```

### Example 2: Creating a Custom Command

Commands must implement the `Command` interface:

```typescript
import { Command } from '../services/commandHistory';

export class MyCommand implements Command {
  private timestamp: Date;
  private previousValue: string;

  constructor(
    private newValue: string,
    private setValue: (value: string) => void,
    currentValue: string
  ) {
    this.timestamp = new Date();
    this.previousValue = currentValue;
  }

  execute(): void {
    this.setValue(this.newValue);
  }

  undo(): void {
    this.setValue(this.previousValue);
  }

  getDescription(): string {
    return 'Change value';
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}
```

### Example 3: Workspace Layout Commands

The system includes built-in workspace commands:

```typescript
import { TogglePanelCommand } from '../commands/workspaceCommands';
import { useWorkspaceLayoutStore } from '../state/workspaceLayout';
import { useUndoManager } from '../state/undoManager';

function MyWorkspace() {
  const { collapsedPanels, togglePanelCollapsed } = useWorkspaceLayoutStore();
  const { execute } = useUndoManager();

  const handleToggleProperties = () => {
    const command = new TogglePanelCommand(
      'properties',
      () => collapsedPanels.properties,
      () => togglePanelCollapsed('properties')
    );
    execute(command);
  };

  return (
    <button onClick={handleToggleProperties}>
      Toggle Properties Panel
    </button>
  );
}
```

### Example 4: Video Editor Timeline Integration

The VideoEditorPage shows how to integrate with existing local command history:

```typescript
import { CommandHistory } from '../services/commandHistory';
import { useUndoManager } from '../state/undoManager';
import { AddClipCommand } from '../commands/clipCommands';

function VideoEditor() {
  // Local command history for video editor (existing pattern)
  const commandHistory = useMemo(() => new CommandHistory(50), []);

  // Can also sync with global undo manager for app-wide undo
  const globalUndo = useUndoManager();

  const handleAddClip = (clip: TimelineClip) => {
    const command = new AddClipCommand(clip, setClips);

    // Execute locally
    commandHistory.execute(command);

    // Optionally also add to global undo (for cross-feature undo)
    // globalUndo.execute(command);
  };

  return <Timeline onAddClip={handleAddClip} />;
}
```

### Example 5: Batching Multiple Commands

For operations that involve multiple steps, use `BatchCommandImpl`:

```typescript
import { BatchCommandImpl } from '../services/commandHistory';
import { useUndoManager } from '../state/undoManager';

function BulkEditor() {
  const { execute } = useUndoManager();

  const handleBulkUpdate = (items: Item[]) => {
    const batch = new BatchCommandImpl('Bulk update items');

    items.forEach(item => {
      const command = new UpdateItemCommand(item);
      batch.addCommand(command);
    });

    execute(batch); // All operations treated as one undo/redo action
  };

  return <button onClick={handleBulkUpdate}>Update All</button>;
}
```

## Best Practices

### 1. Command Naming

Use clear, descriptive names for commands:

- ✅ "Add video clip"
- ✅ "Change layout to Color Grading"
- ✅ "Toggle Properties Panel"
- ❌ "Do action"
- ❌ "Update"

### 2. State Capture

Capture all necessary state in the constructor:

```typescript
constructor(
  private itemId: string,
  private newValue: string,
  items: Item[]  // Current state
) {
  this.timestamp = new Date();
  // Capture current value for undo
  const item = items.find(i => i.id === itemId);
  this.previousValue = item?.value || '';
}
```

### 3. Idempotent Operations

Ensure commands can be executed multiple times:

```typescript
execute(): void {
  // ✅ Good: Sets to specific state
  this.setValue(this.newValue);

  // ❌ Bad: Increments (not idempotent)
  // this.setValue(currentValue + 1);
}
```

### 4. Local vs Global History

- **Local History**: Use for context-specific undo (e.g., text editor, timeline)
- **Global History**: Use for cross-feature operations (e.g., layout changes, settings)

### 5. Memory Management

The global undo manager maintains 100 actions by default. For memory-intensive operations:

- Store only necessary data in commands
- Use IDs instead of full objects when possible
- Consider clearing history after major operations (e.g., project load)

## Testing

### Unit Testing Commands

```typescript
import { describe, it, expect } from 'vitest';
import { MyCommand } from '../commands/myCommands';

describe('MyCommand', () => {
  it('should execute and undo correctly', () => {
    let value = 'initial';
    const setValue = (v: string) => {
      value = v;
    };

    const command = new MyCommand('updated', setValue, value);

    command.execute();
    expect(value).toBe('updated');

    command.undo();
    expect(value).toBe('initial');
  });

  it('should have correct description', () => {
    const command = new MyCommand('test', () => {}, 'old');
    expect(command.getDescription()).toBe('Change value');
  });
});
```

### Integration Testing

```typescript
import { renderHook, act } from '@testing-library/react';
import { useUndoManager } from '../state/undoManager';
import { MyCommand } from '../commands/myCommands';

describe('UndoManager Integration', () => {
  it('should handle command execution and undo', () => {
    const { result } = renderHook(() => useUndoManager());

    let value = 'initial';
    const command = new MyCommand(
      'updated',
      (v) => {
        value = v;
      },
      value
    );

    act(() => {
      result.current.execute(command);
    });

    expect(value).toBe('updated');
    expect(result.current.canUndo).toBe(true);

    act(() => {
      result.current.undo();
    });

    expect(value).toBe('initial');
    expect(result.current.canRedo).toBe(true);
  });
});
```

## Keyboard Shortcuts

The system automatically registers global keyboard shortcuts. To check available shortcuts:

- Press `?` or `Ctrl/Cmd+K` to open the shortcuts panel
- Press `Ctrl/Cmd+Shift+U` to view action history

### Context-Aware Shortcuts

Keyboard shortcuts are context-aware and won't trigger when typing in inputs:

```typescript
// Shortcuts automatically disabled in:
// - <input> elements
// - <textarea> elements
// - contentEditable elements

// Exception: Certain shortcuts work everywhere (Escape, Ctrl+Enter)
```

## Accessibility

The undo/redo system is fully accessible:

- Buttons have proper ARIA labels
- Tooltips provide context
- Keyboard navigation supported
- Screen reader announcements for state changes

## Future Enhancements (Phase 2)

The following features are planned for Phase 2:

- Server-side action logging (ActionLog table)
- Persistent undo across sessions
- Soft-delete for projects/templates
- Server-side undo endpoints
- Cross-user undo with conflict resolution

## Troubleshooting

### Commands Not Working

1. Check that command implements all interface methods
2. Verify execute() and undo() are symmetric
3. Ensure state is captured in constructor

### Undo/Redo Buttons Disabled

1. Check that commands are being added to history
2. Verify `useGlobalUndoShortcuts()` is called in App.tsx
3. Check browser console for errors

### Memory Issues

1. Reduce max history size if needed:
   ```typescript
   // In undoManager.ts
   const commandHistory = new CommandHistory(50); // Reduce from 100
   ```
2. Implement command cleanup for large data
3. Clear history after major operations

## Additional Resources

- **Command Pattern**: [Wikipedia](https://en.wikipedia.org/wiki/Command_pattern)
- **Existing Commands**: See `src/commands/clipCommands.ts` for examples
- **Command History**: See `src/services/commandHistory.ts` for core infrastructure
- **Tests**: See `src/state/__tests__/undoManager.test.ts` for test examples

## Support

For questions or issues:

1. Check this guide and existing command implementations
2. Review test files for usage patterns
3. Create an issue on GitHub with detailed description

---

**Last Updated**: Based on PR 39 implementation (Phase 1 MVP)
