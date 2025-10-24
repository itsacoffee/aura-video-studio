import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';
import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Text,
  Button,
  Switch,
  Field,
  Radio,
  RadioGroup,
  Slider,
  Checkbox,
  Spinner,
} from '@fluentui/react-components';
import {
  Save24Regular,
  ArrowReset24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  panel: {
    padding: tokens.spacingVerticalXL,
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  sectionHeader: {
    marginBottom: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  field: {
    marginBottom: tokens.spacingVerticalM,
  },
  checkboxGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  providerCheckboxes: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  sliderContainer: {
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalXXL,
    paddingTop: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
});

interface AIOptimizationSettings {
  enabled: boolean;
  level: 'Conservative' | 'Balanced' | 'Aggressive';
  autoRegenerateIfLowQuality: boolean;
  minimumQualityThreshold: number;
  trackPerformanceData: boolean;
  shareAnonymousAnalytics: boolean;
  optimizationMetrics: string[];
  enabledProviders: string[];
  selectionMode: 'Automatic' | 'Manual';
  learningMode: 'Passive' | 'Normal' | 'Aggressive';
}

export function AIOptimizationPanel() {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [settings, setSettings] = useState<AIOptimizationSettings>({
    enabled: false,
    level: 'Balanced',
    autoRegenerateIfLowQuality: false,
    minimumQualityThreshold: 75,
    trackPerformanceData: true,
    shareAnonymousAnalytics: false,
    optimizationMetrics: ['Engagement', 'Quality', 'Authenticity'],
    enabledProviders: ['Ollama', 'OpenAI', 'Gemini', 'Azure'],
    selectionMode: 'Automatic',
    learningMode: 'Normal',
  });

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);
      const response = await fetch(`${apiUrl}/settings/ai-optimization`);
      const data = await response.json();
      
      if (data.success && data.settings) {
        setSettings(data.settings);
      }
    } catch (error) {
      console.error('Failed to load AI optimization settings:', error);
    } finally {
      setLoading(false);
    }
  };

  const saveSettings = async () => {
    try {
      setSaving(true);
      const response = await fetch(`${apiUrl}/settings/ai-optimization`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(settings),
      });

      const data = await response.json();
      
      if (data.success) {
        console.log('Settings saved successfully');
      } else {
        console.error('Failed to save settings:', data.error);
      }
    } catch (error) {
      console.error('Failed to save AI optimization settings:', error);
    } finally {
      setSaving(false);
    }
  };

  const resetToDefaults = async () => {
    try {
      setSaving(true);
      const response = await fetch(`${apiUrl}/settings/ai-optimization/reset`, {
        method: 'POST',
      });

      const data = await response.json();
      
      if (data.success && data.settings) {
        setSettings(data.settings);
      }
    } catch (error) {
      console.error('Failed to reset settings:', error);
    } finally {
      setSaving(false);
    }
  };

  const toggleMetric = (metric: string) => {
    setSettings((prev) => ({
      ...prev,
      optimizationMetrics: prev.optimizationMetrics.includes(metric)
        ? prev.optimizationMetrics.filter((m) => m !== metric)
        : [...prev.optimizationMetrics, metric],
    }));
  };

  const toggleProvider = (provider: string) => {
    setSettings((prev) => ({
      ...prev,
      enabledProviders: prev.enabledProviders.includes(provider)
        ? prev.enabledProviders.filter((p) => p !== provider)
        : [...prev.enabledProviders, provider],
    }));
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading settings..." />
      </div>
    );
  }

  return (
    <Card className={styles.panel}>
      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <Title3>AI Content Optimization</Title3>
          <Text className={styles.subtitle}>
            Improve content quality using ML-driven optimization. All features are opt-in.
          </Text>
        </div>

        <Field className={styles.field}>
          <Switch
            checked={settings.enabled}
            onChange={(_, data) =>
              setSettings((prev) => ({ ...prev, enabled: data.checked }))
            }
            label="Enable AI content optimization"
          />
          <Text size={200} className={styles.subtitle}>
            Use machine learning to improve content quality and engagement
          </Text>
        </Field>
      </div>

      {settings.enabled && (
        <>
          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title3>Optimization Level</Title3>
              <Text className={styles.subtitle}>
                How aggressively to optimize content
              </Text>
            </div>

            <RadioGroup
              value={settings.level}
              onChange={(_, data) =>
                setSettings((prev) => ({
                  ...prev,
                  level: data.value as AIOptimizationSettings['level'],
                }))
              }
            >
              <Radio value="Conservative" label="Conservative" />
              <Text size={200} className={styles.subtitle}>
                Minimal optimization, preserve user input
              </Text>
              <Radio value="Balanced" label="Balanced" />
              <Text size={200} className={styles.subtitle}>
                Moderate optimization balancing quality and user intent
              </Text>
              <Radio value="Aggressive" label="Aggressive" />
              <Text size={200} className={styles.subtitle}>
                Maximum optimization for best quality
              </Text>
            </RadioGroup>
          </div>

          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title3>Quality Threshold</Title3>
            </div>

            <Field className={styles.field}>
              <Switch
                checked={settings.autoRegenerateIfLowQuality}
                onChange={(_, data) =>
                  setSettings((prev) => ({
                    ...prev,
                    autoRegenerateIfLowQuality: data.checked,
                  }))
                }
                label="Auto-regenerate low quality content"
              />
            </Field>

            {settings.autoRegenerateIfLowQuality && (
              <div className={styles.sliderContainer}>
                <Field label={`Minimum quality score: ${settings.minimumQualityThreshold}`}>
                  <Slider
                    min={0}
                    max={100}
                    value={settings.minimumQualityThreshold}
                    onChange={(_, data) =>
                      setSettings((prev) => ({
                        ...prev,
                        minimumQualityThreshold: data.value,
                      }))
                    }
                  />
                </Field>
              </div>
            )}
          </div>

          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title3>Optimize For</Title3>
              <Text className={styles.subtitle}>
                Select which metrics to prioritize
              </Text>
            </div>

            <div className={styles.checkboxGroup}>
              <Checkbox
                checked={settings.optimizationMetrics.includes('Engagement')}
                onChange={() => toggleMetric('Engagement')}
                label="Engagement"
              />
              <Checkbox
                checked={settings.optimizationMetrics.includes('Quality')}
                onChange={() => toggleMetric('Quality')}
                label="Quality"
              />
              <Checkbox
                checked={settings.optimizationMetrics.includes('Authenticity')}
                onChange={() => toggleMetric('Authenticity')}
                label="Authenticity"
              />
              <Checkbox
                checked={settings.optimizationMetrics.includes('Speed')}
                onChange={() => toggleMetric('Speed')}
                label="Speed"
              />
            </div>
          </div>

          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title3>Provider Settings</Title3>
            </div>

            <Field label="Provider selection:">
              <RadioGroup
                value={settings.selectionMode}
                onChange={(_, data) =>
                  setSettings((prev) => ({
                    ...prev,
                    selectionMode: data.value as AIOptimizationSettings['selectionMode'],
                  }))
                }
              >
                <Radio value="Automatic" label="Automatic" />
                <Radio value="Manual" label="Manual" />
              </RadioGroup>
            </Field>

            <Field label="Enabled providers:" className={styles.field}>
              <div className={styles.providerCheckboxes}>
                <Checkbox
                  checked={settings.enabledProviders.includes('Ollama')}
                  onChange={() => toggleProvider('Ollama')}
                  label="Ollama"
                />
                <Checkbox
                  checked={settings.enabledProviders.includes('OpenAI')}
                  onChange={() => toggleProvider('OpenAI')}
                  label="OpenAI"
                />
                <Checkbox
                  checked={settings.enabledProviders.includes('Gemini')}
                  onChange={() => toggleProvider('Gemini')}
                  label="Gemini"
                />
                <Checkbox
                  checked={settings.enabledProviders.includes('Azure')}
                  onChange={() => toggleProvider('Azure')}
                  label="Azure"
                />
              </div>
            </Field>
          </div>

          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title3>Privacy & Learning</Title3>
            </div>

            <Field className={styles.field}>
              <Switch
                checked={settings.trackPerformanceData}
                onChange={(_, data) =>
                  setSettings((prev) => ({
                    ...prev,
                    trackPerformanceData: data.checked,
                  }))
                }
                label="Track performance data"
              />
            </Field>

            <Field className={styles.field}>
              <Switch
                checked={settings.shareAnonymousAnalytics}
                onChange={(_, data) =>
                  setSettings((prev) => ({
                    ...prev,
                    shareAnonymousAnalytics: data.checked,
                  }))
                }
                label="Share anonymous analytics"
              />
            </Field>

            <Field label="Learning mode:">
              <RadioGroup
                value={settings.learningMode}
                onChange={(_, data) =>
                  setSettings((prev) => ({
                    ...prev,
                    learningMode: data.value as AIOptimizationSettings['learningMode'],
                  }))
                }
              >
                <Radio value="Passive" label="Passive" />
                <Radio value="Normal" label="Normal" />
                <Radio value="Aggressive" label="Aggressive" />
              </RadioGroup>
            </Field>
          </div>
        </>
      )}

      <div className={styles.actions}>
        <Button
          appearance="secondary"
          icon={<ArrowReset24Regular />}
          onClick={resetToDefaults}
          disabled={saving}
        >
          Reset to Defaults
        </Button>
        <Button
          appearance="primary"
          icon={<Save24Regular />}
          onClick={saveSettings}
          disabled={saving}
        >
          {saving ? 'Saving...' : 'Save Settings'}
        </Button>
      </div>
    </Card>
  );
}
