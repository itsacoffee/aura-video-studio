import {
  Input,
  Button,
  Textarea,
  makeStyles,
  tokens,
  Text,
  Spinner,
  Field,
  Slider,
  shorthands,
} from '@fluentui/react-components';
import { SparkleRegular, SendRegular } from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
import { LlmModelSelector, type LlmSelection } from '../ModelSelection';

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
  modelSelectorSection: {
    paddingTop: tokens.spacingVerticalM,
    borderTopWidth: '1px',
    borderTopStyle: 'solid',
    borderTopColor: tokens.colorNeutralStroke2,
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
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    ...shorthands.padding(tokens.spacingVerticalL),
    justifyContent: 'center',
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

// Local storage key for persisting LLM selection
const LLM_SELECTION_KEY = 'brainstorm-llm-selection';

export const BrainstormInput: React.FC<BrainstormInputProps> = ({
  onBrainstorm,
  loading = false,
  ideaCount,
  onIdeaCountChange,
}) => {
  const styles = useStyles();
  const [topic, setTopic] = useState('');
  const [audience, setAudience] = useState('');
  const [tone, setTone] = useState('');
  const [targetDuration, setTargetDuration] = useState('');
  const [platform, setPlatform] = useState('');

  // Load saved LLM selection from localStorage
  const [llmSelection, setLlmSelection] = useState<LlmSelection>(() => {
    if (typeof window !== 'undefined') {
      try {
        const saved = localStorage.getItem(LLM_SELECTION_KEY);
        if (saved) {
          return JSON.parse(saved) as LlmSelection;
        }
      } catch {
        // Ignore parse errors
      }
    }
    return { provider: '', modelId: '' };
  });

  const handleLlmChange = useCallback((selection: LlmSelection) => {
    setLlmSelection(selection);
    // Persist to localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem(LLM_SELECTION_KEY, JSON.stringify(selection));
    }
  }, []);

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
      // Include LLM selection if user has made a choice
      llmProvider: llmSelection.provider || undefined,
      llmModel: llmSelection.modelId || undefined,
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

      <div className={styles.modelSelectorSection}>
        <LlmModelSelector
          value={llmSelection}
          onChange={handleLlmChange}
          label="AI Model for Brainstorming"
          disabled={loading}
          _featureContext="ideation-brainstorm"
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

      {loading ? (
        <div className={styles.loadingContainer}>
          <Spinner size="small" />
          <Text size={300}>Generating creative concepts...</Text>
        </div>
      ) : (
        <div className={styles.actions}>
          <Button
            appearance="primary"
            icon={<SendRegular />}
            onClick={handleBrainstorm}
            disabled={!topic.trim()}
            size="large"
          >
            Generate Concepts
          </Button>
        </div>
      )}
    </div>
  );
};
