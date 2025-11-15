/**
 * Enhanced Timeline component with advanced editing features
 */

import {
  makeStyles,
  tokens,
  Button,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Cut24Regular,
  Delete24Regular,
  Copy24Regular,
  Question24Regular,
} from '@fluentui/react-icons';
import { useRef, useState, useCallback } from 'react';
import {
  useTimelineKeyboardShortcuts,
  getKeyboardShortcuts,
} from '../../../hooks/useTimelineKeyboardShortcuts';
import { useTimelineStore } from '../../../state/timeline';
import { AudioTrackControls } from './AudioTrackControls';
import { TimelineTrack } from './TimelineTrack';
import { TimelineZoomControls } from './TimelineZoomControls';
import '../../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: 'var(--timeline-bg)',
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 'var(--editor-space-md)',
    borderBottom: `1px solid var(--editor-panel-border)`,
    backgroundColor: 'var(--editor-panel-header-bg)',
  },
  toolbarLeft: {
    display: 'flex',
    gap: 'var(--editor-space-md)',
    alignItems: 'center',
  },
  toolbarRight: {
    display: 'flex',
    gap: 'var(--editor-space-sm)',
    alignItems: 'center',
  },
  timelineContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  ruler: {
    height: '32px',
    borderBottom: `1px solid var(--editor-panel-border)`,
    position: 'relative',
    backgroundColor: 'var(--timeline-ruler-bg)',
  },
  tracksContainer: {
    flex: 1,
    display: 'flex',
    overflow: 'auto',
    position: 'relative',
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: 'var(--playhead-color)',
    boxShadow: '0 0 8px var(--playhead-shadow)',
    pointerEvents: 'none',
    zIndex: 'var(--editor-z-playhead)',
    '&::before': {
      content: '""',
      position: 'absolute',
      top: '-8px',
      left: '50%',
      transform: 'translateX(-50%)',
      width: 0,
      height: 0,
      borderLeft: '6px solid transparent',
      borderRight: '6px solid transparent',
      borderTop: '8px solid var(--playhead-color)',
    },
  },
  playheadTime: {
    position: 'absolute',
    top: '-24px',
    left: '50%',
    transform: 'translateX(-50%)',
    padding: '2px 6px',
    backgroundColor: 'var(--playhead-color)',
    color: 'white',
    fontSize: 'var(--editor-font-size-xs)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    borderRadius: 'var(--editor-radius-sm)',
    whiteSpace: 'nowrap',
    boxShadow: 'var(--editor-shadow-md)',
  },
  snapLine: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '1px',
    backgroundColor: 'var(--editor-accent)',
    boxShadow: '0 0 4px var(--editor-focus-ring)',
    pointerEvents: 'none',
    zIndex: 99,
  },
  shortcutsDialog: {
    maxWidth: '600px',
  },
  shortcutsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  shortcutItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalXS,
  },
  shortcutKeys: {
    fontFamily: 'monospace',
    backgroundColor: tokens.colorNeutralBackground3,
    padding: '4px 8px',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
  },
  shortcutDesc: {
    color: tokens.colorNeutralForeground2,
  },
});

export interface TimelineProps {
  duration?: number;
  onSave?: () => void;
}

export function Timeline({ duration = 120, onSave }: TimelineProps) {
  const styles = useStyles();
  const {
    tracks,
    currentTime,
    zoom,
    isPlaying,
    setCurrentTime,
    setZoom,
    setPlaying,
    updateTrack,
    toggleMute,
    toggleSolo,
    toggleLock,
  } = useTimelineStore();

  const [showShortcuts, setShowShortcuts] = useState(false);
  const timelineRef = useRef<HTMLDivElement>(null);

  // Format time display
  const formatTime = (time: number): string => {
    const mins = Math.floor(time / 60);
    const secs = Math.floor(time % 60);
    const frames = Math.floor((time % 1) * 30);
    return `${mins}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
  };

  // Keyboard shortcut handlers
  const keyboardHandlers = {
    onPlayPause: useCallback(() => {
      setPlaying(!isPlaying);
    }, [isPlaying, setPlaying]),

    onRewind: useCallback(() => {
      setCurrentTime(Math.max(0, currentTime - 1));
    }, [currentTime, setCurrentTime]),

    onFastForward: useCallback(() => {
      setCurrentTime(Math.min(duration, currentTime + 1));
    }, [currentTime, duration, setCurrentTime]),

    onFrameBackward: useCallback(() => {
      setCurrentTime(Math.max(0, currentTime - 1 / 30));
    }, [currentTime, setCurrentTime]),

    onFrameForward: useCallback(() => {
      setCurrentTime(Math.min(duration, currentTime + 1 / 30));
    }, [currentTime, duration, setCurrentTime]),

    onSecondBackward: useCallback(() => {
      setCurrentTime(Math.max(0, currentTime - 1));
    }, [currentTime, setCurrentTime]),

    onSecondForward: useCallback(() => {
      setCurrentTime(Math.min(duration, currentTime + 1));
    }, [currentTime, duration, setCurrentTime]),

    onJumpToStart: useCallback(() => {
      setCurrentTime(0);
    }, [setCurrentTime]),

    onJumpToEnd: useCallback(() => {
      setCurrentTime(duration);
    }, [duration, setCurrentTime]),

    onSplice: useCallback(() => {
      // Placeholder: Split clip at current playhead position
      // Requires integration with timeline store's splitClip method
    }, []),

    onRippleDelete: useCallback(() => {
      // Placeholder: Delete selected clips and shift subsequent clips left
      // Requires integration with timeline store
    }, []),

    onDelete: useCallback(() => {
      // Placeholder: Delete selected clips without shifting others
      // Requires integration with timeline store
    }, []),

    onCopy: useCallback(() => {
      // Placeholder: Copy selected clips to clipboard
      // Requires integration with timeline store
    }, []),

    onPaste: useCallback(() => {
      // Placeholder: Paste clips from clipboard at playhead position
      // Requires integration with timeline store
    }, []),

    onDuplicate: useCallback(() => {
      // Placeholder: Duplicate selected clips
      // Requires integration with timeline store
    }, []),

    onUndo: useCallback(() => {
      // Placeholder: Undo last operation
      // Would pop from undo stack and apply previous state
    }, []),

    onRedo: useCallback(() => {
      // Placeholder: Redo last undone operation
      // Would push to redo stack and apply next state
    }, []),

    onZoomIn: useCallback(() => {
      setZoom(Math.min(200, zoom * 1.2));
    }, [zoom, setZoom]),

    onZoomOut: useCallback(() => {
      setZoom(Math.max(10, zoom / 1.2));
    }, [zoom, setZoom]),

    onSave: useCallback(() => {
      onSave?.();
    }, [onSave]),

    onShowShortcuts: useCallback(() => {
      setShowShortcuts(true);
    }, []),
  };

  useTimelineKeyboardShortcuts(keyboardHandlers, true);

  // Handle zoom to fit
  const handleFitToView = useCallback(() => {
    if (!timelineRef.current) return;
    const width = timelineRef.current.clientWidth - 120; // Minus track labels
    const newZoom = width / duration;
    setZoom(Math.max(10, Math.min(200, newZoom)));
  }, [duration, setZoom]);

  return (
    <div className={styles.container}>
      {/* Toolbar */}
      <div className={styles.toolbar}>
        <div className={styles.toolbarLeft}>
          <Button
            icon={isPlaying ? <Pause24Regular /> : <Play24Regular />}
            onClick={() => setPlaying(!isPlaying)}
            appearance="primary"
          >
            {isPlaying ? 'Pause' : 'Play'}
          </Button>
          <Button icon={<Cut24Regular />} appearance="subtle">
            Splice
          </Button>
          <Button icon={<Delete24Regular />} appearance="subtle">
            Delete
          </Button>
          <Button icon={<Copy24Regular />} appearance="subtle">
            Copy
          </Button>
          <div style={{ marginLeft: '20px', color: tokens.colorNeutralForeground2 }}>
            {formatTime(currentTime)}
          </div>
        </div>
        <div className={styles.toolbarRight}>
          <Dialog open={showShortcuts} onOpenChange={(_, data) => setShowShortcuts(data.open)}>
            <DialogTrigger>
              <Button
                icon={<Question24Regular />}
                appearance="subtle"
                title="Keyboard shortcuts (?)"
              >
                Shortcuts
              </Button>
            </DialogTrigger>
            <DialogSurface className={styles.shortcutsDialog}>
              <DialogBody>
                <DialogTitle>Keyboard Shortcuts</DialogTitle>
                <DialogContent>
                  <div className={styles.shortcutsList}>
                    {getKeyboardShortcuts().map((shortcut, idx) => (
                      <div key={idx} className={styles.shortcutItem}>
                        <span className={styles.shortcutKeys}>{shortcut.keys}</span>
                        <span className={styles.shortcutDesc}>{shortcut.description}</span>
                      </div>
                    ))}
                  </div>
                </DialogContent>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        </div>
      </div>

      {/* Zoom controls */}
      <TimelineZoomControls
        zoom={zoom}
        timelineDuration={duration}
        onZoomChange={setZoom}
        onFitToView={handleFitToView}
      />

      {/* Timeline */}
      <div className={styles.timelineContainer} ref={timelineRef}>
        {/* Ruler */}
        <div className={styles.ruler}>{/* Would render time markers here */}</div>

        {/* Tracks */}
        <div className={styles.tracksContainer}>
          {tracks.map((track) => (
            <div key={track.id} style={{ display: 'flex' }}>
              <AudioTrackControls
                trackName={track.name}
                trackType={track.type === 'audio' ? 'music' : 'music'}
                muted={track.muted}
                solo={track.solo}
                volume={track.volume}
                pan={track.pan}
                locked={track.locked}
                onMuteToggle={() => toggleMute(track.id)}
                onSoloToggle={() => toggleSolo(track.id)}
                onVolumeChange={(vol) => updateTrack(track.id, { volume: vol })}
                onPanChange={(pan) => updateTrack(track.id, { pan })}
                onLockToggle={() => toggleLock(track.id)}
              />
              <TimelineTrack
                name={track.name}
                type="music"
                duration={duration}
                zoom={zoom}
                muted={track.muted}
                selected={false}
                onSeek={setCurrentTime}
              />
            </div>
          ))}

          {/* Playhead */}
          <div className={styles.playhead} style={{ left: `${120 + currentTime * zoom}px` }}>
            <div className={styles.playheadTime}>{formatTime(currentTime)}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
