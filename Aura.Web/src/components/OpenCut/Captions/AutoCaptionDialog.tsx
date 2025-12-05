/**
 * AutoCaptionDialog Component
 *
 * Dialog for generating captions automatically from audio/video clips
 * using speech-to-text transcription. Provides options for language,
 * speaker diarization, and other transcription settings.
 */

import {
  makeStyles,
  tokens,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  Text,
  ProgressBar,
  Dropdown,
  Option,
  Switch,
  Spinner,
} from '@fluentui/react-components';
import { Mic24Regular, Dismiss24Regular, Checkmark24Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import { isSpeechRecognitionSupported } from '../../../services/transcriptionService';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { useTranscriptionStore } from '../../../stores/opencutTranscription';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '480px',
  },
  dialogTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  titleIcon: {
    color: tokens.colorBrandForeground1,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalM,
  },
  optionGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  optionLabel: {
    fontWeight: 500,
  },
  optionDescription: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  optionRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} 0`,
  },
  progressSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  progressHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  progressText: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  warningBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorPaletteYellowBorder1}`,
  },
  warningText: {
    color: tokens.colorPaletteYellowForeground2,
    fontSize: tokens.fontSizeBase200,
  },
  successBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteGreenBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorPaletteGreenBorder1}`,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  dropdown: {
    minWidth: '160px',
  },
});

/**
 * Available languages for transcription
 */
const LANGUAGES = [
  { value: 'en-US', label: 'English (US)' },
  { value: 'en-GB', label: 'English (UK)' },
  { value: 'es-ES', label: 'Spanish' },
  { value: 'fr-FR', label: 'French' },
  { value: 'de-DE', label: 'German' },
  { value: 'it-IT', label: 'Italian' },
  { value: 'pt-BR', label: 'Portuguese (Brazil)' },
  { value: 'ja-JP', label: 'Japanese' },
  { value: 'ko-KR', label: 'Korean' },
  { value: 'zh-CN', label: 'Chinese (Simplified)' },
];

export interface AutoCaptionDialogProps {
  /** Whether the dialog is open */
  open: boolean;
  /** Callback when dialog is dismissed */
  onDismiss: () => void;
  /** Audio URL to transcribe */
  audioUrl: string;
  /** Clip ID for tracking results */
  clipId: string;
  /** Clip name for display */
  clipName?: string;
}

export const AutoCaptionDialog: FC<AutoCaptionDialogProps> = ({
  open,
  onDismiss,
  audioUrl,
  clipId,
  clipName = 'Selected clip',
}) => {
  const styles = useStyles();
  const transcriptionStore = useTranscriptionStore();
  const captionsStore = useOpenCutCaptionsStore();

  // Form state
  const [language, setLanguage] = useState('en-US');
  const [punctuate, setPunctuate] = useState(true);
  const [createNewTrack, setCreateNewTrack] = useState(true);
  const [selectedTrackId, setSelectedTrackId] = useState<string | null>(null);
  const [showSuccess, setShowSuccess] = useState(false);

  const isSupported = isSpeechRecognitionSupported();
  const { isTranscribing, progress, error } = transcriptionStore;
  const tracks = captionsStore.tracks;

  const handleStartTranscription = useCallback(async () => {
    setShowSuccess(false);

    await transcriptionStore.startTranscription(clipId, audioUrl, {
      language,
      punctuate,
    });

    // After transcription completes, apply to captions
    const result = transcriptionStore.getResult(clipId);
    if (result) {
      let trackId = selectedTrackId;

      // Create new track if needed
      if (createNewTrack || !trackId) {
        const langLabel = LANGUAGES.find((l) => l.value === language)?.label ?? 'Unknown';
        trackId = captionsStore.addTrack(`Auto-generated (${langLabel})`, language.split('-')[0]);
      }

      // Apply transcription to captions
      transcriptionStore.applyToCaptions(clipId, trackId);
      setShowSuccess(true);
    }
  }, [
    clipId,
    audioUrl,
    language,
    punctuate,
    createNewTrack,
    selectedTrackId,
    transcriptionStore,
    captionsStore,
  ]);

  const handleCancel = useCallback(() => {
    if (isTranscribing) {
      transcriptionStore.cancelTranscription();
    }
    setShowSuccess(false);
    onDismiss();
  }, [isTranscribing, transcriptionStore, onDismiss]);

  const handleClose = useCallback(() => {
    setShowSuccess(false);
    onDismiss();
  }, [onDismiss]);

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && handleClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogTitle className={styles.dialogTitle}>
          <Mic24Regular className={styles.titleIcon} />
          <span>Auto-Generate Captions</span>
        </DialogTitle>

        <DialogBody>
          <DialogContent>
            <div className={styles.content}>
              {/* Source info */}
              <div className={styles.optionGroup}>
                <Text className={styles.optionLabel}>Source</Text>
                <Text className={styles.optionDescription}>{clipName}</Text>
              </div>

              {/* Browser support warning */}
              {!isSupported && (
                <div className={styles.warningBox}>
                  <Text className={styles.warningText}>
                    Speech recognition is not supported in this browser. Please use Chrome, Edge, or
                    Safari for auto-captioning.
                  </Text>
                </div>
              )}

              {/* Language selection */}
              <div className={styles.optionGroup}>
                <Text className={styles.optionLabel}>Language</Text>
                <Dropdown
                  className={styles.dropdown}
                  value={LANGUAGES.find((l) => l.value === language)?.label ?? 'English (US)'}
                  onOptionSelect={(_, data) => setLanguage(data.optionValue as string)}
                  disabled={isTranscribing || !isSupported}
                >
                  {LANGUAGES.map((lang) => (
                    <Option key={lang.value} value={lang.value}>
                      {lang.label}
                    </Option>
                  ))}
                </Dropdown>
              </div>

              {/* Options */}
              <div className={styles.optionGroup}>
                <Text className={styles.optionLabel}>Options</Text>

                <div className={styles.optionRow}>
                  <div>
                    <Text>Add punctuation</Text>
                    <Text className={styles.optionDescription}>
                      Automatically add periods and commas
                    </Text>
                  </div>
                  <Switch
                    checked={punctuate}
                    onChange={(_, data) => setPunctuate(data.checked)}
                    disabled={isTranscribing || !isSupported}
                  />
                </div>

                <div className={styles.optionRow}>
                  <div>
                    <Text>Create new caption track</Text>
                    <Text className={styles.optionDescription}>
                      Add captions to a new track instead of existing
                    </Text>
                  </div>
                  <Switch
                    checked={createNewTrack}
                    onChange={(_, data) => setCreateNewTrack(data.checked)}
                    disabled={isTranscribing || !isSupported || tracks.length === 0}
                  />
                </div>

                {!createNewTrack && tracks.length > 0 && (
                  <Dropdown
                    className={styles.dropdown}
                    placeholder="Select track"
                    value={tracks.find((t) => t.id === selectedTrackId)?.name ?? ''}
                    onOptionSelect={(_, data) => setSelectedTrackId(data.optionValue as string)}
                    disabled={isTranscribing}
                  >
                    {tracks.map((track) => (
                      <Option key={track.id} value={track.id}>
                        {track.name}
                      </Option>
                    ))}
                  </Dropdown>
                )}
              </div>

              {/* Progress indicator */}
              {isTranscribing && progress && (
                <div className={styles.progressSection}>
                  <div className={styles.progressHeader}>
                    <div className={styles.progressText}>
                      <Spinner size="tiny" />
                      <Text>{progress.message}</Text>
                    </div>
                    <Text>{Math.round(progress.progress)}%</Text>
                  </div>
                  <ProgressBar value={progress.progress / 100} />
                </div>
              )}

              {/* Error display */}
              {error && !isTranscribing && (
                <div className={styles.warningBox}>
                  <Text className={styles.warningText}>{error}</Text>
                </div>
              )}

              {/* Success message */}
              {showSuccess && !isTranscribing && (
                <div className={styles.successBox}>
                  <Checkmark24Regular className={styles.successIcon} />
                  <Text>Captions generated successfully!</Text>
                </div>
              )}
            </div>
          </DialogContent>

          <DialogActions>
            <Button
              appearance="secondary"
              onClick={handleCancel}
              icon={isTranscribing ? <Dismiss24Regular /> : undefined}
            >
              {isTranscribing ? 'Cancel' : 'Close'}
            </Button>
            <Button
              appearance="primary"
              onClick={handleStartTranscription}
              disabled={isTranscribing || !isSupported || showSuccess}
              icon={<Mic24Regular />}
            >
              {isTranscribing ? 'Transcribing...' : 'Start Transcription'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default AutoCaptionDialog;
