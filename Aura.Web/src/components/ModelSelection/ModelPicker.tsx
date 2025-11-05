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
} from '@fluentui/react-components';
import {
  LockClosed20Regular,
  LockOpen20Regular,
  Warning20Regular,
  Checkmark20Regular,
  FlashCheckmark20Regular,
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
            <Tooltip content="Test this model" relationship="label">
              <Button appearance="subtle" icon={<FlashCheckmark20Regular />} disabled={isSaving}>
                Test
              </Button>
            </Tooltip>
          )}
        </div>

        {isPinned && (
          <Badge color="important" appearance="filled" icon={<LockClosed20Regular />}>
            Pinned
          </Badge>
        )}

        {selectedModel?.isDeprecated && (
          <Badge color="warning" appearance="outline" icon={<Warning20Regular />}>
            Deprecated
          </Badge>
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
    </div>
  );
};
