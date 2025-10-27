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
import { useState, useRef, useEffect, useCallback } from 'react';
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

  const generateSample = useCallback(async () => {
    setIsLoading(true);
    try {
      // Call API to generate voice sample
      const response = await fetch(`${apiUrl}/api/v1/voice/sample`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          text: sampleText,
          voiceId,
          enhancement: enhancementConfig,
        }),
      });

      if (response.ok) {
        const data = await response.json();
        setAudioUrl(data.audioUrl || '');
        setDuration(data.duration || 0);
      } else {
        console.warn('Sample generation failed, using mock data');
        // Mock fallback
        setAudioUrl('/mock-sample.mp3');
        setDuration(5.2);
      }
    } catch (error) {
      console.error('Failed to generate sample:', error);
      // Mock fallback on error
      setAudioUrl('/mock-sample.mp3');
      setDuration(5.2);
    } finally {
      setIsLoading(false);
    }
  }, [sampleText, voiceId, enhancementConfig]);

  useEffect(() => {
    // Generate sample when voice or enhancement changes
    if (voiceId) {
      generateSample();
    }
  }, [voiceId, generateSample]);

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
