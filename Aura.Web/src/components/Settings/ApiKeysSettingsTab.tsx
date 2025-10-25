import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Input,
  Card,
  Field,
} from '@fluentui/react-components';
import { CheckmarkCircle24Filled, DismissCircle24Filled } from '@fluentui/react-icons';
import type { ApiKeysSettings } from '../../types/settings';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  inputWithButton: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-start',
  },
  testResult: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface ApiKeysSettingsTabProps {
  settings: ApiKeysSettings;
  onChange: (settings: ApiKeysSettings) => void;
  onSave: () => void;
  onTestApiKey: (provider: string, apiKey: string) => Promise<{ success: boolean; message: string }>;
  hasChanges: boolean;
}

interface TestResult {
  success: boolean;
  message: string;
}

export function ApiKeysSettingsTab({
  settings,
  onChange,
  onSave,
  onTestApiKey,
  hasChanges,
}: ApiKeysSettingsTabProps) {
  const styles = useStyles();
  const [testResults, setTestResults] = useState<Record<string, TestResult>>({});
  const [testing, setTesting] = useState<Record<string, boolean>>({});

  const updateSetting = <K extends keyof ApiKeysSettings>(
    key: K,
    value: ApiKeysSettings[K]
  ) => {
    onChange({ ...settings, [key]: value });
    // Clear test result when value changes
    setTestResults((prev) => ({ ...prev, [key]: undefined as any }));
  };

  const handleTest = async (provider: string, key: keyof ApiKeysSettings) => {
    const apiKey = settings[key];
    if (!apiKey) {
      return;
    }

    setTesting((prev) => ({ ...prev, [key]: true }));
    try {
      const result = await onTestApiKey(provider, apiKey);
      setTestResults((prev) => ({ ...prev, [key]: result }));
    } finally {
      setTesting((prev) => ({ ...prev, [key]: false }));
    }
  };

  const renderTestResult = (key: string) => {
    const result = testResults[key];
    if (!result) return null;

    return (
      <div className={styles.testResult}>
        {result.success ? (
          <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
        ) : (
          <DismissCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
        )}
        <Text size={200}>{result.message}</Text>
      </div>
    );
  };

  return (
    <Card className={styles.section}>
      <Title2>API Keys</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Configure API keys for external services. Keys are stored securely and never exposed in
        plain text.
      </Text>

      <div className={styles.infoBox}>
        <Text weight="semibold" size={300}>
          🔒 Security Notice
        </Text>
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
          API keys are encrypted before storage. Never share your keys or commit them to version
          control. You can test connections to verify keys are working.
        </Text>
      </div>

      <div className={styles.form}>
        <Field
          label="OpenAI API Key"
          hint="Required for GPT-based script generation. Get your key from platform.openai.com"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              type="password"
              value={settings.openAI}
              onChange={(e) => updateSetting('openAI', e.target.value)}
              placeholder="sk-..."
            />
            <Button
              onClick={() => handleTest('openai', 'openAI')}
              disabled={!settings.openAI || testing.openAI}
            >
              {testing.openAI ? 'Testing...' : 'Test'}
            </Button>
          </div>
          {renderTestResult('openAI')}
        </Field>

        <Field
          label="Anthropic API Key (Claude)"
          hint="For Claude-based AI features. Get your key from console.anthropic.com"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              type="password"
              value={settings.anthropic}
              onChange={(e) => updateSetting('anthropic', e.target.value)}
              placeholder="sk-ant-..."
            />
            <Button
              onClick={() => handleTest('anthropic', 'anthropic')}
              disabled={!settings.anthropic || testing.anthropic}
            >
              {testing.anthropic ? 'Testing...' : 'Test'}
            </Button>
          </div>
          {renderTestResult('anthropic')}
        </Field>

        <Field
          label="Stability AI API Key"
          hint="For AI image generation. Get your key from platform.stability.ai"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              type="password"
              value={settings.stabilityAI}
              onChange={(e) => updateSetting('stabilityAI', e.target.value)}
              placeholder="sk-..."
            />
            <Button
              onClick={() => handleTest('stabilityai', 'stabilityAI')}
              disabled={!settings.stabilityAI || testing.stabilityAI}
            >
              {testing.stabilityAI ? 'Testing...' : 'Test'}
            </Button>
          </div>
          {renderTestResult('stabilityAI')}
        </Field>

        <Field
          label="ElevenLabs API Key"
          hint="For high-quality voice synthesis. Get your key from elevenlabs.io"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              type="password"
              value={settings.elevenLabs}
              onChange={(e) => updateSetting('elevenLabs', e.target.value)}
              placeholder="Enter your ElevenLabs API key"
            />
            <Button
              onClick={() => handleTest('elevenlabs', 'elevenLabs')}
              disabled={!settings.elevenLabs || testing.elevenLabs}
            >
              {testing.elevenLabs ? 'Testing...' : 'Test'}
            </Button>
          </div>
          {renderTestResult('elevenLabs')}
        </Field>

        <Field
          label="Pexels API Key"
          hint="For stock video and images. Get your free key from pexels.com/api"
        >
          <Input
            type="password"
            value={settings.pexels}
            onChange={(e) => updateSetting('pexels', e.target.value)}
            placeholder="Enter your Pexels API key"
          />
        </Field>

        <Field
          label="Pixabay API Key"
          hint="For stock video and images. Get your free key from pixabay.com/api"
        >
          <Input
            type="password"
            value={settings.pixabay}
            onChange={(e) => updateSetting('pixabay', e.target.value)}
            placeholder="Enter your Pixabay API key"
          />
        </Field>

        <Field
          label="Unsplash API Key"
          hint="For stock images. Get your free key from unsplash.com/developers"
        >
          <Input
            type="password"
            value={settings.unsplash}
            onChange={(e) => updateSetting('unsplash', e.target.value)}
            placeholder="Enter your Unsplash API key"
          />
        </Field>

        <Field
          label="Google API Key"
          hint="For Google Cloud services (optional)"
        >
          <Input
            type="password"
            value={settings.google}
            onChange={(e) => updateSetting('google', e.target.value)}
            placeholder="Enter your Google API key"
          />
        </Field>

        <Field
          label="Azure API Key"
          hint="For Azure Cognitive Services (optional)"
        >
          <Input
            type="password"
            value={settings.azure}
            onChange={(e) => updateSetting('azure', e.target.value)}
            placeholder="Enter your Azure API key"
          />
        </Field>

        {hasChanges && (
          <div className={styles.infoBox}>
            <Text weight="semibold" style={{ color: tokens.colorPaletteYellowForeground1 }}>
              ⚠️ You have unsaved changes
            </Text>
          </div>
        )}

        <div>
          <Button appearance="primary" onClick={onSave} disabled={!hasChanges}>
            Save API Keys
          </Button>
        </div>
      </div>
    </Card>
  );
}
