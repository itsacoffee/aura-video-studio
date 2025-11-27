/**
 * AssetPreviewModal - Modal component for previewing media assets
 *
 * Displays video, audio, or image assets in a modal dialog with playback controls
 * and metadata information.
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import React from 'react';
import {
  formatFileSize as formatFileSizeUtil,
  formatDuration as formatDurationUtil,
} from '../../utils/formatters';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    maxHeight: '70vh',
    overflow: 'auto',
  },
  mediaContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    minHeight: '200px',
  },
  video: {
    width: '100%',
    maxHeight: '400px',
    borderRadius: tokens.borderRadiusMedium,
  },
  audio: {
    width: '100%',
    marginTop: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
  },
  image: {
    width: '100%',
    maxHeight: '400px',
    objectFit: 'contain',
    borderRadius: tokens.borderRadiusMedium,
  },
  metadata: {
    display: 'grid',
    gridTemplateColumns: 'auto 1fr',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  metadataLabel: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
  metadataValue: {
    color: tokens.colorNeutralForeground1,
    wordBreak: 'break-all',
  },
  dialogSurface: {
    maxWidth: '700px',
    width: '90vw',
  },
});

export interface AssetPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  asset: {
    id: string;
    name: string;
    type: 'video' | 'audio' | 'image';
    filePath?: string;
    preview?: string;
    duration?: number;
    fileSize?: number;
  } | null;
}

/**
 * Wrapper for formatFileSize that handles undefined values
 */
function formatFileSize(bytes: number | undefined): string {
  if (bytes === undefined) return 'Unknown';
  return formatFileSizeUtil(bytes);
}

/**
 * Wrapper for formatDuration that handles undefined values
 */
function formatDuration(seconds: number | undefined): string {
  if (seconds === undefined) return 'Unknown';
  return formatDurationUtil(seconds);
}

export function AssetPreviewModal({ isOpen, onClose, asset }: AssetPreviewModalProps) {
  const styles = useStyles();

  if (!asset) return null;

  // Get the source URL for media
  const mediaSource = asset.preview || asset.filePath || '';

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle>{asset.name}</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.mediaContainer}>
              {asset.type === 'video' && mediaSource && (
                // eslint-disable-next-line jsx-a11y/media-has-caption
                <video className={styles.video} src={mediaSource} controls autoPlay={false}>
                  Your browser does not support the video element.
                </video>
              )}
              {asset.type === 'audio' && mediaSource && (
                // eslint-disable-next-line jsx-a11y/media-has-caption
                <audio className={styles.audio} src={mediaSource} controls>
                  Your browser does not support the audio element.
                </audio>
              )}
              {asset.type === 'image' && mediaSource && (
                <img className={styles.image} src={mediaSource} alt={asset.name} />
              )}
              {!mediaSource && <Text>Preview not available</Text>}
            </div>
            <div className={styles.metadata}>
              <Text className={styles.metadataLabel}>Type:</Text>
              <Text className={styles.metadataValue}>
                {asset.type.charAt(0).toUpperCase() + asset.type.slice(1)}
              </Text>

              {(asset.type === 'video' || asset.type === 'audio') && (
                <>
                  <Text className={styles.metadataLabel}>Duration:</Text>
                  <Text className={styles.metadataValue}>{formatDuration(asset.duration)}</Text>
                </>
              )}

              <Text className={styles.metadataLabel}>Size:</Text>
              <Text className={styles.metadataValue}>{formatFileSize(asset.fileSize)}</Text>

              {asset.filePath && (
                <>
                  <Text className={styles.metadataLabel}>Path:</Text>
                  <Text className={styles.metadataValue}>{asset.filePath}</Text>
                </>
              )}
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" icon={<Dismiss24Regular />} onClick={onClose}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
