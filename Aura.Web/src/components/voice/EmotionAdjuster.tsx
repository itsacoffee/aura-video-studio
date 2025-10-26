import { makeStyles, tokens, Text, Slider, Card, Badge } from '@fluentui/react-components';
import { useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  emotionGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  emotionCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    textAlign: 'center',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  selectedEmotion: {
    backgroundColor: tokens.colorBrandBackground2,
    borderColor: tokens.colorBrandForeground1 as string,
    borderWidth: '2px' as string,
  },
  emotionIcon: {
    fontSize: '32px',
    marginBottom: tokens.spacingVerticalS,
  },
  emotionName: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  intensitySection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  sliderHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  sliderLabel: {
    fontWeight: tokens.fontWeightSemibold,
  },
  sliderValue: {
    color: tokens.colorBrandForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  description: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    marginTop: tokens.spacingVerticalS,
  },
  previewSection: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandBackground}`,
  },
});

interface EmotionAdjusterProps {
  emotion?: string;
  intensity?: number;
  onChange: (emotion: string, intensity: number) => void;
}

const emotions = [
  { name: 'Neutral', icon: '😐', description: 'Balanced, professional tone' },
  { name: 'Happy', icon: '😊', description: 'Cheerful and upbeat' },
  { name: 'Excited', icon: '🤩', description: 'Energetic and enthusiastic' },
  { name: 'Calm', icon: '😌', description: 'Soothing and peaceful' },
  { name: 'Confident', icon: '😎', description: 'Assured and authoritative' },
  { name: 'Sad', icon: '😔', description: 'Melancholic and somber' },
  { name: 'Angry', icon: '😠', description: 'Intense and forceful' },
  { name: 'Fearful', icon: '😰', description: 'Anxious and uncertain' },
  { name: 'Surprised', icon: '😲', description: 'Amazed and astonished' },
  { name: 'Empathetic', icon: '🤗', description: 'Caring and understanding' },
];

export const EmotionAdjuster: React.FC<EmotionAdjusterProps> = ({
  emotion = 'Neutral',
  intensity = 0.5,
  onChange,
}) => {
  const styles = useStyles();
  const [selectedEmotion, setSelectedEmotion] = useState(emotion);
  const [emotionIntensity, setEmotionIntensity] = useState(intensity);

  const handleEmotionSelect = (emotionName: string) => {
    setSelectedEmotion(emotionName);
    onChange(emotionName, emotionIntensity);
  };

  const handleIntensityChange = (value: number) => {
    setEmotionIntensity(value);
    onChange(selectedEmotion, value);
  };

  const getIntensityLabel = (value: number) => {
    if (value < 0.3) return 'Subtle';
    if (value < 0.7) return 'Moderate';
    return 'Strong';
  };

  const selectedEmotionData = emotions.find((e) => e.name === selectedEmotion);

  return (
    <div className={styles.container}>
      <div>
        <Text weight="semibold" size={400}>
          Select Emotion
        </Text>
        <Text className={styles.description}>Choose the emotional tone for the voice</Text>
      </div>

      <div className={styles.emotionGrid}>
        {emotions.map((emo) => (
          <Card
            key={emo.name}
            className={`${styles.emotionCard} ${
              selectedEmotion === emo.name ? styles.selectedEmotion : ''
            }`}
            onClick={() => handleEmotionSelect(emo.name)}
          >
            <div className={styles.emotionIcon}>{emo.icon}</div>
            <Text className={styles.emotionName}>{emo.name}</Text>
          </Card>
        ))}
      </div>

      {selectedEmotionData && (
        <div className={styles.previewSection}>
          <Text weight="semibold">Selected: {selectedEmotionData.name}</Text>
          <Text size={200} className={styles.description}>
            {selectedEmotionData.description}
          </Text>
        </div>
      )}

      <div className={styles.intensitySection}>
        <div className={styles.sliderHeader}>
          <Text className={styles.sliderLabel}>Emotion Intensity</Text>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, alignItems: 'center' }}>
            <Text className={styles.sliderValue}>{(emotionIntensity * 100).toFixed(0)}%</Text>
            <Badge appearance="tint" color="brand">
              {getIntensityLabel(emotionIntensity)}
            </Badge>
          </div>
        </div>
        <Slider
          min={0}
          max={1}
          step={0.05}
          value={emotionIntensity}
          onChange={(_, data) => handleIntensityChange(data.value)}
        />
        <Text className={styles.description}>
          Adjust how strongly the emotion is expressed in the voice
        </Text>
      </div>

      <div className={styles.previewSection}>
        <Text weight="semibold" size={300}>
          💡 Enhancement Tips
        </Text>
        <ul style={{ marginTop: tokens.spacingVerticalS, paddingLeft: '20px' }}>
          <li>
            <Text size={200}>Higher intensity creates more expressive and emotional speech</Text>
          </li>
          <li>
            <Text size={200}>
              Lower intensity maintains professionalism with subtle emotional cues
            </Text>
          </li>
          <li>
            <Text size={200}>Combine with prosody adjustments for best results</Text>
          </li>
        </ul>
      </div>
    </div>
  );
};
