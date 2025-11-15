/**
 * Timeline zoom controls component
 */

import { makeStyles, Button, Slider, Label } from '@fluentui/react-components';
import { ZoomIn24Regular, ZoomOut24Regular, ZoomFit24Regular } from '@fluentui/react-icons';
import '../../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--editor-space-md)',
    padding: 'var(--editor-space-sm)',
    borderBottom: `1px solid var(--editor-panel-border)`,
    backgroundColor: 'var(--editor-panel-header-bg)',
    transition: 'background-color var(--editor-transition-fast)',
  },
  zoomSlider: {
    width: '200px',
  },
  zoomLevel: {
    fontSize: 'var(--editor-font-size-sm)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-primary)',
    minWidth: '60px',
    textAlign: 'center',
    padding: 'var(--editor-space-xs) var(--editor-space-sm)',
    backgroundColor: 'var(--editor-panel-active)',
    borderRadius: 'var(--editor-radius-sm)',
    border: `1px solid var(--editor-panel-border)`,
    transition: 'all var(--editor-transition-fast)',
  },
  presetButtons: {
    display: 'flex',
    gap: 'var(--editor-space-xs)',
  },
  button: {
    transition: 'all var(--editor-transition-fast)',
    '&:hover:not(:disabled)': {
      backgroundColor: 'var(--editor-panel-hover)',
      color: 'var(--editor-accent)',
    },
    '&:active:not(:disabled)': {
      backgroundColor: 'var(--editor-panel-active)',
      transform: 'scale(0.98)',
    },
    '&:disabled': {
      opacity: 0.4,
      cursor: 'not-allowed',
    },
  },
  presetButton: {
    transition: 'all var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      transform: 'translateY(-1px)',
      boxShadow: 'var(--editor-shadow-sm)',
    },
    '&:active': {
      transform: 'translateY(0)',
    },
  },
});

export interface TimelineZoomControlsProps {
  zoom: number; // 1-1000x (pixels per second)
  minZoom?: number;
  maxZoom?: number;
  timelineDuration?: number;
  onZoomChange?: (zoom: number) => void;
  onFitToView?: () => void;
}

export function TimelineZoomControls({
  zoom,
  minZoom = 10,
  maxZoom = 200,
  onZoomChange,
  onFitToView,
}: TimelineZoomControlsProps) {
  const styles = useStyles();

  // Convert zoom to logarithmic scale for natural feel
  const zoomToSlider = (z: number): number => {
    return (Math.log(z / minZoom) / Math.log(maxZoom / minZoom)) * 100;
  };

  const sliderToZoom = (value: number): number => {
    return minZoom * Math.pow(maxZoom / minZoom, value / 100);
  };

  const handleZoomIn = () => {
    const newZoom = Math.min(maxZoom, zoom * 1.5);
    onZoomChange?.(newZoom);
  };

  const handleZoomOut = () => {
    const newZoom = Math.max(minZoom, zoom / 1.5);
    onZoomChange?.(newZoom);
  };

  const handleSliderChange = (_: unknown, data: { value: number }) => {
    const newZoom = sliderToZoom(data.value);
    onZoomChange?.(newZoom);
  };

  const handlePresetZoom = (preset: 'fit' | '1s' | '10f') => {
    if (preset === 'fit') {
      onFitToView?.();
    } else if (preset === '1s') {
      // 1 second view = zoom level that shows exactly 1 second
      onZoomChange?.(100); // 100 pixels per second
    } else if (preset === '10f') {
      // 10 frames view (assuming 30fps)
      onZoomChange?.(150); // Show about 10 frames
    }
  };

  // Format zoom level display
  const formatZoomLevel = (z: number): string => {
    if (z >= 100) {
      return `${Math.round(z / 10)}x`;
    } else if (z >= 50) {
      return `${Math.round(z / 5)}x`;
    } else {
      return `${Math.round(z / minZoom)}x`;
    }
  };

  return (
    <div className={styles.container}>
      <Label>Zoom:</Label>

      <Button
        icon={<ZoomOut24Regular />}
        appearance="subtle"
        onClick={handleZoomOut}
        disabled={zoom <= minZoom}
        title="Zoom out (-)"
        className={styles.button}
      />

      <div className={styles.zoomSlider}>
        <Slider
          min={0}
          max={100}
          value={zoomToSlider(zoom)}
          onChange={handleSliderChange}
          size="small"
        />
      </div>

      <Button
        icon={<ZoomIn24Regular />}
        appearance="subtle"
        onClick={handleZoomIn}
        disabled={zoom >= maxZoom}
        title="Zoom in (+)"
        className={styles.button}
      />

      <div className={styles.zoomLevel}>{formatZoomLevel(zoom)}</div>

      <div className={styles.presetButtons}>
        <Button
          size="small"
          appearance="subtle"
          icon={<ZoomFit24Regular />}
          onClick={() => handlePresetZoom('fit')}
          title="Fit all"
          className={styles.presetButton}
        >
          Fit All
        </Button>
        <Button
          size="small"
          appearance="subtle"
          onClick={() => handlePresetZoom('1s')}
          title="Zoom to 1 second"
          className={styles.presetButton}
        >
          1 Sec
        </Button>
        <Button
          size="small"
          appearance="subtle"
          onClick={() => handlePresetZoom('10f')}
          title="Zoom to 10 frames"
          className={styles.presetButton}
        >
          10 Frames
        </Button>
      </div>
    </div>
  );
}
