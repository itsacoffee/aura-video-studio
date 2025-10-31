import {
  makeStyles,
  tokens,
  Button,
  Slider,
  Label,
  Text,
  Switch,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Subtract24Regular,
  Eye24Regular,
  EyeOff24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
  ArrowFitRegular,
  ChevronDown24Regular,
} from '@fluentui/react-icons';
import { useState, useRef, useCallback, useEffect } from 'react';
import {
  snapToFrame,
  formatTimecode,
  calculateSnapPoints,
  findNearestSnapPoint,
  TimelineDisplayMode,
  TimelineTool,
  TrimMode,
  closeGaps,
  applyRippleEdit,
  SnapPoint,
} from '../../services/timelineEngine';
import { AppliedEffect } from '../../types/effects';
import { PlayheadIndicator } from '../Timeline/PlayheadIndicator';
import { SnapGuides } from '../Timeline/SnapGuides';
import { TimelineClip, TimelineClipData } from '../Timeline/TimelineClip';
import { TimelineRuler } from '../Timeline/TimelineRuler';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
    flexWrap: 'wrap',
  },
  toolbarGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  timelineContainer: {
    flex: 1,
    overflow: 'auto',
    position: 'relative',
  },
  tracksContainer: {
    position: 'relative',
    minHeight: '200px',
  },
  track: {
    height: '60px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    position: 'relative',
    display: 'flex',
  },
  trackDragOver: {
    backgroundColor: tokens.colorBrandBackground2,
  },
  trackLabel: {
    width: '100px',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingVerticalS}`,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase200,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    position: 'sticky',
    left: 0,
    zIndex: 5,
  },
  trackLabelText: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  trackControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXXS,
  },
  trackContent: {
    flex: 1,
    position: 'relative',
    minWidth: '2000px',
  },
  zoomSlider: {
    width: '120px',
  },
  toolButton: {
    minWidth: '80px',
  },
  activeToolButton: {
    backgroundColor: tokens.colorBrandBackground2,
  },
});

interface TimelineClip {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  label: string;
  type: 'video' | 'audio' | 'image';
  effects?: AppliedEffect[];
  // Media source reference
  mediaId?: string;
  file?: File;
  // Visual data
  thumbnails?: Array<{ dataUrl: string; timestamp: number }>;
  waveform?: { peaks: number[]; duration: number };
  preview?: string;
  // Animation keyframes
  keyframes?: Record<string, Array<{ time: number; value: number | string | boolean }>>;
}

interface TimelineTrack {
  id: string;
  label: string;
  type: 'video' | 'audio';
  visible?: boolean;
  locked?: boolean;
}

interface TimelinePanelProps {
  clips?: TimelineClip[];
  tracks?: TimelineTrack[];
  currentTime?: number;
  onTimeChange?: (time: number) => void;
  onClipSelect?: (clipId: string | null) => void;
  selectedClipId?: string | null;
  onClipAdd?: (trackId: string, clip: TimelineClip) => void;
  onClipUpdate?: (clipId: string, updates: Partial<TimelineClip>) => void;
  onTrackToggleVisibility?: (trackId: string) => void;
  onTrackToggleLock?: (trackId: string) => void;
}

export function TimelinePanel({
  clips = [],
  tracks = [
    { id: 'video1', label: 'Video 1', type: 'video', visible: true, locked: false },
    { id: 'video2', label: 'Video 2', type: 'video', visible: true, locked: false },
    { id: 'audio1', label: 'Audio 1', type: 'audio', visible: true, locked: false },
    { id: 'audio2', label: 'Audio 2', type: 'audio', visible: true, locked: false },
  ],
  currentTime = 0,
  onTimeChange,
  onClipSelect,
  selectedClipId = null,
  onClipAdd,
  onClipUpdate,
  onTrackToggleVisibility,
  onTrackToggleLock,
}: TimelinePanelProps) {
  const styles = useStyles();
  const [zoom, setZoom] = useState(50); // pixels per second
  const [snapping, setSnapping] = useState(true);
  const [magneticTimeline, setMagneticTimeline] = useState(false);
  const [dragOverTrack, setDragOverTrack] = useState<string | null>(null);
  const [currentTool, setCurrentTool] = useState<TimelineTool>(TimelineTool.SELECT);
  const [trimMode, setTrimMode] = useState<TrimMode>(TrimMode.RIPPLE);
  const [displayMode, setDisplayMode] = useState<TimelineDisplayMode>(TimelineDisplayMode.TIMECODE);
  const [activeSnapPoint, setActiveSnapPoint] = useState<SnapPoint | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);

  const timelineRef = useRef<HTMLDivElement>(null);
  const frameRate = 30;

  // JKL shuttle control state
  const jklState = useRef<{ key: string | null; speedMultiplier: number }>({
    key: null,
    speedMultiplier: 1,
  });

  // Calculate timeline width based on clips
  const maxTime = Math.max(
    10,
    ...clips.map((clip) => clip.startTime + clip.duration),
    currentTime + 5
  );

  const pixelsPerSecond = zoom;
  const timelineWidth = maxTime * pixelsPerSecond;

  // JKL Shuttle controls and keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // JKL shuttle controls
      if (e.key === 'j') {
        e.preventDefault();
        if (jklState.current.key === 'j') {
          jklState.current.speedMultiplier = Math.min(4, jklState.current.speedMultiplier + 1);
        } else {
          jklState.current.key = 'j';
          jklState.current.speedMultiplier = 1;
        }
        setIsPlaying(true);
        // Note: playback speed would be used by video player component
      } else if (e.key === 'k') {
        e.preventDefault();
        jklState.current.key = 'k';
        jklState.current.speedMultiplier = 1;
        setIsPlaying(false);
        // Note: playback speed would be used by video player component
      } else if (e.key === 'l') {
        e.preventDefault();
        if (jklState.current.key === 'l') {
          jklState.current.speedMultiplier = Math.min(4, jklState.current.speedMultiplier + 1);
        } else {
          jklState.current.key = 'l';
          jklState.current.speedMultiplier = 1;
        }
        setIsPlaying(true);
        // Note: playback speed would be used by video player component
      } else if (e.key === ' ') {
        e.preventDefault();
        setIsPlaying(!isPlaying);
        jklState.current.key = null;
        jklState.current.speedMultiplier = 1;
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        const step = e.shiftKey ? 10 / frameRate : 1 / frameRate;
        const newTime = Math.max(0, currentTime - step);
        onTimeChange?.(snapToFrame(newTime, frameRate));
      } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        const step = e.shiftKey ? 10 / frameRate : 1 / frameRate;
        const newTime = Math.min(maxTime, currentTime + step);
        onTimeChange?.(snapToFrame(newTime, frameRate));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        const newTime = Math.min(maxTime, currentTime + 10 / frameRate);
        onTimeChange?.(snapToFrame(newTime, frameRate));
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        const newTime = Math.max(0, currentTime - 10 / frameRate);
        onTimeChange?.(snapToFrame(newTime, frameRate));
      }
    };

    const handleKeyUp = (e: KeyboardEvent) => {
      if (e.key === 'j' || e.key === 'l') {
        // Reset on key release
        if (jklState.current.key === e.key) {
          jklState.current.key = null;
          jklState.current.speedMultiplier = 1;
          setIsPlaying(false);
          // Note: playback speed would be used by video player component
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
    };
  }, [currentTime, maxTime, frameRate, isPlaying, onTimeChange]);

  // Clip manipulation handlers
  const handleClipMove = useCallback(
    (clipId: string, newStartTime: number) => {
      if (magneticTimeline) {
        // Apply magnetic timeline behavior
        const snapPoints = calculateSnapPoints(
          clips.filter((c) => c.id !== clipId),
          currentTime,
          []
        );
        const nearSnap = findNearestSnapPoint(newStartTime, snapPoints, 0.1);
        if (nearSnap) {
          newStartTime = nearSnap.time;
          setActiveSnapPoint(nearSnap);
        } else {
          setActiveSnapPoint(null);
        }
      }

      onClipUpdate?.(clipId, { startTime: newStartTime });

      // Close gaps if magnetic timeline is enabled
      if (magneticTimeline) {
        const clip = clips.find((c) => c.id === clipId);
        if (clip) {
          const updatedClips = clips.map((c) =>
            c.id === clipId ? { ...c, startTime: newStartTime } : c
          );
          const closedGaps = closeGaps(updatedClips.filter((c) => c.trackId === clip.trackId));
          closedGaps.forEach((c) => {
            if (c.id !== clipId && c.startTime !== clips.find((oc) => oc.id === c.id)?.startTime) {
              onClipUpdate?.(c.id, { startTime: c.startTime });
            }
          });
        }
      }
    },
    [clips, currentTime, magneticTimeline, onClipUpdate]
  );

  const handleClipTrim = useCallback(
    (clipId: string, newStartTime: number, newDuration: number) => {
      const clip = clips.find((c) => c.id === clipId);
      if (!clip) return;

      if (trimMode === TrimMode.RIPPLE) {
        // Ripple edit: adjust following clips
        const delta = newDuration - clip.duration;
        const editPoint = clip.startTime + clip.duration;
        const trackClips = clips.filter((c) => c.trackId === clip.trackId);
        const updated = applyRippleEdit(trackClips, editPoint, delta);

        updated.forEach((c) => {
          if (c.id !== clipId && c.startTime !== clips.find((oc) => oc.id === c.id)?.startTime) {
            onClipUpdate?.(c.id, { startTime: c.startTime });
          }
        });
      }

      onClipUpdate?.(clipId, { startTime: newStartTime, duration: newDuration });
    },
    [clips, trimMode, onClipUpdate]
  );

  const handleRazorSplit = useCallback(
    (time: number) => {
      // Find clip at current time
      const clipToSplit = clips.find((c) => c.startTime <= time && c.startTime + c.duration > time);

      if (!clipToSplit) return;

      const splitPoint = time - clipToSplit.startTime;

      // Update the first clip to end at the split point
      onClipUpdate?.(clipToSplit.id, { duration: splitPoint });

      // Create and add the second clip starting from the split point
      const secondClip: TimelineClip = {
        ...clipToSplit,
        id: `${clipToSplit.id}-split-${Date.now()}`,
        startTime: time,
        duration: clipToSplit.duration - splitPoint,
      };

      onClipAdd?.(clipToSplit.trackId, secondClip);
    },
    [clips, onClipUpdate, onClipAdd]
  );

  const handleFitToWindow = useCallback(() => {
    if (timelineRef.current) {
      const containerWidth = timelineRef.current.clientWidth - 100; // Account for track labels
      const newZoom = containerWidth / maxTime;
      setZoom(Math.max(10, Math.min(100, newZoom)));
    }
  }, [maxTime]);

  const handleTimelineClick = useCallback(
    (time: number) => {
      if (currentTool === TimelineTool.RAZOR) {
        handleRazorSplit(time);
      } else {
        let adjustedTime = time;
        if (snapping) {
          adjustedTime = snapToFrame(time, frameRate);
        }
        onTimeChange?.(adjustedTime);
      }
    },
    [currentTool, snapping, frameRate, handleRazorSplit, onTimeChange]
  );

  const formatTime = (seconds: number) => {
    return formatTimecode(seconds, frameRate);
  };

  const handleTrackDragOver = (e: React.DragEvent, trackId: string) => {
    e.preventDefault();
    e.stopPropagation();
    setDragOverTrack(trackId);
  };

  const handleTrackDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragOverTrack(null);
  };

  const handleTrackDrop = (e: React.DragEvent, trackId: string) => {
    e.preventDefault();
    e.stopPropagation();
    setDragOverTrack(null);

    try {
      const data = e.dataTransfer.getData('application/json');
      if (!data) return;

      const dropData = JSON.parse(data);

      if (dropData.type === 'effect') {
        // Don't handle effects here - they should be dropped on clips
        return;
      }

      // Handle media clip drops
      const mediaClip = dropData;

      // Calculate drop position based on mouse position
      const rect = (e.target as HTMLElement).getBoundingClientRect();
      const x = e.clientX - rect.left;
      let dropTime = Math.max(0, (x - 100) / pixelsPerSecond);

      if (snapping) {
        dropTime = snapToFrame(dropTime, frameRate);
      }

      // Create a new timeline clip from the media clip
      const newClip: TimelineClip = {
        id: `clip-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`,
        trackId,
        startTime: dropTime,
        duration: mediaClip.duration || 3,
        label: mediaClip.name,
        type: mediaClip.type,
        mediaId: mediaClip.id,
        file: mediaClip.file,
        thumbnails: mediaClip.thumbnails,
        waveform: mediaClip.waveform,
        preview: mediaClip.preview,
      };

      onClipAdd?.(trackId, newClip);
    } catch (error) {
      console.error('Failed to parse dropped data:', error);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.toolbar}>
        <div className={styles.toolbarGroup}>
          <Label>Tool</Label>
          <Menu>
            <MenuTrigger>
              <Button
                appearance="subtle"
                className={styles.toolButton}
                icon={<ChevronDown24Regular />}
                iconPosition="after"
              >
                {currentTool === TimelineTool.SELECT && 'Select'}
                {currentTool === TimelineTool.RAZOR && 'Razor'}
                {currentTool === TimelineTool.HAND && 'Hand'}
              </Button>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={() => setCurrentTool(TimelineTool.SELECT)}>Select</MenuItem>
                <MenuItem onClick={() => setCurrentTool(TimelineTool.RAZOR)}>Razor</MenuItem>
                <MenuItem onClick={() => setCurrentTool(TimelineTool.HAND)}>Hand</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>

        <div className={styles.toolbarGroup}>
          <Label>Trim Mode</Label>
          <Menu>
            <MenuTrigger>
              <Button
                appearance="subtle"
                className={styles.toolButton}
                icon={<ChevronDown24Regular />}
                iconPosition="after"
              >
                {trimMode === TrimMode.RIPPLE && 'Ripple'}
                {trimMode === TrimMode.ROLL && 'Roll'}
                {trimMode === TrimMode.SLIP && 'Slip'}
                {trimMode === TrimMode.SLIDE && 'Slide'}
              </Button>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={() => setTrimMode(TrimMode.RIPPLE)}>Ripple Edit</MenuItem>
                <MenuItem onClick={() => setTrimMode(TrimMode.ROLL)}>Roll Edit</MenuItem>
                <MenuItem onClick={() => setTrimMode(TrimMode.SLIP)}>Slip Edit</MenuItem>
                <MenuItem onClick={() => setTrimMode(TrimMode.SLIDE)}>Slide Edit</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>

        <div className={styles.toolbarGroup}>
          <Label>Display</Label>
          <Menu>
            <MenuTrigger>
              <Button
                appearance="subtle"
                className={styles.toolButton}
                icon={<ChevronDown24Regular />}
                iconPosition="after"
              >
                {displayMode === TimelineDisplayMode.TIMECODE && 'Timecode'}
                {displayMode === TimelineDisplayMode.FRAMES && 'Frames'}
                {displayMode === TimelineDisplayMode.SECONDS && 'Seconds'}
              </Button>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={() => setDisplayMode(TimelineDisplayMode.TIMECODE)}>
                  Timecode
                </MenuItem>
                <MenuItem onClick={() => setDisplayMode(TimelineDisplayMode.FRAMES)}>
                  Frames
                </MenuItem>
                <MenuItem onClick={() => setDisplayMode(TimelineDisplayMode.SECONDS)}>
                  Seconds
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>

        <div className={styles.toolbarGroup}>
          <Label>Snapping</Label>
          <Switch checked={snapping} onChange={(_, data) => setSnapping(data.checked)} />
        </div>

        <div className={styles.toolbarGroup}>
          <Label>Magnetic</Label>
          <Switch
            checked={magneticTimeline}
            onChange={(_, data) => setMagneticTimeline(data.checked)}
          />
        </div>

        <div className={styles.toolbarGroup}>
          <Label>Zoom</Label>
          <Button
            appearance="subtle"
            icon={<Subtract24Regular />}
            onClick={() => setZoom(Math.max(10, zoom - 10))}
            aria-label="Zoom out"
          />
          <Slider
            className={styles.zoomSlider}
            min={10}
            max={200}
            value={zoom}
            onChange={(_, data) => setZoom(data.value)}
          />
          <Button
            appearance="subtle"
            icon={<Add24Regular />}
            onClick={() => setZoom(Math.min(200, zoom + 10))}
            aria-label="Zoom in"
          />
          <Text>{Math.round((zoom / 50) * 100)}%</Text>
          <Tooltip content="Fit to window" relationship="label">
            <Button
              appearance="subtle"
              icon={<ArrowFitRegular />}
              onClick={handleFitToWindow}
              aria-label="Fit to window"
            />
          </Tooltip>
        </div>
      </div>

      <div className={styles.timelineContainer} ref={timelineRef}>
        <TimelineRuler
          width={timelineWidth}
          pixelsPerSecond={pixelsPerSecond}
          maxTime={maxTime}
          displayMode={displayMode}
          frameRate={frameRate}
          onTimeClick={handleTimelineClick}
        />

        <div className={styles.tracksContainer} role="region" aria-label="Timeline tracks">
          {tracks.map((track) => (
            /* Timeline track - intentionally clickable for seeking playhead */
            /* eslint-disable-next-line jsx-a11y/click-events-have-key-events, jsx-a11y/no-static-element-interactions */
            <div
              key={track.id}
              className={`${styles.track} ${dragOverTrack === track.id ? styles.trackDragOver : ''}`}
              onDragOver={(e) => handleTrackDragOver(e, track.id)}
              onDragLeave={handleTrackDragLeave}
              onDrop={(e) => handleTrackDrop(e, track.id)}
              onClick={(e) => {
                const rect = e.currentTarget.getBoundingClientRect();
                const x = e.clientX - rect.left - 100;
                const time = Math.max(0, x / pixelsPerSecond);
                handleTimelineClick(time);
              }}
            >
              <div className={styles.trackLabel}>
                <Text className={styles.trackLabelText}>{track.label}</Text>
                <div className={styles.trackControls}>
                  <Tooltip
                    content={track.visible ? 'Hide track' : 'Show track'}
                    relationship="label"
                  >
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={track.visible ? <Eye24Regular /> : <EyeOff24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        onTrackToggleVisibility?.(track.id);
                      }}
                      aria-label={track.visible ? 'Hide track' : 'Show track'}
                      style={{ minWidth: '20px', minHeight: '20px', padding: '2px' }}
                    />
                  </Tooltip>
                  <Tooltip
                    content={track.locked ? 'Unlock track' : 'Lock track'}
                    relationship="label"
                  >
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={track.locked ? <LockClosed24Regular /> : <LockOpen24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        onTrackToggleLock?.(track.id);
                      }}
                      aria-label={track.locked ? 'Unlock track' : 'Lock track'}
                      style={{ minWidth: '20px', minHeight: '20px', padding: '2px' }}
                    />
                  </Tooltip>
                </div>
              </div>
              <div className={styles.trackContent} style={{ width: `${timelineWidth}px` }}>
                {clips
                  .filter((clip) => clip.trackId === track.id)
                  .map((clip) => (
                    <TimelineClip
                      key={clip.id}
                      clip={clip as TimelineClipData}
                      pixelsPerSecond={pixelsPerSecond}
                      isSelected={selectedClipId === clip.id}
                      onSelect={() => onClipSelect?.(clip.id)}
                      onMove={handleClipMove}
                      onTrim={handleClipTrim}
                      snapping={snapping}
                      frameRate={frameRate}
                    />
                  ))}
              </div>
            </div>
          ))}

          {/* Snap guides */}
          <SnapGuides
            activeSnapPoint={activeSnapPoint}
            pixelsPerSecond={pixelsPerSecond}
            trackLabelWidth={100}
          />

          {/* Playhead */}
          <PlayheadIndicator
            currentTime={currentTime}
            pixelsPerSecond={pixelsPerSecond}
            maxTime={maxTime}
            onTimeChange={(time) => onTimeChange?.(time)}
            showTooltip={true}
            timeTooltip={formatTime(currentTime)}
            snapping={snapping}
            frameRate={frameRate}
            containerRef={timelineRef}
          />
        </div>
      </div>
    </div>
  );
}
