import {
  Input,
  Button,
  Textarea,
  makeStyles,
  tokens,
  Text,
  Field,
  Slider,
  shorthands,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Link,
} from '@fluentui/react-components';
import {
  SparkleRegular,
  SendRegular,
  WarningRegular,
  ErrorCircleRegular,
  InfoRegular,
} from '@fluentui/react-icons';
import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { ollamaClient } from '../../services/api/ollamaClient';
import { useGlobalLlmStore } from '../../state/globalLlmStore';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
    ...shorthands.padding(tokens.spacingVerticalXXL, tokens.spacingHorizontalXXL),
    backgroundColor: tokens.colorNeutralBackground1,
    ...shorthands.borderRadius(tokens.borderRadiusLarge),
    boxShadow: tokens.shadow4,
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    maxWidth: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalS,
  },
  icon: {
    fontSize: '28px',
    color: tokens.colorBrandForeground1,
  },
  inputSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalXS,
    fontWeight: tokens.fontWeightSemibold,
    letterSpacing: '-0.01em',
  },
  textArea: {
    minHeight: '120px',
  },
  optionalSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalM,
    borderTopWidth: '1px',
    borderTopStyle: 'solid',
    borderTopColor: tokens.colorNeutralStroke2,
  },
  optionalSectionTitle: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalXXS,
  },
  optionalSectionDescription: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalS,
  },
  optionsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    ...shorthands.gap(tokens.spacingHorizontalXL, tokens.spacingVerticalL),
  },
  formField: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  fieldLabel: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalXXS,
  },
  fieldInput: {
    width: '100%',
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalM,
    paddingTop: tokens.spacingVerticalL,
    borderTopWidth: '1px',
    borderTopStyle: 'solid',
    borderTopColor: tokens.colorNeutralStroke2,
    marginTop: tokens.spacingVerticalS,
  },
  sliderField: {
    marginTop: tokens.spacingVerticalM,
    paddingTop: tokens.spacingVerticalM,
    borderTopWidth: '1px',
    borderTopStyle: 'solid',
    borderTopColor: tokens.colorNeutralStroke2,
  },
  sliderValue: {
    marginTop: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  warningBar: {
    marginTop: tokens.spacingVerticalM,
  },
});

interface BrainstormInputProps {
  onBrainstorm: (topic: string, options: BrainstormOptions) => void;
  loading?: boolean;
  ideaCount: number;
  onIdeaCountChange: (value: number) => void;
}

export interface BrainstormOptions {
  audience?: string;
  tone?: string;
  targetDuration?: number;
  platform?: string;
  conceptCount?: number;
  /** LLM provider override (e.g., 'OpenAI', 'Ollama') */
  llmProvider?: string;
  /** LLM model override (e.g., 'gpt-4o', 'llama3.1:8b') */
  llmModel?: string;
}

export const BrainstormInput: React.FC<BrainstormInputProps> = ({
  onBrainstorm,
  loading = false,
  ideaCount,
  onIdeaCountChange,
}) => {
  const styles = useStyles();
  const { selection: globalLlmSelection, modelValidation, setSelection } = useGlobalLlmStore();
  const [topic, setTopic] = useState('');
  const [audience, setAudience] = useState('');
  const [tone, setTone] = useState('');
  const [targetDuration, setTargetDuration] = useState('');
  const [platform, setPlatform] = useState('');
  const [autoDetectionStatus, setAutoDetectionStatus] = useState<{
    attempted: boolean;
    message?: string;
    noModelsInstalled?: boolean;
  }>({ attempted: false });

  // Auto-detect and set default Ollama model on mount
  const autoDetectOllamaModel = useCallback(async () => {
    // Only auto-detect if no provider/model is selected yet
    if (globalLlmSelection?.provider && globalLlmSelection?.modelId) {
      return;
    }

    try {
      const result = await ollamaClient.getRecommendedModel();

      if (result.success && result.recommendedModel) {
        // Auto-select Ollama with the recommended model
        setSelection({
          provider: 'Ollama',
          modelId: result.recommendedModel,
        });
        setAutoDetectionStatus({
          attempted: true,
          message: `Auto-selected model: ${result.recommendedModel}`,
        });
      } else {
        // No models available - show helpful message
        setAutoDetectionStatus({
          attempted: true,
          message: result.message,
          noModelsInstalled: !result.recommendedModel,
        });
      }
    } catch (error: unknown) {
      // Ollama might not be running - this is okay, user can configure manually
      const errorMessage = error instanceof Error ? error.message : String(error);
      console.debug('Ollama auto-detection skipped:', errorMessage);
      setAutoDetectionStatus({
        attempted: true,
        message: 'Ollama is not available. Configure a provider from the toolbar.',
      });
    }
  }, [globalLlmSelection, setSelection]);

  // Run auto-detection on mount only once
  useEffect(() => {
    // Only run once and only if no selection exists
    if (!autoDetectionStatus.attempted && !globalLlmSelection?.provider) {
      autoDetectOllamaModel();
    }
    // Intentionally only depend on autoDetectionStatus.attempted and globalLlmSelection?.provider
    // to avoid re-running when the callback reference changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [autoDetectionStatus.attempted, globalLlmSelection?.provider]);

  // Check if Ollama is selected but no model is chosen
  const isOllamaWithoutModel = useMemo(() => {
    if (!globalLlmSelection?.provider) return false;
    const isOllama = globalLlmSelection.provider.toLowerCase() === 'ollama';
    const hasNoModel = !globalLlmSelection.modelId || globalLlmSelection.modelId.trim() === '';
    return isOllama && hasNoModel;
  }, [globalLlmSelection]);

  // Check if no provider is selected at all
  const noProviderSelected = useMemo(() => {
    return !globalLlmSelection?.provider || globalLlmSelection.provider.trim() === '';
  }, [globalLlmSelection]);

  // Check if the selected model is invalid (validated but not found)
  const isModelInvalid = useMemo(() => {
    // Only consider invalid if validation has been performed
    if (!modelValidation.isValidated) return false;
    return !modelValidation.isValid;
  }, [modelValidation]);

  // Determine if generation should be disabled
  const isGenerationDisabled = useMemo(() => {
    return loading || !topic.trim() || isOllamaWithoutModel || isModelInvalid;
  }, [loading, topic, isOllamaWithoutModel, isModelInvalid]);

  // Get tooltip for disabled button
  const disabledTooltip = useMemo(() => {
    if (!topic.trim()) return 'Enter a topic to generate concepts';
    if (isOllamaWithoutModel) return 'Please select an Ollama model from the toolbar';
    if (isModelInvalid) return modelValidation.errorMessage || 'Selected model is not available';
    return '';
  }, [topic, isOllamaWithoutModel, isModelInvalid, modelValidation.errorMessage]);

  const handleBrainstorm = () => {
    if (!topic.trim()) {
      return;
    }

    const options: BrainstormOptions = {
      audience: audience.trim() || undefined,
      tone: tone.trim() || undefined,
      targetDuration: targetDuration ? parseInt(targetDuration) : undefined,
      platform: platform.trim() || undefined,
      conceptCount: ideaCount,
      // Include global LLM selection if user has made a choice
      llmProvider: globalLlmSelection?.provider || undefined,
      llmModel: globalLlmSelection?.modelId || undefined,
    };

    onBrainstorm(topic.trim(), options);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && e.ctrlKey) {
      handleBrainstorm();
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <SparkleRegular className={styles.icon} />
        <Text size={600} weight="semibold">
          Brainstorm Video Concepts
        </Text>
      </div>

      <div className={styles.inputSection}>
        <Text className={styles.sectionTitle} size={400}>
          What&apos;s your video topic?
        </Text>
        <Textarea
          className={styles.textArea}
          placeholder="Enter your video topic or idea (e.g., 'How to start a successful podcast')"
          value={topic}
          onChange={(e) => setTopic(e.target.value)}
          onKeyDown={handleKeyPress}
          disabled={loading}
          resize="vertical"
        />
      </div>

      <div className={styles.optionalSection}>
        <Text className={styles.optionalSectionTitle} size={400}>
          Optional Details
        </Text>
        <Text className={styles.optionalSectionDescription} size={200}>
          Providing more context helps generate better concepts
        </Text>

        <div className={styles.optionsGrid}>
          <div className={styles.formField}>
            <Text className={styles.fieldLabel}>Target Audience</Text>
            <Input
              className={styles.fieldInput}
              placeholder="e.g., Beginners, Professionals"
              value={audience}
              onChange={(e) => setAudience(e.target.value)}
              disabled={loading}
              size="large"
            />
          </div>

          <div className={styles.formField}>
            <Text className={styles.fieldLabel}>Tone</Text>
            <Input
              className={styles.fieldInput}
              placeholder="e.g., Casual, Professional, Humorous"
              value={tone}
              onChange={(e) => setTone(e.target.value)}
              disabled={loading}
              size="large"
            />
          </div>

          <div className={styles.formField}>
            <Text className={styles.fieldLabel}>Duration (seconds)</Text>
            <Input
              className={styles.fieldInput}
              type="number"
              placeholder="e.g., 60, 300"
              value={targetDuration}
              onChange={(e) => setTargetDuration(e.target.value)}
              disabled={loading}
              size="large"
            />
          </div>

          <div className={styles.formField}>
            <Text className={styles.fieldLabel}>Platform</Text>
            <Input
              className={styles.fieldInput}
              placeholder="e.g., YouTube, TikTok, Instagram"
              value={platform}
              onChange={(e) => setPlatform(e.target.value)}
              disabled={loading}
              size="large"
            />
          </div>
        </div>
      </div>

      <Field
        label="How many ideas should we generate?"
        hint="Choose between 3 and 9 cards per batch"
        className={styles.sliderField}
      >
        <Slider
          min={3}
          max={9}
          step={1}
          value={ideaCount}
          onChange={(_, data) => onIdeaCountChange(data.value)}
          disabled={loading}
        />
        <Text size={200} className={styles.sliderValue}>
          {ideaCount} idea{ideaCount === 1 ? '' : 's'} per refresh
        </Text>
      </Field>

      {/* Warning when Ollama is selected but no model is chosen */}
      {isOllamaWithoutModel && (
        <MessageBar intent="warning" className={styles.warningBar} icon={<WarningRegular />}>
          <MessageBarBody>
            <MessageBarTitle>No Ollama model selected</MessageBarTitle>
            Please select a model from the AI Model dropdown in the toolbar above. If no models are
            available, run <code>ollama list</code> to see installed models or{' '}
            <code>ollama pull &lt;model-name&gt;</code> to install one (e.g., llama3.1, mistral,
            qwen2.5).
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Info when no models are installed in Ollama */}
      {autoDetectionStatus.noModelsInstalled && !isOllamaWithoutModel && (
        <MessageBar intent="info" className={styles.warningBar} icon={<InfoRegular />}>
          <MessageBarBody>
            <MessageBarTitle>No Ollama models installed</MessageBarTitle>
            To get started with local AI:
            <ol style={{ margin: '8px 0 0 0', paddingLeft: '20px' }}>
              <li>
                Ensure Ollama is running: <code>ollama serve</code>
              </li>
              <li>
                Install a model: <code>ollama pull llama3.1:8b</code>
              </li>
              <li>Refresh the model list in the AI Model dropdown above</li>
            </ol>
            <Link
              href="https://ollama.com"
              target="_blank"
              rel="noopener noreferrer"
              style={{ marginTop: '8px', display: 'inline-block' }}
            >
              Download Ollama
            </Link>
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Error when selected model is not available */}
      {isModelInvalid && modelValidation.errorMessage && (
        <MessageBar intent="error" className={styles.warningBar} icon={<ErrorCircleRegular />}>
          <MessageBarBody>
            <MessageBarTitle>Model not available</MessageBarTitle>
            {modelValidation.errorMessage}
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Info when no provider is selected - will use auto-detection */}
      {noProviderSelected && !loading && !autoDetectionStatus.noModelsInstalled && (
        <MessageBar intent="info" className={styles.warningBar}>
          <MessageBarBody>
            <MessageBarTitle>Using default AI provider</MessageBarTitle>
            No AI provider selected. The system will automatically use the best available provider.
            For better control, select a provider and model from the toolbar above.
          </MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.actions}>
        <Button
          appearance="primary"
          icon={<SendRegular />}
          onClick={handleBrainstorm}
          disabled={isGenerationDisabled}
          size="large"
          title={disabledTooltip}
        >
          Generate Concepts
        </Button>
      </div>
    </div>
  );
};
