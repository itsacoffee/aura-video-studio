import { Button, Card, makeStyles, Text, Title3, tokens } from '@fluentui/react-components';
import {
  ArrowRight24Regular,
  Checkmark20Regular,
  Settings24Regular,
  Sparkle24Regular,
  VideoClip24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    alignItems: 'center',
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
    maxWidth: '700px',
    margin: '0 auto',
  },
  subtitle: {
    maxWidth: '500px',
    fontSize: '16px',
    lineHeight: '1.5',
    color: tokens.colorNeutralForeground2,
  },
  configCardsContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(3, 1fr)',
    gap: tokens.spacingHorizontalM,
    width: '100%',
    marginTop: tokens.spacingVerticalM,
  },
  configCard: {
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
    background: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalXS,
  },
  configIcon: {
    fontSize: '32px',
    color: tokens.colorBrandBackground,
  },
  configLabel: {
    fontSize: '13px',
    fontWeight: tokens.fontWeightSemibold,
  },
  ctaContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
    width: '100%',
  },
  primaryButton: {
    fontSize: '16px',
    padding: '14px 48px',
    height: 'auto',
    minWidth: '220px',
  },
  featureList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    alignItems: 'flex-start',
    textAlign: 'left',
    marginTop: tokens.spacingVerticalS,
  },
  featureItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForeground3,
    fontSize: '13px',
  },
  featureIcon: {
    color: tokens.colorPaletteGreenForeground1,
    fontSize: '16px',
  },
  timeNote: {
    color: tokens.colorNeutralForeground3,
    fontSize: '13px',
    marginTop: tokens.spacingVerticalS,
  },
});

export interface WelcomeScreenProps {
  onGetStarted: () => void;
  onImportProject?: () => void;
}

export function WelcomeScreen({
  onGetStarted,
  onImportProject: _onImportProject,
}: WelcomeScreenProps) {
  const styles = useStyles();

  const configSteps = [
    { icon: <VideoClip24Regular className={styles.configIcon} />, label: 'FFmpeg' },
    { icon: <Sparkle24Regular className={styles.configIcon} />, label: 'AI Providers' },
    { icon: <Settings24Regular className={styles.configIcon} />, label: 'Workspace' },
  ];

  const features = [
    'AI-powered script generation',
    'Text-to-speech synthesis',
    'Automatic video rendering',
  ];

  return (
    <div className={styles.container}>
      {/* Concise subtitle */}
      <Text className={styles.subtitle}>
        Complete a quick setup to configure your video generation environment.
      </Text>

      {/* Configuration cards showing what will be set up */}
      <div className={styles.configCardsContainer}>
        {configSteps.map((step, index) => (
          <Card key={index} className={styles.configCard}>
            {step.icon}
            <Title3 className={styles.configLabel}>{step.label}</Title3>
          </Card>
        ))}
      </div>

      {/* Primary CTA */}
      <div className={styles.ctaContainer}>
        <Button
          appearance="primary"
          size="large"
          className={styles.primaryButton}
          icon={<ArrowRight24Regular />}
          iconPosition="after"
          onClick={onGetStarted}
        >
          Get Started
        </Button>

        {/* Feature highlights */}
        <div className={styles.featureList}>
          {features.map((feature, index) => (
            <div key={index} className={styles.featureItem}>
              <Checkmark20Regular className={styles.featureIcon} />
              <span>{feature}</span>
            </div>
          ))}
        </div>

        <Text className={styles.timeNote}>Setup takes about 3-5 minutes</Text>
      </div>
    </div>
  );
}
