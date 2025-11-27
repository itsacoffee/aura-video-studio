/**
 * MarkerList - Component for displaying and managing video markers
 *
 * Displays a list of markers added to the video timeline.
 * Supports clicking to seek to marker time and deleting markers.
 */

import { Button, Text, makeStyles, tokens } from '@fluentui/react-components';
import { DeleteRegular, LocationRegular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    paddingBottom: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  markerItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalXS,
    paddingLeft: tokens.spacingHorizontalS,
    paddingRight: tokens.spacingHorizontalXS,
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground1,
    transition: 'background-color 0.15s ease',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  markerInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
    overflow: 'hidden',
  },
  markerLabel: {
    flex: 1,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  markerTime: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    flexShrink: 0,
  },
  emptyState: {
    color: tokens.colorNeutralForeground3,
    fontStyle: 'italic',
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
  },
});

export interface Marker {
  id: string;
  time: number;
  label: string;
}

interface MarkerListProps {
  markers: Marker[];
  onMarkerClick: (marker: Marker) => void;
  onMarkerDelete: (markerId: string) => void;
}

/**
 * Format time in seconds to MM:SS format
 */
function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}

export function MarkerList({ markers, onMarkerClick, onMarkerDelete }: MarkerListProps) {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <LocationRegular />
        <Text size={400} weight="semibold">
          Markers
        </Text>
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
          ({markers.length})
        </Text>
      </div>
      <div className={styles.list}>
        {markers.length === 0 ? (
          <Text className={styles.emptyState}>
            No markers. Press M or use context menu to add markers.
          </Text>
        ) : (
          markers.map((marker) => (
            <div
              key={marker.id}
              className={styles.markerItem}
              onClick={() => onMarkerClick(marker)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  onMarkerClick(marker);
                }
              }}
            >
              <div className={styles.markerInfo}>
                <LocationRegular />
                <Text className={styles.markerLabel}>{marker.label}</Text>
                <Text className={styles.markerTime}>{formatTime(marker.time)}</Text>
              </div>
              <Button
                icon={<DeleteRegular />}
                size="small"
                appearance="subtle"
                onClick={(e) => {
                  e.stopPropagation();
                  onMarkerDelete(marker.id);
                }}
                title="Delete marker"
              />
            </div>
          ))
        )}
      </div>
    </div>
  );
}
