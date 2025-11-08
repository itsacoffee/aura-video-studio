import {
  Title2,
  Text,
  Field,
  Label,
  Dropdown,
  Option,
  Button,
  makeStyles,
  tokens,
  Spinner,
  Card,
} from '@fluentui/react-components';
import { PlayRegular, StopRegular } from '@fluentui/react-icons';
import { useEffect, useState, useCallback } from 'react';
import type { FC } from 'react';
import { ttsService } from '../../../services/ttsService';
import type { TtsProvider, TtsVoice } from '../../../services/ttsService';
import type { StyleData, BriefData, StepValidation } from '../types';

interface StyleSelectionProps {
  data: StyleData;
  briefData: BriefData;
  advancedMode: boolean;
  onChange: (data: StyleData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  fieldRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
  },
  voicePreviewCard: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  previewButton: {
    width: 'fit-content',
  },
  loadingText: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
});

export const StyleSelection: FC<StyleSelectionProps> = ({
  data,
  briefData,
  advancedMode,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const [providers, setProviders] = useState<TtsProvider[]>([]);
  const [voices, setVoices] = useState<TtsVoice[]>([]);
  const [loadingProviders, setLoadingProviders] = useState(true);
  const [loadingVoices, setLoadingVoices] = useState(false);
  const [playingPreview, setPlayingPreview] = useState(false);
  const [audio, setAudio] = useState<HTMLAudioElement | null>(null);

  useEffect(() => {
    const fetchProviders = async () => {
      try {
        setLoadingProviders(true);
        const availableProviders = await ttsService.getAvailableProviders();
        setProviders(availableProviders);

        if (availableProviders.length > 0 && !data.voiceProvider) {
          const defaultProvider = availableProviders.find(
            (p) => p.name === 'ElevenLabs' || p.name === 'EdgeTTS'
          );
          if (defaultProvider) {
            onChange({ ...data, voiceProvider: defaultProvider.name as never });
          } else {
            onChange({ ...data, voiceProvider: availableProviders[0].name as never });
          }
        }
      } catch (error) {
        console.error('Failed to load TTS providers:', error);
      } finally {
        setLoadingProviders(false);
      }
    };

    fetchProviders();
  }, []);

  useEffect(() => {
    const fetchVoices = async () => {
      if (!data.voiceProvider) return;

      try {
        setLoadingVoices(true);
        const availableVoices = await ttsService.getVoicesForProvider(data.voiceProvider);
        setVoices(availableVoices);

        if (availableVoices.length > 0 && !data.voiceName) {
          onChange({ ...data, voiceName: availableVoices[0].name });
        }
      } catch (error) {
        console.error('Failed to load voices:', error);
        setVoices([]);
      } finally {
        setLoadingVoices(false);
      }
    };

    fetchVoices();
  }, [data.voiceProvider]);

  useEffect(() => {
    const isValid = !!(data.voiceProvider && data.voiceName && data.visualStyle);
    onValidationChange({
      isValid,
      errors: isValid ? [] : ['Please select voice and visual style'],
    });
  }, [data, onValidationChange]);

  const handlePlayPreview = useCallback(async () => {
    if (playingPreview && audio) {
      audio.pause();
      setPlayingPreview(false);
      return;
    }

    if (!data.voiceProvider || !data.voiceName) {
      return;
    }

    try {
      setPlayingPreview(true);
      const sampleText = briefData.topic
        ? `Here's a preview of my voice reading about ${briefData.topic}.`
        : 'Hello, this is a sample of my voice. How does it sound?';

      const preview = await ttsService.generatePreview({
        provider: data.voiceProvider,
        voice: data.voiceName,
        sampleText,
      });

      const audioElement = new Audio(preview.audioPath);
      audioElement.onended = () => {
        setPlayingPreview(false);
        setAudio(null);
      };
      audioElement.onerror = () => {
        setPlayingPreview(false);
        setAudio(null);
        console.error('Failed to play audio preview');
      };

      setAudio(audioElement);
      await audioElement.play();
    } catch (error) {
      console.error('Failed to generate preview:', error);
      setPlayingPreview(false);
      setAudio(null);
    }
  }, [playingPreview, audio, data.voiceProvider, data.voiceName, briefData.topic]);

  return (
    <div className={styles.container}>
      <div>
        <Title2>Voice & Style Selection</Title2>
        <Text>Configure the voice, visual style, and music for your video.</Text>
      </div>

      <div className={styles.section}>
        <Field label="Voice Provider">
          {loadingProviders ? (
            <div>
              <Spinner size="tiny" />
              <Text className={styles.loadingText}>Loading providers...</Text>
            </div>
          ) : (
            <Dropdown
              placeholder="Select a TTS provider"
              value={data.voiceProvider}
              selectedOptions={[data.voiceProvider]}
              onOptionSelect={(_, option) => {
                onChange({ ...data, voiceProvider: option.optionValue as never, voiceName: '' });
              }}
            >
              {providers.map((provider) => (
                <Option key={provider.name} value={provider.name} text={provider.name}>
                  {provider.name} ({provider.tier})
                </Option>
              ))}
            </Dropdown>
          )}
        </Field>

        {data.voiceProvider && (
          <Field label="Voice">
            {loadingVoices ? (
              <div>
                <Spinner size="tiny" />
                <Text className={styles.loadingText}>Loading voices...</Text>
              </div>
            ) : (
              <Dropdown
                placeholder="Select a voice"
                value={data.voiceName}
                selectedOptions={[data.voiceName]}
                onOptionSelect={(_, option) => {
                  onChange({ ...data, voiceName: option.optionValue as string });
                }}
                disabled={voices.length === 0}
              >
                {voices.map((voice) => (
                  <Option key={voice.name} value={voice.name} text={voice.name}>
                    {voice.name}
                    {voice.gender && ` (${voice.gender})`}
                  </Option>
                ))}
              </Dropdown>
            )}
          </Field>
        )}

        {data.voiceProvider && data.voiceName && (
          <Card className={styles.voicePreviewCard}>
            <Label>Voice Preview</Label>
            <Button
              appearance="secondary"
              icon={playingPreview ? <StopRegular /> : <PlayRegular />}
              onClick={handlePlayPreview}
              disabled={!data.voiceName}
              className={styles.previewButton}
            >
              {playingPreview ? 'Stop Preview' : 'Play Preview'}
            </Button>
          </Card>
        )}
      </div>

      <div className={styles.section}>
        <Field label="Visual Style">
          <Dropdown
            placeholder="Select visual style"
            value={data.visualStyle}
            selectedOptions={[data.visualStyle]}
            onOptionSelect={(_, option) => {
              onChange({ ...data, visualStyle: option.optionValue as never });
            }}
          >
            <Option value="modern">Modern</Option>
            <Option value="minimal">Minimal</Option>
            <Option value="cinematic">Cinematic</Option>
            <Option value="playful">Playful</Option>
            <Option value="professional">Professional</Option>
          </Dropdown>
        </Field>
      </div>

      {advancedMode && (
        <div className={styles.section}>
          <Field label="Music Genre">
            <Dropdown
              placeholder="Select music genre"
              value={data.musicGenre}
              selectedOptions={[data.musicGenre]}
              onOptionSelect={(_, option) => {
                onChange({ ...data, musicGenre: option.optionValue as never });
              }}
            >
              <Option value="ambient">Ambient</Option>
              <Option value="upbeat">Upbeat</Option>
              <Option value="dramatic">Dramatic</Option>
              <Option value="none">None</Option>
            </Dropdown>
          </Field>
        </div>
      )}
    </div>
  );
};
