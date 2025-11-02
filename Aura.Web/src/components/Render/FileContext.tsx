import {
  makeStyles,
  tokens,
  Card,
  Button,
  Badge,
  Body1,
  Body1Strong,
  Caption1,
  Divider,
} from '@fluentui/react-components';
import {
  DocumentRegular,
  VideoRegular,
  MusicNote2Regular,
  FolderOpenRegular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    marginBottom: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  fileInfo: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    alignItems: 'center',
  },
  icon: {
    fontSize: '48px',
    color: tokens.colorBrandForeground1,
  },
  details: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  metadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
  },
  metadataItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
  },
  emptyIcon: {
    fontSize: '64px',
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

export interface FileMetadata {
  name: string;
  path: string;
  type: 'video' | 'audio';
  duration: number;
  size: number;
  resolution?: {
    width: number;
    height: number;
  };
  codec?: string;
  bitrate?: number;
  fps?: number;
  audioCodec?: string;
  sampleRate?: number;
}

interface FileContextProps {
  file: FileMetadata | null;
  onSelectFile: () => void;
  onClearFile?: () => void;
}

export function FileContext({ file, onSelectFile, onClearFile }: FileContextProps) {
  const styles = useStyles();

  const formatDuration = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);

    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  };

  const formatFileSize = (bytes: number): string => {
    const mb = bytes / (1024 * 1024);
    if (mb >= 1024) {
      return `${(mb / 1024).toFixed(2)} GB`;
    }
    return `${mb.toFixed(2)} MB`;
  };

  const formatBitrate = (bps: number): string => {
    const mbps = bps / 1000000;
    if (mbps >= 1) {
      return `${mbps.toFixed(2)} Mbps`;
    }
    return `${(bps / 1000).toFixed(0)} kbps`;
  };

  if (!file) {
    return (
      <div className={styles.container}>
        <Card className={styles.card}>
          <div className={styles.emptyState}>
            <DocumentRegular className={styles.emptyIcon} />
            <Body1Strong>No File Selected</Body1Strong>
            <Caption1>Select a file to configure render settings and start re-encoding</Caption1>
            <Button appearance="primary" icon={<FolderOpenRegular />} onClick={onSelectFile}>
              Select File
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.fileInfo}>
          {file.type === 'video' ? (
            <VideoRegular className={styles.icon} />
          ) : (
            <MusicNote2Regular className={styles.icon} />
          )}

          <div className={styles.details}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
              <div>
                <Body1Strong>{file.name}</Body1Strong>
                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>{file.path}</Caption1>
              </div>
              <div className={styles.actions}>
                <Button appearance="secondary" size="small" onClick={onSelectFile}>
                  Change File
                </Button>
                {onClearFile && (
                  <Button appearance="subtle" size="small" onClick={onClearFile}>
                    Clear
                  </Button>
                )}
              </div>
            </div>

            <Divider />

            <div className={styles.metadata}>
              <div className={styles.metadataItem}>
                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Duration</Caption1>
                <Body1>{formatDuration(file.duration)}</Body1>
              </div>

              <div className={styles.metadataItem}>
                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Size</Caption1>
                <Body1>{formatFileSize(file.size)}</Body1>
              </div>

              {file.resolution && (
                <div className={styles.metadataItem}>
                  <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Resolution</Caption1>
                  <Body1>
                    {file.resolution.width} × {file.resolution.height}
                  </Body1>
                </div>
              )}

              {file.fps && (
                <div className={styles.metadataItem}>
                  <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Frame Rate</Caption1>
                  <Body1>{file.fps} fps</Body1>
                </div>
              )}

              {file.codec && (
                <div className={styles.metadataItem}>
                  <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                    {file.type === 'video' ? 'Video Codec' : 'Audio Codec'}
                  </Caption1>
                  <Badge>{file.codec}</Badge>
                </div>
              )}

              {file.bitrate && (
                <div className={styles.metadataItem}>
                  <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Bitrate</Caption1>
                  <Body1>{formatBitrate(file.bitrate)}</Body1>
                </div>
              )}

              {file.audioCodec && (
                <div className={styles.metadataItem}>
                  <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Audio Codec</Caption1>
                  <Badge>{file.audioCodec}</Badge>
                </div>
              )}

              {file.sampleRate && (
                <div className={styles.metadataItem}>
                  <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>Sample Rate</Caption1>
                  <Body1>{(file.sampleRate / 1000).toFixed(1)} kHz</Body1>
                </div>
              )}
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
