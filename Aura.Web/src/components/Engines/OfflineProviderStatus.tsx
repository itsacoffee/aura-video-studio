import {
  Card,
  CardHeader,
  CardPreview,
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Badge,
  Spinner,
  Link,
} from '@fluentui/react-components';
import {
  Cloud24Regular,
  CloudOff24Regular,
  Checkmark24Regular,
  Dismiss24Regular,
  ArrowSync24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { apiUrl } from '../../config/api';
import { useNotifications } from '../Notifications/Toasts';

const useStyles = makeStyles({
  card: {
    width: '100%',
    marginBottom: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  content: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  statusGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  providerCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  providerHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  providerName: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  providerMessage: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  providerDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  recommendationsList: {
    paddingLeft: tokens.spacingHorizontalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  recommendation: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalXL,
  },
  summary: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  summaryRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

interface ProviderStatus {
  name: string;
  isAvailable: boolean;
  message: string;
  version?: string;
  details: Record<string, unknown>;
  recommendations: string[];
  installationGuideUrl?: string;
}

interface OfflineProvidersStatus {
  piper: ProviderStatus;
  mimic3: ProviderStatus;
  ollama: ProviderStatus;
  stableDiffusion: ProviderStatus;
  windowsTts: ProviderStatus;
  checkedAt: string;
  hasTtsProvider: boolean;
  hasLlmProvider: boolean;
  hasImageProvider: boolean;
  isFullyOperational: boolean;
}

export function OfflineProviderStatus() {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState<OfflineProvidersStatus | null>(null);

  const loadStatus = useCallback(async () => {
    setLoading(true);
    
    try {
      const response = await fetch(`${apiUrl}/offline-providers/status`);
      
      if (!response.ok) {
        throw new Error(`Failed to load status: ${response.statusText}`);
      }
      
      const data = await response.json();
      setStatus(data);
      
      if (data.isFullyOperational) {
        showSuccessToast('Offline Mode Ready', 'All critical offline providers are available');
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
      showFailureToast('Failed to load offline provider status', errorMessage);
    } finally {
      setLoading(false);
    }
  }, [showSuccessToast, showFailureToast]);

  useEffect(() => {
    void loadStatus();
  }, [loadStatus]);

  const renderProviderCard = (provider: ProviderStatus) => (
    <div key={provider.name} className={styles.providerCard}>
      <div className={styles.providerHeader}>
        <div className={styles.providerName}>{provider.name}</div>
        <Badge
          appearance="filled"
          color={provider.isAvailable ? 'success' : 'important'}
          icon={provider.isAvailable ? <Checkmark24Regular /> : <Dismiss24Regular />}
        >
          {provider.isAvailable ? 'Available' : 'Not Available'}
        </Badge>
      </div>
      <div className={styles.providerMessage}>{provider.message}</div>
      {provider.version && (
        <div className={styles.providerDetails}>
          <Text size={200}>Version: {provider.version}</Text>
        </div>
      )}
      {Object.keys(provider.details).length > 0 && (
        <div className={styles.providerDetails}>
          {Object.entries(provider.details).map(([key, value]) => (
            <div key={key}>
              {key}: {String(value)}
            </div>
          ))}
        </div>
      )}
      {provider.recommendations.length > 0 && (
        <div>
          <Text size={200} weight="semibold">Recommendations:</Text>
          <ul className={styles.recommendationsList}>
            {provider.recommendations.map((rec, index) => (
              <li key={index} className={styles.recommendation}>{rec}</li>
            ))}
          </ul>
        </div>
      )}
      {provider.installationGuideUrl && !provider.isAvailable && (
        <Link href={provider.installationGuideUrl} target="_blank">
          Installation Guide
        </Link>
      )}
    </div>
  );

  if (loading) {
    return (
      <Card className={styles.card}>
        <CardPreview className={styles.loading}>
          <Spinner size="small" />
          <Text>Checking offline provider availability...</Text>
        </CardPreview>
      </Card>
    );
  }

  if (!status) {
    return null;
  }

  const Icon = status.isFullyOperational ? Cloud24Regular : CloudOff24Regular;

  return (
    <Card className={styles.card}>
      <CardHeader
        header={
          <div className={styles.header}>
            <Icon />
            <Title3>Offline Provider Status</Title3>
          </div>
        }
        description={`Last checked: ${new Date(status.checkedAt).toLocaleTimeString()}`}
        action={
          <Button
            appearance="subtle"
            icon={<ArrowSync24Regular />}
            onClick={() => void loadStatus()}
          >
            Refresh
          </Button>
        }
      />
      <CardPreview>
        <div className={styles.content}>
          {/* Overall Status Summary */}
          <div className={styles.summary}>
            <Text weight="semibold">Offline Mode Capability</Text>
            <div className={styles.summaryRow}>
              {status.hasTtsProvider ? <Checkmark24Regular color="green" /> : <Dismiss24Regular color="red" />}
              <Text>Text-to-Speech: {status.hasTtsProvider ? 'Available' : 'Not Available'}</Text>
            </div>
            <div className={styles.summaryRow}>
              {status.hasLlmProvider ? <Checkmark24Regular color="green" /> : <Dismiss24Regular color="red" />}
              <Text>Script Generation: {status.hasLlmProvider ? 'Available' : 'Not Available'}</Text>
            </div>
            <div className={styles.summaryRow}>
              {status.hasImageProvider ? <Checkmark24Regular color="green" /> : <Info24Regular color="orange" />}
              <Text>Image Generation: {status.hasImageProvider ? 'Available' : 'Use Stock Images'}</Text>
            </div>
            {status.isFullyOperational && (
              <Badge appearance="filled" color="success" style={{ alignSelf: 'flex-start' }}>
                ✅ Ready for Offline Video Generation
              </Badge>
            )}
            {!status.isFullyOperational && (
              <Badge appearance="filled" color="warning" style={{ alignSelf: 'flex-start' }}>
                ⚠️ Setup Required for Full Offline Mode
              </Badge>
            )}
          </div>

          {/* Individual Provider Status */}
          <div className={styles.statusGrid}>
            {renderProviderCard(status.piper)}
            {renderProviderCard(status.mimic3)}
            {renderProviderCard(status.ollama)}
            {renderProviderCard(status.stableDiffusion)}
            {renderProviderCard(status.windowsTts)}
          </div>
        </div>
      </CardPreview>
    </Card>
  );
}
