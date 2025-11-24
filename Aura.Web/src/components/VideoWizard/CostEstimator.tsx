import {
  Badge,
  Button,
  Divider,
  makeStyles,
  Popover,
  PopoverSurface,
  PopoverTrigger,
  Text,
  Title3,
  tokens,
} from '@fluentui/react-components';
import { Money24Regular, Warning24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import { useMemo } from 'react';
import type { CostBreakdown, WizardData } from './types';

const useStyles = makeStyles({
  trigger: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  content: {
    minWidth: '320px',
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  totalRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalM,
    paddingBottom: tokens.spacingVerticalM,
  },
  breakdownRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalS,
    paddingBottom: tokens.spacingVerticalS,
  },
  breakdownLabel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  warning: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  optimization: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
});

interface CostEstimatorProps {
  wizardData: WizardData;
  selectedLlmProvider?: string;
  budgetLimit?: number;
}

const COST_RATES = {
  llm: {
    OpenAI: { inputTokens: 0.0015 / 1000, outputTokens: 0.002 / 1000 },
    Claude: { inputTokens: 0.008 / 1000, outputTokens: 0.024 / 1000 },
    Anthropic: { inputTokens: 0.008 / 1000, outputTokens: 0.024 / 1000 },
    Gemini: { inputTokens: 0.0005 / 1000, outputTokens: 0.0015 / 1000 },
    Azure: { inputTokens: 0.0015 / 1000, outputTokens: 0.002 / 1000 },
    Ollama: { inputTokens: 0, outputTokens: 0 },
    RuleBased: { inputTokens: 0, outputTokens: 0 },
  },
  tts: {
    ElevenLabs: 0.00018,
    PlayHT: 0.00012,
    Windows: 0,
    Piper: 0,
    Mimic3: 0,
  },
  images: {
    StableDiffusion: 0,
    DALLE: 0.02,
    'DALL-E3': 0.02,
    Midjourney: 0.04,
    Stock: 0,
    Pexels: 0,
    Pixabay: 0,
    Unsplash: 0,
    PlaceholderImages: 0,
  },
};

export const CostEstimator: FC<CostEstimatorProps> = ({ wizardData, selectedLlmProvider, budgetLimit = 5.0 }) => {
  const styles = useStyles();

  const costBreakdown = useMemo((): CostBreakdown => {
    const breakdown: CostBreakdown['breakdown'] = [];

    const scriptLength = wizardData.script.content.length || wizardData.brief.topic.length * 50;
    const estimatedTokens = Math.ceil(scriptLength / 4);
    const sceneCount = wizardData.script.scenes.length || Math.ceil(wizardData.brief.duration / 10);
    const audioCharacters = scriptLength || 500;

    // Determine which LLM provider is being used
    // Check script metadata first (from actual generation), then selected provider, then default to free
    // Script metadata is the most accurate as it reflects what was actually used
    const scriptMetadata = (wizardData.script as any)?.metadata;
    const llmProviderName = scriptMetadata?.providerName ||
                           selectedLlmProvider?.split('(')[0]?.trim() ||
                           'Ollama';

    // Normalize provider name (handle "Ollama (qwen3:4b)" -> "Ollama")
    const normalizedProvider = llmProviderName.split('(')[0].trim();
    const llmRates = COST_RATES.llm[normalizedProvider as keyof typeof COST_RATES.llm] ||
                    COST_RATES.llm.Ollama; // Default to free if unknown

    // Calculate LLM cost based on actual provider rates
    // Assume 50/50 input/output token split
    const llmInputCost = (estimatedTokens * 0.5) * (llmRates.inputTokens || 0);
    const llmOutputCost = (estimatedTokens * 0.5) * (llmRates.outputTokens || 0);
    const llmCost = llmInputCost + llmOutputCost;

    if (llmCost > 0) {
      // Calculate average cost per token accounting for 50/50 split
      // This ensures units * costPerUnit = actual cost
      const averageCostPerToken = 0.5 * ((llmRates.inputTokens || 0) + (llmRates.outputTokens || 0));

      breakdown.push({
        provider: normalizedProvider,
        service: 'Script Generation',
        units: estimatedTokens,
        costPerUnit: averageCostPerToken,
        subtotal: llmCost,
      });
    }

    // Calculate TTS cost - only if provider is configured and not free
    const voiceProvider = wizardData.style.voiceProvider;
    const ttsRate = COST_RATES.tts[voiceProvider as keyof typeof COST_RATES.tts] ?? 0;
    const ttsCost = audioCharacters * ttsRate;
    if (ttsCost > 0 && voiceProvider) {
      breakdown.push({
        provider: voiceProvider,
        service: 'Text-to-Speech',
        units: audioCharacters,
        costPerUnit: ttsRate,
        subtotal: ttsCost,
      });
    }

    // Calculate image generation cost - check actual image provider, not visual style
    // Only charge if using a paid image provider (not free stock/placeholder services)
    const imageProvider = wizardData.style.imageProvider || 'Placeholder';
    const imageRate = COST_RATES.images[imageProvider as keyof typeof COST_RATES.images] ?? 0;
    const imageGenerationCost = sceneCount * imageRate;
    if (imageGenerationCost > 0 && imageProvider) {
      breakdown.push({
        provider: imageProvider === 'Stock' ? 'Stock Images' : imageProvider,
        service: 'Scene Visuals',
        units: sceneCount,
        costPerUnit: imageRate,
        subtotal: imageGenerationCost,
      });
    }

    const totalCost = llmCost + ttsCost + imageGenerationCost;

    return {
      llmCost,
      ttsCost,
      imageGenerationCost,
      totalCost,
      breakdown,
    };
  }, [wizardData, selectedLlmProvider]);

  const exceedsBudget = costBreakdown.totalCost > budgetLimit;

  const optimizationSuggestions = useMemo(() => {
    const suggestions: string[] = [];
    if (costBreakdown.ttsCost > 0) {
      suggestions.push('Use Windows TTS (free) instead of premium voices');
    }
    if (costBreakdown.imageGenerationCost > 0) {
      const imageProvider = wizardData.style.imageProvider || 'Placeholder';
      if (imageProvider !== 'Pexels' && imageProvider !== 'Pixabay' && imageProvider !== 'Unsplash' && imageProvider !== 'Placeholder') {
        suggestions.push('Use Pexels, Pixabay, or Unsplash (free) instead of AI image generation');
      }
    }
    if (costBreakdown.llmCost > 0) {
      suggestions.push('Use Ollama (free, local) for script generation');
    }
    return suggestions;
  }, [costBreakdown, wizardData.style.imageProvider]);

  return (
    <Popover trapFocus positioning="below-end">
      <PopoverTrigger disableButtonEnhancement>
        <Button appearance="subtle" icon={<Money24Regular />} className={styles.trigger}>
          <Text weight="semibold">${costBreakdown.totalCost.toFixed(3)}</Text>
          {exceedsBudget && (
            <Badge appearance="filled" color="danger" shape="rounded">
              Over Budget
            </Badge>
          )}
        </Button>
      </PopoverTrigger>

      <PopoverSurface className={styles.content}>
        <Title3>Cost Estimate</Title3>
        <Divider />

        <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS }}>
          {costBreakdown.breakdown.map((item, index) => (
            <div key={index} className={styles.breakdownRow}>
              <div className={styles.breakdownLabel}>
                <Text weight="semibold" size={300}>
                  {item.provider}
                </Text>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  {item.units}{' '}
                  {item.service === 'Script Generation'
                    ? 'tokens'
                    : item.service === 'Text-to-Speech'
                      ? 'characters'
                      : item.service === 'Scene Visuals'
                        ? 'images'
                        : 'units'}
                </Text>
              </div>
              <Text weight="semibold">${item.subtotal.toFixed(3)}</Text>
            </div>
          ))}
        </div>

        <Divider />

        <div className={styles.totalRow}>
          <Text weight="bold" size={400}>
            Total Estimated Cost
          </Text>
          <Text weight="bold" size={400}>
            ${costBreakdown.totalCost.toFixed(3)}
          </Text>
        </div>

        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
          Budget limit: ${budgetLimit.toFixed(2)}
        </Text>

        {exceedsBudget && (
          <div className={styles.warning}>
            <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
            <div>
              <Text weight="semibold" size={300}>
                Exceeds Budget
              </Text>
              <Text size={200}>
                This configuration will cost ${(costBreakdown.totalCost - budgetLimit).toFixed(3)}{' '}
                more than your budget.
              </Text>
            </div>
          </div>
        )}

        {exceedsBudget && optimizationSuggestions.length > 0 && (
          <div className={styles.optimization}>
            <Text weight="semibold" size={300}>
              Cost Optimizations
            </Text>
            {optimizationSuggestions.map((suggestion, index) => (
              <Text key={index} size={200} style={{ marginLeft: tokens.spacingHorizontalM }}>
                • {suggestion}
              </Text>
            ))}
          </div>
        )}
      </PopoverSurface>
    </Popover>
  );
};
