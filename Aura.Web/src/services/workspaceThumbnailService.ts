/**
 * Workspace Thumbnail Service
 * Manages storage and retrieval of workspace thumbnails
 */

import type { WorkspaceThumbnailMetadata } from '../types/workspaceThumbnail.types';

const STORAGE_KEY = 'aura-workspace-thumbnails';
const MAX_THUMBNAIL_SIZE = 100 * 1024; // 100KB max per thumbnail

/**
 * Get thumbnail metadata for a workspace
 */
export function getWorkspaceThumbnail(workspaceId: string): WorkspaceThumbnailMetadata | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return null;
    }

    const thumbnails: Record<string, WorkspaceThumbnailMetadata> = JSON.parse(stored);
    return thumbnails[workspaceId] || null;
  } catch (error) {
    console.error('Error loading thumbnail:', error);
    return null;
  }
}

/**
 * Get all workspace thumbnails
 */
export function getAllWorkspaceThumbnails(): Record<string, WorkspaceThumbnailMetadata> {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return {};
    }
    return JSON.parse(stored);
  } catch (error) {
    console.error('Error loading thumbnails:', error);
    return {};
  }
}

/**
 * Save thumbnail metadata for a workspace
 */
export function saveWorkspaceThumbnail(
  workspaceId: string,
  thumbnailDataUrl: string,
  isCustom: boolean = false
): void {
  try {
    // Validate thumbnail size
    if (thumbnailDataUrl.length > MAX_THUMBNAIL_SIZE) {
      console.warn('Thumbnail too large, skipping save');
      return;
    }

    const thumbnails = getAllWorkspaceThumbnails();
    const now = new Date().toISOString();

    thumbnails[workspaceId] = {
      workspaceId,
      thumbnailDataUrl,
      isCustom,
      createdAt: thumbnails[workspaceId]?.createdAt || now,
      updatedAt: now,
    };

    localStorage.setItem(STORAGE_KEY, JSON.stringify(thumbnails));
  } catch (error) {
    console.error('Error saving thumbnail:', error);
    // If we hit quota error, try clearing old thumbnails
    if (error instanceof Error && error.name === 'QuotaExceededError') {
      cleanupOldThumbnails();
      // Try again
      try {
        const thumbnails = getAllWorkspaceThumbnails();
        const now = new Date().toISOString();
        thumbnails[workspaceId] = {
          workspaceId,
          thumbnailDataUrl,
          isCustom,
          createdAt: thumbnails[workspaceId]?.createdAt || now,
          updatedAt: now,
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(thumbnails));
      } catch {
        console.error('Failed to save thumbnail after cleanup');
      }
    }
  }
}

/**
 * Delete thumbnail for a workspace
 */
export function deleteWorkspaceThumbnail(workspaceId: string): void {
  try {
    const thumbnails = getAllWorkspaceThumbnails();
    delete thumbnails[workspaceId];
    localStorage.setItem(STORAGE_KEY, JSON.stringify(thumbnails));
  } catch (error) {
    console.error('Error deleting thumbnail:', error);
  }
}

/**
 * Clean up orphaned thumbnails (thumbnails for workspaces that no longer exist)
 */
export function cleanupOrphanedThumbnails(validWorkspaceIds: string[]): void {
  try {
    const thumbnails = getAllWorkspaceThumbnails();
    const validIdSet = new Set(validWorkspaceIds);
    let changed = false;

    Object.keys(thumbnails).forEach((id) => {
      if (!validIdSet.has(id)) {
        delete thumbnails[id];
        changed = true;
      }
    });

    if (changed) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(thumbnails));
    }
  } catch (error) {
    console.error('Error cleaning up thumbnails:', error);
  }
}

/**
 * Clean up old thumbnails to free space
 */
function cleanupOldThumbnails(): void {
  try {
    const thumbnails = getAllWorkspaceThumbnails();
    const entries = Object.entries(thumbnails);

    // Sort by updatedAt, keep most recent 50%
    entries.sort((a, b) => {
      const dateA = new Date(a[1].updatedAt).getTime();
      const dateB = new Date(b[1].updatedAt).getTime();
      return dateB - dateA;
    });

    const keepCount = Math.ceil(entries.length / 2);
    const kept = entries.slice(0, keepCount);

    const newThumbnails: Record<string, WorkspaceThumbnailMetadata> = {};
    kept.forEach(([id, meta]) => {
      newThumbnails[id] = meta;
    });

    localStorage.setItem(STORAGE_KEY, JSON.stringify(newThumbnails));
  } catch (error) {
    console.error('Error during thumbnail cleanup:', error);
  }
}

/**
 * Get total storage size used by thumbnails
 */
export function getThumbnailStorageSize(): number {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return 0;
    }
    return new Blob([stored]).size;
  } catch {
    return 0;
  }
}
