import { renderHook } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { keyboardShortcutManager, ShortcutHandler } from '../../services/keyboardShortcutManager';
import { useKeyboardShortcuts } from '../useKeyboardShortcuts';

describe('useKeyboardShortcuts', () => {
  beforeEach(() => {
    keyboardShortcutManager.clear();
  });

  it('should register shortcuts on mount', () => {
    const handler = vi.fn();
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'test-shortcut',
        keys: 'Ctrl+T',
        description: 'Test shortcut',
        context: 'global',
        handler,
      },
    ];

    renderHook(() => useKeyboardShortcuts(shortcuts));

    const registered = keyboardShortcutManager.getAllShortcuts('global');
    expect(registered).toHaveLength(1);
    expect(registered[0].id).toBe('test-shortcut');
  });

  it('should unregister shortcuts on unmount', () => {
    const handler = vi.fn();
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'test-shortcut',
        keys: 'Ctrl+T',
        description: 'Test shortcut',
        context: 'global',
        handler,
      },
    ];

    const { unmount } = renderHook(() => useKeyboardShortcuts(shortcuts));

    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(1);

    unmount();

    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(0);
  });

  it('should not register shortcuts when disabled', () => {
    const handler = vi.fn();
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'test-shortcut',
        keys: 'Ctrl+T',
        description: 'Test shortcut',
        context: 'global',
        handler,
      },
    ];

    renderHook(() => useKeyboardShortcuts(shortcuts, false));

    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(0);
  });

  it('should register multiple shortcuts', () => {
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'shortcut-1',
        keys: 'Ctrl+A',
        description: 'Shortcut 1',
        context: 'global',
        handler: vi.fn(),
      },
      {
        id: 'shortcut-2',
        keys: 'Ctrl+B',
        description: 'Shortcut 2',
        context: 'global',
        handler: vi.fn(),
      },
    ];

    renderHook(() => useKeyboardShortcuts(shortcuts));

    const registered = keyboardShortcutManager.getAllShortcuts('global');
    expect(registered).toHaveLength(2);
  });
});
