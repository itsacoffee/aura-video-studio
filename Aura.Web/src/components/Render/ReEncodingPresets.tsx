import {
  makeStyles,
  tokens,
  Card,
  Body1,
  Body1Strong,
  Caption1,
  Divider,
  Badge,
} from '@fluentui/react-components';
import { ArrowSyncRegular, CheckmarkCircleRegular } from '@fluentui/react-icons';
import type { FileMetadata } from './FileContext';

const useStyles = makeStyles({
  container: {
    marginBottom: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  presetGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  presetCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    border: `2px solid ${tokens.colorNeutralStroke2}`,
  },
  presetCardSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  presetHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  presetDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  comparison: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalL,
  },
  comparisonColumn: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  comparisonItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  icon: {
    color: tokens.colorPaletteGreenForeground1,
  },
});

export interface ReEncodingPreset {
  id: string;
  name: string;
  description: string;
  targetCodec: string;
  targetBitrate: number;
  targetResolution?: { width: number; height: number };
  estimatedSizeReduction?: number;
}

const PRESETS: ReEncodingPreset[] = [
  {
    id: 'compress-web',
    name: 'Compress for Web',
    description: 'Optimize for web streaming with balanced quality and file size',
    targetCodec: 'H.264',
    targetBitrate: 4000000,
    estimatedSizeReduction: 40,
  },
  {
    id: 'high-quality',
    name: 'High Quality',
    description: 'Maximum quality with efficient compression using HEVC',
    targetCodec: 'HEVC',
    targetBitrate: 8000000,
    estimatedSizeReduction: 30,
  },
  {
    id: 'mobile-optimized',
    name: 'Mobile Optimized',
    description: 'Smaller file size for mobile devices and social media',
    targetCodec: 'H.264',
    targetBitrate: 2500000,
    targetResolution: { width: 1280, height: 720 },
    estimatedSizeReduction: 60,
  },
  {
    id: 'archive',
    name: 'Archive Quality',
    description: 'Preserve quality for long-term storage with lossless compression',
    targetCodec: 'HEVC',
    targetBitrate: 12000000,
    estimatedSizeReduction: 20,
  },
];

interface ReEncodingPresetsProps {
  sourceFile: FileMetadata;
  selectedPreset: string | null;
  onPresetSelect: (preset: ReEncodingPreset) => void;
}

export function ReEncodingPresets({
  sourceFile,
  selectedPreset,
  onPresetSelect,
}: ReEncodingPresetsProps) {
  const styles = useStyles();

  const formatBitrate = (bps: number): string => {
    const mbps = bps / 1000000;
    return `${mbps.toFixed(1)} Mbps`;
  };

  const formatFileSize = (bytes: number): string => {
    const mb = bytes / (1024 * 1024);
    if (mb >= 1024) {
      return `${(mb / 1024).toFixed(2)} GB`;
    }
    return `${mb.toFixed(0)} MB`;
  };

  const estimateOutputSize = (preset: ReEncodingPreset): number => {
    if (preset.estimatedSizeReduction) {
      return sourceFile.size * (1 - preset.estimatedSizeReduction / 100);
    }
    return sourceFile.size;
  };

  const selectedPresetData = PRESETS.find((p) => p.id === selectedPreset);

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.header}>
          <Body1Strong>Re-encoding Presets</Body1Strong>
          <Caption1>Choose a preset optimized for your target use case</Caption1>
        </div>

        <div className={styles.presetGrid}>
          {PRESETS.map((preset) => (
            <Card
              key={preset.id}
              className={`${styles.presetCard} ${selectedPreset === preset.id ? styles.presetCardSelected : ''}`}
              onClick={() => onPresetSelect(preset)}
            >
              <div className={styles.presetHeader}>
                <Body1Strong>{preset.name}</Body1Strong>
                {selectedPreset === preset.id && <CheckmarkCircleRegular className={styles.icon} />}
              </div>

              <div className={styles.presetDetails}>
                <Caption1>{preset.description}</Caption1>

                <Divider style={{ marginTop: tokens.spacingVerticalXS }} />

                <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, flexWrap: 'wrap' }}>
                  <Badge size="small">{preset.targetCodec}</Badge>
                  <Badge size="small">{formatBitrate(preset.targetBitrate)}</Badge>
                  {preset.targetResolution && (
                    <Badge size="small">
                      {preset.targetResolution.width}×{preset.targetResolution.height}
                    </Badge>
                  )}
                </div>

                {preset.estimatedSizeReduction && (
                  <Caption1 style={{ color: tokens.colorPaletteGreenForeground1 }}>
                    ~{preset.estimatedSizeReduction}% size reduction
                  </Caption1>
                )}
              </div>
            </Card>
          ))}
        </div>

        {selectedPresetData && (
          <>
            <Divider style={{ marginTop: tokens.spacingVerticalL }} />

            <div className={styles.comparison}>
              <div className={styles.comparisonColumn}>
                <Body1Strong>Current File</Body1Strong>
                <Divider />
                {sourceFile.codec && (
                  <div className={styles.comparisonItem}>
                    <Caption1>Codec:</Caption1>
                    <Body1>{sourceFile.codec}</Body1>
                  </div>
                )}
                {sourceFile.bitrate && (
                  <div className={styles.comparisonItem}>
                    <Caption1>Bitrate:</Caption1>
                    <Body1>{formatBitrate(sourceFile.bitrate)}</Body1>
                  </div>
                )}
                {sourceFile.resolution && (
                  <div className={styles.comparisonItem}>
                    <Caption1>Resolution:</Caption1>
                    <Body1>
                      {sourceFile.resolution.width}×{sourceFile.resolution.height}
                    </Body1>
                  </div>
                )}
                <div className={styles.comparisonItem}>
                  <Caption1>File Size:</Caption1>
                  <Body1>{formatFileSize(sourceFile.size)}</Body1>
                </div>
              </div>

              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <ArrowSyncRegular
                  style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }}
                />
              </div>

              <div className={styles.comparisonColumn}>
                <Body1Strong>Target Output</Body1Strong>
                <Divider />
                <div className={styles.comparisonItem}>
                  <Caption1>Codec:</Caption1>
                  <Body1>{selectedPresetData.targetCodec}</Body1>
                </div>
                <div className={styles.comparisonItem}>
                  <Caption1>Bitrate:</Caption1>
                  <Body1>{formatBitrate(selectedPresetData.targetBitrate)}</Body1>
                </div>
                {selectedPresetData.targetResolution && (
                  <div className={styles.comparisonItem}>
                    <Caption1>Resolution:</Caption1>
                    <Body1>
                      {selectedPresetData.targetResolution.width}×
                      {selectedPresetData.targetResolution.height}
                    </Body1>
                  </div>
                )}
                <div className={styles.comparisonItem}>
                  <Caption1>Est. File Size:</Caption1>
                  <Body1>{formatFileSize(estimateOutputSize(selectedPresetData))}</Body1>
                </div>
              </div>
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
