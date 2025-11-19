import { makeStyles, tokens, Card, Text, Spinner, Tooltip } from '@fluentui/react-components';
import { Info16Regular, Warning16Regular } from '@fluentui/react-icons';
import { useState, useEffect, type FC } from 'react';
import { useCostTrackingStore } from '../../state/costTracking';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  costRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalXS,
  },
  costLabel: {
    color: tokens.colorNeutralForeground3,
  },
  costValue: {
    fontWeight: tokens.fontWeightSemibold,
  },
  totalRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  totalLabel: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  totalValue: {
    fontWeight: tokens.fontWeightBold,
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorBrandForeground1,
  },
  warningRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusSmall,
  },
  warningText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteYellowForeground2,
  },
});

interface CostEstimationDisplayProps {
  providerConfig?: {
    llm?: string;
    tts?: string;
    images?: string;
  };
  estimatedDuration?: number;
  estimatedScenes?: number;
  onCostEstimated?: (totalCost: number, isWithinBudget: boolean) => void;
}

export const CostEstimationDisplay: FC<CostEstimationDisplayProps> = ({
  providerConfig,
  estimatedDuration = 60,
  estimatedScenes = 5,
  onCostEstimated,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [costEstimate, setCostEstimate] = useState<{
    scriptGeneration: number;
    tts: number;
    images: number;
    total: number;
  } | null>(null);
  const [budgetCheck, setBudgetCheck] = useState<{
    isWithinBudget: boolean;
    warnings: string[];
  } | null>(null);

  const { currentPeriodSpending } = useCostTrackingStore();

  useEffect(() => {
    estimateCost();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [providerConfig, estimatedDuration, estimatedScenes]);

  const estimateCost = async () => {
    setLoading(true);
    try {
      const estimatedInputTokens = estimatedDuration * 20;
      const estimatedOutputTokens = estimatedDuration * 50;
      const estimatedCharacters = estimatedDuration * 150;

      const scriptCostResponse = await fetch('/api/cost-tracking/check-budget', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          providerName: providerConfig?.llm || 'OpenAI',
          estimatedInputTokens,
          estimatedOutputTokens,
        }),
      });

      const ttsCostResponse = await fetch('/api/cost-tracking/check-budget', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          providerName: providerConfig?.tts || 'ElevenLabs',
          estimatedCharacters,
        }),
      });

      if (scriptCostResponse.ok && ttsCostResponse.ok) {
        const scriptData = await scriptCostResponse.json();
        const ttsData = await ttsCostResponse.json();

        const scriptCost = scriptData.estimatedNewTotal - scriptData.currentMonthlyCost || 0.1;
        const ttsCost = ttsData.estimatedNewTotal - ttsData.currentMonthlyCost || 0.08;
        const imageCost = estimatedScenes * 0.04;

        const total = scriptCost + ttsCost + imageCost;

        setCostEstimate({
          scriptGeneration: scriptCost,
          tts: ttsCost,
          images: imageCost,
          total,
        });

        const finalBudgetCheck = await fetch('/api/cost-tracking/check-budget', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            providerName: 'Overall',
            estimatedInputTokens: total * 1000,
            estimatedOutputTokens: 0,
          }),
        });

        if (finalBudgetCheck.ok) {
          const budgetData = await finalBudgetCheck.json();
          setBudgetCheck({
            isWithinBudget: budgetData.isWithinBudget,
            warnings: budgetData.warnings || [],
          });

          if (onCostEstimated) {
            onCostEstimated(total, budgetData.isWithinBudget);
          }
        }
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to estimate cost:', errorObj.message);
      setCostEstimate({
        scriptGeneration: 0.1,
        tts: 0.08,
        images: 0.2,
        total: 0.38,
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (value: number) => {
    return `${currentPeriodSpending?.currency || 'USD'} ${value.toFixed(3)}`;
  };

  if (loading) {
    return (
      <Card className={styles.container}>
        <div className={styles.header}>
          <Text className={styles.title}>Estimated Cost</Text>
          <Spinner size="tiny" />
        </div>
      </Card>
    );
  }

  if (!costEstimate) {
    return null;
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>Estimated Cost</Text>
        <Tooltip
          content="Cost estimation based on selected providers and video duration"
          relationship="label"
        >
          <Info16Regular />
        </Tooltip>
      </div>

      <div className={styles.costRow}>
        <Text className={styles.costLabel}>Script Generation</Text>
        <Text className={styles.costValue}>{formatCurrency(costEstimate.scriptGeneration)}</Text>
      </div>

      <div className={styles.costRow}>
        <Text className={styles.costLabel}>Text-to-Speech</Text>
        <Text className={styles.costValue}>{formatCurrency(costEstimate.tts)}</Text>
      </div>

      <div className={styles.costRow}>
        <Text className={styles.costLabel}>Images/Visuals</Text>
        <Text className={styles.costValue}>{formatCurrency(costEstimate.images)}</Text>
      </div>

      <div className={styles.totalRow}>
        <Text className={styles.totalLabel}>Total Estimated Cost</Text>
        <Text className={styles.totalValue}>{formatCurrency(costEstimate.total)}</Text>
      </div>

      {budgetCheck && !budgetCheck.isWithinBudget && (
        <div className={styles.warningRow}>
          <Warning16Regular />
          <Text className={styles.warningText}>
            {budgetCheck.warnings[0] || 'This generation may exceed your budget'}
          </Text>
        </div>
      )}

      {budgetCheck && budgetCheck.isWithinBudget && currentPeriodSpending && (
        <div style={{ marginTop: tokens.spacingVerticalS }}>
          <Text style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 }}>
            Current period: {formatCurrency(currentPeriodSpending.totalCost)} /{' '}
            {formatCurrency(currentPeriodSpending.budget || 0)} (
            {currentPeriodSpending.percentageUsed.toFixed(1)}% used)
          </Text>
        </div>
      )}
    </Card>
  );
};

export default CostEstimationDisplay;
