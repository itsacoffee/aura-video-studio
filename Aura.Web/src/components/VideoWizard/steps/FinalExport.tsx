import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Field,
  Radio,
  RadioGroup,
  Checkbox,
  Dropdown,
  Option,
  ProgressBar,
  Spinner,
  Tooltip,
  Badge,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  CheckmarkCircle24Regular,
  Dismiss24Regular,
  DocumentMultiple24Regular,
  Info24Regular,
  Folder24Regular,
  Open24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import type { ExportData, WizardData, StepValidation } from '../types';
import { openFile, openFolder } from '../../../utils/fileSystemUtils';
import { pickFolder, resolvePathOnBackend, getDefaultSaveLocation, validatePathWritable } from '../../../utils/pathUtils';
import { apiUrl } from '../../../config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  settingsCard: {
    padding: tokens.spacingVerticalXL,
  },
  batchExportSection: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  formatCheckboxes: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  estimateCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  estimateRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  exportProgress: {
    padding: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  exportActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  completedSection: {
    padding: tokens.spacingVerticalXXL,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  downloadList: {
    width: '100%',
    maxWidth: '500px',
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  downloadItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  downloadItemHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
  },
  downloadItemInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
  },
  downloadItemActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexShrink: 0,
  },
  filePath: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    wordBreak: 'break-all',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface FinalExportProps {
  data: ExportData;
  wizardData: WizardData;
  advancedMode: boolean;
  onChange: (data: ExportData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

type ExportStatus = 'idle' | 'exporting' | 'completed' | 'error';

interface ExportResult {
  format: string;
  resolution: string;
  filePath: string;
  fileSize: number;
  fullPath?: string;
}

const QUALITY_OPTIONS = [
  { label: 'Draft (480p)', value: 'low', bitrate: 1500 },
  { label: 'Standard (720p)', value: 'medium', bitrate: 2500 },
  { label: 'High (1080p)', value: 'high', bitrate: 5000 },
  { label: 'Ultra (4K)', value: 'ultra', bitrate: 15000 },
];

const RESOLUTION_OPTIONS = [
  { label: '480p (854x480)', value: '480p', width: 854, height: 480 },
  { label: '720p (1280x720)', value: '720p', width: 1280, height: 720 },
  { label: '1080p (1920x1080)', value: '1080p', width: 1920, height: 1080 },
  { label: '4K (3840x2160)', value: '4k', width: 3840, height: 2160 },
];

const FORMAT_OPTIONS = [
  { label: 'MP4 (H.264)', value: 'mp4', extension: '.mp4', description: 'Best compatibility' },
  { label: 'WebM (VP9)', value: 'webm', extension: '.webm', description: 'Web optimized' },
  { label: 'MOV (ProRes)', value: 'mov', extension: '.mov', description: 'Professional editing' },
];

export const FinalExport: FC<FinalExportProps> = ({
  data,
  wizardData,
  advancedMode,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const [exportStatus, setExportStatus] = useState<ExportStatus>('idle');
  const [exportProgress, setExportProgress] = useState(0);
  const [exportStage, setExportStage] = useState('');
  const [batchExport, setBatchExport] = useState(false);
  const [selectedFormats, setSelectedFormats] = useState<string[]>([data.format]);
  const [exportResults, setExportResults] = useState<ExportResult[]>([]);
  const [resolvedPaths, setResolvedPaths] = useState<Record<string, string>>({});
  const [saveLocation, setSaveLocation] = useState<string>('');
  const [isLoadingSaveLocation, setIsLoadingSaveLocation] = useState(true);

  // Load default save location from backend on mount
  useEffect(() => {
    const loadDefaultSaveLocation = async () => {
      setIsLoadingSaveLocation(true);
      try {
        // Try to get from portable settings first (for portable mode)
        try {
          const portableResponse = await fetch(apiUrl('/api/settings/portable'));
          if (portableResponse.ok) {
            const portableData = await portableResponse.json();
            if (portableData.downloadsDirectory) {
              const resolved = await resolvePathOnBackend(portableData.downloadsDirectory);
              setSaveLocation(resolved);
              setIsLoadingSaveLocation(false);
              return;
            }
          }
        } catch (e) {
          // Fall through to next method
        }

        // Try to get from provider paths
        try {
          const pathsResponse = await fetch(apiUrl('/api/providers/paths/load'));
          if (pathsResponse.ok) {
            const pathsData = await pathsResponse.json();
            if (pathsData.outputDirectory && pathsData.outputDirectory.trim()) {
              const resolved = await resolvePathOnBackend(pathsData.outputDirectory);
              setSaveLocation(resolved);
              setIsLoadingSaveLocation(false);
              return;
            }
          }
        } catch (e) {
          // Fall through to default
        }

        // Fall back to frontend default and resolve it
        const defaultPath = getDefaultSaveLocation();
        const resolved = await resolvePathOnBackend(defaultPath);
        setSaveLocation(resolved);
      } catch (error) {
        console.error('Failed to load default save location:', error);
        // Use frontend default as last resort
        setSaveLocation(getDefaultSaveLocation());
      } finally {
        setIsLoadingSaveLocation(false);
      }
    };

    void loadDefaultSaveLocation();
  }, []);

  useEffect(() => {
    onValidationChange({ isValid: true, errors: [] });
  }, [onValidationChange]);

  const estimatedFileSize = useMemo(() => {
    const duration = wizardData.brief.duration;
    const quality = QUALITY_OPTIONS.find((q) => q.value === data.quality);
    const bitrate = quality?.bitrate || 2500;

    const sizeKB = (bitrate * duration) / 8;
    return sizeKB / 1024;
  }, [data.quality, wizardData.brief.duration]);

  const estimatedDiskSpace = useMemo(() => {
    const formats = batchExport ? selectedFormats : [data.format];
    const baseSize = estimatedFileSize;

    return formats.reduce((total, format) => {
      const multiplier = format === 'mov' ? 3 : format === 'webm' ? 0.7 : 1;
      return total + baseSize * multiplier;
    }, 0);
  }, [batchExport, selectedFormats, data.format, estimatedFileSize]);

  const handleQualityChange = useCallback(
    (quality: string) => {
      const resolution =
        quality === 'low'
          ? '480p'
          : quality === 'medium'
            ? '720p'
            : quality === 'high'
              ? '1080p'
              : '4k';
      onChange({ ...data, quality: quality as ExportData['quality'], resolution });
    },
    [data, onChange]
  );

  const handleFormatToggle = useCallback((format: string, checked: boolean) => {
    if (checked) {
      setSelectedFormats((prev) => [...prev, format]);
    } else {
      setSelectedFormats((prev) => prev.filter((f) => f !== format));
    }
  }, []);

  const startExport = useCallback(async () => {
    setExportStatus('exporting');
    setExportProgress(0);
    setExportResults([]);

    try {
      const formatsToExport = batchExport ? selectedFormats : [data.format];

      for (let i = 0; i < formatsToExport.length; i++) {
        const format = formatsToExport[i];
        setExportStage(
          `Exporting ${format.toUpperCase()} format (${i + 1} of ${formatsToExport.length})...`
        );

        for (let progress = 0; progress <= 100; progress += 5) {
          await new Promise((resolve) => setTimeout(resolve, 100));
          setExportProgress((i * 100 + progress) / formatsToExport.length);
        }

        const fileSize = estimatedFileSize * (format === 'mov' ? 3 : format === 'webm' ? 0.7 : 1);
        
        // Generate actual file path
        const fileName = `video-${Date.now()}.${format}`;
        const basePath = saveLocation || getDefaultSaveLocation();
        const fullPath = `${basePath.replace(/\\/g, '/').replace(/\/$/, '')}/${fileName}`;

        // Resolve the path to expand environment variables
        const resolvedPath = await resolvePathOnBackend(fullPath);
        const resolvedFolder = await resolvePathOnBackend(basePath);

        // Ensure the directory exists
        try {
          const validation = await validatePathWritable(resolvedFolder);
          if (!validation.valid) {
            console.warn('Directory validation failed:', validation.error);
            // Still continue, but log the warning
          }
        } catch (error) {
          console.warn('Failed to validate directory:', error);
        }

        const result: ExportResult = {
          format: format.toUpperCase(),
          resolution: data.resolution,
          filePath: fileName,
          fullPath: resolvedPath,
          fileSize: Math.round(fileSize * 1024),
        };

        exportResults.push(result);
        setResolvedPaths((prev) => ({
          ...prev,
          [result.fullPath]: resolvedFolder,
        }));

        setExportResults([...exportResults]);
      }

      setExportProgress(100);
      setExportStatus('completed');
      setExportStage('Export completed successfully!');
    } catch (error) {
      console.error('Export failed:', error);
      setExportStatus('error');
      setExportStage('Export failed. Please try again.');
    }
  }, [
    batchExport,
    selectedFormats,
    data.format,
    data.resolution,
    estimatedFileSize,
    exportResults,
  ]);

  const cancelExport = useCallback(() => {
    setExportStatus('idle');
    setExportProgress(0);
    setExportStage('');
  }, []);

  const handleBrowseSaveLocation = useCallback(async () => {
    try {
      const selectedPath = await pickFolder();
      if (selectedPath) {
        setSaveLocation(selectedPath);
      }
    } catch (error) {
      console.error('Failed to pick folder:', error);
    }
  }, []);

  const handleOpenFile = useCallback(async (filePath: string, fullPath?: string) => {
    try {
      const pathToOpen = fullPath || filePath;
      if (!pathToOpen) {
        console.warn('No file path provided');
        return;
      }
      
      const success = await openFile(pathToOpen);
      if (!success) {
        console.warn('Failed to open file, trying folder instead');
        // Try to open the folder containing the file
        const folderPath = resolvedPaths[pathToOpen] || pathToOpen.substring(0, Math.max(
          pathToOpen.lastIndexOf('/'),
          pathToOpen.lastIndexOf('\\')
        ));
        if (folderPath) {
        await openFolder(folderPath);
        }
      }
    } catch (error) {
      console.error('Failed to open file:', error);
    }
  }, [resolvedPaths]);

  const handleOpenFolder = useCallback(async (filePath: string, fullPath?: string) => {
    try {
      const pathToOpen = fullPath || filePath;
      if (!pathToOpen) {
        console.warn('No file path provided');
        return;
      }
      
      // Get the folder path - either from resolved paths or extract from file path
      const folderPath = resolvedPaths[pathToOpen] || pathToOpen.substring(0, Math.max(
        pathToOpen.lastIndexOf('/'),
        pathToOpen.lastIndexOf('\\')
      ));
      
      if (folderPath) {
      await openFolder(folderPath);
      } else {
        console.warn('Could not determine folder path');
      }
    } catch (error) {
      console.error('Failed to open folder:', error);
    }
  }, [resolvedPaths]);

  const renderSettingsView = () => (
    <div className={styles.container}>
      <div className={styles.settingsGrid}>
        <Card className={styles.settingsCard}>
          <Field label="Quality Preset">
            <RadioGroup
              value={data.quality}
              onChange={(_, { value }) => handleQualityChange(value)}
            >
              {QUALITY_OPTIONS.map((option) => (
                <Radio
                  key={option.value}
                  value={option.value}
                  label={
                    <div>
                      <Text weight="semibold">{option.label}</Text>
                      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                        ~{option.bitrate} kbps
                      </Text>
                    </div>
                  }
                />
              ))}
            </RadioGroup>
          </Field>
        </Card>

        <Card className={styles.settingsCard}>
          <Field label="Resolution">
            <Dropdown
              value={data.resolution}
              selectedOptions={[data.resolution]}
              onOptionSelect={(_, { optionValue }) => {
                if (optionValue) {
                  onChange({ ...data, resolution: optionValue as ExportData['resolution'] });
                }
              }}
            >
              {RESOLUTION_OPTIONS.map((option) => (
                <Option key={option.value} value={option.value}>
                  {option.label}
                </Option>
              ))}
            </Dropdown>
          </Field>

          <Field label="Output Format" style={{ marginTop: tokens.spacingVerticalL }}>
            <Dropdown
              value={data.format}
              selectedOptions={[data.format]}
              onOptionSelect={(_, { optionValue }) => {
                if (optionValue) {
                  onChange({ ...data, format: optionValue as ExportData['format'] });
                  setSelectedFormats([optionValue]);
                }
              }}
              disabled={batchExport}
            >
              {FORMAT_OPTIONS.map((option) => (
                <Option key={option.value} value={option.value}>
                  {option.label}
                </Option>
              ))}
            </Dropdown>
          </Field>
        </Card>
      </div>

      <Card className={styles.settingsCard}>
        <Checkbox
          label="Include captions/subtitles"
          checked={data.includeCaptions}
          onChange={(_, { checked }) => onChange({ ...data, includeCaptions: !!checked })}
        />
      </Card>

      <Card className={styles.settingsCard}>
        <Field label="Save Location">
          {isLoadingSaveLocation ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
              <Spinner size="small" />
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Loading default save location...
              </Text>
            </div>
          ) : (
            <>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, alignItems: 'center' }}>
                <Text
                  style={{
                    flex: 1,
                    padding: tokens.spacingVerticalS,
                    backgroundColor: tokens.colorNeutralBackground2,
                    borderRadius: tokens.borderRadiusSmall,
                    fontFamily: 'monospace',
                    fontSize: tokens.fontSizeBase200,
                    wordBreak: 'break-all',
                  }}
                >
                  {saveLocation || 'Default location will be used'}
                </Text>
                <Button
                  appearance="secondary"
                  icon={<Folder24Regular />}
                  onClick={handleBrowseSaveLocation}
                >
                  Browse
                </Button>
              </div>
              <Text size={200} style={{ marginTop: tokens.spacingVerticalXS, color: tokens.colorNeutralForeground3 }}>
                Choose where to save your exported video file
              </Text>
            </>
          )}
        </Field>
      </Card>

      {advancedMode && (
        <Card style={{ padding: tokens.spacingVerticalL, marginTop: tokens.spacingVerticalL }}>
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Advanced Export Options</Title3>
          <div className={styles.batchExportSection}>
            <Title3 style={{ marginBottom: tokens.spacingVerticalM, fontSize: tokens.fontSizeBase400 }}>
              Batch Export
            </Title3>
          <Checkbox
            label="Export to multiple formats"
            checked={batchExport}
            onChange={(_, { checked }) => setBatchExport(!!checked)}
          />

          {batchExport && (
            <div className={styles.formatCheckboxes}>
              {FORMAT_OPTIONS.map((option) => (
                <div key={option.value}>
                  <Checkbox
                    label={option.label}
                    checked={selectedFormats.includes(option.value)}
                    onChange={(_, { checked }) => handleFormatToggle(option.value, !!checked)}
                  />
                  <Text size={200} style={{ marginLeft: tokens.spacingHorizontalXL }}>
                    {option.description}
                  </Text>
                </div>
              ))}
            </div>
          )}
          </div>
        </Card>
      )}

      <div className={styles.estimateCard}>
        <div className={styles.estimateRow}>
          <Text weight="semibold">Estimated File Size:</Text>
          <Text weight="bold">{estimatedFileSize.toFixed(1)} MB</Text>
        </div>

        {batchExport && (
          <div className={styles.estimateRow}>
            <Text weight="semibold">Total Disk Space Required:</Text>
            <Text weight="bold">{estimatedDiskSpace.toFixed(1)} MB</Text>
          </div>
        )}

        <div className={styles.estimateRow}>
          <Text weight="semibold">Estimated Export Time:</Text>
          <Text weight="bold">~{Math.ceil((wizardData.brief.duration / 60) * 2)} minutes</Text>
        </div>

        <Tooltip
          content="Estimates are based on your selected quality and duration settings"
          relationship="label"
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}>
            <Info24Regular style={{ fontSize: '16px' }} />
            <Text size={200}>These are estimates and may vary</Text>
          </div>
        </Tooltip>
      </div>

      <div className={styles.exportActions}>
        <Button appearance="primary" size="large" onClick={startExport}>
          Start Export
        </Button>
      </div>
    </div>
  );

  const renderExportingView = () => (
    <div className={styles.exportProgress}>
      <Spinner size="extra-large" />
      <Title3>Exporting Video...</Title3>
      <Text>{exportStage}</Text>
      <div style={{ width: '100%', maxWidth: '500px' }}>
        <ProgressBar value={exportProgress / 100} />
        <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
          {Math.round(exportProgress)}% complete
        </Text>
      </div>
      <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={cancelExport}>
        Cancel Export
      </Button>
    </div>
  );

  const renderCompletedView = () => (
    <div className={styles.completedSection}>
      <CheckmarkCircle24Regular
        style={{ fontSize: '64px', color: tokens.colorPaletteGreenForeground1 }}
      />
      <Title2>Export Completed!</Title2>
      <Text style={{ marginBottom: tokens.spacingVerticalL }}>
        Your video has been successfully exported and saved to your computer.
      </Text>

      <div className={styles.downloadList}>
        {exportResults.map((result, index) => (
          <Card key={index} className={styles.downloadItem}>
            <div className={styles.downloadItemHeader}>
              <div className={styles.downloadItemInfo}>
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
                  <Text weight="semibold" size={400}>
                {result.format} - {result.resolution}
              </Text>
                  <Badge appearance="filled" color="success">
                {(result.fileSize / 1024).toFixed(1)} MB
                  </Badge>
                </div>
                {result.fullPath && (
                  <div className={styles.filePath}>
                    <Text size={200} weight="semibold" style={{ marginBottom: tokens.spacingVerticalXXS }}>
                      File Location:
              </Text>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground2 }}>
                      {result.fullPath}
                </Text>
                  </div>
              )}
            </div>
              <div className={styles.downloadItemActions}>
              <Button
                appearance="primary"
                icon={<Open24Regular />}
                onClick={() => handleOpenFile(result.filePath, result.fullPath)}
              >
                Open File
              </Button>
              <Button
                appearance="secondary"
                icon={<Folder24Regular />}
                onClick={() => handleOpenFolder(result.filePath, result.fullPath)}
              >
                Open Folder
              </Button>
            </div>
          </div>
          </Card>
        ))}
      </div>

      <div className={styles.exportActions}>
        <Button
          appearance="secondary"
          icon={<DocumentMultiple24Regular />}
          onClick={() => {
            setExportStatus('idle');
            setExportResults([]);
            setResolvedPaths({});
          }}
        >
          Export Another Version
        </Button>
      </div>
    </div>
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Final Export</Title2>
        <Text>Configure your video export settings and download the final result.</Text>
      </div>

      {exportStatus === 'idle' && renderSettingsView()}
      {exportStatus === 'exporting' && renderExportingView()}
      {exportStatus === 'completed' && renderCompletedView()}
    </div>
  );
};
