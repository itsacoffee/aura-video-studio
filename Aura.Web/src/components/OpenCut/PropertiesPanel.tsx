/**
 * PropertiesPanel Component
 *
 * Properties panel for editing selected elements following Apple HIG.
 * Features refined empty states and proper spacing.
 */

import { makeStyles, tokens, Text, mergeClasses } from '@fluentui/react-components';
import { Settings24Regular, TextT24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { useOpenCutMediaStore } from '../../stores/opencutMedia';
import { EmptyState } from './EmptyState';

export interface PropertiesPanelProps {
  className?: string;
}

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
    padding: `${tokens.spacingVerticalL} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '56px',
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalL,
    display: 'flex',
    flexDirection: 'column',
  },
  propertyGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
  },
  propertyGroupTitle: {
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalXS,
  },
  propertyRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
  },
  propertyLabel: {
    color: tokens.colorNeutralForeground3,
    minWidth: '80px',
  },
  propertyValue: {
    flex: 1,
    textAlign: 'right',
    color: tokens.colorNeutralForeground1,
  },
  selectedMediaInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  mediaThumbnail: {
    width: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    objectFit: 'cover',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  mediaName: {
    wordBreak: 'break-word',
  },
});

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export const PropertiesPanel: FC<PropertiesPanelProps> = ({ className }) => {
  const styles = useStyles();
  const mediaStore = useOpenCutMediaStore();
  const selectedMedia = mediaStore.selectedMediaId
    ? mediaStore.getMediaById(mediaStore.selectedMediaId)
    : null;

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Settings24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Properties
          </Text>
        </div>
      </div>

      <div className={styles.content}>
        {!selectedMedia ? (
          <EmptyState
            icon={<TextT24Regular />}
            title="No selection"
            description="Select an element on the timeline or in the media library to view its properties"
            size="medium"
          />
        ) : (
          <div className={styles.selectedMediaInfo}>
            {/* Media Preview */}
            <div className={styles.mediaThumbnail}>
              {selectedMedia.thumbnailUrl ? (
                <img
                  src={selectedMedia.thumbnailUrl}
                  alt={selectedMedia.name}
                  style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                />
              ) : (
                <TextT24Regular
                  style={{ fontSize: '32px', color: tokens.colorNeutralForeground3 }}
                />
              )}
            </div>

            {/* File Info */}
            <div className={styles.propertyGroup}>
              <Text weight="semibold" size={300} className={styles.propertyGroupTitle}>
                File Information
              </Text>
              <div className={styles.propertyRow}>
                <Text size={200} className={styles.propertyLabel}>
                  Name
                </Text>
                <Text size={200} className={mergeClasses(styles.propertyValue, styles.mediaName)}>
                  {selectedMedia.name}
                </Text>
              </div>
              <div className={styles.propertyRow}>
                <Text size={200} className={styles.propertyLabel}>
                  Type
                </Text>
                <Text size={200} className={styles.propertyValue}>
                  {selectedMedia.type.charAt(0).toUpperCase() + selectedMedia.type.slice(1)}
                </Text>
              </div>
              {selectedMedia.file && (
                <div className={styles.propertyRow}>
                  <Text size={200} className={styles.propertyLabel}>
                    Size
                  </Text>
                  <Text size={200} className={styles.propertyValue}>
                    {formatBytes(selectedMedia.file.size)}
                  </Text>
                </div>
              )}
            </div>

            {/* Media Details */}
            {(selectedMedia.duration !== undefined ||
              selectedMedia.width !== undefined ||
              selectedMedia.fps !== undefined) && (
              <div className={styles.propertyGroup}>
                <Text weight="semibold" size={300} className={styles.propertyGroupTitle}>
                  Media Details
                </Text>
                {selectedMedia.duration !== undefined && (
                  <div className={styles.propertyRow}>
                    <Text size={200} className={styles.propertyLabel}>
                      Duration
                    </Text>
                    <Text size={200} className={styles.propertyValue}>
                      {formatDuration(selectedMedia.duration)}
                    </Text>
                  </div>
                )}
                {selectedMedia.width !== undefined && selectedMedia.height !== undefined && (
                  <div className={styles.propertyRow}>
                    <Text size={200} className={styles.propertyLabel}>
                      Resolution
                    </Text>
                    <Text size={200} className={styles.propertyValue}>
                      {selectedMedia.width} × {selectedMedia.height}
                    </Text>
                  </div>
                )}
                {selectedMedia.fps !== undefined && (
                  <div className={styles.propertyRow}>
                    <Text size={200} className={styles.propertyLabel}>
                      Frame Rate
                    </Text>
                    <Text size={200} className={styles.propertyValue}>
                      {selectedMedia.fps} fps
                    </Text>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default PropertiesPanel;
