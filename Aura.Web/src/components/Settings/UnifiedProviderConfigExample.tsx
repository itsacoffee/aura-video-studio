/**
 * Example component demonstrating unified provider configuration
 * This shows how to integrate with the new /api/providers/config endpoints
 *
 * Key principles:
 * 1. Load configuration from backend (never from Electron or localStorage)
 * 2. Update non-secret config via updateProviderConfiguration
 * 3. Update secrets via updateProviderSecrets
 * 4. Never store provider URLs in Electron or browser storage
 */

import {
  makeStyles,
  tokens,
  Title3,
  Button,
  Input,
  Field,
  Card,
  Spinner,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import { useState, useEffect } from 'react';
import {
  getProviderConfiguration,
  updateProviderConfiguration,
  updateProviderSecrets,
} from '../../services/api/providerConfigClient';
import type {
  ProviderConfiguration,
  ProviderConfigurationUpdate,
  ProviderSecretsUpdate,
} from '../../services/api/providerConfigClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
});

/**
 * Example component showing how to use unified provider configuration
 */
export function UnifiedProviderConfigExample() {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Configuration state (loaded but not directly used in render - used to populate form fields)
  const [_config, setConfig] = useState<ProviderConfiguration | null>(null);

  // Form state for editing
  const [openAiEndpoint, setOpenAiEndpoint] = useState('');
  const [ollamaUrl, setOllamaUrl] = useState('');
  const [ollamaModel, setOllamaModel] = useState('');
  const [stableDiffusionUrl, setStableDiffusionUrl] = useState('');

  // Secret fields (masked in UI)
  const [openAiApiKey, setOpenAiApiKey] = useState('');

  // Load configuration on mount
  useEffect(() => {
    loadConfiguration();
  }, []);

  const loadConfiguration = async () => {
    try {
      setLoading(true);
      setError(null);

      const loadedConfig = await getProviderConfiguration();
      setConfig(loadedConfig);

      // Populate form fields
      setOpenAiEndpoint(loadedConfig.openAi.endpoint || 'https://api.openai.com/v1');
      setOllamaUrl(loadedConfig.ollama.url || 'http://127.0.0.1:11434');
      setOllamaModel(loadedConfig.ollama.model || 'llama3.1:8b-q4_k_m');
      setStableDiffusionUrl(loadedConfig.stableDiffusion.url || 'http://127.0.0.1:7860');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load configuration';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveConfiguration = async () => {
    try {
      setSaving(true);
      setError(null);
      setSuccess(null);

      // Update non-secret configuration
      const configUpdate: ProviderConfigurationUpdate = {
        openAi: {
          endpoint: openAiEndpoint,
        },
        ollama: {
          url: ollamaUrl,
          model: ollamaModel,
        },
        stableDiffusion: {
          url: stableDiffusionUrl,
        },
      };

      await updateProviderConfiguration(configUpdate);

      setSuccess('Configuration updated successfully');

      // Reload to verify
      await loadConfiguration();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save configuration';
      setError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleSaveApiKey = async () => {
    try {
      setSaving(true);
      setError(null);
      setSuccess(null);

      if (!openAiApiKey.trim()) {
        setError('API key cannot be empty');
        return;
      }

      // Update secrets via dedicated endpoint
      const secretsUpdate: ProviderSecretsUpdate = {
        openAiApiKey: openAiApiKey,
      };

      await updateProviderSecrets(secretsUpdate);

      setSuccess('API key updated successfully');

      // Clear the input field for security
      setOpenAiApiKey('');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save API key';
      setError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading provider configuration..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Title3>Unified Provider Configuration (Example)</Title3>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      {success && (
        <MessageBar intent="success">
          <MessageBarBody>
            <MessageBarTitle>Success</MessageBarTitle>
            {success}
          </MessageBarBody>
        </MessageBar>
      )}

      <Card>
        <div className={styles.section}>
          <Title3>OpenAI Configuration</Title3>

          <Field label="OpenAI Endpoint">
            <Input
              value={openAiEndpoint}
              onChange={(e) => setOpenAiEndpoint(e.target.value)}
              placeholder="https://api.openai.com/v1"
            />
          </Field>

          <Field label="OpenAI API Key (Secret)">
            <Input
              type="password"
              value={openAiApiKey}
              onChange={(e) => setOpenAiApiKey(e.target.value)}
              placeholder="sk-..."
            />
          </Field>

          <div className={styles.actions}>
            <Button
              appearance="primary"
              onClick={handleSaveApiKey}
              disabled={saving || !openAiApiKey.trim()}
            >
              Update API Key
            </Button>
          </div>
        </div>
      </Card>

      <Card>
        <div className={styles.section}>
          <Title3>Ollama Configuration</Title3>

          <Field label="Ollama URL">
            <Input
              value={ollamaUrl}
              onChange={(e) => setOllamaUrl(e.target.value)}
              placeholder="http://127.0.0.1:11434"
            />
          </Field>

          <Field label="Ollama Model">
            <Input
              value={ollamaModel}
              onChange={(e) => setOllamaModel(e.target.value)}
              placeholder="llama3.1:8b-q4_k_m"
            />
          </Field>
        </div>
      </Card>

      <Card>
        <div className={styles.section}>
          <Title3>Stable Diffusion Configuration</Title3>

          <Field label="Stable Diffusion WebUI URL">
            <Input
              value={stableDiffusionUrl}
              onChange={(e) => setStableDiffusionUrl(e.target.value)}
              placeholder="http://127.0.0.1:7860"
            />
          </Field>
        </div>
      </Card>

      <div className={styles.actions}>
        <Button appearance="primary" onClick={handleSaveConfiguration} disabled={saving}>
          {saving ? 'Saving...' : 'Save Configuration'}
        </Button>

        <Button onClick={loadConfiguration} disabled={loading || saving}>
          Reload
        </Button>
      </div>
    </div>
  );
}
