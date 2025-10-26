import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Dropdown,
  Option,
  Slider,
  Card,
  Field,
  Switch,
  Badge,
} from '@fluentui/react-components';
import { Save24Regular, FlashFlow24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
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
    marginBottom: tokens.spacingVerticalL,
  },
  warningCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    marginBottom: tokens.spacingVerticalL,
  },
  gridLayout: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
  infoIcon: {
    color: tokens.colorNeutralForeground3,
    verticalAlign: 'middle',
    marginLeft: tokens.spacingHorizontalXS,
  },
  hardwareBadge: {
    marginLeft: tokens.spacingHorizontalS,
  },
});

const QUALITY_MODES = [
  { value: 'draft', label: 'Draft (Fastest)', description: 'Quick preview quality' },
  {
    value: 'standard',
    label: 'Standard (Balanced)',
    description: 'Good quality, reasonable speed',
  },
  { value: 'high', label: 'High Quality', description: 'Better quality, slower render' },
  { value: 'maximum', label: 'Maximum Quality', description: 'Best quality, slowest render' },
];

const ENCODER_PRIORITY = [
  { value: 'auto', label: 'Auto (Recommended)' },
  { value: 'nvenc', label: 'NVIDIA NVENC (Hardware)' },
  { value: 'qsv', label: 'Intel QuickSync (Hardware)' },
  { value: 'amf', label: 'AMD AMF (Hardware)' },
  { value: 'x264', label: 'x264 (Software, Slower)' },
];

export function PerformanceSettingsTab() {
  const styles = useStyles();
  const [modified, setModified] = useState(false);
  const [saving, setSaving] = useState(false);

  // Performance settings state
  const [qualityMode, setQualityMode] = useState('standard');
  const [enableHardwareAccel, setEnableHardwareAccel] = useState(true);
  const [encoderPriority, setEncoderPriority] = useState('auto');
  const [maxConcurrentJobs, setMaxConcurrentJobs] = useState(1);
  const [maxRenderThreads, setMaxRenderThreads] = useState(0); // 0 = auto
  const [enableProxyGeneration, setEnableProxyGeneration] = useState(false);
  const [proxyQuality, setProxyQuality] = useState(50);
  const [enablePreviewCache, setEnablePreviewCache] = useState(true);
  const [cacheSize, setCacheSize] = useState(5000); // MB
  const [enableGPUAccel, setEnableGPUAccel] = useState(true);
  const [gpuMemoryLimit, setGpuMemoryLimit] = useState(0); // 0 = auto
  const [enableBackgroundRendering, setEnableBackgroundRendering] = useState(false);
  const [autoPauseOnBattery, setAutoPauseOnBattery] = useState(true);
  const [detectedHardware, setDetectedHardware] = useState<any>(null);

  useEffect(() => {
    fetchPerformanceSettings();
    fetchHardwareInfo();
  }, []);

  const fetchPerformanceSettings = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/performance'));
      if (response.ok) {
        const data = await response.json();
        setQualityMode(data.qualityMode || 'standard');
        setEnableHardwareAccel(data.enableHardwareAccel !== false);
        setEncoderPriority(data.encoderPriority || 'auto');
        setMaxConcurrentJobs(data.maxConcurrentJobs || 1);
        setMaxRenderThreads(data.maxRenderThreads || 0);
        setEnableProxyGeneration(data.enableProxyGeneration || false);
        setProxyQuality(data.proxyQuality || 50);
        setEnablePreviewCache(data.enablePreviewCache !== false);
        setCacheSize(data.cacheSize || 5000);
        setEnableGPUAccel(data.enableGPUAccel !== false);
        setGpuMemoryLimit(data.gpuMemoryLimit || 0);
        setEnableBackgroundRendering(data.enableBackgroundRendering || false);
        setAutoPauseOnBattery(data.autoPauseOnBattery !== false);
      }
    } catch (error) {
      console.error('Error fetching performance settings:', error);
    }
  };

  const fetchHardwareInfo = async () => {
    try {
      const response = await fetch(apiUrl('/api/hardware/info'));
      if (response.ok) {
        const data = await response.json();
        setDetectedHardware(data);
      }
    } catch (error) {
      console.error('Error fetching hardware info:', error);
    }
  };

  const savePerformanceSettings = async () => {
    setSaving(true);
    try {
      const response = await fetch('/api/settings/performance', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          qualityMode,
          enableHardwareAccel,
          encoderPriority,
          maxConcurrentJobs,
          maxRenderThreads,
          enableProxyGeneration,
          proxyQuality,
          enablePreviewCache,
          cacheSize,
          enableGPUAccel,
          gpuMemoryLimit,
          enableBackgroundRendering,
          autoPauseOnBattery,
        }),
      });
      if (response.ok) {
        alert('Performance settings saved successfully');
        setModified(false);
      } else {
        alert('Error saving performance settings');
      }
    } catch (error) {
      console.error('Error saving performance settings:', error);
      alert('Error saving performance settings');
    } finally {
      setSaving(false);
    }
  };

  const runBenchmark = async () => {
    try {
      const response = await fetch('/api/hardware/benchmark', { method: 'POST' });
      if (response.ok) {
        const results = await response.json();
        alert(
          `Benchmark completed!\n\nCPU Score: ${results.cpuScore}\nGPU Score: ${results.gpuScore}\nRecommended Quality Mode: ${results.recommendedMode}`
        );
      }
    } catch (error) {
      console.error('Error running benchmark:', error);
      alert('Error running benchmark');
    }
  };

  return (
    <Card className={styles.section}>
      <Title2>
        Performance Settings
        <FlashFlow24Regular
          style={{ marginLeft: tokens.spacingHorizontalS, verticalAlign: 'middle' }}
        />
      </Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Optimize rendering performance and resource usage
      </Text>

      {detectedHardware && (
        <Card className={styles.infoCard}>
          <Text weight="semibold" size={300}>
            🖥️ Detected Hardware
          </Text>
          <div style={{ marginTop: tokens.spacingVerticalS }}>
            <Text size={200}>
              CPU: {detectedHardware.cpuName || 'Unknown'} ({detectedHardware.cpuCores || 0} cores)
            </Text>
            <br />
            <Text size={200}>
              GPU: {detectedHardware.gpuName || 'Unknown'}
              {detectedHardware.gpuVram &&
                ` (${Math.floor(detectedHardware.gpuVram / 1024)} GB VRAM)`}
            </Text>
            <br />
            <Text size={200}>RAM: {detectedHardware.ramGb || 0} GB</Text>
            <br />
            <Text size={200}>
              Tier:{' '}
              <Badge appearance="filled" color="brand">
                {detectedHardware.tier || 'Unknown'}
              </Badge>
            </Text>
          </div>
        </Card>
      )}

      <div className={styles.form}>
        {/* Quality Mode */}
        <Field label="Quality Mode">
          <Dropdown
            value={qualityMode}
            onOptionSelect={(_, data) => {
              setQualityMode(data.optionValue || 'standard');
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            {QUALITY_MODES.map((mode) => (
              <Option
                key={mode.value}
                value={mode.value}
                text={`${mode.label} - ${mode.description}`}
              >
                {mode.label} - {mode.description}
              </Option>
            ))}
          </Dropdown>
        </Field>

        {/* Hardware Acceleration */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Hardware Acceleration
        </Text>

        <Field label="Enable Hardware Acceleration">
          <Switch
            checked={enableHardwareAccel}
            onChange={(_, data) => {
              setEnableHardwareAccel(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enableHardwareAccel ? 'Enabled' : 'Disabled'} - Use GPU/hardware encoders for faster
            rendering
          </Text>
        </Field>

        {enableHardwareAccel && (
          <>
            <Field label="Encoder Priority">
              <Dropdown
                value={encoderPriority}
                onOptionSelect={(_, data) => {
                  setEncoderPriority(data.optionValue || 'auto');
                  setModified(true);
                }}
                style={{ width: '100%' }}
              >
                {ENCODER_PRIORITY.map((encoder) => (
                  <Option key={encoder.value} value={encoder.value} text={encoder.label}>
                    {encoder.label}
                  </Option>
                ))}
              </Dropdown>
            </Field>

            <Field label="GPU Acceleration">
              <Switch
                checked={enableGPUAccel}
                onChange={(_, data) => {
                  setEnableGPUAccel(data.checked);
                  setModified(true);
                }}
              />
              <Text size={200}>
                {enableGPUAccel ? 'Enabled' : 'Disabled'} - Use GPU for effects processing
              </Text>
            </Field>

            {enableGPUAccel && (
              <Field
                label={`GPU Memory Limit: ${gpuMemoryLimit === 0 ? 'Auto' : `${gpuMemoryLimit} MB`}`}
                hint="Set to 0 for automatic detection"
              >
                <Slider
                  min={0}
                  max={16384}
                  step={512}
                  value={gpuMemoryLimit}
                  onChange={(_, data) => {
                    setGpuMemoryLimit(data.value);
                    setModified(true);
                  }}
                />
              </Field>
            )}
          </>
        )}

        {/* Rendering Options */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Rendering Options
        </Text>

        <Field
          label={`Max Concurrent Jobs: ${maxConcurrentJobs}`}
          hint="Number of videos that can render simultaneously"
        >
          <Slider
            min={1}
            max={4}
            step={1}
            value={maxConcurrentJobs}
            onChange={(_, data) => {
              setMaxConcurrentJobs(data.value);
              setModified(true);
            }}
          />
        </Field>

        <Field
          label={`Render Threads: ${maxRenderThreads === 0 ? 'Auto' : maxRenderThreads}`}
          hint="CPU threads to use for rendering (0 = auto-detect)"
        >
          <Slider
            min={0}
            max={detectedHardware?.cpuCores || 16}
            step={1}
            value={maxRenderThreads}
            onChange={(_, data) => {
              setMaxRenderThreads(data.value);
              setModified(true);
            }}
          />
        </Field>

        <Field label="Background Rendering">
          <Switch
            checked={enableBackgroundRendering}
            onChange={(_, data) => {
              setEnableBackgroundRendering(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enableBackgroundRendering ? 'Enabled' : 'Disabled'} - Continue rendering when window is
            minimized
          </Text>
        </Field>

        <Field label="Auto-Pause on Battery">
          <Switch
            checked={autoPauseOnBattery}
            onChange={(_, data) => {
              setAutoPauseOnBattery(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {autoPauseOnBattery ? 'Enabled' : 'Disabled'} - Pause rendering when on battery power
            (laptops)
          </Text>
        </Field>

        {/* Preview & Caching */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Preview & Caching
        </Text>

        <Field label="Enable Proxy Generation">
          <Switch
            checked={enableProxyGeneration}
            onChange={(_, data) => {
              setEnableProxyGeneration(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enableProxyGeneration ? 'Enabled' : 'Disabled'} - Generate lower-res proxies for 4K/8K
            footage
          </Text>
        </Field>

        {enableProxyGeneration && (
          <Field
            label={`Proxy Quality: ${proxyQuality}%`}
            hint="Quality of proxy files (lower = smaller files)"
          >
            <Slider
              min={25}
              max={75}
              step={5}
              value={proxyQuality}
              onChange={(_, data) => {
                setProxyQuality(data.value);
                setModified(true);
              }}
            />
          </Field>
        )}

        <Field label="Enable Preview Cache">
          <Switch
            checked={enablePreviewCache}
            onChange={(_, data) => {
              setEnablePreviewCache(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enablePreviewCache ? 'Enabled' : 'Disabled'} - Cache rendered previews for smoother
            playback
          </Text>
        </Field>

        {enablePreviewCache && (
          <Field
            label={`Cache Size: ${(cacheSize / 1024).toFixed(1)} GB`}
            hint="Maximum disk space for preview cache"
          >
            <Slider
              min={1000}
              max={20000}
              step={1000}
              value={cacheSize}
              onChange={(_, data) => {
                setCacheSize(data.value);
                setModified(true);
              }}
            />
          </Field>
        )}

        {/* Benchmark */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          System Benchmark
        </Text>

        <Card className={styles.infoCard}>
          <Text size={200}>
            Run a benchmark to test your system's rendering performance and get recommendations for
            optimal settings.
          </Text>
          <Button
            appearance="secondary"
            onClick={runBenchmark}
            style={{ marginTop: tokens.spacingVerticalM }}
          >
            Run Benchmark
          </Button>
        </Card>

        {modified && (
          <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
            ⚠️ You have unsaved changes
          </Text>
        )}

        <div className={styles.actions}>
          <Button
            appearance="secondary"
            onClick={() => {
              fetchPerformanceSettings();
              setModified(false);
            }}
          >
            Reset
          </Button>
          <Button
            appearance="primary"
            icon={<Save24Regular />}
            onClick={savePerformanceSettings}
            disabled={!modified || saving}
          >
            {saving ? 'Saving...' : 'Save Performance Settings'}
          </Button>
        </div>
      </div>
    </Card>
  );
}
