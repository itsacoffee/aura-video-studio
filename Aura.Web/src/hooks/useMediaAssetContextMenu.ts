/**
 * useMediaAssetContextMenu - Custom hook for media asset context menu interactions
 *
 * Provides a React-friendly interface for triggering context menus on media assets
 * in the Media Library and listening for context menu action callbacks.
 */

import { useCallback } from 'react';
import type { MediaAssetMenuData } from '../types/electron-context-menu';
import { useContextMenu, useContextMenuAction } from './useContextMenu';

export interface MediaAssetContextMenuCallbacks {
  onAddToTimeline: (assetId: string) => void;
  onPreview: (assetId: string) => void;
  onRename: (assetId: string, newName: string) => void;
  onToggleFavorite: (assetId: string) => void;
  onDelete: (assetId: string) => void;
  onShowProperties: (assetId: string) => void;
}

export interface MediaAssetForContextMenu {
  id: string;
  type: 'video' | 'audio' | 'image';
  filePath?: string;
  isFavorite?: boolean;
  tags?: string[];
}

/**
 * Hook to integrate context menu functionality for media assets
 *
 * @param callbacks - Object containing callback functions for each context menu action
 * @returns A function to call with the mouse event and asset data to show the context menu
 */
export function useMediaAssetContextMenu(callbacks: MediaAssetContextMenuCallbacks) {
  const { onAddToTimeline, onPreview, onRename, onToggleFavorite, onDelete, onShowProperties } =
    callbacks;

  const showContextMenu = useContextMenu<MediaAssetMenuData>('media-asset');

  const handleContextMenu = useCallback(
    (e: React.MouseEvent, asset: MediaAssetForContextMenu) => {
      const menuData: MediaAssetMenuData = {
        assetId: asset.id,
        assetType: asset.type,
        filePath: asset.filePath || '',
        isFavorite: asset.isFavorite || false,
        tags: asset.tags || [],
      };
      showContextMenu(e, menuData);
    },
    [showContextMenu]
  );

  // Register action handlers for context menu callbacks
  useContextMenuAction<MediaAssetMenuData>(
    'media-asset',
    'onAddToTimeline',
    useCallback(
      (data: MediaAssetMenuData) => {
        console.info('Adding asset to timeline:', data.assetId);
        onAddToTimeline(data.assetId);
      },
      [onAddToTimeline]
    )
  );

  useContextMenuAction<MediaAssetMenuData>(
    'media-asset',
    'onPreview',
    useCallback(
      (data: MediaAssetMenuData) => {
        console.info('Previewing asset:', data.assetId);
        onPreview(data.assetId);
      },
      [onPreview]
    )
  );

  useContextMenuAction<MediaAssetMenuData>(
    'media-asset',
    'onRename',
    useCallback(
      (data: MediaAssetMenuData) => {
        // In a real implementation, this would show a modal dialog
        // Using prompt as a fallback for now
        const newName = window.prompt('Enter new name:');
        if (newName && newName.trim()) {
          console.info('Renaming asset:', data.assetId, 'to', newName);
          onRename(data.assetId, newName.trim());
        }
      },
      [onRename]
    )
  );

  useContextMenuAction<MediaAssetMenuData>(
    'media-asset',
    'onToggleFavorite',
    useCallback(
      (data: MediaAssetMenuData) => {
        console.info('Toggling favorite for asset:', data.assetId);
        onToggleFavorite(data.assetId);
      },
      [onToggleFavorite]
    )
  );

  useContextMenuAction<MediaAssetMenuData>(
    'media-asset',
    'onDelete',
    useCallback(
      (data: MediaAssetMenuData) => {
        // Using confirm as a fallback; in a real implementation this would use a dialog component
        const shouldDelete = window.confirm('Delete this asset from library?');
        if (shouldDelete) {
          console.info('Deleting asset:', data.assetId);
          onDelete(data.assetId);
        }
      },
      [onDelete]
    )
  );

  useContextMenuAction<MediaAssetMenuData>(
    'media-asset',
    'onProperties',
    useCallback(
      (data: MediaAssetMenuData) => {
        console.info('Showing properties for asset:', data.assetId);
        onShowProperties(data.assetId);
      },
      [onShowProperties]
    )
  );

  return handleContextMenu;
}
