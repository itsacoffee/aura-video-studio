import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
  Badge,
  Checkbox,
  ProgressBar,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Warning24Regular,
  ArrowSync24Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  scoresGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  scoreCard: {
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  scoreValue: {
    fontSize: '48px',
    fontWeight: 'bold',
    marginBottom: tokens.spacingVerticalS,
  },
  scoreLabel: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalXS,
  },
  overallScore: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  overallValue: {
    fontSize: '72px',
    fontWeight: 'bold',
  },
  issuesList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  issueItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  suggestionsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'center',
  },
  statistics: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  statItem: {
    textAlign: 'center',
    padding: tokens.spacingVerticalM,
  },
});

interface ScriptAnalysisProps {
  script: string;
  onEnhanceScript?: (enhancedScript: string) => void;
  onProceed?: () => void;
  onRegenerate?: () => void;
}

interface AnalysisResult {
  coherenceScore: number;
  pacingScore: number;
  engagementScore: number;
  readabilityScore: number;
  overallQualityScore: number;
  issues: string[];
  suggestions: string[];
  statistics: {
    totalWordCount: number;
    averageWordsPerScene: number;
    estimatedReadingTime: string;
    complexityScore: number;
  };
}

interface EnhancementOptions {
  fixCoherence: boolean;
  increaseEngagement: boolean;
  improveClarity: boolean;
  addDetails: boolean;
}

export function ScriptAnalysis({ script, onEnhanceScript, onProceed, onRegenerate }: ScriptAnalysisProps) {
  const styles = useStyles();
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [enhancing, setEnhancing] = useState(false);
  const [enhancementOptions, setEnhancementOptions] = useState<EnhancementOptions>({
    fixCoherence: false,
    increaseEngagement: false,
    improveClarity: false,
    addDetails: false,
  });

  const analyzeScript = async () => {
    setLoading(true);
    try {
      const response = await fetch(`${apiUrl}/content/analyze-script`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ script }),
      });
      const data = await response.json();
      setAnalysis(data);
    } catch (error) {
      console.error('Failed to analyze script:', error);
    } finally {
      setLoading(false);
    }
  };

  const enhanceScript = async () => {
    setEnhancing(true);
    try {
      const response = await fetch(`${apiUrl}/content/enhance-script`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ script, ...enhancementOptions }),
      });
      const data = await response.json();
      if (onEnhanceScript) {
        onEnhanceScript(data.newScript);
      }
      // Re-analyze the enhanced script
      await analyzeScript();
    } catch (error) {
      console.error('Failed to enhance script:', error);
    } finally {
      setEnhancing(false);
    }
  };

  const getScoreColor = (score: number) => {
    if (score >= 80) return tokens.colorPaletteGreenForeground1;
    if (score >= 60) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  if (!analysis && !loading) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <Title2>Script Analysis</Title2>
          <Text block style={{ marginTop: tokens.spacingVerticalM, marginBottom: tokens.spacingVerticalL }}>
            Analyze your script to get quality scores and improvement suggestions
          </Text>
          <Button appearance="primary" icon={<Sparkle24Regular />} onClick={analyzeScript}>
            Analyze Script
          </Button>
        </div>
      </Card>
    );
  }

  if (loading) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <Spinner size="large" />
          <Text block style={{ marginTop: tokens.spacingVerticalM }}>
            Analyzing your script...
          </Text>
        </div>
      </Card>
    );
  }

  if (!analysis) return null;

  return (
    <div className={styles.container}>
      <Card className={styles.overallScore}>
        <Text className={styles.scoreLabel}>Overall Quality Score</Text>
        <div className={styles.overallValue} style={{ color: getScoreColor(analysis.overallQualityScore) }}>
          {analysis.overallQualityScore.toFixed(0)}
        </div>
        <ProgressBar
          value={analysis.overallQualityScore / 100}
          color={analysis.overallQualityScore >= 80 ? 'success' : analysis.overallQualityScore >= 60 ? 'warning' : 'error'}
        />
        {analysis.overallQualityScore < 70 && (
          <Badge appearance="tinted" color="warning" style={{ marginTop: tokens.spacingVerticalM }}>
            Below quality threshold - consider enhancement
          </Badge>
        )}
      </Card>

      <div className={styles.scoresGrid}>
        <Card className={styles.scoreCard}>
          <Text className={styles.scoreLabel}>Coherence</Text>
          <div className={styles.scoreValue} style={{ color: getScoreColor(analysis.coherenceScore) }}>
            {analysis.coherenceScore.toFixed(0)}
          </div>
        </Card>
        <Card className={styles.scoreCard}>
          <Text className={styles.scoreLabel}>Pacing</Text>
          <div className={styles.scoreValue} style={{ color: getScoreColor(analysis.pacingScore) }}>
            {analysis.pacingScore.toFixed(0)}
          </div>
        </Card>
        <Card className={styles.scoreCard}>
          <Text className={styles.scoreLabel}>Engagement</Text>
          <div className={styles.scoreValue} style={{ color: getScoreColor(analysis.engagementScore) }}>
            {analysis.engagementScore.toFixed(0)}
          </div>
        </Card>
        <Card className={styles.scoreCard}>
          <Text className={styles.scoreLabel}>Readability</Text>
          <div className={styles.scoreValue} style={{ color: getScoreColor(analysis.readabilityScore) }}>
            {analysis.readabilityScore.toFixed(0)}
          </div>
        </Card>
      </div>

      {analysis.issues.length > 0 && (
        <Card>
          <Title3>Issues Found</Title3>
          <div className={styles.issuesList}>
            {analysis.issues.map((issue, index) => (
              <div key={index} className={styles.issueItem}>
                <Warning24Regular />
                <Text>{issue}</Text>
              </div>
            ))}
          </div>
        </Card>
      )}

      {analysis.suggestions.length > 0 && (
        <Card>
          <Title3>Improvement Suggestions</Title3>
          <div className={styles.suggestionsList}>
            <Checkbox
              label="Fix Coherence (add transitions between scenes)"
              checked={enhancementOptions.fixCoherence}
              onChange={(_, data) =>
                setEnhancementOptions({ ...enhancementOptions, fixCoherence: data.checked === true })
              }
            />
            <Checkbox
              label="Increase Engagement (add hooks, interesting facts)"
              checked={enhancementOptions.increaseEngagement}
              onChange={(_, data) =>
                setEnhancementOptions({ ...enhancementOptions, increaseEngagement: data.checked === true })
              }
            />
            <Checkbox
              label="Improve Clarity (simplify complex sentences)"
              checked={enhancementOptions.improveClarity}
              onChange={(_, data) =>
                setEnhancementOptions({ ...enhancementOptions, improveClarity: data.checked === true })
              }
            />
            <Checkbox
              label="Add Details (expand with relevant context)"
              checked={enhancementOptions.addDetails}
              onChange={(_, data) =>
                setEnhancementOptions({ ...enhancementOptions, addDetails: data.checked === true })
              }
            />
          </div>
        </Card>
      )}

      <Card>
        <Title3>Statistics</Title3>
        <div className={styles.statistics}>
          <div className={styles.statItem}>
            <Text weight="semibold" block>
              {analysis.statistics.totalWordCount}
            </Text>
            <Text size={200}>Total Words</Text>
          </div>
          <div className={styles.statItem}>
            <Text weight="semibold" block>
              {analysis.statistics.averageWordsPerScene.toFixed(0)}
            </Text>
            <Text size={200}>Avg Words/Scene</Text>
          </div>
          <div className={styles.statItem}>
            <Text weight="semibold" block>
              {analysis.statistics.estimatedReadingTime}
            </Text>
            <Text size={200}>Reading Time</Text>
          </div>
          <div className={styles.statItem}>
            <Text weight="semibold" block>
              {analysis.statistics.complexityScore.toFixed(0)}
            </Text>
            <Text size={200}>Complexity</Text>
          </div>
        </div>
      </Card>

      <div className={styles.actions}>
        {onRegenerate && (
          <Button icon={<ArrowSync24Regular />} onClick={onRegenerate}>
            Regenerate Script
          </Button>
        )}
        {onEnhanceScript && (
          <Button
            appearance="primary"
            icon={<Sparkle24Regular />}
            onClick={enhanceScript}
            disabled={
              enhancing ||
              (!enhancementOptions.fixCoherence &&
                !enhancementOptions.increaseEngagement &&
                !enhancementOptions.improveClarity &&
                !enhancementOptions.addDetails)
            }
          >
            {enhancing ? <Spinner size="tiny" /> : 'Enhance Script'}
          </Button>
        )}
        {onProceed && (
          <Button appearance="primary" icon={<CheckmarkCircle24Regular />} onClick={onProceed}>
            Proceed Anyway
          </Button>
        )}
      </div>
    </div>
  );
}
