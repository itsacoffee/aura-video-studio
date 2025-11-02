import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Tab,
  TabList,
  Field,
  Input,
  Textarea,
} from '@fluentui/react-components';
import {
  ShieldCheckmark24Regular,
  Flash24Regular,
  DocumentCheckmark24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { ErrorState } from '../../components/Loading';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  toolHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  toolIcon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '600px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  statCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
  statusBadge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontWeight: tokens.fontWeightSemibold,
  },
  statusVerified: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground1,
  },
  statusWarning: {
    backgroundColor: tokens.colorPaletteYellowBackground2,
    color: tokens.colorPaletteYellowForeground1,
  },
  statusFailed: {
    backgroundColor: tokens.colorPaletteRedBackground2,
    color: tokens.colorPaletteRedForeground1,
  },
});

type TabValue = 'verify' | 'quick';

interface VerificationResult {
  contentId: string;
  overallStatus: string;
  overallConfidence: number;
  claimCount: number;
  factCheckCount: number;
  sourceCount: number;
  warnings: string[];
  misinformationRisk?: string;
  verifiedAt: string;
}

interface QuickVerifyResult {
  status: string;
  confidence: number;
  warnings: string[];
}

const VerificationPage: React.FC = () => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<TabValue>('verify');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [content, setContent] = useState('');
  const [contentId, setContentId] = useState('');
  const [verificationResult, setVerificationResult] = useState<VerificationResult | null>(null);

  const [quickContent, setQuickContent] = useState('');
  const [quickResult, setQuickResult] = useState<QuickVerifyResult | null>(null);

  const handleVerify = useCallback(async () => {
    setLoading(true);
    setError(null);
    setVerificationResult(null);

    try {
      const response = await fetch('/api/verification/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content,
          contentId: contentId || undefined,
          options: {},
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Verification failed');
      }

      const data = await response.json();
      setVerificationResult(data.result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [content, contentId]);

  const handleQuickVerify = useCallback(async () => {
    setLoading(true);
    setError(null);
    setQuickResult(null);

    try {
      const response = await fetch('/api/verification/quick-verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: quickContent }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Quick verification failed');
      }

      const data = await response.json();
      setQuickResult(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [quickContent]);

  const getStatusBadgeClass = (status: string) => {
    const lowerStatus = status.toLowerCase();
    if (lowerStatus.includes('verified') || lowerStatus.includes('pass')) {
      return `${styles.statusBadge} ${styles.statusVerified}`;
    }
    if (lowerStatus.includes('warning') || lowerStatus.includes('uncertain')) {
      return `${styles.statusBadge} ${styles.statusWarning}`;
    }
    return `${styles.statusBadge} ${styles.statusFailed}`;
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <ShieldCheckmark24Regular className={styles.headerIcon} />
        <div>
          <Title1>Content Verification</Title1>
          <Text className={styles.subtitle}>
            Fact-checking and content verification to ensure accuracy and credibility
          </Text>
        </div>
      </div>

      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as TabValue)}
        className={styles.tabs}
      >
        <Tab value="verify" icon={<DocumentCheckmark24Regular />}>
          Full Verification
        </Tab>
        <Tab value="quick" icon={<Flash24Regular />}>
          Quick Verify
        </Tab>
      </TabList>

      <div className={styles.content}>
        {error && <ErrorState message={error} />}

        {selectedTab === 'verify' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <DocumentCheckmark24Regular className={styles.toolIcon} />
              <Title2>Full Content Verification</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Content ID (optional)">
                <Input
                  value={contentId}
                  onChange={(_, data) => setContentId(data.value)}
                  placeholder="content-123"
                />
              </Field>
              <Field label="Content to Verify" required>
                <Textarea
                  value={content}
                  onChange={(_, data) => setContent(data.value)}
                  placeholder="Enter the content you want to verify for factual accuracy..."
                  style={{ minHeight: '150px' }}
                />
              </Field>
              <div className={styles.actions}>
                <Button appearance="primary" onClick={handleVerify} disabled={loading || !content}>
                  {loading ? <Spinner size="tiny" /> : 'Verify Content'}
                </Button>
              </div>
            </div>
            {verificationResult && (
              <div className={styles.resultsSection}>
                <Title3>Verification Results</Title3>
                <div className={styles.statsGrid}>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Status</Text>
                    <div className={getStatusBadgeClass(verificationResult.overallStatus)}>
                      {verificationResult.overallStatus}
                    </div>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Confidence</Text>
                    <Text size={500}>
                      {(verificationResult.overallConfidence * 100).toFixed(1)}%
                    </Text>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Claims</Text>
                    <Text size={500}>{verificationResult.claimCount}</Text>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Fact Checks</Text>
                    <Text size={500}>{verificationResult.factCheckCount}</Text>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Sources</Text>
                    <Text size={500}>{verificationResult.sourceCount}</Text>
                  </div>
                  {verificationResult.misinformationRisk && (
                    <div className={styles.statCard}>
                      <Text weight="semibold">Risk Level</Text>
                      <div className={getStatusBadgeClass(verificationResult.misinformationRisk)}>
                        {verificationResult.misinformationRisk}
                      </div>
                    </div>
                  )}
                </div>
                {verificationResult.warnings.length > 0 && (
                  <div style={{ marginTop: tokens.spacingVerticalM }}>
                    <Text weight="semibold">Warnings:</Text>
                    {verificationResult.warnings.map((warning, i) => (
                      <Text key={i}>• {warning}</Text>
                    ))}
                  </div>
                )}
                <Text style={{ marginTop: tokens.spacingVerticalM }}>
                  Content ID: {verificationResult.contentId}
                </Text>
              </div>
            )}
          </Card>
        )}

        {selectedTab === 'quick' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Flash24Regular className={styles.toolIcon} />
              <Title2>Quick Verification</Title2>
            </div>
            <Text style={{ marginBottom: tokens.spacingVerticalM }}>
              Get rapid feedback on content accuracy for real-time editing
            </Text>
            <div className={styles.form}>
              <Field label="Content to Verify" required>
                <Textarea
                  value={quickContent}
                  onChange={(_, data) => setQuickContent(data.value)}
                  placeholder="Enter a claim or statement to quickly verify..."
                  style={{ minHeight: '120px' }}
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleQuickVerify}
                  disabled={loading || !quickContent}
                >
                  {loading ? <Spinner size="tiny" /> : 'Quick Verify'}
                </Button>
              </div>
            </div>
            {quickResult && (
              <div className={styles.resultsSection}>
                <Title3>Quick Verification Results</Title3>
                <div
                  style={{
                    display: 'flex',
                    gap: tokens.spacingHorizontalM,
                    marginTop: tokens.spacingVerticalM,
                  }}
                >
                  <div>
                    <Text weight="semibold">Status:</Text>
                    <div
                      className={getStatusBadgeClass(quickResult.status)}
                      style={{ marginTop: tokens.spacingVerticalXS }}
                    >
                      {quickResult.status}
                    </div>
                  </div>
                  <div>
                    <Text weight="semibold">Confidence: </Text>
                    <Text size={500}>{(quickResult.confidence * 100).toFixed(1)}%</Text>
                  </div>
                </div>
                {quickResult.warnings.length > 0 && (
                  <div style={{ marginTop: tokens.spacingVerticalM }}>
                    <Text weight="semibold">Warnings:</Text>
                    {quickResult.warnings.map((warning, i) => (
                      <Text key={i}>• {warning}</Text>
                    ))}
                  </div>
                )}
              </div>
            )}
          </Card>
        )}
      </div>
    </div>
  );
};

export default VerificationPage;
