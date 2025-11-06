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
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Lightbulb24Regular,
  ArrowSync24Regular,
  Warning24Regular,
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
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  hardwareSummary: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  hardwareItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  hardwareLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  hardwareValue: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  recommendation: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  recommendationHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  recommendationTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  recommendationDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  detailRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    fontSize: tokens.fontSizeBase200,
  },
  detailLabel: {
    fontWeight: tokens.fontWeightSemibold,
    minWidth: '120px',
  },
  notesList: {
    marginTop: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  note: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  capabilityList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  capability: {
    fontSize: tokens.fontSizeBase300,
  },
  quickStartList: {
    paddingLeft: tokens.spacingHorizontalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  quickStartItem: {
    fontSize: tokens.fontSizeBase200,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalXL,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
    padding: tokens.spacingVerticalM,
  },
});

interface HardwareSummary {
  ramGB: number;
  vramGB: number;
  hasNvidiaGpu: boolean;
  logicalCores: number;
  tier: string;
}

interface ProviderRecommendation {
  primary: string;
  rationale: string;
  expectedSpeed: string;
  expectedQuality: string;
  setupComplexity: string;
  notes: string[];
}

interface MachineRecommendations {
  hardwareSummary: HardwareSummary;
  ttsRecommendation: ProviderRecommendation;
  ttsFallback: string;
  llmRecommendation: ProviderRecommendation;
  imageRecommendation: ProviderRecommendation;
  overallCapabilities: string[];
  quickStartSteps: string[];
}

export function OfflineModeRecommendations() {
  const styles = useStyles();
  const { showFailureToast } = useNotifications();
  const [loading, setLoading] = useState(true);
  const [recommendations, setRecommendations] = useState<MachineRecommendations | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadRecommendations = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${apiUrl}/offline-providers/recommendations`);

      if (!response.ok) {
        throw new Error(`Failed to load recommendations: ${response.statusText}`);
      }

      const data = await response.json();
      setRecommendations(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
      setError(errorMessage);
      showFailureToast({ title: 'Failed to load recommendations', message: errorMessage });
    } finally {
      setLoading(false);
    }
  }, [showFailureToast]);

  useEffect(() => {
    void loadRecommendations();
  }, [loadRecommendations]);

  const renderRecommendation = (title: string, recommendation: ProviderRecommendation) => (
    <div className={styles.recommendation}>
      <div className={styles.recommendationHeader}>
        <div className={styles.recommendationTitle}>{title}</div>
        <Badge appearance="tint" color="informative">
          {recommendation.primary}
        </Badge>
      </div>
      <div className={styles.recommendationDetails}>
        <div className={styles.detailRow}>
          <span className={styles.detailLabel}>Rationale:</span>
          <span>{recommendation.rationale}</span>
        </div>
        <div className={styles.detailRow}>
          <span className={styles.detailLabel}>Expected Speed:</span>
          <span>{recommendation.expectedSpeed}</span>
        </div>
        <div className={styles.detailRow}>
          <span className={styles.detailLabel}>Expected Quality:</span>
          <span>{recommendation.expectedQuality}</span>
        </div>
        <div className={styles.detailRow}>
          <span className={styles.detailLabel}>Setup:</span>
          <span>{recommendation.setupComplexity}</span>
        </div>
        {recommendation.notes.length > 0 && (
          <div className={styles.notesList}>
            {recommendation.notes.map((note, index) => (
              <div key={index} className={styles.note}>
                • {note}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );

  if (loading) {
    return (
      <Card className={styles.card}>
        <CardPreview className={styles.loading}>
          <Spinner size="small" />
          <Text>Analyzing your hardware and generating recommendations...</Text>
        </CardPreview>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={styles.card}>
        <CardHeader
          header={
            <div className={styles.header}>
              <Warning24Regular />
              <Title3>Error Loading Recommendations</Title3>
            </div>
          }
        />
        <CardPreview>
          <div className={styles.error}>
            <Text>{error}</Text>
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<ArrowSync24Regular />}
                onClick={() => void loadRecommendations()}
              >
                Retry
              </Button>
            </div>
          </div>
        </CardPreview>
      </Card>
    );
  }

  if (!recommendations) {
    return null;
  }

  return (
    <Card className={styles.card}>
      <CardHeader
        header={
          <div className={styles.header}>
            <Lightbulb24Regular />
            <Title3>Tune for My Machine</Title3>
          </div>
        }
        description="Hardware-optimized recommendations for offline video generation"
      />
      <CardPreview>
        <div className={styles.content}>
          {/* Hardware Summary */}
          <div className={styles.section}>
            <Text weight="semibold">Your Hardware</Text>
            <div className={styles.hardwareSummary}>
              <div className={styles.hardwareItem}>
                <div className={styles.hardwareLabel}>RAM</div>
                <div className={styles.hardwareValue}>
                  {recommendations.hardwareSummary.ramGB} GB
                </div>
              </div>
              <div className={styles.hardwareItem}>
                <div className={styles.hardwareLabel}>GPU VRAM</div>
                <div className={styles.hardwareValue}>
                  {recommendations.hardwareSummary.vramGB} GB
                </div>
              </div>
              <div className={styles.hardwareItem}>
                <div className={styles.hardwareLabel}>CPU Cores</div>
                <div className={styles.hardwareValue}>
                  {recommendations.hardwareSummary.logicalCores}
                </div>
              </div>
              <div className={styles.hardwareItem}>
                <div className={styles.hardwareLabel}>System Tier</div>
                <div className={styles.hardwareValue}>{recommendations.hardwareSummary.tier}</div>
              </div>
            </div>
          </div>

          {/* Overall Capabilities */}
          <div className={styles.section}>
            <Text weight="semibold">Your Offline Capabilities</Text>
            <div className={styles.capabilityList}>
              {recommendations.overallCapabilities.map((capability, index) => (
                <div key={index} className={styles.capability}>
                  {capability}
                </div>
              ))}
            </div>
          </div>

          {/* Detailed Recommendations */}
          <Accordion collapsible>
            <AccordionItem value="tts">
              <AccordionHeader icon={<Info24Regular />}>
                Text-to-Speech Recommendation
              </AccordionHeader>
              <AccordionPanel>
                {renderRecommendation(
                  'Recommended TTS Provider',
                  recommendations.ttsRecommendation
                )}
                <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
                  <strong>Fallback:</strong> {recommendations.ttsFallback}
                </Text>
              </AccordionPanel>
            </AccordionItem>

            <AccordionItem value="llm">
              <AccordionHeader icon={<Info24Regular />}>
                Script Generation Recommendation
              </AccordionHeader>
              <AccordionPanel>
                {renderRecommendation(
                  'Recommended LLM Provider',
                  recommendations.llmRecommendation
                )}
              </AccordionPanel>
            </AccordionItem>

            <AccordionItem value="images">
              <AccordionHeader icon={<Info24Regular />}>
                Image Generation Recommendation
              </AccordionHeader>
              <AccordionPanel>
                {renderRecommendation(
                  'Recommended Image Provider',
                  recommendations.imageRecommendation
                )}
              </AccordionPanel>
            </AccordionItem>
          </Accordion>

          {/* Quick Start Guide */}
          <div className={styles.section}>
            <Text weight="semibold">Quick Start Guide</Text>
            <ol className={styles.quickStartList}>
              {recommendations.quickStartSteps.map((step, index) => (
                <li key={index} className={styles.quickStartItem}>
                  {step}
                </li>
              ))}
            </ol>
          </div>

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={<ArrowSync24Regular />}
              onClick={() => void loadRecommendations()}
            >
              Refresh Recommendations
            </Button>
          </div>
        </div>
      </CardPreview>
    </Card>
  );
}
