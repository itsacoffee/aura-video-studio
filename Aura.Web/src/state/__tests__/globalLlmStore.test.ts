/**
 * Global LLM Store Tests
 *
 * Tests for the Zustand store that manages global LLM selection.
 * Validates that selection updates correctly and migrations work.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useGlobalLlmStore } from '../globalLlmStore';
import type { ModelValidationStatus } from '../globalLlmStore';

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

// Default validation status
const DEFAULT_MODEL_VALIDATION: ModelValidationStatus = {
  isValidated: false,
  isValid: true,
  errorMessage: undefined,
  lastValidatedAt: undefined,
};

describe('useGlobalLlmStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorageMock.clear();
    // Reset store to initial state
    useGlobalLlmStore.setState({
      selection: null,
      modelValidation: DEFAULT_MODEL_VALIDATION,
    });
  });

  describe('Initial state', () => {
    it('should initialize with null selection', () => {
      const state = useGlobalLlmStore.getState();
      expect(state.selection).toBeNull();
    });

    it('should initialize with default model validation', () => {
      const state = useGlobalLlmStore.getState();
      expect(state.modelValidation).toEqual(DEFAULT_MODEL_VALIDATION);
      expect(state.modelValidation.isValidated).toBe(false);
      expect(state.modelValidation.isValid).toBe(true);
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

    it('should reset model validation when selection changes', () => {
      const { setSelection, setModelValidation } = useGlobalLlmStore.getState();

      // Set initial selection and mark as validated
      setSelection({
        provider: 'Ollama',
        modelId: 'qwen3:4b',
      });
      setModelValidation({
        isValidated: true,
        isValid: false,
        errorMessage: 'Model not found',
        lastValidatedAt: Date.now(),
      });

      // Change selection - validation should reset
      setSelection({
        provider: 'Ollama',
        modelId: 'deepseek-r1:1.5b',
      });

      const state = useGlobalLlmStore.getState();
      expect(state.modelValidation.isValidated).toBe(false);
      expect(state.modelValidation.isValid).toBe(true);
      expect(state.modelValidation.errorMessage).toBeUndefined();
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

    it('should reset model validation when clearing', () => {
      const { setSelection, setModelValidation, clearSelection } = useGlobalLlmStore.getState();

      setSelection({
        provider: 'Ollama',
        modelId: 'qwen3:4b',
      });
      setModelValidation({
        isValidated: true,
        isValid: false,
        errorMessage: 'Model not found',
        lastValidatedAt: Date.now(),
      });

      clearSelection();

      const state = useGlobalLlmStore.getState();
      expect(state.modelValidation).toEqual(DEFAULT_MODEL_VALIDATION);
    });
  });

  describe('setModelValidation', () => {
    it('should update model validation status', () => {
      const { setModelValidation } = useGlobalLlmStore.getState();
      const now = Date.now();

      setModelValidation({
        isValidated: true,
        isValid: false,
        errorMessage: "Model 'qwen3:4b' is not installed",
        lastValidatedAt: now,
      });

      const state = useGlobalLlmStore.getState();
      expect(state.modelValidation.isValidated).toBe(true);
      expect(state.modelValidation.isValid).toBe(false);
      expect(state.modelValidation.errorMessage).toBe("Model 'qwen3:4b' is not installed");
      expect(state.modelValidation.lastValidatedAt).toBe(now);
    });

    it('should mark model as valid when it exists', () => {
      const { setModelValidation } = useGlobalLlmStore.getState();

      setModelValidation({
        isValidated: true,
        isValid: true,
        errorMessage: undefined,
        lastValidatedAt: Date.now(),
      });

      const state = useGlobalLlmStore.getState();
      expect(state.modelValidation.isValidated).toBe(true);
      expect(state.modelValidation.isValid).toBe(true);
      expect(state.modelValidation.errorMessage).toBeUndefined();
    });
  });

  describe('resetModelValidation', () => {
    it('should reset validation to default state', () => {
      const { setModelValidation, resetModelValidation } = useGlobalLlmStore.getState();

      // Set validation state
      setModelValidation({
        isValidated: true,
        isValid: false,
        errorMessage: 'Model not found',
        lastValidatedAt: Date.now(),
      });

      // Reset
      resetModelValidation();

      const state = useGlobalLlmStore.getState();
      expect(state.modelValidation).toEqual(DEFAULT_MODEL_VALIDATION);
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
