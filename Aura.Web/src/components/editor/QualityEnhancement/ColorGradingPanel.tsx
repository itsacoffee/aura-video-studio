/**
 * Color Grading Panel Component
 * UI for mood-based color grading and time-of-day detection
 */

import React, { useState } from 'react';
import {
  Card,
  makeStyles,
  tokens,
  Button,
  Dropdown,
  Option,
  Field,

  Body1Strong,
  Caption1,
  ProgressBar,
  Spinner,
  Badge,
} from '@fluentui/react-components';
import { Color24Regular, CheckmarkCircle24Regular } from '@fluentui/react-icons';
import {
  visualAnalysisService,
  ColorMood,
  TimeOfDay,
  type ColorGradingProfile,
} from '../../../services/analysis/VisualAnalysisService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  profileCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalS,
  },
  adjustmentRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXS,
  },
});

interface ColorGradingPanelProps {
  sceneIndex?: number;
  onApplyEnhancement?: (enhancement: any) => void;
}

export const ColorGradingPanel: React.FC<ColorGradingPanelProps> = ({
  sceneIndex,
  onApplyEnhancement,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [profile, setProfile] = useState<ColorGradingProfile | null>(null);
  const [selectedMood, setSelectedMood] = useState<ColorMood>(ColorMood.Natural);
  const [sentiment, setSentiment] = useState('neutral');
  const [detectedTime, setDetectedTime] = useState<TimeOfDay | null>(null);

  const handleAnalyze = async () => {
    setLoading(true);
    try {
      // Detect time of day from placeholder color histogram
      const colorHistogram = {
        red: 0.3,
        blue: 0.25,
        green: 0.28,
        brightness: 0.6,
      };
      
      const timeOfDay = await visualAnalysisService.detectTimeOfDay(colorHistogram);
      setDetectedTime(timeOfDay);

      // Analyze color grading
      const gradingProfile = await visualAnalysisService.analyzeColorGrading(
        'video',
        sentiment,
        timeOfDay
      );
      
      setProfile(gradingProfile);
    } catch (error) {
      console.error('Failed to analyze color grading:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApply = () => {
    if (profile) {
      onApplyEnhancement?.({
        type: 'colorGrading',
        profile,
        sceneIndex,
      });
    }
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.section}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS, marginBottom: tokens.spacingVerticalM }}>
            <Color24Regular />
            <Body1Strong>Mood-Based Color Grading</Body1Strong>
          </div>

          <Field label="Target Mood">
            <Dropdown
              value={selectedMood}
              selectedOptions={[selectedMood]}
              onOptionSelect={(_e, data) => setSelectedMood(data.optionValue as ColorMood)}
            >
              <Option value={ColorMood.Natural}>Natural</Option>
              <Option value={ColorMood.Cinematic}>Cinematic</Option>
              <Option value={ColorMood.Vibrant}>Vibrant</Option>
              <Option value={ColorMood.Warm}>Warm</Option>
              <Option value={ColorMood.Cool}>Cool</Option>
              <Option value={ColorMood.Dramatic}>Dramatic</Option>
            </Dropdown>
          </Field>

          <Field label="Content Sentiment" style={{ marginTop: tokens.spacingVerticalM }}>
            <Dropdown
              value={sentiment}
              selectedOptions={[sentiment]}
              onOptionSelect={(_e, data) => setSentiment(data.optionValue as string)}
            >
              <Option value="energetic">Energetic</Option>
              <Option value="calm">Calm</Option>
              <Option value="dramatic">Dramatic</Option>
              <Option value="warm">Warm</Option>
              <Option value="neutral">Neutral</Option>
            </Dropdown>
          </Field>

          <Button
            appearance="primary"
            onClick={handleAnalyze}
            disabled={loading}
            icon={loading ? <Spinner size="tiny" /> : <Color24Regular />}
            style={{ marginTop: tokens.spacingVerticalL }}
          >
            {loading ? 'Analyzing...' : 'Analyze Color Grading'}
          </Button>
        </div>
      </Card>

      {detectedTime && (
        <Card>
          <div className={styles.section}>
            <Body1Strong>Detected Conditions</Body1Strong>
            <div style={{ marginTop: tokens.spacingVerticalS }}>
              <Caption1>Time of Day: </Caption1>
              <Badge appearance="tint" color="brand">{detectedTime}</Badge>
            </div>
          </div>
        </Card>
      )}

      {profile && (
        <Card>
          <div className={styles.profileCard}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: tokens.spacingVerticalM }}>
              <div>
                <Body1Strong>{profile.name} Profile</Body1Strong>
                <Caption1 block>Mood: {profile.mood}</Caption1>
              </div>
              <Button
                appearance="primary"
                onClick={handleApply}
                icon={<CheckmarkCircle24Regular />}
              >
                Apply Profile
              </Button>
            </div>

            <div style={{ marginTop: tokens.spacingVerticalM }}>
              <Caption1 block>Adjustments</Caption1>
              
              <div className={styles.adjustmentRow}>
                <Caption1>Saturation</Caption1>
                <ProgressBar value={profile.saturation} thickness="large" style={{ width: 120 }} />
                <Caption1>{profile.saturation.toFixed(2)}</Caption1>
              </div>

              <div className={styles.adjustmentRow}>
                <Caption1>Contrast</Caption1>
                <ProgressBar value={profile.contrast} thickness="large" style={{ width: 120 }} />
                <Caption1>{profile.contrast.toFixed(2)}</Caption1>
              </div>

              <div className={styles.adjustmentRow}>
                <Caption1>Brightness</Caption1>
                <ProgressBar value={profile.brightness} thickness="large" style={{ width: 120 }} />
                <Caption1>{profile.brightness.toFixed(2)}</Caption1>
              </div>

              <div className={styles.adjustmentRow}>
                <Caption1>Temperature</Caption1>
                <ProgressBar 
                  value={(profile.temperature + 0.5) / 1} 
                  thickness="large" 
                  style={{ width: 120 }} 
                />
                <Caption1>{profile.temperature.toFixed(2)}</Caption1>
              </div>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
};

export default ColorGradingPanel;
