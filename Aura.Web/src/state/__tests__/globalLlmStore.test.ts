/**
 * Global LLM Store Tests
 *
 * Tests for the Zustand store that manages global LLM selection.
 * Validates that selection updates correctly and migrations work.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useGlobalLlmStore } from '../globalLlmStore';

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
    get length() {
      return Object.keys(store).length;
    },
    key: vi.fn((index: number) => Object.keys(store)[index] || null),
  };
})();

Object.defineProperty(global, 'localStorage', { value: localStorageMock });

describe('useGlobalLlmStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorageMock.clear();
    // Reset store to initial state
    useGlobalLlmStore.setState({
      selection: null,
    });
  });

  describe('Initial state', () => {
    it('should initialize with null selection', () => {
      const state = useGlobalLlmStore.getState();
      expect(state.selection).toBeNull();
    });
  });

  describe('setSelection', () => {
    it('should update selection with provider and modelId', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.selection).toEqual({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });
    });

    it('should overwrite existing selection', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      // Set initial selection
      setSelection({
        provider: 'Ollama',
        modelId: 'qwen3:4b',
      });

      // Overwrite with new selection
      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.selection).toEqual({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });
    });

    it('should allow changing provider', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      setSelection({
        provider: 'OpenAI',
        modelId: 'gpt-4o',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.selection?.provider).toBe('OpenAI');
      expect(state.selection?.modelId).toBe('gpt-4o');
    });

    it('should allow setting selection to null', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      setSelection(null);

      const state = useGlobalLlmStore.getState();
      expect(state.selection).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('should clear the selection', () => {
      const { setSelection, clearSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      clearSelection();

      const state = useGlobalLlmStore.getState();
      expect(state.selection).toBeNull();
    });
  });

  describe('Backend priority over localStorage', () => {
    it('should allow overwriting stale localStorage selection with backend data', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      // Simulate stale localStorage data (old model)
      setSelection({
        provider: 'Ollama',
        modelId: 'qwen3:4b',
      });

      // Backend returns different model - should overwrite
      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.selection?.modelId).toBe('deepseek-r1:1.5b');
    });
  });

  describe('Edge cases', () => {
    it('should handle empty modelId', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: '',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.selection?.provider).toBe('Ollama');
      expect(state.selection?.modelId).toBe('');
    });

    it('should handle special characters in modelId', () => {
      const { setSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b-instruct',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.selection?.modelId).toBe('deepseek-r1:1.5b-instruct');
    });
  });
});
