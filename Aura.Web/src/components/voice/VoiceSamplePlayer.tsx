import {
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  ProgressBar,
  Textarea,
} from '@fluentui/react-components';
import {
  PlayRegular,
  PauseRegular,
  ArrowDownloadRegular,
  SoundWaveCircle24Regular as WaveformRegular,
} from '@fluentui/react-icons';
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { apiUrl } from '../../config/api';
import type { VoiceEnhancementConfig } from './VoiceStudioPanel';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sampleTextSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  textInput: {
    width: '100%',
  },
  playerSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  controls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  playButton: {
    minWidth: '100px',
  },
  waveform: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '80px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    position: 'relative',
    overflow: 'hidden',
  },
  waveformBars: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
    height: '100%',
    padding: tokens.spacingHorizontalS,
  },
  waveformBar: {
    width: '4px',
    backgroundColor: tokens.colorBrandForeground1,
    borderRadius: tokens.borderRadiusSmall,
    transition: 'height 0.1s ease',
  },
  timeDisplay: {
    display: 'flex',
    justifyContent: 'space-between',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '80px',
    gap: tokens.spacingHorizontalM,
  },
  enhancementInfo: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
  },
  infoList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface VoiceSamplePlayerProps {
  voiceId: string;
  enhancementConfig?: VoiceEnhancementConfig;
}

export const VoiceSamplePlayer: React.FC<VoiceSamplePlayerProps> = ({
  voiceId,
  enhancementConfig,
}) => {
  const styles = useStyles();
  const [sampleText, setSampleText] = useState(
    'Welcome to Aura Video Studio. This is a sample of the selected voice with your enhancement settings.'
  );
  const [isPlaying, setIsPlaying] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [audioUrl, setAudioUrl] = useState<string | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const audioUrlRef = useRef<string | null>(null);

  // Default TTS provider when voiceId doesn't include provider prefix
  const DEFAULT_TTS_PROVIDER = 'Windows';

  // Keep ref in sync with state for cleanup purposes
  useEffect(() => {
    audioUrlRef.current = audioUrl;
  }, [audioUrl]);

  // Store current config in refs for stable access in generateSample
  const sampleTextRef = useRef(sampleText);
  const enhancementConfigRef = useRef(enhancementConfig);

  useEffect(() => {
    sampleTextRef.current = sampleText;
  }, [sampleText]);

  useEffect(() => {
    enhancementConfigRef.current = enhancementConfig;
  }, [enhancementConfig]);

  const generateSample = useCallback(async () => {
    setIsLoading(true);
    try {
      // Parse voiceId to extract provider and voice name
      // Format expected: "provider:voiceName" (e.g., "Windows:Microsoft David" or "ElevenLabs:Rachel")
      const [provider, voice] = voiceId.includes(':')
        ? voiceId.split(':', 2)
        : [DEFAULT_TTS_PROVIDER, voiceId];

      // Use refs for values that shouldn't trigger recreation
      const currentSampleText = sampleTextRef.current;
      const currentEnhancement = enhancementConfigRef.current;

      // Call API to generate voice preview with returnFile query param to get audio directly
      const response = await fetch(apiUrl('/api/tts/preview?returnFile=true'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider,
          voice,
          sampleText: currentSampleText,
          speed: currentEnhancement?.prosody?.rateMultiplier ?? 1.0,
          pitch: currentEnhancement?.prosody?.pitchShift ?? 0.0,
        }),
      });

      if (response.ok) {
        // The API returns the audio file directly when returnFile=true
        const audioBlob = await response.blob();
        const url = URL.createObjectURL(audioBlob);

        // Clean up previous audio URL to prevent memory leaks (using ref for stable reference)
        const prevUrl = audioUrlRef.current;
        if (prevUrl && prevUrl.startsWith('blob:')) {
          URL.revokeObjectURL(prevUrl);
        }

        setAudioUrl(url);
        // Duration will be set when audio metadata is loaded
        setDuration(0);
      } else {
        // Try to get error details from response
        let errorMessage = 'Sample generation failed';
        try {
          const errorData = await response.json();
          errorMessage = errorData.error || errorData.details || errorMessage;
        } catch {
          // Response wasn't JSON
        }
        console.warn('Voice preview generation failed:', errorMessage);
        setAudioUrl('');
        setDuration(0);
      }
    } catch (error) {
      console.error('Failed to generate voice preview:', error);
      setAudioUrl('');
      setDuration(0);
    } finally {
      setIsLoading(false);
    }
  }, [voiceId]); // Only depend on voiceId - other values accessed via refs

  // Clean up blob URL on unmount
  useEffect(() => {
    return () => {
      const urlToCleanup = audioUrlRef.current;
      if (urlToCleanup && urlToCleanup.startsWith('blob:')) {
        URL.revokeObjectURL(urlToCleanup);
      }
    };
  }, []);

  // Effect to trigger sample generation on voice change
  // Using a stable generateSample that only changes when voiceId changes
  useEffect(() => {
    if (voiceId) {
      generateSample();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [voiceId]); // Intentionally exclude generateSample - it's stable per voiceId

  const handlePlayPause = () => {
    if (!audioRef.current) return;

    if (isPlaying) {
      audioRef.current.pause();
    } else {
      audioRef.current.play();
    }
    setIsPlaying(!isPlaying);
  };

  const handleDownload = () => {
    if (audioUrl) {
      // Create a download link and trigger it
      const link = document.createElement('a');
      link.href = audioUrl;
      link.download = `voice-sample-${voiceId}.mp3`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  };

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const renderWaveform = () => {
    // Generate mock waveform bars
    const bars = Array.from({ length: 50 }, (_, i) => {
      const height = Math.random() * 60 + 20;
      return <div key={i} className={styles.waveformBar} style={{ height: `${height}%` }} />;
    });

    return <div className={styles.waveformBars}>{bars}</div>;
  };

  return (
    <div className={styles.container}>
      <div className={styles.sampleTextSection}>
        <Text weight="semibold">Sample Text</Text>
        <Textarea
          className={styles.textInput}
          value={sampleText}
          onChange={(_, data) => setSampleText(data.value)}
          placeholder="Enter text to preview the voice..."
          rows={3}
        />
        <Button
          appearance="primary"
          onClick={generateSample}
          disabled={!voiceId || isLoading || !sampleText}
        >
          {isLoading ? <Spinner size="tiny" /> : 'Generate Sample'}
        </Button>
      </div>

      <div className={styles.playerSection}>
        <Text weight="semibold">Audio Preview</Text>

        {isLoading ? (
          <div className={styles.loading}>
            <Spinner />
            <Text>Generating sample with enhancements...</Text>
          </div>
        ) : audioUrl ? (
          <>
            <div className={styles.waveform}>{renderWaveform()}</div>

            <ProgressBar value={currentTime} max={duration} />

            <div className={styles.timeDisplay}>
              <Text>{formatTime(currentTime)}</Text>
              <Text>{formatTime(duration)}</Text>
            </div>

            <div className={styles.controls}>
              <Button
                className={styles.playButton}
                appearance="primary"
                icon={isPlaying ? <PauseRegular /> : <PlayRegular />}
                onClick={handlePlayPause}
              >
                {isPlaying ? 'Pause' : 'Play'}
              </Button>
              <Button appearance="subtle" icon={<ArrowDownloadRegular />} onClick={handleDownload}>
                Download
              </Button>
            </div>

            {/* Audio preview without captions as it's a TTS voice sample for evaluation */}
            {/* eslint-disable-next-line jsx-a11y/media-has-caption */}
            <audio
              ref={audioRef}
              src={audioUrl}
              onTimeUpdate={(e) => setCurrentTime(e.currentTarget.currentTime)}
              onEnded={() => setIsPlaying(false)}
              onLoadedMetadata={(e) => setDuration(e.currentTarget.duration)}
            />
          </>
        ) : (
          <div className={styles.loading}>
            <WaveformRegular style={{ fontSize: '48px', opacity: 0.3 }} />
            <Text>Select a voice and generate a sample to preview</Text>
          </div>
        )}
      </div>

      {enhancementConfig && (
        <div className={styles.enhancementInfo}>
          <Text weight="semibold">Active Enhancements:</Text>
          <div className={styles.infoList}>
            {enhancementConfig.enableNoiseReduction && (
              <Text size={200}>
                ✓ Noise Reduction ({(enhancementConfig.noiseReductionStrength * 100).toFixed(0)}%)
              </Text>
            )}
            {enhancementConfig.enableEqualization && (
              <Text size={200}>✓ Equalization ({enhancementConfig.equalizationPreset})</Text>
            )}
            {enhancementConfig.enableProsodyAdjustment && (
              <Text size={200}>✓ Prosody Adjustment</Text>
            )}
            {enhancementConfig.enableEmotionEnhancement && (
              <Text size={200}>✓ Emotion: {enhancementConfig.targetEmotion?.emotion}</Text>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
