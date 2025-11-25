import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Text,
  Label,
  Slider,
  Textarea,
  Field,
  Checkbox,
  Input,
  Button,
  Divider,
  Tooltip,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Info24Regular,
  ArrowReset24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useState, useMemo } from 'react';
import type { RagConfigurationDto } from '../../services/api/scriptApi';

const useStyles = makeStyles({
  panel: {
    padding: tokens.spacingVerticalXL,
    marginTop: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    animation: 'slideDown 0.3s ease-out',
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalM,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  sectionDescription: {
    marginBottom: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
  },
  sliderGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginBottom: tokens.spacingVerticalL,
  },
  sliderLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXS,
  },
  sliderValue: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  sliderDescription: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalXS,
  },
  sliderRange: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  textareaField: {
    marginBottom: tokens.spacingVerticalM,
  },
  checkboxGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  inputGroup: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  resetButton: {
    marginTop: tokens.spacingVerticalM,
  },
  parametersGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalM,
  },
  infoIcon: {
    color: tokens.colorNeutralForeground3,
    cursor: 'help',
  },
  '@keyframes slideDown': {
    '0%': {
      opacity: 0,
      transform: 'translateY(-10px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
});

interface AdvancedModePanelProps {
  selectedProvider?: string;
  llmParameters: {
    temperature?: number;
    topP?: number;
    topK?: number;
    maxTokens?: number;
    frequencyPenalty?: number;
    presencePenalty?: number;
  };
  ragConfiguration: RagConfigurationDto;
  customInstructions: string;
  onLlmParametersChange: (params: AdvancedModePanelProps['llmParameters']) => void;
  onRagConfigurationChange: (config: RagConfigurationDto) => void;
  onCustomInstructionsChange: (instructions: string) => void;
}

/**
 * Determines which LLM parameters are supported by the selected provider
 */
function getProviderParameterSupport(providerName: string | undefined) {
  if (!providerName || providerName === 'Auto') {
    return {
      supportsTemperature: true,
      supportsTopP: true,
      supportsTopK: true,
      supportsMaxTokens: true,
      supportsFrequencyPenalty: true,
      supportsPresencePenalty: true,
      maxTokensLimit: 4000,
      temperatureRange: { min: 0, max: 2 },
    };
  }

  const name = providerName.toLowerCase();

  // OpenAI and Azure OpenAI
  if (name.includes('openai') || name.includes('azure')) {
    return {
      supportsTemperature: true,
      supportsTopP: true,
      supportsTopK: false,
      supportsMaxTokens: true,
      supportsFrequencyPenalty: true,
      supportsPresencePenalty: true,
      maxTokensLimit: 4096,
      temperatureRange: { min: 0, max: 2 },
    };
  }

  // Anthropic Claude
  if (name.includes('anthropic') || name.includes('claude')) {
    return {
      supportsTemperature: true,
      supportsTopP: true,
      supportsTopK: false,
      supportsMaxTokens: true,
      supportsFrequencyPenalty: false,
      supportsPresencePenalty: false,
      maxTokensLimit: 4096,
      temperatureRange: { min: 0, max: 1 },
    };
  }

  // Google Gemini
  if (name.includes('gemini') || name.includes('google')) {
    return {
      supportsTemperature: true,
      supportsTopP: true,
      supportsTopK: true,
      supportsMaxTokens: true,
      supportsFrequencyPenalty: false,
      supportsPresencePenalty: false,
      maxTokensLimit: 8192,
      temperatureRange: { min: 0, max: 2 },
    };
  }

  // Ollama
  if (name === 'ollama' || name.startsWith('ollama')) {
    return {
      supportsTemperature: true,
      supportsTopP: true,
      supportsTopK: true,
      supportsMaxTokens: true,
      supportsFrequencyPenalty: false,
      supportsPresencePenalty: false,
      maxTokensLimit: 2048,
      temperatureRange: { min: 0, max: 2 },
    };
  }

  // RuleBased
  if (name === 'rulebased' || name === 'rule-based') {
    return {
      supportsTemperature: false,
      supportsTopP: false,
      supportsTopK: false,
      supportsMaxTokens: false,
      supportsFrequencyPenalty: false,
      supportsPresencePenalty: false,
      maxTokensLimit: 0,
      temperatureRange: { min: 0, max: 0 },
    };
  }

  // Default: support all parameters
  return {
    supportsTemperature: true,
    supportsTopP: true,
    supportsTopK: true,
    supportsMaxTokens: true,
    supportsFrequencyPenalty: true,
    supportsPresencePenalty: true,
    maxTokensLimit: 4000,
    temperatureRange: { min: 0, max: 2 },
  };
}

export const AdvancedModePanel: FC<AdvancedModePanelProps> = ({
  selectedProvider,
  llmParameters,
  ragConfiguration,
  customInstructions,
  onLlmParametersChange,
  onRagConfigurationChange,
  onCustomInstructionsChange,
}) => {
  const styles = useStyles();
  const paramSupport = useMemo(
    () => getProviderParameterSupport(selectedProvider),
    [selectedProvider]
  );

  const handleResetLlmParameters = () => {
    onLlmParametersChange({
      temperature: undefined,
      topP: undefined,
      topK: undefined,
      maxTokens: undefined,
      frequencyPenalty: undefined,
      presencePenalty: undefined,
    });
  };

  const handleResetRag = () => {
    onRagConfigurationChange({
      enabled: false,
      topK: 5,
      minimumScore: 0.6,
      maxContextTokens: 2000,
      includeCitations: true,
      tightenClaims: false,
    });
  };

  return (
    <Card className={styles.panel}>
      <Accordion collapsible defaultOpenItems={['llm', 'rag', 'instructions']}>
        {/* LLM Parameters Section */}
        <AccordionItem value="llm">
          <AccordionHeader>
            <Title3>LLM Generation Parameters</Title3>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text className={styles.sectionDescription}>
                Fine-tune how the LLM generates your script. These parameters control creativity,
                randomness, and output length. Only parameters supported by your selected provider
                are shown.
                {selectedProvider && selectedProvider !== 'Auto' && (
                  <Text weight="semibold" style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                    Provider: {selectedProvider}
                  </Text>
                )}
              </Text>

              {!paramSupport.supportsTemperature &&
                !paramSupport.supportsTopP &&
                !paramSupport.supportsTopK &&
                !paramSupport.supportsMaxTokens && (
                  <div
                    style={{
                      padding: tokens.spacingVerticalM,
                      backgroundColor: tokens.colorNeutralBackground3,
                      borderRadius: tokens.borderRadiusMedium,
                    }}
                  >
                    <Text size={300} weight="semibold" style={{ color: tokens.colorNeutralForeground2 }}>
                      {selectedProvider?.toLowerCase().includes('rule')
                        ? 'Rule-based provider does not support LLM parameters. It uses template-based generation.'
                        : 'No advanced parameters available for this provider.'}
                    </Text>
                  </div>
                )}

              <div className={styles.parametersGrid}>
                {paramSupport.supportsTemperature && (
                  <div className={styles.sliderGroup}>
                    <div className={styles.sliderLabel}>
                      <Label htmlFor="temperature-slider">Temperature</Label>
                      <Text className={styles.sliderValue}>
                        {llmParameters.temperature !== undefined
                          ? llmParameters.temperature.toFixed(2)
                          : 'Auto (0.7)'}
                      </Text>
                    </div>
                    <Tooltip
                      content={`Controls randomness: ${paramSupport.temperatureRange.min} = deterministic, ${paramSupport.temperatureRange.max} = very creative`}
                      relationship="label"
                    >
                      <Text className={styles.sliderDescription}>
                        Controls randomness
                        <Info24Regular className={styles.infoIcon} style={{ marginLeft: tokens.spacingHorizontalXS, verticalAlign: 'middle' }} />
                      </Text>
                    </Tooltip>
                    <Slider
                      id="temperature-slider"
                      min={paramSupport.temperatureRange.min}
                      max={paramSupport.temperatureRange.max}
                      step={0.1}
                      value={llmParameters.temperature ?? 0.7}
                      onChange={(_, data) =>
                        onLlmParametersChange({
                          ...llmParameters,
                          temperature: data.value === 0.7 ? undefined : data.value,
                        })
                      }
                    />
                    <div className={styles.sliderRange}>
                      <Text size={200}>{paramSupport.temperatureRange.min}</Text>
                      <Text size={200}>{paramSupport.temperatureRange.max}</Text>
                    </div>
                  </div>
                )}

                {paramSupport.supportsTopP && (
                  <div className={styles.sliderGroup}>
                    <div className={styles.sliderLabel}>
                      <Label htmlFor="topP-slider">Top P (Nucleus Sampling)</Label>
                      <Text className={styles.sliderValue}>
                        {llmParameters.topP !== undefined ? llmParameters.topP.toFixed(2) : 'Auto (0.9)'}
                      </Text>
                    </div>
                    <Tooltip
                      content="Controls diversity by considering only tokens with cumulative probability mass up to this value"
                      relationship="label"
                    >
                      <Text className={styles.sliderDescription}>
                        Nucleus sampling threshold
                        <Info24Regular className={styles.infoIcon} style={{ marginLeft: tokens.spacingHorizontalXS, verticalAlign: 'middle' }} />
                      </Text>
                    </Tooltip>
                    <Slider
                      id="topP-slider"
                      min={0}
                      max={1}
                      step={0.05}
                      value={llmParameters.topP ?? 0.9}
                      onChange={(_, data) =>
                        onLlmParametersChange({
                          ...llmParameters,
                          topP: data.value === 0.9 ? undefined : data.value,
                        })
                      }
                    />
                    <div className={styles.sliderRange}>
                      <Text size={200}>0.0</Text>
                      <Text size={200}>1.0</Text>
                    </div>
                  </div>
                )}

                {paramSupport.supportsTopK && (
                  <div className={styles.sliderGroup}>
                    <div className={styles.sliderLabel}>
                      <Label htmlFor="topK-slider">Top K</Label>
                      <Text className={styles.sliderValue}>
                        {llmParameters.topK !== undefined ? llmParameters.topK : 'Auto (40)'}
                      </Text>
                    </div>
                    <Tooltip
                      content="Limits sampling to the top K most likely tokens. Only supported by Gemini and Ollama."
                      relationship="label"
                    >
                      <Text className={styles.sliderDescription}>
                        Top K tokens to consider
                        <Info24Regular className={styles.infoIcon} style={{ marginLeft: tokens.spacingHorizontalXS, verticalAlign: 'middle' }} />
                      </Text>
                    </Tooltip>
                    <Slider
                      id="topK-slider"
                      min={0}
                      max={100}
                      step={1}
                      value={llmParameters.topK ?? 40}
                      onChange={(_, data) =>
                        onLlmParametersChange({
                          ...llmParameters,
                          topK: data.value === 40 ? undefined : data.value,
                        })
                      }
                    />
                    <div className={styles.sliderRange}>
                      <Text size={200}>0</Text>
                      <Text size={200}>100</Text>
                    </div>
                  </div>
                )}

                {paramSupport.supportsMaxTokens && (
                  <div className={styles.sliderGroup}>
                    <div className={styles.sliderLabel}>
                      <Label htmlFor="maxTokens-slider">Max Tokens</Label>
                      <Text className={styles.sliderValue}>
                        {llmParameters.maxTokens !== undefined
                          ? llmParameters.maxTokens.toLocaleString()
                          : 'Auto (2000)'}
                      </Text>
                    </div>
                    <Tooltip
                      content={`Maximum number of tokens to generate. Limit: ${paramSupport.maxTokensLimit.toLocaleString()}`}
                      relationship="label"
                    >
                      <Text className={styles.sliderDescription}>
                        Maximum output length
                        <Info24Regular className={styles.infoIcon} style={{ marginLeft: tokens.spacingHorizontalXS, verticalAlign: 'middle' }} />
                      </Text>
                    </Tooltip>
                    <Slider
                      id="maxTokens-slider"
                      min={100}
                      max={paramSupport.maxTokensLimit}
                      step={paramSupport.maxTokensLimit > 2000 ? 100 : 50}
                      value={llmParameters.maxTokens ?? Math.min(2000, paramSupport.maxTokensLimit)}
                      onChange={(_, data) => {
                        const defaultValue = Math.min(2000, paramSupport.maxTokensLimit);
                        onLlmParametersChange({
                          ...llmParameters,
                          maxTokens: data.value === defaultValue ? undefined : data.value,
                        });
                      }}
                    />
                    <div className={styles.sliderRange}>
                      <Text size={200}>100</Text>
                      <Text size={200}>{paramSupport.maxTokensLimit.toLocaleString()}</Text>
                    </div>
                  </div>
                )}

                {paramSupport.supportsFrequencyPenalty && (
                  <div className={styles.sliderGroup}>
                    <div className={styles.sliderLabel}>
                      <Label htmlFor="frequencyPenalty-slider">Frequency Penalty</Label>
                      <Text className={styles.sliderValue}>
                        {llmParameters.frequencyPenalty !== undefined
                          ? llmParameters.frequencyPenalty.toFixed(2)
                          : 'Auto (0.0)'}
                      </Text>
                    </div>
                    <Tooltip
                      content="Reduces repetition by penalizing tokens based on how frequently they appear. Only supported by OpenAI/Azure."
                      relationship="label"
                    >
                      <Text className={styles.sliderDescription}>
                        Reduces repetition
                        <Info24Regular className={styles.infoIcon} style={{ marginLeft: tokens.spacingHorizontalXS, verticalAlign: 'middle' }} />
                      </Text>
                    </Tooltip>
                    <Slider
                      id="frequencyPenalty-slider"
                      min={-2}
                      max={2}
                      step={0.1}
                      value={llmParameters.frequencyPenalty ?? 0}
                      onChange={(_, data) =>
                        onLlmParametersChange({
                          ...llmParameters,
                          frequencyPenalty: data.value === 0 ? undefined : data.value,
                        })
                      }
                    />
                    <div className={styles.sliderRange}>
                      <Text size={200}>-2.0</Text>
                      <Text size={200}>2.0</Text>
                    </div>
                  </div>
                )}

                {paramSupport.supportsPresencePenalty && (
                  <div className={styles.sliderGroup}>
                    <div className={styles.sliderLabel}>
                      <Label htmlFor="presencePenalty-slider">Presence Penalty</Label>
                      <Text className={styles.sliderValue}>
                        {llmParameters.presencePenalty !== undefined
                          ? llmParameters.presencePenalty.toFixed(2)
                          : 'Auto (0.0)'}
                      </Text>
                    </div>
                    <Tooltip
                      content="Encourages new topics by penalizing tokens that have already appeared. Only supported by OpenAI/Azure."
                      relationship="label"
                    >
                      <Text className={styles.sliderDescription}>
                        Encourages new topics
                        <Info24Regular className={styles.infoIcon} style={{ marginLeft: tokens.spacingHorizontalXS, verticalAlign: 'middle' }} />
                      </Text>
                    </Tooltip>
                    <Slider
                      id="presencePenalty-slider"
                      min={-2}
                      max={2}
                      step={0.1}
                      value={llmParameters.presencePenalty ?? 0}
                      onChange={(_, data) =>
                        onLlmParametersChange({
                          ...llmParameters,
                          presencePenalty: data.value === 0 ? undefined : data.value,
                        })
                      }
                    />
                    <div className={styles.sliderRange}>
                      <Text size={200}>-2.0</Text>
                      <Text size={200}>2.0</Text>
                    </div>
                  </div>
                )}
              </div>

              {(paramSupport.supportsTemperature ||
                paramSupport.supportsTopP ||
                paramSupport.supportsTopK ||
                paramSupport.supportsMaxTokens) && (
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<ArrowReset24Regular />}
                  onClick={handleResetLlmParameters}
                  className={styles.resetButton}
                >
                  Reset to Defaults
                </Button>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>

        <Divider />

        {/* RAG Configuration Section */}
        <AccordionItem value="rag">
          <AccordionHeader>
            <Title3>RAG (Retrieval-Augmented Generation)</Title3>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text className={styles.sectionDescription}>
                Configure how the system retrieves and uses relevant documents from your knowledge
                base to ground script generation in factual information.
              </Text>

              <Field className={styles.textareaField}>
                <Checkbox
                  checked={ragConfiguration.enabled}
                  onChange={(_, data) =>
                    onRagConfigurationChange({
                      ...ragConfiguration,
                      enabled: data.checked ?? false,
                    })
                  }
                  label="Enable RAG for script generation"
                />
                <Text size={200} style={{ color: tokens.colorNeutralForeground3, marginTop: tokens.spacingVerticalXS }}>
                  When enabled, the system will search your document library for relevant information
                  to include in the script generation context.
                </Text>
              </Field>

              {ragConfiguration.enabled && (
                <>
                  <div className={styles.inputGroup}>
                    <Field label="Top K Results" hint="Number of document chunks to retrieve (default: 5)">
                      <Input
                        type="number"
                        min={1}
                        max={20}
                        value={ragConfiguration.topK?.toString() ?? '5'}
                        onChange={(_, data) =>
                          onRagConfigurationChange({
                            ...ragConfiguration,
                            topK: data.value ? parseInt(data.value, 10) : 5,
                          })
                        }
                      />
                    </Field>

                    <Field
                      label="Minimum Score"
                      hint="Minimum similarity score for chunks (0.0-1.0, default: 0.6)"
                    >
                      <Input
                        type="number"
                        min={0}
                        max={1}
                        step={0.1}
                        value={ragConfiguration.minimumScore?.toString() ?? '0.6'}
                        onChange={(_, data) =>
                          onRagConfigurationChange({
                            ...ragConfiguration,
                            minimumScore: data.value ? parseFloat(data.value) : 0.6,
                          })
                        }
                      />
                    </Field>

                    <Field
                      label="Max Context Tokens"
                      hint="Maximum tokens to include from retrieved chunks (default: 2000)"
                    >
                      <Input
                        type="number"
                        min={100}
                        max={8000}
                        step={100}
                        value={ragConfiguration.maxContextTokens?.toString() ?? '2000'}
                        onChange={(_, data) =>
                          onRagConfigurationChange({
                            ...ragConfiguration,
                            maxContextTokens: data.value ? parseInt(data.value, 10) : 2000,
                          })
                        }
                      />
                    </Field>
                  </div>

                  <div className={styles.checkboxGroup}>
                    <Checkbox
                      checked={ragConfiguration.includeCitations ?? true}
                      onChange={(_, data) =>
                        onRagConfigurationChange({
                          ...ragConfiguration,
                          includeCitations: data.checked ?? true,
                        })
                      }
                      label="Include citations in generated script"
                    />
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      When enabled, the script will include references to source documents.
                    </Text>

                    <Checkbox
                      checked={ragConfiguration.tightenClaims ?? false}
                      onChange={(_, data) =>
                        onRagConfigurationChange({
                          ...ragConfiguration,
                          tightenClaims: data.checked ?? false,
                        })
                      }
                      label="Tighten claims (use only high-confidence information)"
                    />
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      When enabled, only information with high confidence scores will be used.
                    </Text>
                  </div>

                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<ArrowReset24Regular />}
                    onClick={handleResetRag}
                    className={styles.resetButton}
                  >
                    Reset to Defaults
                  </Button>
                </>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>

        <Divider />

        {/* Custom Instructions Section */}
        <AccordionItem value="instructions">
          <AccordionHeader>
            <Title3>Custom Instructions</Title3>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text className={styles.sectionDescription}>
                Provide additional instructions to guide the LLM during script generation. These
                instructions will be included in the system prompt to steer the generation process.
              </Text>

              <Field className={styles.textareaField} label="Additional Instructions">
                <Textarea
                  placeholder="e.g., 'Focus on technical accuracy', 'Use a conversational tone', 'Include specific examples from the industry'..."
                  value={customInstructions}
                  onChange={(_, data) => onCustomInstructionsChange(data.value)}
                  rows={6}
                  resize="vertical"
                />
                <Text size={200} style={{ color: tokens.colorNeutralForeground3, marginTop: tokens.spacingVerticalXS }}>
                  These instructions will be combined with the standard prompt to customize script
                  generation behavior.
                </Text>
              </Field>

              {customInstructions && (
                <Button
                  appearance="subtle"
                  size="small"
                  onClick={() => onCustomInstructionsChange('')}
                  className={styles.resetButton}
                >
                  Clear Instructions
                </Button>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </Card>
  );
};

