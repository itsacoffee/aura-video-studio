import { Badge, Button, Tooltip, makeStyles, tokens } from '@fluentui/react-components';
import {
  PresenceAvailableRegular,
  PresenceBlockedRegular,
  PresenceUnknownRegular,
  ArrowClockwise16Regular,
} from '@fluentui/react-icons';
import { useMemo } from 'react';
import { useBackendHealth } from '@/hooks/useBackendHealth';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  tooltipContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    maxWidth: '280px',
  },
  label: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  meta: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
});

const statusCopy = {
  online: {
    label: 'Backend Online',
    icon: PresenceAvailableRegular,
    color: 'success' as const,
  },
  offline: {
    label: 'Backend Offline',
    icon: PresenceBlockedRegular,
    color: 'danger' as const,
  },
  unknown: {
    label: 'Backend Status Unknown',
    icon: PresenceUnknownRegular,
    color: 'informative' as const,
  },
};

export function BackendStatusIndicator() {
  const styles = useStyles();
  const { status, diagnostics, error, lastChecked, refresh } = useBackendHealth();
  const statusKey = status ?? 'unknown';
  const copy = statusCopy[statusKey] ?? statusCopy.unknown;
  const Icon = copy.icon;

  const tooltip = useMemo(() => {
    return (
      <div className={styles.tooltipContent}>
        <span className={styles.label}>{copy.label}</span>
        {diagnostics?.backend?.baseUrl && (
          <span className={styles.meta}>Base URL: {diagnostics.backend.baseUrl}</span>
        )}
        {diagnostics?.environment?.mode && (
          <span className={styles.meta}>
            Environment: {diagnostics.environment.mode}{' '}
            {diagnostics.environment.version ? `(${diagnostics.environment.version})` : ''}
          </span>
        )}
        {lastChecked && (
          <span className={styles.meta}>
            Last checked: {lastChecked.toLocaleTimeString()}
          </span>
        )}
        {error && <span className={styles.meta}>Last error: {error}</span>}
      </div>
    );
  }, [copy.label, diagnostics, error, lastChecked, styles.label, styles.meta, styles.tooltipContent]);

  return (
    <Tooltip content={tooltip} relationship="label">
      <div className={styles.container}>
        <Badge appearance="tint" icon={<Icon />} size="small" shape="rounded" color={copy.color}>
          {copy.label}
        </Badge>
        <Button
          icon={<ArrowClockwise16Regular />}
          appearance="subtle"
          size="small"
          aria-label="Refresh backend status"
          onClick={refresh}
        />
      </div>
    </Tooltip>
  );
}

