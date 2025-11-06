import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Divider,
  MessageBar,
  MessageBarBody,
  Badge,
  Select,
  Switch,
  Spinner,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import { Info20Regular, Pin20Filled, Pin20Regular, Beaker20Regular } from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  stageCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  stageHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  modelSelector: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  badges: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
  auditLog: {
    maxHeight: '400px',
    overflowY: 'auto',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
  },
  auditEntry: {
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  explainDialog: {
    minWidth: '600px',
  },
  comparisonGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
    marginBottom: tokens.spacingVerticalM,
  },
  tradeoffsList: {
    listStyle: 'none',
    padding: 0,
  },
  tradeoffItem: {
    padding: tokens.spacingVerticalXS,
    '&::before': {
      content: '"⚠️ "',
      marginRight: tokens.spacingHorizontalXS,
    },
  },
});

interface ModelSelection {
  provider: string;
  stage: string;
  modelId: string;
  scope: string;
  isPinned: boolean;
  setBy: string;
  setAt: string;
  reason: string;
}

interface AuditEntry {
  provider: string;
  stage: string;
  modelId: string;
  source: string;
  reasoning: string;
  isPinned: boolean;
  isBlocked: boolean;
  blockReason?: string;
  fallbackReason?: string;
  timestamp: string;
  jobId?: string;
}

const stages = [
  { id: 'script', name: 'Script Generation', description: 'Generate video scripts from briefs' },
  { id: 'visual', name: 'Visual Prompts', description: 'Create image generation prompts' },
  { id: 'narration', name: 'Narration Optimization', description: 'Optimize text for TTS' },
  { id: 'refinement', name: 'Script Refinement', description: 'Improve and polish scripts' },
];

const providers = ['OpenAI', 'Anthropic', 'Gemini', 'Ollama'];

export const ModelsPage: FC = () => {
  const styles = useStyles();
  const [selections, setSelections] = useState<ModelSelection[]>([]);
  const [auditLog, setAuditLog] = useState<AuditEntry[]>([]);
  const [availableModels, setAvailableModels] = useState<Record<string, string[]>>({});
  const [loading, setLoading] = useState(true);
  const [allowAutomaticFallback, setAllowAutomaticFallback] = useState(false);
  const [explainDialogOpen, setExplainDialogOpen] = useState(false);
  const [explanation, setExplanation] = useState<{
    selectedModel: { modelId: string; contextWindow: number };
    recommendedModel: { modelId: string; contextWindow: number } | null;
    comparison: { reasoning: string; tradeoffs: string[]; suggestions: string[] };
  } | null>(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [selectionsResp, modelsResp, auditResp] = await Promise.all([
        fetch('/api/models/selection'),
        fetch('/api/models/available'),
        fetch('/api/models/audit-log?limit=50'),
      ]);

      if (selectionsResp.ok) {
        const data = await selectionsResp.json();
        setSelections([...data.stageSelections, ...data.projectOverrides, ...data.globalDefaults]);
        setAllowAutomaticFallback(data.allowAutomaticFallback);
      }

      if (modelsResp.ok) {
        const data = await modelsResp.json();
        const models: Record<string, string[]> = {};
        Object.entries(data.providers || {}).forEach(([provider, modelList]) => {
          models[provider] = (modelList as { modelId: string }[]).map(
            (m: { modelId: string }) => m.modelId
          );
        });
        setAvailableModels(models);
      }

      if (auditResp.ok) {
        const data = await auditResp.json();
        setAuditLog(data.entries || []);
      }
    } catch (error: unknown) {
      console.error(
        'Failed to fetch data:',
        error instanceof Error ? error.message : String(error)
      );
    } finally {
      setLoading(false);
    }
  }, []);

  const handleModelChange = async (
    provider: string,
    stage: string,
    modelId: string,
    pin: boolean
  ) => {
    try {
      const response = await fetch('/api/models/selection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider,
          stage,
          modelId,
          scope: 'Stage',
          pin,
          setBy: 'user',
          reason: pin ? 'User pinned selection' : 'User override',
        }),
      });

      if (response.ok) {
        fetchData();
      }
    } catch (error: unknown) {
      console.error(
        'Failed to update selection:',
        error instanceof Error ? error.message : String(error)
      );
    }
  };

  const handleExplainChoice = async (provider: string, stage: string, modelId: string) => {
    setExplainDialogOpen(true);
    try {
      const response = await fetch('/api/models/explain-choice', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ provider, stage, selectedModelId: modelId }),
      });

      if (response.ok) {
        const data = await response.json();
        setExplanation(data);
      }
    } catch (error: unknown) {
      console.error(
        'Failed to get explanation:',
        error instanceof Error ? error.message : String(error)
      );
    }
  };

  const handleTestModel = async (provider: string, modelId: string) => {
    console.log('Testing model:', provider, modelId);
  };

  const getSelectionForStage = (stage: string) => {
    return selections.find((s) => s.stage === stage);
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading models..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Model Selection</Title1>
        <Text>Configure AI models for each pipeline stage with explicit control and pinning.</Text>
      </div>

      <MessageBar intent="info">
        <MessageBarBody>
          Select models for each stage. Pin a model to prevent automatic changes. All selections are
          recorded in the audit log for transparency.
        </MessageBarBody>
      </MessageBar>

      <Divider style={{ margin: `${tokens.spacingVerticalL} 0` }} />

      <div className={styles.section}>
        <Title2>Per-Stage Model Configuration</Title2>

        {stages.map((stage) => {
          const selection = getSelectionForStage(stage.id);
          return (
            <Card key={stage.id} className={styles.stageCard}>
              <div className={styles.stageHeader}>
                <div>
                  <Title3>{stage.name}</Title3>
                  <Text size={200}>{stage.description}</Text>
                </div>
                <div className={styles.badges}>
                  {selection?.isPinned && (
                    <Badge color="success" icon={<Pin20Filled />}>
                      Pinned
                    </Badge>
                  )}
                  {selection && <Badge color="informative">{selection.scope}</Badge>}
                </div>
              </div>

              <div className={styles.modelSelector}>
                <Select
                  value={selection?.modelId || ''}
                  onChange={(_, data) => {
                    const modelId = data.value;
                    if (modelId) {
                      handleModelChange(selection?.provider || 'OpenAI', stage.id, modelId, false);
                    }
                  }}
                  style={{ minWidth: '200px' }}
                >
                  <option value="">Use default</option>
                  {providers.map((provider) =>
                    availableModels[provider]?.map((model) => (
                      <option key={`${provider}-${model}`} value={model}>
                        {provider}: {model}
                      </option>
                    ))
                  )}
                </Select>

                <Button
                  appearance="transparent"
                  icon={selection?.isPinned ? <Pin20Filled /> : <Pin20Regular />}
                  onClick={() => {
                    if (selection) {
                      handleModelChange(
                        selection.provider,
                        stage.id,
                        selection.modelId,
                        !selection.isPinned
                      );
                    }
                  }}
                  disabled={!selection}
                >
                  {selection?.isPinned ? 'Unpin' : 'Pin'}
                </Button>

                {selection && (
                  <>
                    <Button
                      appearance="transparent"
                      icon={<Info20Regular />}
                      onClick={() =>
                        handleExplainChoice(selection.provider, stage.id, selection.modelId)
                      }
                    >
                      Explain my choice
                    </Button>
                    <Button
                      appearance="transparent"
                      icon={<Beaker20Regular />}
                      onClick={() => handleTestModel(selection.provider, selection.modelId)}
                    >
                      Test model
                    </Button>
                  </>
                )}
              </div>

              {selection && (
                <div style={{ marginTop: tokens.spacingVerticalS }}>
                  <Text size={100}>
                    Set by: {selection.setBy} | {selection.reason}
                  </Text>
                </div>
              )}
            </Card>
          );
        })}
      </div>

      <Divider style={{ margin: `${tokens.spacingVerticalL} 0` }} />

      <div className={styles.section}>
        <Title2>Settings</Title2>
        <Switch
          checked={allowAutomaticFallback}
          onChange={(_, data) => setAllowAutomaticFallback(data.checked)}
          label="Allow automatic fallback"
        />
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS, display: 'block' }}>
          When enabled, system can automatically select alternative models if your selection is
          unavailable. When disabled, generation will be blocked until you manually select an
          available model.
        </Text>
      </div>

      <Divider style={{ margin: `${tokens.spacingVerticalL} 0` }} />

      <div className={styles.section}>
        <Title2>Audit Log (Recent)</Title2>
        <Text size={200}>
          All model selection resolutions are recorded with source, reasoning, and timestamp.
        </Text>
        <div className={styles.auditLog}>
          {auditLog.length === 0 && <Text>No audit entries yet.</Text>}
          {auditLog.map((entry, index) => (
            <div key={index} className={styles.auditEntry}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <Text weight="semibold">
                  {entry.provider} / {entry.stage} → {entry.modelId}
                </Text>
                <Text size={100}>{new Date(entry.timestamp).toLocaleString()}</Text>
              </div>
              <Text size={200}>
                Source: {entry.source} {entry.isPinned && '(Pinned)'}
              </Text>
              <Text size={200}>{entry.reasoning}</Text>
              {entry.fallbackReason && (
                <MessageBar intent="info" style={{ marginTop: tokens.spacingVerticalXS }}>
                  <MessageBarBody>Fallback: {entry.fallbackReason}</MessageBarBody>
                </MessageBar>
              )}
              {entry.isBlocked && (
                <MessageBar intent="warning" style={{ marginTop: tokens.spacingVerticalXS }}>
                  <MessageBarBody>Blocked: {entry.blockReason}</MessageBarBody>
                </MessageBar>
              )}
              {entry.jobId && (
                <Text size={100} style={{ marginTop: tokens.spacingVerticalXXS }}>
                  Job ID: {entry.jobId}
                </Text>
              )}
            </div>
          ))}
        </div>
      </div>

      <Dialog open={explainDialogOpen} onOpenChange={(_, data) => setExplainDialogOpen(data.open)}>
        <DialogSurface className={styles.explainDialog}>
          <DialogBody>
            <DialogTitle>Model Choice Explanation</DialogTitle>
            <DialogContent>
              {explanation && (
                <>
                  <div className={styles.comparisonGrid}>
                    <div>
                      <Title3>Your Selection</Title3>
                      <Text>{explanation.selectedModel.modelId}</Text>
                      <Text size={200}>
                        Context: {explanation.selectedModel.contextWindow} tokens
                      </Text>
                    </div>
                    {explanation.recommendedModel && (
                      <div>
                        <Title3>Recommended</Title3>
                        <Text>{explanation.recommendedModel.modelId}</Text>
                        <Text size={200}>
                          Context: {explanation.recommendedModel.contextWindow} tokens
                        </Text>
                      </div>
                    )}
                  </div>

                  <Divider />

                  <div style={{ marginTop: tokens.spacingVerticalM }}>
                    <Title3>Analysis</Title3>
                    <Text>{explanation.comparison.reasoning}</Text>
                  </div>

                  {explanation.comparison.tradeoffs.length > 0 && (
                    <div style={{ marginTop: tokens.spacingVerticalM }}>
                      <Title3>Tradeoffs</Title3>
                      <ul className={styles.tradeoffsList}>
                        {explanation.comparison.tradeoffs.map((tradeoff, idx) => (
                          <li key={idx} className={styles.tradeoffItem}>
                            {tradeoff}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {explanation.comparison.suggestions.length > 0 && (
                    <div style={{ marginTop: tokens.spacingVerticalM }}>
                      <Title3>Suggestions</Title3>
                      <ul>
                        {explanation.comparison.suggestions.map((suggestion, idx) => (
                          <li key={idx}>{suggestion}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </>
              )}
              {!explanation && <Spinner label="Loading explanation..." />}
            </DialogContent>
            <DialogActions>
              <Button appearance="primary" onClick={() => setExplainDialogOpen(false)}>
                Close
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};
