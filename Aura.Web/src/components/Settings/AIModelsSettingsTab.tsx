import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Divider,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import { useState } from 'react';
import { ModelManager } from '../Engines/ModelManager';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  engineSection: {
    marginBottom: tokens.spacingVerticalXL,
  },
});

const engines = [
  {
    id: 'ollama',
    name: 'Ollama',
    description: 'Local LLM models for script generation',
  },
  {
    id: 'piper',
    name: 'Piper',
    description: 'Neural text-to-speech voices',
  },
  {
    id: 'mimic3',
    name: 'Mimic3',
    description: 'Offline text-to-speech voices',
  },
  {
    id: 'stable-diffusion',
    name: 'Stable Diffusion',
    description: 'AI image generation models',
  },
  {
    id: 'comfyui',
    name: 'ComfyUI',
    description: 'Node-based AI image generation',
  },
];

export function AIModelsSettingsTab() {
  const styles = useStyles();
  const [expandedEngines, setExpandedEngines] = useState<Set<string>>(new Set(['ollama']));

  const toggleEngine = (engineId: string) => {
    setExpandedEngines((prev) => {
      const next = new Set(prev);
      if (next.has(engineId)) {
        next.delete(engineId);
      } else {
        next.add(engineId);
      }
      return next;
    });
  };

  return (
    <Card className={styles.section}>
      <Title2>AI Models Management</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Manage AI models and voices for local engines. Download, verify, and organize models for
        text generation, speech synthesis, and image generation.
      </Text>

      <MessageBar intent="info">
        <MessageBarBody>
          Models shown here are for locally-installed AI engines. Cloud-based providers (OpenAI,
          ElevenLabs, etc.) do not require model downloads and are configured in the API Keys tab.
        </MessageBarBody>
      </MessageBar>

      <Divider
        style={{ marginTop: tokens.spacingVerticalL, marginBottom: tokens.spacingVerticalL }}
      />

      <div className={styles.content}>
        <Card className={styles.infoBox}>
          <Text weight="semibold" size={300}>
            📦 About Model Management
          </Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS, display: 'block' }}>
            Each engine can have multiple models installed. You can:
          </Text>
          <ul style={{ marginTop: tokens.spacingVerticalS, marginLeft: tokens.spacingHorizontalL }}>
            <li>
              <Text size={200}>View all installed models and their sizes</Text>
            </li>
            <li>
              <Text size={200}>Add external folders with existing models</Text>
            </li>
            <li>
              <Text size={200}>Verify model checksums for integrity</Text>
            </li>
            <li>
              <Text size={200}>Remove unused models to free up space</Text>
            </li>
          </ul>
          <Text
            size={200}
            style={{
              marginTop: tokens.spacingVerticalS,
              fontStyle: 'italic',
              color: tokens.colorNeutralForeground3,
              display: 'block',
            }}
          >
            To download new models, visit the <strong>Local Engines</strong> tab or the{' '}
            <strong>Downloads</strong> page.
          </Text>
        </Card>

        {engines.map((engine) => (
          <div key={engine.id} className={styles.engineSection}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: tokens.spacingVerticalM,
                cursor: 'pointer',
                padding: tokens.spacingVerticalS,
                backgroundColor: tokens.colorNeutralBackground2,
                borderRadius: tokens.borderRadiusMedium,
              }}
              onClick={() => toggleEngine(engine.id)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  toggleEngine(engine.id);
                }
              }}
              role="button"
              tabIndex={0}
            >
              <div>
                <Title3>{engine.name}</Title3>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  {engine.description}
                </Text>
              </div>
              <Text size={300}>{expandedEngines.has(engine.id) ? '▼' : '▶'}</Text>
            </div>

            {expandedEngines.has(engine.id) && (
              <Card style={{ padding: tokens.spacingVerticalM }}>
                <ModelManager engineId={engine.id} engineName={engine.name} />
              </Card>
            )}
          </div>
        ))}
      </div>
    </Card>
  );
}
