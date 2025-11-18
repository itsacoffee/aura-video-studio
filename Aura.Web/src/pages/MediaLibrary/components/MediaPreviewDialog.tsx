import {
  makeStyles,
  shorthands,
  tokens,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Text,
  Spinner,
  Tab,
  TabList,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  CloudArrowDown24Regular,
  Delete24Regular,
  Edit24Regular,
  Share24Regular,
} from '@fluentui/react-icons';
import React, { useState, useRef, useEffect } from 'react';
import type { MediaItemResponse, MediaMetadata } from '../../../types/mediaLibrary';
import { formatFileSize, formatDate, formatDuration } from '../../../utils/format';

const useStyles = makeStyles({
  surface: {
    maxWidth: '90vw',
    maxHeight: '90vh',
    width: '1200px',
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalL),
  },
  content: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalL),
    height: '600px',
  },
  previewPane: {
    flex: 2,
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
    minWidth: 0,
  },
  previewContainer: {
    flex: 1,
    backgroundColor: tokens.colorNeutralBackground3,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
    overflow: 'hidden',
  },
  media: {
    maxWidth: '100%',
    maxHeight: '100%',
    objectFit: 'contain',
  },
  controls: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    justifyContent: 'center',
  },
  metadataPane: {
    flex: 1,
    overflowY: 'auto',
    ...shorthands.padding(tokens.spacingVerticalM),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
  },
  metadataSection: {
    marginBottom: tokens.spacingVerticalL,
  },
  metadataTitle: {
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalS,
  },
  metadataGrid: {
    display: 'grid',
    gridTemplateColumns: '120px 1fr',
    ...shorthands.gap(tokens.spacingVerticalXS, tokens.spacingHorizontalS),
    fontSize: tokens.fontSizeBase200,
  },
  metadataLabel: {
    color: tokens.colorNeutralForeground3,
  },
  metadataValue: {
    fontWeight: tokens.fontWeightSemibold,
  },
  tags: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalXS),
    flexWrap: 'wrap',
  },
  tag: {
    ...shorthands.padding(tokens.spacingVerticalXXS, tokens.spacingHorizontalXS),
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground2,
    fontSize: tokens.fontSizeBase200,
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
  },
  audioPlayer: {
    width: '100%',
    ...shorthands.padding(tokens.spacingVerticalXL),
  },
  waveform: {
    width: '100%',
    height: '120px',
    backgroundColor: tokens.colorNeutralBackground4,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    marginBottom: tokens.spacingVerticalM,
  },
});

interface MediaPreviewDialogProps {
  media: MediaItemResponse;
  onClose: () => void;
  onDelete?: (id: string) => void;
  onEdit?: (media: MediaItemResponse) => void;
}

export const MediaPreviewDialog: React.FC<MediaPreviewDialogProps> = ({
  media,
  onClose,
  onDelete,
  onEdit,
}) => {
  const styles = useStyles();
  const videoRef = useRef<HTMLVideoElement>(null);
  const audioRef = useRef<HTMLAudioElement>(null);
  const [loading, setLoading] = useState(true);
  const [selectedTab, setSelectedTab] = useState<'preview' | 'metadata'>('preview');

  useEffect(() => {
    setLoading(false);
  }, [media]);

  const renderPreview = () => {
    if (loading) {
      return (
        <div className={styles.loading}>
          <Spinner label="Loading media..." />
        </div>
      );
    }

    switch (media.type) {
      case 'Video':
        return <video ref={videoRef} className={styles.media} src={media.url} controls autoPlay />;

      case 'Audio':
        return (
          <div className={styles.audioPlayer}>
            {media.metadata?.duration && (
              <div className={styles.waveform}>
                {/* Waveform visualization would go here */}
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    height: '100%',
                  }}
                >
                  <Text>🎵 Audio Waveform</Text>
                </div>
              </div>
            )}
            <audio
              ref={audioRef}
              className={styles.media}
              src={media.url}
              controls
              autoPlay
              style={{ width: '100%' }}
            />
          </div>
        );

      case 'Image':
        return (
          <img
            className={styles.media}
            src={media.url}
            alt={media.fileName}
            onLoad={() => setLoading(false)}
          />
        );

      default:
        return (
          <div className={styles.loading}>
            <Text size={400}>Preview not available for this media type</Text>
          </div>
        );
    }
  };

  const renderMetadata = () => {
    const metadata = media.metadata || ({} as MediaMetadata);

    return (
      <div className={styles.metadataPane}>
        <div className={styles.metadataSection}>
          <Text className={styles.metadataTitle}>File Information</Text>
          <div className={styles.metadataGrid}>
            <Text className={styles.metadataLabel}>File Name:</Text>
            <Text className={styles.metadataValue}>{media.fileName}</Text>

            <Text className={styles.metadataLabel}>Type:</Text>
            <Text className={styles.metadataValue}>{media.type}</Text>

            <Text className={styles.metadataLabel}>Size:</Text>
            <Text className={styles.metadataValue}>{formatFileSize(media.fileSize)}</Text>

            <Text className={styles.metadataLabel}>Source:</Text>
            <Text className={styles.metadataValue}>{media.source}</Text>

            <Text className={styles.metadataLabel}>Created:</Text>
            <Text className={styles.metadataValue}>{formatDate(media.createdAt)}</Text>

            <Text className={styles.metadataLabel}>Modified:</Text>
            <Text className={styles.metadataValue}>{formatDate(media.updatedAt)}</Text>

            <Text className={styles.metadataLabel}>Usage Count:</Text>
            <Text className={styles.metadataValue}>{media.usageCount}</Text>
          </div>
        </div>

        {(metadata.width || metadata.height || metadata.duration) && (
          <div className={styles.metadataSection}>
            <Text className={styles.metadataTitle}>Media Properties</Text>
            <div className={styles.metadataGrid}>
              {metadata.width && (
                <>
                  <Text className={styles.metadataLabel}>Resolution:</Text>
                  <Text className={styles.metadataValue}>
                    {metadata.width} × {metadata.height}
                  </Text>
                </>
              )}

              {metadata.duration && (
                <>
                  <Text className={styles.metadataLabel}>Duration:</Text>
                  <Text className={styles.metadataValue}>{formatDuration(metadata.duration)}</Text>
                </>
              )}

              {metadata.framerate && (
                <>
                  <Text className={styles.metadataLabel}>Frame Rate:</Text>
                  <Text className={styles.metadataValue}>{metadata.framerate} fps</Text>
                </>
              )}

              {metadata.format && (
                <>
                  <Text className={styles.metadataLabel}>Format:</Text>
                  <Text className={styles.metadataValue}>{metadata.format}</Text>
                </>
              )}

              {metadata.codec && (
                <>
                  <Text className={styles.metadataLabel}>Codec:</Text>
                  <Text className={styles.metadataValue}>{metadata.codec}</Text>
                </>
              )}

              {metadata.bitrate && (
                <>
                  <Text className={styles.metadataLabel}>Bitrate:</Text>
                  <Text className={styles.metadataValue}>
                    {(metadata.bitrate / 1000).toFixed(0)} kbps
                  </Text>
                </>
              )}

              {metadata.channels && (
                <>
                  <Text className={styles.metadataLabel}>Channels:</Text>
                  <Text className={styles.metadataValue}>{metadata.channels}</Text>
                </>
              )}

              {metadata.sampleRate && (
                <>
                  <Text className={styles.metadataLabel}>Sample Rate:</Text>
                  <Text className={styles.metadataValue}>{metadata.sampleRate} Hz</Text>
                </>
              )}
            </div>
          </div>
        )}

        {media.description && (
          <div className={styles.metadataSection}>
            <Text className={styles.metadataTitle}>Description</Text>
            <Text>{media.description}</Text>
          </div>
        )}

        {media.tags && media.tags.length > 0 && (
          <div className={styles.metadataSection}>
            <Text className={styles.metadataTitle}>Tags</Text>
            <div className={styles.tags}>
              {media.tags.map((tag) => (
                <span key={tag} className={styles.tag}>
                  {tag}
                </span>
              ))}
            </div>
          </div>
        )}

        {media.collectionName && (
          <div className={styles.metadataSection}>
            <Text className={styles.metadataTitle}>Collection</Text>
            <Text>{media.collectionName}</Text>
          </div>
        )}
      </div>
    );
  };

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogSurface className={styles.surface}>
        <DialogBody className={styles.body}>
          <DialogTitle
            action={<Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />}
          >
            {media.fileName}
          </DialogTitle>
          <DialogContent>
            <TabList
              selectedValue={selectedTab}
              onTabSelect={(_, data) => setSelectedTab(data.value as 'preview' | 'metadata')}
            >
              <Tab value="preview">Preview</Tab>
              <Tab value="metadata">Metadata</Tab>
            </TabList>

            <div className={styles.content}>
              {selectedTab === 'preview' && (
                <div className={styles.previewPane}>
                  <div className={styles.previewContainer}>{renderPreview()}</div>
                  <div className={styles.controls}>
                    <Button
                      appearance="subtle"
                      icon={<CloudArrowDown24Regular />}
                      onClick={() => window.open(media.url, '_blank')}
                    >
                      Download
                    </Button>
                    {onEdit && (
                      <Button
                        appearance="subtle"
                        icon={<Edit24Regular />}
                        onClick={() => onEdit(media)}
                      >
                        Edit
                      </Button>
                    )}
                    <Button appearance="subtle" icon={<Share24Regular />}>
                      Share
                    </Button>
                    {onDelete && (
                      <Button
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={() => {
                          onDelete(media.id);
                          onClose();
                        }}
                      >
                        Delete
                      </Button>
                    )}
                  </div>
                </div>
              )}

              {selectedTab === 'metadata' && renderMetadata()}
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
