/**
 * Cut Point Panel Component
 * Displays and manages cut point suggestions
 */

import {
  Button,
  Card,
  makeStyles,
  tokens,
  Badge,
  Body1,
  Body1Strong,
  Caption1,
} from '@fluentui/react-components';
import { Checkmark24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import React from 'react';
import { CutPoint } from '../../../services/editingIntelligenceService';

interface CutPointPanelProps {
  cutPoints: CutPoint[];
  onApply?: (type: string, data: unknown) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  cutPointCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  confidenceBadge: {
    marginLeft: tokens.spacingHorizontalS,
  },
});

const getConfidenceColor = (confidence: number): 'success' | 'warning' | 'important' => {
  if (confidence >= 0.8) return 'success';
  if (confidence >= 0.6) return 'warning';
  return 'important';
};

const getCutTypeLabel = (type: string): string => {
  return type.replace(/([A-Z])/g, ' $1').trim();
};

export const CutPointPanel: React.FC<CutPointPanelProps> = ({ cutPoints, onApply }) => {
  const styles = useStyles();

  if (cutPoints.length === 0) {
    return <Body1>No cut point suggestions available.</Body1>;
  }

  return (
    <div className={styles.container}>
      {cutPoints.map((cutPoint, index) => (
        <Card key={index} className={styles.cutPointCard}>
          <div className={styles.header}>
            <div>
              <Body1Strong>{getCutTypeLabel(cutPoint.type)}</Body1Strong>
              <Badge
                appearance="filled"
                color={getConfidenceColor(cutPoint.confidence)}
                className={styles.confidenceBadge}
              >
                {Math.round(cutPoint.confidence * 100)}% confident
              </Badge>
            </div>
            <Caption1>@ {cutPoint.timestamp}</Caption1>
          </div>

          <Body1>{cutPoint.reasoning}</Body1>

          {cutPoint.durationToRemove && <Caption1>Remove: {cutPoint.durationToRemove}</Caption1>}

          <div className={styles.actions}>
            <Button
              size="small"
              appearance="primary"
              icon={<Checkmark24Regular />}
              onClick={() => onApply?.('cutPoint', cutPoint)}
            >
              Apply
            </Button>
            <Button size="small" appearance="subtle" icon={<Dismiss24Regular />}>
              Dismiss
            </Button>
          </div>
        </Card>
      ))}
    </div>
  );
};
