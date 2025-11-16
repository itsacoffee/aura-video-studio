/**
 * FFmpeg Technical Details Component
 * Displays comprehensive FFmpeg installation information including hardware acceleration
 * Updated to use PR 336 endpoints with detailed error codes and correlation IDs
 */

import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Text,
  Badge,
  Button,
  Spinner,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  Warning24Regular,
  Info24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import { handleApiError, type UserFriendlyError } from '../../services/api/errorHandler';
import { ffmpegClient, type FFmpegStatusExtended } from '../../services/api/ffmpegClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  detailsGrid: {
    display: 'grid',
    gridTemplateColumns: 'auto 1fr',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  detailLabel: {
    color: tokens.colorNeutralForeground3,
    fontWeight: tokens.fontWeightSemibold,
  },
  detailValue: {
    fontFamily: tokens.fontFamilyMonospace,
    wordBreak: 'break-all',
  },
  hardwareSection: {
    marginTop: tokens.spacingVerticalL,
  },
  hardwareList: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  encoderList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    marginTop: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
  correlationId: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

interface FFmpegTechnicalDetailsProps {
  onStatusChange?: (installed: boolean, valid: boolean) => void;
}

export const FFmpegTechnicalDetails: FC<FFmpegTechnicalDetailsProps> = ({ onStatusChange }) => {
  const styles = useStyles();
  const [status, setStatus] = useState<FFmpegStatusExtended | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<UserFriendlyError | null>(null);

  const loadStatus = async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await ffmpegClient.getStatusExtended();
      setStatus(result);
      onStatusChange?.(result.installed, result.valid);
    } catch (err: unknown) {
      const friendlyError = handleApiError(err);
      setError(friendlyError);
      onStatusChange?.(false, false);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading FFmpeg details..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>{error.title}</MessageBarTitle>
            <Text>{error.message}</Text>
            {error.correlationId && (
              <Text size={200} className={styles.correlationId}>
                Correlation ID: {error.correlationId}
              </Text>
            )}
            {error.howToFix && error.howToFix.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalM }}>
                <Text weight="semibold">How to fix:</Text>
                <ul>
                  {error.howToFix.map((step, index) => (
                    <li key={index}>
                      <Text>{step}</Text>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </MessageBarBody>
        </MessageBar>
        <div className={styles.actions}>
          <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={loadStatus}>
            Retry
          </Button>
        </div>
      </div>
    );
  }

  if (!status) {
    return null;
  }

  const getStatusBadge = () => {
    if (status.installed && status.valid) {
      return (
        <div className={styles.statusBadge}>
          <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
          <Badge color="success">Installed and Valid</Badge>
        </div>
      );
    }

    if (status.installed && !status.valid) {
      return (
        <div className={styles.statusBadge}>
          <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
          <Badge color="warning">Installed but Invalid</Badge>
        </div>
      );
    }

    return (
      <div className={styles.statusBadge}>
        <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
        <Badge color="danger">Not Installed</Badge>
      </div>
    );
  };

  const hasHardwareAcceleration =
    status.hardwareAcceleration.nvencSupported ||
    status.hardwareAcceleration.amfSupported ||
    status.hardwareAcceleration.quickSyncSupported ||
    status.hardwareAcceleration.videoToolboxSupported;

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.header}>
          <Title3>FFmpeg Technical Details</Title3>
          {getStatusBadge()}
        </div>

        {!status.valid && status.error && (
          <MessageBar intent="warning" style={{ marginBottom: tokens.spacingVerticalM }}>
            <MessageBarBody>
              <MessageBarTitle>Validation Issue</MessageBarTitle>
              <Text>{status.error}</Text>
              {status.errorCode && <Text size={200}>Error Code: {status.errorCode}</Text>}
            </MessageBarBody>
          </MessageBar>
        )}

        <div className={styles.detailsGrid}>
          <Text className={styles.detailLabel}>Version:</Text>
          <Text className={styles.detailValue}>
            {status.version || 'Unknown'}
            {status.minimumVersion && !status.versionMeetsRequirement && (
              <Badge color="warning" style={{ marginLeft: tokens.spacingHorizontalXS }}>
                Min: {status.minimumVersion}
              </Badge>
            )}
          </Text>

          <Text className={styles.detailLabel}>Path:</Text>
          <Text className={styles.detailValue}>{status.path || 'Not found'}</Text>

          <Text className={styles.detailLabel}>Source:</Text>
          <Text className={styles.detailValue}>{status.source}</Text>

          {status.attemptedPaths && status.attemptedPaths.length > 0 && (
            <>
              <Text className={styles.detailLabel}>Searched Paths:</Text>
              <div>
                {status.attemptedPaths.map((path, index) => (
                  <Text key={index} size={200} block className={styles.detailValue}>
                    {path}
                  </Text>
                ))}
              </div>
            </>
          )}

          {status.correlationId && (
            <>
              <Text className={styles.detailLabel}>Correlation ID:</Text>
              <Text className={styles.correlationId}>{status.correlationId}</Text>
            </>
          )}
        </div>

        <div className={styles.hardwareSection}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <Title3>Hardware Acceleration</Title3>
            {hasHardwareAcceleration ? (
              <Badge color="success">Available</Badge>
            ) : (
              <Badge color="subtle">Not Available</Badge>
            )}
          </div>

          <div className={styles.hardwareList}>
            <Badge
              color={status.hardwareAcceleration.nvencSupported ? 'success' : 'subtle'}
              icon={
                status.hardwareAcceleration.nvencSupported ? (
                  <CheckmarkCircle24Filled />
                ) : (
                  <Info24Regular />
                )
              }
            >
              NVENC (NVIDIA)
            </Badge>
            <Badge
              color={status.hardwareAcceleration.amfSupported ? 'success' : 'subtle'}
              icon={
                status.hardwareAcceleration.amfSupported ? (
                  <CheckmarkCircle24Filled />
                ) : (
                  <Info24Regular />
                )
              }
            >
              AMF (AMD)
            </Badge>
            <Badge
              color={status.hardwareAcceleration.quickSyncSupported ? 'success' : 'subtle'}
              icon={
                status.hardwareAcceleration.quickSyncSupported ? (
                  <CheckmarkCircle24Filled />
                ) : (
                  <Info24Regular />
                )
              }
            >
              QuickSync (Intel)
            </Badge>
            <Badge
              color={status.hardwareAcceleration.videoToolboxSupported ? 'success' : 'subtle'}
              icon={
                status.hardwareAcceleration.videoToolboxSupported ? (
                  <CheckmarkCircle24Filled />
                ) : (
                  <Info24Regular />
                )
              }
            >
              VideoToolbox (Apple)
            </Badge>
          </div>

          {status.hardwareAcceleration.availableEncoders &&
            status.hardwareAcceleration.availableEncoders.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalM }}>
                <Text weight="semibold">Available Encoders:</Text>
                <div className={styles.encoderList}>
                  {status.hardwareAcceleration.availableEncoders.map((encoder, index) => (
                    <Text key={index} size={200}>
                      • {encoder}
                    </Text>
                  ))}
                </div>
              </div>
            )}
        </div>

        <div className={styles.actions}>
          <Button appearance="subtle" icon={<ArrowClockwise24Regular />} onClick={loadStatus}>
            Refresh Status
          </Button>
        </div>
      </Card>
    </div>
  );
};
