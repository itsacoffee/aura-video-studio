/**
 * Hook for fetching paginated templates with abort support
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import {
  TemplateListItem,
  TemplateCategory,
  PaginatedTemplatesResponse,
} from '../types/templates';
import { getTemplatesPaginated } from '../services/templatesService';

export interface UsePaginatedTemplatesOptions {
  page: number;
  pageSize: number;
  category?: TemplateCategory | 'all';
  subCategory?: string;
  systemOnly?: boolean;
  communityOnly?: boolean;
  enabled?: boolean;
}

export interface UsePaginatedTemplatesResult {
  templates: TemplateListItem[];
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

/**
 * Custom hook for fetching paginated templates with automatic abort on unmount or parameter changes
 */
export function usePaginatedTemplates(
  options: UsePaginatedTemplatesOptions
): UsePaginatedTemplatesResult {
  const {
    page,
    pageSize,
    category,
    subCategory,
    systemOnly,
    communityOnly,
    enabled = true,
  } = options;

  const [templates, setTemplates] = useState<TemplateListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [hasPreviousPage, setHasPreviousPage] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const abortControllerRef = useRef<AbortController | null>(null);

  const fetchTemplates = useCallback(async () => {
    if (!enabled) return;

    // Cancel previous request if exists
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Create new abort controller for this request
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    try {
      setLoading(true);
      setError(null);

      // Map 'all' category to undefined
      const categoryFilter =
        category === 'all' || !category ? undefined : category;

      const response: PaginatedTemplatesResponse = await getTemplatesPaginated(
        page,
        pageSize,
        categoryFilter,
        subCategory,
        systemOnly,
        communityOnly,
        abortController.signal
      );

      // Only update state if this request wasn't aborted
      if (!abortController.signal.aborted) {
        setTemplates(response.items);
        setTotalCount(response.totalCount);
        setTotalPages(response.totalPages);
        setHasNextPage(response.hasNextPage);
        setHasPreviousPage(response.hasPreviousPage);
      }
    } catch (err: unknown) {
      // Only set error if request wasn't aborted
      if (abortController.signal.aborted) {
        return;
      }

      const errorMessage =
        err instanceof Error ? err.message : 'Failed to load templates';
      setError(errorMessage);
      console.error('Error loading templates:', err);
    } finally {
      // Only set loading to false if request wasn't aborted
      if (!abortController.signal.aborted) {
        setLoading(false);
      }
    }
  }, [
    page,
    pageSize,
    category,
    subCategory,
    systemOnly,
    communityOnly,
    enabled,
  ]);

  useEffect(() => {
    fetchTemplates();

    // Cleanup: abort request on unmount or when dependencies change
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [fetchTemplates]);

  return {
    templates,
    totalCount,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    loading,
    error,
    refetch: fetchTemplates,
  };
}
