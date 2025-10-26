import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  makeStyles,
  tokens,
  Dropdown,
  Option,
  Input,
  Field,
  Divider,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Caption1,
  Body1,
  Badge,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  ArrowExport24Regular,
  ArrowDownload24Regular,
  Warning24Regular,
  CheckmarkCircle24Regular,
} from '@fluentui/react-icons';
import { useState, useMemo } from 'react';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '700px',
    width: '100%',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  field: {
    flex: 1,
  },
  presetInfo: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  spec: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalM,
  },
  estimate: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  hardwareStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
});

export interface ExportDialogProps {
  open: boolean;
  onClose: () => void;
  onExport: (options: ExportOptions) => void;
  onAddToQueue: (options: ExportOptions) => void;
  timeline?: { totalDuration: number };
  hardwareAccelerationAvailable?: boolean;
  hardwareType?: string;
}

export interface ExportOptions {
  preset: string;
  resolution: { width: number; height: number }; // Preset resolution or custom if advanced settings used
  fps: number;
  videoBitrate: number; // Preset bitrate or custom if advanced settings used
  audioBitrate: number;
  quality: 'draft' | 'good' | 'high' | 'maximum';
  exportRange: 'entire' | 'selection';
  selectionStart?: number;
  selectionEnd?: number;
  outputPath: string;
  codec?: string; // Preset codec or custom if advanced settings used
}

const PRESETS = {
  'YouTube 1080p': {
    description: 'Standard HD quality for YouTube uploads',
    resolution: '1920x1080',
    codec: 'H.264',
    bitrate: '8 Mbps',
    aspectRatio: '16:9',
    platform: 'YouTube',
  },
  'YouTube 4K': {
    description: 'Ultra HD quality for YouTube 4K uploads',
    resolution: '3840x2160',
    codec: 'H.265',
    bitrate: '20 Mbps',
    aspectRatio: '16:9',
    platform: 'YouTube',
  },
  'Instagram Feed': {
    description: 'Square format optimized for Instagram feed posts',
    resolution: '1080x1080',
    codec: 'H.264',
    bitrate: '5 Mbps',
    aspectRatio: '1:1',
    platform: 'Instagram',
  },
  'Instagram Story': {
    description: 'Vertical format for Instagram Stories',
    resolution: '1080x1920',
    codec: 'H.264',
    bitrate: '5 Mbps',
    aspectRatio: '9:16',
    platform: 'Instagram',
  },
  TikTok: {
    description: 'Vertical format optimized for TikTok',
    resolution: '1080x1920',
    codec: 'H.264',
    bitrate: '5 Mbps',
    aspectRatio: '9:16',
    platform: 'TikTok',
  },
  Facebook: {
    description: 'Optimized for Facebook video posts',
    resolution: '1280x720',
    codec: 'H.264',
    bitrate: '4 Mbps',
    aspectRatio: '16:9',
    platform: 'Facebook',
  },
  Twitter: {
    description: 'Optimized for Twitter video posts',
    resolution: '1280x720',
    codec: 'H.264',
    bitrate: '5 Mbps',
    aspectRatio: '16:9',
    platform: 'Twitter',
  },
  LinkedIn: {
    description: 'Professional quality for LinkedIn posts',
    resolution: '1920x1080',
    codec: 'H.264',
    bitrate: '5 Mbps',
    aspectRatio: '16:9',
    platform: 'LinkedIn',
  },
  'Email/Web': {
    description: 'Small file size for email attachments and web embedding',
    resolution: '854x480',
    codec: 'H.264',
    bitrate: '2 Mbps',
    aspectRatio: '16:9',
    platform: 'Generic',
  },
  'Draft Preview': {
    description: 'Quick low-quality preview for reviewing edits',
    resolution: '1280x720',
    codec: 'H.264',
    bitrate: '3 Mbps',
    aspectRatio: '16:9',
    platform: 'Generic',
  },
  'Master Archive': {
    description: 'High quality archival format with excellent compression',
    resolution: '1920x1080',
    codec: 'H.265',
    bitrate: '15 Mbps',
    aspectRatio: '16:9',
    platform: 'Generic',
  },
  'WebM VP9': {
    description: 'Web-optimized format with excellent compression',
    resolution: '1920x1080',
    codec: 'VP9',
    bitrate: '6 Mbps',
    aspectRatio: '16:9',
    platform: 'Generic',
  },
  'ProRes 422 HQ': {
    description: 'Professional quality for editing and mastering',
    resolution: '1920x1080',
    codec: 'ProRes',
    bitrate: '120 Mbps',
    aspectRatio: '16:9',
    platform: 'Generic',
  },
  'Podcast Audio': {
    description: 'Audio-only export for podcasts',
    resolution: 'Audio Only',
    codec: 'MP3',
    bitrate: '128 kbps',
    aspectRatio: 'N/A',
    platform: 'Generic',
  },
};

export function ExportDialog({
  open,
  onClose,
  onExport,
  onAddToQueue,
  timeline,
  hardwareAccelerationAvailable = false,
  hardwareType = 'None',
}: ExportDialogProps) {
  const styles = useStyles();
  const [selectedPreset, setSelectedPreset] = useState('YouTube 1080p');
  const [outputPath, setOutputPath] = useState('');
  const [exportRange, setExportRange] = useState<'entire' | 'selection'>('entire');
  const [advancedSettings, setAdvancedSettings] = useState({
    enabled: false,
    codec: 'H.264',
    customBitrate: 0,
    customWidth: 0,
    customHeight: 0,
    bitrateMode: 'VBR', // CBR or VBR
    gopSize: 0, // Group of Pictures size (0 = auto)
    keyframeInterval: 0, // Keyframe interval in seconds (0 = auto)
    profile: 'high', // baseline, main, high
  });

  // Helper to determine if advanced settings should be considered active
  // Note: Codec selection is always available and doesn't require "enabling" advanced mode
  // Advanced mode only tracks custom bitrate/resolution overrides
  const shouldEnableAdvanced = (settings: typeof advancedSettings) => {
    return settings.customBitrate > 0 || settings.customWidth > 0 || settings.customHeight > 0;
  };

  const presetInfo = PRESETS[selectedPreset as keyof typeof PRESETS];

  // Calculate estimates
  const timelineDuration = timeline?.totalDuration || 180; // 3 minutes default
  const estimatedFileSize = useMemo(() => {
    const bitrateKbps = parseInt(presetInfo.bitrate) * 1000;
    const sizeBytes = (bitrateKbps * timelineDuration) / 8;
    const sizeMB = sizeBytes / (1024 * 1024);
    return sizeMB.toFixed(1);
  }, [presetInfo, timelineDuration]);

  const estimatedRenderTime = useMemo(() => {
    // Base render time (seconds)
    let baseTime = timelineDuration * 0.5; // 0.5x realtime for software

    // Apply hardware acceleration speedup
    if (hardwareAccelerationAvailable) {
      baseTime = baseTime / 5; // 5x speedup
    }

    const minutes = Math.floor(baseTime / 60);
    const seconds = Math.floor(baseTime % 60);
    return `${minutes}m ${seconds}s`;
  }, [timelineDuration, hardwareAccelerationAvailable]);

  const buildExportOptions = (): ExportOptions => {
    const [width, height] = presetInfo.resolution.split('x').map(Number);

    // Determine resolution (custom or preset)
    const resolution =
      advancedSettings.customWidth > 0 && advancedSettings.customHeight > 0
        ? { width: advancedSettings.customWidth, height: advancedSettings.customHeight }
        : { width, height };

    // Determine bitrate (custom or preset)
    const videoBitrate =
      advancedSettings.customBitrate > 0
        ? advancedSettings.customBitrate
        : parseInt(presetInfo.bitrate) * 1000;

    // Always use the codec from advanced settings dropdown (defaults to H.264 same as most presets)
    const codec = advancedSettings.codec;

    return {
      preset: selectedPreset,
      resolution,
      fps: 30,
      videoBitrate,
      audioBitrate: 192,
      quality: 'high',
      exportRange,
      outputPath: outputPath || `export_${Date.now()}.mp4`,
      codec,
    };
  };

  // Validation checks
  const validationWarnings = useMemo(() => {
    const warnings: string[] = [];

    // Check for invalid codec/container combinations
    if (advancedSettings.codec === 'ProRes' && !outputPath.toLowerCase().endsWith('.mov')) {
      warnings.push('ProRes codec requires MOV container format');
    }

    if (advancedSettings.codec === 'VP9' && !outputPath.toLowerCase().endsWith('.webm')) {
      warnings.push('VP9 codec works best with WebM container format');
    }

    // Check for extreme bitrate values
    if (advancedSettings.customBitrate > 0) {
      if (advancedSettings.customBitrate < 500) {
        warnings.push('Very low bitrate may result in poor quality');
      }
      if (advancedSettings.customBitrate > 200000) {
        warnings.push('Very high bitrate will create extremely large files');
      }
    }

    // Check resolution constraints
    if (advancedSettings.customWidth > 0 || advancedSettings.customHeight > 0) {
      if (advancedSettings.customWidth > 0 && advancedSettings.customWidth < 320) {
        warnings.push('Width is too small (minimum 320px recommended)');
      }
      if (advancedSettings.customHeight > 0 && advancedSettings.customHeight < 240) {
        warnings.push('Height is too small (minimum 240px recommended)');
      }
      if (advancedSettings.customWidth > 7680 || advancedSettings.customHeight > 4320) {
        warnings.push('Resolution exceeds 8K - may cause performance issues');
      }
    }

    // Check GOP size and keyframe interval
    if (advancedSettings.gopSize > 0 && advancedSettings.gopSize < 10) {
      warnings.push('Very small GOP size may reduce compression efficiency');
    }

    return warnings;
  }, [advancedSettings, outputPath]);

  const handleExport = () => {
    if (validationWarnings.length > 0) {
      const proceed = confirm(
        `Warning:\n${validationWarnings.join('\n')}\n\nDo you want to continue?`
      );
      if (!proceed) return;
    }
    onExport(buildExportOptions());
  };

  const handleAddToQueue = () => {
    if (validationWarnings.length > 0) {
      const proceed = confirm(
        `Warning:\n${validationWarnings.join('\n')}\n\nDo you want to continue?`
      );
      if (!proceed) return;
    }
    onAddToQueue(buildExportOptions());
  };

  // Group presets by platform
  const groupedPresets = useMemo(() => {
    const groups: Record<string, string[]> = {};
    Object.keys(PRESETS).forEach((presetName) => {
      const preset = PRESETS[presetName as keyof typeof PRESETS];
      if (!groups[preset.platform]) {
        groups[preset.platform] = [];
      }
      groups[preset.platform].push(presetName);
    });
    return groups;
  }, []);

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                aria-label="close"
                icon={<Dismiss24Regular />}
                onClick={onClose}
              />
            }
          >
            Export Video
          </DialogTitle>
          <DialogContent>
            <div className={styles.section}>
              <Field label="Export Preset">
                <Dropdown
                  value={selectedPreset}
                  onOptionSelect={(_, data) => setSelectedPreset(data.optionValue as string)}
                  placeholder="Select preset"
                >
                  {Object.entries(groupedPresets).map(([platform, presets]) => (
                    <div key={platform}>
                      <Option
                        text={platform}
                        disabled
                        style={{ fontWeight: 'bold', color: tokens.colorNeutralForeground3 }}
                      >
                        {platform}
                      </Option>
                      {presets.map((preset) => (
                        <Option key={preset} value={preset} text={preset}>
                          {preset}
                        </Option>
                      ))}
                    </div>
                  ))}
                </Dropdown>
              </Field>

              {presetInfo && (
                <div className={styles.presetInfo}>
                  <Body1>{presetInfo.description}</Body1>
                  <div className={styles.spec}>
                    <Caption1>Resolution: {presetInfo.resolution}</Caption1>
                    <Caption1>Codec: {presetInfo.codec}</Caption1>
                    <Caption1>Bitrate: {presetInfo.bitrate}</Caption1>
                    <Caption1>Aspect: {presetInfo.aspectRatio}</Caption1>
                  </div>
                </div>
              )}
            </div>

            <div className={styles.section}>
              <Field label="Timeline Range">
                <Dropdown
                  value={exportRange}
                  onOptionSelect={(_, data) => setExportRange(data.optionValue as 'entire' | 'selection')}
                >
                  <Option value="entire">Entire Timeline</Option>
                  <Option value="selection">Selected Region</Option>
                </Dropdown>
              </Field>

              <Field label="Output Filename">
                <Input
                  value={outputPath}
                  onChange={(_, data) => setOutputPath(data.value)}
                  placeholder={`project_${selectedPreset.toLowerCase().replace(/\s+/g, '_')}_${new Date().toISOString().split('T')[0]}.mp4`}
                />
              </Field>
            </div>

            <Accordion collapsible>
              <AccordionItem value="advanced">
                <AccordionHeader>Advanced Settings</AccordionHeader>
                <AccordionPanel>
                  <div className={styles.section}>
                    <Field label="Quality">
                      <Dropdown defaultSelectedOptions={['high']}>
                        <Option value="draft">Draft (Fast)</Option>
                        <Option value="good">Good</Option>
                        <Option value="high">High (Recommended)</Option>
                        <Option value="maximum">Maximum (Slow)</Option>
                      </Dropdown>
                    </Field>

                    <Field label="Video Codec">
                      <Dropdown
                        value={advancedSettings.codec}
                        onOptionSelect={(_, data) =>
                          setAdvancedSettings({
                            ...advancedSettings,
                            codec: data.optionValue as string,
                            enabled: true,
                          })
                        }
                      >
                        <Option value="H.264">H.264 (Widely Compatible)</Option>
                        <Option value="H.265">H.265 (Better Compression)</Option>
                        <Option value="VP9">VP9 (Web Optimized)</Option>
                        <Option value="ProRes">ProRes (Professional)</Option>
                      </Dropdown>
                    </Field>

                    <div className={styles.row}>
                      <Field label="Bitrate Mode" className={styles.field}>
                        <Dropdown
                          value={advancedSettings.bitrateMode}
                          onOptionSelect={(_, data) =>
                            setAdvancedSettings({
                              ...advancedSettings,
                              bitrateMode: data.optionValue as string,
                            })
                          }
                        >
                          <Option value="VBR">VBR (Variable Bitrate)</Option>
                          <Option value="CBR">CBR (Constant Bitrate)</Option>
                        </Dropdown>
                      </Field>

                      <Field label="Profile" className={styles.field}>
                        <Dropdown
                          value={advancedSettings.profile}
                          onOptionSelect={(_, data) =>
                            setAdvancedSettings({
                              ...advancedSettings,
                              profile: data.optionValue as string,
                            })
                          }
                        >
                          <Option value="baseline">Baseline</Option>
                          <Option value="main">Main</Option>
                          <Option value="high">High</Option>
                        </Dropdown>
                      </Field>
                    </div>

                    <div className={styles.row}>
                      <Field label="Custom Bitrate (Kbps)" className={styles.field}>
                        <Input
                          type="number"
                          value={
                            advancedSettings.customBitrate > 0
                              ? advancedSettings.customBitrate.toString()
                              : ''
                          }
                          onChange={(_, data) => {
                            const value = parseInt(data.value) || 0;
                            const newSettings = { ...advancedSettings, customBitrate: value };
                            setAdvancedSettings({
                              ...newSettings,
                              enabled: shouldEnableAdvanced(newSettings),
                            });
                          }}
                          placeholder="Auto"
                        />
                      </Field>
                    </div>

                    <div className={styles.row}>
                      <Field label="GOP Size (frames)" className={styles.field}>
                        <Input
                          type="number"
                          value={
                            advancedSettings.gopSize > 0 ? advancedSettings.gopSize.toString() : ''
                          }
                          onChange={(_, data) => {
                            const value = parseInt(data.value) || 0;
                            setAdvancedSettings({ ...advancedSettings, gopSize: value });
                          }}
                          placeholder="Auto (2x FPS)"
                        />
                      </Field>

                      <Field label="Keyframe Interval (sec)" className={styles.field}>
                        <Input
                          type="number"
                          value={
                            advancedSettings.keyframeInterval > 0
                              ? advancedSettings.keyframeInterval.toString()
                              : ''
                          }
                          onChange={(_, data) => {
                            const value = parseInt(data.value) || 0;
                            setAdvancedSettings({ ...advancedSettings, keyframeInterval: value });
                          }}
                          placeholder="Auto (2 sec)"
                        />
                      </Field>
                    </div>

                    <div className={styles.row}>
                      <Field label="Custom Width" className={styles.field}>
                        <Input
                          type="number"
                          value={
                            advancedSettings.customWidth > 0
                              ? advancedSettings.customWidth.toString()
                              : ''
                          }
                          onChange={(_, data) => {
                            const value = parseInt(data.value) || 0;
                            const newSettings = { ...advancedSettings, customWidth: value };
                            setAdvancedSettings({
                              ...newSettings,
                              enabled: shouldEnableAdvanced(newSettings),
                            });
                          }}
                          placeholder="Auto"
                        />
                      </Field>
                      <Field label="Custom Height" className={styles.field}>
                        <Input
                          type="number"
                          value={
                            advancedSettings.customHeight > 0
                              ? advancedSettings.customHeight.toString()
                              : ''
                          }
                          onChange={(_, data) => {
                            const value = parseInt(data.value) || 0;
                            const newSettings = { ...advancedSettings, customHeight: value };
                            setAdvancedSettings({
                              ...newSettings,
                              enabled: shouldEnableAdvanced(newSettings),
                            });
                          }}
                          placeholder="Auto"
                        />
                      </Field>
                    </div>
                  </div>
                </AccordionPanel>
              </AccordionItem>
            </Accordion>

            {validationWarnings.length > 0 && (
              <MessageBar intent="warning">
                <MessageBarBody>
                  <div
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      gap: tokens.spacingVerticalXS,
                    }}
                  >
                    {validationWarnings.map((warning, idx) => (
                      <Caption1 key={idx}>• {warning}</Caption1>
                    ))}
                  </div>
                </MessageBarBody>
              </MessageBar>
            )}

            <Divider />

            <div className={styles.section}>
              <div className={styles.estimate}>
                <Body1 style={{ fontWeight: 600 }}>Export Estimates</Body1>
                <Caption1>Estimated file size: ~{estimatedFileSize} MB</Caption1>
                <Caption1>Estimated render time: ~{estimatedRenderTime}</Caption1>
              </div>

              <div className={styles.hardwareStatus}>
                {hardwareAccelerationAvailable ? (
                  <>
                    <CheckmarkCircle24Regular
                      style={{ color: tokens.colorPaletteGreenForeground1 }}
                    />
                    <Badge color="success">GPU acceleration enabled ({hardwareType})</Badge>
                  </>
                ) : (
                  <>
                    <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                    <Badge color="warning">Software encoding (GPU not available)</Badge>
                  </>
                )}
              </div>
            </div>
          </DialogContent>
          <DialogActions>
            <div className={styles.actions}>
              <Button appearance="secondary" onClick={onClose}>
                Cancel
              </Button>
              <Button
                appearance="secondary"
                icon={<ArrowDownload24Regular />}
                onClick={handleAddToQueue}
              >
                Add to Queue
              </Button>
              <Button appearance="primary" icon={<ArrowExport24Regular />} onClick={handleExport}>
                Export Now
              </Button>
            </div>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
