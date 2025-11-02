import {
  makeStyles,
  tokens,
  Text,
  Card,
  Badge,
  Button,
  Textarea,
  Spinner,
} from '@fluentui/react-components';
import {
  Shield24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { useContentSafetyStore } from '../../state/contentSafety';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  scoreContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  scoreCircle: {
    width: '120px',
    height: '120px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '36px',
    fontWeight: 'bold',
  },
  violationsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  violationItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder1}`,
  },
  warningItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder1}`,
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
});

export const SafetyAnalysisPreview = () => {
  const styles = useStyles();
  const { analyzeContent, isLoading, lastAnalysisResult } = useContentSafetyStore();
  const [testContent, setTestContent] = useState('');

  const handleAnalyze = async () => {
    if (!testContent.trim()) return;
    await analyzeContent(testContent);
  };

  const getScoreColor = (score: number) => {
    if (score >= 80) return tokens.colorPaletteGreenBackground3;
    if (score >= 60) return tokens.colorPaletteYellowBackground3;
    return tokens.colorPaletteRedBackground3;
  };

  const getScoreForeground = (score: number) => {
    if (score >= 80) return tokens.colorPaletteGreenForeground1;
    if (score >= 60) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.header}>
          <Shield24Regular />
          <Text weight="semibold">Test Content Safety</Text>
        </div>

        <Text size={200} style={{ marginBottom: tokens.spacingVerticalM }}>
          Enter sample content to test against your current safety policy
        </Text>

        <Textarea
          value={testContent}
          onChange={(_, data) => setTestContent(data.value)}
          placeholder="Enter script, description, or content to analyze..."
          rows={6}
          style={{ marginBottom: tokens.spacingVerticalM }}
        />

        <div className={styles.actions}>
          <Button
            appearance="secondary"
            onClick={() => {
              setTestContent('');
            }}
            disabled={!testContent}
          >
            Clear
          </Button>
          <Button
            appearance="primary"
            onClick={handleAnalyze}
            disabled={!testContent.trim() || isLoading}
          >
            {isLoading ? <Spinner size="tiny" /> : 'Analyze Content'}
          </Button>
        </div>
      </Card>

      {lastAnalysisResult && (
        <>
          <Card className={styles.card}>
            <div className={styles.header}>
              {lastAnalysisResult.isSafe ? (
                <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
              ) : (
                <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
              )}
              <Text weight="semibold">
                {lastAnalysisResult.isSafe ? 'Content Passed' : 'Content Flagged'}
              </Text>
              <Badge appearance="filled" color={lastAnalysisResult.isSafe ? 'success' : 'danger'}>
                {lastAnalysisResult.isSafe ? 'Safe' : 'Unsafe'}
              </Badge>
            </div>

            <div className={styles.scoreContainer}>
              <Text size={200}>Overall Safety Score</Text>
              <div
                className={styles.scoreCircle}
                style={{
                  backgroundColor: getScoreColor(lastAnalysisResult.overallSafetyScore),
                  color: getScoreForeground(lastAnalysisResult.overallSafetyScore),
                }}
              >
                {lastAnalysisResult.overallSafetyScore}
              </div>
              <Text size={200}>
                {lastAnalysisResult.overallSafetyScore >= 80 && 'Excellent'}
                {lastAnalysisResult.overallSafetyScore >= 60 &&
                  lastAnalysisResult.overallSafetyScore < 80 &&
                  'Good'}
                {lastAnalysisResult.overallSafetyScore >= 40 &&
                  lastAnalysisResult.overallSafetyScore < 60 &&
                  'Fair'}
                {lastAnalysisResult.overallSafetyScore < 40 && 'Poor'}
              </Text>
            </div>

            {lastAnalysisResult.requiresReview && (
              <div
                style={{
                  padding: tokens.spacingVerticalM,
                  backgroundColor: tokens.colorPaletteYellowBackground2,
                  borderRadius: tokens.borderRadiusMedium,
                  marginTop: tokens.spacingVerticalM,
                }}
              >
                <Text style={{ color: tokens.colorPaletteYellowForeground1 }}>
                  ⚠️ This content requires manual review before proceeding
                </Text>
              </div>
            )}

            {lastAnalysisResult.allowWithDisclaimer && (
              <div
                style={{
                  padding: tokens.spacingVerticalM,
                  backgroundColor: tokens.colorNeutralBackground2,
                  borderRadius: tokens.borderRadiusMedium,
                  marginTop: tokens.spacingVerticalM,
                }}
              >
                <Text
                  weight="semibold"
                  style={{
                    display: 'block',
                    marginBottom: tokens.spacingVerticalXS,
                  }}
                >
                  Recommended Disclaimer:
                </Text>
                <Text size={200}>{lastAnalysisResult.recommendedDisclaimer}</Text>
              </div>
            )}
          </Card>

          {lastAnalysisResult.violations.length > 0 && (
            <Card className={styles.card}>
              <div className={styles.header}>
                <Dismiss24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
                <Text weight="semibold">Violations ({lastAnalysisResult.violations.length})</Text>
              </div>

              <div className={styles.violationsList}>
                {lastAnalysisResult.violations.map((violation) => (
                  <div key={violation.id} className={styles.violationItem}>
                    <div className={styles.row}>
                      <Text weight="semibold">{violation.category}</Text>
                      <Badge appearance="filled" color="danger">
                        Severity {violation.severityScore}
                      </Badge>
                    </div>
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                      {violation.reason}
                    </Text>
                    {violation.matchedContent && (
                      <Text
                        size={200}
                        style={{
                          marginTop: tokens.spacingVerticalXS,
                          fontStyle: 'italic',
                          color: tokens.colorNeutralForeground3,
                        }}
                      >
                        Matched: &quot;{violation.matchedContent}&quot;
                      </Text>
                    )}
                    {violation.suggestedFix && (
                      <Text
                        size={200}
                        style={{
                          marginTop: tokens.spacingVerticalXS,
                          color: tokens.colorPaletteGreenForeground1,
                        }}
                      >
                        Suggested fix: {violation.suggestedFix}
                      </Text>
                    )}
                    <div className={styles.row}>
                      <Badge appearance="outline" color="warning">
                        {violation.recommendedAction}
                      </Badge>
                      {violation.canOverride && (
                        <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
                          Can override
                        </Text>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {lastAnalysisResult.warnings.length > 0 && (
            <Card className={styles.card}>
              <div className={styles.header}>
                <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                <Text weight="semibold">Warnings ({lastAnalysisResult.warnings.length})</Text>
              </div>

              <div className={styles.violationsList}>
                {lastAnalysisResult.warnings.map((warning) => (
                  <div key={warning.id} className={styles.warningItem}>
                    <div className={styles.row}>
                      <Text weight="semibold">{warning.category}</Text>
                    </div>
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                      {warning.message}
                    </Text>
                    {warning.suggestions.length > 0 && (
                      <div style={{ marginTop: tokens.spacingVerticalS }}>
                        <Text size={200} weight="semibold">
                          Suggestions:
                        </Text>
                        <ul
                          style={{
                            marginTop: tokens.spacingVerticalXXS,
                            paddingLeft: '20px',
                          }}
                        >
                          {warning.suggestions.map((suggestion, idx) => (
                            <li key={idx}>
                              <Text size={200}>{suggestion}</Text>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </Card>
          )}

          {lastAnalysisResult.suggestedFixes.length > 0 && (
            <Card
              className={styles.card}
              style={{ backgroundColor: tokens.colorNeutralBackground2 }}
            >
              <Text
                weight="semibold"
                style={{
                  display: 'block',
                  marginBottom: tokens.spacingVerticalS,
                }}
              >
                Suggested Fixes:
              </Text>
              <ul style={{ paddingLeft: '20px' }}>
                {lastAnalysisResult.suggestedFixes.map((fix, idx) => (
                  <li key={idx}>
                    <Text size={200}>{fix}</Text>
                  </li>
                ))}
              </ul>
            </Card>
          )}
        </>
      )}
    </div>
  );
};
