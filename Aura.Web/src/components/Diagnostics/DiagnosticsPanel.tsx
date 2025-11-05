import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
  Badge,
  Body1,
  Caption1,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  ErrorCircle24Regular,
  Checkmark24Regular,
  DocumentBulletList24Regular,
  Warning24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  card: {
    padding: tokens.spacingVerticalM,
  },
  section: {
    marginBottom: tokens.spacingVerticalL,
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
  },
  rootCause: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
  },
  recommendationItem: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalS,
  },
  stepsContainer: {
    paddingLeft: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalS,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
  successMessage: {
    marginBottom: tokens.spacingVerticalM,
  },
  confidenceBar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  confidenceBarFill: {
    height: '8px',
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: tokens.borderRadiusMedium,
    transition: 'width 0.3s ease',
  },
  evidence: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'monospace',
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalXS,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalXS,
  },
  docLinks: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  docLink: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    textDecoration: 'none',
    color: tokens.colorBrandForeground1,
    ':hover': {
      textDecoration: 'underline',
    },
  },
});

interface RootCause {
  type: string;
  description: string;
  confidence: number;
  evidence: string[];
  stage?: string;
  provider?: string;
}

interface RecommendedAction {
  priority: number;
  title: string;
  description: string;
  steps: string[];
  canAutomate: boolean;
  estimatedMinutes?: number;
  type: string;
}

interface DocumentationLink {
  title: string;
  url: string;
  description: string;
}

interface FailureAnalysis {
  jobId: string;
  analyzedAt: string;
  summary: string;
  primaryRootCause: RootCause;
  secondaryRootCauses: RootCause[];
  recommendedActions: RecommendedAction[];
  documentationLinks: DocumentationLink[];
  confidenceScore: number;
}

interface DiagnosticsPanelProps {
  jobId: string;
  jobStatus?: string;
  errorMessage?: string;
  errorCode?: string;
  stage?: string;
}

export function DiagnosticsPanel({
  jobId,
  jobStatus,
  errorMessage,
  errorCode,
  stage,
}: DiagnosticsPanelProps) {
  const styles = useStyles();
  const [downloading, setDownloading] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);
  const [analysis, setAnalysis] = useState<FailureAnalysis | null>(null);
  const [bundleDownloaded, setBundleDownloaded] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDownloadDiagnostics = useCallback(async () => {
    setDownloading(true);
    setError(null);

    try {
      const response = await fetch(`/api/diagnostics/bundle/${jobId}`, {
        method: 'POST',
      });

      if (!response.ok) {
        throw new Error('Failed to generate diagnostic bundle');
      }

      const result = await response.json();
      
      const downloadResponse = await fetch(result.downloadUrl);
      if (!downloadResponse.ok) {
        throw new Error('Failed to download bundle');
      }

      const blob = await downloadResponse.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = result.fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      setBundleDownloaded(true);
    } catch (err) {
      console.error('Error downloading diagnostics:', err);
      setError(err instanceof Error ? err.message : 'Failed to download diagnostics');
    } finally {
      setDownloading(false);
    }
  }, [jobId]);

  const handleExplainFailure = useCallback(async () => {
    setAnalyzing(true);
    setError(null);

    try {
      const response = await fetch('/api/diagnostics/explain-failure', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          jobId,
          stage,
          errorMessage,
          errorCode,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to analyze failure');
      }

      const result: FailureAnalysis = await response.json();
      setAnalysis(result);
    } catch (err) {
      console.error('Error analyzing failure:', err);
      setError(err instanceof Error ? err.message : 'Failed to analyze failure');
    } finally {
      setAnalyzing(false);
    }
  }, [jobId, stage, errorMessage, errorCode]);

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'RateLimit':
        return <Warning24Regular />;
      case 'InvalidApiKey':
      case 'MissingApiKey':
        return <ErrorCircle24Regular />;
      case 'NetworkError':
        return <Warning24Regular />;
      default:
        return <ErrorCircle24Regular />;
    }
  };

  const getConfidenceColor = (confidence: number) => {
    if (confidence >= 90) return tokens.colorPaletteGreenBackground3;
    if (confidence >= 70) return tokens.colorPaletteYellowBackground3;
    return tokens.colorPaletteRedBackground3;
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>Diagnostics & Failure Analysis</Title3>
      </div>

      {error && (
        <MessageBar intent="error" className={styles.successMessage}>
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      {bundleDownloaded && (
        <MessageBar intent="success" className={styles.successMessage}>
          <MessageBarBody>
            <MessageBarTitle>Success</MessageBarTitle>
            Diagnostic bundle downloaded successfully!
          </MessageBarBody>
        </MessageBar>
      )}

      <Card className={styles.card}>
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <DocumentBulletList24Regular />
            <Text weight="semibold">Download Diagnostics Bundle</Text>
          </div>
          <Body1>
            Generate a comprehensive diagnostic report including system info, logs, timeline, and
            cost data.
          </Body1>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={handleDownloadDiagnostics}
            disabled={downloading}
            style={{ marginTop: tokens.spacingVerticalM }}
          >
            {downloading ? 'Generating...' : 'Download Diagnostics'}
          </Button>
          {bundleDownloaded && (
            <Text size={200} style={{ marginTop: tokens.spacingVerticalS, display: 'block' }}>
              Bundle includes anonymized logs, timeline, model decisions, FFmpeg commands, and cost
              report.
            </Text>
          )}
        </div>
      </Card>

      <Card className={styles.card}>
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <Info24Regular />
            <Text weight="semibold">Explain Failure</Text>
          </div>
          <Body1>Get AI-powered analysis of the failure with recommended next steps.</Body1>
          <Button
            appearance="primary"
            onClick={handleExplainFailure}
            disabled={analyzing}
            style={{ marginTop: tokens.spacingVerticalM }}
          >
            {analyzing ? 'Analyzing...' : 'Analyze Failure'}
          </Button>
        </div>

        {analyzing && (
          <div className={styles.loadingContainer}>
            <Spinner label="Analyzing failure..." />
          </div>
        )}

        {analysis && (
          <div style={{ marginTop: tokens.spacingVerticalL }}>
            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Summary
              </Text>
              <Body1 style={{ marginTop: tokens.spacingVerticalS }}>{analysis.summary}</Body1>
            </div>

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Primary Root Cause
              </Text>
              <div className={styles.rootCause}>
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  {getTypeIcon(analysis.primaryRootCause.type)}
                  <Text weight="semibold">{analysis.primaryRootCause.type}</Text>
                  <Badge appearance="filled" color="danger">
                    {analysis.primaryRootCause.confidence}% confidence
                  </Badge>
                </div>
                <Body1 style={{ marginTop: tokens.spacingVerticalS }}>
                  {analysis.primaryRootCause.description}
                </Body1>
                {analysis.primaryRootCause.evidence.length > 0 && (
                  <div className={styles.evidence}>{analysis.primaryRootCause.evidence[0]}</div>
                )}
                <div className={styles.confidenceBar}>
                  <Caption1>Confidence:</Caption1>
                  <div
                    style={{
                      flex: 1,
                      backgroundColor: tokens.colorNeutralBackground4,
                      borderRadius: tokens.borderRadiusMedium,
                    }}
                  >
                    <div
                      className={styles.confidenceBarFill}
                      style={{
                        width: `${analysis.primaryRootCause.confidence}%`,
                        backgroundColor: getConfidenceColor(analysis.primaryRootCause.confidence),
                      }}
                    />
                  </div>
                  <Caption1>{analysis.primaryRootCause.confidence}%</Caption1>
                </div>
              </div>
            </div>

            {analysis.secondaryRootCauses.length > 0 && (
              <div className={styles.section}>
                <Accordion collapsible>
                  <AccordionItem value="secondary">
                    <AccordionHeader>
                      <Text weight="semibold">Other Possible Causes ({analysis.secondaryRootCauses.length})</Text>
                    </AccordionHeader>
                    <AccordionPanel>
                      {analysis.secondaryRootCauses.map((cause, index) => (
                        <div key={index} style={{ marginBottom: tokens.spacingVerticalM }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                            {getTypeIcon(cause.type)}
                            <Text weight="semibold">{cause.type}</Text>
                            <Badge>{cause.confidence}%</Badge>
                          </div>
                          <Body1 size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                            {cause.description}
                          </Body1>
                        </div>
                      ))}
                    </AccordionPanel>
                  </AccordionItem>
                </Accordion>
              </div>
            )}

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Recommended Actions
              </Text>
              {analysis.recommendedActions.map((action, index) => (
                <div key={index} className={styles.recommendationItem}>
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                      <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                      <Text weight="semibold">{action.title}</Text>
                      <Badge appearance="tint" color="informative">
                        Priority {action.priority}
                      </Badge>
                      {action.estimatedMinutes && (
                        <Caption1>{action.estimatedMinutes} min</Caption1>
                      )}
                    </div>
                  </div>
                  <Body1 style={{ marginTop: tokens.spacingVerticalS }}>{action.description}</Body1>
                  {action.steps.length > 0 && (
                    <div className={styles.stepsContainer}>
                      <Text weight="semibold" size={200}>
                        Steps:
                      </Text>
                      <ol style={{ marginTop: tokens.spacingVerticalXS }}>
                        {action.steps.map((step, stepIndex) => (
                          <li key={stepIndex}>
                            <Caption1>{step}</Caption1>
                          </li>
                        ))}
                      </ol>
                    </div>
                  )}
                </div>
              ))}
            </div>

            {analysis.documentationLinks.length > 0 && (
              <div className={styles.section}>
                <Text weight="semibold" size={400}>
                  Documentation & Resources
                </Text>
                <div className={styles.docLinks}>
                  {analysis.documentationLinks.map((link, index) => (
                    <a
                      key={index}
                      href={link.url}
                      className={styles.docLink}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      <DocumentBulletList24Regular />
                      <div>
                        <Text>{link.title}</Text>
                        {link.description && (
                          <Caption1 block>{link.description}</Caption1>
                        )}
                      </div>
                    </a>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </Card>
    </div>
  );
}
