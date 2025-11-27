/**
 * TrackHeader - Component for timeline track headers with context menu support
 *
 * Displays track information (name, type, controls) and provides right-click
 * context menu functionality for track operations.
 */

import { makeStyles, Button, Tooltip, Text, tokens } from '@fluentui/react-components';
import {
  Eye24Regular,
  EyeOff24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
} from '@fluentui/react-icons';
import React, { useCallback } from 'react';
import { useContextMenu, useContextMenuAction } from '../../hooks/useContextMenu';
import type { TimelineTrackMenuData } from '../../types/electron-context-menu';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  trackLabel: {
    width: '140px',
    minWidth: '140px',
    padding: 'var(--editor-space-md)',
    borderRight: `1px solid var(--editor-panel-border)`,
    backgroundColor: 'var(--editor-panel-header-bg)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    fontSize: 'var(--editor-font-size-base)',
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--editor-space-sm)',
    position: 'sticky',
    left: 0,
    zIndex: 'var(--editor-z-panel)',
  },
  trackLabelText: {
    fontSize: 'var(--editor-font-size-base)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-primary)',
    whiteSpace: 'nowrap',
    overflow: 'visible',
    textOverflow: 'clip',
  },
  trackControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXXS,
  },
});

export interface TrackData {
  id: string;
  name?: string;
  label?: string;
  type: 'video' | 'audio' | 'overlay';
  locked?: boolean;
  muted?: boolean;
  solo?: boolean;
  visible?: boolean;
  index?: number;
}

interface TrackHeaderProps {
  track: TrackData;
  totalTracks: number;
  onUpdate: (track: TrackData) => void;
  onToggleVisibility?: (trackId: string) => void;
  onToggleLock?: (trackId: string) => void;
  onAddTrack?: (position: 'above' | 'below', trackType: 'video' | 'audio') => void;
  onDeleteTrack?: (trackId: string) => void;
  onRenameTrack?: (trackId: string, newName: string) => void;
}

export function TrackHeader({
  track,
  totalTracks,
  onUpdate,
  onToggleVisibility,
  onToggleLock,
  onAddTrack,
  onDeleteTrack,
  onRenameTrack,
}: TrackHeaderProps) {
  const styles = useStyles();

  // Context menu integration
  const showContextMenu = useContextMenu<TimelineTrackMenuData>('timeline-track');

  const handleContextMenu = useCallback(
    (e: React.MouseEvent) => {
      const menuData: TimelineTrackMenuData = {
        trackId: track.id,
        trackType: track.type === 'overlay' ? 'overlay' : track.type,
        isLocked: track.locked || false,
        isMuted: track.muted || false,
        isSolo: track.solo || false,
        trackIndex: track.index || 0,
        totalTracks,
      };
      showContextMenu(e, menuData);
    },
    [track, totalTracks, showContextMenu]
  );

  // Context menu action handlers
  useContextMenuAction(
    'timeline-track',
    'onAddTrack',
    useCallback(
      (
        data: TimelineTrackMenuData & {
          position?: 'above' | 'below';
          trackType?: 'video' | 'audio';
        }
      ) => {
        console.info('Add track:', data);
        onAddTrack?.(data.position || 'below', data.trackType || 'video');
      },
      [onAddTrack]
    )
  );

  useContextMenuAction(
    'timeline-track',
    'onToggleLock',
    useCallback(
      (data: TimelineTrackMenuData) => {
        onUpdate({ ...track, locked: !track.locked });
        onToggleLock?.(data.trackId);
      },
      [track, onUpdate, onToggleLock]
    )
  );

  useContextMenuAction(
    'timeline-track',
    'onToggleMute',
    useCallback(
      (_data: TimelineTrackMenuData) => {
        onUpdate({ ...track, muted: !track.muted });
      },
      [track, onUpdate]
    )
  );

  useContextMenuAction(
    'timeline-track',
    'onToggleSolo',
    useCallback(
      (_data: TimelineTrackMenuData) => {
        onUpdate({ ...track, solo: !track.solo });
      },
      [track, onUpdate]
    )
  );

  useContextMenuAction(
    'timeline-track',
    'onRename',
    useCallback(
      (data: TimelineTrackMenuData) => {
        const currentName = track.name || track.label || '';
        const newName = window.prompt('Enter new track name:', currentName);
        if (newName) {
          onUpdate({ ...track, name: newName, label: newName });
          onRenameTrack?.(data.trackId, newName);
        }
      },
      [track, onUpdate, onRenameTrack]
    )
  );

  useContextMenuAction(
    'timeline-track',
    'onDelete',
    useCallback(
      (data: TimelineTrackMenuData) => {
        const trackName = track.name || track.label || 'this track';
        if (window.confirm(`Delete track "${trackName}"?`)) {
          onDeleteTrack?.(data.trackId);
        }
      },
      [track, onDeleteTrack]
    )
  );

  const trackLabel = track.name || track.label || '';
  const isVisible = track.visible !== false;
  const isLocked = track.locked || false;

  return (
    <div
      className={styles.trackLabel}
      onContextMenu={handleContextMenu}
      role="group"
      aria-label={`Track: ${trackLabel}`}
    >
      <Text className={styles.trackLabelText}>{trackLabel}</Text>
      <div className={styles.trackControls}>
        <Tooltip content={isVisible ? 'Hide track' : 'Show track'} relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={isVisible ? <Eye24Regular /> : <EyeOff24Regular />}
            onClick={(e) => {
              e.stopPropagation();
              onToggleVisibility?.(track.id);
            }}
            aria-label={isVisible ? 'Hide track' : 'Show track'}
            style={{ minWidth: '20px', minHeight: '20px', padding: '2px' }}
          />
        </Tooltip>
        <Tooltip content={isLocked ? 'Unlock track' : 'Lock track'} relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={isLocked ? <LockClosed24Regular /> : <LockOpen24Regular />}
            onClick={(e) => {
              e.stopPropagation();
              onToggleLock?.(track.id);
            }}
            aria-label={isLocked ? 'Unlock track' : 'Lock track'}
            style={{ minWidth: '20px', minHeight: '20px', padding: '2px' }}
          />
        </Tooltip>
      </div>
    </div>
  );
}
