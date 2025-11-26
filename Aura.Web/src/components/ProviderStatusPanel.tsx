import {
  Card,
  Text,
  Badge,
  Button,
  Spinner,
  makeStyles,
  tokens,
  Title3,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  ErrorCircle24Regular,
  ArrowClockwise24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useProviderStatus, type ProviderStatus } from '../hooks/useProviderStatus';
import { ProviderRecommendations } from './ProviderRecommendations';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  providerList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  providerItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  providerInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flex: 1,
  },
  providerName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  statusIcon: {
    display: 'flex',
    alignItems: 'center',
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXXS,
  },
  refreshButton: {
    marginTop: tokens.spacingVerticalM,
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingHorizontalM,
  },
  lastUpdated: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface ProviderSectionProps {
  title: string;
  providers: ProviderStatus[];
}

function ProviderSection({ title, providers }: ProviderSectionProps) {
  const styles = useStyles();

  if (providers.length === 0) {
    return null;
  }

  return (
    <div className={styles.section}>
      <Title3>{title}</Title3>
      <div className={styles.providerList}>
        {providers.map((provider) => (
          <div key={provider.name} className={styles.providerItem}>
            <div className={styles.providerInfo}>
              <div className={styles.statusIcon}>
                {provider.available ? (
                  <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                ) : (
                  <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
                )}
              </div>
              <div>
                <Text className={styles.providerName}>{provider.name}</Text>
                {provider.errorMessage && (
                  <div className={styles.errorText}>{provider.errorMessage}</div>
                )}
                {provider.details && (
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    {provider.details}
                  </Text>
                )}
                {provider.howToFix && provider.howToFix.length > 0 && (
                  <div style={{ marginTop: tokens.spacingVerticalXS }}>
                    <Text
                      size={200}
                      weight="semibold"
                      style={{ color: tokens.colorNeutralForeground2, display: 'block', marginBottom: tokens.spacingVerticalXXS }}
                    >
                      How to fix:
                    </Text>
                    <ul
                      style={{
                        margin: 0,
                        paddingLeft: tokens.spacingHorizontalM,
                        color: tokens.colorNeutralForeground3,
                      }}
                    >
                      {provider.howToFix.map((step, index) => (
                        <li key={index}>
                          <Text size={200}>{step}</Text>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </div>
            <Badge
              appearance={provider.available ? 'filled' : 'outline'}
              color={
                provider.tier === 'paid'
                  ? 'brand'
                  : provider.tier === 'local'
                    ? 'success'
                    : provider.tier === 'free'
                      ? 'informative'
                      : 'subtle'
              }
            >
              {provider.tier === 'paid'
                ? 'Paid'
                : provider.tier === 'local'
                  ? 'Local'
                  : provider.tier === 'free'
                    ? 'Free'
                    : 'Unknown'}
            </Badge>
          </div>
        ))}
      </div>
    </div>
  );
}

export interface ProviderStatusPanelProps {
  showRecommendations?: boolean;
  compact?: boolean;
}

export function ProviderStatusPanel({
  showRecommendations = true,
  compact = false,
}: ProviderStatusPanelProps) {
  const styles = useStyles();
  const { llmProviders, ttsProviders, imageProviders, isLoading, error, refresh, lastUpdated } =
    useProviderStatus();

  if (isLoading && llmProviders.length === 0) {
    return (
      <Card>
        <div className={styles.loadingContainer}>
          <Spinner size="small" />
          <Text>Loading provider status...</Text>
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <div className={styles.section}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
            <Text>Failed to load provider status</Text>
          </div>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            {error.message}
          </Text>
          <Button onClick={refresh} icon={<ArrowClockwise24Regular />}>
            Retry
          </Button>
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Title3>AI Provider Status</Title3>
        <Button
          appearance="subtle"
          icon={<ArrowClockwise24Regular />}
          onClick={refresh}
          disabled={isLoading}
        >
          Refresh
        </Button>
      </div>

      {isLoading && (
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
          <Spinner size="tiny" />
          <Text size={200}>Updating...</Text>
        </div>
      )}

      <ProviderSection title="Script Generation (LLM)" providers={llmProviders} />
      <ProviderSection title="Voice (TTS)" providers={ttsProviders} />
      <ProviderSection title="Images" providers={imageProviders} />

      {lastUpdated && (
        <Text className={styles.lastUpdated}>
          Last updated: {lastUpdated.toLocaleTimeString()}
        </Text>
      )}

      {showRecommendations && (
        <ProviderRecommendations
          llm={llmProviders}
          tts={ttsProviders}
          images={imageProviders}
        />
      )}
    </Card>
  );
}

