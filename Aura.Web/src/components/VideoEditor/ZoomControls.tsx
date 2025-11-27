/**
 * ZoomControls - Component for controlling preview zoom level
 *
 * Provides zoom in/out buttons, a slider, and a fit-to-window button
 * for adjusting the video preview zoom level.
 */

import { Button, Slider, makeStyles, tokens } from '@fluentui/react-components';
import { ZoomInRegular, ZoomOutRegular, FullScreenMaximizeRegular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  slider: {
    width: '100px',
  },
  zoomLabel: {
    fontSize: tokens.fontSizeBase200,
    minWidth: '50px',
    color: tokens.colorNeutralForeground2,
  },
});

interface ZoomControlsProps {
  zoom: number;
  onZoomChange: (zoom: number) => void;
  onZoomFit: () => void;
  disabled?: boolean;
}

export function ZoomControls({
  zoom,
  onZoomChange,
  onZoomFit,
  disabled = false,
}: ZoomControlsProps) {
  const styles = useStyles();

  const handleZoomIn = () => {
    onZoomChange(Math.min(zoom + 0.25, 3.0));
  };

  const handleZoomOut = () => {
    onZoomChange(Math.max(zoom - 0.25, 0.25));
  };

  return (
    <div className={styles.container}>
      <Button
        icon={<ZoomOutRegular />}
        onClick={handleZoomOut}
        disabled={disabled || zoom <= 0.25}
        size="small"
        appearance="subtle"
        title="Zoom Out"
      />
      <Slider
        min={0.25}
        max={3.0}
        step={0.25}
        value={zoom}
        onChange={(_, data) => onZoomChange(data.value)}
        className={styles.slider}
        disabled={disabled}
      />
      <Button
        icon={<ZoomInRegular />}
        onClick={handleZoomIn}
        disabled={disabled || zoom >= 3.0}
        size="small"
        appearance="subtle"
        title="Zoom In"
      />
      <Button
        icon={<FullScreenMaximizeRegular />}
        onClick={onZoomFit}
        size="small"
        appearance="subtle"
        disabled={disabled}
        title="Fit to Window"
      />
      <span className={styles.zoomLabel}>{Math.round(zoom * 100)}%</span>
    </div>
  );
}
