import React, { useState } from 'react';
import {
  Input,
  Button,
  Textarea,
  makeStyles,
  tokens,
  Text,
  Spinner,
} from '@fluentui/react-components';
import { SparkleRegular, SendRegular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  icon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  inputSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  textArea: {
    minHeight: '100px',
  },
  optionsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalS,
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
  },
});

interface BrainstormInputProps {
  onBrainstorm: (topic: string, options: BrainstormOptions) => void;
  loading?: boolean;
}

export interface BrainstormOptions {
  audience?: string;
  tone?: string;
  targetDuration?: number;
  platform?: string;
}

export const BrainstormInput: React.FC<BrainstormInputProps> = ({
  onBrainstorm,
  loading = false,
}) => {
  const styles = useStyles();
  const [topic, setTopic] = useState('');
  const [audience, setAudience] = useState('');
  const [tone, setTone] = useState('');
  const [targetDuration, setTargetDuration] = useState('');
  const [platform, setPlatform] = useState('');

  const handleBrainstorm = () => {
    if (!topic.trim()) {
      return;
    }

    const options: BrainstormOptions = {
      audience: audience.trim() || undefined,
      tone: tone.trim() || undefined,
      targetDuration: targetDuration ? parseInt(targetDuration) : undefined,
      platform: platform.trim() || undefined,
    };

    onBrainstorm(topic.trim(), options);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && e.ctrlKey) {
      handleBrainstorm();
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <SparkleRegular className={styles.icon} />
        <Text size={500} weight="semibold">
          Brainstorm Video Concepts
        </Text>
      </div>

      <div className={styles.inputSection}>
        <Text weight="semibold">What&apos;s your video topic?</Text>
        <Textarea
          className={styles.textArea}
          placeholder="Enter your video topic or idea (e.g., 'How to start a successful podcast')"
          value={topic}
          onChange={(e) => setTopic(e.target.value)}
          onKeyDown={handleKeyPress}
          disabled={loading}
        />
      </div>

      <Text weight="semibold">Optional Details (Helps refine concepts):</Text>

      <div className={styles.optionsGrid}>
        <div>
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            Target Audience
          </Text>
          <Input
            placeholder="e.g., Beginners, Professionals"
            value={audience}
            onChange={(e) => setAudience(e.target.value)}
            disabled={loading}
          />
        </div>

        <div>
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            Tone
          </Text>
          <Input
            placeholder="e.g., Casual, Professional, Humorous"
            value={tone}
            onChange={(e) => setTone(e.target.value)}
            disabled={loading}
          />
        </div>

        <div>
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            Duration (seconds)
          </Text>
          <Input
            type="number"
            placeholder="e.g., 60, 300"
            value={targetDuration}
            onChange={(e) => setTargetDuration(e.target.value)}
            disabled={loading}
          />
        </div>

        <div>
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            Platform
          </Text>
          <Input
            placeholder="e.g., YouTube, TikTok, Instagram"
            value={platform}
            onChange={(e) => setPlatform(e.target.value)}
            disabled={loading}
          />
        </div>
      </div>

      {loading ? (
        <div className={styles.loadingContainer}>
          <Spinner size="tiny" />
          <Text>Generating creative concepts...</Text>
        </div>
      ) : (
        <div className={styles.actions}>
          <Button
            appearance="primary"
            icon={<SendRegular />}
            onClick={handleBrainstorm}
            disabled={!topic.trim()}
          >
            Generate Concepts
          </Button>
        </div>
      )}
    </div>
  );
};
