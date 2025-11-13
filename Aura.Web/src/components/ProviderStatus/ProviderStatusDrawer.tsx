/**
 * Provider Status Drawer
 * 
 * Displays real-time status of the active provider during video generation.
 * Shows elapsed time, heartbeat count, progress indicators, and manual fallback option.
 */

import React, { useEffect, useState } from 'react';
import {
  Drawer,
  DrawerHeader,
  DrawerHeaderTitle,
  DrawerBody,
  Button,
  Spinner,
  Badge,
  Text,
  makeStyles,
  tokens
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  Warning24Regular,
  Checkmark24Regular,
  ErrorCircle24Regular,
  Clock24Regular
} from '@fluentui/react-icons';
import type { ProviderStatusInfo, ProviderStatusState } from '../../types/profileLock';
import { useProfileLockStore } from '../../state/profileLock';

const useStyles = makeStyles({
  drawer: {
    width: '400px'
  },
  statusSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL
  },
  statusHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM
  },
  statusBadge: {
    marginLeft: 'auto'
  },
  timeDisplay: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`
  },
  progressInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalM
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalL
  },
  warningMessage: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorPaletteYellowBorder2}`,
    marginTop: tokens.spacingVerticalM
  },
  errorMessage: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorPaletteRedBorder2}`,
    marginTop: tokens.spacingVerticalM
  }
});

interface ProviderStatusDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  status: ProviderStatusInfo | null;
  onManualFallback?: () => void;
  onCancel?: () => void;
}

const formatElapsedTime = (seconds: number): string => {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return mins > 0 ? `${mins}m ${secs}s` : `${secs}s`;
};

const getStatusColor = (state: ProviderStatusState): string => {
  switch (state) {
    case 'active':
      return tokens.colorPaletteGreenForeground2;
    case 'waiting':
      return tokens.colorPaletteYellowForeground2;
    case 'extended-wait':
      return tokens.colorPaletteOrangeForeground2;
    case 'stall-suspected':
      return tokens.colorPaletteRedForeground2;
    case 'error':
      return tokens.colorPaletteRedForeground2;
    case 'user-requested-fallback':
      return tokens.colorBrandForeground1;
    default:
      return tokens.colorNeutralForeground1;
  }
};

const getStatusIcon = (state: ProviderStatusState) => {
  switch (state) {
    case 'active':
      return <Spinner size="small" />;
    case 'waiting':
      return <Clock24Regular />;
    case 'extended-wait':
      return <Warning24Regular color={tokens.colorPaletteOrangeForeground2} />;
    case 'stall-suspected':
      return <Warning24Regular color={tokens.colorPaletteRedForeground2} />;
    case 'error':
      return <ErrorCircle24Regular color={tokens.colorPaletteRedForeground2} />;
    case 'user-requested-fallback':
      return <Checkmark24Regular color={tokens.colorBrandForeground1} />;
    default:
      return null;
  }
};

const getStatusLabel = (state: ProviderStatusState): string => {
  switch (state) {
    case 'active':
      return 'Processing';
    case 'waiting':
      return 'Extended Wait';
    case 'extended-wait':
      return 'Deep Wait';
    case 'stall-suspected':
      return 'Stall Suspected';
    case 'error':
      return 'Error';
    case 'user-requested-fallback':
      return 'Fallback Requested';
    default:
      return 'Unknown';
  }
};

const getStatusMessage = (status: ProviderStatusInfo): string => {
  switch (status.state) {
    case 'active':
      return 'Provider is processing your request. Please wait...';
    case 'waiting':
      return 'Operation is taking longer than usual. Provider is still responsive.';
    case 'extended-wait':
      return 'Long-running operation detected. You can wait or switch to another provider.';
    case 'stall-suspected':
      return 'Provider has not sent a heartbeat signal recently. It may be stalled.';
    case 'error':
      return 'Provider encountered a fatal error. Please try a different provider or retry.';
    case 'user-requested-fallback':
      return 'Preparing to switch to fallback provider...';
    default:
      return 'Monitoring provider status...';
  }
};

export const ProviderStatusDrawer: React.FC<ProviderStatusDrawerProps> = ({
  isOpen,
  onClose,
  status,
  onManualFallback,
  onCancel
}) => {
  const styles = useStyles();
  const [localElapsed, setLocalElapsed] = useState(status?.elapsedTimeSeconds ?? 0);
  const activeLock = useProfileLockStore((state) => state.activeLock);

  useEffect(() => {
    if (!status) return;

    const interval = setInterval(() => {
      setLocalElapsed((prev) => prev + 1);
    }, 1000);

    return () => clearInterval(interval);
  }, [status]);

  useEffect(() => {
    if (status) {
      setLocalElapsed(status.elapsedTimeSeconds);
    }
  }, [status]);

  if (!status) {
    return null;
  }

  const showFallbackButton = 
    (status.state === 'extended-wait' || status.state === 'stall-suspected' || status.state === 'error') &&
    status.canManuallyFallback &&
    onManualFallback;

  return (
    <Drawer
      open={isOpen}
      onOpenChange={(_, { open }) => !open && onClose()}
      position="end"
      className={styles.drawer}
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={
            <Button
              appearance="subtle"
              aria-label="Close"
              icon={<Dismiss24Regular />}
              onClick={onClose}
            />
          }
        >
          Provider Status
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody>
        <div className={styles.statusSection}>
          {/* Status Header */}
          <div className={styles.statusHeader}>
            {getStatusIcon(status.state)}
            <Text weight="semibold" size={400}>
              {status.providerName}
            </Text>
            <Badge
              appearance="filled"
              color={getStatusColor(status.state) as never}
              className={styles.statusBadge}
            >
              {getStatusLabel(status.state)}
            </Badge>
          </div>

          {/* Status Message */}
          <Text>{getStatusMessage(status)}</Text>

          {/* Time Display */}
          <div className={styles.timeDisplay}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <Text weight="semibold">Elapsed Time:</Text>
              <Text>{formatElapsedTime(localElapsed)}</Text>
            </div>
            
            {status.timeSinceLastHeartbeatSeconds !== undefined && (
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <Text weight="semibold">Since Last Heartbeat:</Text>
                <Text>{formatElapsedTime(status.timeSinceLastHeartbeatSeconds)}</Text>
              </div>
            )}
            
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <Text weight="semibold">Heartbeat Count:</Text>
              <Text>{status.heartbeatCount}</Text>
            </div>

            {status.estimatedNextCheckSeconds && (
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <Text weight="semibold">Next Check In:</Text>
                <Text>{formatElapsedTime(status.estimatedNextCheckSeconds)}</Text>
              </div>
            )}
          </div>

          {/* Progress Info */}
          {status.progress && (
            <div className={styles.progressInfo}>
              <Text weight="semibold">Progress:</Text>
              
              {status.progress.tokensGenerated !== undefined && (
                <Text>Tokens Generated: {status.progress.tokensGenerated}</Text>
              )}
              
              {status.progress.chunksProcessed !== undefined && (
                <Text>Chunks Processed: {status.progress.chunksProcessed}</Text>
              )}
              
              {status.progress.percentComplete !== undefined && (
                <Text>Completion: {status.progress.percentComplete.toFixed(1)}%</Text>
              )}
              
              {status.progress.message && (
                <Text style={{ fontStyle: 'italic' }}>{status.progress.message}</Text>
              )}
            </div>
          )}

          {/* Profile Lock Info */}
          {activeLock && (
            <div className={styles.timeDisplay}>
              <Text weight="semibold">Profile Lock Active:</Text>
              <Text size={300}>
                Provider {activeLock.providerName} is locked for this pipeline.
                {activeLock.offlineModeEnabled && ' Offline mode is enforced.'}
              </Text>
            </div>
          )}

          {/* Warning Messages */}
          {status.state === 'stall-suspected' && (
            <div className={styles.warningMessage}>
              <Text weight="semibold" style={{ display: 'block', marginBottom: '8px' }}>
                ⚠️ Provider May Be Stalled
              </Text>
              <Text size={300}>
                No heartbeat signal detected for {formatElapsedTime(status.timeSinceLastHeartbeatSeconds ?? 0)}.
                The provider may have encountered an issue or be processing a very large request.
              </Text>
            </div>
          )}

          {status.state === 'error' && (
            <div className={styles.errorMessage}>
              <Text weight="semibold" style={{ display: 'block', marginBottom: '8px' }}>
                ❌ Provider Error
              </Text>
              <Text size={300}>
                The provider encountered a fatal error and cannot continue.
                Please try a different provider or cancel and retry.
              </Text>
            </div>
          )}

          {/* Action Buttons */}
          <div className={styles.actionButtons}>
            {showFallbackButton && (
              <Button
                appearance="primary"
                onClick={onManualFallback}
              >
                Switch Provider
              </Button>
            )}
            
            {onCancel && (
              <Button
                appearance="secondary"
                onClick={onCancel}
              >
                Cancel Job
              </Button>
            )}
          </div>
        </div>
      </DrawerBody>
    </Drawer>
  );
};
