import {
  makeStyles,
  tokens,
  Card,
  Text,
  Switch,
  Divider,
  Button,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import { Delete20Regular } from '@fluentui/react-icons';
import React, { useEffect } from 'react';
import { useModelSelectionStore } from '../../state/modelSelection';
import { ModelPicker } from './ModelPicker';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  settingRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  settingInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  precedenceTable: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  precedenceItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXS} 0`,
  },
  precedenceNumber: {
    width: '24px',
    height: '24px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundInverted,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  infoBox: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorBrandBackground}`,
  },
});

export const ModelSelectionPanel: React.FC = () => {
  const styles = useStyles();
  const { selections, loadSelections, clearSelections, setAllowAutomaticFallback } =
    useModelSelectionStore();

  useEffect(() => {
    loadSelections();
  }, [loadSelections]);

  const handleToggleAutoFallback = async (checked: boolean) => {
    await setAllowAutomaticFallback(checked);
  };

  const handleClearAll = async () => {
    if (
      confirm(
        'This will clear all model selections (global defaults, project overrides, and stage selections). Are you sure?'
      )
    ) {
      await clearSelections(undefined, undefined, undefined);
    }
  };

  const handleClearGlobal = async () => {
    if (confirm('This will clear all global default model selections. Are you sure?')) {
      await clearSelections(undefined, undefined, 'Global');
    }
  };

  const handleClearProject = async () => {
    if (confirm('This will clear all project-specific model overrides. Are you sure?')) {
      await clearSelections(undefined, undefined, 'Project');
    }
  };

  return (
    <div className={styles.container}>
      {/* Settings Overview */}
      <Card className={styles.card}>
        <div className={styles.header}>
          <Text size={500} weight="semibold">
            Model Selection Settings
          </Text>
          <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
            Configure which AI models to use at each stage of the video generation pipeline
          </Text>
        </div>

        {/* Automatic Fallback Setting */}
        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Text weight="semibold">Allow Automatic Fallback</Text>
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              When enabled, the system may automatically select an alternative model if your chosen
              model is unavailable. When disabled, operations will be blocked and require your
              explicit decision.
            </Text>
          </div>
          <Switch
            checked={selections?.allowAutomaticFallback || false}
            onChange={(_, data) => handleToggleAutoFallback(data.checked)}
          />
        </div>

        <MessageBar intent="warning" style={{ marginTop: tokens.spacingVerticalM }}>
          <MessageBarBody>
            <strong>Important:</strong> Pinned models will never be automatically changed,
            regardless of this setting. If a pinned model is unavailable, the operation will always
            be blocked until you make an explicit choice.
          </MessageBarBody>
        </MessageBar>
      </Card>

      {/* Model Selection Precedence */}
      <Card className={styles.card}>
        <div className={styles.header}>
          <Text size={500} weight="semibold">
            Model Selection Precedence
          </Text>
          <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
            The system follows this priority order when resolving which model to use
          </Text>
        </div>

        <div className={styles.precedenceTable}>
          <div className={styles.precedenceItem}>
            <div className={styles.precedenceNumber}>1</div>
            <div>
              <Text weight="semibold">Run Override (Pinned)</Text>
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                CLI or API parameter with pin flag — Blocks if unavailable
              </Text>
            </div>
          </div>

          <div className={styles.precedenceItem}>
            <div className={styles.precedenceNumber}>2</div>
            <div>
              <Text weight="semibold">Run Override</Text>
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                CLI or API parameter without pin flag — Falls back if unavailable
              </Text>
            </div>
          </div>

          <div className={styles.precedenceItem}>
            <div className={styles.precedenceNumber}>3</div>
            <div>
              <Text weight="semibold">Stage Pinned</Text>
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                Per-stage pinned selection (e.g., &quot;Script model&quot;) — Blocks if unavailable
              </Text>
            </div>
          </div>

          <div className={styles.precedenceItem}>
            <div className={styles.precedenceNumber}>4</div>
            <div>
              <Text weight="semibold">Project Override</Text>
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                Per-project model preference — Falls back if unavailable
              </Text>
            </div>
          </div>

          <div className={styles.precedenceItem}>
            <div className={styles.precedenceNumber}>5</div>
            <div>
              <Text weight="semibold">Global Default</Text>
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                Application-wide default model — Falls back if unavailable
              </Text>
            </div>
          </div>

          <div className={styles.precedenceItem}>
            <div className={styles.precedenceNumber}>6</div>
            <div>
              <Text weight="semibold">Automatic Fallback</Text>
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                Safe fallback from model catalog — Only used if &quot;Allow Automatic Fallback&quot;
                is enabled
              </Text>
            </div>
          </div>
        </div>

        <div className={styles.infoBox}>
          <Text size={300} weight="semibold">
            💡 Best Practice
          </Text>
          <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
            Pin models for critical stages where consistency is paramount. Leave non-critical stages
            unpinned to allow automatic fallback when configured models are temporarily unavailable.
          </Text>
        </div>
      </Card>

      {/* Global Defaults */}
      <Card className={styles.card}>
        <div className={styles.header}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <Text size={500} weight="semibold">
                Global Defaults
              </Text>
              <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
                Default models used across all projects unless overridden
              </Text>
            </div>
            <Button appearance="subtle" icon={<Delete20Regular />} onClick={handleClearGlobal}>
              Clear All
            </Button>
          </div>
        </div>

        <div className={styles.section}>
          <ModelPicker
            provider="OpenAI"
            scope="Global"
            label="OpenAI Default Model"
            description="Used for script generation, analysis, and other LLM tasks"
          />

          <ModelPicker
            provider="Anthropic"
            scope="Global"
            label="Anthropic Default Model"
            description="Alternative provider for LLM tasks"
          />

          <ModelPicker
            provider="Gemini"
            scope="Global"
            label="Google Gemini Default Model"
            description="Alternative provider with large context windows"
          />
        </div>
      </Card>

      {/* Per-Stage Selections */}
      <Card className={styles.card}>
        <div className={styles.header}>
          <Text size={500} weight="semibold">
            Per-Stage Model Selection
          </Text>
          <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
            Configure specific models for each pipeline stage (can be pinned for consistency)
          </Text>
        </div>

        <Divider />

        <div className={styles.section}>
          <ModelPicker
            provider="OpenAI"
            stage="script"
            scope="Stage"
            label="Script Generation Model"
            description="Model used for generating video scripts from briefs"
          />

          <ModelPicker
            provider="OpenAI"
            stage="visual"
            scope="Stage"
            label="Visual Prompts Model"
            description="Model used for generating image prompts and descriptions"
          />

          <ModelPicker
            provider="OpenAI"
            stage="analysis"
            scope="Stage"
            label="Content Analysis Model"
            description="Model used for analyzing scenes and content complexity"
          />
        </div>
      </Card>

      {/* Clear All Actions */}
      <Card className={styles.card}>
        <div className={styles.header}>
          <Text size={500} weight="semibold">
            Reset Options
          </Text>
          <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}>
            Clear model selections to restore defaults
          </Text>
        </div>

        <div
          style={{
            display: 'flex',
            gap: tokens.spacingHorizontalM,
            flexWrap: 'wrap',
          }}
        >
          <Button appearance="secondary" icon={<Delete20Regular />} onClick={handleClearGlobal}>
            Clear Global Defaults
          </Button>

          <Button appearance="secondary" icon={<Delete20Regular />} onClick={handleClearProject}>
            Clear Project Overrides
          </Button>

          <Button appearance="secondary" icon={<Delete20Regular />} onClick={handleClearAll}>
            Clear All Selections
          </Button>
        </div>
      </Card>
    </div>
  );
};
