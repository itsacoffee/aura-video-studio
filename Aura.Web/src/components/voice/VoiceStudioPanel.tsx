import {
  makeStyles,
  tokens,
  Text,
  Title3,
  Tab,
  TabList,
  Button,
  Spinner,
} from '@fluentui/react-components';
import {
  MicRegular,
  SoundWaveCircle24Regular as WaveformRegular,
  PlayRegular,
  SaveRegular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { apiUrl } from '../../config/api';
import { EmotionAdjuster } from './EmotionAdjuster';
import { ProsodyEditor } from './ProsodyEditor';
import { VoiceProfileSelector } from './VoiceProfileSelector';
import { VoiceSamplePlayer } from './VoiceSamplePlayer';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    height: '100%',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  icon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    flex: 1,
    overflow: 'auto',
  },
  tabContent: {
    padding: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  previewSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface VoiceStudioPanelProps {
  onVoiceChange?: (voiceId: string) => void;
  onEnhancementChange?: (config: VoiceEnhancementConfig) => void;
  onSave?: () => void;
}

export interface VoiceEnhancementConfig {
  enableNoiseReduction: boolean;
  noiseReductionStrength: number;
  enableEqualization: boolean;
  equalizationPreset: string;
  enableProsodyAdjustment: boolean;
  prosody?: {
    pitchShift: number;
    rateMultiplier: number;
    emphasisLevel: number;
    volumeAdjustment: number;
    pauseDurationMultiplier: number;
  };
  enableEmotionEnhancement: boolean;
  targetEmotion?: {
    emotion: string;
    intensity: number;
  };
}

export const VoiceStudioPanel = ({
  onVoiceChange,
  onEnhancementChange,
  onSave,
}: VoiceStudioPanelProps) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string>('voice');
  const [selectedVoiceId, setSelectedVoiceId] = useState<string>('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [enhancementConfig, setEnhancementConfig] = useState<VoiceEnhancementConfig>({
    enableNoiseReduction: true,
    noiseReductionStrength: 0.7,
    enableEqualization: true,
    equalizationPreset: 'balanced',
    enableProsodyAdjustment: false,
    enableEmotionEnhancement: false,
  });

  const handleVoiceSelect = (voiceId: string) => {
    setSelectedVoiceId(voiceId);
    onVoiceChange?.(voiceId);
  };

  const handleProsodyChange = (prosody: VoiceEnhancementConfig['prosody']) => {
    const updated = {
      ...enhancementConfig,
      prosody,
      enableProsodyAdjustment: true,
    };
    setEnhancementConfig(updated);
    onEnhancementChange?.(updated);
  };

  const handleEmotionChange = (emotion: string, intensity: number) => {
    const updated = {
      ...enhancementConfig,
      targetEmotion: { emotion, intensity },
      enableEmotionEnhancement: true,
    };
    setEnhancementConfig(updated);
    onEnhancementChange?.(updated);
  };

  const handlePreview = async () => {
    setIsProcessing(true);
    try {
      // Generate preview with current voice settings
      const response = await fetch(`${apiUrl}/api/v1/voice/preview`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          text: 'This is a preview of the selected voice with current settings.',
          voiceId: selectedVoiceId,
          prosodyConfig: enhancementConfig.prosody,
          enhancement: enhancementConfig,
        }),
      });

      if (response.ok) {
        const data = await response.json();
        if (data.audioUrl) {
          // Play the preview audio
          const audio = new Audio(data.audioUrl);
          audio.play().catch((err) => console.error('Failed to play preview:', err));
        }
      } else {
        console.warn('Preview generation failed');
        alert('Voice preview not available. This feature requires backend API support.');
      }
    } catch (error) {
      console.error('Error generating preview:', error);
      alert('Could not generate preview. Ensure the backend API is running.');
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <MicRegular className={styles.icon} />
          <Title3>Voice Studio</Title3>
        </div>
        <div className={styles.controls}>
          <Button
            appearance="subtle"
            icon={<PlayRegular />}
            onClick={handlePreview}
            disabled={isProcessing}
          >
            {isProcessing ? <Spinner size="tiny" /> : 'Preview'}
          </Button>
          <Button
            appearance="primary"
            icon={<SaveRegular />}
            onClick={onSave}
            disabled={!selectedVoiceId}
          >
            Save Profile
          </Button>
        </div>
      </div>

      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as string)}
      >
        <Tab value="voice" icon={<MicRegular />}>
          Voice Selection
        </Tab>
        <Tab value="prosody" icon={<WaveformRegular />}>
          Prosody
        </Tab>
        <Tab value="emotion">Emotion</Tab>
        <Tab value="preview">Preview</Tab>
      </TabList>

      <div className={styles.content}>
        {selectedTab === 'voice' && (
          <div className={styles.tabContent}>
            <VoiceProfileSelector
              selectedVoiceId={selectedVoiceId}
              onVoiceSelect={handleVoiceSelect}
            />
          </div>
        )}

        {selectedTab === 'prosody' && (
          <div className={styles.tabContent}>
            <ProsodyEditor prosody={enhancementConfig.prosody} onChange={handleProsodyChange} />
          </div>
        )}

        {selectedTab === 'emotion' && (
          <div className={styles.tabContent}>
            <EmotionAdjuster
              emotion={enhancementConfig.targetEmotion?.emotion}
              intensity={enhancementConfig.targetEmotion?.intensity ?? 0.5}
              onChange={handleEmotionChange}
            />
          </div>
        )}

        {selectedTab === 'preview' && (
          <div className={styles.tabContent}>
            <div className={styles.previewSection}>
              <Text weight="semibold">Audio Preview</Text>
              <VoiceSamplePlayer voiceId={selectedVoiceId} enhancementConfig={enhancementConfig} />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
