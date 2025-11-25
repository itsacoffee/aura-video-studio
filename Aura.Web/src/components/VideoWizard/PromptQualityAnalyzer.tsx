import {
  makeStyles,
  tokens,
  Text,
  Badge,
  ProgressBar,
  Button,
  Spinner,
  Tooltip,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Warning24Regular,
  Info24Regular,
  Lightbulb24Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { ideationService } from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  buttonContainer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  analyzeButton: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  scoreBar: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  scoreLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  suggestions: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  suggestionItem: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-start',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
  suggestionIcon: {
    marginTop: '2px',
    flexShrink: 0,
  },
  suggestionContent: {
    flex: 1,
  },
  metrics: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  metricValue: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  metricLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXL,
  },
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusSmall,
  },
});

interface PromptQualityAnalyzerProps {
  prompt: string;
  targetAudience?: string;
  keyMessage?: string;
  videoType?: string;
}

interface QualityAnalysis {
  score: number;
  level: 'excellent' | 'good' | 'fair' | 'poor';
  suggestions: Array<{
    type: 'success' | 'warning' | 'info' | 'tip';
    message: string;
  }>;
  metrics: {
    length: number;
    specificity: number;
    clarity: number;
    actionability: number;
    engagement: number;
    alignment: number;
  };
}

export const PromptQualityAnalyzer: FC<PromptQualityAnalyzerProps> = ({
  prompt,
  targetAudience,
  keyMessage,
  videoType,
}) => {
  const styles = useStyles();
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [analysis, setAnalysis] = useState<QualityAnalysis | null>(null);
  const [error, setError] = useState<string | null>(null);

  const canAnalyze = prompt && prompt.trim().length >= 10 && targetAudience && keyMessage;

  const handleAnalyze = async () => {
    if (!canAnalyze) return;

    setIsAnalyzing(true);
    setError(null);
    setAnalysis(null);

    try {
      // Call LLM-based analysis via ideation service
      // This will use the enhance-topic endpoint as a foundation and extend it for quality analysis
      // For now, we'll create a comprehensive prompt-based analysis
      const analysisResult = await analyzePromptQualityWithLLM({
        topic: prompt,
        videoType: videoType || 'educational',
        targetAudience: targetAudience || '',
        keyMessage: keyMessage || '',
      });

      setAnalysis(analysisResult);
    } catch (err) {
      console.error('Failed to analyze prompt quality:', err);
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to analyze prompt quality. Please try again.'
      );
    } finally {
      setIsAnalyzing(false);
    }
  };

  const getScoreColor = () => {
    if (!analysis) return tokens.colorNeutralForeground2;
    if (analysis.level === 'excellent') return tokens.colorPaletteGreenForeground1;
    if (analysis.level === 'good') return tokens.colorPaletteBlueForeground2;
    if (analysis.level === 'fair') return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const getScoreBadge = () => {
    if (!analysis) return null;
    if (analysis.level === 'excellent')
      return (
        <Badge appearance="filled" color="success">
          Excellent
        </Badge>
      );
    if (analysis.level === 'good')
      return (
        <Badge appearance="filled" color="informative">
          Good
        </Badge>
      );
    if (analysis.level === 'fair')
      return (
        <Badge appearance="filled" color="warning">
          Fair
        </Badge>
      );
    return (
      <Badge appearance="filled" color="danger">
        Needs Work
      </Badge>
    );
  };

  const getSuggestionIcon = (type: string) => {
    switch (type) {
      case 'success':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'warning':
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'tip':
        return <Lightbulb24Regular style={{ color: tokens.colorPaletteBlueForeground2 }} />;
      default:
        return <Info24Regular style={{ color: tokens.colorNeutralForeground3 }} />;
    }
  };

  // Don't render if prompt is empty
  if (!prompt || prompt.trim().length === 0) {
    return null;
  }

  return (
    <div className={styles.container}>
      <div className={styles.buttonContainer}>
        <Text weight="semibold" size={300}>
          Prompt Quality Analysis
        </Text>
        <Tooltip
          content={
            !canAnalyze
              ? 'Please fill in all required fields (Topic, Target Audience, and Key Message) before analyzing quality'
              : 'Analyze your prompt quality using AI-powered analysis'
          }
          relationship="label"
        >
          <Button
            appearance="secondary"
            icon={isAnalyzing ? <Spinner size="tiny" /> : <Sparkle24Regular />}
            onClick={handleAnalyze}
            disabled={!canAnalyze || isAnalyzing}
            className={styles.analyzeButton}
          >
            {isAnalyzing ? 'Analyzing...' : 'Analyze Quality'}
          </Button>
        </Tooltip>
      </div>

      {isAnalyzing && (
        <div className={styles.loadingContainer}>
          <Spinner size="large" />
          <Text>Analyzing prompt quality with AI...</Text>
        </div>
      )}

      {error && (
        <div className={styles.errorMessage}>
          <Text>{error}</Text>
        </div>
      )}

      {!isAnalyzing && analysis && (
        <>
          <div className={styles.header}>
            <Text weight="semibold" size={300}>
              Analysis Results
            </Text>
            {getScoreBadge()}
          </div>

          <div className={styles.scoreBar}>
            <div className={styles.scoreLabel}>
              <Text size={200}>Overall Quality Score</Text>
              <Text weight="semibold" style={{ color: getScoreColor() }}>
                {analysis.score}/100
              </Text>
            </div>
            <ProgressBar
              value={analysis.score / 100}
              color={
                analysis.level === 'excellent' || analysis.level === 'good' ? 'success' : 'warning'
              }
            />
          </div>

          <div className={styles.metrics}>
            <div className={styles.metricItem}>
              <Text className={styles.metricValue}>{Math.round(analysis.metrics.length)}%</Text>
              <Text className={styles.metricLabel}>Length</Text>
            </div>
            <div className={styles.metricItem}>
              <Text className={styles.metricValue}>{Math.round(analysis.metrics.specificity)}%</Text>
              <Text className={styles.metricLabel}>Specificity</Text>
            </div>
            <div className={styles.metricItem}>
              <Text className={styles.metricValue}>{Math.round(analysis.metrics.clarity)}%</Text>
              <Text className={styles.metricLabel}>Clarity</Text>
            </div>
            <div className={styles.metricItem}>
              <Text className={styles.metricValue}>
                {Math.round(analysis.metrics.actionability)}%
              </Text>
              <Text className={styles.metricLabel}>Actionability</Text>
            </div>
            <div className={styles.metricItem}>
              <Text className={styles.metricValue}>
                {Math.round(analysis.metrics.engagement)}%
              </Text>
              <Text className={styles.metricLabel}>Engagement</Text>
            </div>
            <div className={styles.metricItem}>
              <Text className={styles.metricValue}>{Math.round(analysis.metrics.alignment)}%</Text>
              <Text className={styles.metricLabel}>Alignment</Text>
            </div>
          </div>

          {analysis.suggestions.length > 0 && (
            <div className={styles.suggestions}>
              <Text weight="semibold" size={200}>
                Suggestions
              </Text>
              {analysis.suggestions.map((suggestion, index) => (
                <div key={index} className={styles.suggestionItem}>
                  <div className={styles.suggestionIcon}>{getSuggestionIcon(suggestion.type)}</div>
                  <div className={styles.suggestionContent}>
                    <Text size={200}>{suggestion.message}</Text>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
};

// LLM-based prompt quality analysis function
async function analyzePromptQualityWithLLM(params: {
  topic: string;
  videoType: string;
  targetAudience: string;
  keyMessage: string;
}): Promise<QualityAnalysis> {
  try {
    // Try to use the dedicated prompt quality analysis endpoint
    try {
      const response = await ideationService.analyzePromptQuality({
        topic: params.topic,
        videoType: params.videoType,
        targetAudience: params.targetAudience,
        keyMessage: params.keyMessage,
        ragConfiguration: {
          enabled: true,
          topK: 5,
          minimumScore: 0.6,
          maxContextTokens: 2000,
          includeCitations: false,
          tightenClaims: false,
        },
      });

      if (response.success) {
        return {
          score: response.score,
          level: response.level,
          suggestions: response.suggestions,
          metrics: response.metrics,
        };
      }
    } catch (apiError) {
      // If the dedicated endpoint doesn't exist or fails, fall back to enhance-topic
      console.warn('Prompt quality analysis endpoint not available, using fallback:', apiError);
      
      // Fall back to using enhance-topic for insights
      try {
        const enhanceResponse = await ideationService.enhanceTopic({
          topic: params.topic,
          videoType: params.videoType,
          targetAudience: params.targetAudience,
          keyMessage: params.keyMessage,
        });

        // Extract insights from enhancement response
        const improvements = enhanceResponse.improvements || '';
        return extractQualityFromEnhancement(params, improvements);
      } catch (enhanceError) {
        console.error('Both analysis methods failed:', enhanceError);
        throw enhanceError;
      }
    }

    // If we get here, return fallback
    return getFallbackAnalysis(params);
  } catch (error) {
    console.error('Error in prompt quality analysis:', error);
    // Fallback to basic analysis if all LLM methods fail
    return getFallbackAnalysis(params);
  }
}

// Extract quality metrics from enhancement response
function extractQualityFromEnhancement(
  params: {
    topic: string;
    videoType: string;
    targetAudience: string;
    keyMessage: string;
  },
  improvements: string
): QualityAnalysis {
  const suggestions: QualityAnalysis['suggestions'] = [];
  const wordCount = params.topic.split(/\s+/).filter((w) => w.length > 0).length;

  if (improvements) {
    suggestions.push({
      type: 'tip',
      message: improvements,
    });
  }

  // Calculate metrics based on prompt characteristics
  const lengthScore = Math.min((wordCount / 40) * 20, 20);
  const hasSpecificDetails = wordCount > 15 && params.keyMessage.length > 10;
  const specificityScore = hasSpecificDetails ? 20 : 10;
  const clarityScore = params.targetAudience.length > 5 && params.keyMessage.length > 5 ? 20 : 10;
  const actionabilityScore = params.keyMessage.length > 10 ? 15 : 8;
  const engagementScore = params.videoType && params.targetAudience ? 15 : 8;
  const alignmentScore = params.keyMessage && params.targetAudience ? 10 : 5;

  const totalScore =
    lengthScore +
    specificityScore +
    clarityScore +
    actionabilityScore +
    engagementScore +
    alignmentScore;

  if (wordCount < 15) {
    suggestions.push({
      type: 'warning',
      message: 'Your prompt could benefit from more detail. Consider adding specific examples or context.',
    });
  }

  if (totalScore >= 80) {
    suggestions.push({
      type: 'success',
      message: 'Great prompt! You have a clear topic, audience, and message.',
    });
  }

  const level: QualityAnalysis['level'] =
    totalScore >= 80
      ? 'excellent'
      : totalScore >= 60
        ? 'good'
        : totalScore >= 40
          ? 'fair'
          : 'poor';

  return {
    score: Math.round(totalScore),
    level,
    suggestions,
    metrics: {
      length: Math.round((lengthScore / 20) * 100),
      specificity: Math.round((specificityScore / 20) * 100),
      clarity: Math.round((clarityScore / 20) * 100),
      actionability: Math.round((actionabilityScore / 15) * 100),
      engagement: Math.round((engagementScore / 15) * 100),
      alignment: Math.round((alignmentScore / 10) * 100),
    },
  };
}

// Fallback analysis if LLM is unavailable
function getFallbackAnalysis(params: {
  topic: string;
  videoType: string;
  targetAudience: string;
  keyMessage: string;
}): QualityAnalysis {
  const wordCount = params.topic.split(/\s+/).filter((w) => w.length > 0).length;
  const lengthScore = Math.min((wordCount / 30) * 25, 25);
  const specificityScore = wordCount > 20 ? 20 : 10;
  const clarityScore = params.targetAudience.length > 5 && params.keyMessage.length > 5 ? 20 : 10;
  const actionabilityScore = 15;
  const engagementScore = 10;
  const alignmentScore = 10;

  const totalScore = lengthScore + specificityScore + clarityScore + actionabilityScore + engagementScore + alignmentScore;

  const suggestions: QualityAnalysis['suggestions'] = [];
  
  if (wordCount < 15) {
    suggestions.push({
      type: 'warning',
      message: 'Add more detail to your prompt for better results.',
    });
  }

  const level: QualityAnalysis['level'] =
    totalScore >= 80
      ? 'excellent'
      : totalScore >= 60
        ? 'good'
        : totalScore >= 40
          ? 'fair'
          : 'poor';

  return {
    score: Math.round(totalScore),
    level,
    suggestions,
    metrics: {
      length: Math.round((lengthScore / 25) * 100),
      specificity: Math.round((specificityScore / 20) * 100),
      clarity: Math.round((clarityScore / 20) * 100),
      actionability: Math.round((actionabilityScore / 15) * 100),
      engagement: Math.round((engagementScore / 10) * 100),
      alignment: Math.round((alignmentScore / 10) * 100),
    },
  };
}