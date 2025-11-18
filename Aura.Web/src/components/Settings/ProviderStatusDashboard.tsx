import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Card,
  Button,
  Spinner,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  DismissCircle24Filled,
  ArrowClockwise24Regular,
  Warning24Filled,
  Info24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { useProviderStore } from '../../state/providers';
import type { ProviderStatus } from '../../state/providers';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  providerCard: {
    padding: tokens.spacingVerticalM,
  },
  providerHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  providerName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  statusRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
  icon: {
    fontSize: '20px',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  errorDetails: {
    marginTop: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: '12px',
  },
  howToFix: {
    marginTop: tokens.spacingVerticalXXS,
    paddingLeft: tokens.spacingHorizontalM,
  },
  retryButton: {
    marginTop: tokens.spacingVerticalXS,
  },
});

function ProviderStatusCard({ provider }: { provider: ProviderStatus }) {
  const styles = useStyles();
  const { validateProvider } = useProviderStore();
  const [isValidating, setIsValidating] = useState(false);

  const handleRetry = async () => {
    setIsValidating(true);
    try {
      await validateProvider(provider.name);
    } finally {
      setIsValidating(false);
    }
  };

  const isConfiguredAndReachable = provider.isConfigured && (provider.reachable ?? true);
  const hasError = !isConfiguredAndReachable || provider.errorCode;

  const statusIcon = isConfiguredAndReachable ? (
    <CheckmarkCircle24Filled
      className={styles.icon}
      style={{ color: tokens.colorPaletteGreenForeground1 }}
    />
  ) : !provider.isConfigured ? (
    <Info24Regular className={styles.icon} style={{ color: tokens.colorNeutralForeground3 }} />
  ) : (
    <Warning24Filled className={styles.icon} style={{ color: tokens.colorPaletteRedForeground1 }} />
  );

  const statusBadge = isConfiguredAndReachable ? (
    <Badge appearance="filled" color="success">
      Configured & Reachable
    </Badge>
  ) : !provider.isConfigured ? (
    <Badge appearance="outline" color="subtle">
      Not Configured
    </Badge>
  ) : (
    <Badge appearance="filled" color="danger">
      Error
    </Badge>
  );

  return (
    <Card className={styles.providerCard}>
      <div className={styles.providerHeader}>
        <Text className={styles.providerName}>{provider.name}</Text>
        {statusBadge}
      </div>
      <div className={styles.statusRow}>
        {statusIcon}
        <Text size={200}>{provider.status}</Text>
      </div>
      {provider.category && (
        <Text size={100} style={{ marginTop: tokens.spacingVerticalXXS }}>
          Category: {provider.category} | Tier: {provider.tier || 'Unknown'}
        </Text>
      )}
      {provider.lastValidated && (
        <Text size={100} style={{ marginTop: tokens.spacingVerticalXS }}>
          Last validated: {new Date(provider.lastValidated).toLocaleString()}
        </Text>
      )}
      {hasError && provider.errorMessage && (
        <div className={styles.errorDetails}>
          <Text size={200} weight="semibold" style={{ color: tokens.colorPaletteRedForeground1 }}>
            {provider.errorCode || 'Error'}
          </Text>
          <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
            {provider.errorMessage}
          </Text>
          {provider.howToFix && provider.howToFix.length > 0 && (
            <div className={styles.howToFix}>
              <Text size={100} weight="semibold">
                How to fix:
              </Text>
              <ul
                style={{
                  margin: `${tokens.spacingVerticalXXS} 0`,
                  paddingLeft: tokens.spacingHorizontalM,
                }}
              >
                {provider.howToFix.map((step, index) => (
                  <li key={index}>
                    <Text size={100}>{step}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
      {hasError && (
        <Button
          appearance="secondary"
          size="small"
          onClick={handleRetry}
          disabled={isValidating}
          className={styles.retryButton}
        >
          {isValidating ? 'Retrying...' : 'Retry Validation'}
        </Button>
      )}
    </Card>
  );
}

export function ProviderStatusDashboard() {
  const styles = useStyles();
  const { providerStatuses, isLoadingStatuses, refreshProviderStatuses } = useProviderStore();

  useEffect(() => {
    refreshProviderStatuses();

    const pollInterval = setInterval(() => {
      refreshProviderStatuses();
    }, 30000);

    return () => clearInterval(pollInterval);
  }, [refreshProviderStatuses]);

  if (isLoadingStatuses) {
    return (
      <Card className={styles.container}>
        <div className={styles.loadingContainer}>
          <Spinner label="Loading provider status..." />
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Title3>Provider Status</Title3>
        <Button
          icon={<ArrowClockwise24Regular />}
          onClick={refreshProviderStatuses}
          disabled={isLoadingStatuses}
        >
          Refresh
        </Button>
      </div>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        View the status of all configured providers. Status updates automatically every 30 seconds.
        Click 'Retry Validation' on any provider to perform a live connectivity check.
      </Text>
      <div className={styles.grid}>
        {providerStatuses.map((provider) => (
          <ProviderStatusCard key={provider.name} provider={provider} />
        ))}
      </div>
    </Card>
  );
}
