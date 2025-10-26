import { makeStyles, tokens, Text } from '@fluentui/react-components';
import { SnapPoint } from '../../services/timelineEngine';

const useStyles = makeStyles({
  snapGuide: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '1px',
    backgroundColor: tokens.colorPaletteBlueForeground2,
    zIndex: 15,
    pointerEvents: 'none',
    opacity: 0.8,
  },
  snapGuideDashed: {
    borderLeft: `2px dashed ${tokens.colorPaletteBlueForeground2}`,
    backgroundColor: 'transparent',
  },
  snapLabel: {
    position: 'absolute',
    top: '50%',
    left: '4px',
    transform: 'translateY(-50%)',
    backgroundColor: tokens.colorPaletteBlueForeground2,
    color: tokens.colorNeutralBackground1,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: 'nowrap',
    fontWeight: tokens.fontWeightSemibold,
    boxShadow: tokens.shadow4,
  },
  offsetIndicator: {
    position: 'absolute',
    top: '30px',
    left: '50%',
    transform: 'translateX(-50%)',
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground2,
    whiteSpace: 'nowrap',
    boxShadow: tokens.shadow4,
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
