import {
  makeStyles,
  tokens,
  Card,
  CardHeader,
  Text,
  Caption1,
  Body1,
  Badge,
} from '@fluentui/react-components';
import { VideoClip24Regular } from '@fluentui/react-icons';
import { useMemo } from 'react';

const useStyles = makeStyles({
  card: {
    height: '100%',
  },
  content: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  previewContainer: {
    position: 'relative',
    width: '100%',
    paddingBottom: '56.25%', // 16:9 aspect ratio by default
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewVertical: {
    paddingBottom: '177.78%', // 9:16 aspect ratio
  },
  previewSquare: {
    paddingBottom: '100%', // 1:1 aspect ratio
  },
  previewPlaceholder: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
  },
  specs: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  specRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  badgeContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
  },
});

interface PlatformSpec {
  name: string;
  resolution: string;
  aspectRatio: string;
  bitrate: string;
  fileSize: string;
  format: string;
}

const PLATFORM_SPECS: Record<string, PlatformSpec> = {
  youtube: {
    name: 'YouTube',
    resolution: '1920×1080',
    aspectRatio: '16:9',
    bitrate: '8 Mbps',
    fileSize: '~180 MB',
    format: 'MP4 (H.264)',
  },
  tiktok: {
    name: 'TikTok',
    resolution: '1080×1920',
    aspectRatio: '9:16',
    bitrate: '5 Mbps',
    fileSize: '~112 MB',
    format: 'MP4 (H.264)',
  },
  instagram: {
    name: 'Instagram',
    resolution: '1080×1920',
    aspectRatio: '9:16',
    bitrate: '5 Mbps',
    fileSize: '~112 MB',
    format: 'MP4 (H.264)',
  },
  linkedin: {
    name: 'LinkedIn',
    resolution: '1920×1080',
    aspectRatio: '16:9',
    bitrate: '5 Mbps',
    fileSize: '~112 MB',
    format: 'MP4 (H.264)',
  },
  twitter: {
    name: 'Twitter',
    resolution: '1280×720',
    aspectRatio: '16:9',
    bitrate: '5 Mbps',
    fileSize: '~112 MB',
    format: 'MP4 (H.264)',
  },
  facebook: {
    name: 'Facebook',
    resolution: '1280×720',
    aspectRatio: '16:9',
    bitrate: '4 Mbps',
    fileSize: '~90 MB',
    format: 'MP4 (H.264)',
  },
};

export interface ExportSettings {
  resolution: { width: number; height: number };
  quality: 'draft' | 'good' | 'high' | 'maximum';
  format: string;
  optimizeForPlatform: boolean;
}

export interface ExportPreviewCardProps {
  platformId: string;
  settings: ExportSettings;
  videoPath?: string;
  duration?: number; // in seconds
}

export function ExportPreviewCard({
  platformId,
  settings,
  videoPath: _videoPath,
  duration = 180, // 3 minutes default
}: ExportPreviewCardProps) {
  const styles = useStyles();
  const spec = PLATFORM_SPECS[platformId];

  const estimatedFileSize = useMemo(() => {
    if (!spec) return '~100 MB';

    // Parse bitrate (e.g., "5 Mbps" -> 5000)
    const bitrateMbps = parseInt(spec.bitrate);
    const bitrateKbps = bitrateMbps * 1000;

    // Calculate size: (bitrate * duration) / 8 / 1024
    const sizeKB = (bitrateKbps * duration) / 8;
    const sizeMB = sizeKB / 1024;

    return `~${Math.round(sizeMB)} MB`;
  }, [spec, duration]);

  const aspectRatioClass = useMemo(() => {
    if (!spec) return '';

    if (spec.aspectRatio === '9:16') {
      return styles.previewVertical;
    } else if (spec.aspectRatio === '1:1') {
      return styles.previewSquare;
    }
    return '';
  }, [spec, styles]);

  if (!spec) {
    return null;
  }

  return (
    <Card className={styles.card}>
      <CardHeader
        header={<Body1>{spec.name}</Body1>}
        description={<Caption1>Optimized export settings</Caption1>}
      />
      <div className={styles.content}>
        <div className={`${styles.previewContainer} ${aspectRatioClass}`}>
          <div className={styles.previewPlaceholder}>
            <VideoClip24Regular style={{ fontSize: '32px' }} />
            <Caption1>{spec.aspectRatio}</Caption1>
          </div>
        </div>

        <div className={styles.specs}>
          <div className={styles.specRow}>
            <Caption1>Resolution:</Caption1>
            <Text>{spec.resolution}</Text>
          </div>
          <div className={styles.specRow}>
            <Caption1>Bitrate:</Caption1>
            <Text>{spec.bitrate}</Text>
          </div>
          <div className={styles.specRow}>
            <Caption1>Est. Size:</Caption1>
            <Text>{estimatedFileSize}</Text>
          </div>
          <div className={styles.specRow}>
            <Caption1>Format:</Caption1>
            <Text>{spec.format}</Text>
          </div>
        </div>

        <div className={styles.badgeContainer}>
          {settings.optimizeForPlatform && (
            <Badge appearance="tint" color="success" size="small">
              Platform Optimized
            </Badge>
          )}
          <Badge appearance="outline" size="small">
            {settings.quality}
          </Badge>
        </div>
      </div>
    </Card>
  );
}
