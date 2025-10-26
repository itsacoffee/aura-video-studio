import { makeStyles, tokens, Title1, Title3, Text, Card, Button } from '@fluentui/react-components';
import {
  Checkmark24Regular,
  VideoClip24Regular,
  Apps24Regular,
  Lightbulb24Regular,
  BookOpen24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
    alignItems: 'center',
    textAlign: 'center',
  },
  successIcon: {
    fontSize: '96px',
    color: tokens.colorPaletteGreenForeground1,
    marginBottom: tokens.spacingVerticalL,
    animation: 'checkmarkAnimation 0.6s ease-in-out',
  },
  '@keyframes checkmarkAnimation': {
    '0%': { opacity: 0, transform: 'scale(0)' },
    '50%': { transform: 'scale(1.2)' },
    '100%': { opacity: 1, transform: 'scale(1)' },
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
  },
  subtitle: {
    maxWidth: '600px',
    marginBottom: tokens.spacingVerticalL,
  },
  summaryCard: {
    padding: tokens.spacingVerticalXL,
    width: '100%',
    maxWidth: '600px',
    textAlign: 'left',
    marginTop: tokens.spacingVerticalL,
  },
  summaryList: {
    listStyleType: 'none',
    padding: 0,
    margin: `${tokens.spacingVerticalM} 0`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  summaryItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  checkIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  tipsContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingHorizontalL,
    width: '100%',
    marginTop: tokens.spacingVerticalXL,
  },
  tipCard: {
    padding: tokens.spacingVerticalL,
    textAlign: 'left',
  },
  tipIcon: {
    fontSize: '32px',
    marginBottom: tokens.spacingVerticalS,
    display: 'block',
  },
  buttonContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalXL,
    justifyContent: 'center',
    flexWrap: 'wrap',
  },
});

export interface CompletionStepProps {
  summary: {
    tier: 'free' | 'pro';
    apiKeysConfigured: string[];
    hardwareDetected: boolean;
    componentsInstalled: string[];
  };
  onCreateFirstVideo: () => void;
  onExploreApp: () => void;
}

export function CompletionStep({ summary, onCreateFirstVideo, onExploreApp }: CompletionStepProps) {
  const styles = useStyles();

  const quickStartTips = [
    {
      icon: <VideoClip24Regular className={styles.tipIcon} />,
      title: 'Start Simple',
      description: 'Try creating a short 30-second video first to get familiar with the workflow.',
    },
    {
      icon: <BookOpen24Regular className={styles.tipIcon} />,
      title: 'Explore Templates',
      description: 'Use our pre-built templates to jumpstart your projects with proven structures.',
    },
    {
      icon: <Settings24Regular className={styles.tipIcon} />,
      title: 'Customize Settings',
      description: 'Visit Settings to fine-tune quality, performance, and provider preferences.',
    },
  ];

  return (
    <div className={styles.container}>
      <Checkmark24Regular className={styles.successIcon} />

      <div>
        <Title1 className={styles.title}>All Set!</Title1>
        <Text className={styles.subtitle} size={400}>
          Your system is configured and ready to create amazing videos. Let&apos;s get started!
        </Text>
      </div>

      <Card className={styles.summaryCard}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Configuration Summary</Title3>
        <ul className={styles.summaryList}>
          <li className={styles.summaryItem}>
            <Checkmark24Regular className={styles.checkIcon} />
            <Text>
              <strong>Tier:</strong> {summary.tier === 'free' ? 'Free (Stock)' : 'Pro (AI-Powered)'}
            </Text>
          </li>
          {summary.apiKeysConfigured.length > 0 && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.checkIcon} />
              <Text>
                <strong>API Keys:</strong> {summary.apiKeysConfigured.join(', ')}
              </Text>
            </li>
          )}
          {summary.hardwareDetected && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.checkIcon} />
              <Text>
                <strong>Hardware:</strong> Detected and optimized
              </Text>
            </li>
          )}
          {summary.componentsInstalled.length > 0 && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.checkIcon} />
              <Text>
                <strong>Components:</strong> {summary.componentsInstalled.join(', ')}
              </Text>
            </li>
          )}
        </ul>
      </Card>

      <div style={{ width: '100%' }}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>
          <Lightbulb24Regular
            style={{ verticalAlign: 'middle', marginRight: tokens.spacingHorizontalS }}
          />
          Quick Start Tips
        </Title3>
        <div className={styles.tipsContainer}>
          {quickStartTips.map((tip, index) => (
            <Card key={index} className={styles.tipCard}>
              {tip.icon}
              <Text
                weight="semibold"
                style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}
              >
                {tip.title}
              </Text>
              <Text size={200}>{tip.description}</Text>
            </Card>
          ))}
        </div>
      </div>

      <div className={styles.buttonContainer}>
        <Button
          appearance="primary"
          size="large"
          icon={<VideoClip24Regular />}
          onClick={onCreateFirstVideo}
        >
          Create Your First Video
        </Button>
        <Button appearance="secondary" size="large" icon={<Apps24Regular />} onClick={onExploreApp}>
          Explore the App
        </Button>
      </div>
    </div>
  );
}
