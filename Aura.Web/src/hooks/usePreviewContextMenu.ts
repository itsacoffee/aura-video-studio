/**
 * usePreviewContextMenu - Custom hook for preview window context menu interactions
 *
 * Provides a React-friendly interface for triggering context menus on the video preview
 * window and listening for context menu action callbacks.
 */

import { useCallback } from 'react';
import type { PreviewWindowMenuData } from '../types/electron-context-menu';
import { useContextMenu, useContextMenuAction } from './useContextMenu';

/**
 * Hook to integrate context menu functionality for the video preview window
 *
 * @param onTogglePlayback - Callback to toggle video playback
 * @param onAddMarker - Callback to add a marker at the specified time
 * @param onExportFrame - Callback to export the frame at the specified time
 * @param onSetZoom - Callback to set the zoom level
 * @returns A function to call with the mouse event and preview state to show the context menu
 */
export function usePreviewContextMenu(
  onTogglePlayback: () => void,
  onAddMarker: (time: number) => void,
  onExportFrame: (time: number) => void,
  onSetZoom: (zoom: number | 'fit') => void
) {
  const showContextMenu = useContextMenu<PreviewWindowMenuData>('preview-window');

  const handleContextMenu = useCallback(
    (
      e: React.MouseEvent,
      currentTime: number,
      duration: number,
      isPlaying: boolean,
      zoom: number
    ) => {
      const menuData: PreviewWindowMenuData = {
        currentTime,
        duration,
        isPlaying,
        zoom,
      };
      showContextMenu(e, menuData);
    },
    [showContextMenu]
  );

  // Register action handlers for context menu callbacks
  useContextMenuAction<PreviewWindowMenuData>(
    'preview-window',
    'onTogglePlayback',
    useCallback(() => {
      console.info('Toggle playback via context menu');
      onTogglePlayback();
    }, [onTogglePlayback])
  );

  useContextMenuAction<PreviewWindowMenuData>(
    'preview-window',
    'onAddMarker',
    useCallback(
      (data: PreviewWindowMenuData) => {
        console.info('Adding marker at time:', data.currentTime);
        onAddMarker(data.currentTime);
      },
      [onAddMarker]
    )
  );

  useContextMenuAction<PreviewWindowMenuData>(
    'preview-window',
    'onExportFrame',
    useCallback(
      (data: PreviewWindowMenuData) => {
        console.info('Exporting frame at time:', data.currentTime);
        onExportFrame(data.currentTime);
      },
      [onExportFrame]
    )
  );

  useContextMenuAction<PreviewWindowMenuData>(
    'preview-window',
    'onSetZoom',
    useCallback(
      (data: PreviewWindowMenuData & { actionArgs?: unknown[] }) => {
        // The zoom value is passed as an action argument from the menu
        const zoomArg = data.actionArgs?.[0];
        const newZoom = typeof zoomArg === 'number' ? zoomArg : zoomArg === 'fit' ? 'fit' : 1.0;
        console.info('Setting zoom to:', newZoom);
        onSetZoom(newZoom);
      },
      [onSetZoom]
    )
  );

  return handleContextMenu;
}
