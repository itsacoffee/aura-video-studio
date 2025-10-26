import { describe, it, expect, beforeEach } from 'vitest';
import { keyboardShortcutManager, ShortcutHandler } from '../services/keyboardShortcutManager';

describe('KeyboardShortcutManager', () => {
  beforeEach(() => {
    // Clear all shortcuts before each test
    keyboardShortcutManager.clear();
  });

  it('should register a shortcut', () => {
    const handler = () => {
      // Test handler
    };
    const shortcut: ShortcutHandler = {
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'global',
      handler,
    };

    keyboardShortcutManager.register(shortcut);
    const shortcuts = keyboardShortcutManager.getAllShortcuts('global');

    expect(shortcuts).toHaveLength(1);
    expect(shortcuts[0].id).toBe('test-shortcut');
    expect(shortcuts[0].keys).toBe('Ctrl+T');
  });

  it('should register multiple shortcuts', () => {
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'shortcut-1',
        keys: 'Ctrl+A',
        description: 'Shortcut 1',
        context: 'global',
        handler: () => {},
      },
      {
        id: 'shortcut-2',
        keys: 'Ctrl+B',
        description: 'Shortcut 2',
        context: 'global',
        handler: () => {},
      },
    ];

    keyboardShortcutManager.registerMultiple(shortcuts);
    const registered = keyboardShortcutManager.getAllShortcuts('global');

    expect(registered).toHaveLength(2);
  });

  it('should unregister a shortcut', () => {
    const shortcut: ShortcutHandler = {
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'global',
      handler: () => {},
    };

    keyboardShortcutManager.register(shortcut);
    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(1);

    keyboardShortcutManager.unregister('test-shortcut', 'global');
    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(0);
  });

  it('should unregister all shortcuts in a context', () => {
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'shortcut-1',
        keys: 'Ctrl+A',
        description: 'Shortcut 1',
        context: 'video-editor',
        handler: () => {},
      },
      {
        id: 'shortcut-2',
        keys: 'Ctrl+B',
        description: 'Shortcut 2',
        context: 'video-editor',
        handler: () => {},
      },
      {
        id: 'shortcut-3',
        keys: 'Ctrl+C',
        description: 'Shortcut 3',
        context: 'global',
        handler: () => {},
      },
    ];

    keyboardShortcutManager.registerMultiple(shortcuts);
    expect(keyboardShortcutManager.getAllShortcuts('video-editor')).toHaveLength(2);
    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(1);

    keyboardShortcutManager.unregisterContext('video-editor');
    expect(keyboardShortcutManager.getAllShortcuts('video-editor')).toHaveLength(0);
    expect(keyboardShortcutManager.getAllShortcuts('global')).toHaveLength(1);
  });

  it('should set and get active context', () => {
    keyboardShortcutManager.setActiveContext('video-editor');
    expect(keyboardShortcutManager.getActiveContext()).toBe('video-editor');

    keyboardShortcutManager.setActiveContext('timeline');
    expect(keyboardShortcutManager.getActiveContext()).toBe('timeline');
  });

  it('should group shortcuts by context', () => {
    const shortcuts: ShortcutHandler[] = [
      {
        id: 'shortcut-1',
        keys: 'Ctrl+A',
        description: 'Shortcut 1',
        context: 'video-editor',
        handler: () => {},
      },
      {
        id: 'shortcut-2',
        keys: 'Ctrl+B',
        description: 'Shortcut 2',
        context: 'global',
        handler: () => {},
      },
      {
        id: 'shortcut-3',
        keys: 'Ctrl+C',
        description: 'Shortcut 3',
        context: 'timeline',
        handler: () => {},
      },
    ];

    keyboardShortcutManager.registerMultiple(shortcuts);
    const groups = keyboardShortcutManager.getShortcutGroups();

    expect(groups).toHaveLength(3);
    expect(groups.some((g) => g.context === 'video-editor')).toBe(true);
    expect(groups.some((g) => g.context === 'global')).toBe(true);
    expect(groups.some((g) => g.context === 'timeline')).toBe(true);
  });

  it('should handle keyboard events', () => {
    let called = false;
    const shortcut: ShortcutHandler = {
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'global',
      handler: () => {
        called = true;
      },
    };

    keyboardShortcutManager.register(shortcut);
    keyboardShortcutManager.setActiveContext('global');

    // Simulate Ctrl+T keyboard event
    const event = new KeyboardEvent('keydown', {
      key: 't',
      ctrlKey: true,
      bubbles: true,
      cancelable: true,
    });

    const handled = keyboardShortcutManager.handleKeyEvent(event);
    expect(handled).toBe(true);
    expect(called).toBe(true);
  });

  it('should not handle events from inactive contexts', () => {
    let called = false;
    const shortcut: ShortcutHandler = {
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'video-editor',
      handler: () => {
        called = true;
      },
    };

    keyboardShortcutManager.register(shortcut);
    keyboardShortcutManager.setActiveContext('timeline'); // Different context

    const event = new KeyboardEvent('keydown', {
      key: 't',
      ctrlKey: true,
      bubbles: true,
      cancelable: true,
    });

    const handled = keyboardShortcutManager.handleKeyEvent(event);
    expect(handled).toBe(false);
    expect(called).toBe(false);
  });

  it('should enable and disable shortcuts', () => {
    let called = false;
    const shortcut: ShortcutHandler = {
      id: 'test-shortcut',
      keys: 'Ctrl+T',
      description: 'Test shortcut',
      context: 'global',
      handler: () => {
        called = true;
      },
    };

    keyboardShortcutManager.register(shortcut);
    keyboardShortcutManager.setActiveContext('global');

    // Disable the shortcut
    keyboardShortcutManager.setShortcutEnabled('test-shortcut', 'global', false);

    const event = new KeyboardEvent('keydown', {
      key: 't',
      ctrlKey: true,
      bubbles: true,
      cancelable: true,
    });

    let handled = keyboardShortcutManager.handleKeyEvent(event);
    expect(handled).toBe(false);
    expect(called).toBe(false);

    // Re-enable the shortcut
    keyboardShortcutManager.setShortcutEnabled('test-shortcut', 'global', true);
    handled = keyboardShortcutManager.handleKeyEvent(event);
    expect(handled).toBe(true);
    expect(called).toBe(true);
  });
});
