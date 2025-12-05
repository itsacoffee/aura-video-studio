import { Badge, Button, Spinner, Tooltip, makeStyles, tokens } from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import React from 'react';
import { useOllamaHealth } from '../hooks/useOllamaHealth';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  tooltipContent: {
    padding: tokens.spacingVerticalS,
  },
  tooltipTitle: {
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  tooltipError: {
    color: tokens.colorStatusDangerForeground1,
  },
  tooltipHint: {
    marginTop: tokens.spacingVerticalS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

interface OllamaStatusIndicatorProps {
  /** Show detailed status text next to the badge */
  showDetails?: boolean;
  /** Polling interval in milliseconds (default: 30000ms) */
  pollingIntervalMs?: number;
}

/**
 * Visual indicator component for Ollama connection status
 * Shows a badge with tooltip containing detailed status information
 */
export function OllamaStatusIndicator({
  showDetails = false,
  pollingIntervalMs = 30000,
}: OllamaStatusIndicatorProps) {
  const styles = useStyles();
  const { status, isLoading, refresh } = useOllamaHealth(pollingIntervalMs);

  if (isLoading && !status) {
    return <Spinner size="tiny" label="Checking Ollama..." />;
  }

  const isHealthy = status?.isHealthy ?? false;
  const modelCount = status?.availableModels?.length ?? 0;

  const tooltipContent = (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipTitle}>
        {isHealthy ? 'Ollama Connected' : 'Ollama Unavailable'}
      </div>
      {isHealthy && status?.version && <div>Version: {status.version}</div>}
      {isHealthy && <div>Models: {modelCount}</div>}
      {!isHealthy && status?.errorMessage && (
        <div className={styles.tooltipError}>{status.errorMessage}</div>
      )}
      {!isHealthy && <div className={styles.tooltipHint}>Start Ollama with: ollama serve</div>}
    </div>
  );

  return (
    <div className={styles.container}>
      <Tooltip content={tooltipContent} relationship="description">
        <Badge
          appearance={isHealthy ? 'filled' : 'ghost'}
          color={isHealthy ? 'success' : 'danger'}
          icon={isHealthy ? <CheckmarkCircle24Regular /> : <ErrorCircle24Regular />}
        >
          {showDetails ? (isHealthy ? 'Ollama Online' : 'Ollama Offline') : ''}
        </Badge>
      </Tooltip>

      <Tooltip content="Refresh Ollama status" relationship="label">
        <Button
          appearance="subtle"
          size="small"
          icon={<ArrowClockwise24Regular />}
          onClick={refresh}
          aria-label="Refresh Ollama status"
        />
      </Tooltip>
    </div>
  );
}

export default OllamaStatusIndicator;
