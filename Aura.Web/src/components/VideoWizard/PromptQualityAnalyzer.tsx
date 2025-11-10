import { makeStyles, tokens, Text, Badge, ProgressBar } from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Warning24Regular,
  Info24Regular,
  Lightbulb24Regular,
} from '@fluentui/react-icons';
import { useMemo } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
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
  };
}

export const PromptQualityAnalyzer: FC<PromptQualityAnalyzerProps> = ({
  prompt,
  targetAudience,
  keyMessage,
  videoType,
}) => {
  const styles = useStyles();

  const analysis = useMemo((): QualityAnalysis => {
    const suggestions: QualityAnalysis['suggestions'] = [];
    let score = 0;

    // Length analysis
    const wordCount = prompt.split(/\s+/).filter((w) => w.length > 0).length;
    const lengthScore = Math.min((wordCount / 30) * 25, 25);
    score += lengthScore;

    if (wordCount < 10) {
      suggestions.push({
        type: 'warning',
        message: 'Your prompt is quite short. Add more details about what you want in the video.',
      });
    } else if (wordCount > 100) {
      suggestions.push({
        type: 'info',
        message: 'Your prompt is detailed, which is great! Just ensure it remains focused.',
      });
    } else if (wordCount >= 20 && wordCount <= 50) {
      suggestions.push({
        type: 'success',
        message: 'Excellent prompt length! You have enough detail without being too verbose.',
      });
    }

    // Specificity analysis
    const specificKeywords = [
      'explain',
      'demonstrate',
      'show',
      'compare',
      'analyze',
      'step-by-step',
      'guide',
      'tutorial',
      'how to',
      'what is',
      'why',
      'when',
      'where',
    ];
    const hasSpecificKeywords = specificKeywords.some((keyword) =>
      prompt.toLowerCase().includes(keyword)
    );
    const specificityScore = hasSpecificKeywords ? 25 : 10;
    score += specificityScore;

    if (!hasSpecificKeywords) {
      suggestions.push({
        type: 'tip',
        message:
          'Try using action words like "explain", "demonstrate", or "show" to make your intent clearer.',
      });
    }

    // Clarity analysis (checking for vague terms)
    const vagueTerms = ['stuff', 'things', 'something', 'various', 'etc'];
    const hasVagueTerms = vagueTerms.some((term) => prompt.toLowerCase().includes(term));
    const clarityScore = hasVagueTerms ? 10 : 25;
    score += clarityScore;

    if (hasVagueTerms) {
      suggestions.push({
        type: 'warning',
        message: 'Avoid vague terms like "stuff" or "things". Be specific about what you want.',
      });
    }

    // Actionability analysis
    const actionVerbs = [
      'create',
      'build',
      'design',
      'develop',
      'implement',
      'learn',
      'understand',
      'discover',
    ];
    const hasActionVerbs = actionVerbs.some((verb) => prompt.toLowerCase().includes(verb));
    const actionabilityScore = hasActionVerbs ? 25 : 15;
    score += actionabilityScore;

    // Target audience check
    if (targetAudience && targetAudience.trim()) {
      suggestions.push({
        type: 'success',
        message: 'Great! You\'ve specified your target audience.',
      });
    } else {
      suggestions.push({
        type: 'info',
        message: 'Consider adding your target audience to get more tailored content.',
      });
    }

    // Key message check
    if (keyMessage && keyMessage.trim()) {
      suggestions.push({
        type: 'success',
        message: 'Excellent! You\'ve defined a clear key message.',
      });
    } else {
      suggestions.push({
        type: 'info',
        message: 'Define a key message to keep your video focused.',
      });
    }

    // Video type specific suggestions
    if (videoType === 'educational' || videoType === 'tutorial') {
      if (!prompt.toLowerCase().includes('step') && !prompt.toLowerCase().includes('how')) {
        suggestions.push({
          type: 'tip',
          message:
            'For educational content, consider breaking down into steps or adding "how to" for clarity.',
        });
      }
    }

    const level: QualityAnalysis['level'] =
      score >= 80 ? 'excellent' : score >= 60 ? 'good' : score >= 40 ? 'fair' : 'poor';

    return {
      score,
      level,
      suggestions,
      metrics: {
        length: Math.min((wordCount / 50) * 100, 100),
        specificity: specificityScore * 4,
        clarity: clarityScore * 4,
        actionability: actionabilityScore * 4,
      },
    };
  }, [prompt, targetAudience, keyMessage, videoType]);

  const getScoreColor = () => {
    if (analysis.level === 'excellent') return tokens.colorPaletteGreenForeground1;
    if (analysis.level === 'good') return tokens.colorPaletteBlueForeground1;
    if (analysis.level === 'fair') return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const getScoreBadge = () => {
    if (analysis.level === 'excellent') return <Badge appearance="filled" color="success">Excellent</Badge>;
    if (analysis.level === 'good') return <Badge appearance="filled" color="informative">Good</Badge>;
    if (analysis.level === 'fair') return <Badge appearance="filled" color="warning">Fair</Badge>;
    return <Badge appearance="filled" color="danger">Needs Work</Badge>;
  };

  const getSuggestionIcon = (type: string) => {
    switch (type) {
      case 'success':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'warning':
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'tip':
        return <Lightbulb24Regular style={{ color: tokens.colorPaletteBlueForeground1 }} />;
      default:
        return <Info24Regular style={{ color: tokens.colorNeutralForeground3 }} />;
    }
  };

  if (!prompt || prompt.trim().length === 0) {
    return null;
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text weight="semibold" size={300}>
          Prompt Quality Analysis
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
          color={analysis.level === 'excellent' || analysis.level === 'good' ? 'success' : 'warning'}
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
          <Text className={styles.metricValue}>{Math.round(analysis.metrics.actionability)}%</Text>
          <Text className={styles.metricLabel}>Actionability</Text>
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
    </div>
  );
};
