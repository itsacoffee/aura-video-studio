import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { assetService } from '../../services/assetService';
import { Asset, AssetType, AssetSource } from '../../types/assets';
import { useAssets } from '../useAssets';

vi.mock('../../services/assetService');
vi.mock('../../services/loggingService');

describe('useAssets', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should load assets successfully', async () => {
    const mockAssets: Asset[] = [
      {
        id: '1',
        title: 'Test Asset',
        description: 'Test Description',
        type: AssetType.Image,
        source: AssetSource.Uploaded,
        path: '/test/path.jpg',
        thumbnailPath: '/test/thumb.jpg',
        dateAdded: '2024-01-01T00:00:00Z',
        tags: [],
        metadata: {},
      },
    ];

    vi.spyOn(assetService, 'getAssets').mockResolvedValueOnce({
      assets: mockAssets,
      totalCount: 1,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });

    const { result } = renderHook(() => useAssets());

    expect(result.current.loading).toBe(true);
    expect(result.current.assets).toEqual([]);
    expect(result.current.error).toBeNull();

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.assets).toEqual(mockAssets);
    expect(result.current.totalCount).toBe(1);
    expect(result.current.error).toBeNull();
  });

  it('should handle empty array response', async () => {
    vi.spyOn(assetService, 'getAssets').mockResolvedValueOnce({
      assets: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      totalPages: 0,
    });

    const { result } = renderHook(() => useAssets());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.assets).toEqual([]);
    expect(result.current.totalCount).toBe(0);
    expect(result.current.error).toBeNull();
  });

  it('should handle error and set empty array', async () => {
    const mockError = new Error('Failed to load assets');
    vi.spyOn(assetService, 'getAssets').mockRejectedValueOnce(mockError);

    const { result } = renderHook(() => useAssets());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.assets).toEqual([]);
    expect(result.current.totalCount).toBe(0);
    expect(result.current.error).toEqual(mockError);
  });

  it('should retry loading assets when retry is called', async () => {
    const mockAssets: Asset[] = [
      {
        id: '1',
        title: 'Test Asset',
        description: 'Test Description',
        type: AssetType.Image,
        source: AssetSource.Uploaded,
        path: '/test/path.jpg',
        thumbnailPath: '/test/thumb.jpg',
        dateAdded: '2024-01-01T00:00:00Z',
        tags: [],
        metadata: {},
      },
    ];

    const getAssetsSpy = vi
      .spyOn(assetService, 'getAssets')
      .mockRejectedValueOnce(new Error('First call fails'))
      .mockResolvedValueOnce({
        assets: mockAssets,
        totalCount: 1,
        page: 1,
        pageSize: 50,
        totalPages: 1,
      });

    const { result } = renderHook(() => useAssets());

    // Wait for initial load to fail
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBeTruthy();
    expect(result.current.assets).toEqual([]);
    expect(getAssetsSpy).toHaveBeenCalledTimes(1);

    // Call retry
    result.current.retry();

    // Wait for retry to succeed
    await waitFor(() => {
      expect(result.current.assets).toEqual(mockAssets);
    });

    expect(result.current.error).toBeNull();
    expect(getAssetsSpy).toHaveBeenCalledTimes(2);
  });

  it('should handle malformed response gracefully', async () => {
    // Mock a response that doesn't have the expected structure
    vi.spyOn(assetService, 'getAssets').mockResolvedValueOnce({} as never);

    const { result } = renderHook(() => useAssets());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.assets).toEqual([]);
    expect(result.current.totalCount).toBe(0);
    expect(result.current.error).toBeNull();
  });

  it('should support filtering by query and type', async () => {
    const mockAssets: Asset[] = [
      {
        id: '1',
        title: 'Filtered Asset',
        description: 'Test Description',
        type: AssetType.Image,
        source: AssetSource.Uploaded,
        path: '/test/path.jpg',
        thumbnailPath: '/test/thumb.jpg',
        dateAdded: '2024-01-01T00:00:00Z',
        tags: [],
        metadata: {},
      },
    ];

    const getAssetsSpy = vi.spyOn(assetService, 'getAssets').mockResolvedValueOnce({
      assets: mockAssets,
      totalCount: 1,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });

    renderHook(() =>
      useAssets({
        query: 'test',
        type: AssetType.Image,
        page: 1,
        pageSize: 20,
      })
    );

    await waitFor(() => {
      expect(getAssetsSpy).toHaveBeenCalledWith('test', AssetType.Image, undefined, 1, 20);
    });
  });
});
