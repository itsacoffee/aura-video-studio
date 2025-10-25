/**
 * Tests for useLoadingState hook
 */

import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useLoadingState, withLoadingState } from '../useLoadingState';

describe('useLoadingState', () => {
  it('should initialize with default state', () => {
    const { result } = renderHook(() => useLoadingState());
    const [state] = result.current;

    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
    expect(state.progress).toBeUndefined();
    expect(state.estimatedTimeRemaining).toBeUndefined();
    expect(state.status).toBeUndefined();
  });

  it('should initialize with custom loading state', () => {
    const { result } = renderHook(() => useLoadingState(true));
    const [state] = result.current;

    expect(state.isLoading).toBe(true);
  });

  it('should start loading', () => {
    const { result } = renderHook(() => useLoadingState());

    act(() => {
      const [, actions] = result.current;
      actions.startLoading('Initializing...');
    });

    const [state] = result.current;
    expect(state.isLoading).toBe(true);
    expect(state.status).toBe('Initializing...');
    expect(state.error).toBeNull();
  });

  it('should stop loading', () => {
    const { result } = renderHook(() => useLoadingState(true));

    act(() => {
      const [, actions] = result.current;
      actions.stopLoading();
    });

    const [state] = result.current;
    expect(state.isLoading).toBe(false);
  });

  it('should set error', () => {
    const { result } = renderHook(() => useLoadingState());

    act(() => {
      const [, actions] = result.current;
      actions.setError('Something went wrong');
    });

    const [state] = result.current;
    expect(state.error).toBe('Something went wrong');
    expect(state.isLoading).toBe(false);
  });

  it('should update progress', () => {
    const { result } = renderHook(() => useLoadingState(true));

    act(() => {
      const [, actions] = result.current;
      actions.updateProgress(50, 120, 'Processing...');
    });

    const [state] = result.current;
    expect(state.progress).toBe(50);
    expect(state.estimatedTimeRemaining).toBe(120);
    expect(state.status).toBe('Processing...');
  });

  it('should reset state', () => {
    const { result } = renderHook(() => useLoadingState(true));

    act(() => {
      const [, actions] = result.current;
      actions.setError('Error');
      actions.updateProgress(75);
    });

    act(() => {
      const [, actions] = result.current;
      actions.reset();
    });

    const [state] = result.current;
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
    expect(state.progress).toBeUndefined();
    expect(state.estimatedTimeRemaining).toBeUndefined();
    expect(state.status).toBeUndefined();
  });
});

describe('withLoadingState', () => {
  it('should handle successful operation', async () => {
    const { result } = renderHook(() => useLoadingState());

    const operation = async () => {
      return 'success';
    };

    const returnValue = await act(async () => {
      const [, actions] = result.current;
      return await withLoadingState(actions, operation);
    });

    expect(returnValue).toBe('success');
    const [state] = result.current;
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
  });

  it('should handle failed operation', async () => {
    const { result } = renderHook(() => useLoadingState());

    const operation = async () => {
      throw new Error('Operation failed');
    };

    const returnValue = await act(async () => {
      const [, actions] = result.current;
      return await withLoadingState(actions, operation);
    });

    expect(returnValue).toBeUndefined();
    const [state] = result.current;
    expect(state.isLoading).toBe(false);
    expect(state.error).toBe('Operation failed');
  });

  it('should use custom error message', async () => {
    const { result } = renderHook(() => useLoadingState());

    const operation = async () => {
      throw new Error('Internal error');
    };

    await act(async () => {
      const [, actions] = result.current;
      await withLoadingState(actions, operation, 'Custom error message');
    });

    const [state] = result.current;
    expect(state.error).toBe('Custom error message');
  });
});
