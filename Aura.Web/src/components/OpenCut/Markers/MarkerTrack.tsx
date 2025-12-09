/**
 * MarkerTrack Component
 *
 * A dedicated track above the timeline ruler that displays all marker flags.
 * Supports zoom-aware positioning, color-coding by type, and interaction.
 */

import { makeStyles, tokens, Button, Tooltip } from '@fluentui/react-components';
import { Add24Regular } from '@fluentui/react-icons';
import { AnimatePresence } from 'framer-motion';
import { useState, useCallback, useRef } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import type { Marker, MarkerType } from '../../../types/opencut';
import { MarkerEditor } from './MarkerEditor';
import { MarkerFlag } from './MarkerFlag';

export interface MarkerTrackProps {
  markers: Marker[];
  selectedMarkerId: string | null;
  pixelsPerSecond: number;
  totalWidth: number;
  currentTime: number;
  onSelectMarker: (markerId: string | null) => void;
  onMoveMarker: (markerId: string, newTime: number) => void;
  onUpdateMarker: (markerId: string, updates: Partial<Marker>) => void;
  onDeleteMarker: (markerId: string) => void;
  onAddMarker: (time: number, type?: MarkerType) => void;
  onSeek: (time: number) => void;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    height: '24px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    overflow: 'hidden',
  },
  labelArea: {
    width: '140px',
    flexShrink: 0,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `0 ${openCutTokens.spacing.xs}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  labelText: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: tokens.colorNeutralForeground3,
    fontWeight: openCutTokens.typography.fontWeight.medium,
  },
  addButton: {
    minWidth: '20px',
    minHeight: '20px',
    padding: '2px',
  },
  trackArea: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
    cursor: 'crosshair',
  },
  track: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
  },
});

export const MarkerTrack: FC<MarkerTrackProps> = ({
  markers,
  selectedMarkerId,
  pixelsPerSecond,
  totalWidth,
  currentTime,
  onSelectMarker,
  onMoveMarker,
  onUpdateMarker,
  onDeleteMarker,
  onAddMarker,
  onSeek,
}) => {
  const styles = useStyles();
  const [editingMarkerId, setEditingMarkerId] = useState<string | null>(null);
  const dragStartTimeRef = useRef<number>(0);
  const trackRef = useRef<HTMLDivElement>(null);

  const handleTrackClick = useCallback(
    (e: ReactMouseEvent) => {
      if (!trackRef.current) return;

      const rect = trackRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const time = x / pixelsPerSecond;

      // If clicking empty area, deselect and seek
      if (e.target === trackRef.current) {
        onSelectMarker(null);
        onSeek(Math.max(0, time));
      }
    },
    [pixelsPerSecond, onSelectMarker, onSeek]
  );

  const handleTrackDoubleClick = useCallback(
    (e: ReactMouseEvent) => {
      if (!trackRef.current) return;

      const rect = trackRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const time = x / pixelsPerSecond;

      // Double-click to add a marker
      onAddMarker(Math.max(0, time));
    },
    [pixelsPerSecond, onAddMarker]
  );

  const handleMarkerClick = useCallback(
    (markerId: string, e: ReactMouseEvent) => {
      e.stopPropagation();
      onSelectMarker(markerId);
    },
    [onSelectMarker]
  );

  const handleMarkerDoubleClick = useCallback((markerId: string) => {
    setEditingMarkerId(markerId);
  }, []);

  const handleDragStart = useCallback(
    (markerId: string) => {
      const marker = markers.find((m) => m.id === markerId);
      if (marker) {
        dragStartTimeRef.current = marker.time;
      }
    },
    [markers]
  );

  const handleDrag = useCallback(
    (markerId: string, deltaX: number) => {
      const deltaTime = deltaX / pixelsPerSecond;
      const newTime = Math.max(0, dragStartTimeRef.current + deltaTime);
      onMoveMarker(markerId, newTime);
    },
    [pixelsPerSecond, onMoveMarker]
  );

  const handleDragEnd = useCallback(() => {
    // Drag completed - state already updated
  }, []);

  const handleAddMarkerAtPlayhead = useCallback(() => {
    onAddMarker(currentTime);
  }, [currentTime, onAddMarker]);

  const editingMarker = editingMarkerId ? markers.find((m) => m.id === editingMarkerId) : null;

  return (
    <div className={styles.container}>
      {/* Label Area */}
      <div className={styles.labelArea}>
        <span className={styles.labelText}>Markers</span>
        <Tooltip content="Add marker at playhead (M)" relationship="label">
          <Button
            appearance="subtle"
            icon={<Add24Regular />}
            size="small"
            className={styles.addButton}
            onClick={handleAddMarkerAtPlayhead}
          />
        </Tooltip>
      </div>

      {/* Track Area */}
      {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/click-events-have-key-events */}
      <div
        ref={trackRef}
        className={styles.trackArea}
        onClick={handleTrackClick}
        onDoubleClick={handleTrackDoubleClick}
        role="region"
        aria-label="Marker track. Double-click to add a marker."
      >
        <div className={styles.track} style={{ width: totalWidth }}>
          <AnimatePresence>
            {markers.map((marker) => {
              const position = marker.time * pixelsPerSecond;

              return (
                <MarkerFlag
                  key={marker.id}
                  marker={marker}
                  position={position}
                  isSelected={selectedMarkerId === marker.id}
                  onClick={handleMarkerClick}
                  onDoubleClick={handleMarkerDoubleClick}
                  onDragStart={handleDragStart}
                  onDrag={handleDrag}
                  onDragEnd={handleDragEnd}
                />
              );
            })}
          </AnimatePresence>
        </div>
      </div>

      {/* Marker Editor Popover */}
      {editingMarker && (
        <MarkerEditor
          marker={editingMarker}
          trigger={<span style={{ display: 'none' }} />}
          open={!!editingMarkerId}
          onOpenChange={(open) => {
            if (!open) setEditingMarkerId(null);
          }}
          onUpdate={(updates) => {
            if (editingMarkerId) {
              onUpdateMarker(editingMarkerId, updates);
            }
          }}
          onDelete={() => {
            if (editingMarkerId) {
              onDeleteMarker(editingMarkerId);
              setEditingMarkerId(null);
            }
          }}
        />
      )}
    </div>
  );
};

export default MarkerTrack;
