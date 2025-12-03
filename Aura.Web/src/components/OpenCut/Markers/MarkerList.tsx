/**
 * MarkerList Component
 *
 * A sidebar panel listing all markers with filtering, sorting,
 * navigation, and bulk operations.
 */

import {
  makeStyles,
  tokens,
  mergeClasses,
  Text,
  Button,
  Input,
  Checkbox,
  Dropdown,
  Option,
  Badge,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Tooltip,
} from '@fluentui/react-components';
import {
  Search24Regular,
  Delete24Regular,
  Flag24Filled,
  BookmarkMultiple24Filled,
  CheckmarkCircle24Filled,
  MusicNote124Filled,
  MoreVertical24Regular,
  ArrowDownload24Regular,
} from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import type { Marker, MarkerType, MarkerColor } from '../../../types/opencut';

export interface MarkerListProps {
  markers: Marker[];
  selectedMarkerId: string | null;
  visibleTypes: MarkerType[];
  filterColor: MarkerColor | null;
  onSelectMarker: (markerId: string | null) => void;
  onGoToMarker: (markerId: string) => void;
  onToggleTodoComplete: (markerId: string) => void;
  onDeleteMarker: (markerId: string) => void;
  onDeleteAll: () => void;
  onDeleteByType: (type: MarkerType) => void;
  onToggleTypeVisibility: (type: MarkerType) => void;
  onSetFilterColor: (color: MarkerColor | null) => void;
  onExportChapters: () => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    padding: openCutTokens.spacing.sm,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xs,
  },
  headerRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  searchRow: {
    display: 'flex',
    gap: openCutTokens.spacing.xs,
  },
  filters: {
    display: 'flex',
    gap: openCutTokens.spacing.xs,
    flexWrap: 'wrap',
    padding: `${openCutTokens.spacing.xs} ${openCutTokens.spacing.sm}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  filterChip: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    padding: '2px 8px',
    borderRadius: openCutTokens.radius.sm,
    fontSize: openCutTokens.typography.fontSize.xs,
    cursor: 'pointer',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: 'transparent',
    transition: 'background-color 0.1s, opacity 0.1s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  filterChipActive: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  filterChipDisabled: {
    opacity: 0.5,
  },
  list: {
    flex: 1,
    overflow: 'auto',
    padding: openCutTokens.spacing.xs,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    padding: openCutTokens.spacing.lg,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
  markerItem: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    padding: openCutTokens.spacing.xs,
    borderRadius: openCutTokens.radius.sm,
    cursor: 'pointer',
    transition: 'background-color 0.1s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  markerItemSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  markerIcon: {
    width: '20px',
    height: '20px',
    borderRadius: '4px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    '& svg': {
      fontSize: '12px',
      color: 'white',
    },
  },
  markerInfo: {
    flex: 1,
    minWidth: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  markerName: {
    fontSize: openCutTokens.typography.fontSize.sm,
    fontWeight: openCutTokens.typography.fontWeight.medium,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  markerTime: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: tokens.colorNeutralForeground3,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  markerCompleted: {
    textDecoration: 'line-through',
    opacity: 0.6,
  },
  checkbox: {
    flexShrink: 0,
  },
  deleteButton: {
    opacity: 0,
    transition: 'opacity 0.1s',
  },
  markerItemHover: {
    '& $deleteButton': {
      opacity: 1,
    },
  },
});

const MARKER_ICON: Record<MarkerType, React.ReactNode> = {
  standard: <Flag24Filled />,
  chapter: <BookmarkMultiple24Filled />,
  todo: <CheckmarkCircle24Filled />,
  beat: <MusicNote124Filled />,
};

const COLOR_MAP: Record<string, string> = {
  red: '#EF4444',
  orange: '#F97316',
  yellow: '#EAB308',
  green: '#22C55E',
  blue: '#3B82F6',
  purple: '#A855F7',
  pink: '#EC4899',
};

const TYPE_LABELS: Record<MarkerType, string> = {
  standard: 'Standard',
  chapter: 'Chapter',
  todo: 'To-do',
  beat: 'Beat',
};

function formatTimecode(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

export const MarkerList: FC<MarkerListProps> = ({
  markers,
  selectedMarkerId,
  visibleTypes,
  filterColor,
  onSelectMarker,
  onGoToMarker,
  onToggleTodoComplete,
  onDeleteMarker,
  onDeleteAll,
  onDeleteByType,
  onToggleTypeVisibility,
  onSetFilterColor,
  onExportChapters,
}) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<'time' | 'name'>('time');

  const filteredAndSortedMarkers = useMemo(() => {
    let result = markers.filter((m) => {
      if (!visibleTypes.includes(m.type)) return false;
      if (filterColor && m.color !== filterColor) return false;
      if (searchQuery && !m.name.toLowerCase().includes(searchQuery.toLowerCase())) return false;
      return true;
    });

    if (sortBy === 'name') {
      result = [...result].sort((a, b) => a.name.localeCompare(b.name));
    } else {
      result = [...result].sort((a, b) => a.time - b.time);
    }

    return result;
  }, [markers, visibleTypes, filterColor, searchQuery, sortBy]);

  const handleMarkerClick = useCallback(
    (markerId: string) => {
      onSelectMarker(markerId);
      onGoToMarker(markerId);
    },
    [onSelectMarker, onGoToMarker]
  );

  const typeCounts = useMemo(() => {
    const counts: Record<MarkerType, number> = {
      standard: 0,
      chapter: 0,
      todo: 0,
      beat: 0,
    };
    markers.forEach((m) => {
      counts[m.type]++;
    });
    return counts;
  }, [markers]);

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerRow}>
          <Text weight="semibold" size={400}>
            Markers
          </Text>
          <div style={{ display: 'flex', gap: '4px' }}>
            <Badge appearance="outline" size="small">
              {markers.length}
            </Badge>
            <Menu>
              <MenuTrigger disableButtonEnhancement>
                <Button appearance="subtle" icon={<MoreVertical24Regular />} size="small" />
              </MenuTrigger>
              <MenuPopover>
                <MenuList>
                  <MenuItem icon={<ArrowDownload24Regular />} onClick={onExportChapters}>
                    Export Chapters
                  </MenuItem>
                  <MenuItem onClick={() => onDeleteByType('standard')}>
                    Delete Standard Markers
                  </MenuItem>
                  <MenuItem onClick={() => onDeleteByType('todo')}>Delete Todo Markers</MenuItem>
                  <MenuItem icon={<Delete24Regular />} onClick={onDeleteAll}>
                    Delete All Markers
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>
          </div>
        </div>
        <div className={styles.searchRow}>
          <Input
            placeholder="Search markers..."
            value={searchQuery}
            onChange={(_, data) => setSearchQuery(data.value)}
            contentBefore={<Search24Regular />}
            size="small"
            style={{ flex: 1 }}
          />
          <Dropdown
            value={sortBy === 'time' ? 'Time' : 'Name'}
            selectedOptions={[sortBy]}
            onOptionSelect={(_, data) => setSortBy(data.optionValue as 'time' | 'name')}
            size="small"
            style={{ minWidth: '80px' }}
          >
            <Option value="time">Time</Option>
            <Option value="name">Name</Option>
          </Dropdown>
        </div>
      </div>

      {/* Type Filters */}
      <div className={styles.filters}>
        {(['standard', 'chapter', 'todo', 'beat'] as MarkerType[]).map((type) => (
          <button
            key={type}
            type="button"
            className={mergeClasses(
              styles.filterChip,
              visibleTypes.includes(type) && styles.filterChipActive,
              typeCounts[type] === 0 && styles.filterChipDisabled
            )}
            onClick={() => onToggleTypeVisibility(type)}
            aria-pressed={visibleTypes.includes(type)}
          >
            {MARKER_ICON[type]}
            <span>{TYPE_LABELS[type]}</span>
            <Badge appearance="outline" size="small">
              {typeCounts[type]}
            </Badge>
          </button>
        ))}
        {/* Color filter dropdown */}
        <Dropdown
          value={
            filterColor ? filterColor.charAt(0).toUpperCase() + filterColor.slice(1) : 'All Colors'
          }
          selectedOptions={filterColor ? [filterColor] : []}
          onOptionSelect={(_, data) =>
            onSetFilterColor(data.optionValue === 'all' ? null : (data.optionValue as MarkerColor))
          }
          size="small"
          style={{ minWidth: '100px' }}
        >
          <Option value="all">All Colors</Option>
          <Option value="red">Red</Option>
          <Option value="orange">Orange</Option>
          <Option value="yellow">Yellow</Option>
          <Option value="green">Green</Option>
          <Option value="blue">Blue</Option>
          <Option value="purple">Purple</Option>
          <Option value="pink">Pink</Option>
        </Dropdown>
      </div>

      {/* Marker List */}
      <div className={styles.list}>
        {filteredAndSortedMarkers.length === 0 ? (
          <div className={styles.emptyState}>
            <Flag24Filled style={{ fontSize: '48px', opacity: 0.3, marginBottom: '8px' }} />
            <Text size={300}>No markers</Text>
            <Text size={200}>Press M to add a marker at the playhead</Text>
          </div>
        ) : (
          <AnimatePresence>
            {filteredAndSortedMarkers.map((marker) => (
              <motion.div
                key={marker.id}
                className={mergeClasses(
                  styles.markerItem,
                  selectedMarkerId === marker.id && styles.markerItemSelected
                )}
                onClick={() => handleMarkerClick(marker.id)}
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.15 }}
                role="button"
                tabIndex={0}
                aria-pressed={selectedMarkerId === marker.id}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    handleMarkerClick(marker.id);
                  }
                }}
              >
                {/* Todo checkbox */}
                {marker.type === 'todo' && (
                  <Checkbox
                    className={styles.checkbox}
                    checked={marker.completed}
                    onChange={(e) => {
                      e.stopPropagation();
                      onToggleTodoComplete(marker.id);
                    }}
                    size="medium"
                  />
                )}

                {/* Marker icon */}
                <div
                  className={styles.markerIcon}
                  style={{ backgroundColor: COLOR_MAP[marker.color] }}
                >
                  {MARKER_ICON[marker.type]}
                </div>

                {/* Marker info */}
                <div className={styles.markerInfo}>
                  <span
                    className={mergeClasses(
                      styles.markerName,
                      marker.type === 'todo' && marker.completed && styles.markerCompleted
                    )}
                  >
                    {marker.name}
                  </span>
                  <span className={styles.markerTime}>{formatTimecode(marker.time)}</span>
                </div>

                {/* Delete button */}
                <Tooltip content="Delete marker" relationship="label">
                  <Button
                    appearance="subtle"
                    icon={<Delete24Regular />}
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation();
                      onDeleteMarker(marker.id);
                    }}
                  />
                </Tooltip>
              </motion.div>
            ))}
          </AnimatePresence>
        )}
      </div>
    </div>
  );
};

export default MarkerList;
