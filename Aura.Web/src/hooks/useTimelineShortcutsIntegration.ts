/**
 * Timeline Keyboard Shortcuts Integration Hook
 * Connects keyboard shortcuts to timeline state and undo/redo system
 */

import { useCallback, useEffect } from 'react';
import {
  DeleteClipsCommand,
  RippleDeleteClipsCommand,
  AddMarkerCommand,
  SplitClipCommand,
  ToggleRippleEditCommand,
} from '../commands/timelineCommands';
import { BatchCommandImpl } from '../services/commandHistory';
import { useTimelineStore } from '../state/timeline';
import { useUndoManager } from '../state/undoManager';

export interface TimelineShortcutsConfig {
  enabled?: boolean;
  onSave?: () => void;
  onShowShortcuts?: () => void;
}

/**
 * Hook to integrate timeline keyboard shortcuts with state and undo/redo
 */
export function useTimelineShortcutsIntegration(config: TimelineShortcutsConfig = {}) {
  const { enabled = true, onSave, onShowShortcuts } = config;
  const { execute, undo, redo } = useUndoManager();
  const {
    selectedClipIds,
    currentTime,
    setCurrentTime,
    setPlaying,
    isPlaying,
    setInPoint,
    setOutPoint,
    zoom,
    setZoom,
    tracks,
  } = useTimelineStore();

  const handlePlayPause = useCallback(() => {
    setPlaying(!isPlaying);
  }, [isPlaying, setPlaying]);

  const handleFrameBackward = useCallback(() => {
    const frameTime = 1 / 30;
    setCurrentTime(Math.max(0, currentTime - frameTime));
  }, [currentTime, setCurrentTime]);

  const handleFrameForward = useCallback(() => {
    const frameTime = 1 / 30;
    setCurrentTime(currentTime + frameTime);
  }, [currentTime, setCurrentTime]);

  const handleSecondBackward = useCallback(() => {
    setCurrentTime(Math.max(0, currentTime - 1));
  }, [currentTime, setCurrentTime]);

  const handleSecondForward = useCallback(() => {
    setCurrentTime(currentTime + 1);
  }, [currentTime, setCurrentTime]);

  const handleJumpToStart = useCallback(() => {
    setCurrentTime(0);
  }, [setCurrentTime]);

  const handleJumpToEnd = useCallback(() => {
    let maxTime = 0;
    tracks.forEach((track) => {
      track.clips.forEach((clip) => {
        const clipEnd = clip.timelineStart + (clip.sourceOut - clip.sourceIn);
        if (clipEnd > maxTime) {
          maxTime = clipEnd;
        }
      });
    });
    setCurrentTime(maxTime);
  }, [tracks, setCurrentTime]);

  const handleSetInPoint = useCallback(() => {
    setInPoint(currentTime);
  }, [currentTime, setInPoint]);

  const handleSetOutPoint = useCallback(() => {
    setOutPoint(currentTime);
  }, [currentTime, setOutPoint]);

  const handleClearInOut = useCallback(() => {
    setInPoint(undefined);
    setOutPoint(undefined);
  }, [setInPoint, setOutPoint]);

  const handleDeleteSelected = useCallback(async () => {
    if (selectedClipIds.length === 0) return;

    const command = new DeleteClipsCommand(selectedClipIds, useTimelineStore);
    await execute(command);
  }, [selectedClipIds, execute]);

  const handleRippleDeleteSelected = useCallback(async () => {
    if (selectedClipIds.length === 0) return;

    const command = new RippleDeleteClipsCommand(selectedClipIds, useTimelineStore);
    await execute(command);
  }, [selectedClipIds, execute]);

  const handleSplitAtPlayhead = useCallback(async () => {
    if (selectedClipIds.length === 0) return;

    const batch = new BatchCommandImpl('Split clips at playhead');

    selectedClipIds.forEach((clipId) => {
      tracks.forEach((track) => {
        const clip = track.clips.find((c) => c.id === clipId);
        if (clip) {
          const clipStart = clip.timelineStart;
          const clipEnd = clipStart + (clip.sourceOut - clip.sourceIn);
          if (currentTime > clipStart && currentTime < clipEnd) {
            const command = new SplitClipCommand(clipId, currentTime, useTimelineStore);
            batch.addCommand(command);
          }
        }
      });
    });

    if (batch.getCommands().length > 0) {
      await execute(batch);
    }
  }, [selectedClipIds, currentTime, tracks, execute]);

  const handleAddMarker = useCallback(async () => {
    const marker = {
      id: `marker_${Date.now()}`,
      title: 'Marker',
      time: currentTime,
    };

    const command = new AddMarkerCommand(marker, useTimelineStore);
    await execute(command);
  }, [currentTime, execute]);

  const handleToggleRippleEdit = useCallback(async () => {
    const command = new ToggleRippleEditCommand(useTimelineStore);
    await execute(command);
  }, [execute]);

  const handleZoomIn = useCallback(() => {
    setZoom(Math.min(200, zoom + 10));
  }, [zoom, setZoom]);

  const handleZoomOut = useCallback(() => {
    setZoom(Math.max(10, zoom - 10));
  }, [zoom, setZoom]);

  const handleSelectAll = useCallback(() => {
    const allClipIds: string[] = [];
    tracks.forEach((track) => {
      track.clips.forEach((clip) => {
        allClipIds.push(clip.id);
      });
    });
    useTimelineStore.getState().setSelectedClipIds(allClipIds);
  }, [tracks]);

  const handleDeselectAll = useCallback(() => {
    useTimelineStore.getState().clearSelection();
  }, []);

  const handleUndo = useCallback(async () => {
    await undo();
  }, [undo]);

  const handleRedo = useCallback(() => {
    redo();
  }, [redo]);

  const handleKeyDown = useCallback(
    // eslint-disable-next-line sonarjs/cognitive-complexity -- Comprehensive keyboard shortcut handler with many conditional branches for different key combinations
    (event: KeyboardEvent) => {
      if (!enabled) return;

      const target = event.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable) {
        return;
      }

      const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
      const ctrlOrCmd = isMac ? event.metaKey : event.ctrlKey;

      if (event.code === 'Space') {
        event.preventDefault();
        handlePlayPause();
        return;
      }

      if (event.key === 'ArrowLeft' && !event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handleFrameBackward();
        return;
      }

      if (event.key === 'ArrowRight' && !event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handleFrameForward();
        return;
      }

      if (event.key === 'ArrowLeft' && event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handleSecondBackward();
        return;
      }

      if (event.key === 'ArrowRight' && event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handleSecondForward();
        return;
      }

      if (event.key === 'Home') {
        event.preventDefault();
        handleJumpToStart();
        return;
      }

      if (event.key === 'End') {
        event.preventDefault();
        handleJumpToEnd();
        return;
      }

      if (event.key === 'i' && !ctrlOrCmd) {
        event.preventDefault();
        handleSetInPoint();
        return;
      }

      if (event.key === 'o' && !ctrlOrCmd) {
        event.preventDefault();
        handleSetOutPoint();
        return;
      }

      if (event.key === 'x' && !ctrlOrCmd) {
        event.preventDefault();
        handleClearInOut();
        return;
      }

      if ((event.key === 'Delete' || event.key === 'Backspace') && event.shiftKey) {
        event.preventDefault();
        handleRippleDeleteSelected();
        return;
      }

      if ((event.key === 'Delete' || event.key === 'Backspace') && !ctrlOrCmd) {
        event.preventDefault();
        handleDeleteSelected();
        return;
      }

      if (event.key === 's' && !ctrlOrCmd) {
        event.preventDefault();
        handleSplitAtPlayhead();
        return;
      }

      if (event.key === 'm' && !ctrlOrCmd) {
        event.preventDefault();
        handleAddMarker();
        return;
      }

      if (event.key === 'r' && ctrlOrCmd) {
        event.preventDefault();
        handleToggleRippleEdit();
        return;
      }

      if ((event.key === '=' || event.key === '+') && !ctrlOrCmd) {
        event.preventDefault();
        handleZoomIn();
        return;
      }

      if (event.key === '-' && !ctrlOrCmd) {
        event.preventDefault();
        handleZoomOut();
        return;
      }

      if (event.key === 'a' && ctrlOrCmd) {
        event.preventDefault();
        handleSelectAll();
        return;
      }

      if (event.key === 'd' && ctrlOrCmd && !event.shiftKey) {
        event.preventDefault();
        handleDeselectAll();
        return;
      }

      if (event.key === 'z' && ctrlOrCmd && !event.shiftKey) {
        event.preventDefault();
        handleUndo();
        return;
      }

      if (event.key === 'z' && ctrlOrCmd && event.shiftKey) {
        event.preventDefault();
        handleRedo();
        return;
      }

      if (event.key === 'y' && ctrlOrCmd) {
        event.preventDefault();
        handleRedo();
        return;
      }

      if (event.key === 's' && ctrlOrCmd) {
        event.preventDefault();
        onSave?.();
        return;
      }

      if (event.key === '?' && !ctrlOrCmd) {
        event.preventDefault();
        onShowShortcuts?.();
        return;
      }
    },
    [
      enabled,
      handlePlayPause,
      handleFrameBackward,
      handleFrameForward,
      handleSecondBackward,
      handleSecondForward,
      handleJumpToStart,
      handleJumpToEnd,
      handleSetInPoint,
      handleSetOutPoint,
      handleClearInOut,
      handleDeleteSelected,
      handleRippleDeleteSelected,
      handleSplitAtPlayhead,
      handleAddMarker,
      handleToggleRippleEdit,
      handleZoomIn,
      handleZoomOut,
      handleSelectAll,
      handleDeselectAll,
      handleUndo,
      handleRedo,
      onSave,
      onShowShortcuts,
    ]
  );

  useEffect(() => {
    if (!enabled) return;

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown, enabled]);

  return {
    handlePlayPause,
    handleFrameBackward,
    handleFrameForward,
    handleJumpToStart,
    handleJumpToEnd,
    handleDeleteSelected,
    handleRippleDeleteSelected,
    handleSplitAtPlayhead,
    handleAddMarker,
    handleSelectAll,
    handleDeselectAll,
    handleUndo,
    handleRedo,
  };
}
