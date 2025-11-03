import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Slider,
  Field,
  Input,
  Textarea,
  Switch,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
} from '@fluentui/react-components';
import { Add24Regular, Delete24Regular, ArrowReset24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import { useUserPreferencesStore } from '../../state/userPreferences';
import type { AIBehaviorSettings, LLMStageParameters } from '../../state/userPreferences';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  settingsCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  settingItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    ':last-child': {
      borderBottom: 'none',
    },
  },
  settingInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  settingActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  formGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  fullWidth: {
    gridColumn: '1 / -1',
  },
  sliderContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sliderLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  stageSection: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
});

interface AIBehaviorSettingsEditorProps {
  settings: AIBehaviorSettings;
  onChange: (settings: AIBehaviorSettings) => void;
  onSave: () => void;
  onCancel: () => void;
  isNew?: boolean;
}

const AIBehaviorSettingsEditor: FC<AIBehaviorSettingsEditorProps> = ({
  settings,
  onChange,
  onSave,
  onCancel,
  isNew = false,
}) => {
  const styles = useStyles();

  const updateStage = (
    stage: keyof Pick<
      AIBehaviorSettings,
      | 'scriptGeneration'
      | 'sceneDescription'
      | 'contentOptimization'
      | 'translation'
      | 'qualityAnalysis'
    >,
    params: Partial<LLMStageParameters>
  ) => {
    onChange({
      ...settings,
      [stage]: { ...settings[stage], ...params },
    });
  };

  const renderStageParameters = (
    title: string,
    stage: keyof Pick<
      AIBehaviorSettings,
      | 'scriptGeneration'
      | 'sceneDescription'
      | 'contentOptimization'
      | 'translation'
      | 'qualityAnalysis'
    >
  ) => {
    const params = settings[stage];
    return (
      <div className={styles.stageSection}>
        <Text weight="semibold" size={400} style={{ marginBottom: tokens.spacingVerticalM }}>
          {title}
        </Text>
        <div className={styles.formGrid}>
          <Field label="Temperature">
            <div className={styles.sliderContainer}>
              <div className={styles.sliderLabel}>
                <Text size={200}>Randomness/Creativity</Text>
                <Text size={200} weight="semibold">
                  {params.temperature.toFixed(2)}
                </Text>
              </div>
              <Slider
                min={0}
                max={2}
                step={0.1}
                value={params.temperature}
                onChange={(_, data) => updateStage(stage, { temperature: data.value })}
              />
              <Text size={100}>Lower = more predictable, Higher = more creative</Text>
            </div>
          </Field>

          <Field label="Top P">
            <div className={styles.sliderContainer}>
              <div className={styles.sliderLabel}>
                <Text size={200}>Nucleus Sampling</Text>
                <Text size={200} weight="semibold">
                  {params.topP.toFixed(2)}
                </Text>
              </div>
              <Slider
                min={0}
                max={1}
                step={0.05}
                value={params.topP}
                onChange={(_, data) => updateStage(stage, { topP: data.value })}
              />
            </div>
          </Field>

          <Field label="Max Tokens">
            <Input
              type="number"
              value={params.maxTokens.toString()}
              onChange={(_, data) =>
                updateStage(stage, { maxTokens: parseInt(data.value) || 2000 })
              }
            />
          </Field>

          <Field label="Frequency Penalty">
            <div className={styles.sliderContainer}>
              <div className={styles.sliderLabel}>
                <Text size={200}>Repetition Penalty</Text>
                <Text size={200} weight="semibold">
                  {params.frequencyPenalty.toFixed(2)}
                </Text>
              </div>
              <Slider
                min={-2}
                max={2}
                step={0.1}
                value={params.frequencyPenalty}
                onChange={(_, data) => updateStage(stage, { frequencyPenalty: data.value })}
              />
            </div>
          </Field>

          <Field label="Presence Penalty" className={styles.fullWidth}>
            <div className={styles.sliderContainer}>
              <div className={styles.sliderLabel}>
                <Text size={200}>Topic Diversity</Text>
                <Text size={200} weight="semibold">
                  {params.presencePenalty.toFixed(2)}
                </Text>
              </div>
              <Slider
                min={-2}
                max={2}
                step={0.1}
                value={params.presencePenalty}
                onChange={(_, data) => updateStage(stage, { presencePenalty: data.value })}
              />
            </div>
          </Field>

          <Field label="Custom System Prompt (optional)" className={styles.fullWidth}>
            <Textarea
              value={params.customSystemPrompt || ''}
              onChange={(_, data) => updateStage(stage, { customSystemPrompt: data.value })}
              placeholder="Add custom instructions for this stage..."
              rows={3}
            />
          </Field>

          <Field label="Preferred Model (optional)">
            <Input
              value={params.preferredModel || ''}
              onChange={(_, data) => updateStage(stage, { preferredModel: data.value })}
              placeholder="e.g., gpt-4, claude-3-opus"
            />
          </Field>

          <Field label="Strictness Level">
            <div className={styles.sliderContainer}>
              <div className={styles.sliderLabel}>
                <Text size={200}>Validation Strictness</Text>
                <Text size={200} weight="semibold">
                  {params.strictnessLevel.toFixed(2)}
                </Text>
              </div>
              <Slider
                min={0}
                max={1}
                step={0.1}
                value={params.strictnessLevel}
                onChange={(_, data) => updateStage(stage, { strictnessLevel: data.value })}
              />
              <Text size={100}>Lower = lenient, Higher = strict</Text>
            </div>
          </Field>
        </div>
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <div className={styles.formGrid}>
        <Field label="Setting Name" required>
          <Input
            value={settings.name}
            onChange={(_, data) => onChange({ ...settings, name: data.value })}
            placeholder="My AI Behavior Settings"
          />
        </Field>

        <Field label="Description">
          <Input
            value={settings.description || ''}
            onChange={(_, data) => onChange({ ...settings, description: data.value })}
            placeholder="Brief description..."
          />
        </Field>

        <Field label="Creativity vs Adherence" className={styles.fullWidth}>
          <div className={styles.sliderContainer}>
            <div className={styles.sliderLabel}>
              <Text size={200}>Balance between creativity and following instructions</Text>
              <Text size={200} weight="semibold">
                {settings.creativityVsAdherence.toFixed(2)}
              </Text>
            </div>
            <Slider
              min={0}
              max={1}
              step={0.05}
              value={settings.creativityVsAdherence}
              onChange={(_, data) => onChange({ ...settings, creativityVsAdherence: data.value })}
            />
            <Text size={100}>0 = Strict adherence to instructions | 1 = Full creative freedom</Text>
          </div>
        </Field>

        <Field label="Enable Chain of Thought">
          <Switch
            checked={settings.enableChainOfThought}
            onChange={(_, data) => onChange({ ...settings, enableChainOfThought: data.checked })}
            label="Use step-by-step reasoning for better quality"
          />
        </Field>

        <Field label="Show Prompts Before Sending">
          <Switch
            checked={settings.showPromptsBeforeSending}
            onChange={(_, data) =>
              onChange({ ...settings, showPromptsBeforeSending: data.checked })
            }
            label="Review prompts before sending to AI"
          />
        </Field>
      </div>

      <Accordion multiple collapsible defaultOpenItems={['scriptGeneration']}>
        <AccordionItem value="scriptGeneration">
          <AccordionHeader>Script Generation Parameters</AccordionHeader>
          <AccordionPanel>
            {renderStageParameters('Script Generation', 'scriptGeneration')}
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="sceneDescription">
          <AccordionHeader>Scene Description Parameters</AccordionHeader>
          <AccordionPanel>
            {renderStageParameters('Scene Description', 'sceneDescription')}
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="contentOptimization">
          <AccordionHeader>Content Optimization Parameters</AccordionHeader>
          <AccordionPanel>
            {renderStageParameters('Content Optimization', 'contentOptimization')}
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="translation">
          <AccordionHeader>Translation Parameters</AccordionHeader>
          <AccordionPanel>{renderStageParameters('Translation', 'translation')}</AccordionPanel>
        </AccordionItem>

        <AccordionItem value="qualityAnalysis">
          <AccordionHeader>Quality Analysis Parameters</AccordionHeader>
          <AccordionPanel>
            {renderStageParameters('Quality Analysis', 'qualityAnalysis')}
          </AccordionPanel>
        </AccordionItem>
      </Accordion>

      <div className={styles.actions}>
        <Button onClick={onCancel}>Cancel</Button>
        <Button appearance="primary" onClick={onSave}>
          {isNew ? 'Create' : 'Save Changes'}
        </Button>
      </div>
    </div>
  );
};

export const AIBehaviorSettingsComponent: FC = () => {
  const styles = useStyles();
  const {
    aiBehaviorSettings,
    selectedAIBehaviorId,
    error,
    loadAIBehaviorSettings,
    selectAIBehaviorSetting,
    createAIBehaviorSetting,
    updateAIBehaviorSetting,
    deleteAIBehaviorSetting,
    resetAIBehaviorSetting,
  } = useUserPreferencesStore();

  const [editing, setEditing] = useState<AIBehaviorSettings | null>(null);
  const [isNew, setIsNew] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    loadAIBehaviorSettings();
  }, [loadAIBehaviorSettings]);

  const createDefaultSettings = (): AIBehaviorSettings => ({
    id: '',
    name: 'New AI Behavior Settings',
    createdAt: new Date(),
    updatedAt: new Date(),
    scriptGeneration: {
      stageName: 'ScriptGeneration',
      temperature: 0.7,
      topP: 0.9,
      frequencyPenalty: 0.0,
      presencePenalty: 0.0,
      maxTokens: 2000,
      strictnessLevel: 0.5,
    },
    sceneDescription: {
      stageName: 'SceneDescription',
      temperature: 0.7,
      topP: 0.9,
      frequencyPenalty: 0.0,
      presencePenalty: 0.0,
      maxTokens: 1500,
      strictnessLevel: 0.5,
    },
    contentOptimization: {
      stageName: 'ContentOptimization',
      temperature: 0.5,
      topP: 0.9,
      frequencyPenalty: 0.0,
      presencePenalty: 0.0,
      maxTokens: 2000,
      strictnessLevel: 0.7,
    },
    translation: {
      stageName: 'Translation',
      temperature: 0.3,
      topP: 0.9,
      frequencyPenalty: 0.0,
      presencePenalty: 0.0,
      maxTokens: 2000,
      strictnessLevel: 0.8,
    },
    qualityAnalysis: {
      stageName: 'QualityAnalysis',
      temperature: 0.2,
      topP: 0.9,
      frequencyPenalty: 0.0,
      presencePenalty: 0.0,
      maxTokens: 1000,
      strictnessLevel: 0.9,
    },
    creativityVsAdherence: 0.5,
    enableChainOfThought: false,
    showPromptsBeforeSending: false,
    isDefault: false,
    usageCount: 0,
  });

  const handleCreate = () => {
    setEditing(createDefaultSettings());
    setIsNew(true);
  };

  const handleEdit = (setting: AIBehaviorSettings) => {
    setEditing(setting);
    setIsNew(false);
  };

  const handleSave = async () => {
    if (!editing) return;

    try {
      if (isNew) {
        await createAIBehaviorSetting(editing);
        setMessage({ type: 'success', text: 'AI behavior settings created successfully' });
      } else {
        await updateAIBehaviorSetting(editing.id, editing);
        setMessage({ type: 'success', text: 'AI behavior settings updated successfully' });
      }
      setEditing(null);
      setIsNew(false);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setMessage({ type: 'error', text: `Save failed: ${errorObj.message}` });
    }
  };

  const handleCancel = () => {
    setEditing(null);
    setIsNew(false);
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this AI behavior setting?')) {
      try {
        await deleteAIBehaviorSetting(id);
        setMessage({ type: 'success', text: 'AI behavior settings deleted successfully' });
      } catch (err: unknown) {
        const errorObj = err instanceof Error ? err : new Error(String(err));
        setMessage({ type: 'error', text: `Delete failed: ${errorObj.message}` });
      }
    }
  };

  const handleReset = async (id: string) => {
    if (window.confirm('Are you sure you want to reset this setting to defaults?')) {
      try {
        await resetAIBehaviorSetting(id);
        setMessage({ type: 'success', text: 'AI behavior settings reset to defaults' });
      } catch (err: unknown) {
        const errorObj = err instanceof Error ? err : new Error(String(err));
        setMessage({ type: 'error', text: `Reset failed: ${errorObj.message}` });
      }
    }
  };

  if (editing) {
    return (
      <AIBehaviorSettingsEditor
        settings={editing}
        onChange={setEditing}
        onSave={handleSave}
        onCancel={handleCancel}
        isNew={isNew}
      />
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Text weight="semibold" size={400}>
            Manage AI Behavior Settings
          </Text>
          <Text size={200}>
            Control LLM parameters, prompts, and behavior for each pipeline stage
          </Text>
        </div>
        <Button icon={<Add24Regular />} appearance="primary" onClick={handleCreate}>
          Create Settings
        </Button>
      </div>

      {message && (
        <MessageBar intent={message.type === 'error' ? 'error' : 'success'}>
          <MessageBarBody>
            <MessageBarTitle>{message.type === 'error' ? 'Error' : 'Success'}</MessageBarTitle>
            {message.text}
          </MessageBarBody>
        </MessageBar>
      )}

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      <Card className={styles.settingsCard}>
        {aiBehaviorSettings.length === 0 ? (
          <div className={styles.emptyState}>
            <Text>
              No AI behavior settings yet. Create one to customize AI generation parameters.
            </Text>
          </div>
        ) : (
          aiBehaviorSettings.map((setting) => (
            <div key={setting.id} className={styles.settingItem}>
              <div className={styles.settingInfo}>
                <Text weight="semibold">{setting.name}</Text>
                <Text size={200}>
                  Creativity: {(setting.creativityVsAdherence * 100).toFixed(0)}% | Chain of
                  Thought: {setting.enableChainOfThought ? 'Enabled' : 'Disabled'}
                </Text>
                {setting.description && <Text size={200}>{setting.description}</Text>}
                <Text size={100}>
                  Last used:{' '}
                  {setting.lastUsedAt ? new Date(setting.lastUsedAt).toLocaleString() : 'Never'} |
                  Usage count: {setting.usageCount}
                </Text>
              </div>
              <div className={styles.settingActions}>
                <Button
                  size="small"
                  appearance={selectedAIBehaviorId === setting.id ? 'primary' : 'secondary'}
                  onClick={() => selectAIBehaviorSetting(setting.id)}
                >
                  {selectedAIBehaviorId === setting.id ? 'Selected' : 'Select'}
                </Button>
                <Button size="small" onClick={() => handleEdit(setting)}>
                  Edit
                </Button>
                <Button
                  size="small"
                  icon={<ArrowReset24Regular />}
                  onClick={() => handleReset(setting.id)}
                >
                  Reset
                </Button>
                <Button
                  size="small"
                  icon={<Delete24Regular />}
                  onClick={() => handleDelete(setting.id)}
                  disabled={setting.isDefault}
                >
                  Delete
                </Button>
              </div>
            </div>
          ))
        )}
      </Card>
    </div>
  );
};
