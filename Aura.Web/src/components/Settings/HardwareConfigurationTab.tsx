import {
  Button,
  Card,
  Field,
  Select,
  Spinner,
  Switch,
  Text,
  Title2,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle24Filled, DismissCircle24Filled } from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  statusCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
});

interface HardwareConfig {
  preferredGpuId: string;
  enableHardwareAcceleration: boolean;
  preferredEncoder: string;
  encodingPreset: string;
  useGpuForImageGeneration: boolean;
  maxConcurrentJobs: number;
}

interface GPU {
  id: string;
  vendor: string;
  model: string;
  vramGB: number;
  capabilities: {
    nvenc: boolean;
    amf: boolean;
    quickSync: boolean;
  };
  recommended: boolean;
}

interface Encoder {
  id: string;
  name: string;
  type: string;
  vendor?: string;
  available: boolean;
  quality: string;
  speed: string;
  recommended: boolean;
}

interface Preset {
  id: string;
  name: string;
  description: string;
  speed: string;
  quality: string;
  ffmpegPreset: string;
  recommendedFor: string[];
  default_?: boolean;
}

export function HardwareConfigurationTab() {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  
  const [config, setConfig] = useState<HardwareConfig>({
    preferredGpuId: 'auto',
    enableHardwareAcceleration: true,
    preferredEncoder: 'auto',
    encodingPreset: 'balanced',
    useGpuForImageGeneration: true,
    maxConcurrentJobs: 1,
  });
  
  const [gpus, setGpus] = useState<GPU[]>([]);
  const [encoders, setEncoders] = useState<Encoder[]>([]);
  const [presets, setPresets] = useState<Preset[]>([]);
  const [testResults, setTestResults] = useState<{
    nvenc?: { available: boolean; message: string };
    amf?: { available: boolean; message: string };
    quickSync?: { available: boolean; message: string };
  }>({});

  useEffect(() => {
    loadHardwareConfig();
  }, []);

  const loadHardwareConfig = async () => {
    setLoading(true);
    try {
      const [configResponse, gpusResponse, encodersResponse, presetsResponse] = await Promise.all([
        fetch(apiUrl('/api/hardware-config')),
        fetch(apiUrl('/api/hardware-config/gpus')),
        fetch(apiUrl('/api/hardware-config/encoders')),
        fetch(apiUrl('/api/hardware-config/presets')),
      ]);

      if (configResponse.ok) {
        const data = await configResponse.json();
        if (data.config) {
          setConfig(data.config);
        }
      }

      if (gpusResponse.ok) {
        const data = await gpusResponse.json();
        setGpus(data.gpus || []);
      }

      if (encodersResponse.ok) {
        const data = await encodersResponse.json();
        setEncoders(data.encoders || []);
      }

      if (presetsResponse.ok) {
        const data = await presetsResponse.json();
        setPresets(data.presets || []);
      }
    } catch (error) {
      console.error('Error loading hardware config:', error);
    } finally {
      setLoading(false);
    }
  };

  const saveConfig = async () => {
    setSaving(true);
    try {
      const response = await fetch(apiUrl('/api/hardware-config'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config),
      });

      if (response.ok) {
        alert('Hardware configuration saved successfully!');
      } else {
        alert('Failed to save hardware configuration');
      }
    } catch (error) {
      console.error('Error saving config:', error);
      alert('Error saving hardware configuration');
    } finally {
      setSaving(false);
    }
  };

  const testAcceleration = async () => {
    setTesting(true);
    try {
      const response = await fetch(apiUrl('/api/hardware-config/test-acceleration'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({}),
      });

      if (response.ok) {
        const results = await response.json();
        setTestResults(results);
      }
    } catch (error) {
      console.error('Error testing acceleration:', error);
    } finally {
      setTesting(false);
    }
  };

  if (loading) {
    return (
      <Card className={styles.section}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          <Spinner size="small" />
          <Text>Loading hardware configuration...</Text>
        </div>
      </Card>
    );
  }

  const recommendedEncoder = encoders.find((e) => e.recommended);

  return (
    <div className={styles.container}>
      <Card className={styles.section}>
        <Title2>Hardware Configuration</Title2>
        <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
          Configure hardware acceleration and GPU preferences for video rendering and AI processing
        </Text>

        {gpus.length > 0 && (
          <Card className={styles.infoCard} style={{ marginBottom: tokens.spacingVerticalL }}>
            <Text weight="semibold" size={300}>
              ℹ️ Detected Hardware
            </Text>
            {gpus.map((gpu) => (
              <div key={gpu.id} style={{ marginTop: tokens.spacingVerticalS }}>
                <Text size={200}>
                  <strong>GPU:</strong> {gpu.vendor} {gpu.model} ({gpu.vramGB} GB VRAM)
                </Text>
                <Text
                  size={200}
                  style={{
                    marginTop: tokens.spacingVerticalXS,
                    marginLeft: tokens.spacingHorizontalM,
                    color: tokens.colorNeutralForeground3,
                  }}
                >
                  Hardware Acceleration: NVENC {gpu.capabilities.nvenc ? '✓' : '✗'} | AMF{' '}
                  {gpu.capabilities.amf ? '✓' : '✗'} | QuickSync{' '}
                  {gpu.capabilities.quickSync ? '✓' : '✗'}
                </Text>
              </div>
            ))}
          </Card>
        )}

        <div className={styles.form}>
          <Field label="GPU Selection">
            <Select
              value={config.preferredGpuId}
              onChange={(_, data) => setConfig({ ...config, preferredGpuId: data.value })}
            >
              <option value="auto">Auto-detect (Recommended)</option>
              {gpus.map((gpu) => (
                <option key={gpu.id} value={gpu.id}>
                  {gpu.vendor} {gpu.model}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Hardware Acceleration">
            <Switch
              checked={config.enableHardwareAcceleration}
              onChange={(_, data) =>
                setConfig({ ...config, enableHardwareAcceleration: data.checked })
              }
            />
            <Text size={200}>
              Enable hardware-accelerated encoding (NVENC, AMF, QuickSync) for faster rendering
            </Text>
          </Field>

          <Field label="Preferred Video Encoder">
            <Select
              value={config.preferredEncoder}
              onChange={(_, data) => setConfig({ ...config, preferredEncoder: data.value })}
              disabled={!config.enableHardwareAcceleration}
            >
              <option value="auto">Auto-select best encoder</option>
              {encoders
                .filter((e) => e.available)
                .map((encoder) => (
                  <option key={encoder.id} value={encoder.id}>
                    {encoder.name} {encoder.recommended ? '(Recommended)' : ''}
                  </option>
                ))}
            </Select>
            {recommendedEncoder && (
              <Text
                size={200}
                style={{ marginTop: tokens.spacingVerticalXS, color: tokens.colorNeutralForeground3 }}
              >
                Recommended: {recommendedEncoder.name} - Quality: {recommendedEncoder.quality}, Speed:{' '}
                {recommendedEncoder.speed}
              </Text>
            )}
          </Field>

          <Field label="Encoding Quality Preset">
            <Select
              value={config.encodingPreset}
              onChange={(_, data) => setConfig({ ...config, encodingPreset: data.value })}
            >
              {presets.map((preset) => (
                <option key={preset.id} value={preset.id}>
                  {preset.name} {preset.default_ ? '(Default)' : ''}
                </option>
              ))}
            </Select>
            {presets.find((p) => p.id === config.encodingPreset) && (
              <Text
                size={200}
                style={{ marginTop: tokens.spacingVerticalXS, color: tokens.colorNeutralForeground3 }}
              >
                {presets.find((p) => p.id === config.encodingPreset)?.description}
              </Text>
            )}
          </Field>

          <Field label="GPU for Image Generation">
            <Switch
              checked={config.useGpuForImageGeneration}
              onChange={(_, data) => setConfig({ ...config, useGpuForImageGeneration: data.checked })}
            />
            <Text size={200}>
              Use GPU for AI image generation (Stable Diffusion). Requires compatible GPU.
            </Text>
          </Field>

          <Field label="Maximum Concurrent Jobs" hint="Number of videos to render simultaneously">
            <Select
              value={config.maxConcurrentJobs.toString()}
              onChange={(_, data) =>
                setConfig({ ...config, maxConcurrentJobs: parseInt(data.value) })
              }
            >
              <option value="1">1 (Recommended)</option>
              <option value="2">2</option>
              <option value="3">3</option>
              <option value="4">4</option>
            </Select>
          </Field>
        </div>
      </Card>

      <Card className={styles.section}>
        <Title2>Hardware Acceleration Test</Title2>
        <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
          Test hardware acceleration capabilities of your system
        </Text>

        <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, marginBottom: tokens.spacingVerticalL }}>
          <Button appearance="secondary" onClick={testAcceleration} disabled={testing}>
            {testing ? 'Testing...' : 'Test Hardware Acceleration'}
          </Button>
        </div>

        {Object.keys(testResults).length > 0 && (
          <Card className={styles.statusCard}>
            <Text weight="semibold" size={300} style={{ marginBottom: tokens.spacingVerticalS }}>
              Test Results:
            </Text>
            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS }}>
              {testResults.nvenc && (
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  {testResults.nvenc.available ? (
                    <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
                  ) : (
                    <DismissCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
                  )}
                  <Text size={200}>
                    <strong>NVIDIA NVENC:</strong> {testResults.nvenc.message}
                  </Text>
                </div>
              )}
              {testResults.amf && (
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  {testResults.amf.available ? (
                    <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
                  ) : (
                    <DismissCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
                  )}
                  <Text size={200}>
                    <strong>AMD AMF:</strong> {testResults.amf.message}
                  </Text>
                </div>
              )}
              {testResults.quickSync && (
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  {testResults.quickSync.available ? (
                    <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
                  ) : (
                    <DismissCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
                  )}
                  <Text size={200}>
                    <strong>Intel Quick Sync:</strong> {testResults.quickSync.message}
                  </Text>
                </div>
              )}
            </div>
          </Card>
        )}
      </Card>

      <div className={styles.actions}>
        <Button appearance="primary" onClick={saveConfig} disabled={saving}>
          {saving ? 'Saving...' : 'Save Hardware Configuration'}
        </Button>
      </div>
    </div>
  );
}
