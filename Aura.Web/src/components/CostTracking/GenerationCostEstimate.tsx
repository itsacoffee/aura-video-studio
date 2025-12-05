import {
  Badge,
  Card,
  Divider,
  makeStyles,
  Spinner,
  Text,
  Title3,
  tokens,
  Tooltip,
} from '@fluentui/react-components';
import { CheckmarkCircle16Regular, Info16Regular, Warning16Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { useEffect, useState, useCallback } from 'react';
import apiClient from '@/services/api/apiClient';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusLarge,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  freeTag: {
    marginLeft: tokens.spacingHorizontalS,
  },
  breakdownItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: `${tokens.spacingVerticalS} 0`,
  },
  breakdownLabel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  providerName: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  freeText: {
    color: tokens.colorPaletteGreenForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  costText: {
    fontWeight: tokens.fontWeightSemibold,
  },
  totalRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalM,
  },
  totalLabel: {
    fontWeight: tokens.fontWeightBold,
    fontSize: tokens.fontSizeBase400,
  },
  totalValue: {
    fontWeight: tokens.fontWeightBold,
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorBrandForeground1,
  },
  freeTotal: {
    color: tokens.colorPaletteGreenForeground1,
  },
  warningCard: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground2,
    flexShrink: 0,
  },
  warningText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteYellowForeground2,
  },
  successCard: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteGreenBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
    flexShrink: 0,
  },
  successText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteGreenForeground1,
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingHorizontalS,
  },
  confidenceNote: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
    fontStyle: 'italic',
  },
});

interface CostBreakdownItem {
  name: string;
  provider: string;
  cost: number;
  isFree: boolean;
  units: number;
  unitType: string;
}

interface BudgetCheck {
  isWithinBudget: boolean;
  shouldBlock: boolean;
  warnings: string[];
  currentMonthlyCost: number;
  estimatedNewTotal: number;
}

interface GenerationCostEstimateResponse {
  llmCost: number;
  ttsCost: number;
  imageCost: number;
  totalCost: number;
  currency: string;
  breakdown: CostBreakdownItem[];
  isFreeGeneration: boolean;
  confidence: 'high' | 'medium' | 'low';
  budgetCheck?: BudgetCheck;
}

export interface GenerationCostEstimateProps {
  /**
   * Estimated script length in characters
   */
  estimatedScriptLength: number;

  /**
   * Number of scenes to generate
   */
  sceneCount: number;

  /**
   * LLM provider name (e.g., "OpenAI", "Ollama")
   */
  llmProvider: string;

  /**
   * LLM model name (e.g., "gpt-4o-mini")
   */
  llmModel: string;

  /**
   * TTS provider name (e.g., "ElevenLabs", "Piper")
   */
  ttsProvider: string;

  /**
   * Image provider name (optional, e.g., "Pexels", "Placeholder")
   */
  imageProvider?: string;

  /**
   * Callback when cost estimation completes
   */
  onCostEstimated?: (estimate: GenerationCostEstimateResponse) => void;
}

/**
 * Component to display estimated costs before video generation.
 * Shows breakdown by provider/stage with free provider indicators.
 */
export const GenerationCostEstimate: FC<GenerationCostEstimateProps> = ({
  estimatedScriptLength,
  sceneCount,
  llmProvider,
  llmModel,
  ttsProvider,
  imageProvider,
  onCostEstimated,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [estimate, setEstimate] = useState<GenerationCostEstimateResponse | null>(null);
  const [isUnavailable, setIsUnavailable] = useState(false);

  const fetchEstimate = useCallback(async () => {
    setLoading(true);
    setError(null);
    setIsUnavailable(false);

    try {
      const response = await apiClient.post<GenerationCostEstimateResponse>(
        '/api/cost-tracking/estimate-generation',
        {
          estimatedScriptLength,
          sceneCount,
          llmProvider,
          llmModel,
          ttsProvider,
          imageProvider,
        }
      );

      setEstimate(response.data);
      onCostEstimated?.(response.data);
    } catch (err: unknown) {
      // Don't show error for cost estimation failures - show fallback free generation estimate
      // This handles 428 Precondition Required and other API errors gracefully
      const errorMessage = err instanceof Error ? err.message : 'Failed to estimate costs';
      console.warn('Cost estimation unavailable:', errorMessage);

      // Provide a fallback estimate indicating cost estimation is unavailable
      const fallbackEstimate: GenerationCostEstimateResponse = {
        llmCost: 0,
        ttsCost: 0,
        imageCost: 0,
        totalCost: 0,
        currency: 'USD',
        breakdown: [],
        isFreeGeneration: true,
        confidence: 'low',
      };
      setEstimate(fallbackEstimate);
      onCostEstimated?.(fallbackEstimate);
      setError(null);
      setIsUnavailable(true);
    } finally {
      setLoading(false);
    }
  }, [
    estimatedScriptLength,
    sceneCount,
    llmProvider,
    llmModel,
    ttsProvider,
    imageProvider,
    onCostEstimated,
  ]);

  useEffect(() => {
    // Debounce fetch to avoid too many API calls
    const timer = setTimeout(() => {
      void fetchEstimate();
    }, 300);

    return () => clearTimeout(timer);
  }, [fetchEstimate]);

  const formatCost = (cost: number, _currency: string = 'USD'): string => {
    if (cost === 0) {
      return 'Free';
    }
    return `$${cost.toFixed(4)}`;
  };

  const getConfidenceTooltip = (confidence: string): string => {
    switch (confidence) {
      case 'high':
        return 'Based on current pricing data - estimate is accurate';
      case 'medium':
        return 'Some assumptions made - actual cost may vary slightly';
      case 'low':
        return 'Significant assumptions used - actual cost may vary';
      default:
        return 'Cost estimate';
    }
  };

  if (loading) {
    return (
      <Card className={styles.container}>
        <div className={styles.loadingContainer}>
          <Spinner size="small" />
          <Text>Calculating estimated costs...</Text>
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={styles.container}>
        <div className={styles.warningCard}>
          <Warning16Regular className={styles.warningIcon} />
          <Text className={styles.warningText}>Could not estimate costs: {error}</Text>
        </div>
      </Card>
    );
  }

  if (!estimate) {
    return null;
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <Title3>Estimated Cost</Title3>
          {estimate.isFreeGeneration && (
            <Badge appearance="filled" color="success" className={styles.freeTag}>
              Free Generation
            </Badge>
          )}
        </div>
        <Tooltip content={getConfidenceTooltip(estimate.confidence)} relationship="label">
          <Info16Regular />
        </Tooltip>
      </div>

      {estimate.breakdown.map((item, index) => (
        <div key={index} className={styles.breakdownItem}>
          <div className={styles.breakdownLabel}>
            <Text>{item.name}</Text>
            <Text className={styles.providerName}>
              {item.provider} • {item.units.toLocaleString()} {item.unitType}
            </Text>
          </div>
          {item.isFree ? (
            <Text className={styles.freeText}>Free (local)</Text>
          ) : (
            <Text className={styles.costText}>{formatCost(item.cost, estimate.currency)}</Text>
          )}
        </div>
      ))}

      <Divider style={{ margin: `${tokens.spacingVerticalM} 0` }} />

      <div className={styles.totalRow}>
        <Text className={styles.totalLabel}>Total Estimate</Text>
        <Text
          className={`${styles.totalValue} ${estimate.isFreeGeneration ? styles.freeTotal : ''}`}
        >
          {estimate.isFreeGeneration ? 'Free' : formatCost(estimate.totalCost, estimate.currency)}
        </Text>
      </div>

      {estimate.confidence !== 'high' && (
        <Text className={styles.confidenceNote}>* Estimate confidence: {estimate.confidence}</Text>
      )}

      {estimate.budgetCheck && !estimate.budgetCheck.isWithinBudget && (
        <div className={styles.warningCard}>
          <Warning16Regular className={styles.warningIcon} />
          <div>
            <Text className={styles.warningText} weight="semibold">
              Budget Warning
            </Text>
            {estimate.budgetCheck.warnings.map((warning, index) => (
              <Text key={index} className={styles.warningText} block>
                {warning}
              </Text>
            ))}
          </div>
        </div>
      )}

      {estimate.budgetCheck &&
        estimate.budgetCheck.isWithinBudget &&
        !estimate.isFreeGeneration && (
          <div className={styles.successCard}>
            <CheckmarkCircle16Regular className={styles.successIcon} />
            <Text className={styles.successText}>
              Within budget (Current: ${estimate.budgetCheck.currentMonthlyCost.toFixed(2)} →
              Estimated: ${estimate.budgetCheck.estimatedNewTotal.toFixed(2)})
            </Text>
          </div>
        )}

      {estimate.isFreeGeneration && !isUnavailable && (
        <div className={styles.successCard}>
          <CheckmarkCircle16Regular className={styles.successIcon} />
          <Text className={styles.successText}>All providers are local/free - no API costs</Text>
        </div>
      )}

      {isUnavailable && (
        <div className={styles.confidenceNote}>
          Cost estimation unavailable - using local/free providers by default
        </div>
      )}
    </Card>
  );
};

export default GenerationCostEstimate;
