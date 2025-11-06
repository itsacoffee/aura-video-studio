import { Card, Text, ProgressBar, makeStyles, tokens, Caption1 } from '@fluentui/react-components';
import React from 'react';
import { useCostTrackingStore } from '../../state/costTracking';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  costAmount: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  stageRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXS,
  },
  warningText: {
    color: tokens.colorPaletteRedForeground1,
  },
});

interface CostMeterProps {
  /**
   * Job ID being tracked
   */
  jobId?: string;

  /**
   * Show detailed breakdown by stage
   */
  showDetails?: boolean;
}

/**
 * Real-time cost meter component that displays accumulating costs during video generation
 */
export const CostMeter: React.FC<CostMeterProps> = ({ showDetails = false }) => {
  const styles = useStyles();
  const { liveAccumulation, configuration } = useCostTrackingStore();

  if (!liveAccumulation) {
    return null;
  }

  const { currentCost, estimatedTotalCost, costByStage } = liveAccumulation;
  const budget = configuration?.overallMonthlyBudget;
  const isNearBudget = budget && currentCost > budget * 0.9;
  const isOverBudget = budget && currentCost > budget;

  const progressValue = budget ? Math.min((currentCost / budget) * 100, 100) : 0;

  const progressColor = isOverBudget ? 'error' : isNearBudget ? 'warning' : 'success';

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <Text weight="semibold">Cost Meter</Text>
        <Text className={styles.costAmount}>${currentCost.toFixed(4)}</Text>
      </div>

      {budget && (
        <>
          <ProgressBar value={progressValue} color={progressColor} thickness="large" />
          <div className={styles.stageRow}>
            <Caption1>
              {isOverBudget ? (
                <span className={styles.warningText}>
                  Budget exceeded: ${(currentCost - budget).toFixed(4)} over
                </span>
              ) : (
                `${progressValue.toFixed(1)}% of ${configuration.currency} ${budget.toFixed(2)} budget`
              )}
            </Caption1>
          </div>
        </>
      )}

      <div className={styles.stageRow}>
        <Caption1>Estimated total:</Caption1>
        <Caption1>${estimatedTotalCost.toFixed(4)}</Caption1>
      </div>

      {showDetails && Object.keys(costByStage).length > 0 && (
        <>
          <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
            By Stage
          </Text>
          {Object.entries(costByStage).map(([stage, cost]) => (
            <div key={stage} className={styles.stageRow}>
              <Caption1>{stage}</Caption1>
              <Caption1>${cost.toFixed(4)}</Caption1>
            </div>
          ))}
        </>
      )}
    </Card>
  );
};
