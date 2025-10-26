/**
 * Keyboard Shortcut Manager Service
 *
 * Centralized service for managing keyboard shortcuts across the application.
 * Handles shortcut registration, conflict detection, and context-aware execution.
 */

export type ShortcutContext = 'global' | 'video-editor' | 'timeline' | 'create' | 'settings';

export interface ShortcutHandler {
  id: string;
  keys: string; // e.g., "Ctrl+K", "Space", "J"
  description: string;
  context: ShortcutContext;
  handler: (event: KeyboardEvent) => void;
  enabled?: boolean;
  // If true, prevents default browser behavior
  preventDefault?: boolean;
  // If true, stops event propagation
  stopPropagation?: boolean;
}

export interface ShortcutGroup {
  name: string;
  context: ShortcutContext;
  shortcuts: ShortcutHandler[];
}

class KeyboardShortcutManager {
  private shortcuts: Map<string, ShortcutHandler> = new Map();
  private activeContext: ShortcutContext = 'global';
  private enabledContexts: Set<ShortcutContext> = new Set(['global']);
  private customKeyMappings: Map<string, string> = new Map(); // Maps action IDs to custom key combinations

  constructor() {
    this.loadCustomMappings();
  }

  /**
   * Load custom key mappings from localStorage
   */
  private loadCustomMappings() {
    try {
      const saved = localStorage.getItem('keyboardShortcuts');
      if (saved) {
        const data = JSON.parse(saved);
        if (data.shortcuts && Array.isArray(data.shortcuts)) {
          data.shortcuts.forEach((s: any) => {
            if (s.action && s.currentShortcut) {
              this.customKeyMappings.set(s.action, s.currentShortcut);
            }
          });
        }
      }
    } catch (error) {
      console.error('Error loading custom key mappings:', error);
    }
  }

  /**
   * Get the effective keys for a shortcut, considering custom mappings
   */
  private getEffectiveKeys(id: string, defaultKeys: string): string {
    return this.customKeyMappings.get(id) || defaultKeys;
  }

  /**
   * Register a keyboard shortcut
   */
  register(shortcut: ShortcutHandler): void {
    const effectiveKeys = this.getEffectiveKeys(shortcut.id, shortcut.keys);
    const key = this.createKey(effectiveKeys, shortcut.context);

    // Check for conflicts
    if (this.shortcuts.has(key)) {
      console.warn(`Shortcut conflict detected: ${effectiveKeys} in context ${shortcut.context}`);
    }

    this.shortcuts.set(key, {
      ...shortcut,
      keys: effectiveKeys,
      enabled: shortcut.enabled !== false,
      preventDefault: shortcut.preventDefault !== false,
      stopPropagation: shortcut.stopPropagation !== false,
    });
  }

  /**
   * Register multiple shortcuts at once
   */
  registerMultiple(shortcuts: ShortcutHandler[]): void {
    shortcuts.forEach((shortcut) => this.register(shortcut));
  }

  /**
   * Unregister a shortcut by ID and context
   */
  unregister(id: string, context: ShortcutContext): void {
    // Find and remove the shortcut
    for (const [key, shortcut] of this.shortcuts.entries()) {
      if (shortcut.id === id && shortcut.context === context) {
        this.shortcuts.delete(key);
        break;
      }
    }
  }

  /**
   * Unregister all shortcuts for a given context
   */
  unregisterContext(context: ShortcutContext): void {
    const keysToDelete: string[] = [];

    for (const [key, shortcut] of this.shortcuts.entries()) {
      if (shortcut.context === context) {
        keysToDelete.push(key);
      }
    }

    keysToDelete.forEach((key) => this.shortcuts.delete(key));
  }

  /**
   * Set the active context (e.g., when navigating to a different page)
   */
  setActiveContext(context: ShortcutContext): void {
    this.activeContext = context;
    this.enabledContexts.clear();
    this.enabledContexts.add('global'); // Global shortcuts always active
    this.enabledContexts.add(context);
  }

  /**
   * Get the current active context
   */
  getActiveContext(): ShortcutContext {
    return this.activeContext;
  }

  /**
   * Handle keyboard event
   */
  handleKeyEvent(event: KeyboardEvent): boolean {
    // Don't handle shortcuts if user is typing in an input/textarea
    if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
      // Exception: Allow shortcuts like Ctrl+Enter or Escape even in inputs
      const allowedInInputs = ['Enter', 'Escape'];
      const hasModifier = event.ctrlKey || event.metaKey || event.altKey;

      if (!allowedInInputs.includes(event.key) || !hasModifier) {
        return false;
      }
    }

    const keyCombo = this.getKeyCombo(event);

    // Try to find a matching shortcut in active contexts
    for (const context of this.enabledContexts) {
      const key = this.createKey(keyCombo, context);
      const shortcut = this.shortcuts.get(key);

      if (shortcut && shortcut.enabled) {
        if (shortcut.preventDefault) {
          event.preventDefault();
        }
        if (shortcut.stopPropagation) {
          event.stopPropagation();
        }

        try {
          shortcut.handler(event);
          return true;
        } catch (error) {
          console.error(`Error executing shortcut handler for ${keyCombo}:`, error);
          return false;
        }
      }
    }

    return false;
  }

  /**
   * Get all registered shortcuts, optionally filtered by context
   */
  getAllShortcuts(context?: ShortcutContext): ShortcutHandler[] {
    const shortcuts: ShortcutHandler[] = [];

    for (const shortcut of this.shortcuts.values()) {
      if (!context || shortcut.context === context) {
        shortcuts.push(shortcut);
      }
    }

    return shortcuts;
  }

  /**
   * Get shortcuts grouped by context
   */
  getShortcutGroups(): ShortcutGroup[] {
    const groups = new Map<ShortcutContext, ShortcutHandler[]>();

    for (const shortcut of this.shortcuts.values()) {
      const existing = groups.get(shortcut.context) || [];
      existing.push(shortcut);
      groups.set(shortcut.context, existing);
    }

    const contextNames: Record<ShortcutContext, string> = {
      global: 'Global',
      'video-editor': 'Video Editor',
      timeline: 'Timeline Editor',
      create: 'Create Workflow',
      settings: 'Settings',
    };

    return Array.from(groups.entries()).map(([context, shortcuts]) => ({
      name: contextNames[context],
      context,
      shortcuts: shortcuts.sort((a, b) => a.description.localeCompare(b.description)),
    }));
  }

  /**
   * Create a unique key for a shortcut
   */
  private createKey(keys: string, context: ShortcutContext): string {
    return `${context}:${keys.toLowerCase()}`;
  }

  /**
   * Convert keyboard event to key combination string
   */
  private getKeyCombo(event: KeyboardEvent): string {
    const parts: string[] = [];

    if (event.ctrlKey || event.metaKey) parts.push('Ctrl');
    if (event.altKey) parts.push('Alt');
    if (event.shiftKey) parts.push('Shift');

    // Normalize key names
    let key = event.key;

    // Special key mappings
    const keyMap: Record<string, string> = {
      ' ': 'Space',
      '+': 'Plus',
      '-': 'Minus',
      '=': 'Equals',
      '/': 'Slash',
      '\\': 'Backslash',
      ',': 'Comma',
      '.': 'Period',
      '?': 'Question',
    };

    key = keyMap[key] || key;

    // Handle special keys
    if (key.startsWith('Arrow')) {
      key = key; // Keep ArrowLeft, ArrowRight, etc.
    } else if (key.length === 1) {
      // For single character keys, use uppercase
      key = key.toUpperCase();
    }

    parts.push(key);

    return parts.join('+');
  }

  /**
   * Enable or disable a specific shortcut
   */
  setShortcutEnabled(id: string, context: ShortcutContext, enabled: boolean): void {
    for (const [key, shortcut] of this.shortcuts.entries()) {
      if (shortcut.id === id && shortcut.context === context) {
        shortcut.enabled = enabled;
        this.shortcuts.set(key, shortcut);
        break;
      }
    }
  }

  /**
   * Clear all registered shortcuts
   */
  clear(): void {
    this.shortcuts.clear();
  }
}

// Singleton instance
export const keyboardShortcutManager = new KeyboardShortcutManager();
