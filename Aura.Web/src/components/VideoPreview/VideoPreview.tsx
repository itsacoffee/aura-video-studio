import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Badge,
  Divider,
} from '@fluentui/react-components';
import {
  Folder24Regular,
  Share24Regular,
  PlayCircle24Regular,
  Info24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  videoCard: {
    padding: 0,
    overflow: 'hidden',
  },
  videoContainer: {
    position: 'relative',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground6,
    aspectRatio: '16 / 9',
  },
  video: {
    width: '100%',
    height: '100%',
    objectFit: 'contain',
  },
  videoControls: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  metadataCard: {
    padding: tokens.spacingVerticalL,
  },
  metadataGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  metadataItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  metadataLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  metadataValue: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
});

interface VideoMetadata {
  duration?: string;
  resolution?: string;
  fps?: number;
  codec?: string;
  bitrate?: string;
  size?: string;
  format?: string;
}

interface VideoPreviewProps {
  videoPath: string;
  metadata?: VideoMetadata;
  jobId?: string;
  correlationId?: string;
  onOpenFolder?: () => void;
  onShare?: () => void;
}

export function VideoPreview({
  videoPath,
  metadata,
  jobId,
  correlationId,
  onOpenFolder,
  onShare,
}: VideoPreviewProps) {
  const styles = useStyles();



  const handleOpenFolder = () => {
    if (onOpenFolder) {
      onOpenFolder();
    } else {
      // Fallback to opening via file protocol
      const folderPath = videoPath.substring(0, videoPath.lastIndexOf('/'));
      window.open(`file://${folderPath}`, '_blank');
    }
  };

  const handleShare = () => {
    if (onShare) {
      onShare();
    } else {
      // Default share behavior - copy path to clipboard
      navigator.clipboard.writeText(videoPath);
    }
  };

  return (
    <div className={styles.container}>
      <Card className={styles.videoCard}>
        <div className={styles.videoContainer}>
          <video
            className={styles.video}
            src={videoPath}
            controls
            preload="metadata"
          >
            Your browser does not support the video tag.
          </video>
        </div>
        <div className={styles.videoControls}>
          {onOpenFolder && (
            <Button
              appearance="primary"
              icon={<Folder24Regular />}
              onClick={handleOpenFolder}
            >
              Open Output Folder
            </Button>
          )}
          {onShare && (
            <Button
              appearance="subtle"
              icon={<Share24Regular />}
              onClick={handleShare}
            >
              Share
            </Button>
          )}
          <Button
            appearance="subtle"
            icon={<PlayCircle24Regular />}
            as="a"
            href={videoPath}
            target="_blank"
          >
            Open in Player
          </Button>
        </div>
      </Card>

      {metadata && (
        <Card className={styles.metadataCard}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <Info24Regular />
            <Text weight="semibold" size={400}>
              Video Metadata
            </Text>
          </div>

          <div className={styles.metadataGrid}>
            {metadata.resolution && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>Resolution</Text>
                <Text className={styles.metadataValue}>{metadata.resolution}</Text>
              </div>
            )}

            {metadata.duration && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>Duration</Text>
                <Text className={styles.metadataValue}>{metadata.duration}</Text>
              </div>
            )}

            {metadata.fps && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>Frame Rate</Text>
                <Text className={styles.metadataValue}>{metadata.fps} fps</Text>
              </div>
            )}

            {metadata.codec && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>Codec</Text>
                <Text className={styles.metadataValue}>{metadata.codec}</Text>
              </div>
            )}

            {metadata.bitrate && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>Bitrate</Text>
                <Text className={styles.metadataValue}>{metadata.bitrate}</Text>
              </div>
            )}

            {metadata.size && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>File Size</Text>
                <Text className={styles.metadataValue}>{metadata.size}</Text>
              </div>
            )}

            {metadata.format && (
              <div className={styles.metadataItem}>
                <Text className={styles.metadataLabel}>Format</Text>
                <Text className={styles.metadataValue}>{metadata.format}</Text>
              </div>
            )}
          </div>

          {(jobId || correlationId) && (
            <>
              <Divider style={{ marginTop: tokens.spacingVerticalM }} />
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, marginTop: tokens.spacingVerticalM }}>
                {jobId && (
                  <div className={styles.metadataItem}>
                    <Text className={styles.metadataLabel}>Job ID</Text>
                    <Badge appearance="outline">{jobId}</Badge>
                  </div>
                )}
                {correlationId && (
                  <div className={styles.metadataItem}>
                    <Text className={styles.metadataLabel}>Correlation ID</Text>
                    <Badge appearance="outline">{correlationId}</Badge>
                  </div>
                )}
              </div>
            </>
          )}
        </Card>
      )}
    </div>
  );
}
