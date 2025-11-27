/**
 * ProviderConfigModal - Modal component for configuring AI provider settings
 *
 * Allows editing provider configuration including API key, endpoint,
 * model settings, and enable/disable options.
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  Button,
  Field,
  Input,
  Checkbox,
  makeStyles,
  tokens,
  Spinner,
  Text,
} from '@fluentui/react-components';
import { useCallback, useEffect, useState, type FC, type ChangeEvent } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  actions: {
    marginTop: tokens.spacingVerticalL,
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXL,
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  checkboxField: {
    marginTop: tokens.spacingVerticalS,
  },
});

interface ProviderConfig {
  apiKey?: string;
  endpoint?: string;
  model?: string;
  maxTokens?: number;
  temperature?: number;
  isEnabled?: boolean;
  hasFallback?: boolean;
  type?: string;
}

interface ProviderConfigModalProps {
  providerId: string;
  onClose: () => void;
  onSave: () => void;
}

export const ProviderConfigModal: FC<ProviderConfigModalProps> = ({
  providerId,
  onClose,
  onSave,
}) => {
  const styles = useStyles();
  const [config, setConfig] = useState<ProviderConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadConfig = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(apiUrl(`/api/providers/${providerId}/config`));
      if (!response.ok) {
        throw new Error(`Failed to load config: ${response.statusText}`);
      }
      const data = await response.json();
      setConfig(data);
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to load provider configuration';
      setError(errorMessage);
      console.error('Failed to load provider config:', err);
    } finally {
      setLoading(false);
    }
  }, [providerId]);

  useEffect(() => {
    loadConfig();
  }, [loadConfig]);

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      const response = await fetch(apiUrl(`/api/providers/${providerId}/config`), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config),
      });
      if (!response.ok) {
        throw new Error(`Failed to save config: ${response.statusText}`);
      }
      onSave();
      onClose();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to save provider configuration';
      setError(errorMessage);
      console.error('Failed to save provider config:', err);
    } finally {
      setSaving(false);
    }
  };

  const updateConfig = (key: keyof ProviderConfig, value: string | number | boolean) => {
    setConfig((prev) => (prev ? { ...prev, [key]: value } : { [key]: value }));
  };

  if (loading) {
    return (
      <Dialog open={true}>
        <DialogSurface>
          <DialogBody>
            <DialogContent>
              <div className={styles.loadingContainer}>
                <Spinner label="Loading configuration..." />
              </div>
            </DialogContent>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    );
  }

  return (
    <Dialog
      open={true}
      onOpenChange={(_, data) => {
        if (!data.open) onClose();
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Configure Provider</DialogTitle>
          <DialogContent>
            {error && <Text className={styles.errorText}>{error}</Text>}
            <div className={styles.form}>
              <Field label="API Key">
                <Input
                  type="password"
                  value={config?.apiKey || ''}
                  onChange={(e: ChangeEvent<HTMLInputElement>) =>
                    updateConfig('apiKey', e.target.value)
                  }
                  placeholder="Enter API key"
                />
              </Field>
              <Field label="API Endpoint">
                <Input
                  value={config?.endpoint || ''}
                  onChange={(e: ChangeEvent<HTMLInputElement>) =>
                    updateConfig('endpoint', e.target.value)
                  }
                  placeholder="https://api.provider.com/v1"
                />
              </Field>
              {config?.type === 'llm' && (
                <>
                  <Field label="Model">
                    <Input
                      value={config?.model || ''}
                      onChange={(e: ChangeEvent<HTMLInputElement>) =>
                        updateConfig('model', e.target.value)
                      }
                      placeholder="gpt-4, claude-3-opus, etc."
                    />
                  </Field>
                  <Field label="Max Tokens">
                    <Input
                      type="number"
                      value={config?.maxTokens?.toString() || ''}
                      onChange={(e: ChangeEvent<HTMLInputElement>) =>
                        updateConfig('maxTokens', parseInt(e.target.value, 10) || 0)
                      }
                      placeholder="4096"
                    />
                  </Field>
                  <Field label="Temperature">
                    <Input
                      type="number"
                      step="0.1"
                      min="0"
                      max="2"
                      value={config?.temperature?.toString() || ''}
                      onChange={(e: ChangeEvent<HTMLInputElement>) =>
                        updateConfig('temperature', parseFloat(e.target.value) || 0)
                      }
                      placeholder="0.7"
                    />
                  </Field>
                </>
              )}
              <div className={styles.checkboxField}>
                <Checkbox
                  label="Enable this provider"
                  checked={config?.isEnabled || false}
                  onChange={(_, data) => updateConfig('isEnabled', Boolean(data.checked))}
                />
              </div>
              <div className={styles.checkboxField}>
                <Checkbox
                  label="Enable fallback to other providers"
                  checked={config?.hasFallback || false}
                  onChange={(_, data) => updateConfig('hasFallback', Boolean(data.checked))}
                />
              </div>
            </div>
            <div className={styles.actions}>
              <Button onClick={onClose}>Cancel</Button>
              <Button appearance="primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default ProviderConfigModal;
