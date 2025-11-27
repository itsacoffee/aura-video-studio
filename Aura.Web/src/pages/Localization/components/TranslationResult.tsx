/**
 * Translation Result Component
 * Displays translation results with side-by-side comparison, quality metrics,
 * cultural adaptations, timing adjustments, and visual recommendations
 */

import {
  Card,
  Text,
  Title2,
  Title3,
  makeStyles,
  tokens,
  Badge,
  ProgressBar,
  Divider,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import {
  Warning24Regular,
  ErrorCircle24Regular,
  Info24Regular,
  Copy24Regular,
  Info16Regular,
} from '@fluentui/react-icons';
import type { TranslationResultDto } from '../../../types/api-v1';

const useStyles = makeStyles({
  resultsContainer: {
    marginTop: tokens.spacingVerticalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  comparisonGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalM,
  },
  textPanel: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    minHeight: '200px',
  },
  textContent: {
    fontFamily: 'monospace',
    fontSize: '14px',
    whiteSpace: 'pre-wrap',
    lineHeight: '1.6',
  },
  qualityGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalM,
  },
  qualityCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  scoreValue: {
    fontSize: '32px',
    fontWeight: tokens.fontWeightBold,
  },
  adaptationsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  adaptationItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorBrandForeground1}`,
  },
  adaptationPhrase: {
    fontWeight: tokens.fontWeightSemibold,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalXS,
  },
  adaptationReason: {
    fontSize: '13px',
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
  warningsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  warningItem: {
    padding: tokens.spacingVerticalS,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  statsBar: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    flexWrap: 'wrap',
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  statLabel: {
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  statValue: {
    fontSize: '18px',
    fontWeight: tokens.fontWeightSemibold,
  },
  visualRecommendations: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  recommendationItem: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  issuesList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  issueItem: {
    padding: tokens.spacingVerticalS,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  copyButton: {
    marginTop: tokens.spacingVerticalS,
  },
  qualityIndicator: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  metricsCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalS,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
});

interface TranslationResultProps {
  result: TranslationResultDto;
}

export const TranslationResult: React.FC<TranslationResultProps> = ({ result }) => {
  const styles = useStyles();

  const getScoreColor = (score: number): string => {
    if (score >= 85) return tokens.colorPaletteGreenForeground1;
    if (score >= 70) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const getGradeBadgeColor = (grade: string): 'success' | 'informative' | 'warning' | 'danger' => {
    switch (grade) {
      case 'Excellent':
        return 'success';
      case 'Good':
        return 'informative';
      case 'Fair':
        return 'warning';
      case 'Poor':
        return 'danger';
      default:
        return 'informative';
    }
  };

  const getGradeBadgeAppearance = (grade: string): 'filled' | 'outline' | 'tint' => {
    if (grade === 'Excellent') return 'filled';
    if (grade === 'Good') return 'outline';
    return 'tint';
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
      case 'error':
        return <ErrorCircle24Regular color={tokens.colorPaletteRedForeground1} />;
      case 'warning':
        return <Warning24Regular color={tokens.colorPaletteYellowForeground1} />;
      default:
        return <Info24Regular color={tokens.colorBrandForeground1} />;
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  return (
    <div className={styles.resultsContainer}>
      {/* Stats Bar */}
      <div className={styles.statsBar}>
        <div className={styles.statItem}>
          <Text className={styles.statLabel}>Overall Quality</Text>
          <Text
            className={styles.statValue}
            style={{ color: getScoreColor(result.quality.overallScore) }}
          >
            {result.quality.overallScore.toFixed(1)}%
          </Text>
        </div>
        <div className={styles.statItem}>
          <Text className={styles.statLabel}>Translation Time</Text>
          <Text className={styles.statValue}>{result.translationTimeSeconds.toFixed(1)}s</Text>
        </div>
        <div className={styles.statItem}>
          <Text className={styles.statLabel}>Languages</Text>
          <Text className={styles.statValue}>
            {result.sourceLanguage} → {result.targetLanguage}
          </Text>
        </div>
        <div className={styles.statItem}>
          <Text className={styles.statLabel}>Cultural Adaptations</Text>
          <Text className={styles.statValue}>{result.culturalAdaptations.length}</Text>
        </div>
        {/* Quality Metrics Indicator */}
        {result.metrics && (
          <div className={styles.statItem}>
            <Text className={styles.statLabel}>Output Quality</Text>
            <div className={styles.qualityIndicator}>
              <Badge
                appearance={getGradeBadgeAppearance(result.metrics.grade)}
                color={getGradeBadgeColor(result.metrics.grade)}
              >
                {result.metrics.grade}
              </Badge>
              {result.metrics.qualityIssues.length > 0 && (
                <Tooltip content={result.metrics.qualityIssues.join(', ')} relationship="label">
                  <Info16Regular />
                </Tooltip>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Translation Metrics Card */}
      {result.metrics && (
        <Card>
          <Title2>Translation Metrics</Title2>
          <Text style={{ color: tokens.colorNeutralForeground3 }}>
            Quality analysis of the LLM translation output
          </Text>
          <div className={styles.metricsCard}>
            <div className={styles.metricsGrid}>
              <div className={styles.metricItem}>
                <Text className={styles.statLabel}>Provider</Text>
                <Text weight="semibold">{result.metrics.providerUsed || 'Unknown'}</Text>
              </div>
              <div className={styles.metricItem}>
                <Text className={styles.statLabel}>Length Ratio</Text>
                <Text weight="semibold">{result.metrics.lengthRatio.toFixed(2)}x</Text>
              </div>
              <div className={styles.metricItem}>
                <Text className={styles.statLabel}>Character Count</Text>
                <Text weight="semibold">{result.metrics.characterCount.toLocaleString()}</Text>
              </div>
              <div className={styles.metricItem}>
                <Text className={styles.statLabel}>Word Count</Text>
                <Text weight="semibold">{result.metrics.wordCount.toLocaleString()}</Text>
              </div>
              <div className={styles.metricItem}>
                <Text className={styles.statLabel}>Artifacts Detected</Text>
                <Badge
                  appearance="tint"
                  color={result.metrics.hasStructuredArtifacts ? 'warning' : 'success'}
                >
                  {result.metrics.hasStructuredArtifacts ? 'Yes' : 'No'}
                </Badge>
              </div>
              <div className={styles.metricItem}>
                <Text className={styles.statLabel}>Prefixes Cleaned</Text>
                <Badge
                  appearance="tint"
                  color={result.metrics.hasUnwantedPrefixes ? 'warning' : 'success'}
                >
                  {result.metrics.hasUnwantedPrefixes ? 'Yes' : 'No'}
                </Badge>
              </div>
            </div>
            {result.metrics.qualityIssues.length > 0 && (
              <>
                <Divider
                  style={{
                    marginTop: tokens.spacingVerticalM,
                    marginBottom: tokens.spacingVerticalM,
                  }}
                />
                <Title3>Quality Issues Detected</Title3>
                <div className={styles.issuesList}>
                  {result.metrics.qualityIssues.map((issue, idx) => (
                    <div key={idx} className={styles.issueItem}>
                      <Warning24Regular color={tokens.colorPaletteYellowForeground1} />
                      <Text>{issue}</Text>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </Card>
      )}

      {/* Side-by-Side Comparison */}
      <Card>
        <Title2>Translation Comparison</Title2>
        <div className={styles.comparisonGrid}>
          <div>
            <Title3>Source Text ({result.sourceLanguage})</Title3>
            <div className={styles.textPanel}>
              <div className={styles.textContent}>{result.sourceText}</div>
            </div>
            <Button
              className={styles.copyButton}
              icon={<Copy24Regular />}
              size="small"
              onClick={() => copyToClipboard(result.sourceText)}
            >
              Copy Source
            </Button>
          </div>
          <div>
            <Title3>Translated Text ({result.targetLanguage})</Title3>
            <div className={styles.textPanel}>
              <div className={styles.textContent}>{result.translatedText}</div>
            </div>
            <Button
              className={styles.copyButton}
              icon={<Copy24Regular />}
              size="small"
              onClick={() => copyToClipboard(result.translatedText)}
            >
              Copy Translation
            </Button>
          </div>
        </div>
      </Card>

      {/* Quality Metrics */}
      <Card>
        <Title2>Quality Metrics</Title2>
        <div className={styles.qualityGrid}>
          <div className={styles.qualityCard}>
            <Text>Fluency</Text>
            <ProgressBar value={result.quality.fluencyScore / 100} />
            <Text
              className={styles.scoreValue}
              style={{ color: getScoreColor(result.quality.fluencyScore) }}
            >
              {result.quality.fluencyScore.toFixed(1)}%
            </Text>
          </div>
          <div className={styles.qualityCard}>
            <Text>Accuracy</Text>
            <ProgressBar value={result.quality.accuracyScore / 100} />
            <Text
              className={styles.scoreValue}
              style={{ color: getScoreColor(result.quality.accuracyScore) }}
            >
              {result.quality.accuracyScore.toFixed(1)}%
            </Text>
          </div>
          <div className={styles.qualityCard}>
            <Text>Cultural Appropriateness</Text>
            <ProgressBar value={result.quality.culturalAppropriatenessScore / 100} />
            <Text
              className={styles.scoreValue}
              style={{ color: getScoreColor(result.quality.culturalAppropriatenessScore) }}
            >
              {result.quality.culturalAppropriatenessScore.toFixed(1)}%
            </Text>
          </div>
          <div className={styles.qualityCard}>
            <Text>Terminology Consistency</Text>
            <ProgressBar value={result.quality.terminologyConsistencyScore / 100} />
            <Text
              className={styles.scoreValue}
              style={{ color: getScoreColor(result.quality.terminologyConsistencyScore) }}
            >
              {result.quality.terminologyConsistencyScore.toFixed(1)}%
            </Text>
          </div>
        </div>

        {result.quality.backTranslatedText && (
          <>
            <Divider
              style={{ marginTop: tokens.spacingVerticalL, marginBottom: tokens.spacingVerticalM }}
            />
            <Title3>Back-Translation Verification</Title3>
            <Text style={{ fontSize: '13px', color: tokens.colorNeutralForeground3 }}>
              Score: {result.quality.backTranslationScore.toFixed(1)}% - Translation was converted
              back to source language to verify accuracy
            </Text>
            <div className={styles.textPanel} style={{ marginTop: tokens.spacingVerticalM }}>
              <div className={styles.textContent}>{result.quality.backTranslatedText}</div>
            </div>
          </>
        )}

        {result.quality.issues.length > 0 && (
          <>
            <Divider
              style={{ marginTop: tokens.spacingVerticalL, marginBottom: tokens.spacingVerticalM }}
            />
            <Title3>Quality Issues</Title3>
            <div className={styles.issuesList}>
              {result.quality.issues.map((issue, idx) => (
                <div key={idx} className={styles.issueItem}>
                  {getSeverityIcon(issue.severity)}
                  <div style={{ flex: 1 }}>
                    <Text weight="semibold">{issue.category}</Text>
                    <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                      {issue.description}
                    </Text>
                    {issue.suggestion && (
                      <Text
                        style={{
                          display: 'block',
                          marginTop: tokens.spacingVerticalXS,
                          fontSize: '13px',
                          color: tokens.colorNeutralForeground3,
                        }}
                      >
                        Suggestion: {issue.suggestion}
                      </Text>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </Card>

      {/* Cultural Adaptations */}
      {result.culturalAdaptations.length > 0 && (
        <Card>
          <Title2>Cultural Adaptations ({result.culturalAdaptations.length})</Title2>
          <Text style={{ color: tokens.colorNeutralForeground3 }}>
            Content adapted for cultural relevance and appropriateness
          </Text>
          <div className={styles.adaptationsList}>
            {result.culturalAdaptations.map((adaptation, idx) => (
              <div key={idx} className={styles.adaptationItem}>
                <Badge appearance="tint" color="brand">
                  {adaptation.category}
                </Badge>
                <div className={styles.adaptationPhrase}>
                  <Text weight="semibold">&quot;{adaptation.sourcePhrase}&quot;</Text>
                  <Text>→</Text>
                  <Text weight="semibold">&quot;{adaptation.adaptedPhrase}&quot;</Text>
                </div>
                <Text className={styles.adaptationReason}>{adaptation.reasoning}</Text>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Timing Adjustments */}
      {result.timingAdjustment.warnings.length > 0 && (
        <Card>
          <Title2>Timing Adjustments</Title2>
          <div className={styles.statsBar} style={{ marginTop: tokens.spacingVerticalM }}>
            <div className={styles.statItem}>
              <Text className={styles.statLabel}>Expansion Factor</Text>
              <Text className={styles.statValue}>
                {result.timingAdjustment.expansionFactor.toFixed(2)}x
              </Text>
            </div>
            <div className={styles.statItem}>
              <Text className={styles.statLabel}>Original Duration</Text>
              <Text className={styles.statValue}>
                {result.timingAdjustment.originalTotalDuration.toFixed(1)}s
              </Text>
            </div>
            <div className={styles.statItem}>
              <Text className={styles.statLabel}>Adjusted Duration</Text>
              <Text className={styles.statValue}>
                {result.timingAdjustment.adjustedTotalDuration.toFixed(1)}s
              </Text>
            </div>
          </div>

          {result.timingAdjustment.requiresCompression && (
            <>
              <Divider
                style={{
                  marginTop: tokens.spacingVerticalM,
                  marginBottom: tokens.spacingVerticalM,
                }}
              />
              <Badge appearance="filled" color="warning">
                Compression Recommended
              </Badge>
              <Text style={{ marginTop: tokens.spacingVerticalS, display: 'block' }}>
                Suggestions:
              </Text>
              <ul style={{ marginTop: tokens.spacingVerticalS }}>
                {result.timingAdjustment.compressionSuggestions.map((suggestion, idx) => (
                  <li key={idx}>
                    <Text>{suggestion}</Text>
                  </li>
                ))}
              </ul>
            </>
          )}

          <div className={styles.warningsList}>
            {result.timingAdjustment.warnings.map((warning, idx) => (
              <div key={idx} className={styles.warningItem}>
                {getSeverityIcon(warning.severity)}
                <Text>{warning.message}</Text>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Visual Localization Recommendations */}
      {result.visualRecommendations.length > 0 && (
        <Card>
          <Title2>
            Visual Localization Recommendations ({result.visualRecommendations.length})
          </Title2>
          <Text style={{ color: tokens.colorNeutralForeground3 }}>
            Visual elements that may need adjustment for target culture
          </Text>
          <div className={styles.visualRecommendations}>
            {result.visualRecommendations.map((rec, idx) => (
              <div key={idx} className={styles.recommendationItem}>
                <div
                  style={{
                    display: 'flex',
                    gap: tokens.spacingHorizontalS,
                    alignItems: 'center',
                    marginBottom: tokens.spacingVerticalXS,
                  }}
                >
                  <Badge
                    appearance="tint"
                    color={
                      rec.priority === 'High'
                        ? 'danger'
                        : rec.priority === 'Medium'
                          ? 'warning'
                          : 'brand'
                    }
                  >
                    {rec.priority}
                  </Badge>
                  <Badge appearance="outline">{rec.elementType}</Badge>
                </div>
                <Text
                  weight="semibold"
                  style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}
                >
                  {rec.description}
                </Text>
                <Text style={{ fontSize: '13px', color: tokens.colorNeutralForeground3 }}>
                  {rec.recommendation}
                </Text>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
};
