import { useState, useRef, useCallback } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Slider,
  Label,
  Text,
  Switch,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Subtract24Regular,
  Cut24Regular,
} from '@fluentui/react-icons';

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
  ruler: {
    height: '30px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    position: 'sticky',
    top: 0,
    backgroundColor: tokens.colorNeutralBackground1,
    zIndex: 10,
    display: 'flex',
  },
  rulerMarker: {
    position: 'absolute',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    userSelect: 'none',
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
  trackLabel: {
    width: '100px',
    padding: tokens.spacingVerticalS,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    display: 'flex',
    alignItems: 'center',
    position: 'sticky',
    left: 0,
    zIndex: 5,
  },
  trackContent: {
    flex: 1,
    position: 'relative',
    minWidth: '2000px',
  },
  clip: {
    position: 'absolute',
    top: '8px',
    height: '44px',
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid ${tokens.colorBrandStroke1}`,
    cursor: 'pointer',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    color: tokens.colorNeutralForegroundOnBrand,
    fontSize: tokens.fontSizeBase200,
    overflow: 'hidden',
    whiteSpace: 'nowrap',
    textOverflow: 'ellipsis',
    display: 'flex',
    alignItems: 'center',
    userSelect: 'none',
    '&:hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
  clipSelected: {
    border: `2px solid ${tokens.colorBrandForeground1}`,
    backgroundColor: tokens.colorBrandBackgroundHover,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    zIndex: 20,
    pointerEvents: 'none',
  },
  playheadHandle: {
    position: 'absolute',
    top: '-6px',
    left: '-6px',
    width: '14px',
    height: '14px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    borderRadius: '50%',
    cursor: 'ew-resize',
    pointerEvents: 'auto',
  },
  zoomSlider: {
    width: '120px',
  },
});

interface TimelineClip {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  label: string;
  type: 'video' | 'audio' | 'image';
}

interface TimelineTrack {
  id: string;
  label: string;
  type: 'video' | 'audio';
}

interface TimelinePanelProps {
  clips?: TimelineClip[];
  tracks?: TimelineTrack[];
  currentTime?: number;
  onTimeChange?: (time: number) => void;
  onClipSelect?: (clipId: string | null) => void;
  selectedClipId?: string | null;
}

export function TimelinePanel({
  clips = [],
  tracks = [
    { id: 'video1', label: 'Video 1', type: 'video' },
    { id: 'video2', label: 'Video 2', type: 'video' },
    { id: 'audio1', label: 'Audio 1', type: 'audio' },
    { id: 'audio2', label: 'Audio 2', type: 'audio' },
  ],
  currentTime = 0,
  onTimeChange,
  onClipSelect,
  selectedClipId = null,
}: TimelinePanelProps) {
  const styles = useStyles();
  const [zoom, setZoom] = useState(50); // pixels per second
  const [snapping, setSnapping] = useState(true);
  const timelineRef = useRef<HTMLDivElement>(null);
  const isDraggingPlayhead = useRef(false);

  // Calculate timeline width based on clips
  const maxTime = Math.max(
    10,
    ...clips.map((clip) => clip.startTime + clip.duration),
    currentTime + 5
  );

  const pixelsPerSecond = zoom;
  const timelineWidth = maxTime * pixelsPerSecond;

  const handlePlayheadDrag = useCallback(
    (_e: React.MouseEvent) => {
      if (!timelineRef.current) return;
      isDraggingPlayhead.current = true;

      const handleMouseMove = (moveEvent: MouseEvent) => {
        if (!timelineRef.current || !isDraggingPlayhead.current) return;

        const rect = timelineRef.current.getBoundingClientRect();
        const x = moveEvent.clientX - rect.left - 100; // Account for track label width
        let time = Math.max(0, x / pixelsPerSecond);

        if (snapping) {
          time = Math.round(time * 2) / 2; // Snap to 0.5 second intervals
        }

        onTimeChange?.(time);
      };

      const handleMouseUp = () => {
        isDraggingPlayhead.current = false;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    },
    [pixelsPerSecond, snapping, onTimeChange]
  );

  const handleTimelineClick = (e: React.MouseEvent) => {
    if (!timelineRef.current || isDraggingPlayhead.current) return;

    const rect = timelineRef.current.getBoundingClientRect();
    const x = e.clientX - rect.left - 100; // Account for track label width
    let time = Math.max(0, x / pixelsPerSecond);

    if (snapping) {
      time = Math.round(time * 2) / 2;
    }

    onTimeChange?.(time);
  };

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    const frames = Math.floor((seconds % 1) * 30);
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
  };

  const renderRuler = () => {
    const markers = [];
    const interval = zoom < 30 ? 5 : zoom < 60 ? 2 : 1;

    for (let i = 0; i <= maxTime; i += interval) {
      const left = i * pixelsPerSecond + 100;
      markers.push(
        <div key={i} className={styles.rulerMarker} style={{ left: `${left}px`, top: '8px' }}>
          {formatTime(i)}
        </div>
      );
    }

    return markers;
  };

  return (
    <div className={styles.container}>
      <div className={styles.toolbar}>
        <div className={styles.toolbarGroup}>
          <Button appearance="subtle" icon={<Cut24Regular />} disabled>
            Split
          </Button>
          <Button appearance="subtle" icon={<Cut24Regular />} disabled>
            Delete
          </Button>
        </div>

        <div className={styles.toolbarGroup}>
          <Label>Snapping</Label>
          <Switch checked={snapping} onChange={(_, data) => setSnapping(data.checked)} />
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
            max={100}
            value={zoom}
            onChange={(_, data) => setZoom(data.value)}
          />
          <Button
            appearance="subtle"
            icon={<Add24Regular />}
            onClick={() => setZoom(Math.min(100, zoom + 10))}
            aria-label="Zoom in"
          />
          <Text>{Math.round((zoom / 50) * 100)}%</Text>
        </div>
      </div>

      <div className={styles.timelineContainer} ref={timelineRef}>
        <div className={styles.ruler} style={{ width: `${timelineWidth + 100}px` }}>
          {renderRuler()}
        </div>

        <div className={styles.tracksContainer} onClick={handleTimelineClick} role="region" aria-label="Timeline tracks">
          {tracks.map((track) => (
            <div key={track.id} className={styles.track}>
              <div className={styles.trackLabel}>{track.label}</div>
              <div className={styles.trackContent} style={{ width: `${timelineWidth}px` }}>
                {clips
                  .filter((clip) => clip.trackId === track.id)
                  .map((clip) => (
                    <div
                      key={clip.id}
                      className={`${styles.clip} ${selectedClipId === clip.id ? styles.clipSelected : ''}`}
                      style={{
                        left: `${clip.startTime * pixelsPerSecond}px`,
                        width: `${clip.duration * pixelsPerSecond}px`,
                      }}
                      onClick={(e) => {
                        e.stopPropagation();
                        onClipSelect?.(clip.id);
                      }}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          e.stopPropagation();
                          onClipSelect?.(clip.id);
                        }
                      }}
                      role="button"
                      tabIndex={0}
                      aria-label={`${clip.label} clip`}
                    >
                      {clip.label}
                    </div>
                  ))}
              </div>
            </div>
          ))}

          {/* Playhead */}
          <div className={styles.playhead} style={{ left: `${currentTime * pixelsPerSecond + 100}px` }}>
            <div className={styles.playheadHandle} onMouseDown={handlePlayheadDrag} role="slider" aria-label="Playhead" />
          </div>
        </div>
      </div>
    </div>
  );
}
