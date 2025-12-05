/**
 * Timeline Component
 *
 * Professional timeline with full editing capabilities:
 * - Track and clip management with visual feedback
 * - Functional split, copy, delete, and add track buttons
 * - Keyboard shortcuts (S for split, Cmd/Ctrl+D duplicate, Delete/Backspace, Cmd/Ctrl+T for transitions)
 * - Zoom that actually affects timeline scale
 * - Scroll-to-zoom with Cmd/Ctrl + mouse wheel
 * - Clip rendering with thumbnails and waveforms
 * - Selection support with multi-select
 * - Undo/redo support
 * - Timeline markers support
 * - Transitions between clips
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  Badge,
  mergeClasses,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Divider,
} from '@fluentui/react-components';
import {
  Video24Regular,
  MusicNote224Regular,
  TextT24Regular,
  Cut24Regular,
  Copy24Regular,
  Delete24Regular,
  Add24Regular,
  ZoomIn24Regular,
  ZoomOut24Regular,
  ArrowUndo24Regular,
  ArrowRedo24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
  Speaker224Regular,
  SpeakerMute24Regular,
  ChevronDown16Regular,
  Image24Regular,
} from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import { useState, useCallback, useRef, useEffect, useMemo } from 'react';
import type { FC, MouseEvent as ReactMouseEvent, WheelEvent } from 'react';
import { useOpenCutKeyframesStore } from '../../stores/opencutKeyframes';
import { useOpenCutMarkersStore } from '../../stores/opencutMarkers';
import { useOpenCutMediaStore } from '../../stores/opencutMedia';
import { useOpenCutPlaybackStore } from '../../stores/opencutPlayback';
import { useOpenCutProjectStore } from '../../stores/opencutProject';
import {
  useOpenCutTimelineStore,
  type ClipType,
  type TimelineClip,
  type TimelineGap,
} from '../../stores/opencutTimeline';
import { useOpenCutTransitionsStore } from '../../stores/opencutTransitions';
import { openCutTokens } from '../../styles/designTokens';
import type { MarkerType } from '../../types/opencut';
import { MarkerTrack } from './Markers';
import { GapIndicator, SnapIndicator, TimelineToolbar } from './Timeline/index';
import { TransitionHandle } from './Transitions';
import { ClipWaveform } from './Waveform';

export interface TimelineProps {
  className?: string;
  onResize?: (height: number) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    position: 'relative',
    outline: 'none',
  },
  resizeHandle: {
    position: 'absolute',
    top: '-4px',
    left: 0,
    right: 0,
    height: '8px',
    cursor: 'ns-resize',
    zIndex: openCutTokens.zIndex.sticky,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    ':hover::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
    '::after': {
      content: '""',
      width: '48px',
      height: '3px',
      backgroundColor: tokens.colorNeutralStroke3,
      borderRadius: openCutTokens.radius.md,
      opacity: 0,
      transition: `opacity ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}, background-color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    },
  },
  resizeHandleActive: {
    '::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '52px',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  zoomControls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginRight: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  undoRedoControls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginRight: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  magneticToolbar: {
    marginRight: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  headerDivider: {
    height: '24px',
    margin: `0 ${tokens.spacingHorizontalS}`,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    position: 'relative',
  },
  rulerArea: {
    display: 'flex',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
    minHeight: '32px',
  },
  rulerLabels: {
    width: '140px',
    flexShrink: 0,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  rulerScrollable: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
  },
  ruler: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
    display: 'flex',
    alignItems: 'flex-end',
  },
  rulerMark: {
    position: 'absolute',
    bottom: 0,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
  },
  rulerMarkLine: {
    width: '1px',
    backgroundColor: tokens.colorNeutralStroke3,
  },
  rulerMarkLabel: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    marginTop: tokens.spacingVerticalXXS,
  },
  tracksScrollable: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'auto',
  },
  track: {
    display: 'flex',
    minHeight: '56px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  trackSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  trackLocked: {
    opacity: 0.6,
  },
  trackLabel: {
    width: '140px',
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `0 ${tokens.spacingHorizontalS}`,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  trackLabelIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '16px',
  },
  trackLabelText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontWeight: tokens.fontWeightMedium,
    flex: 1,
    minWidth: 0,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  trackControls: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
  },
  trackControlButton: {
    minWidth: '20px',
    minHeight: '20px',
    padding: '2px',
  },
  trackContentScrollable: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
  },
  trackContent: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
    backgroundColor: tokens.colorNeutralBackground4,
  },
  clip: {
    position: 'absolute',
    top: '4px',
    bottom: '4px',
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    transition: 'box-shadow 100ms ease-out, transform 100ms ease-out',
    ':hover': {
      boxShadow: tokens.shadow8,
      transform: 'translateY(-1px)',
    },
  },
  clipVideo: {
    backgroundColor: tokens.colorPaletteBlueBorderActive,
    border: `1px solid ${tokens.colorPaletteBlueBackground2}`,
  },
  clipAudio: {
    backgroundColor: tokens.colorPaletteGreenBorderActive,
    border: `1px solid ${tokens.colorPaletteGreenBackground3}`,
  },
  clipImage: {
    backgroundColor: tokens.colorPalettePurpleBorderActive,
    border: `1px solid ${tokens.colorPalettePurpleBackground2}`,
  },
  clipText: {
    backgroundColor: tokens.colorPaletteYellowBorderActive,
    border: `1px solid ${tokens.colorPaletteYellowBackground3}`,
  },
  clipSelected: {
    boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}`,
    transform: 'translateY(-1px)',
  },
  clipThumbnail: {
    width: '48px',
    height: '100%',
    objectFit: 'cover',
    flexShrink: 0,
  },
  clipWaveform: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    opacity: 0.6,
    pointerEvents: 'none',
  },
  clipInfo: {
    flex: 1,
    padding: `0 ${tokens.spacingHorizontalXS}`,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    minWidth: 0,
    zIndex: 1,
  },
  clipName: {
    fontSize: tokens.fontSizeBase100,
    color: 'white',
    fontWeight: tokens.fontWeightMedium,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  clipDuration: {
    fontSize: '10px',
    color: 'rgba(255,255,255,0.8)',
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  clipTrimHandle: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '6px',
    backgroundColor: 'rgba(255,255,255,0.3)',
    cursor: 'ew-resize',
    opacity: 0,
    transition: 'opacity 100ms ease-out',
    ':hover': {
      opacity: 1,
      backgroundColor: 'rgba(255,255,255,0.5)',
    },
  },
  clipTrimHandleLeft: {
    left: 0,
    borderRadius: `${tokens.borderRadiusMedium} 0 0 ${tokens.borderRadiusMedium}`,
  },
  clipTrimHandleRight: {
    right: 0,
    borderRadius: `0 ${tokens.borderRadiusMedium} ${tokens.borderRadiusMedium} 0`,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: openCutTokens.colors.playhead,
    zIndex: openCutTokens.zIndex.sticky - 5,
    pointerEvents: 'none',
  },
  playheadHandle: {
    position: 'absolute',
    top: '-2px',
    left: '-7px',
    width: '16px',
    height: '16px',
    backgroundColor: openCutTokens.colors.playhead,
    borderRadius: '4px 4px 50% 50%',
    boxShadow: openCutTokens.shadows.sm,
    pointerEvents: 'auto',
    cursor: 'ew-resize',
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      transform: 'scale(1.1)',
    },
  },
  controlButton: {
    minWidth: '36px',
    minHeight: '36px',
  },
  snapIndicator: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '1px',
    backgroundColor: openCutTokens.colors.snap,
    zIndex: openCutTokens.zIndex.sticky - 6,
    pointerEvents: 'none',
  },
});

function formatTimeRuler(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

const TRACK_TYPE_ICONS: Record<ClipType, React.ReactNode> = {
  video: <Video24Regular />,
  audio: <MusicNote224Regular />,
  image: <Image24Regular />,
  text: <TextT24Regular />,
};

/** Minimum samples for waveform generation */
const WAVEFORM_MIN_SAMPLES = 50;

/** Maximum samples for waveform generation */
const WAVEFORM_MAX_SAMPLES = 500;

/** Pixels per sample for waveform detail calculation */
const WAVEFORM_PIXELS_PER_SAMPLE = 2;

export const Timeline: FC<TimelineProps> = ({ className, onResize }) => {
  const styles = useStyles();
  const playbackStore = useOpenCutPlaybackStore();
  const projectStore = useOpenCutProjectStore();
  const timelineStore = useOpenCutTimelineStore();
  const keyframesStore = useOpenCutKeyframesStore();
  const markersStore = useOpenCutMarkersStore();
  const mediaStore = useOpenCutMediaStore();
  const transitionsStore = useOpenCutTransitionsStore();

  const [isResizing, setIsResizing] = useState(false);
  const [activeSnapPoint, setActiveSnapPoint] = useState<number | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const startYRef = useRef(0);
  const startHeightRef = useRef(0);

  const {
    tracks,
    clips,
    selectedClipIds,
    selectedTrackId,
    zoom,
    snapEnabled,
    magneticTimelineEnabled,
    snapToClips,
    findGaps,
    closeGap,
    findNearestSnapPoint,
  } = timelineStore;
  const { selectedMarkerId, getFilteredMarkers } = markersStore;
  const { selectedTransitionId } = transitionsStore;
  const duration = playbackStore.duration;
  const currentTime = playbackStore.currentTime;

  // Calculate timeline width based on zoom
  const pixelsPerSecond = 100 * zoom;
  const totalWidth = Math.max(duration * pixelsPerSecond, 800);

  const playheadPosition = duration > 0 ? (currentTime / duration) * totalWidth : 0;

  // Get filtered markers for display
  const visibleMarkers = getFilteredMarkers();

  // Generate time ruler marks based on zoom level
  const rulerMarks = useMemo(() => {
    const marks: { time: number; position: number; major: boolean }[] = [];
    let markInterval = 1;

    if (zoom < 0.5) markInterval = 10;
    else if (zoom < 1) markInterval = 5;
    else if (zoom < 2) markInterval = 2;
    else markInterval = 1;

    for (let t = 0; t <= duration; t += markInterval) {
      marks.push({
        time: t,
        position: t * pixelsPerSecond,
        major: t % (markInterval * 2) === 0,
      });
    }
    return marks;
  }, [duration, zoom, pixelsPerSecond]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Only handle if timeline is focused or no other input is focused
      const activeElement = document.activeElement;
      const isInputFocused =
        activeElement instanceof HTMLInputElement ||
        activeElement instanceof HTMLTextAreaElement ||
        activeElement instanceof HTMLSelectElement;

      if (isInputFocused) return;

      const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
      const cmdOrCtrl = isMac ? e.metaKey : e.ctrlKey;

      switch (e.key.toLowerCase()) {
        case 's':
          if (!cmdOrCtrl) {
            e.preventDefault();
            // Split at playhead
            if (selectedClipIds.length > 0) {
              timelineStore.splitSelectedClips(currentTime);
            }
          }
          break;
        case 'd':
          if (cmdOrCtrl) {
            e.preventDefault();
            // Duplicate
            timelineStore.duplicateSelectedClips();
          }
          break;
        case 'delete':
        case 'backspace':
          if (!cmdOrCtrl) {
            e.preventDefault();
            // Delete selected keyframes first if any, otherwise delete clips
            if (keyframesStore.selectedKeyframeIds.length > 0) {
              keyframesStore.deleteSelectedKeyframes();
            } else {
              timelineStore.deleteSelectedClips();
            }
          }
          break;
        case 'z':
          if (cmdOrCtrl) {
            e.preventDefault();
            if (e.shiftKey) {
              timelineStore.redo();
            } else {
              timelineStore.undo();
            }
          }
          break;
        case 'a':
          if (cmdOrCtrl) {
            e.preventDefault();
            // Select all clips
            timelineStore.selectClips(clips.map((c) => c.id));
          }
          break;
        case 'escape':
          e.preventDefault();
          keyframesStore.clearKeyframeSelection();
          timelineStore.clearSelection();
          break;
        // Keyframe shortcuts
        case 'c':
          if (cmdOrCtrl && keyframesStore.selectedKeyframeIds.length > 0) {
            e.preventDefault();
            keyframesStore.copySelectedKeyframes();
          }
          break;
        case 'v':
          if (
            cmdOrCtrl &&
            keyframesStore.copiedKeyframes.length > 0 &&
            selectedClipIds.length === 1
          ) {
            e.preventDefault();
            // Paste keyframes to first selected clip at current time
            const selectedClip = clips.find((c) => c.id === selectedClipIds[0]);
            if (selectedClip) {
              // Paste to a default property (opacity) - could be enhanced to track last used property
              keyframesStore.pasteKeyframes(selectedClip.id, 'opacity', currentTime);
            }
          }
          break;
        case '[':
          // Navigate to previous keyframe
          if (selectedClipIds.length === 1) {
            e.preventDefault();
            const clipId = selectedClipIds[0];
            const tracksList = keyframesStore.getTracksForClip(clipId);
            let prevTime: number | undefined;
            tracksList.forEach((track) => {
              const { prev } = keyframesStore.getAdjacentKeyframes(
                clipId,
                track.property,
                currentTime
              );
              if (prev && (prevTime === undefined || prev.time > prevTime)) {
                prevTime = prev.time;
              }
            });
            if (prevTime !== undefined) {
              playbackStore.seek(prevTime);
            }
          }
          break;
        case ']':
          // Navigate to next keyframe
          if (selectedClipIds.length === 1) {
            e.preventDefault();
            const clipId = selectedClipIds[0];
            const tracksList = keyframesStore.getTracksForClip(clipId);
            let nextTime: number | undefined;
            tracksList.forEach((track) => {
              const { next } = keyframesStore.getAdjacentKeyframes(
                clipId,
                track.property,
                currentTime
              );
              if (next && (nextTime === undefined || next.time < nextTime)) {
                nextTime = next.time;
              }
            });
            if (nextTime !== undefined) {
              playbackStore.seek(nextTime);
            }
          }
          break;
        // Marker shortcuts
        case 'm':
          e.preventDefault();
          if (e.shiftKey) {
            // Shift+M - Add chapter marker
            markersStore.addMarker(currentTime, {
              type: 'chapter',
              name: `Chapter ${markersStore.getChapterMarkers().length + 1}`,
            });
          } else if (e.altKey) {
            // Alt+M - Add to-do marker
            markersStore.addMarker(currentTime, {
              type: 'todo',
              name: `Task ${markersStore.getTodoMarkers().length + 1}`,
            });
          } else if (!cmdOrCtrl) {
            // M - Add standard marker
            markersStore.addMarker(currentTime);
          }
          break;
        case ';':
          if (!cmdOrCtrl && !e.shiftKey && !e.altKey) {
            e.preventDefault();
            // Go to previous marker
            const prevMarker = markersStore.goToPreviousMarker(currentTime);
            if (prevMarker) {
              playbackStore.seek(prevMarker.time);
            }
          }
          break;
        case "'":
          if (!cmdOrCtrl && !e.shiftKey && !e.altKey) {
            e.preventDefault();
            // Go to next marker
            const nextMarker = markersStore.goToNextMarker(currentTime);
            if (nextMarker) {
              playbackStore.seek(nextMarker.time);
            }
          }
          break;
        case 't':
          if (cmdOrCtrl) {
            e.preventDefault();
            // Apply default transition to selected clips
            selectedClipIds.forEach((clipId) => {
              transitionsStore.applyTransition('cross-dissolve', clipId, 'end');
            });
          }
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [
    selectedClipIds,
    currentTime,
    clips,
    timelineStore,
    keyframesStore,
    playbackStore,
    markersStore,
    transitionsStore,
  ]);

  // Scroll-to-zoom with Cmd/Ctrl + mouse wheel
  const handleWheel = useCallback(
    (e: WheelEvent) => {
      const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
      const cmdOrCtrl = isMac ? e.metaKey : e.ctrlKey;

      if (cmdOrCtrl) {
        e.preventDefault();
        if (e.deltaY < 0) {
          timelineStore.zoomIn();
        } else {
          timelineStore.zoomOut();
        }
      }
    },
    [timelineStore]
  );

  const handleResizeStart = useCallback((e: ReactMouseEvent) => {
    e.preventDefault();
    setIsResizing(true);
    startYRef.current = e.clientY;
    startHeightRef.current = containerRef.current?.offsetHeight || 280;
  }, []);

  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (e: globalThis.MouseEvent) => {
      const delta = startYRef.current - e.clientY;
      const newHeight = Math.max(200, Math.min(500, startHeightRef.current + delta));
      if (containerRef.current) {
        containerRef.current.style.height = `${newHeight}px`;
      }
      onResize?.(newHeight);
    };

    const handleMouseUp = () => {
      setIsResizing(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isResizing, onResize]);

  const handleZoomIn = useCallback(() => {
    timelineStore.zoomIn();
  }, [timelineStore]);

  const handleZoomOut = useCallback(() => {
    timelineStore.zoomOut();
  }, [timelineStore]);

  const handleSplit = useCallback(() => {
    if (selectedClipIds.length > 0) {
      timelineStore.splitSelectedClips(currentTime);
    }
  }, [selectedClipIds, currentTime, timelineStore]);

  const handleDuplicate = useCallback(() => {
    timelineStore.duplicateSelectedClips();
  }, [timelineStore]);

  const handleDelete = useCallback(() => {
    timelineStore.deleteSelectedClips();
  }, [timelineStore]);

  const handleAddTrack = useCallback(
    (type: ClipType) => {
      timelineStore.addTrack(type);
    },
    [timelineStore]
  );

  const handleUndo = useCallback(() => {
    timelineStore.undo();
  }, [timelineStore]);

  const handleRedo = useCallback(() => {
    timelineStore.redo();
  }, [timelineStore]);

  const handleClipClick = useCallback(
    (clipId: string, e: ReactMouseEvent) => {
      const addToSelection = e.shiftKey || e.metaKey || e.ctrlKey;
      timelineStore.selectClip(clipId, addToSelection);
    },
    [timelineStore]
  );

  const handleTrackClick = useCallback(
    (trackId: string) => {
      timelineStore.selectTrack(trackId);
    },
    [timelineStore]
  );

  // Marker handlers
  const handleSelectMarker = useCallback(
    (markerId: string | null) => {
      markersStore.selectMarker(markerId);
    },
    [markersStore]
  );

  const handleMoveMarker = useCallback(
    (markerId: string, newTime: number) => {
      markersStore.moveMarker(markerId, newTime);
    },
    [markersStore]
  );

  const handleUpdateMarker = useCallback(
    (markerId: string, updates: Parameters<typeof markersStore.updateMarker>[1]) => {
      markersStore.updateMarker(markerId, updates);
    },
    [markersStore]
  );

  const handleDeleteMarker = useCallback(
    (markerId: string) => {
      markersStore.removeMarker(markerId);
    },
    [markersStore]
  );

  const handleAddMarker = useCallback(
    (time: number, type?: MarkerType) => {
      markersStore.addMarker(time, type ? { type } : undefined);
    },
    [markersStore]
  );

  const handleSeekToTime = useCallback(
    (time: number) => {
      playbackStore.seek(time);
    },
    [playbackStore]
  );

  // Magnetic timeline handlers
  const handleCloseGap = useCallback(
    (trackId: string, gapStart: number, gapEnd: number) => {
      closeGap(trackId, gapStart, gapEnd);
    },
    [closeGap]
  );

  // Snapping handler for clip drag operations (available for integration with clip drag logic)
  const _handleClipDragSnap = useCallback(
    (clipId: string, proposedTime: number) => {
      if (!snapToClips) {
        setActiveSnapPoint(null);
        return proposedTime;
      }

      const nearestSnapPoint = findNearestSnapPoint(proposedTime, clipId);
      if (nearestSnapPoint !== null) {
        setActiveSnapPoint(nearestSnapPoint);
        return nearestSnapPoint;
      }

      setActiveSnapPoint(null);
      return proposedTime;
    },
    [snapToClips, findNearestSnapPoint]
  );

  // Clear snap point when drag ends (available for integration with clip drag logic)
  const _handleClipDragEnd = useCallback(() => {
    setActiveSnapPoint(null);
  }, []);

  // Compute gaps for each track when magnetic timeline is enabled
  const trackGaps = useMemo(() => {
    if (!magneticTimelineEnabled) return new Map<string, TimelineGap[]>();
    const gaps = new Map<string, TimelineGap[]>();
    tracks.forEach((track) => {
      const trackGapList = findGaps(track.id);
      if (trackGapList.length > 0) {
        gaps.set(track.id, trackGapList);
      }
    });
    return gaps;
  }, [magneticTimelineEnabled, tracks, findGaps, clips]);

  const renderClip = (clip: TimelineClip) => {
    const isSelected = selectedClipIds.includes(clip.id);
    const clipLeft = clip.startTime * pixelsPerSecond;
    const clipWidth = clip.duration * pixelsPerSecond;

    const clipTypeClass = {
      video: styles.clipVideo,
      audio: styles.clipAudio,
      image: styles.clipImage,
      text: styles.clipText,
    }[clip.type];

    // Get media file for waveform
    const mediaFile = clip.mediaId ? mediaStore.getMediaById(clip.mediaId) : null;
    const showWaveform = (clip.type === 'audio' || clip.type === 'video') && mediaFile?.url;
    const waveformColor = clip.type === 'audio' ? '#22C55E' : '#3B82F6';

    // Calculate samples based on clip width for appropriate detail
    const waveformSamples = Math.max(
      WAVEFORM_MIN_SAMPLES,
      Math.min(WAVEFORM_MAX_SAMPLES, Math.floor(clipWidth / WAVEFORM_PIXELS_PER_SAMPLE))
    );

    return (
      <motion.div
        key={clip.id}
        className={mergeClasses(styles.clip, clipTypeClass, isSelected && styles.clipSelected)}
        style={{
          left: clipLeft,
          width: Math.max(clipWidth, 30),
        }}
        onClick={(e) => handleClipClick(clip.id, e)}
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        transition={{ duration: 0.15 }}
        role="button"
        tabIndex={0}
        aria-label={`${clip.name} clip`}
        aria-pressed={isSelected}
      >
        {showWaveform && mediaFile?.url && (
          <div className={styles.clipWaveform}>
            <ClipWaveform
              mediaId={clip.mediaId ?? clip.id}
              audioUrl={mediaFile.url}
              width={Math.max(clipWidth, 30)}
              height={48}
              color={waveformColor}
              trimStart={clip.inPoint}
              trimEnd={0}
              clipDuration={clip.duration}
              samples={waveformSamples}
            />
          </div>
        )}
        {clip.thumbnailUrl && (
          <img src={clip.thumbnailUrl} alt="" className={styles.clipThumbnail} />
        )}
        <div className={styles.clipInfo}>
          <span className={styles.clipName}>{clip.name}</span>
          <span className={styles.clipDuration}>{formatDuration(clip.duration)}</span>
        </div>
        <div className={mergeClasses(styles.clipTrimHandle, styles.clipTrimHandleLeft)} />
        <div className={mergeClasses(styles.clipTrimHandle, styles.clipTrimHandleRight)} />
      </motion.div>
    );
  };

  // Render transition handles for clips in a track
  const renderTransitionHandles = (trackClips: TimelineClip[]) => {
    return trackClips.flatMap((clip) => {
      const clipTransitions = transitionsStore.getTransitionsForClip(clip.id);
      return clipTransitions.map((transition) => {
        // Calculate position based on whether it's at start or end
        const clipStart = clip.startTime * pixelsPerSecond;
        const clipEnd = (clip.startTime + clip.duration) * pixelsPerSecond;
        const position = transition.position === 'start' ? clipStart : clipEnd;

        return (
          <TransitionHandle
            key={transition.id}
            transition={transition}
            position={position}
            pixelsPerSecond={pixelsPerSecond}
            isSelected={selectedTransitionId === transition.id}
            onClick={() => transitionsStore.selectTransition(transition.id)}
            onDurationChange={(id, newDuration) => {
              transitionsStore.updateTransition(id, { duration: newDuration });
            }}
          />
        );
      });
    });
  };

  const sortedTracks = useMemo(() => [...tracks].sort((a, b) => a.order - b.order), [tracks]);

  return (
    <div
      ref={containerRef}
      className={mergeClasses(styles.container, className)}
      style={{ height: '280px', minHeight: '200px', maxHeight: '500px' }}
      onWheel={handleWheel}
      // eslint-disable-next-line jsx-a11y/no-noninteractive-tabindex
      tabIndex={0}
      role="application"
      aria-label="Timeline editor"
    >
      {/* Resize Handle */}
      <button
        type="button"
        className={mergeClasses(styles.resizeHandle, isResizing && styles.resizeHandleActive)}
        onMouseDown={handleResizeStart}
        aria-label="Resize timeline"
        style={{
          background: 'transparent',
          border: 'none',
          padding: 0,
          margin: 0,
        }}
      />

      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Text weight="semibold" size={400}>
            Timeline
          </Text>
          <Badge appearance="outline" size="small">
            {projectStore.activeProject?.fps || 30} fps
          </Badge>
          {selectedClipIds.length > 0 && (
            <Badge appearance="filled" size="small" color="brand">
              {selectedClipIds.length} selected
            </Badge>
          )}
        </div>
        <div className={styles.headerRight}>
          {/* Undo/Redo Controls */}
          <div className={styles.undoRedoControls}>
            <Tooltip content="Undo (Cmd+Z)" relationship="label">
              <Button
                appearance="subtle"
                icon={<ArrowUndo24Regular />}
                size="small"
                className={styles.controlButton}
                onClick={handleUndo}
                disabled={!timelineStore.canUndo()}
              />
            </Tooltip>
            <Tooltip content="Redo (Cmd+Shift+Z)" relationship="label">
              <Button
                appearance="subtle"
                icon={<ArrowRedo24Regular />}
                size="small"
                className={styles.controlButton}
                onClick={handleRedo}
                disabled={!timelineStore.canRedo()}
              />
            </Tooltip>
          </div>

          {/* Magnetic Timeline Toolbar */}
          <TimelineToolbar className={styles.magneticToolbar} />

          <Divider vertical className={styles.headerDivider} />

          {/* Zoom Controls */}
          <div className={styles.zoomControls}>
            <Tooltip content="Zoom out" relationship="label">
              <Button
                appearance="subtle"
                icon={<ZoomOut24Regular />}
                size="small"
                className={styles.controlButton}
                onClick={handleZoomOut}
              />
            </Tooltip>
            <Text size={200} style={{ minWidth: '40px', textAlign: 'center' }}>
              {Math.round(zoom * 100)}%
            </Text>
            <Tooltip content="Zoom in" relationship="label">
              <Button
                appearance="subtle"
                icon={<ZoomIn24Regular />}
                size="small"
                className={styles.controlButton}
                onClick={handleZoomIn}
              />
            </Tooltip>
          </div>

          {/* Edit Controls */}
          <Tooltip content="Split at playhead (S)" relationship="label">
            <Button
              appearance="subtle"
              icon={<Cut24Regular />}
              size="small"
              className={styles.controlButton}
              onClick={handleSplit}
              disabled={selectedClipIds.length === 0}
            />
          </Tooltip>
          <Tooltip content="Duplicate (Cmd+D)" relationship="label">
            <Button
              appearance="subtle"
              icon={<Copy24Regular />}
              size="small"
              className={styles.controlButton}
              onClick={handleDuplicate}
              disabled={selectedClipIds.length === 0}
            />
          </Tooltip>
          <Tooltip content="Delete (Del)" relationship="label">
            <Button
              appearance="subtle"
              icon={<Delete24Regular />}
              size="small"
              className={styles.controlButton}
              onClick={handleDelete}
              disabled={selectedClipIds.length === 0}
            />
          </Tooltip>

          {/* Add Track Menu */}
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <Tooltip content="Add track" relationship="label">
                <Button
                  appearance="subtle"
                  icon={<Add24Regular />}
                  size="small"
                  className={styles.controlButton}
                >
                  <ChevronDown16Regular />
                </Button>
              </Tooltip>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem icon={<Video24Regular />} onClick={() => handleAddTrack('video')}>
                  Video Track
                </MenuItem>
                <MenuItem icon={<MusicNote224Regular />} onClick={() => handleAddTrack('audio')}>
                  Audio Track
                </MenuItem>
                <MenuItem icon={<Image24Regular />} onClick={() => handleAddTrack('image')}>
                  Image Track
                </MenuItem>
                <MenuItem icon={<TextT24Regular />} onClick={() => handleAddTrack('text')}>
                  Text Track
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
      </div>

      {/* Timeline Content */}
      <div className={styles.content}>
        {/* Time Ruler */}
        <div className={styles.rulerArea}>
          <div className={styles.rulerLabels}>
            <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
              Tracks
            </Text>
          </div>
          <div className={styles.rulerScrollable}>
            <div className={styles.ruler} style={{ width: totalWidth }}>
              {rulerMarks.map((mark) => (
                <div key={mark.time} className={styles.rulerMark} style={{ left: mark.position }}>
                  <div
                    className={styles.rulerMarkLine}
                    style={{ height: mark.major ? '12px' : '6px' }}
                  />
                  {mark.major && (
                    <span className={styles.rulerMarkLabel}>{formatTimeRuler(mark.time)}</span>
                  )}
                </div>
              ))}

              {/* Playhead in ruler */}
              <motion.div
                className={styles.playhead}
                style={{ left: playheadPosition }}
                initial={false}
                animate={{ left: playheadPosition }}
                transition={{ type: 'tween', duration: 0.05 }}
              >
                <div className={styles.playheadHandle} />
              </motion.div>
            </div>
          </div>
        </div>

        {/* Marker Track */}
        <MarkerTrack
          markers={visibleMarkers}
          selectedMarkerId={selectedMarkerId}
          pixelsPerSecond={pixelsPerSecond}
          totalWidth={totalWidth}
          currentTime={currentTime}
          onSelectMarker={handleSelectMarker}
          onMoveMarker={handleMoveMarker}
          onUpdateMarker={handleUpdateMarker}
          onDeleteMarker={handleDeleteMarker}
          onAddMarker={handleAddMarker}
          onSeek={handleSeekToTime}
        />

        {/* Tracks */}
        <div className={styles.tracksScrollable}>
          {sortedTracks.map((track) => {
            const trackClips = clips.filter((c) => c.trackId === track.id);
            const isSelected = selectedTrackId === track.id;

            return (
              <div
                key={track.id}
                className={mergeClasses(
                  styles.track,
                  isSelected && styles.trackSelected,
                  track.locked && styles.trackLocked
                )}
                onClick={() => handleTrackClick(track.id)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    handleTrackClick(track.id);
                  }
                }}
                role="button"
                tabIndex={0}
                aria-pressed={isSelected}
              >
                <div className={styles.trackLabel}>
                  <span className={styles.trackLabelIcon}>{TRACK_TYPE_ICONS[track.type]}</span>
                  <span className={styles.trackLabelText}>{track.name}</span>
                  <div className={styles.trackControls}>
                    <Tooltip content={track.muted ? 'Unmute' : 'Mute'} relationship="label">
                      <Button
                        appearance="subtle"
                        size="small"
                        className={styles.trackControlButton}
                        icon={track.muted ? <SpeakerMute24Regular /> : <Speaker224Regular />}
                        onClick={(e) => {
                          e.stopPropagation();
                          timelineStore.muteTrack(track.id, !track.muted);
                        }}
                      />
                    </Tooltip>
                    <Tooltip content={track.locked ? 'Unlock' : 'Lock'} relationship="label">
                      <Button
                        appearance="subtle"
                        size="small"
                        className={styles.trackControlButton}
                        icon={track.locked ? <LockClosed24Regular /> : <LockOpen24Regular />}
                        onClick={(e) => {
                          e.stopPropagation();
                          timelineStore.lockTrack(track.id, !track.locked);
                        }}
                      />
                    </Tooltip>
                  </div>
                </div>
                <div className={styles.trackContentScrollable}>
                  <div className={styles.trackContent} style={{ width: totalWidth }}>
                    {trackClips.map(renderClip)}

                    {/* Gap indicators */}
                    {magneticTimelineEnabled &&
                      trackGaps
                        .get(track.id)
                        ?.map((gap, index) => (
                          <GapIndicator
                            key={`gap-${track.id}-${index}`}
                            startPosition={gap.start * pixelsPerSecond}
                            endPosition={gap.end * pixelsPerSecond}
                            duration={gap.end - gap.start}
                            trackId={track.id}
                            gapStart={gap.start}
                            gapEnd={gap.end}
                            visible={true}
                            onCloseGap={handleCloseGap}
                          />
                        ))}

                    {/* Transition handles */}
                    {renderTransitionHandles(trackClips)}

                    {/* Playhead line continues through tracks */}
                    <motion.div
                      className={styles.playhead}
                      style={{ left: playheadPosition }}
                      initial={false}
                      animate={{ left: playheadPosition }}
                      transition={{ type: 'tween', duration: 0.05 }}
                    />

                    {/* Snap indicator */}
                    {snapEnabled && snapToClips && (
                      <SnapIndicator
                        position={activeSnapPoint !== null ? activeSnapPoint * pixelsPerSecond : 0}
                        visible={activeSnapPoint !== null}
                        snapType="clip-edge"
                      />
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default Timeline;
