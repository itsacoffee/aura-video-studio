/**
 * MediaPanel Component
 *
 * Media library panel with improved layout and spacing following Apple HIG.
 * Features drag-and-drop import, refined grid layout, and elegant empty states.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Video24Regular,
  MusicNote224Regular,
  Image24Regular,
  Folder24Regular,
} from '@fluentui/react-icons';
import { useRef, useState, useCallback } from 'react';
import type { FC, DragEvent } from 'react';
import { useOpenCutMediaStore } from '../../stores/opencutMedia';
import { EmptyState } from './EmptyState';

export interface MediaPanelProps {
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
    gap: tokens.spacingVerticalL,
  },
  mediaGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  mediaItem: {
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
    border: `2px solid transparent`,
    transition: 'all 200ms ease-out',
    overflow: 'hidden',
    position: 'relative',
    ':hover': {
      border: `2px solid ${tokens.colorBrandStroke1}`,
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'scale(1.02)',
      boxShadow: tokens.shadow4,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '2px',
    },
  },
  mediaItemSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    boxShadow: tokens.shadow8,
  },
  mediaItemImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  mediaItemIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '24px',
  },
  mediaItemDuration: {
    position: 'absolute',
    bottom: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
    backgroundColor: 'rgba(0, 0, 0, 0.75)',
    color: 'white',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  dropZone: {
    border: `2px dashed ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusLarge,
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
    transition: 'all 200ms ease-out',
    backgroundColor: tokens.colorNeutralBackground3,
    minHeight: '100px',
  },
  dropZoneActive: {
    border: `2px dashed ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    transform: 'scale(1.01)',
  },
  dropZoneIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '32px',
  },
  importButton: {
    minWidth: '44px',
    minHeight: '36px',
  },
});

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export const MediaPanel: FC<MediaPanelProps> = ({ className }) => {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const mediaStore = useOpenCutMediaStore();

  const handleFileSelect = useCallback(
    async (files: FileList | null) => {
      if (!files) return;
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        await mediaStore.addMediaFile(file);
      }
    },
    [mediaStore]
  );

  const handleDrop = useCallback(
    async (e: DragEvent) => {
      e.preventDefault();
      setIsDragging(false);
      if (e.dataTransfer.files.length > 0) {
        await handleFileSelect(e.dataTransfer.files);
      }
    },
    [handleFileSelect]
  );

  const handleDragOver = useCallback((e: DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleImportClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Folder24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Media
          </Text>
        </div>
        <Button
          appearance="subtle"
          icon={<Add24Regular />}
          size="small"
          className={styles.importButton}
          onClick={handleImportClick}
        >
          Import
        </Button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept="video/*,audio/*,image/*"
          style={{ display: 'none' }}
          onChange={(e) => handleFileSelect(e.target.files)}
        />
      </div>

      <div
        className={styles.content}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        role="region"
        aria-label="Media files drop zone"
      >
        {mediaStore.mediaFiles.length === 0 ? (
          <EmptyState
            icon={<Video24Regular />}
            title="No media files"
            description="Import videos, audio, or images to get started"
            action={{
              label: 'Import Media',
              onClick: handleImportClick,
              icon: <Add24Regular />,
            }}
            size="medium"
          />
        ) : (
          <div className={styles.mediaGrid}>
            {mediaStore.mediaFiles.map((file) => (
              <Tooltip key={file.id} content={file.name} relationship="label">
                <div
                  className={mergeClasses(
                    styles.mediaItem,
                    mediaStore.selectedMediaId === file.id && styles.mediaItemSelected
                  )}
                  onClick={() => mediaStore.selectMedia(file.id)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      mediaStore.selectMedia(file.id);
                    }
                  }}
                  aria-label={file.name}
                  aria-pressed={mediaStore.selectedMediaId === file.id}
                >
                  {file.thumbnailUrl ? (
                    <img
                      src={file.thumbnailUrl}
                      alt={file.name}
                      className={styles.mediaItemImage}
                    />
                  ) : file.type === 'video' ? (
                    <Video24Regular className={styles.mediaItemIcon} />
                  ) : file.type === 'audio' ? (
                    <MusicNote224Regular className={styles.mediaItemIcon} />
                  ) : (
                    <Image24Regular className={styles.mediaItemIcon} />
                  )}
                  {file.duration !== undefined && (
                    <span className={styles.mediaItemDuration}>
                      {formatDuration(file.duration)}
                    </span>
                  )}
                </div>
              </Tooltip>
            ))}
          </div>
        )}

        <div className={mergeClasses(styles.dropZone, isDragging && styles.dropZoneActive)}>
          <Add24Regular className={styles.dropZoneIcon} />
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Drop media files here
          </Text>
        </div>
      </div>
    </div>
  );
};

export default MediaPanel;
