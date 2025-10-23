import React, { useState } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Title3,
  Tab,
  TabList,
  Card,
  Button,
  Spinner,
} from '@fluentui/react-components';
import {
  MicRegular,
  WaveformRegular,
  PlayRegular,
  SaveRegular,
} from '@fluentui/react-icons';
import { VoiceProfileSelector } from './VoiceProfileSelector';
import { ProsodyEditor } from './ProsodyEditor';
import { EmotionAdjuster } from './EmotionAdjuster';
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
  };
  enableEmotionEnhancement: boolean;
  targetEmotion?: {
    emotion: string;
    intensity: number;
  };
}

export const VoiceStudioPanel: React.FC<VoiceStudioPanelProps> = ({
  onVoiceChange,
  onEnhancementChange,
  onSave,
}) => {
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

  const handleProsodyChange = (prosody: any) => {
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
      // TODO: Implement preview functionality
      await new Promise(resolve => setTimeout(resolve, 1000));
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
            <ProsodyEditor
              prosody={enhancementConfig.prosody}
              onChange={handleProsodyChange}
            />
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
              <VoiceSamplePlayer
                voiceId={selectedVoiceId}
                enhancementConfig={enhancementConfig}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
