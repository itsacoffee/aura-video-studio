/**
 * Transition Suggestion Card Component
 * Component for displaying transition options between scenes
 */

import { useState, useEffect } from 'react';
import {
  Card,
  makeStyles,
  tokens,
  Body1,
  Body1Strong,
  Caption1,
  Badge,
  Spinner,
  Tooltip,
} from '@fluentui/react-components';
import {
  VideoClipMultiple24Regular,
  ArrowRight24Regular,
  Info24Regular,
  Lightbulb24Regular,
} from '@fluentui/react-icons';
import { Scene } from '../../types';

interface TransitionSuggestionCardProps {
  scenes: Scene[];
  optimizationActive: boolean;
}

interface TransitionSuggestion {
  fromSceneIndex: number;
  toSceneIndex: number;
  recommendedType: string;
  duration: number;
  confidence: number;
  reasoning: string;
}

const useStyles = makeStyles({
  container: {
    width: '100%',
  },
  transitionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  transitionCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    ':hover': {
      boxShadow: tokens.shadow4,
    },
  },
  transitionHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  sceneFlow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  sceneLabel: {
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
  transitionDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  confidenceBar: {
    height: '4px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusLarge,
    overflow: 'hidden',
    marginTop: tokens.spacingVerticalXS,
  },
  confidenceFill: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    transition: 'width 0.3s ease',
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

export const TransitionSuggestionCard = ({
  scenes,
  optimizationActive,
}: TransitionSuggestionCardProps) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [transitions, setTransitions] = useState<TransitionSuggestion[]>([]);

  useEffect(() => {
    if (optimizationActive && scenes.length > 1) {
      analyzeTransitions();
    }
  }, [optimizationActive, scenes]);

  const analyzeTransitions = async () => {
    setLoading(true);
    
    try {
      // Simulate transition analysis
      // In production, this would call the TransitionRecommendationService API
      await new Promise(resolve => setTimeout(resolve, 800));
      
      const transitionTypes = ['Cut', 'Dissolve', 'Fade to Black', 'Wipe', 'Zoom'];
      const mockTransitions: TransitionSuggestion[] = [];

      for (let i = 0; i < scenes.length - 1; i++) {
        const typeIndex = Math.floor(Math.random() * transitionTypes.length);
        mockTransitions.push({
          fromSceneIndex: i,
          toSceneIndex: i + 1,
          recommendedType: transitionTypes[typeIndex],
          duration: typeIndex === 0 ? 0 : Math.random() * 0.5 + 0.3, // Cut is instant
          confidence: Math.random() * 0.3 + 0.6, // 60-90% confidence
          reasoning: generateReasoning(transitionTypes[typeIndex]),
        });
      }

      setTransitions(mockTransitions);
    } catch (error) {
      console.error('Transition analysis failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const generateReasoning = (type: string): string => {
    const reasons: Record<string, string> = {
      'Cut': 'Quick transition maintains energy and flow between similar scenes',
      'Dissolve': 'Smooth blend recommended for moderate content shift',
      'Fade to Black': 'Significant mood change detected, fade creates dramatic separation',
      'Wipe': 'Dynamic transition suitable for pace change',
      'Zoom': 'Engaging transition for building momentum',
    };

    return reasons[type] || 'Recommended based on content analysis';
  };

  const getTransitionBadge = (type: string) => {
    const colorMap: Record<string, any> = {
      'Cut': 'brand',
      'Dissolve': 'informative',
      'Fade to Black': 'severe',
      'Wipe': 'success',
      'Zoom': 'warning',
    };

    return <Badge appearance="tint" color={colorMap[type] || 'subtle'}>{type}</Badge>;
  };

  const formatDuration = (seconds: number): string => {
    if (seconds === 0) return 'Instant';
    return `${(seconds * 1000).toFixed(0)}ms`;
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Analyzing transitions..." />
      </div>
    );
  }

  if (!optimizationActive) {
    return (
      <div className={styles.emptyState}>
        <Info24Regular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
        <Body1>Click "Optimize Pacing" to analyze transitions</Body1>
      </div>
    );
  }

  if (transitions.length === 0) {
    return (
      <div className={styles.emptyState}>
        <VideoClipMultiple24Regular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
        <Body1>Need at least 2 scenes to suggest transitions</Body1>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div style={{ marginBottom: tokens.spacingVerticalM }}>
        <Body1Strong>Recommended Transitions</Body1Strong>
        <Caption1>AI-suggested transitions based on content flow and pacing</Caption1>
      </div>

      <div className={styles.transitionList}>
        {transitions.map((transition, index) => (
          <Card key={index} className={styles.transitionCard}>
            <div className={styles.transitionHeader}>
              <div className={styles.sceneFlow}>
                <div className={styles.sceneLabel}>
                  <Caption1>Scene {transition.fromSceneIndex + 1}</Caption1>
                </div>
                <ArrowRight24Regular />
                <div className={styles.sceneLabel}>
                  <Caption1>Scene {transition.toSceneIndex + 1}</Caption1>
                </div>
              </div>
              {getTransitionBadge(transition.recommendedType)}
            </div>

            <div className={styles.transitionDetails}>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM }}>
                <Caption1><strong>Duration:</strong> {formatDuration(transition.duration)}</Caption1>
                <Caption1>
                  <strong>Confidence:</strong> {(transition.confidence * 100).toFixed(0)}%
                </Caption1>
              </div>

              <div style={{ display: 'flex', alignItems: 'flex-start', gap: tokens.spacingHorizontalXS }}>
                <Tooltip content="AI Reasoning" relationship="label">
                  <Lightbulb24Regular style={{ fontSize: '16px', marginTop: '2px' }} />
                </Tooltip>
                <Caption1 style={{ flex: 1 }}>{transition.reasoning}</Caption1>
              </div>

              <div className={styles.confidenceBar}>
                <div
                  className={styles.confidenceFill}
                  style={{ width: `${transition.confidence * 100}%` }}
                />
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
};
