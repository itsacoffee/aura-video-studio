import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { WizardData } from '../../components/VideoWizard/types';
import { useWizardPersistence } from '../useWizardPersistence';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
    get store() {
      return store;
    },
  };
})();

Object.defineProperty(window, 'localStorage', { value: localStorageMock });

// Test data
const mockWizardData: WizardData = {
  brief: {
    topic: 'Test Topic',
    videoType: 'educational',
    targetAudience: 'Test Audience',
    keyMessage: 'Test Message',
    duration: 60,
  },
  style: {
    voiceProvider: 'Windows',
    voiceName: 'default',
    visualStyle: 'modern',
    musicGenre: 'ambient',
    musicEnabled: true,
    imageProvider: 'Placeholder',
  },
  script: {
    content: '',
    scenes: [],
    generatedAt: null,
  },
  preview: {
    thumbnails: [],
    audioSamples: [],
  },
  export: {
    quality: 'high',
    format: 'mp4',
    resolution: '1080p',
    includeCaptions: true,
  },
  advanced: {
    targetPlatform: 'youtube',
    customTransitions: false,
    llmParameters: {},
    ragConfiguration: {
      enabled: false,
      topK: 5,
      minimumScore: 0.6,
      maxContextTokens: 2000,
      includeCitations: true,
      tightenClaims: false,
    },
    customInstructions: '',
  },
};

describe('useWizardPersistence', () => {
  beforeEach(() => {
    localStorageMock.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllTimers();
  });

  it('initializes with no resumable session when localStorage is empty', () => {
    const { result } = renderHook(() => useWizardPersistence());

    expect(result.current.state.hasResumableSession).toBe(false);
    expect(result.current.state.savedData).toBeNull();
    expect(result.current.state.savedStep).toBe(0);
  });

  it('saves state to localStorage', () => {
    const { result } = renderHook(() => useWizardPersistence({ enableAutoSave: false }));

    act(() => {
      result.current.saveState(mockWizardData, 2);
    });

    expect(localStorageMock.setItem).toHaveBeenCalledWith('aura-wizard-data', expect.any(String));
    expect(localStorageMock.setItem).toHaveBeenCalledWith('aura-wizard-step', '2');
    expect(result.current.state.hasResumableSession).toBe(true);
    expect(result.current.state.savedStep).toBe(2);
  });

  it('restores session from localStorage', () => {
    // Pre-populate localStorage
    const timestamp = new Date().toISOString();
    localStorageMock.setItem('aura-wizard-data', JSON.stringify(mockWizardData));
    localStorageMock.setItem('aura-wizard-step', '3');
    localStorageMock.setItem('aura-wizard-timestamp', timestamp);
    localStorageMock.setItem('aura-wizard-session-id', 'test-session-123');

    const { result } = renderHook(() => useWizardPersistence());

    expect(result.current.state.hasResumableSession).toBe(true);
    expect(result.current.state.savedStep).toBe(3);

    const restored = result.current.restoreSession();
    expect(restored).not.toBeNull();
    expect(restored?.step).toBe(3);
    expect(restored?.data.brief.topic).toBe('Test Topic');
  });

  it('clears session from localStorage', () => {
    // Pre-populate localStorage
    localStorageMock.setItem('aura-wizard-data', JSON.stringify(mockWizardData));
    localStorageMock.setItem('aura-wizard-step', '2');
    localStorageMock.setItem('aura-wizard-timestamp', new Date().toISOString());
    localStorageMock.setItem('aura-wizard-session-id', 'test-session');

    const { result } = renderHook(() => useWizardPersistence());

    act(() => {
      result.current.clearSession();
    });

    expect(localStorageMock.removeItem).toHaveBeenCalledWith('aura-wizard-data');
    expect(localStorageMock.removeItem).toHaveBeenCalledWith('aura-wizard-step');
    expect(localStorageMock.removeItem).toHaveBeenCalledWith('aura-wizard-timestamp');
    expect(localStorageMock.removeItem).toHaveBeenCalledWith('aura-wizard-session-id');
    expect(result.current.state.hasResumableSession).toBe(false);
  });

  it('checkResumable returns correct value', () => {
    const { result } = renderHook(() => useWizardPersistence({ enableAutoSave: false }));

    // Initially false
    expect(result.current.checkResumable()).toBe(false);

    // After saving - use direct localStorage to simulate a proper save
    act(() => {
      result.current.saveState(mockWizardData, 1);
    });

    // checkResumable reads directly from localStorage
    // After saveState, the mock should have the data
    expect(result.current.checkResumable()).toBe(true);
  });

  it('calls onSessionRestored callback when restoring', () => {
    const onSessionRestored = vi.fn();

    // Pre-populate localStorage
    localStorageMock.setItem('aura-wizard-data', JSON.stringify(mockWizardData));
    localStorageMock.setItem('aura-wizard-step', '2');
    localStorageMock.setItem('aura-wizard-timestamp', new Date().toISOString());

    const { result } = renderHook(() => useWizardPersistence({ onSessionRestored }));

    act(() => {
      result.current.restoreSession();
    });

    expect(onSessionRestored).toHaveBeenCalledWith(
      expect.objectContaining({ brief: expect.any(Object) }),
      2
    );
  });

  it('calls onSaveError callback when save fails', () => {
    const onSaveError = vi.fn();

    // Make localStorage.setItem throw
    localStorageMock.setItem.mockImplementationOnce(() => {
      throw new Error('Storage quota exceeded');
    });

    const { result } = renderHook(() =>
      useWizardPersistence({
        enableAutoSave: false,
        onSaveError,
      })
    );

    act(() => {
      result.current.saveState(mockWizardData, 1);
    });

    expect(onSaveError).toHaveBeenCalled();
    expect(result.current.state.saveError).toBe('Storage quota exceeded');
  });

  it('ignores stale sessions older than 24 hours', () => {
    // Set timestamp to 25 hours ago
    const staleTimestamp = new Date(Date.now() - 25 * 60 * 60 * 1000).toISOString();
    localStorageMock.setItem('aura-wizard-data', JSON.stringify(mockWizardData));
    localStorageMock.setItem('aura-wizard-step', '3');
    localStorageMock.setItem('aura-wizard-timestamp', staleTimestamp);

    const { result } = renderHook(() => useWizardPersistence());

    expect(result.current.state.hasResumableSession).toBe(false);
  });

  it('debounces auto-save when enabled', async () => {
    vi.useFakeTimers();

    const { result } = renderHook(() =>
      useWizardPersistence({
        enableAutoSave: true,
        autoSaveInterval: 1000,
      })
    );

    // Make multiple rapid saves
    act(() => {
      result.current.saveState(mockWizardData, 1);
      result.current.saveState(mockWizardData, 2);
      result.current.saveState(mockWizardData, 3);
    });

    // Should not have saved yet (debounced)
    expect(localStorageMock.setItem).not.toHaveBeenCalledWith(
      'aura-wizard-step',
      expect.any(String)
    );

    // Fast-forward past debounce time
    act(() => {
      vi.advanceTimersByTime(1500);
    });

    // Now it should have saved (only the last value)
    expect(localStorageMock.setItem).toHaveBeenCalledWith('aura-wizard-step', '3');

    vi.useRealTimers();
  });

  it('triggerSave bypasses debounce', async () => {
    vi.useFakeTimers();

    const { result } = renderHook(() =>
      useWizardPersistence({
        enableAutoSave: true,
        autoSaveInterval: 5000,
      })
    );

    // Start a save that would be debounced
    act(() => {
      result.current.saveState(mockWizardData, 2);
    });

    // Immediately trigger save
    act(() => {
      result.current.triggerSave();
    });

    // Should have saved immediately without waiting for debounce
    expect(localStorageMock.setItem).toHaveBeenCalledWith('aura-wizard-step', '2');

    vi.useRealTimers();
  });

  it('updates lastSaveTime after successful save', async () => {
    const { result } = renderHook(() => useWizardPersistence({ enableAutoSave: false }));

    expect(result.current.state.lastSaveTime).toBeNull();

    act(() => {
      result.current.saveState(mockWizardData, 1);
    });

    expect(result.current.state.lastSaveTime).toBeInstanceOf(Date);
  });

  it('generates unique session ID on save', () => {
    const { result } = renderHook(() => useWizardPersistence({ enableAutoSave: false }));

    act(() => {
      result.current.saveState(mockWizardData, 1);
    });

    expect(localStorageMock.setItem).toHaveBeenCalledWith(
      'aura-wizard-session-id',
      expect.stringMatching(/^wizard-\d+-[a-z0-9]+$/)
    );
  });
});
