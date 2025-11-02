/**
 * Custom hook for fetching templates with pagination, virtualization support, and abort control
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import { getTemplates } from '../services/templatesService';
import { TemplateListItem, TemplateCategory } from '../types/templates';

export interface UseTemplatesPaginationOptions {
  category?: TemplateCategory | 'all';
  subCategory?: string;
  searchQuery?: string;
  pageSize?: number;
  initialPage?: number;
}

export interface UseTemplatesPaginationResult {
  templates: TemplateListItem[];
  loading: boolean;
  error: string | null;
  page: number;
  totalPages: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  loadNextPage: () => void;
  loadPreviousPage: () => void;
  setPage: (page: number) => void;
  reload: () => void;
}

export function useTemplatesPagination(
  options: UseTemplatesPaginationOptions = {}
): UseTemplatesPaginationResult {
  const { category, subCategory, searchQuery, pageSize = 50, initialPage = 1 } = options;

  const [templates, setTemplates] = useState<TemplateListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(initialPage);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [hasPreviousPage, setHasPreviousPage] = useState(false);

  const abortControllerRef = useRef<AbortController | null>(null);
  const loadCountRef = useRef(0);

  const loadTemplates = useCallback(
    async (pageNum: number) => {
      // Cancel previous request if it exists
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }

      // Create new abort controller for this request
      const abortController = new AbortController();
      abortControllerRef.current = abortController;

      // Track load count to prevent stale responses
      const currentLoadCount = ++loadCountRef.current;

      try {
        setLoading(true);
        setError(null);

        const response = await getTemplates(
          category !== 'all' && category ? category : undefined,
          subCategory,
          undefined,
          undefined,
          pageNum,
          pageSize,
          abortController.signal
        );

        // Check if this is still the latest request
        if (currentLoadCount !== loadCountRef.current) {
          return;
        }

        // Filter by search query if provided (client-side filtering)
        let filteredTemplates = response.items;
        if (searchQuery) {
          const query = searchQuery.toLowerCase();
          filteredTemplates = response.items.filter(
            (t) =>
              t.name.toLowerCase().includes(query) ||
              t.description.toLowerCase().includes(query) ||
              t.tags.some((tag) => tag.toLowerCase().includes(query))
          );
        }

        setTemplates(filteredTemplates);
        setTotalPages(response.totalPages);
        setTotalCount(response.totalCount);
        setHasNextPage(response.hasNextPage);
        setHasPreviousPage(response.hasPreviousPage);
      } catch (err: unknown) {
        // Ignore abort errors
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }

        // Check if this is still the latest request
        if (currentLoadCount !== loadCountRef.current) {
          return;
        }

        setError('Failed to load templates');
        console.error('Error loading templates:', err);
      } finally {
        // Check if this is still the latest request
        if (currentLoadCount === loadCountRef.current) {
          setLoading(false);
        }
      }
    },
    [category, subCategory, searchQuery, pageSize]
  );

  useEffect(() => {
    loadTemplates(page);

    // Cleanup function to abort on unmount or dependency change
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [loadTemplates, page]);

  const loadNextPage = useCallback(() => {
    if (hasNextPage) {
      setPage((prev) => prev + 1);
    }
  }, [hasNextPage]);

  const loadPreviousPage = useCallback(() => {
    if (hasPreviousPage) {
      setPage((prev) => Math.max(1, prev - 1));
    }
  }, [hasPreviousPage]);

  const reload = useCallback(() => {
    loadTemplates(page);
  }, [loadTemplates, page]);

  return {
    templates,
    loading,
    error,
    page,
    totalPages,
    totalCount,
    hasNextPage,
    hasPreviousPage,
    loadNextPage,
    loadPreviousPage,
    setPage,
    reload,
  };
}
