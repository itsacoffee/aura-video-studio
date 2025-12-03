/**
 * MarkerList Component
 *
 * Sidebar panel that lists all markers with filtering, sorting,
 * and navigation capabilities. Supports type/color filtering,
 * time-based sorting, and quick navigation to markers.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Input,
  Checkbox,
  Badge,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  Divider,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Search24Regular,
  Delete24Regular,
  ArrowSort24Regular,
  Filter24Regular,
  Flag24Regular,
  BookmarkMultiple24Regular,
  CheckmarkCircle24Regular,
  MusicNote224Regular,
  ChevronDown16Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useState, useMemo, useCallback } from 'react';
import type { Marker, MarkerType } from '../../../types/opencut';
import { getMarkerColorHex } from './MarkerColorPicker';

export interface MarkerListProps {
  markers: Marker[];
  selectedMarkerId: string | null;
  visibleTypes: MarkerType[];
  onSelectMarker: (markerId: string) => void;
  onGoToMarker: (marker: Marker) => void;
  onToggleTodoComplete: (markerId: string) => void;
  onDeleteMarker: (markerId: string) => void;
  onToggleTypeVisibility: (type: MarkerType) => void;
  onDeleteAllMarkers: () => void;
  onDeleteMarkersByType: (type: MarkerType) => void;
}

type SortMode = 'time' | 'name' | 'type';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  searchInput: {
    flex: 1,
    minWidth: '100px',
  },
  filterChips: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  filterChip: {
    fontSize: tokens.fontSizeBase100,
  },
  list: {
    flex: 1,
    overflow: 'auto',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
  },
  markerItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    cursor: 'pointer',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    transition: 'background-color 100ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  markerItemSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  markerColor: {
    width: '8px',
    height: '8px',
    borderRadius: tokens.borderRadiusCircular,
    flexShrink: 0,
  },
  markerInfo: {
    flex: 1,
    minWidth: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  markerName: {
    fontWeight: tokens.fontWeightMedium,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  markerNameCompleted: {
    textDecoration: 'line-through',
    opacity: 0.6,
  },
  markerMeta: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  markerTime: {
    fontFamily: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, monospace',
  },
  markerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    opacity: 0,
    transition: 'opacity 100ms ease-out',
    '.markerItem:hover &': {
      opacity: 1,
    },
  },
  markerActionsVisible: {
    opacity: 1,
  },
  todoCheckbox: {
    marginRight: tokens.spacingHorizontalXS,
  },
});

const MARKER_TYPE_ICONS: Record<MarkerType, React.ReactNode> = {
  standard: <Flag24Regular />,
  chapter: <BookmarkMultiple24Regular />,
  todo: <CheckmarkCircle24Regular />,
  beat: <MusicNote224Regular />,
};

const MARKER_TYPE_LABELS: Record<MarkerType, string> = {
  standard: 'Standard',
  chapter: 'Chapter',
  todo: 'To-Do',
  beat: 'Beat',
};

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

export const MarkerList: FC<MarkerListProps> = ({
  markers,
  selectedMarkerId,
  visibleTypes,
  onSelectMarker,
  onGoToMarker,
  onToggleTodoComplete,
  onDeleteMarker,
  onToggleTypeVisibility,
  onDeleteAllMarkers,
  onDeleteMarkersByType,
}) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [sortMode, setSortMode] = useState<SortMode>('time');

  const filteredMarkers = useMemo(() => {
    let result = markers.filter((m) => visibleTypes.includes(m.type));

    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(
        (m) => m.name.toLowerCase().includes(query) || m.notes?.toLowerCase().includes(query)
      );
    }

    switch (sortMode) {
      case 'name':
        result.sort((a, b) => a.name.localeCompare(b.name));
        break;
      case 'type':
        result.sort((a, b) => a.type.localeCompare(b.type) || a.time - b.time);
        break;
      case 'time':
      default:
        result.sort((a, b) => a.time - b.time);
    }

    return result;
  }, [markers, visibleTypes, searchQuery, sortMode]);

  const handleMarkerClick = useCallback(
    (marker: Marker) => {
      onSelectMarker(marker.id);
      onGoToMarker(marker);
    },
    [onSelectMarker, onGoToMarker]
  );

  const handleTodoToggle = useCallback(
    (e: React.MouseEvent, markerId: string) => {
      e.stopPropagation();
      onToggleTodoComplete(markerId);
    },
    [onToggleTodoComplete]
  );

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.title}>
          <Text weight="semibold" size={400}>
            Markers
          </Text>
          <Badge appearance="filled" size="small" color="informative">
            {markers.length}
          </Badge>
        </div>
        <div className={styles.headerActions}>
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <Button appearance="subtle" icon={<Delete24Regular />} size="small" />
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={onDeleteAllMarkers}>Delete All Markers</MenuItem>
                <MenuDivider />
                <MenuItem onClick={() => onDeleteMarkersByType('standard')}>
                  Delete All Standard
                </MenuItem>
                <MenuItem onClick={() => onDeleteMarkersByType('chapter')}>
                  Delete All Chapters
                </MenuItem>
                <MenuItem onClick={() => onDeleteMarkersByType('todo')}>Delete All To-Do</MenuItem>
                <MenuItem onClick={() => onDeleteMarkersByType('beat')}>Delete All Beat</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
      </div>

      {/* Toolbar */}
      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          placeholder="Search markers..."
          contentBefore={<Search24Regular />}
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
          size="small"
        />
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Button
              appearance="subtle"
              icon={<ArrowSort24Regular />}
              size="small"
              title="Sort markers"
            >
              <ChevronDown16Regular />
            </Button>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              <MenuItem onClick={() => setSortMode('time')}>
                Sort by Time {sortMode === 'time' && '✓'}
              </MenuItem>
              <MenuItem onClick={() => setSortMode('name')}>
                Sort by Name {sortMode === 'name' && '✓'}
              </MenuItem>
              <MenuItem onClick={() => setSortMode('type')}>
                Sort by Type {sortMode === 'type' && '✓'}
              </MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Button
              appearance="subtle"
              icon={<Filter24Regular />}
              size="small"
              title="Filter markers"
            >
              <ChevronDown16Regular />
            </Button>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              {(['standard', 'chapter', 'todo', 'beat'] as MarkerType[]).map((type) => (
                <MenuItem key={type} onClick={() => onToggleTypeVisibility(type)}>
                  <Checkbox
                    checked={visibleTypes.includes(type)}
                    label={MARKER_TYPE_LABELS[type]}
                  />
                </MenuItem>
              ))}
            </MenuList>
          </MenuPopover>
        </Menu>
      </div>

      {/* Filter chips */}
      {visibleTypes.length < 4 && (
        <div className={styles.filterChips}>
          {visibleTypes.map((type) => (
            <Badge key={type} appearance="outline" size="small" className={styles.filterChip}>
              {MARKER_TYPE_LABELS[type]}
            </Badge>
          ))}
        </div>
      )}

      <Divider />

      {/* Marker list */}
      <div className={styles.list}>
        {filteredMarkers.length === 0 ? (
          <div className={styles.emptyState}>
            <Text size={300}>No markers found</Text>
            <Text size={200}>Press M to add a marker at the playhead</Text>
          </div>
        ) : (
          filteredMarkers.map((marker) => {
            const isSelected = marker.id === selectedMarkerId;
            return (
              <div
                key={marker.id}
                className={mergeClasses(styles.markerItem, isSelected && styles.markerItemSelected)}
                onClick={() => handleMarkerClick(marker)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    handleMarkerClick(marker);
                  }
                }}
                role="button"
                tabIndex={0}
              >
                {marker.type === 'todo' && (
                  <Checkbox
                    checked={marker.completed || false}
                    onClick={(e) => handleTodoToggle(e as unknown as React.MouseEvent, marker.id)}
                    className={styles.todoCheckbox}
                  />
                )}
                <div
                  className={styles.markerColor}
                  style={{ backgroundColor: getMarkerColorHex(marker.color) }}
                />
                <div className={styles.markerInfo}>
                  <span
                    className={mergeClasses(
                      styles.markerName,
                      marker.type === 'todo' && marker.completed && styles.markerNameCompleted
                    )}
                  >
                    {marker.name}
                  </span>
                  <div className={styles.markerMeta}>
                    <span className={styles.markerTime}>{formatTime(marker.time)}</span>
                    <span>{MARKER_TYPE_LABELS[marker.type]}</span>
                  </div>
                </div>
                <div
                  className={mergeClasses(
                    styles.markerActions,
                    isSelected && styles.markerActionsVisible
                  )}
                >
                  <Button
                    appearance="subtle"
                    icon={<Delete24Regular />}
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation();
                      onDeleteMarker(marker.id);
                    }}
                    title="Delete marker"
                    aria-label="Delete marker"
                  />
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
};

export default MarkerList;
