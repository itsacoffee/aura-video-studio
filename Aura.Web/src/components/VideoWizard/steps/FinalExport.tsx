import {
  Badge,
  Button,
  Card,
  Checkbox,
  Dropdown,
  Field,
  makeStyles,
  Option,
  ProgressBar,
  Radio,
  RadioGroup,
  Spinner,
  Text,
  Title2,
  Title3,
  tokens,
  Tooltip,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Dismiss24Regular,
  DocumentMultiple24Regular,
  ErrorCircle24Regular,
  Folder24Regular,
  Info24Regular,
  Open24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { apiUrl } from '../../../config/api';
import { startFinalRendering } from '../../../services/wizardService';
import { openFile, openFolder } from '../../../utils/fileSystemUtils';
import { getDefaultSaveLocation, pickFolder, resolvePathOnBackend } from '../../../utils/pathUtils';
import type { ExportData, StepValidation, WizardData } from '../types';

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
        } catch {
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
        } catch {
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

  // eslint-disable-next-line sonarjs/cognitive-complexity
  const startExport = useCallback(async () => {
    setExportStatus('exporting');
    setExportProgress(0);
    setExportResults([]);
    setResolvedPaths({});

    try {
      const formatsToExport = batchExport ? selectedFormats : [data.format];
      const newExportResults: ExportResult[] = [];

      for (let i = 0; i < formatsToExport.length; i++) {
        const format = formatsToExport[i];
        console.info('[FinalExport] Starting export for format:', format);
        setExportStage(`Preparing export for ${format.toUpperCase()} format...`);

        // Map quality to video bitrate
        const qualityBitrateMap: Record<string, number> = {
          low: 1500,
          medium: 2500,
          high: 5000,
          ultra: 15000,
        };

        // Map resolution string to backend format
        const resolutionMap: Record<string, string> = {
          '480p': '480p',
          '720p': '720p',
          '1080p': '1080p',
          '4k': '4K',
        };

        // Map format to container
        const formatContainerMap: Record<string, string> = {
          mp4: 'mp4',
          webm: 'webm',
          mov: 'mov',
        };

        // Map quality to codec
        const qualityCodecMap: Record<string, string> = {
          low: 'h264',
          medium: 'h264',
          high: 'h264',
          ultra: 'h265',
        };

        try {
          // ✅ REAL API CALL - Start video generation
          const { jobId } = await startFinalRendering(
            {
              topic: wizardData.brief.topic,
              audience: wizardData.brief.targetAudience || 'General',
              goal: wizardData.brief.keyMessage || 'Inform',
              tone: wizardData.style.tone || 'Professional',
              language: 'English', // Default, could be extracted from wizardData if available
              duration: wizardData.brief.duration,
              videoType: wizardData.brief.videoType || 'educational',
            },
            {
              voiceProvider: wizardData.style.voiceProvider || 'Windows',
              voiceName: wizardData.style.voiceName || 'default',
              visualStyle: wizardData.style.visualStyle || 'modern',
              musicGenre: wizardData.style.musicGenre || 'none',
              musicEnabled: wizardData.style.musicEnabled || false,
            },
            {
              generatedScript: wizardData.script.content || '',
              scenes: wizardData.script.scenes || [],
              totalDuration: wizardData.brief.duration,
            },
            {
              resolution: resolutionMap[data.resolution] || '1080p',
              fps: 30, // Default FPS
              codec: qualityCodecMap[data.quality] || 'h264',
              quality: qualityBitrateMap[data.quality] || 5000,
              includeSubs: data.includeCaptions,
              outputFormat: formatContainerMap[format] || 'mp4',
            }
          );

          // Provide initial feedback before polling starts
          console.info('[FinalExport] Job submitted, waiting for backend to start processing...');
          console.info('[FinalExport] Job ID received:', jobId);
          setExportStage('Job submitted, waiting for backend to start processing...');
          setExportProgress(1); // Show some progress so user knows something is happening

          // Poll job status with improved error handling
          let jobCompleted = false;
          let jobData: unknown = null;
          const maxPollAttempts = 600; // 10 minutes max (600 * 1 second)
          let pollAttempts = 0;
          let consecutiveErrors = 0;
          const maxConsecutiveErrors = 5;

          while (!jobCompleted && pollAttempts < maxPollAttempts) {
            await new Promise((resolve) => setTimeout(resolve, 1000)); // Poll every 1 second
            pollAttempts++;

            try {
              const statusResponse = await fetch(apiUrl(`/api/jobs/${jobId}`), {
                headers: {
                  Accept: 'application/json',
                },
              });

              if (!statusResponse.ok) {
                if (statusResponse.status === 404) {
                  consecutiveErrors++;

                  // Provide user feedback at different stages
                  if (pollAttempts === 5) {
                    setExportStage('Starting video generation...');
                  } else if (pollAttempts === 10) {
                    setExportStage('Initializing rendering pipeline...');
                  } else if (pollAttempts > 15 && consecutiveErrors > 10) {
                    throw new Error(
                      'Video generation job not found. The backend may not have started the job correctly. ' +
                        'Please check backend logs and ensure the API is running.'
                    );
                  }

                  // Log after a few attempts for debugging
                  if (pollAttempts > 5) {
                    console.warn(
                      `[FinalExport] Job ${jobId} not found after ${pollAttempts} attempts`
                    );
                  }
                  continue;
                }

                const errorText = await statusResponse.text().catch(() => 'Unknown error');
                throw new Error(
                  `Failed to check job status: ${statusResponse.status} - ${errorText}`
                );
              }

              consecutiveErrors = 0; // Reset error counter on success
              jobData = await statusResponse.json();

              // Type guard for job data
              const typedJobData = jobData as {
                percent?: number;
                stage?: string;
                progressMessage?: string;
                status?: string;
                errorMessage?: string;
                error?: string;
                failureDetails?: {
                  message?: string;
                  suggestedActions?: string[];
                };
                outputPath?: string;
                artifacts?: Array<{
                  path?: string;
                  filePath?: string;
                  type?: string;
                }>;
              };

              // Update progress based on job percent
              const jobProgress = typedJobData.percent || 0;
              const overallProgress = (i * 100 + jobProgress) / formatsToExport.length;
              setExportProgress(overallProgress);

              // Show detailed stage information
              const stageMessage = typedJobData.stage || 'Processing...';
              const detailMessage = typedJobData.progressMessage || '';
              setExportStage(
                detailMessage
                  ? `${stageMessage}: ${detailMessage}`
                  : `${stageMessage} (${Math.round(jobProgress)}%)`
              );

              console.info(
                '[FinalExport] Polling attempt:',
                pollAttempts,
                'Status:',
                typedJobData.status
              );

              // Check completion status
              const jobStatus = (typedJobData.status?.toLowerCase() || '').trim();
              if (jobStatus === 'done' || jobStatus === 'succeeded' || jobStatus === 'completed') {
                jobCompleted = true;
                console.info('[FinalExport] Job completed:', typedJobData);
              } else if (jobStatus === 'failed') {
                const errorMsg =
                  typedJobData.errorMessage ||
                  typedJobData.error ||
                  typedJobData.failureDetails?.message ||
                  'Video generation failed';
                const errorDetails = typedJobData.failureDetails?.suggestedActions
                  ? `\n\nSuggested actions:\n${typedJobData.failureDetails.suggestedActions.join('\n')}`
                  : '';
                throw new Error(`${errorMsg}${errorDetails}`);
              } else if (jobStatus === 'canceled' || jobStatus === 'cancelled') {
                throw new Error('Video generation was cancelled');
              }
              // Continue polling for 'queued' or 'running' status
            } catch (error) {
              // Re-throw "not found" errors after threshold
              if (error instanceof Error && error.message.includes('not found')) {
                throw error;
              }

              // If it's a network error, continue polling (with retry limit)
              if (
                error instanceof TypeError &&
                (error.message.includes('fetch') || error.message.includes('network'))
              ) {
                consecutiveErrors++;
                console.warn(
                  `[FinalExport] Network error while polling job status (attempt ${consecutiveErrors}/${maxConsecutiveErrors}), retrying...`
                );
                if (consecutiveErrors >= maxConsecutiveErrors) {
                  throw new Error(
                    'Network connection lost. Please check your internet connection and try again.'
                  );
                }
                continue;
              }

              consecutiveErrors++;
              console.warn(`[FinalExport] Poll attempt ${pollAttempts} failed:`, error);

              if (consecutiveErrors >= maxConsecutiveErrors) {
                throw new Error(
                  `Failed to check job status after ${maxConsecutiveErrors} attempts. ` +
                    `Last error: ${error instanceof Error ? error.message : String(error)}`
                );
              }
            }
          }

          // Timeout handling
          if (!jobCompleted) {
            throw new Error(
              `Video generation timed out after ${Math.round(pollAttempts / 60)} minutes. ` +
                `The rendering process may still be running on the server.`
            );
          }

          // Get typed job data for output extraction
          const typedJobData = jobData as {
            outputPath?: string;
            artifacts?: Array<{
              path?: string;
              filePath?: string;
              type?: string;
            }>;
          };

          // Get file path from job artifacts or output path
          let outputPath = typedJobData.outputPath;

          // Try to extract from artifacts if outputPath is not directly available
          if (
            !outputPath &&
            typedJobData.artifacts &&
            Array.isArray(typedJobData.artifacts) &&
            typedJobData.artifacts.length > 0
          ) {
            // Find video artifact - check multiple possible formats
            const videoArtifact = typedJobData.artifacts.find((a) => {
              const path = a.path || a.filePath || '';
              const type = (a.type || '').toLowerCase();
              return (
                type.includes('video') ||
                path.endsWith('.mp4') ||
                path.endsWith('.webm') ||
                path.endsWith('.mov') ||
                path.endsWith('.mkv') ||
                path.endsWith('.avi')
              );
            });
            if (videoArtifact) {
              outputPath = videoArtifact.path || videoArtifact.filePath;
              console.info('[FinalExport] Found video artifact:', videoArtifact);
            }
          }

          // If still no output path, try to construct from job directory
          if (!outputPath) {
            console.warn(
              '[FinalExport] No output path in job data, attempting to construct from job ID'
            );
            // The backend should set outputPath, but if it doesn't, we can't proceed
            throw new Error(
              'No output path returned from video generation. ' +
                'The video file may not have been created. ' +
                'Please check the job logs for errors.'
            );
          }

          console.info('[FinalExport] Video generation completed, output path:', outputPath);

          // Resolve path for display
          const resolvedPath = await resolvePathOnBackend(outputPath);
          const lastSeparatorIndex = Math.max(
            resolvedPath.lastIndexOf('/'),
            resolvedPath.lastIndexOf('\\')
          );
          const resolvedFolder =
            lastSeparatorIndex >= 0 ? resolvedPath.substring(0, lastSeparatorIndex) : resolvedPath;
          const fileName =
            lastSeparatorIndex >= 0 ? resolvedPath.substring(lastSeparatorIndex + 1) : outputPath;

          // ✅ REAL FILE SIZE - Get from file system
          let actualFileSize = 0;
          try {
            const fileStatResponse = await fetch(
              apiUrl(`/api/files/stat?path=${encodeURIComponent(resolvedPath)}`)
            );
            if (fileStatResponse.ok) {
              const statData = await fileStatResponse.json();
              actualFileSize = statData.size || 0;
            }
          } catch (error) {
            console.warn('[FinalExport] Could not get file size:', error);
            // Fallback to estimate
            actualFileSize = Math.round(estimatedFileSize * 1024 * 1024);
          }

          // If we still don't have a file size, use estimate
          if (actualFileSize === 0) {
            const formatMultiplier = format === 'mov' ? 3 : format === 'webm' ? 0.7 : 1;
            actualFileSize = Math.round(estimatedFileSize * 1024 * 1024 * formatMultiplier);
          }

          const result: ExportResult = {
            format: format.toUpperCase(),
            resolution: data.resolution,
            filePath: fileName,
            fullPath: resolvedPath,
            fileSize: actualFileSize,
          };

          newExportResults.push(result);
          setResolvedPaths((prev) => ({
            ...prev,
            [resolvedPath]: resolvedFolder,
          }));
        } catch (error) {
          console.error(`[FinalExport] Export failed for format ${format}:`, error);
          throw error; // Re-throw to be caught by outer try-catch
        }
      }

      setExportResults(newExportResults);
      setExportProgress(100);
      setExportStatus('completed');
      setExportStage('Export completed successfully!');
    } catch (error) {
      console.error('[FinalExport] Export failed:', error);
      setExportStatus('error');
      setExportStage(error instanceof Error ? error.message : 'Export failed. Please try again.');
    }
  }, [batchExport, selectedFormats, data, wizardData, estimatedFileSize]);

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

  const handleOpenFile = useCallback(
    async (filePath: string, fullPath?: string) => {
      try {
        let pathToOpen = fullPath || filePath;
        if (!pathToOpen) {
          console.warn('No file path provided');
          return;
        }

        // If path contains environment variables, resolve it
        if (pathToOpen.includes('%') || pathToOpen.includes('~')) {
          pathToOpen = await resolvePathOnBackend(pathToOpen);
        }

        const success = await openFile(pathToOpen);
        if (!success) {
          console.warn('Failed to open file, trying folder instead');
          // Try to open the folder containing the file
          const folderPath =
            resolvedPaths[pathToOpen] ||
            pathToOpen.substring(
              0,
              Math.max(pathToOpen.lastIndexOf('/'), pathToOpen.lastIndexOf('\\'))
            );
          if (folderPath) {
            await openFolder(folderPath);
          }
        }
      } catch (error) {
        console.error('Failed to open file:', error);
      }
    },
    [resolvedPaths]
  );

  const handleOpenFolder = useCallback(
    async (filePath: string, fullPath?: string) => {
      try {
        let pathToOpen = fullPath || filePath;
        if (!pathToOpen) {
          console.warn('No file path provided');
          return;
        }

        // If path contains environment variables, resolve it
        if (pathToOpen.includes('%') || pathToOpen.includes('~')) {
          pathToOpen = await resolvePathOnBackend(pathToOpen);
        }

        // Get the folder path - either from resolved paths or extract from file path
        let folderPath = resolvedPaths[pathToOpen];
        if (!folderPath) {
          // Extract directory from file path
          const lastSlash = Math.max(pathToOpen.lastIndexOf('/'), pathToOpen.lastIndexOf('\\'));
          if (lastSlash >= 0) {
            folderPath = pathToOpen.substring(0, lastSlash);
          } else {
            folderPath = pathToOpen; // Assume it's already a folder
          }
        }

        if (folderPath) {
          // Resolve folder path if it contains environment variables
          if (folderPath.includes('%') || folderPath.includes('~')) {
            folderPath = await resolvePathOnBackend(folderPath);
          }
          await openFolder(folderPath);
        } else {
          console.warn('Could not determine folder path');
        }
      } catch (error) {
        console.error('Failed to open folder:', error);
      }
    },
    [resolvedPaths]
  );

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
              <div
                style={{ display: 'flex', gap: tokens.spacingHorizontalS, alignItems: 'center' }}
              >
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
              <Text
                size={200}
                style={{
                  marginTop: tokens.spacingVerticalXS,
                  color: tokens.colorNeutralForeground3,
                }}
              >
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
            <Title3
              style={{ marginBottom: tokens.spacingVerticalM, fontSize: tokens.fontSizeBase400 }}
            >
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
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
                >
                  <Text weight="semibold" size={400}>
                    {result.format} - {result.resolution}
                  </Text>
                  <Badge appearance="filled" color="success">
                    {(result.fileSize / 1024).toFixed(1)} MB
                  </Badge>
                </div>
                {result.fullPath && (
                  <div className={styles.filePath}>
                    <Text
                      size={200}
                      weight="semibold"
                      style={{ marginBottom: tokens.spacingVerticalXXS }}
                    >
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

  const renderErrorView = () => (
    <div className={styles.exportProgress}>
      <ErrorCircle24Regular
        style={{ fontSize: '64px', color: tokens.colorPaletteRedForeground1 }}
      />
      <Title3>Export Failed</Title3>
      <div
        style={{
          padding: tokens.spacingVerticalM,
          backgroundColor: tokens.colorNeutralBackground3,
          borderRadius: tokens.borderRadiusMedium,
          marginBottom: tokens.spacingVerticalL,
          maxWidth: '600px',
        }}
      >
        <Text style={{ color: tokens.colorPaletteRedForeground1, fontWeight: 600 }}>
          Error Details:
        </Text>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
          {exportStage || 'An error occurred while exporting your video.'}
        </Text>
        <Text
          size={200}
          style={{
            display: 'block',
            marginTop: tokens.spacingVerticalS,
            color: tokens.colorNeutralForeground3,
          }}
        >
          Check the browser console (F12) for detailed logs.
        </Text>
      </div>
      <div className={styles.exportActions}>
        <Button
          appearance="primary"
          onClick={() => {
            setExportStatus('idle');
            setExportProgress(0);
            setExportStage('');
            setExportResults([]);
            setResolvedPaths({});
          }}
        >
          Try Again
        </Button>
        <Button
          appearance="secondary"
          onClick={() => {
            setExportStatus('idle');
            setExportProgress(0);
            setExportStage('');
          }}
        >
          Back to Settings
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
      {exportStatus === 'error' && renderErrorView()}
    </div>
  );
};
