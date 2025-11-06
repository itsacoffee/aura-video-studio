import {
  makeStyles,
  tokens,
  Dropdown,
  Option,
  Button,
  Badge,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Text,
  Spinner,
  Tooltip,
  MessageBar,
  MessageBarBody,
  Input,
  Field,
} from '@fluentui/react-components';
import {
  LockClosed20Regular,
  LockOpen20Regular,
  Warning20Regular,
  Checkmark20Regular,
  FlashCheckmark20Regular,
  Info20Regular,
} from '@fluentui/react-icons';
import React, { useEffect, useState } from 'react';
import { useModelSelectionStore } from '../../state/modelSelection';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  pickerRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  dropdown: {
    minWidth: '250px',
    flex: 1,
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  modelInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    marginTop: tokens.spacingVerticalXS,
  },
  deprecationWarning: {
    marginTop: tokens.spacingVerticalS,
  },
});

export interface ModelPickerProps {
  provider: string;
  stage?: string;
  scope: 'Global' | 'Project' | 'Stage';
  label: string;
  description?: string;
  onModelSelected?: (modelId: string, isPinned: boolean) => void;
}

export const ModelPicker: React.FC<ModelPickerProps> = ({
  provider,
  stage = '',
  scope,
  label,
  description,
  onModelSelected,
}) => {
  const styles = useStyles();
  const {
    availableModels,
    selections,
    isLoadingModels,
    loadAvailableModels,
    loadSelections,
    setModelSelection,
  } = useModelSelectionStore();

  const [selectedModelId, setSelectedModelId] = useState<string>('');
  const [isPinned, setIsPinned] = useState<boolean>(false);
  const [showDeprecationDialog, setShowDeprecationDialog] = useState<boolean>(false);
  const [pendingSelection, setPendingSelection] = useState<string | null>(null);
  const [deprecationWarning, setDeprecationWarning] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [showTestDialog, setShowTestDialog] = useState<boolean>(false);
  const [testApiKey, setTestApiKey] = useState<string>('');
  const [testResult, setTestResult] = useState<{
    success: boolean;
    message: string;
    isAvailable?: boolean;
  } | null>(null);
  const [isTesting, setIsTesting] = useState<boolean>(false);
  const [showExplanationDialog, setShowExplanationDialog] = useState<boolean>(false);
  const [explanation, setExplanation] = useState<{
    selectedModel: { modelId: string; contextWindow: number; maxTokens: number };
    recommendedModel: { modelId: string; contextWindow: number; maxTokens: number } | null;
    selectedIsRecommended: boolean;
    reasoning: string;
    tradeoffs: string[];
    suggestions: string[];
  } | null>(null);
  const [isLoadingExplanation, setIsLoadingExplanation] = useState<boolean>(false);

  useEffect(() => {
    loadAvailableModels(provider);
    loadSelections();
  }, [provider, loadAvailableModels, loadSelections]);

  useEffect(() => {
    // Find current selection for this provider/stage/scope
    if (!selections) return;

    const allSelections = [
      ...selections.globalDefaults,
      ...selections.projectOverrides,
      ...selections.stageSelections,
    ];

    const currentSelection = allSelections.find(
      (s) => s.provider === provider && s.stage === stage && s.scope === scope
    );

    if (currentSelection) {
      setSelectedModelId(currentSelection.modelId);
      setIsPinned(currentSelection.isPinned);
    }
  }, [selections, provider, stage, scope]);

  const models = availableModels[provider] || [];
  const selectedModel = models.find((m) => m.modelId === selectedModelId);

  const handleModelChange = (modelId: string) => {
    const model = models.find((m) => m.modelId === modelId);
    if (!model) return;

    if (model.isDeprecated) {
      setPendingSelection(modelId);
      setShowDeprecationDialog(true);
    } else {
      applyModelSelection(modelId);
    }
  };

  const applyModelSelection = async (modelId: string) => {
    setIsSaving(true);
    setDeprecationWarning(null);

    try {
      const result = await setModelSelection(provider, stage, modelId, scope, isPinned);

      if (result.success) {
        setSelectedModelId(modelId);
        if (result.deprecationWarning) {
          setDeprecationWarning(result.deprecationWarning);
        }
        onModelSelected?.(modelId, isPinned);
      } else {
        alert(`Failed to set model: ${result.error}`);
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      alert(`Error setting model: ${errorMessage}`);
    } finally {
      setIsSaving(false);
    }
  };

  const confirmDeprecatedModel = () => {
    if (pendingSelection) {
      applyModelSelection(pendingSelection);
      setPendingSelection(null);
      setShowDeprecationDialog(false);
    }
  };

  const handleTestModel = () => {
    setShowTestDialog(true);
    setTestResult(null);
    setTestApiKey('');
  };

  const executeModelTest = async () => {
    if (!selectedModelId || !testApiKey.trim()) {
      setTestResult({
        success: false,
        message: 'Please provide an API key',
      });
      return;
    }

    setIsTesting(true);
    setTestResult(null);

    try {
      const response = await fetch('/api/models/test', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider,
          modelId: selectedModelId,
          apiKey: testApiKey,
        }),
      });

      const data = await response.json();

      if (response.ok) {
        setTestResult({
          success: true,
          message: data.isAvailable
            ? `✓ Model is available and working! Context: ${data.contextWindow.toLocaleString()} tokens`
            : `✗ Model is not available: ${data.errorMessage || 'Unknown error'}`,
          isAvailable: data.isAvailable,
        });
      } else {
        setTestResult({
          success: false,
          message: data.error || 'Failed to test model',
        });
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      setTestResult({
        success: false,
        message: `Test failed: ${errorMessage}`,
      });
    } finally {
      setIsTesting(false);
    }
  };

  const handleTogglePin = async () => {
    const newPinnedState = !isPinned;
    setIsPinned(newPinnedState);

    if (selectedModelId) {
      setIsSaving(true);
      try {
        const result = await setModelSelection(
          provider,
          stage,
          selectedModelId,
          scope,
          newPinnedState,
          newPinnedState ? 'User pinned model' : 'User unpinned model'
        );

        if (result.success) {
          onModelSelected?.(selectedModelId, newPinnedState);
        } else {
          setIsPinned(!newPinnedState);
          alert(`Failed to ${newPinnedState ? 'pin' : 'unpin'} model: ${result.error}`);
        }
      } catch (error: unknown) {
        setIsPinned(!newPinnedState);
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        alert(`Error ${newPinnedState ? 'pinning' : 'unpinning'} model: ${errorMessage}`);
      } finally {
        setIsSaving(false);
      }
    }
  };

  const handleExplainChoice = async () => {
    if (!selectedModelId) return;

    setShowExplanationDialog(true);
    setIsLoadingExplanation(true);
    setExplanation(null);

    try {
      const response = await fetch('/api/models/explain-choice', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider,
          stage,
          selectedModelId,
        }),
      });

      const data = await response.json();

      if (response.ok) {
        setExplanation({
          selectedModel: data.selectedModel,
          recommendedModel: data.recommendedModel,
          selectedIsRecommended: data.comparison.selectedIsRecommended,
          reasoning: data.comparison.reasoning,
          tradeoffs: data.comparison.tradeoffs,
          suggestions: data.comparison.suggestions,
        });
      } else {
        alert(`Failed to explain model choice: ${data.error || 'Unknown error'}`);
        setShowExplanationDialog(false);
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      alert(`Error explaining model choice: ${errorMessage}`);
      setShowExplanationDialog(false);
    } finally {
      setIsLoadingExplanation(false);
    }
  };

  if (isLoadingModels) {
    return (
      <div className={styles.container}>
        <Text weight="semibold">{label}</Text>
        <Spinner size="tiny" label="Loading models..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div>
        <Text weight="semibold">{label}</Text>
        {description && (
          <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
            {description}
          </Text>
        )}
      </div>

      <div className={styles.pickerRow}>
        <Dropdown
          className={styles.dropdown}
          placeholder="Select a model"
          value={selectedModelId || ''}
          selectedOptions={selectedModelId ? [selectedModelId] : []}
          onOptionSelect={(_, data) => handleModelChange(data.optionValue as string)}
          disabled={isSaving}
        >
          {models.map((model) => (
            <Option
              key={model.modelId}
              value={model.modelId}
              text={`${model.modelId}${model.isDeprecated ? ' (Deprecated)' : ''}`}
            >
              {model.modelId}
              {model.isDeprecated && ' (Deprecated)'}
            </Option>
          ))}
        </Dropdown>

        <div className={styles.buttonGroup}>
          <Tooltip
            content={isPinned ? 'Unpin model (allow fallback)' : 'Pin model (never auto-change)'}
            relationship="label"
          >
            <Button
              appearance="subtle"
              icon={isPinned ? <LockClosed20Regular /> : <LockOpen20Regular />}
              onClick={handleTogglePin}
              disabled={!selectedModelId || isSaving}
            >
              {isPinned ? 'Pinned' : 'Pin'}
            </Button>
          </Tooltip>

          {selectedModel && (
            <>
              <Tooltip content="Test model availability with a lightweight probe" relationship="label">
                <Button
                  appearance="subtle"
                  icon={<FlashCheckmark20Regular />}
                  onClick={handleTestModel}
                  disabled={isSaving}
                >
                  Test
                </Button>
              </Tooltip>
              
              <Tooltip content="Explain this model choice and compare with recommendations" relationship="label">
                <Button
                  appearance="subtle"
                  icon={<Info20Regular />}
                  onClick={handleExplainChoice}
                  disabled={isSaving}
                >
                  Explain
                </Button>
              </Tooltip>
            </>
          )}
        </div>

        {isPinned && (
          <Tooltip
            content="This model is pinned and will never be automatically changed. If unavailable, operations will be blocked until you make a manual choice."
            relationship="description"
          >
            <Badge color="important" appearance="filled" icon={<LockClosed20Regular />}>
              Pinned
            </Badge>
          </Tooltip>
        )}

        {selectedModelId && scope === 'Stage' && (
          <Tooltip
            content={`This is a per-stage override (${scope} scope). It takes precedence over project and global defaults.`}
            relationship="description"
          >
            <Badge color="brand" appearance="outline">
              Stage Override
            </Badge>
          </Tooltip>
        )}

        {selectedModelId && scope === 'Project' && (
          <Tooltip
            content={`This is a project-level override (${scope} scope). It takes precedence over global defaults but not stage pins.`}
            relationship="description"
          >
            <Badge color="informative" appearance="outline">
              Project Override
            </Badge>
          </Tooltip>
        )}

        {selectedModel?.isDeprecated && (
          <Tooltip
            content={`This model is deprecated and may be removed soon. ${selectedModel.replacementModel ? `Consider migrating to ${selectedModel.replacementModel}.` : 'Consider using an alternative model.'}`}
            relationship="description"
          >
            <Badge color="warning" appearance="outline" icon={<Warning20Regular />}>
              Deprecated
            </Badge>
          </Tooltip>
        )}
      </div>

      {selectedModel && (
        <div className={styles.modelInfo}>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Context: {selectedModel.contextWindow.toLocaleString()} tokens | Max output:{' '}
            {selectedModel.maxTokens.toLocaleString()} tokens
          </Text>
          {selectedModel.aliases.length > 0 && (
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              Aliases: {selectedModel.aliases.join(', ')}
            </Text>
          )}
        </div>
      )}

      {deprecationWarning && (
        <MessageBar intent="warning" className={styles.deprecationWarning}>
          <MessageBarBody>{deprecationWarning}</MessageBarBody>
        </MessageBar>
      )}

      {/* Deprecation Confirmation Dialog */}
      <Dialog
        open={showDeprecationDialog}
        onOpenChange={(_, data) => setShowDeprecationDialog(data.open)}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Model Deprecated</DialogTitle>
            <DialogContent>
              <Text>
                The model <strong>{pendingSelection}</strong> is deprecated.
              </Text>
              {selectedModel?.replacementModel && (
                <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
                  Recommended replacement: <strong>{selectedModel.replacementModel}</strong>
                </Text>
              )}
              <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
                Do you want to continue using this deprecated model?
              </Text>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => {
                  setShowDeprecationDialog(false);
                  setPendingSelection(null);
                }}
              >
                Cancel
              </Button>
              <Button appearance="primary" onClick={confirmDeprecatedModel}>
                <Checkmark20Regular /> Use Anyway
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {/* Test Model Dialog */}
      <Dialog open={showTestDialog} onOpenChange={(_, data) => setShowTestDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Test Model: {selectedModelId}</DialogTitle>
            <DialogContent>
              <Text style={{ display: 'block', marginBottom: tokens.spacingVerticalM }}>
                Test if the model <strong>{selectedModelId}</strong> from provider{' '}
                <strong>{provider}</strong> is available and working properly.
              </Text>

              <Field label="API Key" required style={{ marginBottom: tokens.spacingVerticalM }}>
                <Input
                  type="password"
                  value={testApiKey}
                  onChange={(_, data) => setTestApiKey(data.value)}
                  placeholder={`Enter your ${provider} API key`}
                  disabled={isTesting}
                />
              </Field>

              {testResult && (
                <MessageBar
                  intent={testResult.isAvailable ? 'success' : 'error'}
                  style={{ marginTop: tokens.spacingVerticalM }}
                >
                  <MessageBarBody>{testResult.message}</MessageBarBody>
                </MessageBar>
              )}

              <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
                <em>Note: Your API key is not stored and only used for this test.</em>
              </Text>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => {
                  setShowTestDialog(false);
                  setTestResult(null);
                  setTestApiKey('');
                }}
                disabled={isTesting}
              >
                Close
              </Button>
              <Button
                appearance="primary"
                onClick={executeModelTest}
                disabled={isTesting || !testApiKey.trim()}
              >
                {isTesting ? <Spinner size="tiny" /> : <FlashCheckmark20Regular />}
                {isTesting ? 'Testing...' : 'Test Model'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {/* Explain Choice Dialog */}
      <Dialog
        open={showExplanationDialog}
        onOpenChange={(_, data) => setShowExplanationDialog(data.open)}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Explain Model Choice: {selectedModelId}</DialogTitle>
            <DialogContent>
              {isLoadingExplanation ? (
                <Spinner label="Loading explanation..." />
              ) : explanation ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
                  <div>
                    <Text weight="semibold" style={{ display: 'block' }}>Your Selection</Text>
                    <Text style={{ display: 'block' }}>
                      {explanation.selectedModel.modelId} - Context: {explanation.selectedModel.contextWindow.toLocaleString()} tokens, 
                      Max output: {explanation.selectedModel.maxTokens.toLocaleString()} tokens
                    </Text>
                  </div>

                  {explanation.recommendedModel && !explanation.selectedIsRecommended && (
                    <div>
                      <Text weight="semibold" style={{ display: 'block' }}>Recommended Model</Text>
                      <Text style={{ display: 'block' }}>
                        {explanation.recommendedModel.modelId} - Context: {explanation.recommendedModel.contextWindow.toLocaleString()} tokens, 
                        Max output: {explanation.recommendedModel.maxTokens.toLocaleString()} tokens
                      </Text>
                    </div>
                  )}

                  {explanation.selectedIsRecommended && (
                    <MessageBar intent="success">
                      <MessageBarBody>
                        ✓ Your selection matches the recommended model for this stage.
                      </MessageBarBody>
                    </MessageBar>
                  )}

                  <div>
                    <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}>
                      Reasoning
                    </Text>
                    <Text>{explanation.reasoning}</Text>
                  </div>

                  {explanation.tradeoffs.length > 0 && (
                    <div>
                      <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}>
                        Tradeoffs
                      </Text>
                      <ul style={{ margin: 0, paddingLeft: tokens.spacingHorizontalL }}>
                        {explanation.tradeoffs.map((tradeoff, index) => (
                          <li key={index}>
                            <Text>{tradeoff}</Text>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {explanation.suggestions.length > 0 && (
                    <div>
                      <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}>
                        Suggestions
                      </Text>
                      <ul style={{ margin: 0, paddingLeft: tokens.spacingHorizontalL }}>
                        {explanation.suggestions.map((suggestion, index) => (
                          <li key={index}>
                            <Text>{suggestion}</Text>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              ) : (
                <Text>No explanation available.</Text>
              )}
            </DialogContent>
            <DialogActions>
              <Button
                appearance="primary"
                onClick={() => setShowExplanationDialog(false)}
              >
                Close
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};
