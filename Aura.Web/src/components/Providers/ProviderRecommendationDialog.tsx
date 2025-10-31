import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Spinner,
  Label,
  Text,
  Badge,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  Checkmark20Regular,
  Warning20Regular,
  Dismiss20Regular,
  Info20Regular,
} from '@fluentui/react-icons';
import React, { useEffect, useState } from 'react';
import {
  providerRecommendationService,
  type ProviderRecommendation,
  type LlmOperationType,
} from '../../services/providers/providerRecommendationService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  recommendation: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    transitionProperty: 'all',
    transitionDuration: '0.2s',
  },
  selected: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `2px solid ${tokens.colorBrandStroke1}`,
  },
  recommendationHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  providerName: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  metrics: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  metric: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  reasoning: {
    marginTop: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  healthBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  loading: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

interface ProviderRecommendationDialogProps {
  open: boolean;
  operationType: LlmOperationType;
  onSelect: (providerName: string) => void;
  onCancel: () => void;
}

export const ProviderRecommendationDialog: React.FC<ProviderRecommendationDialogProps> = ({
  open,
  operationType,
  onSelect,
  onCancel,
}) => {
  const styles = useStyles();
  const [recommendations, setRecommendations] = useState<ProviderRecommendation[]>([]);
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [recommendationsEnabled, setRecommendationsEnabled] = useState(false);

  useEffect(() => {
    if (open) {
      loadRecommendations();
    }
  }, [open, operationType]);

  const loadRecommendations = async () => {
    setLoading(true);
    try {
      // Check if recommendations are enabled
      const prefs = await providerRecommendationService.getPreferences();
      setRecommendationsEnabled(prefs.enableRecommendations && prefs.assistanceLevel !== 'Off');

      // Only load recommendations if enabled
      if (prefs.enableRecommendations && prefs.assistanceLevel !== 'Off') {
        const recs = await providerRecommendationService.getRecommendations(operationType);
        setRecommendations(recs);
        if (recs.length > 0) {
          setSelectedProvider(recs[0].providerName);
        }
      }
    } catch (error: unknown) {
      console.error('Failed to load recommendations:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = () => {
    if (selectedProvider) {
      onSelect(selectedProvider);
    }
  };

  const getHealthIcon = (status: string) => {
    switch (status) {
      case 'Healthy':
        return <Checkmark20Regular />;
      case 'Degraded':
        return <Warning20Regular />;
      case 'Unhealthy':
        return <Dismiss20Regular />;
      default:
        return <Info20Regular />;
    }
  };

  const getHealthColor = (status: string): 'success' | 'warning' | 'danger' | 'informative' => {
    switch (status) {
      case 'Healthy':
        return 'success';
      case 'Degraded':
        return 'warning';
      case 'Unhealthy':
        return 'danger';
      default:
        return 'informative';
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onCancel()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Select Provider for {operationType}</DialogTitle>
          <DialogContent>
            {loading ? (
              <div className={styles.loading}>
                <Spinner label="Loading..." />
              </div>
            ) : !recommendationsEnabled ? (
              <div className={styles.container}>
                <Text>Provider recommendations are disabled. Choose a provider manually.</Text>
                <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
                  To enable recommendations, go to Settings → Recommendations and turn on
                  &quot;Enable Provider Recommendations&quot;.
                </Text>
                <div style={{ marginTop: tokens.spacingVerticalL }}>
                  {['OpenAI', 'Claude', 'Gemini', 'Ollama', 'RuleBased'].map((provider) => (
                    <div
                      key={provider}
                      className={`${styles.recommendation} ${
                        selectedProvider === provider ? styles.selected : ''
                      }`}
                      onClick={() => setSelectedProvider(provider)}
                      role="button"
                      tabIndex={0}
                      onKeyPress={(e) => {
                        if (e.key === 'Enter') {
                          setSelectedProvider(provider);
                        }
                      }}
                    >
                      <span className={styles.providerName}>{provider}</span>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <div className={styles.container}>
                <Text>
                  Choose a provider for this operation. The recommendations are ranked by quality,
                  cost, and performance.
                </Text>
                {recommendations.length === 0 ? (
                  <Text size={200} style={{ marginTop: tokens.spacingVerticalM }}>
                    No recommendations available. Please check your provider configuration in
                    Settings.
                  </Text>
                ) : (
                  recommendations.map((rec) => (
                    <div
                      key={rec.providerName}
                      className={`${styles.recommendation} ${
                        selectedProvider === rec.providerName ? styles.selected : ''
                      }`}
                      onClick={() => setSelectedProvider(rec.providerName)}
                      role="button"
                      tabIndex={0}
                      onKeyPress={(e) => {
                        if (e.key === 'Enter') {
                          setSelectedProvider(rec.providerName);
                        }
                      }}
                    >
                      <div className={styles.recommendationHeader}>
                        <span className={styles.providerName}>{rec.providerName}</span>
                        <div className={styles.healthBadge}>
                          <Badge
                            appearance="outline"
                            color={getHealthColor(rec.healthStatus)}
                            icon={getHealthIcon(rec.healthStatus)}
                          >
                            {rec.healthStatus}
                          </Badge>
                          {!rec.isAvailable && <Badge color="danger">Not Available</Badge>}
                        </div>
                      </div>

                      <div className={styles.metrics}>
                        <div className={styles.metric}>
                          <Label size="small">Quality:</Label>
                          <Text size={200}>{rec.qualityScore}/100</Text>
                        </div>
                        <div className={styles.metric}>
                          <Label size="small">Cost:</Label>
                          <Text size={200}>
                            {providerRecommendationService.formatCost(rec.estimatedCost)}
                          </Text>
                        </div>
                        <div className={styles.metric}>
                          <Label size="small">Latency:</Label>
                          <Text size={200}>
                            {providerRecommendationService.formatLatency(
                              rec.expectedLatencySeconds
                            )}
                          </Text>
                        </div>
                        <div className={styles.metric}>
                          <Label size="small">Confidence:</Label>
                          <Text size={200}>{rec.confidence}%</Text>
                        </div>
                      </div>

                      <div className={styles.reasoning}>
                        <Text size={200}>{rec.reasoning}</Text>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onCancel}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={handleSelect}
              disabled={!selectedProvider || loading}
            >
              Use {selectedProvider || 'Provider'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
