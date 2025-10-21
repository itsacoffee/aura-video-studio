/**
 * Enhanced Timeline component with advanced editing features
 */

import { useRef, useState, useCallback, useEffect } from 'react';
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
import { useTimelineStore } from '../../../state/timeline';
import { TimelineZoomControls } from './TimelineZoomControls';
import { TimelineTrack } from './TimelineTrack';
import { AudioTrackControls } from './AudioTrackControls';
import { SceneBlock } from './SceneBlock';
import { useTimelineKeyboardShortcuts, getKeyboardShortcuts } from '../../../hooks/useTimelineKeyboardShortcuts';
import { timelineEditor } from '../../../services/timeline/TimelineEditor';
import { clipboardService } from '../../../services/timeline/ClipboardService';
import { snappingService } from '../../../services/timeline/SnappingService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  toolbarLeft: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  toolbarRight: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  timelineContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  ruler: {
    height: '30px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    position: 'relative',
    backgroundColor: tokens.colorNeutralBackground2,
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
    backgroundColor: tokens.colorPaletteRedBackground3,
    pointerEvents: 'none',
    zIndex: 100,
  },
  playheadTime: {
    position: 'absolute',
    top: '-20px',
    left: '50%',
    transform: 'translateX(-50%)',
    padding: '2px 6px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    color: tokens.colorNeutralForegroundOnBrand,
    fontSize: tokens.fontSizeBase100,
    borderRadius: tokens.borderRadiusSmall,
    whiteSpace: 'nowrap',
  },
  snapLine: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '1px',
    backgroundColor: tokens.colorPaletteYellowBackground3,
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
    snappingEnabled,
    isPlaying,
    selectedClipId,
    setCurrentTime,
    setZoom,
    setSnappingEnabled,
    setPlaying,
    setSelectedClipId,
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
      setCurrentTime(Math.max(0, currentTime - (1 / 30)));
    }, [currentTime, setCurrentTime]),
    
    onFrameForward: useCallback(() => {
      setCurrentTime(Math.min(duration, currentTime + (1 / 30)));
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
      // Would implement splice logic here
      console.log('Splice at playhead');
    }, []),
    
    onRippleDelete: useCallback(() => {
      // Would implement ripple delete here
      console.log('Ripple delete');
    }, []),
    
    onDelete: useCallback(() => {
      // Would implement delete here
      console.log('Delete');
    }, []),
    
    onCopy: useCallback(() => {
      // Would implement copy here
      console.log('Copy');
    }, []),
    
    onPaste: useCallback(() => {
      // Would implement paste here
      console.log('Paste');
    }, []),
    
    onDuplicate: useCallback(() => {
      // Would implement duplicate here
      console.log('Duplicate');
    }, []),
    
    onUndo: useCallback(() => {
      // Would implement undo here
      console.log('Undo');
    }, []),
    
    onRedo: useCallback(() => {
      // Would implement redo here
      console.log('Redo');
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
              <Button icon={<Question24Regular />} appearance="subtle" title="Keyboard shortcuts (?)">
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
        <div className={styles.ruler}>
          {/* Would render time markers here */}
        </div>

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
