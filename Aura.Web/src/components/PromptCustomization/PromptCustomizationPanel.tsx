import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Textarea,
  Dropdown,
  Option,
  Switch,
  Card,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Spinner,
  Badge,
  Label,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Input,
  Field,
} from '@fluentui/react-components';
import {
  Sparkle24Regular,
  Eye24Regular,
  Save24Regular,
  ArrowReset24Regular,
  BookInformation24Regular,
  Lightbulb24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import type { FC } from 'react';
import {
  getPromptPreview,
  listExamples,
  listPromptVersions,
  validateInstructions,
} from '../../services/api/promptsApi';
import { usePromptCustomizationStore } from '../../state/promptCustomization';
import type { Brief, PlanSpec, PromptPreset } from '../../types';
import { parseApiError } from '../../utils/apiErrorHandler';
import { useNotifications } from '../Notifications/Toasts';

const useStyles = makeStyles({
  panel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalM,
  },
  previewCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  previewText: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    whiteSpace: 'pre-wrap',
    maxHeight: '400px',
    overflowY: 'auto',
  },
  exampleCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  selectedExample: {
    backgroundColor: tokens.colorBrandBackground2,
  },
  badge: {
    marginLeft: tokens.spacingHorizontalXS,
  },
  tokenCount: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  substitutionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
  },
  substitutionItem: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  variableName: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
});

interface PromptCustomizationPanelProps {
  brief: Brief;
  planSpec: PlanSpec;
  onApply: () => void;
  onClose: () => void;
}

export const PromptCustomizationPanel: FC<PromptCustomizationPanelProps> = ({
  brief,
  planSpec,
  onApply,
  onClose,
}) => {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();

  const {
    promptModifiers,
    fewShotExamples,
    promptVersions,
    currentPreview,
    savedPresets,
    isLoadingPreview,
    isLoadingExamples,
    chainOfThoughtEnabled,
    updatePromptModifier,
    setFewShotExamples,
    setPromptVersions,
    setCurrentPreview,
    setLoadingPreview,
    setLoadingExamples,
    setChainOfThoughtEnabled,
    savePreset,
    loadPreset,
    deletePreset,
    resetPromptModifiers,
    clearPreview,
  } = usePromptCustomizationStore();

  const [savePresetDialogOpen, setSavePresetDialogOpen] = useState(false);
  const [newPresetName, setNewPresetName] = useState('');
  const [newPresetDescription, setNewPresetDescription] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const loadExamplesAndVersions = useCallback(async () => {
    setLoadingExamples(true);
    try {
      const [examplesData, versionsData] = await Promise.all([
        listExamples(),
        listPromptVersions(),
      ]);

      setFewShotExamples(examplesData.examples, examplesData.videoTypes);
      setPromptVersions(versionsData.versions);
    } catch (error: unknown) {
      const apiError = await parseApiError(error);
      showFailureToast({
        title: 'Error',
        message: 'Failed to load prompt resources: ' + apiError.message,
      });
    } finally {
      setLoadingExamples(false);
    }
  }, [setLoadingExamples, setFewShotExamples, setPromptVersions, showFailureToast]);

  useEffect(() => {
    loadExamplesAndVersions();
  }, [loadExamplesAndVersions]);

  const handlePreview = useCallback(async () => {
    if (!brief.topic) {
      showFailureToast({ title: 'Warning', message: 'Please enter a topic first' });
      return;
    }

    setLoadingPreview(true);
    clearPreview();

    try {
      const preview = await getPromptPreview(brief, planSpec, promptModifiers || undefined);
      setCurrentPreview(preview);
      showSuccessToast({ title: 'Success', message: 'Preview generated successfully' });
    } catch (error: unknown) {
      const apiError = await parseApiError(error);
      showFailureToast({
        title: 'Error',
        message: 'Failed to generate preview: ' + apiError.message,
      });
    } finally {
      setLoadingPreview(false);
    }
  }, [
    brief,
    planSpec,
    promptModifiers,
    setLoadingPreview,
    clearPreview,
    setCurrentPreview,
    showFailureToast,
    showSuccessToast,
  ]);

  const handleValidateInstructions = async (instructions: string) => {
    if (!instructions.trim()) {
      setValidationError(null);
      return;
    }

    try {
      const result = await validateInstructions(instructions);
      if (!result.isValid) {
        setValidationError(result.message);
        showFailureToast({
          title: 'Warning',
          message: 'Custom instructions contain potentially unsafe patterns',
        });
      } else {
        setValidationError(null);
      }
    } catch (error: unknown) {
      const apiError = await parseApiError(error);
      showFailureToast({ title: 'Error', message: 'Validation failed: ' + apiError.message });
    }
  };

  const handleSavePreset = () => {
    if (!newPresetName.trim()) {
      showFailureToast({ title: 'Warning', message: 'Please enter a preset name' });
      return;
    }

    if (savedPresets.some((p) => p.name === newPresetName)) {
      showFailureToast({ title: 'Warning', message: 'A preset with this name already exists' });
      return;
    }

    const preset: PromptPreset = {
      name: newPresetName,
      description: newPresetDescription,
      additionalInstructions: promptModifiers?.additionalInstructions,
      exampleStyle: promptModifiers?.exampleStyle,
      enableChainOfThought: chainOfThoughtEnabled,
      promptVersion: promptModifiers?.promptVersion,
      createdAt: new Date().toISOString(),
    };

    savePreset(preset);
    showSuccessToast({ title: 'Success', message: `Preset "${newPresetName}" saved successfully` });
    setSavePresetDialogOpen(false);
    setNewPresetName('');
    setNewPresetDescription('');
  };

  const handleLoadPreset = (presetName: string) => {
    loadPreset(presetName);
    showSuccessToast({ title: 'Success', message: `Preset "${presetName}" loaded` });
  };

  const handleDeletePreset = (presetName: string) => {
    deletePreset(presetName);
    showSuccessToast({ title: 'Success', message: `Preset "${presetName}" deleted` });
  };

  const handleReset = () => {
    resetPromptModifiers();
    setValidationError(null);
    showSuccessToast({ title: 'Success', message: 'Prompt modifiers reset to defaults' });
  };

  const handleApply = () => {
    if (validationError) {
      showFailureToast({ title: 'Error', message: 'Please fix validation errors before applying' });
      return;
    }
    showSuccessToast({ title: 'Success', message: 'Prompt customization applied' });
    onApply();
  };

  return (
    <div className={styles.panel}>
      <div className={styles.row}>
        <Title3>Customize Prompts</Title3>
        <Badge appearance="tint" icon={<Sparkle24Regular />}>
          Advanced
        </Badge>
      </div>

      <Accordion multiple collapsible>
        {/* Custom Instructions */}
        <AccordionItem value="instructions">
          <AccordionHeader icon={<BookInformation24Regular />}>Custom Instructions</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>
                Add specific instructions to guide the AI in generating your script. These will be
                injected with clear markers.
              </Text>
              <Field
                label="Additional Instructions (optional)"
                validationMessage={validationError || undefined}
                validationState={validationError ? 'error' : 'none'}
              >
                <Textarea
                  value={promptModifiers?.additionalInstructions || ''}
                  onChange={(_, data) => {
                    updatePromptModifier('additionalInstructions', data.value);
                    handleValidateInstructions(data.value);
                  }}
                  rows={6}
                  placeholder="e.g., Focus on practical examples, use analogies from everyday life, maintain an optimistic tone..."
                  maxLength={5000}
                />
              </Field>
              <Text size={200}>
                {promptModifiers?.additionalInstructions?.length || 0} / 5000 characters
              </Text>
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Few-Shot Examples */}
        <AccordionItem value="examples">
          <AccordionHeader icon={<Lightbulb24Regular />}>Example Styles</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              {isLoadingExamples ? (
                <Spinner label="Loading examples..." />
              ) : (
                <>
                  <Text>
                    Choose an example style to guide the tone and structure of your script.
                  </Text>
                  <Dropdown
                    placeholder="Select an example style"
                    value={promptModifiers?.exampleStyle || ''}
                    onOptionSelect={(_, data) =>
                      updatePromptModifier('exampleStyle', data.optionValue)
                    }
                  >
                    <Option value="" text="None">
                      None
                    </Option>
                    {fewShotExamples.map((example) => (
                      <Option
                        key={example.exampleName}
                        value={example.exampleName}
                        text={`${example.videoType} - ${example.exampleName}`}
                      >
                        {example.videoType} - {example.exampleName}
                      </Option>
                    ))}
                  </Dropdown>
                  {promptModifiers?.exampleStyle && (
                    <Card className={styles.exampleCard}>
                      {(() => {
                        const example = fewShotExamples.find(
                          (e) => e.exampleName === promptModifiers.exampleStyle
                        );
                        return example ? (
                          <>
                            <Text weight="semibold">{example.description}</Text>
                            <Text size={200}>
                              Key techniques: {example.keyTechniques.join(', ')}
                            </Text>
                          </>
                        ) : null;
                      })()}
                    </Card>
                  )}
                </>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Chain of Thought */}
        <AccordionItem value="chain-of-thought">
          <AccordionHeader>Chain-of-Thought Generation</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>
                Generate your script in three iterative stages with review points between each:
              </Text>
              <ul>
                <li>
                  <Text weight="semibold">Stage 1: Topic Analysis</Text> - Analyze themes, angles,
                  and strategy
                </li>
                <li>
                  <Text weight="semibold">Stage 2: Outline</Text> - Create detailed structure with
                  sections
                </li>
                <li>
                  <Text weight="semibold">Stage 3: Full Script</Text> - Expand outline into complete
                  script
                </li>
              </ul>
              <Switch
                checked={chainOfThoughtEnabled}
                onChange={(_, data) => setChainOfThoughtEnabled(data.checked)}
                label="Enable Chain-of-Thought Mode"
              />
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Note: This mode will require you to review and approve content between stages
              </Text>
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Prompt Version */}
        <AccordionItem value="version">
          <AccordionHeader>Prompt Version</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>Choose a prompt optimization strategy for different content goals.</Text>
              <Dropdown
                placeholder="Select prompt version"
                value={promptModifiers?.promptVersion || ''}
                onOptionSelect={(_, data) =>
                  updatePromptModifier('promptVersion', data.optionValue)
                }
              >
                {promptVersions.map((version) => (
                  <Option
                    key={version.version}
                    value={version.version}
                    text={version.name + (version.isDefault ? ' (Default)' : '')}
                  >
                    {version.name}
                    {version.isDefault && (
                      <Badge appearance="tint" className={styles.badge}>
                        Default
                      </Badge>
                    )}
                  </Option>
                ))}
              </Dropdown>
              {promptModifiers?.promptVersion && (
                <Card className={styles.previewCard}>
                  <Text size={200}>
                    {
                      promptVersions.find((v) => v.version === promptModifiers.promptVersion)
                        ?.description
                    }
                  </Text>
                </Card>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Preview */}
        <AccordionItem value="preview">
          <AccordionHeader icon={<Eye24Regular />}>Preview Prompt</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Button
                appearance="primary"
                icon={<Eye24Regular />}
                onClick={handlePreview}
                disabled={isLoadingPreview || !brief.topic}
              >
                Generate Preview
              </Button>

              {isLoadingPreview && <Spinner label="Generating preview..." />}

              {currentPreview && (
                <>
                  <div className={styles.substitutionList}>
                    <Text weight="semibold">Variable Substitutions:</Text>
                    {Object.entries(currentPreview.substitutedVariables).map(([key, value]) => (
                      <div key={key} className={styles.substitutionItem}>
                        <span className={styles.variableName}>{key}:</span>
                        <span>{value}</span>
                      </div>
                    ))}
                  </div>

                  <Text className={styles.tokenCount}>
                    Estimated tokens: {currentPreview.estimatedTokens}
                  </Text>

                  <Card className={styles.previewCard}>
                    <Label weight="semibold">System Prompt:</Label>
                    <div className={styles.previewText}>{currentPreview.systemPrompt}</div>
                  </Card>

                  <Card className={styles.previewCard}>
                    <Label weight="semibold">User Prompt:</Label>
                    <div className={styles.previewText}>{currentPreview.userPrompt}</div>
                  </Card>
                </>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Presets */}
        <AccordionItem value="presets">
          <AccordionHeader icon={<Save24Regular />}>Saved Presets</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Dialog
                open={savePresetDialogOpen}
                onOpenChange={(_, data) => setSavePresetDialogOpen(data.open)}
              >
                <DialogTrigger disableButtonEnhancement>
                  <Button icon={<Save24Regular />}>Save Current Settings</Button>
                </DialogTrigger>
                <DialogSurface>
                  <DialogBody>
                    <DialogTitle>Save Prompt Preset</DialogTitle>
                    <DialogContent>
                      <Field label="Preset Name" required>
                        <Input
                          value={newPresetName}
                          onChange={(_, data) => setNewPresetName(data.value)}
                          placeholder="My Custom Preset"
                        />
                      </Field>
                      <Field label="Description (optional)">
                        <Textarea
                          value={newPresetDescription}
                          onChange={(_, data) => setNewPresetDescription(data.value)}
                          rows={3}
                          placeholder="Describe what makes this preset unique..."
                        />
                      </Field>
                    </DialogContent>
                    <DialogActions>
                      <Button appearance="secondary" onClick={() => setSavePresetDialogOpen(false)}>
                        Cancel
                      </Button>
                      <Button appearance="primary" onClick={handleSavePreset}>
                        Save
                      </Button>
                    </DialogActions>
                  </DialogBody>
                </DialogSurface>
              </Dialog>

              {savedPresets.length > 0 ? (
                <div
                  style={{
                    display: 'flex',
                    flexDirection: 'column',
                    gap: tokens.spacingVerticalS,
                  }}
                >
                  {savedPresets.map((preset) => (
                    <Card key={preset.name} className={styles.exampleCard}>
                      <div
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                        }}
                      >
                        <div>
                          <Text weight="semibold">{preset.name}</Text>
                          {preset.description && <Text size={200}>{preset.description}</Text>}
                        </div>
                        <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                          <Button size="small" onClick={() => handleLoadPreset(preset.name)}>
                            Load
                          </Button>
                          <Button
                            size="small"
                            appearance="subtle"
                            icon={<Dismiss24Regular />}
                            onClick={() => handleDeletePreset(preset.name)}
                          />
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>
              ) : (
                <Text>No saved presets yet. Save your current settings to create one.</Text>
              )}
            </div>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>

      {/* Actions */}
      <div className={styles.actions}>
        <Button icon={<ArrowReset24Regular />} onClick={handleReset}>
          Reset
        </Button>
        <Button appearance="secondary" onClick={onClose}>
          Cancel
        </Button>
        <Button appearance="primary" onClick={handleApply}>
          Apply
        </Button>
      </div>
    </div>
  );
};
