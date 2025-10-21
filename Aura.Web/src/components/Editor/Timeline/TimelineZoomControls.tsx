/**
 * Timeline zoom controls component
 */

import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Slider,
  Label,
} from '@fluentui/react-components';
import {
  ZoomIn24Regular,
  ZoomOut24Regular,
  ZoomFit24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  zoomSlider: {
    width: '200px',
  },
  zoomLevel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    minWidth: '60px',
    textAlign: 'center',
  },
  presetButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
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
  timelineDuration = 60,
  onZoomChange,
  onFitToView,
}: TimelineZoomControlsProps) {
  const styles = useStyles();

  // Convert zoom to logarithmic scale for natural feel
  const zoomToSlider = (z: number): number => {
    return Math.log(z / minZoom) / Math.log(maxZoom / minZoom) * 100;
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

  const handleSliderChange = (_: any, data: { value: number }) => {
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
      />

      <div className={styles.zoomLevel}>{formatZoomLevel(zoom)}</div>

      <div className={styles.presetButtons}>
        <Button
          size="small"
          appearance="subtle"
          icon={<ZoomFit24Regular />}
          onClick={() => handlePresetZoom('fit')}
          title="Fit all"
        >
          Fit All
        </Button>
        <Button
          size="small"
          appearance="subtle"
          onClick={() => handlePresetZoom('1s')}
          title="Zoom to 1 second"
        >
          1 Sec
        </Button>
        <Button
          size="small"
          appearance="subtle"
          onClick={() => handlePresetZoom('10f')}
          title="Zoom to 10 frames"
        >
          10 Frames
        </Button>
      </div>
    </div>
  );
}
