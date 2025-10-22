import { useState, useMemo } from 'react';
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
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  ArrowExport24Regular,
  ArrowDownload24Regular,
  Warning24Regular,
  CheckmarkCircle24Regular,
} from '@fluentui/react-icons';

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
  timeline?: any;
  hardwareAccelerationAvailable?: boolean;
  hardwareType?: string;
}

export interface ExportOptions {
  preset: string;
  resolution: { width: number; height: number };
  fps: number;
  videoBitrate: number;
  audioBitrate: number;
  quality: 'draft' | 'good' | 'high' | 'maximum';
  exportRange: 'entire' | 'selection';
  selectionStart?: number;
  selectionEnd?: number;
  outputPath: string;
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

  const handleExport = () => {
    const [width, height] = presetInfo.resolution.split('x').map(Number);
    const options: ExportOptions = {
      preset: selectedPreset,
      resolution: { width, height },
      fps: 30,
      videoBitrate: parseInt(presetInfo.bitrate) * 1000,
      audioBitrate: 192,
      quality: 'high',
      exportRange,
      outputPath: outputPath || `export_${Date.now()}.mp4`,
    };
    onExport(options);
  };

  const handleAddToQueue = () => {
    const [width, height] = presetInfo.resolution.split('x').map(Number);
    const options: ExportOptions = {
      preset: selectedPreset,
      resolution: { width, height },
      fps: 30,
      videoBitrate: parseInt(presetInfo.bitrate) * 1000,
      audioBitrate: 192,
      quality: 'high',
      exportRange,
      outputPath: outputPath || `export_${Date.now()}.mp4`,
    };
    onAddToQueue(options);
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
                  onOptionSelect={(_, data) => setExportRange(data.optionValue as any)}
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
                  </div>
                </AccordionPanel>
              </AccordionItem>
            </Accordion>

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
