/**
 * Provider Health Indicator Component
 *
 * Compact health indicator for the app header showing provider availability.
 * - Green dot: All providers healthy
 * - Yellow dot: Some providers unavailable (degraded mode)
 * - Red dot: Critical providers unavailable
 *
 * Polls every 30 seconds and shows tooltip on hover with status summary.
 * Clicking opens a drawer with detailed provider status information.
 */

import {
  makeStyles,
  tokens,
  Tooltip,
  Button,
  Text,
  Spinner,
  Drawer,
  DrawerHeader,
  DrawerHeaderTitle,
  DrawerBody,
} from '@fluentui/react-components';
import { Circle20Filled, ChevronRight20Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, type FC } from 'react';
import { useProviderStatus, type ProviderHealthLevel } from '../../hooks/useProviderStatus';
import { ProviderStatusPanel } from '../ProviderStatusPanel';

const POLL_INTERVAL = 30000; // 30 seconds as specified in requirements

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
  },
  button: {
    minWidth: 'auto',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    gap: tokens.spacingHorizontalXS,
    borderRadius: tokens.borderRadiusMedium,
  },
  statusDot: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  healthyDot: {
    color: tokens.colorPaletteGreenForeground1,
  },
  degradedDot: {
    color: tokens.colorPaletteYellowForeground1,
  },
  criticalDot: {
    color: tokens.colorPaletteRedForeground1,
  },
  tooltipContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalXS,
  },
  tooltipTitle: {
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXXS,
  },
  tooltipRow: {
    display: 'flex',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
    fontSize: tokens.fontSizeBase200,
  },
  tooltipLabel: {
    color: tokens.colorNeutralForeground2,
  },
  tooltipValue: {
    fontWeight: tokens.fontWeightMedium,
  },
  tooltipFooter: {
    marginTop: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  drawer: {
    width: '450px',
  },
  drawerBody: {
    padding: tokens.spacingVerticalM,
  },
});

/**
 * Get the appropriate CSS class for the status dot based on health level
 */
function getStatusDotClass(
  level: ProviderHealthLevel,
  styles: ReturnType<typeof useStyles>
): string {
  switch (level) {
    case 'healthy':
      return styles.healthyDot;
    case 'degraded':
      return styles.degradedDot;
    case 'critical':
      return styles.criticalDot;
    default:
      return styles.degradedDot;
  }
}

/**
 * Get aria-label for accessibility based on health level
 */
function getAriaLabel(level: ProviderHealthLevel, message: string): string {
  const levelText =
    level === 'healthy'
      ? 'All providers healthy'
      : level === 'degraded'
        ? 'Some providers unavailable'
        : 'Critical providers unavailable';
  return `Provider Status: ${levelText}. ${message}. Click to view details.`;
}

interface TooltipContentProps {
  level: ProviderHealthLevel;
  message: string;
  availableLlm: number;
  totalLlm: number;
  availableTts: number;
  totalTts: number;
  availableImages: number;
  totalImages: number;
  lastUpdated: Date | null;
}

const TooltipContentComponent: FC<TooltipContentProps> = ({
  level,
  message,
  availableLlm,
  totalLlm,
  availableTts,
  totalTts,
  availableImages,
  totalImages,
  lastUpdated,
}) => {
  const styles = useStyles();

  const levelText =
    level === 'healthy' ? 'Healthy' : level === 'degraded' ? 'Degraded' : 'Critical';

  return (
    <div className={styles.tooltipContent}>
      <Text className={styles.tooltipTitle}>Provider Status: {levelText}</Text>
      <Text size={200}>{message}</Text>

      <div className={styles.tooltipRow}>
        <span className={styles.tooltipLabel}>Script (LLM):</span>
        <span className={styles.tooltipValue}>
          {availableLlm}/{totalLlm} available
        </span>
      </div>

      <div className={styles.tooltipRow}>
        <span className={styles.tooltipLabel}>Voice (TTS):</span>
        <span className={styles.tooltipValue}>
          {availableTts}/{totalTts} available
        </span>
      </div>

      <div className={styles.tooltipRow}>
        <span className={styles.tooltipLabel}>Images:</span>
        <span className={styles.tooltipValue}>
          {availableImages}/{totalImages} available
        </span>
      </div>

      <div className={styles.tooltipFooter}>
        <span>Click for details</span>
        <ChevronRight20Regular />
      </div>

      {lastUpdated && (
        <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>
          Updated: {lastUpdated.toLocaleTimeString()}
        </Text>
      )}
    </div>
  );
};

export interface ProviderHealthIndicatorProps {
  /** Custom poll interval in milliseconds (default: 30000) */
  pollInterval?: number;
}

/**
 * Provider Health Indicator
 *
 * A compact indicator showing overall provider health status.
 * Displays in the app header and provides quick access to detailed status.
 */
export const ProviderHealthIndicator: FC<ProviderHealthIndicatorProps> = ({
  pollInterval = POLL_INTERVAL,
}) => {
  const styles = useStyles();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  const { healthSummary, isLoading, lastUpdated } = useProviderStatus(pollInterval);

  const handleClick = () => {
    setIsDrawerOpen(true);
  };

  const handleDrawerClose = () => {
    setIsDrawerOpen(false);
  };

  // Show loading spinner during initial load
  if (isLoading && !lastUpdated) {
    return (
      <div className={styles.container}>
        <Button
          appearance="subtle"
          className={styles.button}
          disabled
          aria-label="Loading provider status..."
        >
          <Spinner size="tiny" />
        </Button>
      </div>
    );
  }

  const {
    level,
    message,
    availableLlm,
    totalLlm,
    availableTts,
    totalTts,
    availableImages,
    totalImages,
  } = healthSummary;

  const dotClass = getStatusDotClass(level, styles);
  const ariaLabel = getAriaLabel(level, message);

  return (
    <>
      <div className={styles.container}>
        <Tooltip
          content={
            <TooltipContentComponent
              level={level}
              message={message}
              availableLlm={availableLlm}
              totalLlm={totalLlm}
              availableTts={availableTts}
              totalTts={totalTts}
              availableImages={availableImages}
              totalImages={totalImages}
              lastUpdated={lastUpdated}
            />
          }
          relationship="description"
          positioning="below"
        >
          <Button
            appearance="subtle"
            className={styles.button}
            onClick={handleClick}
            aria-label={ariaLabel}
            icon={
              <span className={`${styles.statusDot} ${dotClass}`}>
                <Circle20Filled />
              </span>
            }
          />
        </Tooltip>
      </div>

      {/* Provider Status Drawer with detailed information */}
      <Drawer
        open={isDrawerOpen}
        onOpenChange={(_, { open }) => setIsDrawerOpen(open)}
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
                onClick={handleDrawerClose}
              />
            }
          >
            Provider Status
          </DrawerHeaderTitle>
        </DrawerHeader>
        <DrawerBody className={styles.drawerBody}>
          <ProviderStatusPanel showRecommendations={true} compact={false} />
        </DrawerBody>
      </Drawer>
    </>
  );
};
