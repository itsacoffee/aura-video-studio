import {
  Card,
  Text,
  ProgressBar,
  makeStyles,
  tokens,
  Caption1,
  Badge,
} from '@fluentui/react-components';
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
  softLimitText: {
    color: tokens.colorPaletteYellowForeground1,
  },
  statusBadge: {
    marginLeft: tokens.spacingHorizontalS,
  },
  budgetInfo: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface CostMeterProps {
  /**
   * Show detailed breakdown by stage
   */
  showDetails?: boolean;
}

/**
 * Real-time cost meter component that displays accumulating costs during video generation
 * Enhanced with clear soft/hard budget threshold indicators
 */
export const CostMeter: React.FC<CostMeterProps> = ({ showDetails = false }) => {
  const styles = useStyles();
  const { liveAccumulation, configuration } = useCostTrackingStore();

  if (!liveAccumulation) {
    return null;
  }

  const { currentCost, estimatedTotalCost, costByStage } = liveAccumulation;
  const budget = configuration?.overallMonthlyBudget;
  const isHardLimit = configuration?.hardBudgetLimit ?? false;

  // Calculate budget status
  const budgetPercentage = budget ? (currentCost / budget) * 100 : 0;
  const isNearBudget = budget && budgetPercentage >= 90;
  const isOverBudget = budget && currentCost > budget;
  const isApproachingBudget = budget && budgetPercentage >= 75 && budgetPercentage < 90;

  // Determine progress bar color based on budget status
  const progressColor = isOverBudget ? 'error' : isNearBudget ? 'warning' : 'success';

  // Determine status badge
  const getBudgetStatus = () => {
    if (!budget) return null;

    if (isOverBudget && isHardLimit) {
      return (
        <Badge appearance="filled" color="danger" className={styles.statusBadge}>
          HARD LIMIT EXCEEDED
        </Badge>
      );
    }
    if (isOverBudget) {
      return (
        <Badge appearance="filled" color="warning" className={styles.statusBadge}>
          SOFT LIMIT EXCEEDED
        </Badge>
      );
    }
    if (isNearBudget) {
      return (
        <Badge appearance="filled" color="warning" className={styles.statusBadge}>
          APPROACHING LIMIT
        </Badge>
      );
    }
    if (isApproachingBudget) {
      return (
        <Badge appearance="outline" color="warning" className={styles.statusBadge}>
          WARNING
        </Badge>
      );
    }
    return (
      <Badge appearance="outline" color="success" className={styles.statusBadge}>
        WITHIN BUDGET
      </Badge>
    );
  };

  const progressValue = budget ? Math.min(budgetPercentage, 100) : 0;

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <div>
          <Text weight="semibold">Cost Meter</Text>
          {getBudgetStatus()}
        </div>
        <Text className={styles.costAmount}>${currentCost.toFixed(4)}</Text>
      </div>

      {budget && (
        <>
          <ProgressBar value={progressValue} color={progressColor} thickness="large" />
          <div className={styles.budgetInfo}>
            <div className={styles.stageRow}>
              <Caption1>Budget Type:</Caption1>
              <Text size={200} weight="semibold">
                {isHardLimit ? 'Hard Limit' : 'Soft Limit'}
              </Text>
            </div>
            <div className={styles.stageRow}>
              <Caption1>Budget Usage:</Caption1>
              <Text size={200} weight="semibold">
                {budgetPercentage.toFixed(1)}% of {configuration.currency} ${budget.toFixed(2)}
              </Text>
            </div>
            {isOverBudget ? (
              <div className={styles.stageRow}>
                <Caption1 className={isHardLimit ? styles.warningText : styles.softLimitText}>
                  {isHardLimit
                    ? `⛔ Operations blocked: $${(currentCost - budget).toFixed(4)} over hard limit`
                    : `⚠️ Over budget by: $${(currentCost - budget).toFixed(4)} (soft limit)`}
                </Caption1>
              </div>
            ) : (
              <div className={styles.stageRow}>
                <Caption1>Remaining:</Caption1>
                <Text size={200} weight="semibold">
                  ${(budget - currentCost).toFixed(4)}
                </Text>
              </div>
            )}
          </div>
        </>
      )}

      <div className={styles.stageRow} style={{ marginTop: tokens.spacingVerticalM }}>
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
