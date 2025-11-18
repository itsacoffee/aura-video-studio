import { makeStyles, tokens, Card, Title3, Text, ProgressBar } from '@fluentui/react-components';
import type { FC } from 'react';
import type {
  CurrentPeriodSpending,
  SpendingReport,
} from '../../services/providers/providerRecommendationService';

const useStyles = makeStyles({
  container: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  totalCost: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorBrandForeground1,
    marginBottom: tokens.spacingVerticalS,
  },
  progressSection: {
    marginTop: tokens.spacingVerticalM,
  },
  budgetText: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  providerList: {
    marginTop: tokens.spacingVerticalM,
  },
  providerItem: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  providerName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  providerCost: {
    color: tokens.colorNeutralForeground3,
  },
});

interface CostDashboardProps {
  currentPeriod: CurrentPeriodSpending | null;
  spendingReport: SpendingReport | null;
}

export const CostDashboard: FC<CostDashboardProps> = ({ currentPeriod, spendingReport }) => {
  const styles = useStyles();

  const getProgressColor = (percentage: number): 'success' | 'warning' | 'error' => {
    if (percentage < 50) return 'success';
    if (percentage < 90) return 'warning';
    return 'error';
  };

  const formatCurrency = (value: number, currency: string) => {
    return `${currency} ${value.toFixed(2)}`;
  };

  const topProviders = spendingReport
    ? Object.entries(spendingReport.costByProvider)
        .sort(([, a], [, b]) => b - a)
        .slice(0, 5)
    : [];

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <Title3>Current Period Spending</Title3>
        {currentPeriod ? (
          <>
            <div className={styles.totalCost}>
              {formatCurrency(currentPeriod.totalCost, currentPeriod.currency)}
            </div>
            <Text>Period: {currentPeriod.periodType}</Text>

            {currentPeriod.budget && (
              <div className={styles.progressSection}>
                <div className={styles.budgetText}>
                  <Text>Budget</Text>
                  <Text>{formatCurrency(currentPeriod.budget, currentPeriod.currency)}</Text>
                </div>
                <ProgressBar
                  value={currentPeriod.percentageUsed / 100}
                  color={getProgressColor(currentPeriod.percentageUsed)}
                />
                <Text>{currentPeriod.percentageUsed.toFixed(1)}% used</Text>
              </div>
            )}
          </>
        ) : (
          <Text>Loading...</Text>
        )}
      </Card>

      <Card className={styles.card}>
        <Title3>Top Spending Providers</Title3>
        {topProviders.length > 0 ? (
          <div className={styles.providerList}>
            {topProviders.map(([provider, cost]) => (
              <div key={provider} className={styles.providerItem}>
                <span className={styles.providerName}>{provider}</span>
                <span className={styles.providerCost}>
                  {formatCurrency(cost, spendingReport?.currency || 'USD')}
                </span>
              </div>
            ))}
          </div>
        ) : (
          <Text>No spending data available</Text>
        )}
      </Card>

      {spendingReport && (
        <Card className={styles.card}>
          <Title3>Spending by Feature</Title3>
          <div className={styles.providerList}>
            {Object.entries(spendingReport.costByFeature)
              .sort(([, a], [, b]) => b - a)
              .slice(0, 5)
              .map(([feature, cost]) => (
                <div key={feature} className={styles.providerItem}>
                  <span className={styles.providerName}>{feature}</span>
                  <span className={styles.providerCost}>
                    {formatCurrency(cost, spendingReport.currency)}
                  </span>
                </div>
              ))}
          </div>
        </Card>
      )}
    </div>
  );
};
