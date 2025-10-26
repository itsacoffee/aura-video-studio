/**
 * Transition Panel Component
 * Displays transition recommendations
 */

import {
  Button,
  Card,
  makeStyles,
  tokens,
  Spinner,
  Body1,
  Body1Strong,
  Caption1,
} from '@fluentui/react-components';
import { Checkmark24Regular } from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import {
  recommendTransitions,
  TransitionSuggestion,
} from '../../../services/editingIntelligenceService';

interface TransitionPanelProps {
  jobId: string;
  onApply?: (type: string, data: any) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  transitionCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  transitionType: {
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusSmall,
    display: 'inline-block',
    marginTop: tokens.spacingVerticalS,
  },
});

export const TransitionPanel: React.FC<TransitionPanelProps> = ({ jobId, onApply }) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [transitions, setTransitions] = useState<TransitionSuggestion[]>([]);

  useEffect(() => {
    const loadTransitions = async () => {
      if (!jobId) return;

      setLoading(true);
      try {
        const result = await recommendTransitions(jobId);
        setTransitions(result.suggestions);
      } catch (error) {
        console.error('Failed to load transitions:', error);
      } finally {
        setLoading(false);
      }
    };

    loadTransitions();
  }, [jobId]);

  if (loading) {
    return <Spinner label="Loading transitions..." />;
  }

  if (transitions.length === 0) {
    return <Body1>No transition suggestions available.</Body1>;
  }

  return (
    <div className={styles.container}>
      {transitions.map((transition, index) => (
        <Card key={index} className={styles.transitionCard}>
          <div className={styles.header}>
            <Body1Strong>
              Scene {transition.fromSceneIndex + 1} → Scene {transition.toSceneIndex + 1}
            </Body1Strong>
            <Caption1>{Math.round(transition.confidence * 100)}% confident</Caption1>
          </div>

          <div className={styles.transitionType}>
            <Caption1>{transition.type}</Caption1>
          </div>

          <Body1 style={{ marginTop: tokens.spacingVerticalM }}>{transition.reasoning}</Body1>

          <Caption1 style={{ marginTop: tokens.spacingVerticalS }}>
            Duration: {transition.duration}
          </Caption1>

          <Button
            size="small"
            appearance="primary"
            icon={<Checkmark24Regular />}
            onClick={() => onApply?.('transition', transition)}
            style={{ marginTop: tokens.spacingVerticalM }}
          >
            Apply Transition
          </Button>
        </Card>
      ))}
    </div>
  );
};
