import { makeStyles, Text } from '@fluentui/react-components';
import { SnapPoint } from '../../services/timelineEngine';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  snapGuide: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: 'var(--editor-accent)',
    boxShadow: '0 0 8px var(--editor-focus-ring)',
    zIndex: 'var(--editor-z-toolbar)',
    pointerEvents: 'none',
    opacity: 0.9,
    transition: 'all var(--editor-transition-fast)',
  },
  snapGuideDashed: {
    borderLeft: `2px dashed var(--editor-accent)`,
    backgroundColor: 'transparent',
    boxShadow: '0 0 6px var(--editor-focus-ring)',
  },
  snapLabel: {
    position: 'absolute',
    top: '50%',
    left: '4px',
    transform: 'translateY(-50%)',
    backgroundColor: 'var(--editor-accent)',
    color: 'white',
    padding: 'var(--editor-space-xs) var(--editor-space-sm)',
    borderRadius: 'var(--editor-radius-sm)',
    fontSize: 'var(--editor-font-size-xs)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    whiteSpace: 'nowrap',
    boxShadow: 'var(--editor-shadow-md)',
    transition: 'all var(--editor-transition-fast)',
    animation: 'editorFadeIn var(--editor-transition-base) ease-out',
  },
  offsetIndicator: {
    position: 'absolute',
    top: '30px',
    left: '50%',
    transform: 'translateX(-50%)',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-accent)`,
    padding: 'var(--editor-space-xs) var(--editor-space-sm)',
    borderRadius: 'var(--editor-radius-sm)',
    fontSize: 'var(--editor-font-size-xs)',
    fontWeight: 'var(--editor-font-weight-medium)',
    color: 'var(--editor-text-primary)',
    whiteSpace: 'nowrap',
    boxShadow: 'var(--editor-shadow-md)',
    transition: 'all var(--editor-transition-fast)',
    animation: 'editorFadeIn var(--editor-transition-base) ease-out',
  },
});

interface SnapGuidesProps {
  activeSnapPoint: SnapPoint | null;
  pixelsPerSecond: number;
  offsetDistance?: number;
  trackLabelWidth?: number;
}

export function SnapGuides({
  activeSnapPoint,
  pixelsPerSecond,
  offsetDistance,
  trackLabelWidth = 100,
}: SnapGuidesProps) {
  const styles = useStyles();

  if (!activeSnapPoint) return null;

  const leftPosition = activeSnapPoint.time * pixelsPerSecond + trackLabelWidth;

  const getSnapLabel = (point: SnapPoint): string => {
    switch (point.type) {
      case 'clip-start':
        return 'Clip Start';
      case 'clip-end':
        return 'Clip End';
      case 'playhead':
        return 'Playhead';
      case 'marker':
        return point.label || 'Marker';
      case 'in-point':
        return 'In Point';
      case 'out-point':
        return 'Out Point';
      case 'caption':
        return point.label ? `Caption: ${point.label}` : 'Caption';
      case 'audio-peak':
        return `Audio Peak ${point.intensity ? `(${Math.round(point.intensity * 100)}%)` : ''}`;
      case 'scene-boundary':
        return 'Scene Boundary';
      default:
        return 'Snap';
    }
  };

  const formatOffset = (distance: number): string => {
    const frames = Math.round(Math.abs(distance) * 30); // Assuming 30 fps
    const sign = distance < 0 ? '-' : '+';
    return `${sign}${frames}f`;
  };

  return (
    <>
      <div
        className={`${styles.snapGuide} ${styles.snapGuideDashed}`}
        style={{ left: `${leftPosition}px` }}
      >
        <div className={styles.snapLabel}>
          <Text>{getSnapLabel(activeSnapPoint)}</Text>
        </div>
      </div>
      {offsetDistance !== undefined && Math.abs(offsetDistance) > 0.001 && (
        <div className={styles.offsetIndicator} style={{ left: `${leftPosition}px` }}>
          <Text>{formatOffset(offsetDistance)}</Text>
        </div>
      )}
    </>
  );
}
