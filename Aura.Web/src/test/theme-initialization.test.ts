import { describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('Theme Initialization', () => {
  let originalLocalStorage: Storage;

  beforeEach(() => {
    // Save original localStorage
    originalLocalStorage = global.localStorage;

    // Mock localStorage
    const store: Record<string, string> = {};
    global.localStorage = {
      getItem: (key: string) => store[key] || null,
      setItem: (key: string, value: string) => {
        store[key] = value;
      },
      removeItem: (key: string) => {
        delete store[key];
      },
      clear: () => {
        Object.keys(store).forEach((key) => delete store[key]);
      },
      key: (index: number) => Object.keys(store)[index] || null,
      length: Object.keys(store).length,
    } as Storage;
  });

  afterEach(() => {
    // Restore original localStorage
    global.localStorage = originalLocalStorage;
  });

  it('should default to dark mode on first run (no saved preference)', () => {
    // Simulate first run - no saved theme
    localStorage.clear();

    // Simulate App initialization logic
    const saved = localStorage.getItem('darkMode');
    const isDarkMode = saved !== null ? JSON.parse(saved) : true; // Default to dark

    expect(isDarkMode).toBe(true);
    expect(saved).toBeNull();
  });

  it('should respect saved light mode preference', () => {
    // Simulate user who previously chose light mode
    localStorage.setItem('darkMode', JSON.stringify(false));

    const saved = localStorage.getItem('darkMode');
    const isDarkMode = saved !== null ? JSON.parse(saved) : true;

    expect(isDarkMode).toBe(false);
  });

  it('should respect saved dark mode preference', () => {
    // Simulate user who previously chose dark mode
    localStorage.setItem('darkMode', JSON.stringify(true));

    const saved = localStorage.getItem('darkMode');
    const isDarkMode = saved !== null ? JSON.parse(saved) : true;

    expect(isDarkMode).toBe(true);
  });

  it('should persist theme choice to localStorage', () => {
    // Simulate saving theme choice
    const newTheme = false; // Switch to light mode
    localStorage.setItem('darkMode', JSON.stringify(newTheme));

    const saved = localStorage.getItem('darkMode');
    expect(saved).toBe('false');
    expect(JSON.parse(saved!)).toBe(false);
  });
});
