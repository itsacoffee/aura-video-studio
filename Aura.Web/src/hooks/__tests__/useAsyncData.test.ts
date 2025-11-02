import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { useAsyncData } from '../useAsyncData';

describe('useAsyncData', () => {
  it('should load data successfully', async () => {
    const mockData = { id: 1, name: 'Test' };
    const fetchFn = vi.fn(() => Promise.resolve(mockData));

    const { result } = renderHook(() => useAsyncData(fetchFn));

    expect(result.current.loading).toBe(true);
    expect(result.current.data).toBe(null);
    expect(result.current.error).toBe(null);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.data).toEqual(mockData);
    expect(result.current.error).toBe(null);
    expect(fetchFn).toHaveBeenCalledTimes(1);
  });

  it('should handle errors gracefully', async () => {
    const mockError = new Error('Test error');
    const fetchFn = vi.fn(() => Promise.reject(mockError));

    const { result } = renderHook(() => useAsyncData(fetchFn));

    expect(result.current.loading).toBe(true);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.data).toBe(null);
    expect(result.current.error).toEqual(mockError);
  });

  it('should use fallback data on error', async () => {
    const mockError = new Error('Test error');
    const fallbackData = { id: 0, name: 'Fallback' };
    const fetchFn = vi.fn(() => Promise.reject(mockError));

    const { result } = renderHook(() => useAsyncData(fetchFn, [], { fallbackData }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.data).toEqual(fallbackData);
    expect(result.current.error).toEqual(mockError);
  });

  it('should allow manual refetch', async () => {
    const mockData = { id: 1, name: 'Test' };
    const fetchFn = vi.fn(() => Promise.resolve(mockData));

    const { result } = renderHook(() => useAsyncData(fetchFn, [], { immediate: false }));

    expect(result.current.loading).toBe(false);
    expect(result.current.data).toBe(null);

    // Manually trigger fetch
    await result.current.refetch();

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.data).toEqual(mockData);
    expect(fetchFn).toHaveBeenCalledTimes(1);
  });

  it('should handle empty/null responses', async () => {
    const fetchFn = vi.fn(() => Promise.resolve(null));
    const fallbackData = [];

    const { result } = renderHook(() => useAsyncData(fetchFn, [], { fallbackData }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.data).toEqual(fallbackData);
    expect(result.current.error).toBe(null);
  });

  it('should call onSuccess callback', async () => {
    const mockData = { id: 1, name: 'Test' };
    const fetchFn = vi.fn(() => Promise.resolve(mockData));
    const onSuccess = vi.fn();

    renderHook(() => useAsyncData(fetchFn, [], { onSuccess }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledWith(mockData);
    });
  });

  it('should call onError callback', async () => {
    const mockError = new Error('Test error');
    const fetchFn = vi.fn(() => Promise.reject(mockError));
    const onError = vi.fn();

    renderHook(() => useAsyncData(fetchFn, [], { onError }));

    await waitFor(() => {
      expect(onError).toHaveBeenCalledWith(mockError);
    });
  });
});
