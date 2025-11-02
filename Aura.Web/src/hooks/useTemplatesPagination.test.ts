/**
 * Tests for useTemplatesPagination hook
 */

import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as templatesService from '../services/templatesService';
import { TemplateCategory } from '../types/templates';
import { useTemplatesPagination } from './useTemplatesPagination';

vi.mock('../services/templatesService');

describe('useTemplatesPagination', () => {
  const mockTemplates = [
    {
      id: '1',
      name: 'Template 1',
      description: 'Description 1',
      category: TemplateCategory.YouTube,
      subCategory: 'Intro',
      previewImage: '/image1.jpg',
      previewVideo: '',
      tags: ['tag1', 'tag2'],
      usageCount: 100,
      rating: 4.5,
      isSystemTemplate: true,
      isCommunityTemplate: false,
    },
    {
      id: '2',
      name: 'Template 2',
      description: 'Description 2',
      category: TemplateCategory.SocialMedia,
      subCategory: 'Story',
      previewImage: '/image2.jpg',
      previewVideo: '',
      tags: ['tag3'],
      usageCount: 50,
      rating: 4.0,
      isSystemTemplate: true,
      isCommunityTemplate: false,
    },
  ];

  const mockResponse = {
    items: mockTemplates,
    page: 1,
    pageSize: 50,
    totalCount: 2,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should load templates on mount', async () => {
    vi.mocked(templatesService.getTemplates).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useTemplatesPagination());

    expect(result.current.loading).toBe(true);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.templates).toEqual(mockTemplates);
    expect(result.current.error).toBeNull();
    expect(result.current.totalCount).toBe(2);
    expect(result.current.totalPages).toBe(1);
  });

  it('should handle errors gracefully', async () => {
    vi.mocked(templatesService.getTemplates).mockRejectedValue(new Error('Failed to load'));

    const { result } = renderHook(() => useTemplatesPagination());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBe('Failed to load templates');
    expect(result.current.templates).toEqual([]);
  });

  it('should filter templates by search query', async () => {
    vi.mocked(templatesService.getTemplates).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useTemplatesPagination({ searchQuery: 'template 1' }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.templates).toHaveLength(1);
    expect(result.current.templates[0].name).toBe('Template 1');
  });

  it('should filter templates by tag', async () => {
    vi.mocked(templatesService.getTemplates).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useTemplatesPagination({ searchQuery: 'tag3' }));

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.templates).toHaveLength(1);
    expect(result.current.templates[0].tags).toContain('tag3');
  });

  it('should load next page when hasNextPage is true', async () => {
    const page1Response = {
      ...mockResponse,
      page: 1,
      hasNextPage: true,
      hasPreviousPage: false,
    };

    const page2Response = {
      ...mockResponse,
      page: 2,
      hasNextPage: false,
      hasPreviousPage: true,
    };

    vi.mocked(templatesService.getTemplates)
      .mockResolvedValueOnce(page1Response)
      .mockResolvedValueOnce(page2Response);

    const { result } = renderHook(() => useTemplatesPagination());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.page).toBe(1);
    expect(result.current.hasNextPage).toBe(true);

    result.current.loadNextPage();

    await waitFor(() => {
      expect(result.current.page).toBe(2);
    });

    expect(result.current.hasNextPage).toBe(false);
    expect(result.current.hasPreviousPage).toBe(true);
  });

  it('should abort previous request when loading new page', async () => {
    const abortSpy = vi.fn();
    const mockAbortController = {
      abort: abortSpy,
      signal: {} as AbortSignal,
    };

    global.AbortController = vi.fn(() => mockAbortController) as unknown as typeof AbortController;

    vi.mocked(templatesService.getTemplates).mockImplementation(
      () =>
        new Promise((resolve) => {
          setTimeout(() => resolve(mockResponse), 100);
        })
    );

    const { result, rerender } = renderHook(
      ({ category }) => useTemplatesPagination({ category }),
      {
        initialProps: { category: 'all' as const },
      }
    );

    // Change category to trigger new request
    rerender({ category: TemplateCategory.YouTube });

    await waitFor(() => {
      expect(abortSpy).toHaveBeenCalled();
    });
  });

  it('should reload templates when reload is called', async () => {
    vi.mocked(templatesService.getTemplates).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useTemplatesPagination());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(templatesService.getTemplates).toHaveBeenCalledTimes(1);

    result.current.reload();

    await waitFor(() => {
      expect(templatesService.getTemplates).toHaveBeenCalledTimes(2);
    });
  });

  it('should not load previous page when on first page', async () => {
    vi.mocked(templatesService.getTemplates).mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useTemplatesPagination());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.page).toBe(1);
    expect(result.current.hasPreviousPage).toBe(false);

    result.current.loadPreviousPage();

    await waitFor(() => {
      expect(result.current.page).toBe(1);
    });
  });

  it('should handle AbortError gracefully', async () => {
    const abortError = new Error('AbortError');
    abortError.name = 'AbortError';

    vi.mocked(templatesService.getTemplates).mockRejectedValue(abortError);

    const { result } = renderHook(() => useTemplatesPagination());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBeNull();
  });
});
