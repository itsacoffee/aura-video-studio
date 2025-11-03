/**
 * useWorkspaceThumbnails Hook
 * React hook for managing workspace thumbnails
 */

import { useState, useEffect, useCallback } from 'react';
import type { WorkspaceLayout } from '../services/workspaceLayoutService';
import {
  getWorkspaceThumbnail,
  saveWorkspaceThumbnail,
  deleteWorkspaceThumbnail,
  getAllWorkspaceThumbnails,
} from '../services/workspaceThumbnailService';
import type { WorkspaceThumbnailMetadata } from '../types/workspaceThumbnail.types';
import { generateWorkspaceThumbnail } from '../utils/workspaceThumbnailGenerator';

export interface UseWorkspaceThumbnailsResult {
  thumbnails: Record<string, WorkspaceThumbnailMetadata>;
  getThumbnail: (workspaceId: string) => WorkspaceThumbnailMetadata | null;
  generateThumbnail: (workspace: WorkspaceLayout) => Promise<string>;
  saveThumbnail: (workspaceId: string, dataUrl: string, isCustom?: boolean) => void;
  removeThumbnail: (workspaceId: string) => void;
  refreshThumbnails: () => void;
  isGenerating: boolean;
}

/**
 * Hook for managing workspace thumbnails
 */
export function useWorkspaceThumbnails(): UseWorkspaceThumbnailsResult {
  const [thumbnails, setThumbnails] = useState<Record<string, WorkspaceThumbnailMetadata>>({});
  const [isGenerating, setIsGenerating] = useState(false);

  // Load thumbnails on mount
  useEffect(() => {
    setThumbnails(getAllWorkspaceThumbnails());
  }, []);

  // Refresh thumbnails from storage
  const refreshThumbnails = useCallback(() => {
    setThumbnails(getAllWorkspaceThumbnails());
  }, []);

  // Get thumbnail for a workspace
  const getThumbnail = useCallback(
    (workspaceId: string): WorkspaceThumbnailMetadata | null => {
      return thumbnails[workspaceId] || getWorkspaceThumbnail(workspaceId);
    },
    [thumbnails]
  );

  // Generate thumbnail for a workspace
  const generateThumbnail = useCallback(async (workspace: WorkspaceLayout): Promise<string> => {
    setIsGenerating(true);
    try {
      // Generate thumbnail on next frame to avoid blocking UI
      await new Promise((resolve) => setTimeout(resolve, 0));
      return generateWorkspaceThumbnail(workspace);
    } finally {
      setIsGenerating(false);
    }
  }, []);

  // Save thumbnail
  const saveThumbnail = useCallback(
    (workspaceId: string, dataUrl: string, isCustom: boolean = false): void => {
      saveWorkspaceThumbnail(workspaceId, dataUrl, isCustom);
      refreshThumbnails();
    },
    [refreshThumbnails]
  );

  // Remove thumbnail
  const removeThumbnail = useCallback(
    (workspaceId: string): void => {
      deleteWorkspaceThumbnail(workspaceId);
      refreshThumbnails();
    },
    [refreshThumbnails]
  );

  return {
    thumbnails,
    getThumbnail,
    generateThumbnail,
    saveThumbnail,
    removeThumbnail,
    refreshThumbnails,
    isGenerating,
  };
}

/**
 * Hook for managing a single workspace thumbnail
 */
export function useWorkspaceThumbnail(workspace: WorkspaceLayout | null) {
  const [thumbnailUrl, setThumbnailUrl] = useState<string | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const { getThumbnail, generateThumbnail, saveThumbnail } = useWorkspaceThumbnails();

  useEffect(() => {
    if (!workspace) {
      setThumbnailUrl(null);
      return;
    }

    const existing = getThumbnail(workspace.id);
    if (existing?.thumbnailDataUrl) {
      setThumbnailUrl(existing.thumbnailDataUrl);
    } else {
      // Generate thumbnail if not exists
      setIsGenerating(true);
      generateThumbnail(workspace)
        .then((dataUrl) => {
          setThumbnailUrl(dataUrl);
          saveThumbnail(workspace.id, dataUrl, false);
        })
        .catch((error) => {
          console.error('Failed to generate thumbnail:', error);
        })
        .finally(() => {
          setIsGenerating(false);
        });
    }
  }, [workspace, getThumbnail, generateThumbnail, saveThumbnail]);

  return { thumbnailUrl, isGenerating };
}
