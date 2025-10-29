import {
  makeStyles,
  tokens,
  Button,
  Switch,
  Slider,
  Label,
  Card,
} from '@fluentui/react-components';
import { Play24Regular, Pause24Regular, Cut24Regular } from '@fluentui/react-icons';
import { useEffect, useCallback } from 'react';
import { useTimelineStore } from '../../state/timeline';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    height: '100%',
  },
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  timeline: {
    flex: 1,
    position: 'relative',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'auto',
  },
  ruler: {
    height: '30px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    position: 'relative',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  tracks: {
    display: 'flex',
    flexDirection: 'column',
  },
  track: {
    height: '60px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    position: 'relative',
    display: 'flex',
  },
  trackLabel: {
    width: '80px',
    padding: tokens.spacingHorizontalM,
    display: 'flex',
    alignItems: 'center',
    borderRight: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground3,
    fontWeight: tokens.fontWeightSemibold,
  },
  trackContent: {
    flex: 1,
    position: 'relative',
  },
  clip: {
    position: 'absolute',
    top: '5px',
    height: '50px',
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    padding: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForegroundOnBrand,
    fontSize: tokens.fontSizeBase200,
    overflow: 'hidden',
    whiteSpace: 'nowrap',
    textOverflow: 'ellipsis',
    border: `2px solid transparent`,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
  clipSelected: {
    border: `2px solid ${tokens.colorBrandForeground1}`,
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
  marker: {
    position: 'absolute',
    top: 0,
    width: '2px',
    height: '30px',
    backgroundColor: tokens.colorPaletteYellowBackground3,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorPaletteYellowForeground1,
    },
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
});

const PIXELS_PER_SECOND = 50;

export function TimelineView() {
  const styles = useStyles();
  const {
    tracks,
    markers,
    snappingEnabled,
    currentTime,
    zoom,
    selectedClipId,
    setSnappingEnabled,
    setCurrentTime,
    setZoom,
    setSelectedClipId,
    splitClip,
    addMarker,
  } = useTimelineStore();

  const pixelsPerSecond = PIXELS_PER_SECOND * zoom;

  const handleKeyPress = useCallback(
    (event: KeyboardEvent) => {
      if (event.code === 'Space') {
        event.preventDefault();
      } else if (event.code === 'KeyS') {
        if (selectedClipId) {
          splitClip(selectedClipId, currentTime);
        }
      } else if (event.code === 'KeyQ') {
        setCurrentTime(Math.max(0, currentTime - 1));
      } else if (event.code === 'KeyW') {
        setCurrentTime(currentTime + 1);
      } else if (event.code === 'Minus') {
        setZoom(Math.max(0.1, zoom - 0.1));
      } else if (event.code === 'Equal') {
        setZoom(Math.min(5, zoom + 0.1));
      }
    },
    [selectedClipId, currentTime, zoom, splitClip, setCurrentTime, setZoom]
  );

  useEffect(() => {
    window.addEventListener('keydown', handleKeyPress);
    return () => {
      window.removeEventListener('keydown', handleKeyPress);
    };
  }, [handleKeyPress]);

  const handleTimelineClick = (event: React.MouseEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - rect.left - 80;
    const time = Math.max(0, x / pixelsPerSecond);
    setCurrentTime(time);
  };

  const handleClipClick = (clipId: string, event: React.MouseEvent) => {
    event.stopPropagation();
    setSelectedClipId(clipId);
  };

  const handleSplitClick = () => {
    if (selectedClipId) {
      splitClip(selectedClipId, currentTime);
    }
  };

  const handleAddMarker = () => {
    const marker = {
      id: `marker_${Date.now()}`,
      title: 'Chapter',
      time: currentTime,
    };
    addMarker(marker);
  };

  const maxDuration = Math.max(
    ...tracks.flatMap((t) => t.clips.map((c) => c.timelineStart + (c.sourceOut - c.sourceIn))),
    currentTime + 10
  );

  return (
    <div className={styles.container}>
      <div className={styles.toolbar}>
        <div className={styles.controls}>
          <Button icon={<Play24Regular />} appearance="subtle">
            Play
          </Button>
          <Button icon={<Pause24Regular />} appearance="subtle">
            Pause
          </Button>
        </div>

        <div className={styles.controls}>
          <Button icon={<Cut24Regular />} onClick={handleSplitClick} disabled={!selectedClipId}>
            Split (S)
          </Button>
          <Button onClick={handleAddMarker}>Add Marker</Button>
        </div>

        <div
          style={{
            display: 'flex',
            gap: tokens.spacingHorizontalM,
            alignItems: 'center',
            marginLeft: 'auto',
          }}
        >
          <Label>Snapping</Label>
          <Switch
            checked={snappingEnabled}
            onChange={(_, data) => setSnappingEnabled(data.checked)}
          />
          <Label>Zoom</Label>
          <Slider
            value={zoom}
            min={0.1}
            max={3}
            step={0.1}
            onChange={(_, data) => setZoom(data.value)}
            style={{ width: '100px' }}
          />
        </div>
      </div>

      <div className={styles.timeline}>
        <div
          className={styles.ruler}
          role="button"
          tabIndex={0}
          onClick={handleTimelineClick}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              // Timeline click position requires mouse coordinates
            }
          }}
        >
          {Array.from({ length: Math.ceil(maxDuration) + 1 }).map((_, i) => (
            <div
              key={i}
              style={{
                position: 'absolute',
                left: `${80 + i * pixelsPerSecond}px`,
                top: 0,
                height: '100%',
                borderLeft: `1px solid ${tokens.colorNeutralStroke2}`,
                paddingLeft: '2px',
                fontSize: tokens.fontSizeBase200,
                color: tokens.colorNeutralForeground3,
              }}
            >
              {i}s
            </div>
          ))}
          {markers.map((marker) => (
            <div
              key={marker.id}
              className={styles.marker}
              style={{ left: `${80 + marker.time * pixelsPerSecond}px` }}
              title={marker.title}
            />
          ))}
          <div
            className={styles.playhead}
            style={{ left: `${80 + currentTime * pixelsPerSecond}px` }}
          />
        </div>

        <div className={styles.tracks}>
          {tracks.map((track) => (
            <div
              key={track.id}
              className={styles.track}
              role="button"
              tabIndex={0}
              onClick={handleTimelineClick}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  // Timeline click requires mouse coordinates for precise positioning
                }
              }}
            >
              <div className={styles.trackLabel}>{track.name}</div>
              <div className={styles.trackContent}>
                {track.clips.map((clip) => {
                  const duration = clip.sourceOut - clip.sourceIn;
                  const left = clip.timelineStart * pixelsPerSecond;
                  const width = duration * pixelsPerSecond;
                  const isSelected = clip.id === selectedClipId;

                  return (
                    <div
                      key={clip.id}
                      className={`${styles.clip} ${isSelected ? styles.clipSelected : ''}`}
                      style={{ left: `${left}px`, width: `${width}px` }}
                      role="button"
                      tabIndex={0}
                      onClick={(e) => handleClipClick(clip.id, e)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          handleClipClick(clip.id, e as unknown as React.MouseEvent);
                        }
                      }}
                      title={`${clip.sourcePath} (${duration.toFixed(1)}s)`}
                    >
                      Clip {duration.toFixed(1)}s
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      </div>

      <Card style={{ padding: tokens.spacingVerticalM }}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            fontSize: tokens.fontSizeBase200,
          }}
        >
          <span>Time: {currentTime.toFixed(2)}s</span>
          <span>Zoom: {(zoom * 100).toFixed(0)}%</span>
          <span>Snapping: {snappingEnabled ? 'On' : 'Off'}</span>
          <span>Shortcuts: Space (Play/Pause) | S (Split) | Q/W (Seek) | +/- (Zoom)</span>
        </div>
      </Card>
    </div>
  );
}
