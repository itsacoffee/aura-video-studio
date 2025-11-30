/**
 * Tests for usePanelPreferences hook
 */

import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { usePanelPreferences, useSinglePanelPreference } from '../usePanelPreferences';

const STORAGE_KEY = 'aura-panel-preferences';

describe('usePanelPreferences', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('returns empty preferences initially', () => {
    const { result } = renderHook(() => usePanelPreferences());

    expect(result.current.preferences).toEqual({});
  });

  it('loads existing preferences from localStorage', () => {
    const savedPrefs = {
      sidebar: { width: 300, collapsed: false, visible: true },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(savedPrefs));

    const { result } = renderHook(() => usePanelPreferences());

    expect(result.current.preferences).toEqual(savedPrefs);
  });

  it('getPreference returns correct preference', () => {
    const savedPrefs = {
      sidebar: { width: 300, collapsed: false, visible: true },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(savedPrefs));

    const { result } = renderHook(() => usePanelPreferences());

    const pref = result.current.getPreference('sidebar');
    expect(pref).toEqual(savedPrefs.sidebar);
  });

  it('getPreference returns undefined for non-existent panel', () => {
    const { result } = renderHook(() => usePanelPreferences());

    const pref = result.current.getPreference('nonexistent');
    expect(pref).toBeUndefined();
  });

  it('setPreference updates and persists preference', () => {
    const { result } = renderHook(() => usePanelPreferences());

    act(() => {
      result.current.setPreference('sidebar', { width: 350, collapsed: true, visible: true });
    });

    expect(result.current.preferences.sidebar).toEqual({
      width: 350,
      collapsed: true,
      visible: true,
    });

    const stored = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');
    expect(stored.sidebar).toEqual({ width: 350, collapsed: true, visible: true });
  });

  it('setWidth updates only width', () => {
    const { result } = renderHook(() => usePanelPreferences());

    act(() => {
      result.current.setPreference('sidebar', { width: 280, collapsed: false, visible: true });
    });

    act(() => {
      result.current.setWidth('sidebar', 400);
    });

    expect(result.current.preferences.sidebar?.width).toBe(400);
    expect(result.current.preferences.sidebar?.collapsed).toBe(false);
  });

  it('setCollapsed updates only collapsed state', () => {
    const { result } = renderHook(() => usePanelPreferences());

    act(() => {
      result.current.setPreference('sidebar', { width: 280, collapsed: false, visible: true });
    });

    act(() => {
      result.current.setCollapsed('sidebar', true);
    });

    expect(result.current.preferences.sidebar?.collapsed).toBe(true);
    expect(result.current.preferences.sidebar?.width).toBe(280);
  });

  it('setVisible updates only visible state', () => {
    const { result } = renderHook(() => usePanelPreferences());

    act(() => {
      result.current.setPreference('sidebar', { width: 280, collapsed: false, visible: true });
    });

    act(() => {
      result.current.setVisible('sidebar', false);
    });

    expect(result.current.preferences.sidebar?.visible).toBe(false);
    expect(result.current.preferences.sidebar?.width).toBe(280);
  });

  it('resetPreference removes specific panel preference', () => {
    const { result } = renderHook(() => usePanelPreferences());

    act(() => {
      result.current.setPreference('sidebar', { width: 280, collapsed: false, visible: true });
      result.current.setPreference('inspector', { width: 300, collapsed: false, visible: true });
    });

    act(() => {
      result.current.resetPreference('sidebar');
    });

    expect(result.current.preferences.sidebar).toBeUndefined();
    expect(result.current.preferences.inspector).toBeDefined();
  });

  it('resetAll clears all preferences', () => {
    const { result } = renderHook(() => usePanelPreferences());

    act(() => {
      result.current.setPreference('sidebar', { width: 280, collapsed: false, visible: true });
      result.current.setPreference('inspector', { width: 300, collapsed: false, visible: true });
    });

    act(() => {
      result.current.resetAll();
    });

    expect(result.current.preferences).toEqual({});
    // After resetAll, localStorage should be cleared (removed entirely)
    // Note: The hook also saves {} to localStorage in the effect, so we check if it's null or empty
    const stored = localStorage.getItem(STORAGE_KEY);
    expect(stored === null || stored === '{}').toBe(true);
  });

  it('handles invalid JSON in localStorage gracefully', () => {
    localStorage.setItem(STORAGE_KEY, 'invalid json');

    const { result } = renderHook(() => usePanelPreferences());

    expect(result.current.preferences).toEqual({});
  });
});

describe('useSinglePanelPreference', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('returns default values when no preference exists', () => {
    const { result } = renderHook(() =>
      useSinglePanelPreference('sidebar', {
        width: 280,
        collapsed: false,
        visible: true,
      })
    );

    expect(result.current.width).toBe(280);
    expect(result.current.collapsed).toBe(false);
    expect(result.current.visible).toBe(true);
  });

  it('returns saved values when preference exists', () => {
    const savedPrefs = {
      sidebar: { width: 350, collapsed: true, visible: false },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(savedPrefs));

    const { result } = renderHook(() =>
      useSinglePanelPreference('sidebar', {
        width: 280,
        collapsed: false,
        visible: true,
      })
    );

    expect(result.current.width).toBe(350);
    expect(result.current.collapsed).toBe(true);
    expect(result.current.visible).toBe(false);
  });

  it('setWidth updates width for the specific panel', () => {
    const { result } = renderHook(() =>
      useSinglePanelPreference('sidebar', {
        width: 280,
        collapsed: false,
        visible: true,
      })
    );

    act(() => {
      result.current.setWidth(400);
    });

    expect(result.current.width).toBe(400);
  });

  it('setCollapsed updates collapsed state', () => {
    const { result } = renderHook(() =>
      useSinglePanelPreference('sidebar', {
        width: 280,
        collapsed: false,
        visible: true,
      })
    );

    act(() => {
      result.current.setCollapsed(true);
    });

    expect(result.current.collapsed).toBe(true);
  });

  it('reset removes the panel preference', () => {
    const savedPrefs = {
      sidebar: { width: 350, collapsed: true, visible: true },
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(savedPrefs));

    const { result } = renderHook(() =>
      useSinglePanelPreference('sidebar', {
        width: 280,
        collapsed: false,
        visible: true,
      })
    );

    expect(result.current.width).toBe(350);

    act(() => {
      result.current.reset();
    });

    // After reset, should return default value
    expect(result.current.width).toBe(280);
  });
});
