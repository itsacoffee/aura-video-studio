import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Field,
  Input,
  ProgressBar,
  MessageBar,
  MessageBarBody,
  Spinner,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import type { GriffelStyle } from '@fluentui/react-components';
import { Dismiss24Regular, MicRegular, Add24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useRef } from 'react';
import type { FC } from 'react';
import { useVoiceStore } from '@/stores/voiceStore';
import type { ClonedVoice } from '@/stores/voiceStore';

const useStyles = makeStyles({
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  dropzone: {
    border: `2px dashed ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalXL,
    textAlign: 'center' as const,
    cursor: 'pointer',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  dropzoneActive: {
    borderColor: tokens.colorBrandStroke1,
    backgroundColor: tokens.colorBrandBackground2,
  } as GriffelStyle,
  sampleList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  sampleItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
  processingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
  },
  previewContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    alignItems: 'center',
  },
  audioPlayer: {
    width: '100%',
  },
  successIcon: {
    fontSize: '48px',
    color: tokens.colorPaletteGreenForeground1,
  },
});

interface VoiceCloningWizardProps {
  open: boolean;
  onClose: () => void;
  onComplete: (voice: ClonedVoice) => void;
}

type WizardStep = 'upload' | 'processing' | 'preview' | 'complete';

const ACCEPTED_FORMATS = '.mp3,.wav,.m4a';
const MAX_SAMPLES = 5;
const MIN_FILE_SIZE = 10000; // 10KB minimum
const MAX_FILE_SIZE = 50_000_000; // 50MB maximum

export const VoiceCloningWizard: FC<VoiceCloningWizardProps> = ({
  open,
  onClose,
  onComplete,
}) => {
  const styles = useStyles();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState<WizardStep>('upload');
  const [samples, setSamples] = useState<File[]>([]);
  const [voiceName, setVoiceName] = useState('');
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isDragActive, setIsDragActive] = useState(false);
  const [createdVoice, setCreatedVoice] = useState<ClonedVoice | null>(null);

  const {
    cloningInProgress,
    cloningProgress,
    cloningError,
    createClonedVoice,
    previewVoice,
  } = useVoiceStore();

  const validateFile = (file: File): string | null => {
    const extension = file.name.toLowerCase().split('.').pop();
    if (!extension || !['mp3', 'wav', 'm4a'].includes(extension)) {
      return `Invalid format: ${file.name}. Use MP3, WAV, or M4A.`;
    }
    if (file.size < MIN_FILE_SIZE) {
      return `File too small: ${file.name}. Minimum 10 seconds of audio required.`;
    }
    if (file.size > MAX_FILE_SIZE) {
      return `File too large: ${file.name}. Maximum 50MB allowed.`;
    }
    return null;
  };

  const handleFilesAdded = useCallback(
    (files: FileList | File[]) => {
      const fileArray = Array.from(files);
      const validFiles: File[] = [];
      const errors: string[] = [];

      for (const file of fileArray) {
        const validationError = validateFile(file);
        if (validationError) {
          errors.push(validationError);
        } else if (samples.length + validFiles.length < MAX_SAMPLES) {
          validFiles.push(file);
        } else {
          errors.push(`Maximum ${MAX_SAMPLES} samples allowed.`);
          break;
        }
      }

      if (errors.length > 0) {
        setError(errors.join(' '));
      } else {
        setError(null);
      }

      if (validFiles.length > 0) {
        setSamples((prev) => [...prev, ...validFiles].slice(0, MAX_SAMPLES));
      }
    },
    [samples.length]
  );

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(false);
  }, []);

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setIsDragActive(false);
      handleFilesAdded(e.dataTransfer.files);
    },
    [handleFilesAdded]
  );

  const handleFileInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      if (e.target.files) {
        handleFilesAdded(e.target.files);
      }
    },
    [handleFilesAdded]
  );

  const removeSample = useCallback((index: number) => {
    setSamples((prev) => prev.filter((_, i) => i !== index));
  }, []);

  const startCloning = async () => {
    if (!voiceName.trim() || samples.length === 0) {
      setError('Please provide a voice name and at least one audio sample.');
      return;
    }

    setStep('processing');
    setError(null);

    const voice = await createClonedVoice(voiceName.trim(), samples);

    if (voice) {
      setCreatedVoice(voice);

      // Generate preview
      const url = await previewVoice(voice.id);
      if (url) {
        setPreviewUrl(url);
      }

      setStep('preview');
    } else {
      setError(cloningError || 'Voice cloning failed. Please try again.');
      setStep('upload');
    }
  };

  const handleComplete = () => {
    if (createdVoice) {
      onComplete(createdVoice);
    }
    handleClose();
  };

  const handleClose = () => {
    setStep('upload');
    setSamples([]);
    setVoiceName('');
    setPreviewUrl(null);
    setError(null);
    setCreatedVoice(null);
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={() => handleClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<Dismiss24Regular />}
                onClick={handleClose}
              />
            }
          >
            🎤 Clone Your Voice
          </DialogTitle>

          <DialogContent className={styles.dialogContent}>
            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}

            {step === 'upload' && (
              <>
                <Field label="Voice Name" required>
                  <Input
                    value={voiceName}
                    onChange={(_, data) => setVoiceName(data.value)}
                    placeholder="Enter a name for your voice"
                  />
                </Field>

                <div
                  className={`${styles.dropzone} ${isDragActive ? styles.dropzoneActive : ''}`}
                  onDragOver={handleDragOver}
                  onDragLeave={handleDragLeave}
                  onDrop={handleDrop}
                  onClick={() => fileInputRef.current?.click()}
                >
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept={ACCEPTED_FORMATS}
                    multiple
                    style={{ display: 'none' }}
                    onChange={handleFileInputChange}
                  />
                  <MicRegular style={{ fontSize: '32px', marginBottom: '8px' }} />
                  {isDragActive ? (
                    <Text>Drop audio files here...</Text>
                  ) : (
                    <>
                      <Text weight="semibold">
                        Drag & drop audio samples here
                      </Text>
                      <Text size={200}>
                        MP3, WAV, or M4A • 10s-5min each • Up to 5 files
                      </Text>
                    </>
                  )}
                </div>

                {samples.length > 0 && (
                  <div className={styles.sampleList}>
                    {samples.map((sample, index) => (
                      <div key={sample.name + index} className={styles.sampleItem}>
                        <Text>{sample.name}</Text>
                        <Button
                          appearance="subtle"
                          icon={<Dismiss24Regular />}
                          onClick={() => removeSample(index)}
                          aria-label="Remove sample"
                        />
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}

            {step === 'processing' && (
              <div className={styles.processingContainer}>
                <Spinner size="large" />
                <Text weight="semibold">Creating your voice clone...</Text>
                <ProgressBar value={cloningProgress / 100} />
                <Text size={200}>
                  This may take a minute. Please don&apos;t close this dialog.
                </Text>
              </div>
            )}

            {step === 'preview' && (
              <div className={styles.previewContainer}>
                <Text weight="semibold" size={400}>
                  ✨ Voice Created Successfully!
                </Text>
                <Text>Preview your new voice:</Text>
                {previewUrl && (
                  <audio
                    controls
                    src={previewUrl}
                    className={styles.audioPlayer}
                  />
                )}
                {!previewUrl && (
                  <Text size={200}>
                    Preview not available. You can still use the voice.
                  </Text>
                )}
              </div>
            )}
          </DialogContent>

          <DialogActions>
            {step === 'upload' && (
              <>
                <Button appearance="secondary" onClick={handleClose}>
                  Cancel
                </Button>
                <Button
                  appearance="primary"
                  icon={<Add24Regular />}
                  disabled={!voiceName.trim() || samples.length === 0 || cloningInProgress}
                  onClick={startCloning}
                >
                  Create Voice Clone
                </Button>
              </>
            )}

            {step === 'processing' && (
              <Button appearance="secondary" onClick={handleClose}>
                Cancel
              </Button>
            )}

            {step === 'preview' && (
              <Button appearance="primary" onClick={handleComplete}>
                Save Voice
              </Button>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
