/**
 * MarkerTrack Component
 *
 * Dedicated marker track displayed above the timeline tracks.
 * Shows all marker flags with zoom-aware positioning and
 * supports range markers with duration bars.
 */

import { makeStyles, tokens, Text, mergeClasses } from '@fluentui/react-components';
import { AnimatePresence } from 'framer-motion';
import type { FC } from 'react';
import { useCallback } from 'react';
import type { Marker } from '../../../types/opencut';
import { MarkerFlag } from './MarkerFlag';

export interface MarkerTrackProps {
  markers: Marker[];
  selectedMarkerId: string | null;
  pixelsPerSecond: number;
  totalWidth: number;
  onSelectMarker: (markerId: string) => void;
  onMoveMarker: (markerId: string, newTime: number) => void;
  onMarkerClick?: (marker: Marker) => void;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
    minHeight: '32px',
    position: 'relative',
  },
  label: {
    width: '140px',
    flexShrink: 0,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  labelText: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    fontWeight: tokens.fontWeightMedium,
  },
  track: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
  },
  trackContent: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
  },
  emptyTrack: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    opacity: 0.5,
  },
});

export const MarkerTrack: FC<MarkerTrackProps> = ({
  markers,
  selectedMarkerId,
  pixelsPerSecond,
  totalWidth,
  onSelectMarker,
  onMoveMarker,
  onMarkerClick,
  className,
}) => {
  const styles = useStyles();

  const handleSelect = useCallback(
    (markerId: string) => {
      onSelectMarker(markerId);
    },
    [onSelectMarker]
  );

  const handleMove = useCallback(
    (markerId: string, newTime: number) => {
      onMoveMarker(markerId, newTime);
    },
    [onMoveMarker]
  );

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.label}>
        <Text className={styles.labelText}>Markers</Text>
      </div>
      <div className={styles.track}>
        <div className={styles.trackContent} style={{ width: totalWidth }}>
          <AnimatePresence>
            {markers.map((marker) => (
              <MarkerFlag
                key={marker.id}
                marker={marker}
                isSelected={marker.id === selectedMarkerId}
                pixelsPerSecond={pixelsPerSecond}
                onSelect={handleSelect}
                onMove={handleMove}
                onClick={onMarkerClick}
              />
            ))}
          </AnimatePresence>
          {markers.length === 0 && (
            <div className={styles.emptyTrack} style={{ width: '100%', height: '100%' }}>
              <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>
                Press M to add a marker
              </Text>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default MarkerTrack;
